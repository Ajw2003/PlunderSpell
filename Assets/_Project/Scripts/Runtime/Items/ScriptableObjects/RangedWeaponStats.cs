using UnityEngine;

/// <summary>Stats for a ranged weapon: one shot, then a long reload. See docs/plunderspell.md's
/// weapon table and docs/plans/GitIssues/Issue_39_Plan.md.</summary>
[CreateAssetMenu(fileName = "RangedWeaponStats", menuName = "Scriptable Objects/Ranged Weapon Stats")]
public class RangedWeaponStats : ScriptableObject
{
    [SerializeField] private GameObject m_projectilePrefab;

    [Tooltip("Metres per second the projectile leaves at.")]
    [SerializeField] private float m_projectileSpeed = 30f;

    [Tooltip("Damage dealt on a hit.")]
    [SerializeField] private float m_damage = 35f;

    [Tooltip("Seconds of reload after firing before the weapon can fire again.")]
    [SerializeField] private float m_reloadDuration = 3.5f;

    [Tooltip("Noise radius broadcast to the alarm system on every shot, in metres.")]
    [SerializeField] private float m_noiseRadius = 18f;

    [Range(0f, 1f)]
    [Tooltip("Noise strength (0..1) broadcast on every shot, before wall attenuation.")]
    [SerializeField] private float m_noiseStrength = 0.9f;

    public GameObject ProjectilePrefab => m_projectilePrefab;
    public float ProjectileSpeed => m_projectileSpeed;
    public float Damage => m_damage;
    public float ReloadDuration => m_reloadDuration;
    public float NoiseRadius => m_noiseRadius;
    public float NoiseStrength => m_noiseStrength;
}
