using PurrNet;
using StateMachine;
using UnityEngine;

namespace Plunderspell.Loot
{
    /// <summary>
    /// Turns a downed player (HP ≤ 0) into a carryable "body" that teammates can haul to extraction
    /// for a lair revival — exactly like any other heavy loot item.
    ///
    /// When <see cref="EnterDownedState"/> is called by the <see cref="PlayerStateMachine"/>:
    /// <list type="number">
    /// <item>Ragdoll bones (limb rigidbodies + colliders) are enabled, the <see cref="Animator"/> is
    /// disabled, and the movement rigidbody / <see cref="CharacterController"/> are switched off.</item>
    /// <item>A <see cref="LootPickup"/> is added (or re-used) on the root with a runtime
    /// <see cref="LootItem"/>: WeightKg = 12 stone (forces a dual carry), Worth = 0, Fragility =
    /// float.MaxValue (a body cannot shatter).</item>
    /// </list>
    /// On reaching the extraction point <see cref="RevivePlayer"/> reverses the process and restores
    /// the player to 25% HP.
    /// </summary>
    [RequireComponent(typeof(LootPickup))]
    public class DownedPlayerCarryAdapter : NetworkBehaviour
    {
        [Header("Downed body loot profile")]
        [Tooltip("Weight in kg of a downed body — 12 forces a two-player carry.")]
        [SerializeField] private float _downedBulk = 12f;

        [Tooltip("Fraction of max HP restored on lair revival (0.25 = 25%).")]
        [Range(0f, 1f)]
        [SerializeField] private float _reviveHealthFraction = 0.25f;

        [Header("References (auto-resolved if left empty)")]
        [SerializeField] private PlayerStateMachine _stateMachine;
        [SerializeField] private Animator _animator;
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private Rigidbody _movementRigidbody;

        [Tooltip("Limb rigidbodies that make up the ragdoll. Auto-collected from children if empty.")]
        [SerializeField] private Rigidbody[] _ragdollBones;

        private LootPickup _pickup;
        private LootItem _bodyLootData;
        private bool _isDowned;

        public bool IsDowned => _isDowned;

        private void Awake()
        {
            _pickup = GetComponent<LootPickup>();
            if (_stateMachine == null) _stateMachine = GetComponent<PlayerStateMachine>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            if (_characterController == null) _characterController = GetComponent<CharacterController>();
            if (_movementRigidbody == null) _movementRigidbody = GetComponent<Rigidbody>();

            if (_ragdollBones == null || _ragdollBones.Length == 0)
                _ragdollBones = CollectLimbRigidbodies();
        }

        /// <summary>
        /// Collects limb rigidbodies (every child rigidbody except the root movement body) so the
        /// ragdoll can be toggled without a hand-authored list.
        /// </summary>
        private Rigidbody[] CollectLimbRigidbodies()
        {
            var all = GetComponentsInChildren<Rigidbody>(true);
            var limbs = new System.Collections.Generic.List<Rigidbody>();
            foreach (var rb in all)
            {
                if (rb == _movementRigidbody)
                    continue;
                limbs.Add(rb);
            }
            return limbs.ToArray();
        }

        /// <summary>
        /// Called by <see cref="PlayerStateMachine"/> when the player's HP reaches zero. Activates the
        /// ragdoll and makes the body a dual-carry loot object.
        /// </summary>
        public void EnterDownedState()
        {
            if (_isDowned)
                return;
            _isDowned = true;

            SetRagdollActive(true);

            _bodyLootData = BuildBodyLootData();
            _pickup.SetData(_bodyLootData);
            _pickup.enabled = true;
        }

        private LootItem BuildBodyLootData()
        {
            var data = ScriptableObject.CreateInstance<LootItem>();
            data.WeightKg = _downedBulk;                       // 12 kg → too heavy to lift
            data.Worth = 0f;                                // a body is not treasure
            data.Fragility = float.MaxValue;               // a body cannot shatter
            data.IsArtifact = false;
            data.DisplayName = $"{ResolvePlayerName()} (Downed)";
            return data;
        }

        private string ResolvePlayerName()
        {
            // Prefer the network owner id when spawned; fall back to the GameObject name.
            if (isSpawned && owner.HasValue)
                return $"Player {owner.Value}";
            return gameObject.name;
        }

        /// <summary>Enables/disables ragdoll physics and switches off animation + movement drivers.</summary>
        private void SetRagdollActive(bool active)
        {
            if (_animator != null) _animator.enabled = !active;
            if (_characterController != null) _characterController.enabled = !active;

            if (_movementRigidbody != null)
            {
                // While ragdolled the movement body must not fight the limb physics.
                _movementRigidbody.isKinematic = active;
            }

            if (_ragdollBones != null)
            {
                foreach (var bone in _ragdollBones)
                {
                    if (bone == null)
                        continue;
                    bone.isKinematic = !active;
                    bone.detectCollisions = active;
                }
            }
        }

        /// <summary>
        /// Server-authoritative lair revival, invoked once the body reaches the extraction point.
        /// Disables the ragdoll, re-enables the state machine and restores health to
        /// <see cref="_reviveHealthFraction"/> of max.
        /// </summary>
        [ServerRpc(requireOwnership: false)]
        public void RevivePlayer()
        {
            RevivePlayerObservers();
        }

        [ObserversRpc(bufferLast: true)]
        private void RevivePlayerObservers() => ApplyRevive();

        /// <summary>
        /// Local application of the revival — separated out so it can be unit-tested without a live
        /// network transport.
        /// </summary>
        public void ApplyRevive()
        {
            if (!_isDowned)
                return;
            _isDowned = false;

            SetRagdollActive(false);

            if (_pickup != null)
                _pickup.enabled = false;

            if (_stateMachine != null)
            {
                _stateMachine.enabled = true;
                _stateMachine.ReviveTo(_reviveHealthFraction);
            }

            if (_bodyLootData != null)
            {
                Destroy(_bodyLootData);
                _bodyLootData = null;
            }
        }
    }
}
