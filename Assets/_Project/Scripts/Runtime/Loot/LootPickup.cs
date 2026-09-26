using Interfaces;
using PurrNet;
using UnityEngine;

namespace RogueAi.Loot
{
    /// <summary>Resolved carry requirement for a pickup attempt.</summary>
    public enum CarryMode
    {
        None,
        Single,
        Dual
    }

    /// <summary>
    /// Runtime physics + networking behaviour for a single lootable object. The design-time numbers
    /// live on the <see cref="LootItem"/> ScriptableObject; this component turns them into physical
    /// behaviour:
    /// <list type="bullet">
    /// <item>Fragility: a hard collision above <see cref="LootItem.Fragility"/> shatters the item.</item>
    /// <item>Single carry: <see cref="LootItem.Bulk"/> ≤ 10 stone → one carrier owns and kinematically
    /// parents the object to their hand socket.</item>
    /// <item>Dual carry: Bulk &gt; 10 stone → a primary carrier plus a secondary carrier chained via a
    /// <see cref="ConfigurableJoint"/> (linear axes locked, angular free so it swings naturally).</item>
    /// </list>
    ///
    /// PurrNet 1.15 note: PurrNet has no Mirror-style <c>[SyncVar(hook=...)]</c> attribute — replicated
    /// state uses field-based <see cref="SyncVar{T}"/> modules, ownership transfer uses
    /// <see cref="NetworkIdentity.GiveOwnership(PlayerID, bool)"/>, and state-changing entry points are
    /// <c>[ServerRpc]</c> / effects are <c>[ObserversRpc]</c>. Pure decision helpers
    /// (<see cref="EvaluatePickup"/>, <see cref="WouldBreak"/>) are network-free so they can be unit
    /// tested in EditMode without a live transport.
    /// </summary>
    /// <remarks>
    /// Implements <see cref="IBreakable"/> and <see cref="ILevitatable"/> so spell effects can reach
    /// loot without the Spells assembly referencing Loot (which would cycle back through Player).
    /// Frango shatters loot; Levo lifts it.
    /// </remarks>
    [RequireComponent(typeof(Rigidbody))]
    public class LootPickup : NetworkBehaviour, IBreakable, ILevitatable
    {
        [Header("Data")]
        [SerializeField] private LootItem _data;

        [Header("Broken-state visuals")]
        [Tooltip("Renderer(s) hidden when the item shatters.")]
        [SerializeField] private MeshRenderer _meshRenderer;
        [Tooltip("Particle VFX enabled when the item shatters.")]
        [SerializeField] private ParticleSystem _brokenVfx;

        [Header("Carry")]
        [Tooltip("Local anchor offset used when parenting to a carrier's hand socket.")]
        [SerializeField] private Vector3 _handLocalOffset = Vector3.zero;
        [Tooltip("Where the carrier's hand closes on this item. Empty: the centre of its meshes.")]
        [SerializeField] private Transform _gripPoint;

        // The rotation the item spawned with. It carries the Z-up import correction that stands an
        // ArtForge model upright, so the item is held the same way up rather than tipped on its side.
        private Quaternion _uprightRotation = Quaternion.identity;

        private Rigidbody _rb;
        private ConfigurableJoint _carryJoint;

        // Replicated state (PurrNet field-based SyncVars; inline-initialised so they are never null).
        private readonly SyncVar<bool> _isBroken = new SyncVar<bool>(false);
        private readonly SyncVar<bool> _isBeingCarried = new SyncVar<bool>(false);

        /// <summary>Network identity of the primary carrier (null when not carried).</summary>
        public NetworkIdentity PrimaryCarrierNetId { get; private set; }

        /// <summary>Network identity of the secondary carrier (dual carry only).</summary>
        public NetworkIdentity SecondaryCarrierNetId { get; private set; }

        public bool IsBroken => _isBroken.value;
        public bool IsBeingCarried => _isBeingCarried.value;
        public LootItem Data => _data;

        /// <summary>Last carry mode resolved by <see cref="RequestPickup"/> — surfaced for tests/UI.</summary>
        public CarryMode CurrentCarryMode { get; private set; } = CarryMode.None;

        private void Awake()
        {
            CaptureUprightRotation();
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = true;
            if (_meshRenderer == null)
                _meshRenderer = GetComponentInChildren<MeshRenderer>();
        }

