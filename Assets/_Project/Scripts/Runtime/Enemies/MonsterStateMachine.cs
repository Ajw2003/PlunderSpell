using System.Collections.Generic;
using Interfaces;
using StateMachine;
using StateMachine.States;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Deprecated: kept as reference, not live.</summary>
[System.Obsolete("Deprecated 2026-10-01: unused by any prefab, scene or code. See docs/reference/deprecated-code.md")]
public class MonsterStateMachine : BaseStateMachine, IHealth, ICarryableCreature
{
    public float CurrentHealth => _health;
    public float MaxHealth => maxHealth;

    public Transform PlayerTarget { get; set; }
    public float MoveSpeed = 3f;
    public List<Vector3> PatrolPoints = new List<Vector3>();
    public int CurrentPatrolPointIndex = 0;
    public float PatrolPointReachedThreshold = 1.0f; // Increased for NavMesh precision

    [Header("Patrol Settings")]
    public float PatrolRadius = 10f;
    public int PatrolPointCount = 3;

    [Header("Vision Cone Settings")]
    public float VisionRange = 10f;
    public float VisionAngle = 90f;
    public LayerMask VisionBlockingLayers;

    [Header("Combat Settings")]
    public float AttackDamage = 10f;
    public float AttackRange = 1.5f;
    public float AttackRate = 1.5f; // Seconds between attacks

    [Header("Escape Settings")]
    public float MinEscapeTime = 2f;
    public float MaxEscapeTime = 5f;

    [Header("Physics Damage Settings")]
    public float MinVelocityForDamage = 3f;

    public NavMeshAgent agent;
    private float _health;
    public float maxHealth = 100;
    private Coroutine _struggleCoroutine;

    // States
    public MonsterPatrolState PatrolState { get; private set; }
    public MonsterPursueState PursueState { get; private set; }
    public MonsterAttackState AttackState { get; private set; }
    public MonsterIdleState IdleState { get; private set; }
    public MonsterDeadState DeadState { get; private set; }
    public MonsterPickedUpState PickedUpState { get; private set; }

    private bool _isActive = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = MoveSpeed;

