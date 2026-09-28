using System.Collections.Generic;
using Interfaces;
using UnityEngine;
using UnityEngine.AI;

// Physical grab/carry/throw. Uses standard world gravity: the Rigidbody has useGravity = true,
// so a released or thrown item falls along world -Y (Physics.gravity).
[RequireComponent(typeof(Rigidbody))]
public class Item : MonoBehaviour, Interfaces.IPortalResting
{
    /// <summary>
    /// Whether this machine may move the item's body. Always true offline; in a session
    /// Plunderspell.Net installs a check that this machine controls the item's NetworkTransform, since a
    /// body driven here while another machine drives it would snap back every frame. A piece on the
    /// beam has no owner while held, so the server controls it and every holder's pull is applied
    /// there; an in-hand weapon still hands ownership to whoever holds it.
    /// </summary>
    public static System.Func<Item, bool> CanDriveHere = _ => true;

    /// <summary>
    /// Asks the server to take control of a held piece so it can apply every holder's pull (or, for
    /// an in-hand weapon, asks for ownership); installed by Plunderspell.Net. The drag starts once
    /// <see cref="CanDriveHere"/> turns true.
    /// </summary>
    public static System.Action<Item> RequestDrive;

    /// <summary>Asks the server to apply a throw thrown by a machine that does not control the body;
    /// installed by Plunderspell.Net.</summary>
    public static System.Action<Item, Vector3, float> RequestThrow;

    private Rigidbody _rb;
    private bool _isDragging = false;
    private Vector3 _targetPosition;
    private Quaternion _targetRotation = Quaternion.identity;

    // Where the hand wants the held point, and how fast that point is moving. The velocity is what
    // the spring damps toward, so a target moving at walking pace is followed without a jolt at
    // every change of direction (#119, #144).
    private Vector3 _targetVelocity;
    private float _targetStampedAt;
    private bool _hasTargetVelocity;

    // True while the player is turning the item on purpose.
    private bool _isRotating;

    // Otherwise a held item keeps the orientation it had when picked up, relative to where the
    // holder faces, and turns with them. A free hang from the grab point was floppy.
    private Quaternion _rotationInView = Quaternion.identity;
    private float _viewYaw;

    // A weapon is not hung on the beam: it sits rigidly in the hand, pointing where the player looks.
    // On the beam a crossbow hung on the crosshair line, and its own bolt hit it.
    private bool _isInHand;
    private RigidbodyInterpolation _interpolationWhenFree;
    private int[] _layerWhenFree;
    private bool[] _triggerWhenFree;
    private bool _hasAimFrame;
    private Quaternion _aimFrameLocal;
    private Vector3 _handGripOffset;
    private float _angularDampingWhenFree;

    // The held point, relative to the item's position and in its rotation frame, measured at pickup:
    // where the player grabbed it, or its authored grip.
    private Vector3 _gripOffset;

    // Other holders' pulls, sent over the network and applied here when this machine controls the
    // body (CanDriveHere). Keyed by a stable per-player integer (LootPickup.HolderKey). A pull not
    // heard from for k_remotePullTimeout is dropped, which also covers a holder's disconnect.
    private readonly Dictionary<int, (CarryPull pull, float heardAt)> _remotePulls = new Dictionary<int, (CarryPull, float)>();
    private readonly List<int> _expiredHolderKeys = new List<int>();
    private const float k_remotePullTimeout = 0.5f;

    // Opposite pulls (#169): with more than one holder, a held point left far from its own target
    // means that holder is being pulled away rather than lagging behind a moving one, so the beam
    // snaps instead of stretching forever. Timed per holder (local plus every remote key) so one
    // holder snapping does not touch another's timer; solo play never calls this (FixedUpdate only
    // checks it once holders > 1), so solo feel is unchanged.
    private readonly Dictionary<int, float> _beamStrainSince = new Dictionary<int, float>();
    private readonly List<int> _snappedRemoteHolderKeys = new List<int>();
    private const int k_localHolderKey = -1;
    private const float k_beamSnapDistance = 1.5f;
    private const float k_beamSnapSeconds = 0.3f;
    private bool _localBeamSnapped;

    /// <summary>True once this machine's own hold has snapped from an opposite pull; ItemManager
    /// reads it, the same way it reads the rope-break check, and lets go. Cleared on
    /// <see cref="StartDragging(GameObject, Vector3)"/>.</summary>
    public bool LocalBeamSnapped => _localBeamSnapped;

    /// <summary>Raised (server side, since only the controlling machine ever calls
    /// <see cref="ApplyPull"/> for a remote pull) when a remote holder's beam snaps: the pull is
    /// already removed by the time this fires, so the listener only needs to tell that holder to
    /// let go. Installed by <see cref="Plunderspell.Net.CarryBeamRelay"/>.</summary>
    public static System.Action<Item, int> RemoteBeamSnapped;

    // A remote holder's own body must not collide with the piece they hold, same reason as
    // SetIgnoreHolder for the local holder: holding it close would otherwise bump or block them.
    // Colliders fetched once, when a holder's pull first appears (not on every renewal), and
    // restored k_remoteReleaseDelay after their pull ends, unless they take hold again first.
    private readonly Dictionary<int, Collider[]> _remoteHolderColliders = new Dictionary<int, Collider[]>();
    private readonly List<(int holder, Collider[] colliders, float restoreAt)> _pendingRemoteRestores =
        new List<(int, Collider[], float)>();
    private const float k_remoteReleaseDelay = 0.4f;

    // References for enemy handling. Resolved through Core interfaces, not MonsterStateMachine
    // directly, so Items does not depend on Enemies (Enemies already depends on Items via Item
    // references in the Monster FSM, and a direct reference back would create a cycle).
    private IHealth _monsterHealth;
    private ICarryableCreature _carryableCreature;
    private NavMeshAgent _agent;

    // The one weight knob: for loot, its LootItem's Weight (kg), which the pickup gives the body at
    // spawn; for anything else (weapons), the Rigidbody's Mass. Lifting or towing, the tow pace and
    // pull, throws and impact damage all scale from the body's mass.
    [Header("Weight: loot uses its LootItem's Weight (kg); else the Rigidbody's Mass.")]
    [Header("Physics Settings")]
    [Tooltip("Stiffness of the beam's spring, per metre of error (1/s^2). Higher: the held point " +
             "snaps to the target faster.")]
    [SerializeField] private float _springRate = 120f;

    [Tooltip("Damping of the spring as a fraction of critical. 1: no overshoot.")]
    [Range(0.2f, 2f)] [SerializeField] private float _dampingRatio = 0.9f;

    [SerializeField] private float _rotationSpeed = 10f;