        /// <summary>
        /// Injects a data source at runtime. Used by <c>DownedPlayerCarryAdapter</c> to turn a downed
        /// player into a carryable object, and by tests to drive fragility/bulk logic.
        /// </summary>
        public void SetData(LootItem data) => _data = data;

        /// <summary>The point the hand holds, or null when the item is held by its mesh centre.</summary>
        public Transform GripPoint => _gripPoint;

        /// <summary>
        /// Records the current rotation as the way up the item is carried. Runs in Awake, which the
        /// spawner reaches with the prefab's own rotation; exposed for EditMode tests, where Awake
        /// does not run.
        /// </summary>
        public void CaptureUprightRotation() => _uprightRotation = transform.rotation;

        /// <summary>
        /// Parents the item under <paramref name="socket"/>, upright, with its grip point (or mesh
        /// centre) on the socket plus the hand offset.
        /// </summary>
        public void AttachToSocket(Transform socket)
        {
            transform.SetParent(socket, false);
            transform.localRotation = _uprightRotation;
            transform.localPosition = Vector3.zero;
            Vector3 gripInSocket = socket.InverseTransformPoint(GripWorldPosition());
            transform.localPosition = _handLocalOffset - gripInSocket;
        }

        private Vector3 GripWorldPosition()
        {
            if (_gripPoint != null)
                return _gripPoint.position;

            bool any = false;
            var bounds = new Bounds(transform.position, Vector3.zero);
            foreach (Renderer meshRenderer in GetComponentsInChildren<Renderer>())
            {
                if (meshRenderer is ParticleSystemRenderer)
                    continue;
                if (!any)
                    bounds = meshRenderer.bounds;
                else
                    bounds.Encapsulate(meshRenderer.bounds);
                any = true;
            }
            return bounds.center;
        }

        // ---------------------------------------------------------------------------------------
        // Fragility
        // ---------------------------------------------------------------------------------------

        private void OnCollisionEnter(Collision col)
        {
            // A carried item is protected — only free-falling / thrown loot can shatter.
            if (IsBeingCarried || IsBroken || _data == null)
                return;

            // Only the machine simulating this body judges its impacts. Elsewhere the body is moved
            // by the replicated transform, and those teleports read as violent collisions.
            if (isSpawned && TryGetComponent(out NetworkTransform synced) && !synced.IsController(synced.ownerAuth))
                return;

            ApplyImpact(col.relativeVelocity.magnitude);
        }

        /// <summary>Pure fragility test — does an impact of this magnitude break the item?</summary>
        public bool WouldBreak(float relativeVelocityMagnitude)
        {
            return _data != null &&
                   !IsBeingCarried &&
                   !IsBroken &&
                   relativeVelocityMagnitude > _data.Fragility;
        }

        /// <summary>
        /// Evaluates an impact and shatters the item if it exceeds the fragility threshold.
        /// Safe to call from tests without a live network — the break is applied locally, and when
        /// spawned on the server it is additionally replicated to all observers.
        /// </summary>
        public void ApplyImpact(float relativeVelocityMagnitude)
        {
            if (WouldBreak(relativeVelocityMagnitude))
                BreakItem();
        }

        /// <summary>
        /// Shatters the item: hides the mesh, plays the broken VFX and disables physics. When running
        /// on a spawned server object the effect is fanned out to every observer via
        /// <see cref="BreakItemObservers"/>; otherwise (single-player / tests) it is applied locally.
        /// </summary>
        public void BreakItem()
        {
            // A client carrying the piece simulates it, so it judges the impact and asks the server
            // to break it; the server is the only one that marks it broken.
            if (isSpawned && !isServer)
            {
                if (isOwner)
                    RequestBreak();
                return;
            }
            if (isSpawned && isServer)
                BreakItemObservers();
            else
                ApplyBrokenState();
        }

        [ObserversRpc(bufferLast: true)]
        private void BreakItemObservers() => ApplyBrokenState();

        [ServerRpc(requireOwnership: true)]
        private void RequestBreak() => BreakItem();

