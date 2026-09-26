using Plunderspell.Inventory;
using UnityEngine;

/// <summary>
/// Combat-specific stats for a melee weapon. Heft is read from <see cref="InventoryItem.Weight"/>
/// rather than duplicated here — see docs/Decisions.md, "Melee weight is read from InventoryItem".
/// </summary>
[CreateAssetMenu(fileName = "MeleeWeaponStats", menuName = "Scriptable Objects/Melee Weapon Stats")]
public class MeleeWeaponStats : ScriptableObject
{
    [SerializeField] private InventoryItem m_item;

    [Tooltip("How far in front of the swing origin an enemy can be struck, in metres.")]
    [SerializeField] private float m_reach = 1.5f;

    [Tooltip("Noise radius broadcast to the alarm system on every swing, in metres. Maps to the pitch's Noise column (none/low/mid/high/max).")]
    [SerializeField] private float m_noiseRadius = 3f;

    [Tooltip("Base seconds between swings, before the weapon's own weight slows it further.")]
    [SerializeField] private float m_baseSwingDuration = 0.35f;

    [Tooltip("Extra swing-duration seconds added per stone of the weapon's weight.")]
    [SerializeField] private float m_swingDurationPerWeight = 0.08f;

    [Tooltip("Damage dealt per stone of the weapon's weight.")]
    [SerializeField] private float m_damagePerWeight = 6f;

    public float Reach => m_reach;
    public float NoiseRadius => m_noiseRadius;
    public float Weight => m_item != null ? m_item.Weight : 0f;
    public float SwingDuration => m_baseSwingDuration + Weight * m_swingDurationPerWeight;
    public float Damage => Weight * m_damagePerWeight;
}
