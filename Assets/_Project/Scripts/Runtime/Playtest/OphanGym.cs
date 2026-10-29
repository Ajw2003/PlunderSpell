using System.Collections;
using Code.Scripts.EventSystems;
using Interfaces;
using Plunderspell.Extraction;
using Plunderspell.Guards;
using Plunderspell.Loot;
using Plunderspell.Raid;
using PurrNet;
using StateMachine;
using UnityEngine;

namespace Plunderspell.Playtest
{
    /// <summary>
    /// The Ophan Gym's rules (#451): a long raid, the Ophan arriving <see cref="ArriveAfterSeconds"/> in, and one artifact
    /// marked by a pillar of light, so each try is a race to reach it (spared) or miss it (hunted). Also the test actions
    /// the keys ask for. Only in OphanGym.unity, on the raid's Network object beside <see cref="OphanOfHours"/>; built by
    /// Tools > Plunderspell > Build Ophan Gym. See docs/4-systems/fear.md, "The Ophan Gym".
    /// </summary>
    public sealed class OphanGym : NetworkBehaviour
    {
        public const float ArriveAfterSeconds = 30f;
        private const float RaidSeconds = 600f;
        private const float RaceMetres = 40f;
        private const float CarryableMass = 12f;
        private static readonly float[] TimeScales = { 1f, 0.5f, 0.25f };

        private OphanOfHours _ophan;
        private int _timeScaleIndex;

        /// <summary>Game time on this machine when this raid started, or a negative number outside a raid.</summary>
        public float RaidStartedAt { get; private set; } = -1f;

        /// <summary>The Ophan's state as this machine knows it: the hunt on the host, what it was told to show elsewhere.</summary>
        public OphanState OphanState =>
            _ophan == null ? OphanState.Absent
            : isServer ? _ophan.State
            : _ophan.Look != null ? _ophan.Look.State : OphanState.Absent;

        /// <summary>True while this machine's own player takes no damage (F8).</summary>
        public bool IsLocalPlayerUntouchable { get; private set; }

        public float TimeScale => TimeScales[_timeScaleIndex];

        private void Awake()
        {
            // The Ophan lives in the Bootstrap scene; the gym is a raid scene loaded on top of it (#542).
            _ophan = FindFirstObjectByType<OphanOfHours>();
            // The gym brings the Ophan itself, on its own clock; the raid's own schedule must stay quiet.
            OphanOfHours.HuntOverride = false;
        }

        private void OnEnable() => EventManager.Instance?.Subscribe(this, (RaidPhaseChanged e) => OnPhase(e.Phase));

        private void OnDisable() => EventManager.Instance?.UnsubscribeFromAllEvents(this);

        private void OnDestroy()
        {
            // These are statics and outlive Play mode when domain reload is off.
            OphanOfHours.HuntOverride = null;
            Damage.Untouchable = null;
            Time.timeScale = 1f;
        }

        private void OnPhase(RaidPhase phase)
        {
            RaidStartedAt = phase == RaidPhase.Raiding ? Time.time : -1f;
            if (!isServer)
                return;
            StopAllCoroutines();
            if (phase == RaidPhase.Raiding)
                StartCoroutine(RunTheRace());
        }

        private IEnumerator RunTheRace()
        {
            FindFirstObjectByType<ExtractionZone>()?.SetRaidDuration(RaidSeconds);
            // Spawned loot reaches the clients a moment after the raid starts; mark it once they have it.
            yield return new WaitForSeconds(1f);
            LootValue artifact = RaceArtifact();
            if (artifact != null && artifact.TryGetComponent(out NetworkIdentity identity))
                MarkArtifact(identity);
            else
                Debug.LogWarning("[OphanGym] found no carryable artifact to mark, and the loot table has none to place.");
            yield return new WaitForSeconds(ArriveAfterSeconds - 1f);
            // F7 may have brought it already; arriving again would restart its hunt mid-test.
            if (OphanState == OphanState.Absent)
                SummonOnServer();
        }

