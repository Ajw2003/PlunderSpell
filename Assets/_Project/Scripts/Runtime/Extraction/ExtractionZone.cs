using System;
using System.Collections.Generic;
using Interfaces;
using Plunderspell.Core;
using PurrNet;
using RogueAi.Loot;
using UnityEngine;

namespace RogueAi.Extraction
{
    /// <summary>
    /// A trigger-collider "portal" that ends a raid. While the raid timer counts down on the server,
    /// loot pickups and players are tracked as they enter/leave the trigger volume. When the timer hits
    /// zero (or extraction is otherwise triggered), the server tallies the worth of all non-broken loot
    /// physically inside the zone, counts the players saved (including downed players carried inside),
    /// and broadcasts the final result to every client.
    ///
    /// PurrNet 1.15 note: replicated state uses field-based <see cref="SyncVar{T}"/> modules; the
    /// server-authoritative trigger is an <c>[ServerRpc]</c> and the result fan-out is an
    /// <c>[ObserversRpc]</c>. The tally logic is exposed via <see cref="ComputeWorth"/> so it can be
    /// unit-tested without a live transport.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ExtractionZone : NetworkBehaviour
    {
        [Header("Raid timing")]
        [Tooltip("Length of a raid in seconds (default 10 minutes).")]
        [SerializeField] private float RaidDurationSeconds = 600f;

        [Header("Leaving")]
        [Tooltip("Seconds of the raid that must pass before standing on the pad starts the extraction " +
                 "countdown, so a player who spawns beside it does not leave by accident.")]
        [SerializeField] private float _minimumRaidSecondsBeforeLeaving = 10f;

        // Replicated state (PurrNet field-based SyncVars; inline-initialised so never null).
        private readonly SyncVar<float> _timeRemaining = new SyncVar<float>(0f);
        private readonly SyncVar<bool> _extractionComplete = new SyncVar<bool>(false);

        // Server-side tracking of everything currently inside the trigger.
        private readonly List<LootValue> _lootInZone = new List<LootValue>();
        // Player bodies (IPlayerBody), or identities registered through TrackPlayer by tests.
        private readonly List<Component> _playersInZone = new List<Component>();

        // What TrackLoot/TrackPlayer registered by hand. The overlap poll never drops these.
        private readonly HashSet<LootValue> _pinnedLoot = new HashSet<LootValue>();
        private readonly HashSet<Component> _pinnedPlayers = new HashSet<Component>();

        private const float k_pollInterval = 0.25f;
        // Grows when full: the pad sits in the gatehouse among dozens of wall and floor colliders,
        // and a capped buffer silently dropped whichever loot came after them.
        private static Collider[] s_overlap = new Collider[256];
        private readonly HashSet<LootValue> _seenLoot = new HashSet<LootValue>();
        private readonly HashSet<Component> _seenPlayers = new HashSet<Component>();
        private float _nextPoll;
        private Collider _volume;

        /// <summary>Seconds left in the raid (replicated).</summary>
        public float TimeRemaining => _timeRemaining.value;

        /// <summary>True once the extraction has been resolved.</summary>
        public bool ExtractionComplete => _extractionComplete.value;

        /// <summary>Raised on every peer when the extraction resolves: (worthExtracted, playersSaved).</summary>
        public event Action<float, int> ExtractionResolved;

        /// <summary>
        /// Raised whenever loot enters or leaves the zone: (worthInZone, pieceCount). The HUD shows a
        /// running total from this, so a player can see the haul grow as they stack it on the pad
        /// rather than only learning what it was worth after the raid has already ended.
        /// </summary>
        public event Action<float, int> HaulInZoneChanged;

        /// <summary>Worth of everything currently standing in the zone.</summary>
        public float WorthInZone => ComputeWorth(_lootInZone);

        /// <summary>How many pieces are currently standing in the zone.</summary>
        public int PiecesInZone => _lootInZone.Count;

        /// <summary>Width and depth of the portal's trigger, in metres.</summary>
        public const float PortalFootprint = 4f;

        /// <summary>Height of the portal's trigger, in metres.</summary>
        public const float PortalHeight = 4f;

        /// <summary>
        /// Moves the zone to where the team arrived, so the way in is the way out
        /// (docs/plans/night-atmosphere.md, section 6). Called on every peer with the same point,
        /// derived from the raid seed, so nothing about it needs replicating.
        /// </summary>
        public void PlaceAsPortal(Vector3 floorPoint)
        {
            transform.position = floorPoint;

            if (TryGetComponent(out BoxCollider box))
            {
                box.size = new Vector3(PortalFootprint, PortalHeight, PortalFootprint);
                box.center = new Vector3(0f, PortalHeight * 0.5f, 0f);
            }

            Transform marker = transform.Find("Marker");
            if (marker != null)
                marker.localScale = new Vector3(PortalFootprint, marker.localScale.y, PortalFootprint);
        }

        /// <summary>Living players who were not in the portal when it closed.</summary>
        public static int CountLeftBehind(int livingPlayers, int saved) => Mathf.Max(0, livingPlayers - saved);

        protected override void OnSpawned()
        {
            base.OnSpawned();
            if (isServer)
                ResetClock();
        }

        /// <summary>
        /// Offline (single-player, or a scene played without starting a host) there is no spawn
        /// event, so the clock would sit at zero and the raid would end the instant it began. The
        /// authority checks below all read "spawned AND not the server" for the same reason: an
        /// unspawned object is its own authority.
        /// </summary>
        private void Awake()
        {
            if (!isSpawned)
                ResetClock();
        }

        private void ResetClock()
        {
            // Awake runs before the network spawns this object, so isSpawned cannot tell a client
            // here; the manager can. A client's clock is the server's, replicated.
            if (NetworkManager.main != null && NetworkManager.main.isClientOnly)
                return;
            _timeRemaining.value = RaidDurationSeconds;
            _extractionComplete.value = false;
        }

        /// <summary>True while someone is standing on the pad and the leaving countdown is running.</summary>
        public bool IsPlayerExtracting => GameServices.Extraction != null && GameServices.Extraction.IsExtracting;

        /// <summary>Seconds left on the leaving countdown, or 0 when none is running.</summary>
        public float PlayerExtractionRemaining =>
            IsPlayerExtracting ? Mathf.Max(0f, GameServices.Extraction.RemainingSeconds) : 0f;

        /// <summary>Living players currently standing in the zone.</summary>
        public int LivingPlayersInZone
        {
            get
            {
                int count = 0;
                foreach (Component c in _playersInZone)
                {
                    if (c != null && (!(c is IPlayerBody body) || body.IsAlive))
                        count++;
                }
                return count;
            }
        }

        /// <summary>
        /// The "stand on the pad to leave" countdown. Runs while a living player is in the zone, stops
        /// the moment the last one steps off, and resolves the extraction when it finishes. Uses the
        /// same <see cref="ExtractionController"/> the HUD's "Extracting…" bar is bound to.
        /// </summary>
        private void TickPlayerExtraction()
        {
            ExtractionController leaving = GameServices.Extraction;
            if (leaving == null)
                return;

            bool raidRunning = RaidDurationSeconds - _timeRemaining.value >= _minimumRaidSecondsBeforeLeaving;
            bool someoneOnThePad = LivingPlayersInZone > 0 && GameServices.IsPlaying;

            if (!someoneOnThePad || !raidRunning)
            {
                leaving.CancelExtraction();
                return;
            }

            if (!leaving.IsExtracting)
                leaving.StartExtraction();

            leaving.Tick(Time.deltaTime);
            if (!leaving.IsExtracting)
                ResolveExtraction(); // the countdown just completed
        }

        /// <summary>Stops a leaving countdown in progress, e.g. because the raid was lost.</summary>
        public void CancelPlayerExtraction() => GameServices.Extraction?.CancelExtraction();

        private void Update()
        {
            if ((isSpawned && !isServer) || _extractionComplete.value)
                return;

            if (Time.time >= _nextPoll)
            {
                _nextPoll = Time.time + k_pollInterval;
                PollContents();
            }

            TickPlayerExtraction();
            if (_extractionComplete.value)
                return;

            _timeRemaining.value -= Time.deltaTime;
            if (_timeRemaining.value <= 0f)
            {
                _timeRemaining.value = 0f;
                ResolveExtraction();
            }
        }

        /// <summary>
        /// Server-authoritative extraction resolution. Tallies non-broken loot worth inside the zone,
        /// counts saved players, marks complete, broadcasts the result and raises the event.
        /// </summary>
        [ServerRpc(requireOwnership: false)]
        public void TriggerExtraction() => ResolveExtraction();

        /// <summary>
        /// The actual resolution, separate from the RPC that carries it.
        ///
        /// PurrNet rewrites an [ServerRpc] method at build time into a send: its body runs on the
        /// server after a round trip, and on an UNSPAWNED object it does not run at all. Offline —
        /// single-player, or a scene played without starting a host — calling the RPC would
        /// therefore silently do nothing, so the clock expiring and <c>RaidDirector</c> both call
        /// this directly and let the RPC be the networked door onto it.
        /// </summary>
        public void ResolveExtraction()
        {
            if (_extractionComplete.value)
                return;

            float totalWorth = ComputeWorth(_lootInZone);
            int playersSaved = LivingPlayersInZone;
            _extractionComplete.value = true;
            CancelPlayerExtraction();

            if (isSpawned && isServer)
                BroadcastExtractionResult(totalWorth, playersSaved);
            else
                ApplyExtractionResult(totalWorth, playersSaved);
        }

        /// <summary>
        /// Pure, network-free tally helper: sum the worth of every non-broken pickup in the list.
        /// Exposed for unit testing (<c>Test_ExtractionTally</c>).
        /// </summary>
        public static float ComputeWorth(IEnumerable<LootValue> loot)
        {
            float total = 0f;
            foreach (LootValue piece in loot)
            {
                if (piece == null || piece.IsRuined)
                    continue;
                total += piece.Worth;
            }
            return total;
        }

        [ObserversRpc(bufferLast: true)]
        private void BroadcastExtractionResult(float worth, int saved) => ApplyExtractionResult(worth, saved);

        /// <summary>Presentation half: mark complete locally and raise the event on this peer.</summary>
        private void ApplyExtractionResult(float worth, int saved)
        {
            _extractionComplete.value = true;
            ExtractionResolved?.Invoke(worth, saved);
        }

        // -----------------------------------------------------------------------------------------
        // Contents tracking (server-authoritative)
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Re-reads what is physically inside the zone. A poll rather than trigger enter/exit:
        /// Unity sends no trigger events between a kinematic body and a static trigger, and resting
        /// loot is kinematic while it settles, so a piece already on the pad when it was released
        /// never "entered" and was left out of the haul.
        /// </summary>
        private void PollContents()
        {
            if (_volume == null)
                _volume = GetComponent<Collider>();
            if (_volume == null || !_volume.enabled)
                return;

            int count = OverlapVolume();
            while (count == s_overlap.Length)
            {
                s_overlap = new Collider[s_overlap.Length * 2];
                count = OverlapVolume();
            }

            _seenLoot.Clear();
            _seenPlayers.Clear();
            for (int i = 0; i < count; i++)
            {
                var piece = s_overlap[i].GetComponentInParent<LootValue>();
                if (piece != null)
                {
                    _seenLoot.Add(piece);
                    continue;
                }

                if (s_overlap[i].GetComponentInParent<IPlayerBody>() is Component body)
                    _seenPlayers.Add(body);
            }

            bool haulChanged = Sync(_lootInZone, _seenLoot, _pinnedLoot);
            Sync(_playersInZone, _seenPlayers, _pinnedPlayers);
            if (haulChanged)
                OnHaulChanged();
        }

        private int OverlapVolume()
        {
            if (_volume is BoxCollider box)
            {
                Transform t = box.transform;
                Vector3 centre = t.TransformPoint(box.center);
                Vector3 halfExtents = Vector3.Scale(box.size, t.lossyScale) * 0.5f;
                return Physics.OverlapBoxNonAlloc(centre, halfExtents, s_overlap, t.rotation,
                    ~0, QueryTriggerInteraction.Ignore);
            }

            Bounds b = _volume.bounds;
            return Physics.OverlapBoxNonAlloc(b.center, b.extents, s_overlap, Quaternion.identity,
                ~0, QueryTriggerInteraction.Ignore);
        }

        /// <summary>Makes <paramref name="live"/> = seen ∪ pinned. Returns whether it changed.</summary>
        private static bool Sync<T>(List<T> live, HashSet<T> seen, HashSet<T> pinned) where T : class
        {
            bool changed = false;
            for (int i = live.Count - 1; i >= 0; i--)
            {
                T item = live[i];
                if (item == null || (!seen.Contains(item) && !pinned.Contains(item)))
                {
                    live.RemoveAt(i);
                    changed = true;
                }
            }

            foreach (T item in seen)
            {
                if (!live.Contains(item))
                {
                    live.Add(item);
                    changed = true;
                }
            }
            return changed;
        }

        private void OnHaulChanged() => HaulInZoneChanged?.Invoke(WorthInZone, _lootInZone.Count);

        // -----------------------------------------------------------------------------------------
        // Test / integration seams (network-free mutation of the tracked lists)
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Sets the raid length. Used by scene tooling and by the director when a raid's duration
        /// depends on the era. Takes effect on the next spawn, or immediately when already running.
        /// </summary>
        public void SetRaidDuration(float seconds)
        {
            RaidDurationSeconds = Mathf.Max(1f, seconds);
            if (!_extractionComplete.value)
                _timeRemaining.value = RaidDurationSeconds;
        }

        /// <summary>
        /// Re-arms the zone for a new raid: clock back to full, extraction un-resolved, and both
        /// tracked lists emptied.
        ///
        /// Without this a second raid is unwinnable — the zone stays flagged complete from the last
        /// one, so <see cref="TriggerExtraction"/> returns immediately and the players can never
        /// leave with anything.
        /// </summary>
        public void ResetForNewRaid()
        {
            _lootInZone.Clear();
            _playersInZone.Clear();
            _pinnedLoot.Clear();
            _pinnedPlayers.Clear();
            ResetClock();
            OnHaulChanged();
        }

        /// <summary>Test seam: register a piece as being inside the zone.</summary>
        public void TrackLoot(LootValue piece)
        {
            if (piece != null && !_lootInZone.Contains(piece))
            {
                _pinnedLoot.Add(piece);
                _lootInZone.Add(piece);
                OnHaulChanged();
            }
        }

        /// <summary>
        /// Transition overload for callers still holding a <see cref="LootPickup"/>. Attaches the
        /// <see cref="LootValue"/> the zone now tallies, carrying the pickup's authored worth across.
        /// Delete alongside <see cref="LootPickup"/>.
        /// </summary>
        public void TrackLoot(LootPickup pickup)
        {
            if (pickup == null)
                return;

            if (!pickup.TryGetComponent(out LootValue piece))
            {
                piece = pickup.gameObject.AddComponent<LootValue>();
                piece.SetItem(pickup.Data);
            }

            if (pickup.IsBroken)
                piece.Ruin();

            TrackLoot(piece);
        }

        /// <summary>Test seam: register a player identity as being inside the zone.</summary>
        public void TrackPlayer(NetworkIdentity identity)
        {
            if (identity != null && !_playersInZone.Contains(identity))
            {
                _pinnedPlayers.Add(identity);
                _playersInZone.Add(identity);
            }
        }

        /// <summary>Test seam: resolve the extraction and report what it paid out.</summary>
        public (float worth, int saved) ResolveLocally()
        {
            float worth = ComputeWorth(_lootInZone);
            int saved = LivingPlayersInZone;
            ResolveExtraction();
            return (worth, saved);
        }

        /// <summary>Count of loot currently tracked inside the zone.</summary>
        public int LootInZoneCount => _lootInZone.Count;

        /// <summary>Count of players currently tracked inside the zone.</summary>
        public int PlayersInZoneCount => _playersInZone.Count;
    }
}
