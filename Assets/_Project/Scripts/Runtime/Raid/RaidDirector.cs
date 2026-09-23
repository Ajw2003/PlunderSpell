using System;
using PurrNet;
using RogueAi.Alarm;
using RogueAi.Castle;
using RogueAi.Extraction;
using RogueAi.Guards;
using RogueAi.Inventory;
using RogueAi.Lair;
using UnityEngine;

namespace RogueAi.Raid
{
    /// <summary>
    /// Runs the game loop: Lair → castle → raid → extraction → Lair, with the takings applied to the
    /// debt. Everything else in the project is a system; this is the thing that makes them a game.
    ///
    /// One seed drives the whole raid. It is chosen on the server, replicated by
    /// <see cref="CastleNetworkManager"/>, and feeds both the castle layout and the loot plan, so
    /// four peers build an identical castle holding identical treasure without a byte of layout or
    /// loot data crossing the wire.
    ///
    /// Server authority: phase transitions, seed choice, loot spawning and the final tally all happen
    /// on the server. Clients follow via the replicated phase and the extraction broadcast.
    /// </summary>
    public class RaidDirector : NetworkBehaviour
    {
        [Header("Scene wiring")]
        [Tooltip("Generates the castle from the raid seed.")]
        [SerializeField] private ProceduralCastleGenerator _generator;

        [Tooltip("Replicates the seed to every peer. Optional in single-player.")]
        [SerializeField] private CastleNetworkManager _castleNetwork;

        [Tooltip("Spawns the haul into the generated castle.")]
        [SerializeField] private LootSpawner _lootSpawner;

        [Tooltip("Spawns the garrison into the generated castle.")]
        [SerializeField] private GuardSpawner _guardSpawner;

        [Tooltip("The zone that ends the raid.")]
        [SerializeField] private ExtractionZone _extractionZone;

        [Tooltip("Between-raids meta-progression: debt, banked gold, era.")]
        [SerializeField] private LairHubManager _lair;

        [Tooltip("The castle's alert level. Reset at the start of every raid.")]
        [SerializeField] private AlarmFSMManager _alarm;

        [Tooltip("Bakes the walkable surface over the generated castle. Without it guards cannot move.")]
        [SerializeField] private CastleNavMeshBaker _navigation;

        [Tooltip("The player to stand just inside the gatehouse when the castle is built. The seed " +
                 "is rolled per raid, so a spawn baked into the scene is only right for one of them.")]
        [SerializeField] private Transform _playerRoot;

        [Header("Raid setup")]
        [Tooltip("Seed for the next raid. Left at 0, a fresh one is rolled per raid.")]
        [SerializeField] private int _fixedSeed;

        // Replicated so a late-joining client knows what is going on without asking.
        private readonly SyncVar<RaidPhase> _phase = new SyncVar<RaidPhase>(RaidPhase.InLair);
        private readonly SyncVar<int> _seed = new SyncVar<int>(0);

        /// <summary>Where the session is in the loop.</summary>
        public RaidPhase Phase => _phase.value;

        /// <summary>The seed the current (or most recent) raid was built from.</summary>
        public int Seed => _seed.value;

        /// <summary>The era the current raid is set in.</summary>
        public HistoricalEra Era { get; private set; } = HistoricalEra.BronzeAge;

        /// <summary>The layout the current raid is being played in, or null in the Lair.</summary>
        public ProceduralCastleData Castle { get; private set; }

        /// <summary>Worth banked by the most recent extraction.</summary>
        public float LastWorthExtracted { get; private set; }

        /// <summary>Players saved by the most recent extraction.</summary>
        public int LastPlayersSaved { get; private set; }

        /// <summary>Raised on every phase change. HUD and scene loading subscribe.</summary>
        public event Action<RaidPhase> PhaseChanged;

        /// <summary>Raised when a raid resolves: (worth extracted, players saved).</summary>
        public event Action<float, int> RaidResolved;

        private bool _subscribedToZone;

        protected override void OnSpawned()
        {
            base.OnSpawned();
            _phase.onChanged += OnPhaseReplicated;
            SubscribeToZone();
        }

        protected override void OnDespawned()
        {
            base.OnDespawned();
            _phase.onChanged -= OnPhaseReplicated;
            UnsubscribeFromZone();
        }

        private void Awake() => SubscribeToZone();

        private void OnDestroy() => UnsubscribeFromZone();