        /// <summary>The castle's carryable artifact nearest <see cref="RaceMetres"/> from the gate, or a new one.</summary>
        private LootValue RaceArtifact()
        {
            var director = FindFirstObjectByType<RaidDirector>();
            var spawner = FindFirstObjectByType<LootSpawner>();
            if (director == null || spawner == null)
                return null;
            Vector3 gate = director.ArrivalPoint;

            LootValue best = null;
            float bestMiss = float.MaxValue;
            foreach (GameObject piece in spawner.Spawned)
            {
                if (piece == null || !piece.TryGetComponent(out LootValue value) || !IsCarryableArtifact(value.Item, piece))
                    continue;
                float miss = Mathf.Abs(Vector3.Distance(gate, piece.transform.position) - RaceMetres);
                if (miss < bestMiss)
                {
                    best = value;
                    bestMiss = miss;
                }
            }
            return best != null ? best : PlaceArtifact(spawner, gate);
        }

        /// <summary>No artifact in this castle: put one from the loot table on the planned spot nearest the race distance.</summary>
        private static LootValue PlaceArtifact(LootSpawner spawner, Vector3 gate)
        {
            RaidLootTable.Entry entry = spawner.Table?.Entries.Find(e => e.Prefab != null && IsCarryableArtifact(e.Item, e.Prefab));
            if (entry == null || spawner.LastPlan.Count == 0)
                return null;
            Vector3 spot = spawner.LastPlan[0].Position;
            foreach (LootPlacement placement in spawner.LastPlan)
            {
                if (Mathf.Abs(Vector3.Distance(gate, placement.Position) - RaceMetres) < Mathf.Abs(Vector3.Distance(gate, spot) - RaceMetres))
                    spot = placement.Position;
            }
            GameObject piece = spawner.SpawnLoose(entry.Item, entry.Prefab, spot);
            return piece != null ? piece.GetComponent<LootValue>() : null;
        }

        private static bool IsCarryableArtifact(LootItem item, GameObject piece) =>
            item != null && item.IsArtifact && piece.TryGetComponent(out Rigidbody body) && body.mass <= CarryableMass;

        [ObserversRpc(bufferLast: true, runLocally: true)]
        private void MarkArtifact(NetworkIdentity piece)
        {
            if (piece != null && !piece.TryGetComponent(out OphanGymBeacon _))
                piece.gameObject.AddComponent<OphanGymBeacon>();
        }

        /// <summary>F7: the Ophan arrives now, whatever it was doing.</summary>
        public void Summon()
        {
            if (isServer)
                SummonOnServer();
            else
                AskToSummon();
        }

        [ServerRpc(requireOwnership: false)]
        private void AskToSummon() => SummonOnServer();

        private void SummonOnServer()
        {
            if (_ophan != null)
                _ophan.ArriveNow();
        }

        /// <summary>F8: this machine's own player takes no damage, or does again. The beams still show.</summary>
        public void ToggleLocalPlayerUntouchable()
        {
            IsLocalPlayerUntouchable = !IsLocalPlayerUntouchable;
            Damage.Untouchable = IsLocalPlayerUntouchable
                ? target => PlayerStateMachine.Local != null && ReferenceEquals(target, PlayerStateMachine.Local)
                : null;
        }

        /// <summary>F9: every guard is removed for every machine.</summary>
        public void RemoveGuards()
        {
            if (isServer)
                RemoveGuardsOnServer();
            else
                AskToRemoveGuards();
        }

        [ServerRpc(requireOwnership: false)]
        private void AskToRemoveGuards() => RemoveGuardsOnServer();

        private static void RemoveGuardsOnServer()
        {
            // A server destroy despawns each one for every machine.
            foreach (Guard guard in FindObjectsByType<Guard>(FindObjectsSortMode.None))
                Destroy(guard.gameObject);
        }

        /// <summary>F10: time on every machine steps to the next of full, half and a quarter speed.</summary>
        public void CycleTimeScale()
        {
            if (isServer)
                CycleTimeScaleOnServer();
            else
                AskToCycleTimeScale();
        }

        [ServerRpc(requireOwnership: false)]
        private void AskToCycleTimeScale() => CycleTimeScaleOnServer();

        private void CycleTimeScaleOnServer() => ShowTimeScale((_timeScaleIndex + 1) % TimeScales.Length);

        [ObserversRpc(bufferLast: true, runLocally: true)]
        private void ShowTimeScale(int index)
        {
            _timeScaleIndex = index;
            Time.timeScale = TimeScales[index];
        }
    }
}
