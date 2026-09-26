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
    /// RogueAi.Net installs a check that this machine controls the item's NetworkTransform, since a
    /// body driven here while another machine drives it would snap back every frame.
    /// </summary>
    public static System.Func<Item, bool> CanDriveHere = _ => true;

    /// <summary>Asks for the right to move the item; installed by RogueAi.Net. The drag starts once
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

    // True while the player is turning the item on purpose. Otherwise it hangs from the held point.
    private bool _isRotating;
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

    [Header("Physics Settings")]
    [Tooltip("Stiffness of the beam's spring, per metre of error (1/s^2). Higher: the held point " +
             "snaps to the target faster.")]
    [SerializeField] private float _springRate = 120f;

    [Tooltip("Damping of the spring as a fraction of critical. 1: no overshoot.")]
    [Range(0.2f, 2f)] [SerializeField] private float _dampingRatio = 0.9f;

    [SerializeField] private float _rotationSpeed = 10f;

    [Tooltip("The most upward force (N) the beam can apply. Lifting takes mass x 9.81 of it: " +
             "under 10 kg lifts, heavier drags along the floor. See docs/systems/damage.md, Weight.")]
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

    private float _lastDamageTime;

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

    /// <summary>Mass in kg, which is what makes it heavy to carry and hard to swing.</summary>
    public float Mass => _rb != null ? _rb.mass : 1f;

    /// <summary>How much of the beam's strength holding this up takes: 0 weightless, 1 at the limit.
    /// Over 1 it cannot be lifted and drags. The beam's colour reads this.</summary>
    public float Load => Mass * -Physics.gravity.y / Mathf.Max(1f, _gripStrength);

    /// <summary>True when the item is too heavy to lift and is dragged instead.</summary>
    public bool IsTooHeavyToLift => Load > 1f;

    /// <summary>Where the beam is pulling the held point to, extrapolated to now.</summary>
    public Vector3 TargetPosition => _targetPosition;

    private void FixedUpdate()
    {
        if (!_isDragging)
            return;

        // A real body hung from the point the player grabbed, pulled by a spring of limited
        // strength: the R.E.P.O. beam (#144, docs/plans/carry-like-repo.md). The force acts at that
        // point, so an off-centre grab swings and turns by itself. The spring damps toward the
        // target's own velocity, so following a walking player needs no jolt. Up and sideways have
        // separate limits: past about 10 kg the up part cannot hold the weight, and the item drags
        // on the floor while the sideways part still hauls it.
        float dt = Time.fixedDeltaTime;
        // A target nobody has updated for a while is standing still, whatever it was doing.
        float age = Time.time - _targetStampedAt;
        Vector3 targetVelocity = age > 0.1f ? Vector3.zero : _targetVelocity;
        Vector3 target = _targetPosition + targetVelocity * Mathf.Clamp(age, 0f, 0.05f);
        Vector3 held = HeldPointWorld;
        Vector3 heldVelocity = _rb.GetPointVelocity(held);

        float damping = 2f * Mathf.Sqrt(_springRate) * _dampingRatio;
        Vector3 accel = (target - held) * _springRate + (targetVelocity - heldVelocity) * damping;
        Vector3 force = (accel - Physics.gravity) * _rb.mass;

        var sideways = Vector3.ClampMagnitude(new Vector3(force.x, 0f, force.z), _haulStrength);
        float up = Mathf.Clamp(force.y, -_gripStrength, _gripStrength);
        _rb.AddForceAtPosition(sideways + Vector3.up * up, held, ForceMode.Force);

        if (!_isRotating)
            return;

        Quaternion delta = _targetRotation * Quaternion.Inverse(_rb.rotation);
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

        float impactVelocity = collision.relativeVelocity.magnitude;
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
        _angularDampingWhenFree = _rb.angularDamping;
        _rb.angularDamping = _heldAngularDamping;
        _gripOffset = Quaternion.Inverse(_rb.rotation) * (grabPoint - _rb.position);
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
        _isDragging = false;
        _releasedAt = Time.time;
        _rb.isKinematic = false;
        _rb.angularDamping = _angularDampingWhenFree;
        SetIgnoreHolder(false);

        // Release call removed - Monster handles its own recovery via struggle routine.
    }

    public void Throw(Vector3 direction, float force)
    {
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
        _isRotating = rotating;
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