        /// <summary>
        /// A player wants to pick this up: hand them the body, so their machine simulates it while it
        /// is carried and their movement of it replicates. Ownership then stays with them until
        /// someone else picks it up.
        /// </summary>
        [ServerRpc(requireOwnership: false)]
        public void RequestCarry(RPCInfo info = default)
        {
            if (!IsBroken)
                GiveOwnership(info.sender);
        }

        private void ApplyBrokenState()
        {
            if (!isSpawned || isServer)
                _isBroken.value = true;

            // Transition bridge: extraction tallies LootValue now, so breaking through the old
            // system has to reach the new one or a smashed piece still pays out. Goes away with
            // LootPickup. See docs/systems/raid.md, "Carrying and extracting".
            if (TryGetComponent(out LootValue value))
                value.Ruin();
            if (_meshRenderer != null) _meshRenderer.enabled = false;
            if (_brokenVfx != null) _brokenVfx.Play();
            if (_rb != null)
            {
                _rb.isKinematic = true;
                _rb.detectCollisions = false;
            }
        }

        // ---------------------------------------------------------------------------------------
        // Carry resolution
        // ---------------------------------------------------------------------------------------

        /// <summary>Pure decision: how many carriers does this item require? Network-free (testable).</summary>
        public CarryMode EvaluatePickup()
        {
            if (_data == null)
                return CarryMode.None;
            return _data.RequiresDualCarry ? CarryMode.Dual : CarryMode.Single;
        }

        /// <summary>
        /// Primary pickup request. Bulk ≤ 10 → single carry; Bulk &gt; 10 → begins a dual carry that
        /// waits for a second carrier. Server-authoritative so ownership transfer cannot race.
        /// </summary>
        [ServerRpc(requireOwnership: false)]
        public void RequestPickup(NetworkIdentity picker) => PerformPickup(picker);

        /// <summary>
        /// The pickup itself, separate from the RPC that carries it. PurrNet rewrites an [ServerRpc]
        /// into a send, and on an UNSPAWNED object it runs nothing at all — so offline callers use
        /// this directly rather than silently failing to pick anything up.
        /// </summary>
        public void PerformPickup(NetworkIdentity picker)
        {
            if (IsBroken || picker == null)
                return;

            CurrentCarryMode = EvaluatePickup();
            if (CurrentCarryMode == CarryMode.Dual)
                InitiatePrimaryForDualCarry(picker);
            else
                SingleCarry(picker);
        }

        /// <summary>Single-carrier path: give ownership to the carrier and parent to their hand socket.</summary>
        private void SingleCarry(NetworkIdentity carrier)
        {
            _isBeingCarried.value = true;
            PrimaryCarrierNetId = carrier;
            CurrentCarryMode = CarryMode.Single;

            if (carrier.owner.HasValue)
                GiveOwnership(carrier.owner.Value);

            _rb.isKinematic = true;
            ParentToHandSocket(carrier);
        }

        /// <summary>Dual carry — primary side: latch the first carrier and await the secondary.</summary>
        private void InitiatePrimaryForDualCarry(NetworkIdentity primary)
        {
            _isBeingCarried.value = true;
            PrimaryCarrierNetId = primary;
            CurrentCarryMode = CarryMode.Dual;

            if (primary.owner.HasValue)
                GiveOwnership(primary.owner.Value);

            _rb.isKinematic = true;
            ParentToHandSocket(primary);
        }

        /// <summary>
        /// Secondary pickup request for a heavy (dual-carry) item. Chains the secondary carrier to the
        /// primary carrier's hand socket via a <see cref="ConfigurableJoint"/>.
        /// </summary>
        [ServerRpc(requireOwnership: false)]
        public void RequestSecondaryPickup(NetworkIdentity secondaryPicker) =>
            PerformSecondaryPickup(secondaryPicker);

        /// <summary>Takes the other end of a heavy item. See <see cref="PerformPickup"/> on why this
        /// is separate from the RPC.</summary>
        public void PerformSecondaryPickup(NetworkIdentity secondaryPicker)
        {
            if (IsBroken || secondaryPicker == null || PrimaryCarrierNetId == null)
                return;
            if (CurrentCarryMode != CarryMode.Dual)
                return;

            InitiateDualCarry(secondaryPicker);
        }

