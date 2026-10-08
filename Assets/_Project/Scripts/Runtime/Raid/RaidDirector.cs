using System;
using Code.Scripts.EventSystems;
using Interfaces;
using PurrNet;
using Plunderspell.Alarm;
using Plunderspell.Castle;
using Plunderspell.Extraction;
using Plunderspell.Guards;
using Plunderspell.Inventory;
using Plunderspell.Lair;
using UnityEngine;

namespace Plunderspell.Raid
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

        [Tooltip("Spawns the doors where castle zones meet (#248). Optional: without it the castle has no doors.")]
        [SerializeField] private CastleDoorSpawner _doorSpawner;

        /// <summary>The door spawner, for scenes built by tooling.</summary>
        public CastleDoorSpawner DoorSpawner { get => _doorSpawner; set => _doorSpawner = value; }

        [Tooltip("The zone that ends the raid.")]
        [SerializeField] private ExtractionZone _extractionZone;

        [Tooltip("Between-raids meta-progression: debt, banked gold, era.")]
        [SerializeField] private LairHubManager _lair;

        [Tooltip("The castle's alert level. Reset at the start of every raid.")]
        [SerializeField] private EnemyDirector _alarm;

        [Tooltip("The player to stand just inside the gatehouse when the castle is built. The seed " +
                 "is rolled per raid, so a spawn baked into the scene is only right for one of them.")]
        [SerializeField] private Transform _playerRoot;

        [Tooltip("Per-era rooms, loot and garrison. Empty: every era raids with the scene defaults.")]
        [SerializeField] private EraContentCatalogue _eraContent;

        /// <summary>Every era's content, so a saved piece of any era can be found again.</summary>
        public EraContentCatalogue EraContent => _eraContent;

        [Header("Raid setup")]
        [Tooltip("Seed for the next raid. Left at 0, a fresh one is rolled per raid.")]
        [SerializeField] private int _fixedSeed;

        // Replicated so a late-joining client knows what is going on without asking.
        private readonly SyncVar<RaidPhase> _phase = new SyncVar<RaidPhase>(RaidPhase.InLair);

        // The seed and the era together, because every peer builds its own castle geometry from both
        // (the era picks the rooms). As two SyncVars a client could get the new seed before the new
        // era, build the castle in the last raid's era, then build it again when the era arrived.
        private readonly SyncVar<long> _layout = new SyncVar<long>(PackLayout(0, HistoricalEra.BronzeAge));

        private static long PackLayout(int seed, HistoricalEra era) => ((long)seed << 8) | (byte)era;

        // The host's campaign, so a friend's Lair shows the debt they are paying off together.
        private readonly SyncVar<float> _hostDebt = new SyncVar<float>(0f);
        private readonly SyncVar<float> _hostGold = new SyncVar<float>(0f);
        private readonly SyncVar<float> _hostLastRaidWorth = new SyncVar<float>(-1f);

        // The host's purses, paid-last, seats present and Collector line as one string (LairHubManager.HostLedger).
        private readonly SyncVar<string> _hostLedger = new SyncVar<string>(string.Empty);

        // The Age the host's century dial has chosen for the next raid (#358); -1 until the host has published it.
        private readonly SyncVar<int> _hostEra = new SyncVar<int>(-1);

        /// <summary>Where the session is in the loop.</summary>
        public RaidPhase Phase => _phase.value;

        /// <summary>The seed the current (or most recent) raid was built from.</summary>
        public int Seed => (int)(_layout.value >> 8);

        /// <summary>The era the current raid is set in. Replicated, so a client reads the host's.</summary>
        public HistoricalEra Era => (HistoricalEra)(_layout.value & 0xFF);

        /// <summary>The layout the current raid is being played in, or null in the Lair.</summary>
        public ProceduralCastleData Castle { get; private set; }

        /// <summary>Worth banked by the most recent extraction.</summary>
        public float LastWorthExtracted { get; private set; }

        /// <summary>Players saved by the most recent extraction.</summary>
        public int LastPlayersSaved { get; private set; }

        /// <summary>Where the team stepped out of the portal: a capsule centre, as the spawn resolver returns.</summary>
        public Vector3 ArrivalPoint { get; private set; }

        /// <summary>The module the team arrived in, or <see cref="CastleArrivalPlanner.NoArrival"/>.</summary>
        public int ArrivalModuleIndex { get; private set; } = CastleArrivalPlanner.NoArrival;

        /// <summary>Living players who were outside the portal when the most recent raid ended.</summary>
        public int LastPlayersLeftBehind { get; private set; }

        /// <summary>
        /// How far from the portal's centre each player stands on arrival. Outside the 4 m portal, so
        /// arriving does not start the leaving countdown.
        /// </summary>
        public const float PlayerRingRadius = 3.5f;

        private bool _subscribedToZone;

        protected override void OnSpawned()
        {
            base.OnSpawned();
            _phase.onChanged += OnPhaseReplicated;
            _layout.onChanged += OnLayoutReplicated;
            _hostDebt.onChanged += OnHostCampaignReplicated;
            _hostGold.onChanged += OnHostCampaignReplicated;
            _hostLastRaidWorth.onChanged += OnHostCampaignReplicated;
            _hostLedger.onChanged += OnHostLedgerReplicated;
            _hostEra.onChanged += OnHostEraReplicated;
            if (isServer)
            {
                // A pouch banked or a wizard arriving changes the ledger between raids; the host publishes it as it happens.
                EventManager.Instance?.Subscribe(this, (PurseChanged e) => PublishCampaign());
                EventManager.Instance?.Subscribe(this, (PresentChanged e) => PublishCampaign());
                EventManager.Instance?.Subscribe(this, (AgeChosen e) => PublishCampaign()); // the century dial or the Lair screen
                PublishCampaign();
            }
            else
            {
                OnHostEraReplicated(0);
                OnHostCampaignReplicated(0f);
                OnHostLedgerReplicated(string.Empty);
            }
            SubscribeToZone();
            Debug.Log($"[Raid] Director spawned as {(isServer ? "server" : "client")}: phase {_phase.value}, seed {Seed}.");
        }

        protected override void OnDespawned()
        {
            base.OnDespawned();
            _phase.onChanged -= OnPhaseReplicated;
            _layout.onChanged -= OnLayoutReplicated;
            _hostDebt.onChanged -= OnHostCampaignReplicated;
            _hostGold.onChanged -= OnHostCampaignReplicated;
            _hostLastRaidWorth.onChanged -= OnHostCampaignReplicated;
            _hostLedger.onChanged -= OnHostLedgerReplicated;
            if (isServer)
            {
                EventManager.Instance?.Unsubscribe<PurseChanged>(this);
                EventManager.Instance?.Unsubscribe<PresentChanged>(this);
                EventManager.Instance?.Unsubscribe<AgeChosen>(this);
            }
            _hostEra.onChanged -= OnHostEraReplicated;

            // Leaving a friend's session: back to this machine's own saved campaign.
            if (!isServer)
                _lair?.Load();
            UnsubscribeFromZone();
        }

        private GameObject _fallbackListener;

        private void Awake()
        {
            SubscribeToZone();
            _fallbackListener = new GameObject("RaidFallbackListener", typeof(RaidListenerFallback));
        }

        /// <summary>
        /// A client's fallback for building the castle: the phase and seed change events can arrive
        /// in either order, so this checks the pair once per frame until the castle exists.
        /// </summary>
        private void Update()
        {
            if (isSpawned && !isServer && _phase.value == RaidPhase.Raiding && NeedsClientBuild(Seed))
                BuildCastle(Seed);

            // The Lair's ledger shows tonight's shares, so the host keeps the seats present up to date between raids.
            if ((!isSpawned || isServer) && _phase.value == RaidPhase.InLair && Time.unscaledTime >= _nextSeatCheck)
            {
                _nextSeatCheck = Time.unscaledTime + 0.5f;
                _lair?.SetPresent(SeatsPresent());
            }
        }

        private float _nextSeatCheck;

        /// <summary>The seats with a wizard: owner 1 is seat 0 and so on; a lone offline player is seat 0.</summary>
        private static bool[] SeatsPresent()
        {
            var present = new bool[LairHubManager.Seats];
            foreach (StateMachine.PlayerStateMachine player in FindObjectsByType<StateMachine.PlayerStateMachine>(FindObjectsSortMode.None))
            {
                int seat = player.TryGetComponent(out NetworkIdentity identity) && identity.owner.HasValue
                    ? Mathf.Max(0, (int)(ulong)identity.owner.Value.id - 1)
                    : 0;
                present[Mathf.Min(seat, LairHubManager.Seats - 1)] = true;
            }
            if (!System.Array.Exists(present, seated => seated))
                present[0] = true; // solo with the body not found yet: seat 1 alone
            return present;
        }

        private void OnDestroy()
        {
            UnsubscribeFromZone();
            if (_fallbackListener != null)
                Destroy(_fallbackListener);
        }

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

            _layout.value = PackLayout(Seed, era);
            _lair?.SelectEra(era);

            // The Collector calls first, on the debt as it stands (equal shares, #313); then the debt grows.
            if (_lair != null)
            {
                _lair.SetPresent(SeatsPresent());
                _lair.Collect();
            }

            // Debt grows every time you set out, which is what puts a clock on the whole campaign.
            _lair?.OnNewSession();
            PublishCampaign();

            SetPhase(RaidPhase.Generating);

            // The zone carries the last raid's result until it is re-armed.
            _extractionZone?.ResetForNewRaid();

            _layout.value = PackLayout(_fixedSeed != 0 ? _fixedSeed : NewSeed(), era);
            BuildCastle(Seed);

            _alarm?.ResetForNewRaid(GuardArrivalGrace.Seconds);
            GuardArrivalGrace.Begin();

            SetPhase(RaidPhase.Raiding);
        }

        /// <summary>Starts a raid in whichever era the Lair currently has selected.</summary>
        public void StartRaid() => StartRaid(_lair != null ? _lair.GetLairState().SelectedEra : Era);

        // Decision 2026-10-02: a locked door costs the guards a detour and a barred door stops them, so the
        // lockdown has to tell the graph about every door it changes (#207).
        private static void WireLockdownToNavigation(CastleNavGraph graph)
        {
            CastleLockdown lockdown = FindFirstObjectByType<CastleLockdown>();
            if (lockdown == null)
                Debug.LogWarning("[Raid] No CastleLockdown in the scene; guards will walk through barred doors.");
            else
                lockdown.NavGraph = graph;
        }

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

            ApplyEraContent(Era);

            Debug.Log($"[Raid] Building the castle from seed {seed}, {Era} ({(isSpawned && !isServer ? "client" : "host")}).");

            // Published before generating, so anything below the raid in the dependency graph (the
            // castle generator's era rooms, when they land) reads the same Age the garrison is drawn for.
            RaidContext.Publish(new RaidContext(seed, Era));
            Castle = GenerateWalkable(ref seed);
            if ((!isSpawned || isServer) && _alarm != null && Castle != null && Castle.NavGraph != null)
            {
                _alarm.Navigation.SetMap(new CastleGuardNavigationMap(Castle.NavGraph)); // what the fresh guard walks on (#206)
                WireLockdownToNavigation(Castle.NavGraph);
            }
            if (RaidContext.Current.Seed != seed)
                RaidContext.Publish(new RaidContext(seed, Era));
            if (!isSpawned || isServer)
                _layout.value = PackLayout(seed, Era);
            else
            {
                _clientBuiltSeed = seed;
                _clientBuiltEra = Era;
            }

            // The rooms were instantiated a moment ago; without this their colliders are still at
            // their old transforms and every overlap probe reports clear.
            Physics.SyncTransforms();

            // Every peer derives the same arrival from the seed, so the portal and the team land in
            // the same place on every screen without it being sent.
            ArrivalPoint = CastleSpawnResolver.ResolveArrival(Castle, seed, out int arrivalModule);
            ArrivalModuleIndex = arrivalModule;
            OpenPortal();
            SealCastle();

            // Before the spawners, so a guard or a loot pile is never dropped
            // on top of a player who is about to be moved there.
            PlacePlayerAtSpawn();

            if (_castleNetwork != null && (!isSpawned || isServer))
                _castleNetwork.SetSeed(seed);

            // Only the server populates the world; clients receive the loot objects as spawned network
            // objects rather than instantiating their own copies.
            if (!isSpawned || isServer)
            {
                _lootSpawner?.SpawnFor(Castle, seed, _generator != null ? _generator.Registry : null);
                _guardSpawner?.SpawnFor(Castle, seed, Era, ArrivalModuleIndex, LobbySize);
                _doorSpawner?.SpawnFor(Castle);
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
        /// Applies an extraction result: record what was carried home (nothing is banked; coins come
        /// from selling in the Market) and return to the Lair. Public and network-free so the economics are testable on their own.
        /// </summary>
        public void ApplyResult(float worthExtracted, int playersSaved)
        {
            LastWorthExtracted = worthExtracted;
            LastPlayersSaved = playersSaved;

            _lair?.ApplyExtractionResult(worthExtracted);
            PublishCampaign();
            _lootSpawner?.Clear();
            _guardSpawner?.Clear();
            _doorSpawner?.Clear();

            // The raid is over, so its alarm is: otherwise the Roused or Hue and Cry grade stays on the Lair (#352).
            _alarm?.ResetForNewRaid(0f);

            // Deliberately NOT clearing the director's Intruders: IntruderTag owns that list by
            // component lifetime, and wiping it here would leave every surviving player invisible
            // to guards for the rest of the session.

            SetPhase(RaidPhase.Resolved);
            EventManager.Instance?.Publish(new RaidResolved(worthExtracted, playersSaved));
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
            RaidContext.Clear();
            if (!isSpawned || isServer)
                SetPhase(RaidPhase.InLair);
        }

        // -----------------------------------------------------------------------------------------
        // Plumbing
        // -----------------------------------------------------------------------------------------

        /// <summary>Points the generator, loot and garrison at the chosen era's content.</summary>
        private void ApplyEraContent(HistoricalEra era)
        {
            // The scene's own assignments are the fallback, captured once so that raiding a
            // catalogued era and then an uncatalogued one does not keep the first era's content.
            if (!_capturedDefaults)
            {
                _defaultRooms = _generator != null ? _generator.Registry : null;
                _defaultLoot = _lootSpawner != null ? _lootSpawner.Table : null;
                _defaultEnemies = _guardSpawner != null ? _guardSpawner.Roster : null;
                _capturedDefaults = true;
            }

            EraContentCatalogue.Entry entry = _eraContent != null ? _eraContent.For(era) : null;
            CastleRoomRegistry rooms = entry?.Rooms != null ? entry.Rooms : _defaultRooms;
            RaidLootTable loot = entry?.Loot != null ? entry.Loot : _defaultLoot;
            EnemyRoster enemies = entry?.Enemies != null ? entry.Enemies : _defaultEnemies;

            if (_generator != null)
                _generator.Registry = rooms;
            if (_lootSpawner != null)
                _lootSpawner.Table = loot;
            if (_guardSpawner != null)
                _guardSpawner.Roster = enemies;

            Debug.Log($"[Raid] {era} content: rooms {(rooms != null ? rooms.name : "none")}, " +
                      $"loot {(loot != null ? loot.name : "none")}, enemies {(enemies != null ? enemies.name : "none")}.");
        }

        private bool _capturedDefaults;
        private CastleRoomRegistry _defaultRooms;
        private RaidLootTable _defaultLoot;
        private EnemyRoster _defaultEnemies;

        /// <summary>
        /// A new raid starts with no fire, sleep, stun or lift left over from the last one: a player
        /// who died burning set out again still alight (#143).
        /// </summary>
        public static void ClearCarriedOverState(GameObject player)
        {
            if (player != null && player.TryGetComponent(out Plunderspell.Status.StatusEffectReceiver status))
                status.ClearAll();
        }

        /// <summary>Players in this raid: everyone connected in a session, else one (#154).</summary>
        private int LobbySize => isSpawned && networkManager != null ? Mathf.Max(1, networkManager.playerCount) : 1;

        /// <summary>What an arriving player turns to: the portal, or on the curtain strip the room
        /// inward of it, whose archway is the way in (#140).</summary>
        private Vector3 FacingTarget()
        {
            if (Castle == null || ArrivalModuleIndex < 0 || ArrivalModuleIndex >= Castle.PlacedModules.Count)
                return ArrivalPoint;
            ProceduralCastleData.PlacedModule arrival = Castle.PlacedModules[ArrivalModuleIndex];
            if (arrival.Zone != CastleZone.CurtainWall)
                return ArrivalPoint;
            Vector2Int inward = CastleEntrancePlanner.InwardCell(arrival.GridPosition);
            foreach (ProceduralCastleData.PlacedModule module in Castle.PlacedModules)
                if (module.GridPosition == inward && module.Level == CastleLevels.Ground)   // the ground room, not the keep above it (#247)
                    return module.Position;
            return ArrivalPoint;
        }

        /// <summary>Where player number <paramref name="index"/> (0-based) stands: their point on the ring round the portal, moved to clear floor.</summary>
        public static Vector3 PlayerSpawn(Vector3 arrivalPoint, int index)
        {
            float angle = index * Mathf.PI * 0.5f;
            var anchor = new Vector3(arrivalPoint.x + Mathf.Cos(angle) * PlayerRingRadius, 0f,
                arrivalPoint.z + Mathf.Sin(angle) * PlayerRingRadius);
            return CastleSpawnResolver.FirstClearStandingPoint(anchor);
        }

        /// <summary>
        /// Stands the player beside the arrival portal of the castle just built. Derived here, from
        /// the seed this raid actually rolled, rather than baked into the scene — a baked spawn is
        /// only in the right place for the one seed it was baked from.
        /// </summary>
        private void PlacePlayerAtSpawn()
        {
            // In a session the player is spawned per connection rather than placed in the scene, so
            // each machine moves the body it controls; its position replicates to everyone else.
            Transform player = _playerRoot != null ? _playerRoot
                : StateMachine.PlayerStateMachine.Local != null ? StateMachine.PlayerStateMachine.Local.transform : null;
            if (player == null || Castle == null)
            {
                Debug.LogWarning($"[Raid] Not placing a player: player {(player == null ? "missing" : "found")}, castle {(Castle == null ? "missing" : "built")}.");
                return;
            }

            ClearCarriedOverState(player.gameObject);

            // Each player stands at their own point on a ring round the portal, by owner number, so
            // two bodies are never placed inside each other (a client places itself as soon as its
            // castle is built, before the host's body has arrived there on its screen) and nobody
            // arrives standing in the exit.
            int index = player.TryGetComponent(out NetworkIdentity identity) && identity.owner.HasValue
                ? Mathf.Max(0, (int)(ulong)identity.owner.Value.id - 1)
                : 0;
            Vector3 spawn = PlayerSpawn(ArrivalPoint, index);

            // Face the portal, so the first thing a player sees is the way home; on the curtain strip,
            // face the way in instead, which the portal otherwise puts off to one side (#140).
            Vector3 toFacing = FacingTarget() - spawn;
            toFacing.y = 0f;
            if (toFacing.sqrMagnitude > 0.01f && player.TryGetComponent(out StateMachine.PlayerStateMachine look))
                look.FaceYaw(Quaternion.LookRotation(toFacing.normalized, Vector3.up).eulerAngles.y);

            // Through the rigidbody as well as the transform: an interpolated body writes its old
            // position back over a transform-only move on the next physics step.
            if (player.TryGetComponent(out Rigidbody body))
            {
                body.position = spawn;
                body.linearVelocity = Vector3.zero;
            }
            player.position = spawn;
            Debug.Log($"[Raid] Placed {player.name} at {player.position}.");
        }

        /// <summary>Stands the extraction zone on the floor under the arrival point: the way in is the way out.</summary>
        private void OpenPortal()
        {
            float feet = ArrivalPoint.y - CastleSpawnResolver.PlayerHeight * 0.5f - CastleSpawnResolver.FloorClearance;
            var floorPoint = new Vector3(ArrivalPoint.x, feet, ArrivalPoint.z);
            if (_extractionZone != null)
                _extractionZone.PlaceAsPortal(floorPoint);
            EventManager.Instance?.Publish(new PortalOpened(floorPoint));
        }

        /// <summary>Puts the invisible boundary round the castle just built.</summary>
        private void SealCastle()
        {
            if (_generator == null)
                return;

            CastleBoundary.EnsureOn(_generator.gameObject)
                .Rebuild(_generator.CurtainWallRadius, k_CellSize);
        }

        /// <summary>The castle kit's cell size. The generator's own field is private; they must match.</summary>
        private const float k_CellSize = 12f;

        /// <summary>Living player bodies in the scene. Only called once, when a raid ends.</summary>
        private static int CountLivingPlayers()
        {
            int living = 0;
            foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (behaviour is IPlayerBody body && body.IsAlive)
                    living++;
            }
            return living;
        }

        private void OnExtractionResolved(float worth, int saved)
        {
            // Every peer counts for itself: the bodies are replicated, the count is not.
            LastPlayersLeftBehind = ExtractionZone.CountLeftBehind(CountLivingPlayers(), saved);
            _lair?.RecordLeftBehind(LastPlayersLeftBehind);

            if (isSpawned && !isServer)
            {
                // A client only mirrors the summary; the server owns the economy.
                LastWorthExtracted = worth;
                LastPlayersSaved = saved;
                EventManager.Instance?.Publish(new RaidResolved(worth, saved));
                return;
            }

            ApplyResult(worth, saved);
        }

        private void SubscribeToZone()
        {
            if (_subscribedToZone || _extractionZone == null)
                return;
            EventManager.Instance?.Subscribe(this, (ExtractionResolved e) => OnExtractionResolved(e.Worth, e.Saved));
            _subscribedToZone = true;
        }

        private void UnsubscribeFromZone()
        {
            if (!_subscribedToZone || _extractionZone == null)
                return;
            EventManager.Instance?.Unsubscribe<ExtractionResolved>(this);
            _subscribedToZone = false;
        }

        private void SetPhase(RaidPhase phase)
        {
            if (_phase.value == phase)
                return;
            _phase.value = phase;
            EventManager.Instance?.Publish(new RaidPhaseChanged(phase));
        }

        /// <summary>
        /// The layout (seed and era) and the phase are separate SyncVars and a client can receive
        /// "raiding" before the layout, so whichever of the two arrives second builds the castle.
        /// </summary>
        /// <summary>
        /// Whether a client's castle is missing or from an older raid. Keyed on the seed, not on the
        /// castle being null: the host can go from a finished raid straight into the next in one
        /// frame, so a client may never see the Lair phase that would have cleared the old one.
        /// </summary>
        private bool NeedsClientBuild(int seed) =>
            seed != 0 && (Castle == null || _clientBuiltSeed != seed || _clientBuiltEra != Era);

        private int _clientBuiltSeed;
        private HistoricalEra _clientBuiltEra;

        private void OnLayoutReplicated(long layout)
        {
            Debug.Log($"[Raid] Host's castle arrived: seed {Seed}, {Era} (phase {_phase.value}).");
            if (!isServer && _phase.value == RaidPhase.Raiding && NeedsClientBuild(Seed))
                BuildCastle(Seed);
        }

        private void PublishCampaign()
        {
            if (!isSpawned || !isServer || _lair == null)
                return;
            _hostDebt.value = _lair.TotalDebt;
            _hostGold.value = _lair.AccumulatedGold;
            _hostLastRaidWorth.value = _lair.LastRaidWorth;
            _hostLedger.value = _lair.HostLedger();
            _hostEra.value = (int)_lair.GetLairState().SelectedEra;
        }

        private void OnHostEraReplicated(int _)
        {
            if (!isServer && _lair != null && _hostEra.value >= 0)
                _lair.ShowHostEra((HistoricalEra)_hostEra.value);
        }

        private void OnHostLedgerReplicated(string _)
        {
            if (!isServer && _lair != null && _hostLedger.value.Length > 0)
                _lair.ShowHostLedger(_hostLedger.value);
        }

        private void OnHostCampaignReplicated(float _)
        {
            if (!isServer && _lair != null)
                _lair.ShowHostCampaign(_hostDebt.value, _hostGold.value, _hostLastRaidWorth.value);
        }

        /// <summary>Mirrors a server-driven phase change onto a client's local event.</summary>
        private void OnPhaseReplicated(RaidPhase phase)
        {
            if (isServer)
                return; // the server already raised it in SetPhase
            Debug.Log($"[Raid] Host moved the raid to {phase} (seed {Seed}).");

            // A client builds the same castle from the replicated seed: the geometry is local on
            // every machine, only the seed crosses the network. Loot and guards arrive as network
            // objects from the server instead.
            if (phase == RaidPhase.Raiding && NeedsClientBuild(Seed))
                BuildCastle(Seed);
            else if (phase == RaidPhase.InLair && Castle != null)
            {
                _generator?.ClearGenerated();
                Castle = null;
            }

            EventManager.Instance?.Publish(new RaidPhaseChanged(phase));
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
            ExtractionZone zone, LairHubManager lair, EnemyDirector alarm = null,
            CastleNetworkManager castleNetwork = null, GuardSpawner guardSpawner = null,
            Transform playerRoot = null,
            EraContentCatalogue eraContent = null)
        {
            UnsubscribeFromZone();

            _generator = generator;
            _lootSpawner = spawner;
            _extractionZone = zone;
            _lair = lair;
            _alarm = alarm;
            _castleNetwork = castleNetwork;
            _guardSpawner = guardSpawner;
            _playerRoot = playerRoot;
            _eraContent = eraContent;
            _capturedDefaults = false;

            SubscribeToZone();
        }

        /// <summary>Pins the seed so a raid is reproducible. Zero restores per-raid rolling.</summary>
        public void SetFixedSeed(int seed) => _fixedSeed = seed;
    }
}
