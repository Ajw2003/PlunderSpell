using UnityEngine;

/// <summary>
/// Recolours a bolt at spawn, so one projectile prefab can read as fire, frost or lightning without
/// a prefab per spell. Uses a property block, which never writes to the shared material asset.
/// </summary>
public class ProjectileTint : MonoBehaviour
{
    private static readonly int s_baseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int s_color = Shader.PropertyToID("_Color");
    private static readonly int s_emissionColor = Shader.PropertyToID("_EmissionColor");

    [Tooltip("How much brighter the emissive is than the base colour.")]
    [SerializeField] private float m_emissionBoost = 2.5f;

    private MaterialPropertyBlock m_block;

    /// <summary>Recolours the bolt and its glow.</summary>
    public void Apply(Color colour)
    {
        m_block ??= new MaterialPropertyBlock();

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            renderer.GetPropertyBlock(m_block);
            // Both names are set because URP/Lit reads _BaseColor and the built-in Standard shader
            // reads _Color; which one this project ends up on is still unsettled.
            m_block.SetColor(s_baseColor, colour);
            m_block.SetColor(s_color, colour);
            m_block.SetColor(s_emissionColor, colour * m_emissionBoost);
            renderer.SetPropertyBlock(m_block);
        }

        foreach (Light light in GetComponentsInChildren<Light>(true))
        {
            light.color = colour;
        }
    }
}