        // -----------------------------------------------------------------------------------------
        // Starting a raid
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Begins a raid in the chosen era. Rolls (or reuses) the seed, builds the castle, plans and
        /// spawns the haul, resets the alarm and starts the clock.
        ///
        /// Safe to call from a UI button on the host; on a client it does nothing, because the raid a
        /// client plays is the one the server started.
        /// </summary>
        public void StartRaid(HistoricalEra era)
        {
            if (isSpawned && !isServer)
                return;
            if (_phase.value != RaidPhase.InLair && _phase.value != RaidPhase.Resolved)
            {
                Debug.LogWarning($"[Raid] StartRaid ignored: already in phase {_phase.value}.");
                return;
            }

            Era = era;
            _lair?.SelectEra(era);

            // Debt grows every time you set out, which is what puts a clock on the whole campaign.
            _lair?.OnNewSession();

            SetPhase(RaidPhase.Generating);

            // The zone carries the last raid's result until it is re-armed.
            _extractionZone?.ResetForNewRaid();

            _seed.value = _fixedSeed != 0 ? _fixedSeed : NewSeed();
            BuildCastle(_seed.value);

            _alarm?.SetAlarmLevel(0f);

            SetPhase(RaidPhase.Raiding);
        }

        /// <summary>Starts a raid in whichever era the Lair currently has selected.</summary>
        public void StartRaid() => StartRaid(_lair != null ? _lair.GetLairState().SelectedEra : Era);

        /// <summary>
        /// Builds the castle and its haul from a seed. Separate from <see cref="StartRaid"/> so a
        /// client can rebuild from a replicated seed, and so tests can build without a full loop.
        /// </summary>
        public ProceduralCastleData BuildCastle(int seed)
        {
            if (_generator == null)
            {
                Debug.LogWarning("[Raid] No castle generator assigned; raid will have no castle.");
                return null;
            }

            Castle = GenerateWalkable(ref seed);
            _seed.value = seed;

            // Before the NavMesh bake and the spawners, so a guard or a loot pile is never dropped
            // on top of a player who is about to be moved there.
            PlacePlayerAtSpawn();

            if (_castleNetwork != null && (!isSpawned || isServer))
                _castleNetwork.SetSeed(seed);

            // The rooms only exist now, so the walkable surface has to be built between generating
            // them and posting the garrison — a guard spawned before the bake lands off-mesh and
            // stands still for the whole raid.
            _navigation?.Rebuild();

            // Only the server populates the world; clients receive the loot objects as spawned network
            // objects rather than instantiating their own copies.
            if (!isSpawned || isServer)
            {
                _lootSpawner?.SpawnFor(Castle, seed);
                _guardSpawner?.SpawnFor(Castle, seed);
            }

            return Castle;
        }

        // -----------------------------------------------------------------------------------------
        // Ending a raid
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Calls the extraction early — the "leave now with what we have" button. Anything not inside
        /// the zone is left behind, which is the decision the whole raid builds toward.
        /// </summary>
        public void CallExtraction()
        {
            if (isSpawned && !isServer)
                return;
            if (_phase.value != RaidPhase.Raiding)
                return;

            SetPhase(RaidPhase.Extracting);

            if (_extractionZone == null)
            {
                ApplyResult(0f, 0);
                return;
            }

            // Offline the [ServerRpc] wrapper would send nothing and run nothing, so go straight to
            // the resolver; spawned, the RPC is the right door because a client may be asking.
            if (isSpawned)
                _extractionZone.TriggerExtraction();
            else
                _extractionZone.ResolveExtraction();
        }

        /// <summary>
        /// Applies an extraction result: bank the worth against the debt, record the summary and
        /// return to the Lair. Public and network-free so the economics are testable on their own.
        /// </summary>
        public void ApplyResult(float worthExtracted, int playersSaved)
        {
            LastWorthExtracted = worthExtracted;
            LastPlayersSaved = playersSaved;

            _lair?.ApplyExtractionResult(worthExtracted);
            _lootSpawner?.Clear();
            _guardSpawner?.Clear();

            // Deliberately NOT clearing CastleGuard.Intruders: IntruderTag owns that list by
            // component lifetime, and wiping it here would leave every surviving player invisible
            // to guards for the rest of the session.

            SetPhase(RaidPhase.Resolved);
            RaidResolved?.Invoke(worthExtracted, playersSaved);
        }

        /// <summary>
        /// Ends the raid with nothing banked — everyone went down. The castle and its guards are
        /// cleared the same way a successful extraction clears them.
        /// </summary>
        public void AbandonRaid()
        {
            if (isSpawned && !isServer)
                return;
            if (_phase.value != RaidPhase.Raiding && _phase.value != RaidPhase.Extracting)
                return;
            _extractionZone?.CancelPlayerExtraction();
            ApplyResult(0f, 0);
        }

