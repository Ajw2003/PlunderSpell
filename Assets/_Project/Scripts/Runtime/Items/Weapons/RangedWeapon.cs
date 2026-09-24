using Interfaces;
using RogueAi.Acoustics;
using UnityEngine;

/// <summary>One bolt, then a long reload. See docs/Decisions.md, "A ranged shot spawns ahead of the
/// wielder instead of tracking their colliders".</summary>
[RequireComponent(typeof(Item))]
[RequireComponent(typeof(AcousticEmitter))]
public class RangedWeapon : MonoBehaviour
{
    // Metres in front of the fire origin the shot spawns at, clear of the wielder's own collider.
    private const float k_muzzleOffset = 0.8f;

    [SerializeField] private RangedWeaponStats m_stats;

    private AcousticEmitter m_emitter;
    private bool m_isLoaded = true;
    private float m_reloadStartTime;

    /// <summary>True while the player is holding this weapon up to fire.</summary>
    public bool IsAiming { get; private set; }

    public bool IsLoaded => m_isLoaded;

    /// <summary>0 while loaded/ready, ramping to 1 across the reload.</summary>
    public float ReloadProgress01 => m_isLoaded || m_stats == null
        ? 0f
        : Mathf.Clamp01((Time.time - m_reloadStartTime) / m_stats.ReloadDuration);

    private void Awake()
    {
        m_emitter = GetComponent<AcousticEmitter>();
    }

    public void SetAiming(bool aiming)
    {
        IsAiming = aiming;
    }

    /// <summary>Fires from <paramref name="origin"/> toward <paramref name="direction"/>. Requires
    /// <see cref="IsAiming"/> and a loaded weapon; no-ops (returns false) otherwise.</summary>
    public bool TryFire(Vector3 origin, Vector3 direction)
    {
        if (m_stats == null || !m_isLoaded || !IsAiming)
            return false;

        m_isLoaded = false;
        m_reloadStartTime = Time.time;

        SpawnProjectile(origin, direction);
        AlertNearbyListeners();
        return true;
    }

    private void Update()
    {
        if (!m_isLoaded && ReloadProgress01 >= 1f)
        {
            m_isLoaded = true;
        }
    }

    /// <summary>
    /// Raised on the firing machine after a real shot: (weapon, where the shot left the muzzle, its
    /// direction). RogueAi.Net shows the same shot on every other machine with
    /// <see cref="SpawnCosmeticShot"/>.
    /// </summary>
    public static event System.Action<RangedWeapon, Vector3, Vector3> Fired;

    /// <summary>
    /// A copy of another machine's shot: the same projectile flying the same way, carrying no damage.
    /// The real shot already hit, or missed, on the machine that fired it.
    /// </summary>
    public void SpawnCosmeticShot(Vector3 spawnPoint, Vector3 direction)
    {
        if (m_stats == null || m_stats.ProjectilePrefab == null)
            return;
        GameObject shot = Instantiate(m_stats.ProjectilePrefab, spawnPoint, Quaternion.LookRotation(direction));
        if (shot.TryGetComponent(out NetworkedProjectile projectile))
            projectile.Damage = 0;
        if (shot.TryGetComponent(out Rigidbody body))
            body.linearVelocity = direction * m_stats.ProjectileSpeed;
    }

    private void SpawnProjectile(Vector3 origin, Vector3 direction)
    {
        if (m_stats.ProjectilePrefab == null)
            return;

        // Spawning at the muzzle rather than the eye clears the wielder's own capsule collider by
        // construction, the same fix already used for spell bursts (docs/Decisions.md, "A spell
        // burst is centred on where it lands, not where it starts") — no per-frame collision-ignore
        // bookkeeping needed.
        Vector3 spawnPoint = origin + direction * k_muzzleOffset;
        GameObject shot = Instantiate(m_stats.ProjectilePrefab, spawnPoint, Quaternion.LookRotation(direction));

        if (shot.TryGetComponent(out NetworkedProjectile projectile))
        {
            projectile.Damage = (int)m_stats.Damage;
            projectile.Instigator = TryGetComponent(out Item item) ? item.Holder : null;
        }

        if (shot.TryGetComponent(out Rigidbody body))
            body.linearVelocity = direction * m_stats.ProjectileSpeed;

        Fired?.Invoke(this, spawnPoint, direction);
    }

    private void AlertNearbyListeners()
    {
        m_emitter.NoiseType = NoiseType.Gunshot;
        m_emitter.EmitNoise(m_stats.NoiseRadius, m_stats.NoiseStrength);
    }
}
