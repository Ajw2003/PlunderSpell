using System;
using System.Collections.Generic;
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

        // Replicated state (PurrNet field-based SyncVars; inline-initialised so never null).
        private readonly SyncVar<float> _timeRemaining = new SyncVar<float>(0f);
        private readonly SyncVar<bool> _extractionComplete = new SyncVar<bool>(false);

        // Server-side tracking of everything currently inside the trigger.
        private readonly List<LootPickup> _lootInZone = new List<LootPickup>();
        private readonly List<NetworkIdentity> _playersInZone = new List<NetworkIdentity>();

        /// <summary>Seconds left in the raid (replicated).</summary>
        public float TimeRemaining => _timeRemaining.value;

        /// <summary>True once the extraction has been resolved.</summary>
        public bool ExtractionComplete => _extractionComplete.value;

        /// <summary>Raised on every peer when the extraction resolves: (worthExtracted, playersSaved).</summary>
        public event Action<float, int> ExtractionResolved;

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
            _timeRemaining.value = RaidDurationSeconds;
            _extractionComplete.value = false;
        }

        private void Update()
        {
            if ((isSpawned && !isServer) || _extractionComplete.value)
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
            int playersSaved = _playersInZone.Count;
            _extractionComplete.value = true;

            if (isSpawned && isServer)
                BroadcastExtractionResult(totalWorth, playersSaved);
            else
                ApplyExtractionResult(totalWorth, playersSaved);
        }

        /// <summary>
        /// Pure, network-free tally helper: sum the worth of every non-broken pickup in the list.
        /// Exposed for unit testing (<c>Test_ExtractionTally</c>).
        /// </summary>
        public static float ComputeWorth(IEnumerable<LootPickup> loot)
        {
            float total = 0f;
            foreach (var pickup in loot)
            {
                if (pickup == null || pickup.IsBroken || pickup.Data == null)
                    continue;
                total += pickup.Data.Worth;
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
        // Trigger tracking (server-authoritative)
        // -----------------------------------------------------------------------------------------

        private void OnTriggerEnter(Collider other)
        {
            if (isSpawned && !isServer)
                return;

            var pickup = other.GetComponentInParent<LootPickup>();
            if (pickup != null && !_lootInZone.Contains(pickup))
                _lootInZone.Add(pickup);

            var identity = other.GetComponentInParent<NetworkIdentity>();
            if (identity != null && pickup == null && !_playersInZone.Contains(identity))
                _playersInZone.Add(identity);
        }

        private void OnTriggerExit(Collider other)
        {
            if (isSpawned && !isServer)
                return;

            var pickup = other.GetComponentInParent<LootPickup>();
            if (pickup != null)
                _lootInZone.Remove(pickup);

            var identity = other.GetComponentInParent<NetworkIdentity>();
            if (identity != null && pickup == null)
                _playersInZone.Remove(identity);
        }

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
            ResetClock();
        }

        /// <summary>Test seam: register a pickup as being inside the zone.</summary>
        public void TrackLoot(LootPickup pickup)
        {
            if (pickup != null && !_lootInZone.Contains(pickup))
                _lootInZone.Add(pickup);
        }

        /// <summary>Test seam: register a player identity as being inside the zone.</summary>
        public void TrackPlayer(NetworkIdentity identity)
        {
            if (identity != null && !_playersInZone.Contains(identity))
                _playersInZone.Add(identity);
        }

        /// <summary>Test seam: resolve the extraction and report what it paid out.</summary>
        public (float worth, int saved) ResolveLocally()
        {
            float worth = ComputeWorth(_lootInZone);
            int saved = _playersInZone.Count;
            ResolveExtraction();
            return (worth, saved);
        }

        /// <summary>Count of loot currently tracked inside the zone.</summary>
        public int LootInZoneCount => _lootInZone.Count;

        /// <summary>Count of players currently tracked inside the zone.</summary>
        public int PlayersInZoneCount => _playersInZone.Count;
    }
}