        /// <summary>Returns to the Lair, clearing the raid's castle. Call after the summary is dismissed.</summary>
        public void ReturnToLair()
        {
            _generator?.ClearGenerated();
            Castle = null;
            SetPhase(RaidPhase.InLair);
        }

        // -----------------------------------------------------------------------------------------
        // Plumbing
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Stands the player just inside the gatehouse of the castle just built. Derived here, from
        /// the seed this raid actually rolled, rather than baked into the scene — a baked spawn is
        /// only in the right place for the one seed it was baked from.
        /// </summary>
        private void PlacePlayerAtSpawn()
        {
            if (_playerRoot == null || Castle == null)
                return;

            // The rooms were instantiated a moment ago; without this their colliders are still at
            // their old transforms and every overlap probe reports clear.
            Physics.SyncTransforms();

            _playerRoot.position = CastleSpawnResolver.ResolveSpawn(Castle);
        }

        private void OnExtractionResolved(float worth, int saved)
        {
            if (isSpawned && !isServer)
            {
                // A client only mirrors the summary; the server owns the economy.
                LastWorthExtracted = worth;
                LastPlayersSaved = saved;
                RaidResolved?.Invoke(worth, saved);
                return;
            }

            ApplyResult(worth, saved);
        }

        private void SubscribeToZone()
        {
            if (_subscribedToZone || _extractionZone == null)
                return;
            _extractionZone.ExtractionResolved += OnExtractionResolved;
            _subscribedToZone = true;
        }

        private void UnsubscribeFromZone()
        {
            if (!_subscribedToZone || _extractionZone == null)
                return;
            _extractionZone.ExtractionResolved -= OnExtractionResolved;
            _subscribedToZone = false;
        }

        private void SetPhase(RaidPhase phase)
        {
            if (_phase.value == phase)
                return;
            _phase.value = phase;
            PhaseChanged?.Invoke(phase);
        }

        /// <summary>Mirrors a server-driven phase change onto a client's local event.</summary>
        private void OnPhaseReplicated(RaidPhase phase)
        {
            if (isServer)
                return; // the server already raised it in SetPhase
            PhaseChanged?.Invoke(phase);
        }

        /// <summary>
        /// Generates a castle the raid can actually be completed in: one where a path exists from the
        /// crypt to the extraction exit. A layout without that path is unplayable, so a failing seed
        /// is walked forward deterministically rather than shipped to the players.
        ///
        /// The walk is <c>seed + 1</c>, not a fresh random number, so the retry is reproducible: the
        /// seed the director ends up with is the seed it reports and replicates.
        /// </summary>
        private ProceduralCastleData GenerateWalkable(ref int seed)
        {
            const int maxAttempts = 16;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                ProceduralCastleData data = _generator.Generate(seed);
                if (CastlePathValidator.ValidatePath(data, out _))
                    return data;

                Debug.LogWarning($"[Raid] Seed {seed} produced no crypt-to-exit path; trying {seed + 1}.");
                seed = unchecked(seed + 1);
            }

            Debug.LogError($"[Raid] No walkable castle after {maxAttempts} seeds; " +
                           "raiding the last layout anyway.");
            return _generator.LastGenerated;
        }

        /// <summary>A fresh, non-zero seed. Zero is reserved to mean "roll one".</summary>
        private static int NewSeed()
        {
            int seed = Environment.TickCount ^ (int)(Time.realtimeSinceStartup * 1000f);
            return seed == 0 ? 1 : seed;
        }

        // -----------------------------------------------------------------------------------------
        // Test / tooling seams
        // -----------------------------------------------------------------------------------------

        /// <summary>Wires the director up from code, for tests and for scenes built by tooling.</summary>
        public void Configure(ProceduralCastleGenerator generator, LootSpawner spawner,
            ExtractionZone zone, LairHubManager lair, AlarmFSMManager alarm = null,
            CastleNetworkManager castleNetwork = null, GuardSpawner guardSpawner = null,
            CastleNavMeshBaker navigation = null, Transform playerRoot = null)
        {
            UnsubscribeFromZone();

            _generator = generator;
            _lootSpawner = spawner;
            _extractionZone = zone;
            _lair = lair;
            _alarm = alarm;
            _castleNetwork = castleNetwork;
            _guardSpawner = guardSpawner;
            _navigation = navigation;
            _playerRoot = playerRoot;

            SubscribeToZone();
        }

        /// <summary>Pins the seed so a raid is reproducible. Zero restores per-raid rolling.</summary>
        public void SetFixedSeed(int seed) => _fixedSeed = seed;
    }
}
