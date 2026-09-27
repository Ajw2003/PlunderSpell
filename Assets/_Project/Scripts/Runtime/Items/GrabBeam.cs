using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The line from a player's hand to what they are holding, bent through where they aim, coloured
/// by <see cref="Item.Load"/> (#144). How it is drawn and synced: docs/4-systems/damage.md, "Weight".
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class GrabBeam : MonoBehaviour
{
    /// <summary>Where the beam's material is loaded from (Resources, so it ships in builds).</summary>
    public const string MaterialResourcePath = "GrabBeam";

    private const int k_segments = 24;

    private static readonly Color s_easy = new Color(0.78f, 0.68f, 1f);    // arcane violet, as the portal
    private static readonly Color s_straining = new Color(1f, 0.78f, 0.34f); // gold
    private static readonly Color s_limit = new Color(1f, 0.42f, 0.14f);     // hot orange
    private static readonly Color s_dragging = new Color(1f, 0.16f, 0.08f);  // red

    private static Material s_material;

    private LineRenderer _line;
    private readonly Vector3[] _points = new Vector3[k_segments + 1];
    private readonly GradientColorKey[] _colourKeys = new GradientColorKey[2];
    private readonly GradientAlphaKey[] _alphaKeys = new GradientAlphaKey[2];
    private Gradient _gradient;

    /// <summary>Makes a beam object, hidden until <see cref="Show"/> is called.</summary>
    public static GrabBeam Create(string name)
    {
        var go = new GameObject(name, typeof(LineRenderer), typeof(GrabBeam));
        return go.GetComponent<GrabBeam>();
    }

    private void Awake()
    {
        _line = GetComponent<LineRenderer>();
        _line.useWorldSpace = true;
        _line.positionCount = _points.Length;
        _line.numCapVertices = 4;
        _line.numCornerVertices = 2;
        _line.alignment = LineAlignment.View;
        _line.shadowCastingMode = ShadowCastingMode.Off;
        _line.receiveShadows = false;
        _line.sharedMaterial = BeamMaterial();
        _gradient = new Gradient();
        _line.enabled = false;
    }

    /// <summary>
    /// Draws the beam from <paramref name="hand"/> to <paramref name="held"/>, bent through
    /// <paramref name="aim"/>, the point the player is pulling the item toward.
    /// </summary>
    public void Show(Vector3 hand, Vector3 aim, Vector3 held, float load)
    {
        if (_line == null)
            return;

        // Strain is how far the item is from where it is wanted; the beam trembles with it.
        float strain = Mathf.Clamp01(Vector3.Distance(aim, held) / 1.5f);
        Vector3 along = held - hand;
        Vector3 side = Vector3.Cross(along, Vector3.up);
        side = side.sqrMagnitude > 1e-6f ? side.normalized : Vector3.right;
        float tremble = 0.015f + 0.05f * strain + (load > 1f ? 0.03f : 0f);

        for (int i = 0; i <= k_segments; i++)
        {
            float t = i / (float)k_segments;
            float u = 1f - t;
            Vector3 point = u * u * hand + 2f * u * t * aim + t * t * held;
            point += side * (Mathf.Sin(Time.time * 17f + t * 9f) * tremble * Mathf.Sin(t * Mathf.PI));
            _points[i] = point;
        }
        _line.SetPositions(_points);

        Color colour = ColourFor(load);
        if (load > 1f)
            colour *= 0.75f + 0.25f * Mathf.PerlinNoise(Time.time * 12f, 0.3f);
        _colourKeys[0] = new GradientColorKey(colour, 0f);
        _colourKeys[1] = new GradientColorKey(colour, 1f);
        _alphaKeys[0] = new GradientAlphaKey(0.3f, 0f);
        _alphaKeys[1] = new GradientAlphaKey(0.9f, 1f);
        _gradient.SetKeys(_colourKeys, _alphaKeys);
        _line.colorGradient = _gradient;

        float width = Mathf.Lerp(0.02f, 0.045f, Mathf.Clamp01(load));
        _line.startWidth = width * 0.15f; // the hand end sits just in front of the camera
        _line.endWidth = width;
        _line.enabled = true;
    }

    public void Hide()
    {
        if (_line != null)
            _line.enabled = false;
    }

    public bool IsShown => _line != null && _line.enabled;

    /// <summary>The beam's colour for a load (see <see cref="Item.Load"/>): violet when easy, gold,
    /// then orange toward the limit, red past it. Pure, so it is testable.</summary>
    public static Color ColourFor(float load)
    {
        if (load > 1f)
            return s_dragging;
        if (load < 0.5f)
            return Color.Lerp(s_easy, s_straining, load / 0.5f);
        return Color.Lerp(s_straining, s_limit, (load - 0.5f) / 0.5f);
    }

    private static Material BeamMaterial()
    {
        if (s_material != null)
            return s_material;

        s_material = Resources.Load<Material>(MaterialResourcePath);
        if (s_material == null)
        {
            Debug.LogWarning($"[GrabBeam] No material at Resources/{MaterialResourcePath}; " +
                             "falling back to Sprites/Default, which will not glow.");
            s_material = new Material(Shader.Find("Sprites/Default"));
        }
        return s_material;
    }
}
