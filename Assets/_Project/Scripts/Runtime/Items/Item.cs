using Interfaces;
using UnityEngine;
using UnityEngine.AI;

// Physical grab/carry/throw. Uses standard world gravity: the Rigidbody has useGravity = true,
// so a released or thrown item falls along world -Y (Physics.gravity).
[RequireComponent(typeof(Rigidbody))]
public class Item : MonoBehaviour
{
    private Rigidbody _rb;
    private bool _isDragging = false;
    private Vector3 _targetPosition;
    private Quaternion _targetRotation = Quaternion.identity;

    // References for enemy handling. Resolved through Core interfaces, not MonsterStateMachine
    // directly, so Items does not depend on Enemies (Enemies already depends on Items via Item
    // references in the Monster FSM, and a direct reference back would create a cycle).
    private IHealth _monsterHealth;
    private ICarryableCreature _carryableCreature;
    private NavMeshAgent _agent;

    [Header("Physics Settings")]
    [Tooltip("How hard the hand pulls toward where it wants the item, per metre of error (1/s).")]
    [SerializeField] private float _followSpeed = 12f;
    [SerializeField] private float _rotationSpeed = 10f;

    [Tooltip("The most force (N) one hand can apply. Lifting takes mass x 9.81 of it, so a heavy " +
             "item has little left to move with: it lags, swings wide and sags. " +
             "See docs/systems/damage.md, Weight.")]
    [SerializeField] private float _gripStrength = 180f;

    [Tooltip("Fastest a held item is pulled toward the hand, m/s.")]
    [SerializeField] private float _maxHoldSpeed = 14f;

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

    /// <summary>How much a held item slows its carrier: 1 up to 2 kg, falling to 0.5 at 15 kg.</summary>
    public float CarrySpeedMultiplier => Mathf.Clamp(1f - (Mass - 2f) / 26f, 0.5f, 1f);

    private void FixedUpdate()
    {
        if (!_isDragging)
            return;

        // A real body pulled by a hand of limited strength, not a teleport: the solver keeps it out
        // of walls, it carries momentum into whatever it hits, and weight shows as lag and sag.
        float dt = Time.fixedDeltaTime;
        Vector3 wantedVelocity = Vector3.ClampMagnitude((_targetPosition - _rb.position) * _followSpeed, _maxHoldSpeed);
        Vector3 force = (wantedVelocity - _rb.linearVelocity) / dt * _rb.mass - Physics.gravity * _rb.mass;
        _rb.AddForce(Vector3.ClampMagnitude(force, _gripStrength), ForceMode.Force);

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

    public void StartDragging(GameObject holder = null)
    {
        _isDragging = true;
        Holder = holder;

        // Stays a dynamic body while held (see FixedUpdate). It must not collide with the person
        // holding it, or carrying it pushes them around and swinging it hits them.
        _rb.isKinematic = false;
        _rb.WakeUp();
        SetIgnoreHolder(true);

        _targetRotation = transform.rotation;

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
        SetIgnoreHolder(false);

        // Release call removed - Monster handles its own recovery via struggle routine.
    }

    public void Throw(Vector3 direction, float force)
    {
        _isDragging = false;
        _releasedAt = Time.time;
        _rb.isKinematic = false;
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

    public void UpdateTargetPosition(Vector3 position)
    {
        _targetPosition = position;
    }

    public void UpdateRotation(Quaternion rotation)
    {
        _targetRotation = rotation;
    }

    public Quaternion TargetRotation => _targetRotation;
    public bool IsDragging => _isDragging;
}
