using Interfaces;
using UnityEngine;
using UnityEngine.AI;

// Physical grab/carry/throw. Uses standard world gravity: the Rigidbody has useGravity = true,
// so a released or thrown item falls along world -Y (Physics.gravity).
[RequireComponent(typeof(Rigidbody))]
public class Item : MonoBehaviour
{
    /// <summary>
    /// Whether this machine may move the item's body. Always true offline; in a session
    /// Plunderspell.Net installs a check that this machine controls the item's NetworkTransform, since a
    /// body driven here while another machine drives it would snap back every frame.
    /// </summary>
    public static System.Func<Item, bool> CanDriveHere = _ => true;

    /// <summary>Asks for the right to move the item; installed by Plunderspell.Net. The drag starts once
    /// <see cref="CanDriveHere"/> turns true.</summary>
    public static System.Action<Item> RequestDrive;

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
    public float Load => Mass * -Physics.gravity.y / Mathf.Max(1f, _gripStrength);

    /// <summary>True when the item is too heavy to lift and is dragged instead.</summary>
    public bool IsTooHeavyToLift => Load > 1f;

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
        Mathf.Max(k_minTowStrength, k_floorFriction * Mathf.Max(0f, Mass * -Physics.gravity.y - _gripStrength) + 80f);

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
    /// <see cref="TowStrength"/>, so it never outpaces the holder.</summary>
    private void Tow()
    {
        // The grip bears what it can of the weight, straight up through the centre of mass: borne
        // at the grip on a cauldron's rim, it tipped a 25 kg cauldron over onto its rim, where it stuck.
        Vector3 centre = _rb.worldCenterOfMass;
        _rb.AddForce(Vector3.up * Mathf.Min(_gripStrength, _rb.mass * -Physics.gravity.y), ForceMode.Force);

        // Kept upright as it was picked up, free to turn about the vertical so it swings round to
        // trail: a towed altarpiece otherwise fell on its face, and a cauldron onto its rim.
        Vector3 up = _rb.rotation * _uprightLocalUp;
        Vector3 tiltAxis = Vector3.Cross(up, Vector3.up);
        float tilt = Vector3.Angle(up, Vector3.up) * Mathf.Deg2Rad;
        Vector3 spinAboutUp = Vector3.Project(_rb.angularVelocity, Vector3.up);
        _rb.angularVelocity = spinAboutUp + (tiltAxis.sqrMagnitude > 1e-8f ? tiltAxis.normalized * tilt * k_uprightRate : Vector3.zero);

        Vector3 fromHolder = centre - _towFeet;
        fromHolder.y = 0f;
        TowStrain = Mathf.Max(0f, fromHolder.magnitude - _towRope);
        Vector3 wanted = TowVelocity(_towFeet, centre, _towVelocity, _towRope);
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

    private void FixedUpdate()
    {
        _velocityIntoStep = _rb.linearVelocity;
        if (!_isDragging || _isInHand)
            return;

        // A real body hung from the point the player grabbed, pulled by a spring of limited
        // strength: the R.E.P.O. beam (#144, docs/plans/carry-like-repo.md). The force acts at that
        // point, so an off-centre grab swings and turns by itself. The spring damps toward the
        // target's own velocity, so following a walking player needs no jolt. Up and sideways have
        // separate limits: past about 10 kg the up part cannot hold the weight, and the item drags
        // on the floor while the sideways part still hauls it.
        float dt = Time.fixedDeltaTime;

        if (IsTooHeavyToLift && !_isRotating && Time.time - _towStampedAt <= 0.1f)
        {
            Tow();
            return;
        }

        // A target nobody has updated for a while is standing still, whatever it was doing.
        float age = Time.time - _targetStampedAt;
        Vector3 targetVelocity = age > 0.1f ? Vector3.zero : _targetVelocity;
        Vector3 target = _targetPosition + targetVelocity * Mathf.Clamp(age, 0f, 0.05f);
        Vector3 held = HeldPointWorld;
        bool towed = IsTooHeavyToLift && !_isRotating;

        // While its orientation is held, the pull acts at the centre of mass, moved so the held
        // point lands on the target: pulled at an off-centre point instead, a light item was
        // twisted by the spring and twisted back by the orientation hold every step, and shook
        // (17 degrees a step at 0.5 kg). A towed piece is pulled by the point it was grabbed
        // at, so it tips and swings as it scrapes along.
        Vector3 pulled = towed ? held : _rb.worldCenterOfMass;
        Vector3 pulledTarget = target + (pulled - held);
        Vector3 pulledVelocity = _rb.GetPointVelocity(pulled);

        float damping = 2f * Mathf.Sqrt(_springRate) * _dampingRatio;
        Vector3 accel = (pulledTarget - pulled) * _springRate + (targetVelocity - pulledVelocity) * damping;
        Vector3 force = (accel - Physics.gravity) * _rb.mass;

        // A towed piece gets no lift at all, so the floor's full friction holds it back, and a
        // weaker pull, so it is slow to get going: it should feel like a weight on a rope.
        var sideways = Vector3.ClampMagnitude(new Vector3(force.x, 0f, force.z), towed ? TowStrength : _haulStrength);
        float up = towed ? 0f : Mathf.Clamp(force.y, -_gripStrength, _gripStrength);
        _rb.AddForceAtPosition(sideways + Vector3.up * up, pulled, ForceMode.Force);

        // Turned on purpose, or kept as it was picked up and turned with the holder.
        if (towed)
            return;
        Quaternion wanted = _isRotating ? _targetRotation : Quaternion.Euler(0f, _viewYaw, 0f) * _rotationInView;

        Quaternion delta = wanted * Quaternion.Inverse(_rb.rotation);
        delta.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f)
            angle -= 360f;
        Vector3 wantedSpin = Mathf.Abs(angle) < 0.01f || float.IsInfinity(axis.x) || float.IsNaN(axis.x)
            ? Vector3.zero
            : axis * (angle * Mathf.Deg2Rad * _rotationSpeed);
        // Heavy things turn slowly too.
        float turnRate = Mathf.Clamp01(8f / Mathf.Max(1f, _rb.mass) * dt * 10f);
        _rb.angularVelocity = Vector3.Lerp(_rb.angularVelocity, wantedSpin, turnRate);
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
        _isDragging = true;
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

        // An impulse, so the same arm throws a pot far and a chest barely at all.
        _rb.AddForce(direction * force, ForceMode.Impulse);
        _rb.linearVelocity = Vector3.ClampMagnitude(_rb.linearVelocity, _maxThrowSpeed);

        // Release call removed - Monster handles its own recovery via struggle routine.
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
            ApplyIgnore(true);
            return;
        }

        CancelInvoke(nameof(RestoreHolderCollisions));
        Invoke(nameof(RestoreHolderCollisions), 0.4f);
    }

    private void RestoreHolderCollisions()
    {
        if (!_isDragging)
            ApplyIgnore(false);
    }

    private void ApplyIgnore(bool ignore)
    {
        if (_ownColliders == null || _holderColliders == null)
            return;
        foreach (Collider own in _ownColliders)
        {
            foreach (Collider theirs in _holderColliders)
            {
                if (own != null && theirs != null)
                    Physics.IgnoreCollision(own, theirs, ignore);
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