        private void InitiateDualCarry(NetworkIdentity secondary)
        {
            SecondaryCarrierNetId = secondary;

            Transform primarySocket = ResolveHandSocket(PrimaryCarrierNetId);
            var secondaryRb = secondary.GetComponent<Rigidbody>();
            if (secondaryRb != null && primarySocket != null)
                _carryJoint = CreateCarryJoint(secondaryRb, primarySocket);
        }

        /// <summary>
        /// Builds the dual-carry joint: linear XYZ drives Locked (the load stays fixed relative to the
        /// primary carrier) while angular motion is Free so the object swings naturally between the two
        /// players. The joint's connected body is the primary carrier's rigidbody.
        /// </summary>
        public ConfigurableJoint CreateCarryJoint(Rigidbody secondaryRb, Transform primarySocket)
        {
            var joint = secondaryRb.gameObject.AddComponent<ConfigurableJoint>();

            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;

            joint.angularXMotion = ConfigurableJointMotion.Free;
            joint.angularYMotion = ConfigurableJointMotion.Free;
            joint.angularZMotion = ConfigurableJointMotion.Free;

            var primaryRb = primarySocket.GetComponentInParent<Rigidbody>();
            joint.connectedBody = primaryRb;
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = primaryRb != null
                ? primaryRb.transform.InverseTransformPoint(primarySocket.position)
                : primarySocket.position;

            return joint;
        }

        /// <summary>Release request — drops the item back into free physics and clears carry state.</summary>
        [ServerRpc(requireOwnership: false)]
        public void RequestDrop() => PerformDrop();

        /// <summary>Puts the item down. See <see cref="PerformPickup"/> on why this is separate from
        /// the RPC.</summary>
        public void PerformDrop()
        {
            _isBeingCarried.value = false;
            CurrentCarryMode = CarryMode.None;

            // Return ownership to the server (no player owner). Only meaningful once spawned.
            if (isSpawned)
                GiveOwnership((PlayerID?)null);

            transform.SetParent(null, true);

            if (_carryJoint != null)
            {
                Destroy(_carryJoint);
                _carryJoint = null;
            }

            PrimaryCarrierNetId = null;
            SecondaryCarrierNetId = null;

            if (_rb != null)
                _rb.isKinematic = false;
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        private void ParentToHandSocket(NetworkIdentity carrier)
        {
            Transform socket = ResolveHandSocket(carrier);
            if (socket == null)
                return;
            AttachToSocket(socket);
        }

        // ---------------------------------------------------------------------------------------
        // Spell targets
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// <see cref="IBreakable"/>: shatter this item. Idempotent, and identical to a fatal impact —
        /// a Frango'd vase is exactly as worthless as a dropped one.
        /// </summary>
        public void Break()
        {
            if (IsBroken)
                return;
            BreakItem();
        }

        /// <summary>
        /// <see cref="ILevitatable"/>: lift the item. A carried item is not liftable — it is already
        /// kinematic and parented, and un-sticking it from a carrier's hand mid-carry would strand it.
        /// </summary>
        public void Levitate(Vector3 impulse, float duration, GameObject instigator = null)
        {
            if (IsBroken || IsBeingCarried || _rb == null || _rb.isKinematic)
                return;

            _rb.AddForce(impulse, ForceMode.VelocityChange);
            _levitationRemaining = Mathf.Max(_levitationRemaining, duration);
        }

        /// <summary>Seconds of levitation left; while positive the item ignores gravity.</summary>
        public float LevitationRemaining => _levitationRemaining;

        private float _levitationRemaining;

        private void FixedUpdate()
        {
            if (_levitationRemaining <= 0f)
                return;

            _levitationRemaining -= Time.fixedDeltaTime;

            // Cancel gravity for the duration so the item hangs rather than arcing straight back down.
            if (_rb != null && !_rb.isKinematic)
                _rb.AddForce(-Physics.gravity * _rb.mass, ForceMode.Force);

            if (_levitationRemaining <= 0f)
                _levitationRemaining = 0f;
        }

        /// <summary>Finds a child transform named "HandSocket" on the carrier, falling back to its root.</summary>
        private static Transform ResolveHandSocket(NetworkIdentity carrier)
        {
            if (carrier == null)
                return null;
            Transform socket = carrier.transform.Find("HandSocket");
            return socket != null ? socket : carrier.transform;
        }
    }
}