    [Tooltip("The most upward force (N) the beam can apply. Lifting takes mass x 9.81 of it: " +
             "under 10 kg lifts, heavier drags along the floor. See docs/4-systems/damage.md, Weight.")]
    [SerializeField] private float _gripStrength = 100f;

    [Tooltip("The most sideways force (N) the beam can apply. More than it can lift, as hauling " +
             "is easier than lifting: a heavy thing still follows you, only slower.")]
    [SerializeField] private float _haulStrength = 250f;


    [Tooltip("Angular damping while held and not being turned, so it hangs and settles, not spins.")]
    [SerializeField] private float _heldAngularDamping = 3f;

    [Tooltip("Where the hand holds this item. Empty: the centre of its meshes.")]
    [SerializeField] private Transform _gripPoint;

    [Tooltip("Fastest a throw can launch anything, m/s. A light cup is not a bullet.")]
    [SerializeField] private float _maxThrowSpeed = 18f;

    [Header("Damage Settings")]
    [SerializeField] private float _damageMultiplier = 2f;
    [SerializeField] private float _damageCooldown = 0.5f;

    private float _lastDamageTime = float.NegativeInfinity;

    // The body's velocity going into the physics step, before any contact changes it. An impact
    // only counts the item's own motion (#146), and after the step its velocity is already spent.
    private Vector3 _velocityIntoStep;

    /// <summary>How long after being let go an item still counts as its thrower's doing.</summary>
    private const float k_blameSeconds = 4f;

    /// <summary>Impact speed (m/s) below which an item nobody touched hurts nothing.</summary>
    private const float k_minUnheldImpactSpeed = 4f;

    /// <summary>Impact speed (m/s) a held item needs to hurt what it hits.</summary>
    private const float k_minSwingImpactSpeed = 2.5f;

    private Collider[] _ownColliders;
    private Collider[] _holderColliders;

    /// <summary>The player holding this right now, or who last held or threw it.</summary>
    public GameObject Holder { get; private set; }

    private float _releasedAt = float.NegativeInfinity;

    /// <summary>Who is to blame for what this hits: the holder while held, and for a few seconds
    /// after a throw. Null once it is just junk on the floor.</summary>
    public GameObject Instigator =>
        _isDragging || Time.time - _releasedAt <= k_blameSeconds ? Holder : null;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _monsterHealth = GetComponent<IHealth>();
        _carryableCreature = GetComponent<ICarryableCreature>();
        _agent = GetComponent<NavMeshAgent>();

