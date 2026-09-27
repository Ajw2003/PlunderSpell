using Interfaces;
using Plunderspell.Acoustics;
using UnityEngine;

/// <summary>Turns a held <see cref="Item"/> into a melee weapon. See docs/6-decisions/Decisions.md, "Melee hit
/// detection has no enemy layer to filter on".</summary>
[RequireComponent(typeof(Item))]
[RequireComponent(typeof(AcousticEmitter))]
public class MeleeWeapon : MonoBehaviour
{
    private static readonly Collider[] s_hitBuffer = new Collider[16];

    [SerializeField] private MeleeWeaponStats m_stats;

    [Tooltip("Radius of the hit-check sphere at the tip of the weapon's reach, in metres.")]
    [SerializeField] private float m_hitRadius = 1f;

    [Range(0f, 1f)]
    [Tooltip("Noise strength (0..1) broadcast on every swing, before wall attenuation.")]
    [SerializeField] private float m_swingNoiseStrength = 0.6f;

    private AcousticEmitter m_emitter;
    private float m_lastSwingTime = float.NegativeInfinity;

    public MeleeWeaponStats Stats => m_stats;
    public bool IsReady => m_stats == null || Time.time >= m_lastSwingTime + m_stats.SwingDuration;

    private void Awake()
    {
        m_emitter = GetComponent<AcousticEmitter>();
    }

    /// <summary>Swings from <paramref name="origin"/> toward <paramref name="forward"/>. No-ops
    /// (returns false) while still recovering from the last swing.</summary>
    public bool TrySwing(Vector3 origin, Vector3 forward)
    {
        if (m_stats == null || !IsReady)
            return false;

        m_lastSwingTime = Time.time;
        DealDamage(origin, forward);
        AlertNearbyListeners();
        return true;
    }

    private static readonly System.Collections.Generic.HashSet<IHealth> s_alreadyHit =
        new System.Collections.Generic.HashSet<IHealth>();

    private void DealDamage(Vector3 origin, Vector3 forward)
    {
        GameObject holder = TryGetComponent(out Item item) ? item.Holder : null;
        Vector3 hitPoint = origin + forward * m_stats.Reach;
        int count = Physics.OverlapSphereNonAlloc(hitPoint, m_hitRadius, s_hitBuffer);

        // A body with several colliders is one victim, and the swinger's own capsule is not a target.
        s_alreadyHit.Clear();
        for (int i = 0; i < count; i++)
        {
            Collider hit = s_hitBuffer[i];
            if (holder != null && hit.transform.root == holder.transform.root)
                continue;

            IHealth health = hit.GetComponentInParent<IHealth>();
            if (health == null || !s_alreadyHit.Add(health))
                continue;

            Damage.Apply(health, m_stats.Damage, gameObject, holder, hit.ClosestPoint(hitPoint),
                DamageKind.Melee);
        }
    }

    private void AlertNearbyListeners()
    {
        m_emitter.NoiseType = NoiseType.MeleeSwing;
        m_emitter.EmitNoise(m_stats.NoiseRadius, m_swingNoiseStrength);
    }
}