        PatrolState = new MonsterPatrolState(this);
        PursueState = new MonsterPursueState(this);
        AttackState = new MonsterAttackState(this);
        IdleState = new MonsterIdleState(this);
        DeadState = new MonsterDeadState(this);
        PickedUpState = new MonsterPickedUpState(this);
        _health = maxHealth;
    }

    private void Start()
    {
        // Found by tag rather than PlayerStateMachine's type, so Enemies has no compile-time
        // dependency on Player - the player GameObject must be tagged "Player" in the editor.
        if (PlayerTarget == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                PlayerTarget = playerObject.transform;
            }
        }
    }

    public void Activate()
    {
        _isActive = true;

        if (PatrolPoints.Count == 0)
        {
            GeneratePatrolPoints();
        }

        ChangeState(PatrolState);
    }

    public void Deactivate()
    {
        if (!_isActive) return;
        _isActive = false;

        StopMoving();
        ChangeState(IdleState);
    }

    public void StartStruggling()
    {
        StopStruggling();
        _struggleCoroutine = StartCoroutine(StruggleRoutine());
    }

    public void StopStruggling()
    {
        if (_struggleCoroutine != null)
        {
            StopCoroutine(_struggleCoroutine);
            _struggleCoroutine = null;
        }
    }

    private System.Collections.IEnumerator StruggleRoutine()
    {
        float waitTime = Random.Range(MinEscapeTime, MaxEscapeTime);
        yield return new WaitForSeconds(waitTime);

        Debug.Log($"{gameObject.name} struggled free!");
        if (ItemManager.Instance != null)
        {
            ItemManager.Instance.ForceRelease();
        }

        // Ensure state transition to Idle, which handles the re-activation.
        Release();
    }

    private void GeneratePatrolPoints()
    {
        PatrolPoints.Clear();
        int attempts = 0;
        while (PatrolPoints.Count < PatrolPointCount && attempts < 50)
        {
            attempts++;
            Vector2 randomCircle = Random.insideUnitCircle * PatrolRadius;
            Vector3 randomPoint = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 10.0f, NavMesh.AllAreas))
            {
                PatrolPoints.Add(hit.position);
            }
        }
    }

    public override void ChangeState(IState newState)
    {
        if (newState == CurrentState)
            return;

        base.ChangeState(newState);
    }

    public void TakeDamage(float damage)
    {
        _health -= damage;
        if (_health <= 0)
            Die();
    }

    public void TakeDamage(float damage, float impactVelocity)
    {
        if (impactVelocity < MinVelocityForDamage) return;

        _health -= damage;
        if (_health <= 0)
            Die();
    }

    public void DestroySelf()
    {
        Destroy(this.gameObject);
    }

    public void Die()
    {
        ChangeState(DeadState);
    }

    public void PickUp()
    {
        ChangeState(PickedUpState);
    }

    public void Release()
    {
        // Transition back to Idle. IdleState.Update will handle landing on the NavMesh and
        // re-activating AI logic.
        ChangeState(IdleState);
    }

    public bool CanSeePlayer()
    {
        if (PlayerTarget == null) return false;

        Vector3 offsetToPlayer = PlayerTarget.position - transform.position;

        // Check squared distance first to avoid Sqrt and expensive Angle checks.
        if (offsetToPlayer.sqrMagnitude > VisionRange * VisionRange) return false;

        if (Vector3.Angle(transform.forward, offsetToPlayer) < VisionAngle / 2f)
        {
            Vector3 rayOrigin = transform.position + Vector3.up * 1.5f;
            Vector3 rayDirection = (PlayerTarget.position + Vector3.up * 1.5f) - rayOrigin;

            if (Physics.Raycast(rayOrigin, rayDirection.normalized, out RaycastHit hit, VisionRange, VisionBlockingLayers))
            {
                return hit.collider.transform == PlayerTarget || hit.collider.transform.IsChildOf(PlayerTarget);
            }

            // Raycast hit nothing (e.g. inside a trigger) - treat as visible, matching the
            // original behaviour.
            return true;
        }

        return false;
    }

    [Header("Debug")]
    public bool ShowDebugGizmos = false;

    private void OnDrawGizmosSelected()
    {
        if (!ShowDebugGizmos) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, AttackRange);

        Gizmos.color = Color.yellow;
        Vector3 forward = transform.forward * VisionRange;
        Quaternion leftRayRotation = Quaternion.AngleAxis(-VisionAngle / 2, Vector3.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(VisionAngle / 2, Vector3.up);
        Vector3 leftRayDirection = leftRayRotation * forward;
        Vector3 rightRayDirection = rightRayRotation * forward;

        Gizmos.DrawRay(transform.position + Vector3.up, leftRayDirection);
        Gizmos.DrawRay(transform.position + Vector3.up, rightRayDirection);
        Gizmos.DrawLine(transform.position + Vector3.up + leftRayDirection, transform.position + Vector3.up + rightRayDirection);

        if (PatrolPoints != null && PatrolPoints.Count > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < PatrolPoints.Count; i++)
            {
                Gizmos.DrawWireCube(PatrolPoints[i], Vector3.one * 0.5f);
                if (i < PatrolPoints.Count - 1)
                    Gizmos.DrawLine(PatrolPoints[i], PatrolPoints[i + 1]);
                else
                    Gizmos.DrawLine(PatrolPoints[i], PatrolPoints[0]);
            }
        }
    }

    public void MoveTo(Vector3 targetPosition)
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(targetPosition);
        }
    }

    public void StopMoving()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }
    }

    public bool HasReachedDestination()
    {
        if (agent == null || !agent.isOnNavMesh) return false;

        return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + PatrolPointReachedThreshold;
    }
}
