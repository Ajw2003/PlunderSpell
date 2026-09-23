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
    [SerializeField] private float _followSpeed = 20f;
    [SerializeField] private float _rotationSpeed = 10f;

    [Header("Damage Settings")]
    [SerializeField] private float _damageMultiplier = 2f;
    [SerializeField] private float _damageCooldown = 0.5f;

    private float _lastDamageTime;

    /// <summary>How long after being let go an item still counts as its thrower's doing.</summary>
    private const float k_blameSeconds = 4f;

    /// <summary>Impact speed (m/s) below which an item nobody touched hurts nothing.</summary>
    private const float k_minUnheldImpactSpeed = 4f;

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

    private void FixedUpdate()
    {
        if (_isDragging)
        {
            // MovePosition follows the target point exactly, rather than fighting the physics
            // solver the way setting linearVelocity every frame would.
            Vector3 newPosition = Vector3.Lerp(_rb.position, _targetPosition, Time.fixedDeltaTime * _followSpeed);
            _rb.MovePosition(newPosition);

            Quaternion newRotation = Quaternion.Slerp(_rb.rotation, _targetRotation, Time.fixedDeltaTime * _rotationSpeed);
            _rb.MoveRotation(newRotation);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_isDragging) return;
        if (Time.time < _lastDamageTime + _damageCooldown) return;

        float impactVelocity = collision.relativeVelocity.magnitude;
        float myVelocity = _rb.linearVelocity.magnitude;

        // Loot settling at spawn or rolling off a shelf is not an attack; only a real hit on its
        // own, or anything a player swung or threw, does damage.
        GameObject blame = Instigator;
        if (blame == null && impactVelocity < k_minUnheldImpactSpeed) return;

        int damage = Mathf.RoundToInt(impactVelocity * _damageMultiplier);
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

        // Kinematic while held: MovePosition follow still works, but the solver stops fighting
        // gravity and collisions to try to move the body itself.
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.isKinematic = true;

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

        // Release call removed - Monster handles its own recovery via struggle routine.
    }

    public void Throw(Vector3 direction, float force)
    {
        _isDragging = false;
        _releasedAt = Time.time;
        _rb.isKinematic = false;
        _rb.AddForce(direction * force, ForceMode.Impulse);

        // Release call removed - Monster handles its own recovery via struggle routine.
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
