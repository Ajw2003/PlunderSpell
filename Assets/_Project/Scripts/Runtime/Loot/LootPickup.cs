using Interfaces;
using PurrNet;
using UnityEngine;

namespace Plunderspell.Loot
{
    /// <summary>
    /// Runtime physics + networking behaviour for a single lootable object. The design-time numbers
    /// live on the <see cref="LootItem"/> ScriptableObject; this component turns them into physical
    /// behaviour:
    /// <list type="bullet">
    /// <item>Fragility: a hard collision above <see cref="LootItem.Fragility"/> shatters the item.</item>
    /// <item>Value, levitation and portal-resting for spell targets (<see cref="IBreakable"/>,
    /// <see cref="ILevitatable"/>, <see cref="IPortalResting"/>).</item>
    /// <item>Carry handoff: the actual grabbing and dragging is <see cref="Item"/>'s beam
    /// (<see cref="ItemManager"/>); this component only answers the network's questions about who may
    /// simulate the body (<see cref="RequestCarry"/>, <see cref="RequestHostControl"/>) and applies a
    /// let-go throw on the holder's behalf (<see cref="RequestThrow"/>).</item>
    /// </list>
    ///
    /// PurrNet 1.15 note: PurrNet has no Mirror-style <c>[SyncVar(hook=...)]</c> attribute — replicated
    /// state uses field-based <see cref="SyncVar{T}"/> modules, ownership transfer uses
    /// <see cref="NetworkIdentity.GiveOwnership(PlayerID, bool)"/>, and state-changing entry points are
    /// <c>[ServerRpc]</c> / effects are <c>[ObserversRpc]</c>. Pure decision helpers
    /// (<see cref="WouldBreak"/>) are network-free so they can be unit tested in EditMode without a
    /// live transport.
    /// </summary>
    /// <remarks>
    /// Implements <see cref="IBreakable"/> and <see cref="ILevitatable"/> so spell effects can reach
    /// loot without the Spells assembly referencing Loot (which would cycle back through Player).
    /// Frango shatters loot; Levo lifts it.
    /// </remarks>
    [RequireComponent(typeof(Rigidbody))]
    public class LootPickup : NetworkBehaviour, IBreakable, ILevitatable, IPortalResting
    {
        [Header("Data")]
        [SerializeField] private LootItem _data;

        [Header("Broken-state visuals")]
        [Tooltip("Renderer(s) hidden when the item shatters.")]
        [SerializeField] private MeshRenderer _meshRenderer;
        [Tooltip("Particle VFX enabled when the item shatters.")]
        [SerializeField] private ParticleSystem _brokenVfx;

        private Rigidbody _rb;
        private Item _item;

        // Replicated state (PurrNet field-based SyncVars; inline-initialised so they are never null).
        private readonly SyncVar<bool> _isBroken = new SyncVar<bool>(false);

        // A held piece's combined grip (#169, "Add strengths together"): only the machine
        // controlling the body (the server, while it has any holder) knows every holder's pull, so
        // a client holder's own ItemManager (Load, IsTooHeavyToLift, TowSpeedMultiplier, the beam's
        // colour) reads this instead. 0 means nobody is holding it, which Item.TotalGrip reads as
        // "use your own grip" rather than a real zero.
        private readonly SyncVar<float> _totalGrip = new SyncVar<float>(0f);

        /// <summary>The combined grip <see cref="Item.TotalGrip"/> replicates for a client holder;
        /// see <see cref="_totalGrip"/>.</summary>
        public float TotalGrip => _totalGrip.value;

        /// <summary>Below this the SyncVar is not rewritten: a held piece's total grip does not
        /// change every physics step, so this avoids spamming the network for noise.</summary>
        private const float k_totalGripTolerance = 1f;

        public bool IsBroken => _isBroken.value;

        /// <summary>True while someone holds this piece — this machine's own hold
        /// (<see cref="Item.IsDragging"/>) or a remote holder's pull tallied on the beam
        /// (<see cref="Item.HolderCount"/>). Replaces the old primary/secondary-carrier SyncVar now
        /// that carrying goes through <see cref="Item"/>, not this component.</summary>
        public bool IsBeingCarried => _item != null && (_item.IsDragging || _item.HolderCount > 0);

        public LootItem Data => _data;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _item = GetComponent<Item>();
            _rb.useGravity = true;
            ApplyWeight();
            if (_meshRenderer == null)
                _meshRenderer = GetComponentInChildren<MeshRenderer>();
        }

        /// <summary>
        /// Injects a data source at runtime. Used by <c>DownedPlayerCarryAdapter</c> to turn a downed
        /// player into a carryable object, and by tests to drive fragility/bulk logic.
        /// </summary>
        public void SetData(LootItem data)
        {
            _data = data;
            ApplyWeight();
        }