        // Standard world gravity now that the custom gravity module is gone.
        _rb.useGravity = true;

        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        _targetRotation = transform.rotation;
    }

    /// <summary>Mass in kg, the body's: for loot, set from its LootItem's Weight at spawn. Lifting or
    /// towing, the tow pace and pull, throws and impact damage all scale from it.</summary>
    public float Mass => _rb != null ? _rb.mass : 1f;

    /// <summary>How much of the beam's strength holding this up takes: 0 weightless, 1 at the limit.
    /// Over 1 it cannot be lifted and drags. The beam's colour reads this.</summary>
    public float Load => Mass * -Physics.gravity.y / Mathf.Max(1f, TotalGrip);

    /// <summary>True when the item is too heavy to lift and is dragged instead.</summary>
    public bool IsTooHeavyToLift => Load > 1f;

    /// <summary>
    /// Asks the network layer for the grip a spawned <c>LootPickup</c> replicates (0 if nobody is
    /// holding, or offline); installed by Plunderspell.Net (<see cref="NetworkCarry"/>).
    /// </summary>
    public static System.Func<Item, float> NetworkedTotalGrip;

    /// <summary>
    /// How much this piece can be lifted by right now, combining every live holder: on the
    /// controlling machine, the sum of <see cref="CarryPull.GripStrength"/> over this machine's own
    /// hold (if any) plus every remote pull, so two 100 N holders lift up to ~20 kg where one lifts
    /// 10 kg. With nobody holding it (<see cref="TotalGripOfHolders"/> is 0), falls back to this
    /// item's own <see cref="_gripStrength"/>, so an unheld piece's <see cref="Load"/> still reads
    /// sensibly. A machine that does not control the body (a client holder) has no view of every
    /// holder, so it reads the value the server replicated (<see cref="NetworkedTotalGrip"/>)
    /// instead, again falling back to its own grip if nothing has been heard yet.
    /// </summary>
    public float TotalGrip
    {
        get
        {
            if (CanDriveHere(this))
            {
                float total = TotalGripOfHolders;
                return total > 0f ? total : _gripStrength;
            }
            float networked = NetworkedTotalGrip != null ? NetworkedTotalGrip(this) : 0f;
            return networked > 0f ? networked : _gripStrength;
        }
    }

    /// <summary>The raw sum of every live holder's grip on the controlling machine, or 0 with
    /// nobody holding: what <c>LootPickup</c> replicates as its total-grip SyncVar, since there 0
    /// means "use your own grip" to a client that has not heard from anybody holding it.</summary>
    public float TotalGripOfHolders
    {
        get
        {
            float total = 0f;
            if (_isDragging && !_isInHand)
                total += _gripStrength;
            foreach (KeyValuePair<int, (CarryPull pull, float heardAt)> remote in _remotePulls)
                total += remote.Value.pull.GripStrength;
            return total;
        }
    }

    /// <summary>
    /// How fast the holder may walk, as a fraction of their normal pace, while towing this: 1 for
    /// anything they can lift, else <see cref="TowPace"/> (6 / mass). A rope does not stretch: while the piece lags past the rope's length the
    /// holder is held back further, down to a fifth of that pace at a metre of strain.
    /// </summary>
    public float TowSpeedMultiplier
    {
        get
        {
            if (!IsTooHeavyToLift)
                return 1f;
            float heldBack = Mathf.Clamp(1f - (TowStrain - 0.2f) / 0.8f, 0.2f, 1f);
            return TowPace * heldBack;
        }
    }

    /// <summary>
    /// The holder's walking pace while towing this, before any holding back: 6 / mass, so it scales
    /// with the one weight knob, the Rigidbody's Mass (0.5 at 12 kg, 0.4 at 15 kg, 0.2 at 30 kg),
    /// never under 0.15. 1 for anything liftable.
    /// </summary>
    public float TowPace => IsTooHeavyToLift ? Mathf.Clamp(k_towPaceKg / Mass, 0.15f, 1f) : 1f;

    /// <summary>
    /// The tow rope's pull (N), which sets how quickly a towed piece gets up to speed: TowStrength / mass
    /// m/s per second. At least 160 N, more for a piece heavy enough that the floor's friction on the
    /// weight the grip cannot bear would otherwise hold it.
    /// </summary>
    public float TowStrength =>
        Mathf.Max(k_minTowStrength, k_floorFriction * Mathf.Max(0f, Mass * -Physics.gravity.y - TotalGrip) + 80f);

    private const float k_minTowStrength = 160f;

    /// <summary>Unity's default friction, which the castle floors and loot colliders use.</summary>
    private const float k_floorFriction = 0.6f;

    /// <summary>How far past the rope's length the towed piece is lagging, in metres (0 when slack).
    /// Past <see cref="k_ropeBreaksAt"/> the piece is caught on something and the holder lets go.</summary>
    public float TowStrain { get; private set; }

    /// <summary>Strain (m) at which a towed piece is let go: it is stuck, and the holder is not.</summary>
    public const float k_ropeBreaksAt = 2f;

    private const float k_towPaceKg = 6f;

    // The rope a piece too heavy to lift is towed on (#144 follow-up): who is towing, how fast
    // they walk, and how long the rope is. Updated every frame by ItemManager.
    private Vector3 _towFeet;
    private Vector3 _towVelocity;
    private float _towRope;
    private float _towStampedAt = float.NegativeInfinity;
    private Vector3 _uprightLocalUp = Vector3.up;

    /// <summary>How fast a towed piece rights itself (1/s): a tilt of 0.1 rad closes at 0.8 rad/s.</summary>
    private const float k_uprightRate = 8f;

    /// <summary>How much faster than the holder a towed piece may close a stretched rope, per metre of
    /// stretch (1/s).</summary>
    private const float k_ropeCatchUp = 1.5f;

    /// <summary>Tows this piece behind a holder standing at <paramref name="holderFeet"/> and moving at
    /// <paramref name="holderVelocity"/>, on a rope <paramref name="rope"/> metres long. Call every
    /// frame while towing; it lapses after 0.1 s without a call.</summary>
    public void SetTow(Vector3 holderFeet, Vector3 holderVelocity, float rope)
    {
        _towFeet = holderFeet;
        _towVelocity = holderVelocity;
        _towRope = rope;
        _towStampedAt = Time.time;
        // The beam is drawn through the target; a towed piece has none, so aim it at the piece.
        _targetPosition = HeldPointWorld;
    }

    /// <summary>
    /// The velocity a towed piece is driven toward: nothing while the rope is slack (friction stops
    /// it), and once taut, the holder's pace along the rope plus a gentle catch-up for any stretch.
    /// It is never yanked faster than that, so it plods behind instead of lurching. Horizontal only.
    /// </summary>
    public static Vector3 TowVelocity(Vector3 holderFeet, Vector3 piece, Vector3 holderVelocity, float rope)
    {
        Vector3 toHolder = holderFeet - piece;
        toHolder.y = 0f;
        float distance = toHolder.magnitude;
        if (distance <= rope || distance < 1e-4f)
            return Vector3.zero;
        Vector3 along = toHolder / distance;
        float holderPace = Mathf.Max(0f, Vector3.Dot(new Vector3(holderVelocity.x, 0f, holderVelocity.z), along));
        return along * (holderPace + (distance - rope) * k_ropeCatchUp);
    }

    /// <summary>One physics step of towing: the grip bears what weight it can, the piece is kept
    /// upright, and it is hauled toward <see cref="TowVelocity"/>, with at most
    /// <see cref="TowStrength"/>, so it never outpaces the holder. Parametrized so a remote holder's
    /// tow (heard over the network) can be applied the same way as the local holder's; only the
    /// local holder's tow updates <see cref="TowStrain"/>, since that drives that holder's own
    /// walk pace.</summary>
    private void Tow(Vector3 towFeet, Vector3 towVelocity, float towRope, Vector3 uprightLocalUp, bool setStrain)
    {
        // The grip bears what it can of the weight, straight up through the centre of mass: borne
        // at the grip on a cauldron's rim, it tipped a 25 kg cauldron over onto its rim, where it stuck.
        Vector3 centre = _rb.worldCenterOfMass;
        _rb.AddForce(Vector3.up * Mathf.Min(_gripStrength, _rb.mass * -Physics.gravity.y), ForceMode.Force);

        // Kept upright as it was picked up, free to turn about the vertical so it swings round to
        // trail: a towed altarpiece otherwise fell on its face, and a cauldron onto its rim.
        Vector3 up = _rb.rotation * uprightLocalUp;
        Vector3 tiltAxis = Vector3.Cross(up, Vector3.up);
        float tilt = Vector3.Angle(up, Vector3.up) * Mathf.Deg2Rad;
        Vector3 spinAboutUp = Vector3.Project(_rb.angularVelocity, Vector3.up);
        _rb.angularVelocity = spinAboutUp + (tiltAxis.sqrMagnitude > 1e-8f ? tiltAxis.normalized * tilt * k_uprightRate : Vector3.zero);

        Vector3 fromHolder = centre - towFeet;
        fromHolder.y = 0f;
        float strain = Mathf.Max(0f, fromHolder.magnitude - towRope);
        if (setStrain)
            TowStrain = strain;
        Vector3 wanted = TowVelocity(towFeet, centre, towVelocity, towRope);
        // Its ground speed is eased toward that, at most TowStrength / mass per second, so heavier
        // pieces are slower to get going; with the rope slack it slows at k_towBrake. Its own
        // friction is off while towed (SetTowFriction) and this does the braking instead: the
        // floor's friction on a 25 kg cauldron took back 0.14 m/s of every 0.15 m/s step, more
        // than its whole weight should, and varied too much by shape to tune against.
        SetTowFriction(true);
        Vector3 velocity = _rb.linearVelocity;
        var ground = new Vector3(velocity.x, 0f, velocity.z);
        ground = wanted == Vector3.zero
            ? Vector3.MoveTowards(ground, Vector3.zero, k_towBrake * Time.fixedDeltaTime)
            : Vector3.MoveTowards(ground, wanted, TowStrength / _rb.mass * Time.fixedDeltaTime);
        _rb.linearVelocity = new Vector3(ground.x, velocity.y, ground.z);
    }

    /// <summary>How fast a towed piece slows on a slack rope (m/s per second), standing in for the
    /// floor's friction, which is off while towed.</summary>
    private const float k_towBrake = 4f;

    private static PhysicsMaterial s_towMaterial;
    private PhysicsMaterial[] _materialsWhenFree;
    private bool _towFrictionOff;

    /// <summary>Turns the item's own friction off while towed (true) and back as it was (false).</summary>
    private void SetTowFriction(bool off)
    {
        if (off == _towFrictionOff)
            return;
        if (_ownColliders == null)
            _ownColliders = GetComponentsInChildren<Collider>();
        if (off)
        {
            if (s_towMaterial == null)
                s_towMaterial = new PhysicsMaterial("Towed")
                {
                    staticFriction = 0f,
                    dynamicFriction = 0f,
                    frictionCombine = PhysicsMaterialCombine.Minimum,
                };
            _materialsWhenFree = new PhysicsMaterial[_ownColliders.Length];
            for (int i = 0; i < _ownColliders.Length; i++)
            {
                _materialsWhenFree[i] = _ownColliders[i].sharedMaterial;
                _ownColliders[i].sharedMaterial = s_towMaterial;
            }
        }
        else
        {
            for (int i = 0; i < _ownColliders.Length; i++)
            {
                if (_ownColliders[i] != null)
                    _ownColliders[i].sharedMaterial = _materialsWhenFree[i];
            }
        }
        _towFrictionOff = off;
    }

    /// <summary>Where the beam is pulling the held point to, extrapolated to now.</summary>
    public Vector3 TargetPosition => _targetPosition;

    /// <summary>This machine's own hold, valid while <see cref="_isDragging"/>: what it works out
    /// from its grab point, aim and view, sent to whichever machine controls the body
    /// (<see cref="CanDriveHere"/>) so every holder's pull can be applied there.</summary>
    public CarryPull LocalPull => new CarryPull
    {
        GripLocal = _gripOffset,
        Target = _targetPosition,
        TargetVelocity = Time.time - _targetStampedAt > 0.1f ? Vector3.zero : _targetVelocity,
        WantedRotation = _isRotating ? _targetRotation : Quaternion.Euler(0f, _viewYaw, 0f) * _rotationInView,
        IsTowing = IsTooHeavyToLift && !_isRotating && Time.time - _towStampedAt <= 0.1f,
        TowFeet = _towFeet,
        TowVelocity = _towVelocity,
        TowRope = _towRope,
        UprightLocalUp = _uprightLocalUp,
        GripStrength = _gripStrength,
        HaulStrength = _haulStrength,
    };

    /// <summary>Records another holder's pull, heard over the network; applied here once this
    /// machine controls the body. Wakes the body and lifts a portal freeze, since a held piece is
    /// not resting. The first time this holder appears (not on every renewal), <paramref
    /// name="holderBody"/>'s colliders are fetched and told to ignore this piece's own, so holding
    /// it close does not bump or block the holder; null skips that (no body to ignore).</summary>
    public void SetRemotePull(int holder, CarryPull pull, GameObject holderBody)
    {
        bool isNewHolder = !_remotePulls.ContainsKey(holder);
        _remotePulls[holder] = (pull, Time.time);
        _rb.WakeUp();
        if (_frozenByPortal)
        {
            _frozenByPortal = false;
            _rb.isKinematic = false;
        }

        if (!isNewHolder)
            return;

        // Retaking hold before the pending restore from the last release fired: cancel it, or the
        // restore would turn collisions back on while this new hold still needs them off.
        CancelPendingRemoteRestore(holder);

        if (holderBody == null)
            return;
        if (_ownColliders == null)
            _ownColliders = GetComponentsInChildren<Collider>();
        Collider[] theirs = holderBody.GetComponentsInChildren<Collider>();
        _remoteHolderColliders[holder] = theirs;
        ApplyIgnore(theirs, true);
    }

    /// <summary>Drops a holder's pull: they let go, disconnected or died. Counts as a release for
    /// <see cref="UpdatePortalFreeze"/>'s grace period, same as the local holder's own release, so a
    /// piece a client lets go of in the portal still lands first instead of being frozen mid-air.
    /// Schedules that holder's body to stop ignoring this piece again, same delay as the local
    /// holder's own release, so a piece let go close to them does not explode out of their capsule.
    /// </summary>
    public void RemoveRemotePull(int holder)
    {
        _remotePulls.Remove(holder);
        _beamStrainSince.Remove(holder);
        _releasedAt = Time.time;
        ScheduleRemoteRestore(holder);
    }

    private void ScheduleRemoteRestore(int holder)
    {
        if (!_remoteHolderColliders.TryGetValue(holder, out Collider[] theirs))
            return;
        _remoteHolderColliders.Remove(holder);
        CancelPendingRemoteRestore(holder);
        _pendingRemoteRestores.Add((holder, theirs, Time.time + k_remoteReleaseDelay));
    }

    private void CancelPendingRemoteRestore(int holder)
    {
        for (int i = _pendingRemoteRestores.Count - 1; i >= 0; i--)
        {
            if (_pendingRemoteRestores[i].holder == holder)
                _pendingRemoteRestores.RemoveAt(i);
        }
    }

    /// <summary>Turns collisions back on for every remote holder whose delayed restore has come due,
    /// unless they took hold again first (then <see cref="CancelPendingRemoteRestore"/> already
    /// dropped their entry). Walks backward so removing an entry does not skip the next one; no
    /// allocation per frame, since entries are only ever added on a release.</summary>
    private void ProcessPendingRemoteRestores()
    {
        for (int i = _pendingRemoteRestores.Count - 1; i >= 0; i--)
        {
            if (Time.time < _pendingRemoteRestores[i].restoreAt)
                continue;
            ApplyIgnore(_pendingRemoteRestores[i].colliders, false);
            _pendingRemoteRestores.RemoveAt(i);
        }
    }

    /// <summary>How many holders are pulling this right now: this machine's own hold, if any, plus
    /// every live remote pull.</summary>
    public int HolderCount => (_isDragging && !_isInHand ? 1 : 0) + _remotePulls.Count;

    // Loot in the portal (#158): frozen once it settles, until picked up or out of the portal.
    private bool _inPortal;
    private bool _frozenByPortal;
    private const float k_portalSettleSpeed = 0.15f;

    /// <summary>How long after a release/throw <see cref="UpdatePortalFreeze"/> holds off: a queued
    /// impulse (a throw, or gravity itself) only shows up in <see cref="Rigidbody.linearVelocity"/>
    /// after the next physics step (FixedUpdate scripts run before the step), so a piece read right
    /// after it was let go looks settled and would be frozen with that impulse still pending.</summary>
    private const float k_portalFreezeGrace = 0.5f;

    /// <summary>True while the portal is holding this piece still.</summary>
    public bool IsFrozenByPortal => _frozenByPortal;

    /// <summary><see cref="Interfaces.IPortalResting"/>: the extraction portal took this piece in, or let it go.</summary>
    public void SetInPortal(bool inPortal)
    {
        _inPortal = inPortal;
        if (!inPortal)
            ReleasePortalFreeze();
    }

    private void ReleasePortalFreeze()
    {
        if (!_frozenByPortal)
            return;
        _frozenByPortal = false;
        if (!_isInHand)
            _rb.isKinematic = false;
    }

    /// <summary>Freezes a piece resting in the portal: not held, and slow enough to have settled,
    /// so a dropped piece still lands first.</summary>
    private void UpdatePortalFreeze()
    {
        // A remote hold means someone else is holding this piece, same as _isDragging locally: not
        // resting, so it must not freeze mid-hold.
        if (!_inPortal || _frozenByPortal || _isDragging || _rb.isKinematic || !CanDriveHere(this) || _remotePulls.Count > 0
            || Time.time - _releasedAt < k_portalFreezeGrace)
            return;
        if (_rb.linearVelocity.magnitude > k_portalSettleSpeed || _rb.angularVelocity.magnitude > 0.5f)
            return;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.isKinematic = true;
        _frozenByPortal = true;
    }

    private void FixedUpdate()
    {
        _velocityIntoStep = _rb.linearVelocity;
        UpdatePortalFreeze();
        ProcessPendingRemoteRestores();
        // A machine that does not control the body never pushes it: elsewhere the body is moved by
        // the replicated transform, and pushing it here as well would fight that every step.
        if (!CanDriveHere(this))
            return;

        ExpireRemotePulls();

        bool localHold = _isDragging && !_isInHand;
        int holders = (localHold ? 1 : 0) + _remotePulls.Count;
        if (holders == 0)
            return;

        // A real body hung from the point the player grabbed, pulled by a spring of limited
        // strength: the R.E.P.O. beam (#144, docs/plans/carry-like-repo.md). The force acts at that
        // point, so an off-centre grab swings and turns by itself. The spring damps toward the
        // target's own velocity, so following a walking player needs no jolt. Up and sideways have
        // separate limits: past about 10 kg the up part cannot hold the weight, and the item drags
        // on the floor while the sideways part still hauls it.
        bool localSnappedBefore = _localBeamSnapped;
        if (localHold)
            ApplyPull(LocalPull, true, Time.time - _targetStampedAt, holders, k_localHolderKey);

        // Collected rather than removed on the spot: a remote pull that snaps must not be dropped
        // from _remotePulls mid-enumeration of it below.
        _snappedRemoteHolderKeys.Clear();
        foreach (KeyValuePair<int, (CarryPull pull, float heardAt)> remote in _remotePulls)
            ApplyPull(remote.Value.pull, false, Time.time - remote.Value.heardAt, holders, remote.Key);

        // Pulled apart, the piece drops: every holder lets go, not just the first beam to give.
        // Snapping one at a time left the other holder alone, and a lone holder never snaps, so a
        // tug of war handed the piece to whoever held on longest.
        if ((_localBeamSnapped && !localSnappedBefore) || _snappedRemoteHolderKeys.Count > 0)
        {
            if (localHold)
                _localBeamSnapped = true;
            _snappedRemoteHolderKeys.Clear();
            foreach (int key in _remotePulls.Keys)
                _snappedRemoteHolderKeys.Add(key);
            _beamStrainSince.Clear();
        }

        foreach (int key in _snappedRemoteHolderKeys)
        {
            RemoveRemotePull(key);
            RemoteBeamSnapped?.Invoke(this, key);
        }
    }

    /// <summary>Drops a remote pull nobody has renewed for <see cref="k_remotePullTimeout"/>: the
    /// holder let go, disconnected, or their machine stopped sending. No allocation: reuses
    /// <see cref="_expiredHolderKeys"/> instead of building a new list every step.</summary>
    private void ExpireRemotePulls()
    {
        _expiredHolderKeys.Clear();
        foreach (KeyValuePair<int, (CarryPull pull, float heardAt)> remote in _remotePulls)
        {
            if (Time.time - remote.Value.heardAt > k_remotePullTimeout)
                _expiredHolderKeys.Add(remote.Key);
        }
        foreach (int key in _expiredHolderKeys)
        {
            _remotePulls.Remove(key);
            // Expiry ends a holder's pull the same as RemoveRemotePull: their body's ignore needs
            // the same delayed restore, and it counts toward UpdatePortalFreeze's grace period too.
            ScheduleRemoteRestore(key);
        }
        if (_expiredHolderKeys.Count > 0)
            _releasedAt = Time.time;
    }

    /// <summary>
    /// Applies one holder's pull. With exactly one LOCAL holder this is numerically identical to the
    /// original single-holder carry, so <c>CarryFeelTests</c> sees the same numbers. With more than
    /// one holder, gravity is shared between them, each pulls at its own held point instead of the
    /// centre of mass, and the orientation hold is skipped (multi-holder turning is a later step).
    /// </summary>
    /// <param name="pull">The holder's pull.</param>
    /// <param name="isLocal">Whether this is this machine's own hold, rather than one heard from
    /// the network.</param>
    /// <param name="age">Seconds since the pull's target was last updated (local) or last heard
    /// (remote); a stale target is treated as standing still.</param>
    /// <param name="holders">How many holders are pulling this piece right now.</param>
    /// <param name="holderKey">This holder's key into <see cref="_beamStrainSince"/>:
    /// <see cref="k_localHolderKey"/> for the local hold, else its remote key.</param>
    private void ApplyPull(CarryPull pull, bool isLocal, float age, int holders, int holderKey)
    {
        if (pull.IsTowing)
        {
            // Only the local holder's tow updates TowStrain, which drives that holder's own walk
            // pace; a remote holder's tow still pulls the body but says nothing about our pace.
            Tow(pull.TowFeet, pull.TowVelocity, pull.TowRope, pull.UprightLocalUp, isLocal);
            return;
        }

        // A target nobody has updated for a while is standing still, whatever it was doing. Network
        // updates arrive about 15 times a second, so a remote pull tolerates a longer gap before it
        // is treated as stale.
        Vector3 targetVelocity = isLocal
            ? (age > 0.1f ? Vector3.zero : pull.TargetVelocity)
            : (age > 0.25f ? Vector3.zero : pull.TargetVelocity);
        float extrapolateClamp = isLocal ? 0.05f : 0.1f;
        Vector3 target = pull.Target + targetVelocity * Mathf.Clamp(age, 0f, extrapolateClamp);
        Vector3 held = _rb.position + _rb.rotation * pull.GripLocal;

        // Opposite pulls (#169): only past one holder, so solo feel (holders == 1) never snaps.
        if (holders > 1)
            UpdateBeamSnap(holderKey, isLocal, held, target);

        // A too-heavy piece whose tow input has gone stale (nobody towing it this step) still drags
        // along the floor rather than springing free, same as before this was split out.
        bool towed = isLocal && IsTooHeavyToLift && !_isRotating;

        // While its orientation is held, one holder's pull acts at the centre of mass, moved so the
        // held point lands on the target: pulled at an off-centre point instead, a light item was
        // twisted by the spring and twisted back by the orientation hold every step, and shook
        // (17 degrees a step at 0.5 kg). A towed piece is pulled by the point it was grabbed at, so
        // it tips and swings as it scrapes along. With more than one holder each pulls at its own
        // held point, so two holders at different points turn the piece naturally.
        Vector3 pulled = holders == 1 ? (towed ? held : _rb.worldCenterOfMass) : held;
        Vector3 pulledTarget = target + (pulled - held);
        Vector3 pulledVelocity = _rb.GetPointVelocity(pulled);

        float damping = 2f * Mathf.Sqrt(_springRate) * _dampingRatio;
        Vector3 accel = (pulledTarget - pulled) * _springRate + (targetVelocity - pulledVelocity) * damping;
        // Gravity is shared equally between every holder; with one holder this is exactly
        // (accel - Physics.gravity) * mass, as before.
        Vector3 force = accel * _rb.mass - Physics.gravity * _rb.mass / holders;

        // A towed piece gets no lift at all, so the floor's full friction holds it back, and a
        // weaker pull, so it is slow to get going: it should feel like a weight on a rope. Each
        // holder's upward part is capped by their own grip, and sideways by their own haul, so a
        // strength upgrade later only has to change the pull it sends.
        var sideways = Vector3.ClampMagnitude(new Vector3(force.x, 0f, force.z), towed ? TowStrength : pull.HaulStrength);
        float up = towed ? 0f : Mathf.Clamp(force.y, -pull.GripStrength, pull.GripStrength);
        _rb.AddForceAtPosition(sideways + Vector3.up * up, pulled, ForceMode.Force);

        // Turned on purpose, or kept as it was picked up and turned with the holder. Skipped for a
        // towed piece, and with more than one holder (step 5 replaces this with torque).
        if (towed || holders > 1)
            return;
        Quaternion wanted = pull.WantedRotation;

        Quaternion delta = wanted * Quaternion.Inverse(_rb.rotation);
        delta.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f)
            angle -= 360f;
        Vector3 wantedSpin = Mathf.Abs(angle) < 0.01f || float.IsInfinity(axis.x) || float.IsNaN(axis.x)
            ? Vector3.zero
            : axis * (angle * Mathf.Deg2Rad * _rotationSpeed);
        // Heavy things turn slowly too.
        float turnRate = Mathf.Clamp01(8f / Mathf.Max(1f, _rb.mass) * Time.fixedDeltaTime * 10f);
        _rb.angularVelocity = Vector3.Lerp(_rb.angularVelocity, wantedSpin, turnRate);
    }

    /// <summary>
    /// Tracks how long <paramref name="holderKey"/>'s held point has sat more than
    /// <see cref="k_beamSnapDistance"/> from its own target: past <see cref="k_beamSnapSeconds"/>
    /// continuously, that holder is being pulled away rather than lagging behind a moving target,
    /// so their beam snaps. The local holder's flag is set at once (<see cref="ItemManager"/> reads
    /// it next frame); a remote holder's key is queued in <see cref="_snappedRemoteHolderKeys"/>
    /// rather than removed here, since this runs from inside a foreach over
    /// <see cref="_remotePulls"/> in <see cref="FixedUpdate"/>.
    /// </summary>
    private void UpdateBeamSnap(int holderKey, bool isLocal, Vector3 held, Vector3 target)
    {
        if (Vector3.Distance(held, target) <= k_beamSnapDistance)
        {
            _beamStrainSince.Remove(holderKey);
            return;
        }
        if (!_beamStrainSince.TryGetValue(holderKey, out float since))
        {
            _beamStrainSince[holderKey] = Time.time;
            return;
        }
        if (Time.time - since < k_beamSnapSeconds)
            return;

        _beamStrainSince.Remove(holderKey);
        if (isLocal)
            _localBeamSnapped = true;
        else
            _snappedRemoteHolderKeys.Add(holderKey);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (Time.time < _lastDamageTime + _damageCooldown) return;

        // Only the item's own motion hurts: walking into a cauldron on the floor is not being hit
        // by it, though the contact's relative speed is the walker's speed (#146).
        float impactVelocity = Mathf.Min(collision.relativeVelocity.magnitude, _velocityIntoStep.magnitude);
        float myVelocity = _rb.linearVelocity.magnitude;

        // Loot settling at spawn or rolling off a shelf is not an attack; only a real hit on its
        // own, or anything a player swung or threw, does damage.
        GameObject blame = Instigator;
        if (blame == null && impactVelocity < k_minUnheldImpactSpeed) return;

        // Swung while held: resting it against someone is not a hit.
        if (_isDragging && impactVelocity < k_minSwingImpactSpeed) return;

        // Weight hits harder: x1 at 1 kg, x1.5 at 4 kg, x2 at 9 kg.
        float heft = 0.5f + 0.5f * Mathf.Sqrt(Mathf.Max(0.25f, Mass));
        int damage = Mathf.RoundToInt(impactVelocity * _damageMultiplier * heft);
        bool dealtDamage = false;
        Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
        GameObject instigator = blame;

        IHealth targetHealth = collision.gameObject.GetComponentInParent<IHealth>();
        if (targetHealth != null)
        {
            dealtDamage = Damage.Apply(targetHealth, damage, gameObject, instigator, point,
                DamageKind.Impact, impactVelocity) > 0f;
        }

        // Damage ourselves if we are an enemy item being thrown. GetComponent<IHealth>() hands
        // back an interface reference, so Unity's fake-null override (which only applies to a
        // statically-typed UnityEngine.Object) does not kick in here — check the underlying
        // Object directly, or a destroyed monster reads as still alive.
        if (_monsterHealth != null && (Object)_monsterHealth != null)
        {
            // Only take impact damage if we are NOT grounded/active.
            if (!_agent.enabled || myVelocity > 1f)
            {
                if (Damage.Apply(_monsterHealth, damage, collision.gameObject, instigator, point,
                        DamageKind.Impact, impactVelocity) > 0f)
                    dealtDamage = true;
            }
        }

        if (dealtDamage)
        {
            _lastDamageTime = Time.time;
            Debug.Log($"Impact Damage Dealt: {damage} (Impact: {impactVelocity:F1})");
        }
    }

    /// <summary>Picks the item up by its authored grip (or mesh centre).</summary>
    public void StartDragging(GameObject holder = null) => StartDragging(holder, AuthoredGripWorld);

    /// <summary>Picks the item up by <paramref name="grabPoint"/>, the point on it the player aimed
    /// at. It hangs from there while held.</summary>
    public void StartDragging(GameObject holder, Vector3 grabPoint)
    {
        _frozenByPortal = false;
        _isDragging = true;
        _localBeamSnapped = false;
        _beamStrainSince.Remove(k_localHolderKey);
        Holder = holder;
        // Stays a dynamic body while held (see FixedUpdate). It must not collide with the person
        // holding it, or carrying it pushes them around and swinging it hits them.
        _rb.isKinematic = false;
        _rb.WakeUp();
        SetIgnoreHolder(true);
        _targetRotation = transform.rotation;
        _isRotating = false;
        _rotationInView = Quaternion.Euler(0f, -_viewYaw, 0f) * _rb.rotation;
        _angularDampingWhenFree = _rb.angularDamping;
        _rb.angularDamping = _heldAngularDamping;
        _gripOffset = Quaternion.Inverse(_rb.rotation) * (grabPoint - _rb.position);
        // Which of its own axes points up now, so towing can keep it that way up.
        _uprightLocalUp = Quaternion.Inverse(_rb.rotation) * Vector3.up;
        // Hold it where it is until the hand says otherwise.
        _hasTargetVelocity = false;
        UpdateTargetPosition(grabPoint);

        if (_carryableCreature != null && (Object)_carryableCreature != null)
        {
            _carryableCreature.PickUp();
        }
    }

    public void StopDragging()
    {
        TowStrain = 0f;
        SetTowFriction(false);
        HoldInHand(false);
        _isDragging = false;
        _releasedAt = Time.time;
        _rb.isKinematic = false;
        _rb.angularDamping = _angularDampingWhenFree;
        SetIgnoreHolder(false);

        // Release call removed - Monster handles its own recovery via struggle routine.
    }

    public void Throw(Vector3 direction, float force)
    {
        SetTowFriction(false);
        HoldInHand(false);
        _isDragging = false;
        _releasedAt = Time.time;
        _rb.isKinematic = false;
        _rb.angularDamping = _angularDampingWhenFree;
        SetIgnoreHolder(false);

        // This machine's own hold is cleared either way; only the throw itself needs the machine
        // that controls the body, which asks the server to apply it when this one does not.
        if (!CanDriveHere(this))
        {
            RequestThrow?.Invoke(this, direction, force);
            return;
        }

        // An impulse, so the same arm throws a pot far and a chest barely at all.
        _rb.AddForce(direction * force, ForceMode.Impulse);
        _rb.linearVelocity = Vector3.ClampMagnitude(_rb.linearVelocity, _maxThrowSpeed);

        // Release call removed - Monster handles its own recovery via struggle routine.
    }

    /// <summary>Applies a throw asked for by a holder whose machine does not control this body, so
    /// the server can throw a piece any holder lets go of with force.</summary>
    public void ApplyRemoteThrow(int holder, Vector3 direction, float force)
    {
        RemoveRemotePull(holder);
        // The pull ending (RemoveRemotePull above) can leave the piece resting in the portal for a
        // moment before this throw's impulse lands, and UpdatePortalFreeze freezes anything that
        // slow: an impulse added to a kinematic body does nothing, so the throw must lift the
        // freeze first, the same as SetRemotePull already does for a renewed pull.
        _rb.WakeUp();
        if (_frozenByPortal)
        {
            _frozenByPortal = false;
            _rb.isKinematic = false;
        }
        _rb.AddForce(direction * force, ForceMode.Impulse);
        _rb.linearVelocity = Vector3.ClampMagnitude(_rb.linearVelocity, _maxThrowSpeed);
        _releasedAt = Time.time;
    }

    /// <summary>
    /// Turns collisions between this item and its holder off while held, and back on shortly after
    /// release, so a dropped item does not explode out of the holder's capsule.
    /// </summary>
    private void SetIgnoreHolder(bool ignore)
    {
        if (Holder == null)
            return;

        if (_ownColliders == null)
            _ownColliders = GetComponentsInChildren<Collider>();
        _holderColliders = Holder.GetComponentsInChildren<Collider>();

        if (ignore)
        {
            CancelInvoke(nameof(RestoreHolderCollisions));
            ApplyIgnore(_holderColliders, true);
            return;
        }

        CancelInvoke(nameof(RestoreHolderCollisions));
        Invoke(nameof(RestoreHolderCollisions), 0.4f);
    }

    private void RestoreHolderCollisions()
    {
        if (!_isDragging)
            ApplyIgnore(_holderColliders, false);
    }

    /// <summary>Turns collisions between this piece's own colliders and <paramref name="theirs"/> on
    /// or off. Shared by the local holder (<see cref="SetIgnoreHolder"/>) and every remote holder
    /// (<see cref="SetRemotePull"/>/<see cref="ScheduleRemoteRestore"/>).</summary>
    private void ApplyIgnore(Collider[] theirs, bool ignore)
    {
        if (_ownColliders == null || theirs == null)
            return;
        foreach (Collider own in _ownColliders)
        {
            foreach (Collider theirCollider in theirs)
            {
                if (own != null && theirCollider != null)
                    Physics.IgnoreCollision(own, theirCollider, ignore);
            }
        }
    }

    /// <summary>
    /// Where the held point should be. Its velocity is estimated from successive calls, smoothed,
    /// unless <see cref="UpdateTarget"/> supplies one.
    /// </summary>
    public void UpdateTargetPosition(Vector3 position)
    {
        if (!_hasTargetVelocity)
        {
            UpdateTarget(position, Vector3.zero);
            return;
        }

        float elapsed = Time.time - _targetStampedAt;
        if (elapsed <= 1e-4f)
        {
            // A second call in the same frame: no new time has passed to measure a velocity over.
            _targetPosition = position;
            return;
        }

        Vector3 measured = (position - _targetPosition) / elapsed;
        UpdateTarget(position, Vector3.Lerp(_targetVelocity, measured, 0.5f));
    }

    /// <summary>Where the held point should be, and how fast that target is moving.</summary>
    public void UpdateTarget(Vector3 position, Vector3 velocity)
    {
        _targetPosition = position;
        _targetVelocity = velocity;
        _targetStampedAt = Time.time;
        _hasTargetVelocity = true;
    }

    public void UpdateRotation(Quaternion rotation)
    {
        _targetRotation = rotation;
    }

    /// <summary>Turning the item on purpose (the rotate button held). Starting keeps its current
    /// orientation as the target; letting go lets it hang freely again.</summary>
    public void SetRotating(bool rotating)
    {
        if (rotating && !_isRotating)
            _targetRotation = _rb.rotation;
        // Let go of the rotate button: keep the orientation it was turned to.
        if (!rotating && _isRotating)
            _rotationInView = Quaternion.Euler(0f, -_viewYaw, 0f) * _targetRotation;
        _isRotating = rotating;
    }

    /// <summary>The holder's facing, in degrees about world up. A held item keeps its orientation
    /// relative to this, so it turns with the holder instead of flopping. Call every frame while
    /// held; before a pickup it sets the frame the pickup orientation is measured in.</summary>
    public void SetViewYaw(float yawDegrees) => _viewYaw = yawDegrees;

    /// <summary>True while held rigidly in the hand (weapons), rather than hung on the beam.</summary>
    public bool IsInHand => _isInHand;

    private const int k_ignoreRaycastLayer = 2;

    /// <summary>
    /// Holds the item rigidly in the hand (true) or puts it back on the beam (false). In the hand
    /// the body is kinematic and its colliders are triggers on the Ignore Raycast layer, so it
    /// neither shoves loot about nor stops its own shots. <see cref="SetHandPose"/> places it.
    /// </summary>
    public void HoldInHand(bool inHand)
    {
        if (inHand == _isInHand)
            return;
        _isInHand = inHand;
        if (_ownColliders == null)
            _ownColliders = GetComponentsInChildren<Collider>();

        if (inHand)
        {
            _interpolationWhenFree = _rb.interpolation;
            _triggerWhenFree = new bool[_ownColliders.Length];
            _layerWhenFree = new int[_ownColliders.Length];
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;
            _rb.interpolation = RigidbodyInterpolation.None;
            // Held by its authored grip, or by its origin (a crossbow's butt, a sword's pommel),
            // never by whatever point the crosshair happened to be on.
            _handGripOffset = _gripPoint != null
                ? Quaternion.Inverse(_rb.rotation) * (_gripPoint.position - _rb.position)
                : Vector3.zero;
            for (int i = 0; i < _ownColliders.Length; i++)
            {
                _triggerWhenFree[i] = _ownColliders[i].isTrigger;
                _layerWhenFree[i] = _ownColliders[i].gameObject.layer;
                _ownColliders[i].isTrigger = true;
                _ownColliders[i].gameObject.layer = k_ignoreRaycastLayer;
            }
            return;
        }

        _rb.isKinematic = false;
        _rb.interpolation = _interpolationWhenFree;
        for (int i = 0; i < _ownColliders.Length; i++)
        {
            if (_ownColliders[i] == null)
                continue;
            _ownColliders[i].isTrigger = _triggerWhenFree[i];
            _ownColliders[i].gameObject.layer = _layerWhenFree[i];
        }
    }

    /// <summary>Places an in-hand item with its held point at <paramref name="handPosition"/>,
    /// pointing along <paramref name="view"/>'s forward. Called just before the camera renders, so
    /// it never lags the view.</summary>
    public void SetHandPose(Vector3 handPosition, Quaternion view)
    {
        if (!_isInHand)
            return;
        Quaternion rotation = view * Quaternion.Inverse(AimFrameLocal());
        Vector3 position = handPosition - rotation * _handGripOffset;
        transform.SetPositionAndRotation(position, rotation);
        _rb.position = position;
        _rb.rotation = rotation;
    }

    /// <summary>
    /// The item's own pointing frame: forward along its longest reach from its origin (a crossbow's
    /// stock to its prod, a sword's hilt to its tip), up along the next longest. Measured from the
    /// meshes, since the forged weapons point along different local axes.
    /// </summary>
    public Quaternion AimFrameLocal()
    {
        if (_hasAimFrame)
            return _aimFrameLocal;
        _hasAimFrame = true;
        var bounds = new Bounds();
        bool any = false;
        Matrix4x4 toLocal = transform.worldToLocalMatrix;
        foreach (MeshFilter filter in GetComponentsInChildren<MeshFilter>())
        {
            if (filter.sharedMesh == null)
                continue;
            Bounds mesh = filter.sharedMesh.bounds;
            Matrix4x4 meshToLocal = toLocal * filter.transform.localToWorldMatrix;
            foreach (Vector3 corner in new[] { mesh.min, mesh.max })
            {
                Vector3 point = meshToLocal.MultiplyPoint3x4(corner);
                if (!any)
                    bounds = new Bounds(point, Vector3.zero);
                else
                    bounds.Encapsulate(point);
                any = true;
            }
        }
        Vector3 reach = any ? bounds.center : Vector3.forward;
        Vector3 forward = DominantAxis(reach, Vector3.zero);
        if (forward == Vector3.zero)
            forward = Vector3.forward;
        Vector3 up = DominantAxis(reach, forward);
        if (up == Vector3.zero)
            up = Mathf.Abs(forward.y) > 0.5f ? Vector3.forward : Vector3.up;
        _aimFrameLocal = Quaternion.LookRotation(forward, up);
        return _aimFrameLocal;
    }

    /// <summary>The signed unit axis along which <paramref name="v"/> reaches furthest, skipping
    /// <paramref name="exclude"/>'s axis.</summary>
    private static Vector3 DominantAxis(Vector3 v, Vector3 exclude)
    {
        Vector3 best = Vector3.zero;
        float bestSize = 1e-4f;
        foreach (Vector3 axis in new[] { Vector3.right, Vector3.up, Vector3.forward })
        {
            if (Mathf.Abs(Vector3.Dot(axis, exclude)) > 0.5f)
                continue;
            float size = Mathf.Abs(Vector3.Dot(v, axis));
            if (size > bestSize)
            {
                bestSize = size;
                best = axis * Mathf.Sign(Vector3.Dot(v, axis));
            }
        }
        return best;
    }

    /// <summary>Sets the authored grip, used when a pickup has no aimed point; null uses the mesh
    /// centre.</summary>
    public void SetGripPoint(Transform gripPoint) => _gripPoint = gripPoint;

    /// <summary>The authored grip, or the mesh centre.</summary>
    private Vector3 AuthoredGripWorld => _gripPoint != null ? _gripPoint.position : MeshCentre();

    /// <summary>The held point, in world space, from the physics body's pose.</summary>
    private Vector3 HeldPointWorld => _rb.position + _rb.rotation * _gripOffset;

    /// <summary>The held point in the item's own frame, for sending over the network.</summary>
    public Vector3 HeldPointLocal => _gripOffset;

    /// <summary>A point given in the item's own frame (like <see cref="HeldPointLocal"/>), in world
    /// space as the item is drawn.</summary>
    public Vector3 LocalToWorldPoint(Vector3 local) => transform.position + transform.rotation * local;

    /// <summary>Where the item is held while held, as drawn; its authored grip otherwise.</summary>
    public Vector3 GripWorldPosition => _isDragging ? LocalToWorldPoint(_gripOffset) : AuthoredGripWorld;

    private Vector3 MeshCentre()
    {
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

    public Quaternion TargetRotation => _targetRotation;
    public bool IsDragging => _isDragging;
}