        /// <summary>
        /// Gives the body its <see cref="LootItem.WeightKg"/>, the one weight to tune: everything a
        /// held or towed item does scales from the body's mass. Not for a downed player, whose body
        /// is carried as loot but must keep a player's mass.
        /// </summary>
        private void ApplyWeight()
        {
            if (_data == null || _rb == null || _data.WeightKg <= 0f || TryGetComponent(out IPlayerBody _))
                return;
            _rb.mass = _data.WeightKg;
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

            // A creature bumping into it never breaks it. Players set their velocity every step, so
            // walking into loot shoves it and then strikes it again while it moves, which shattered
            // a 2 m/s item on the second bump (#142). It still breaks if the shove sends it into a wall.
            if (col.gameObject.GetComponentInParent<IHealth>() != null)
                return;

            ApplyImpact(ImpactSpeed(col));
        }

        /// <summary>
        /// How hard a contact struck this item: the closing speed along the contact normal, so
        /// sliding or tumbling along the floor after a knock is not a fresh blow.
        /// </summary>
        private static float ImpactSpeed(Collision col)
        {
            if (col.contactCount == 0)
                return col.relativeVelocity.magnitude;
            return Mathf.Abs(Vector3.Dot(col.relativeVelocity, col.GetContact(0).normal));
        }

        private bool _inPortal;

        /// <summary>True while the extraction portal holds this piece safe (#158).</summary>
        public bool IsInPortal => _inPortal;

        /// <summary><see cref="IPortalResting"/>: nothing breaks a piece inside the portal.</summary>
        public void SetInPortal(bool inPortal) => _inPortal = inPortal;

        /// <summary>Pure fragility test — does an impact of this magnitude break the item?</summary>
        public bool WouldBreak(float relativeVelocityMagnitude)
        {
            return _data != null &&
                   !_inPortal &&
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
        /// A player wants to hold this in hand (a weapon): hand them the body, so their machine
        /// simulates it while it is held and their movement of it replicates. Ownership then stays
        /// with them until someone else picks it up. A piece on the beam does not use this — see
        /// <see cref="RequestHostControl"/> — since any number of players may hold one at once.
        /// </summary>
        [ServerRpc(requireOwnership: false)]
        public void RequestCarry(RPCInfo info = default)
        {
            if (!IsBroken)
                GiveOwnership(info.sender);
        }

        /// <summary>
        /// A player wants to hold this on the beam: while held it has no owner, so the server
        /// controls it and applies every holder's pull (<see cref="Item.CanDriveHere"/>). Nobody owns
        /// it, so nobody can take it by grabbing it, and it does not leave with a player who quits.
        /// </summary>
        [ServerRpc(requireOwnership: false)]
        public void RequestHostControl()
        {
            if (!IsBroken && hasOwner)
                RemoveOwnership();
        }

        /// <summary>
        /// A holder let go of this while their machine did not control its body: the server, which
        /// does, throws it on their behalf and drops their pull.
        /// </summary>
        [ServerRpc(requireOwnership: false)]
        public void RequestThrow(Vector3 direction, float force, RPCInfo info = default)
        {
            if (TryGetComponent(out Item item))
                item.ApplyRemoteThrow(HolderKey(info.sender), direction, force);
        }

        /// <summary>A stable per-player integer key for a held piece's remote pulls (<see
        /// cref="Item.SetRemotePull"/>), shared by <see cref="Plunderspell.Net.CarryBeamRelay"/>.</summary>
        public static int HolderKey(PlayerID player) => (int)player.id.value;

        private void ApplyBrokenState()
        {
            if (!isSpawned || isServer)
                _isBroken.value = true;

            // Transition bridge: extraction tallies LootValue now, so breaking through the old
            // system has to reach the new one or a smashed piece still pays out. Goes away with
            // LootPickup. See docs/4-systems/raid.md, "Carrying and extracting".
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
        // Spell targets
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// <see cref="IBreakable"/>: shatter this item. Idempotent, and identical to a fatal impact —
        /// a Frango'd vase is exactly as worthless as a dropped one.
        /// </summary>
        public void Break()
        {
            // Safe in the portal, from spells as from falls (#158).
            if (IsBroken || _inPortal)
                return;
            BreakItem();
        }

        /// <summary>
        /// <see cref="ILevitatable"/>: lift the item. A carried item is not liftable — un-sticking it
        /// from whoever is holding it mid-carry would strand it.
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
            UpdateTotalGripSync();

            if (_levitationRemaining <= 0f)
                return;

            _levitationRemaining -= Time.fixedDeltaTime;

            // Cancel gravity for the duration so the item hangs rather than arcing straight back down.
            if (_rb != null && !_rb.isKinematic)
                _rb.AddForce(-Physics.gravity * _rb.mass, ForceMode.Force);

            if (_levitationRemaining <= 0f)
                _levitationRemaining = 0f;
        }

        /// <summary>Keeps <see cref="_totalGrip"/> replicated for a client holder (<see
        /// cref="Item.NetworkedTotalGrip"/>). Only the server writes it, and only while spawned:
        /// offline or on a client, whoever is asking reads the item's own grip directly.</summary>
        private void UpdateTotalGripSync()
        {
            if (!isSpawned || !isServer || _item == null)
                return;
            float current = _item.TotalGripOfHolders;
            if (Mathf.Abs(current - _totalGrip.value) > k_totalGripTolerance)
                _totalGrip.value = current;
        }

    }
}
