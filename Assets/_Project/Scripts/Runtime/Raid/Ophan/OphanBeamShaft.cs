using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// The Ophan's beam drawn as a shaft of light (#460): an open tapered tube from its eye to the target, seen from both
    /// sides and added onto the scene. A flat camera-facing ribbon is end-on to anyone looking up at the Ophan, which is
    /// everyone it beams at, so it showed only as a sliver by the eye; a tube reads end-on as light closing in around the
    /// eye and from the side as a solid shaft. See docs/4-systems/fear.md, "Beam fixes".
    /// </summary>
    public sealed class OphanBeamShaft
    {
        private const int Sides = 24;
        // Rings along the shaft: the eye, where the fade starts, and the target.
        private const int Rings = 3;

        private readonly GameObject _go;
        private readonly Mesh _mesh;
        private readonly MeshRenderer _renderer;
        private readonly Vector3[] _vertices = new Vector3[Sides * Rings];
        private readonly Color[] _colours = new Color[Sides * Rings];

        private readonly bool _opaque;

        /// <param name="opaque">A solid unlit core instead of the soft additive shaft; it fades by tapering to a point, as unlit ignores vertex alpha.</param>
        public OphanBeamShaft(bool opaque = false)
        {
            _opaque = opaque;
            _go = new GameObject("OphanBeam");
            _mesh = new Mesh { name = "OphanBeamShaft" };
            _mesh.MarkDynamic();
            _mesh.vertices = _vertices;
            _mesh.colors = _colours;
            _mesh.triangles = Triangles();
            _go.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = _go.AddComponent<MeshRenderer>();
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            // The grab beam's additive look, drawn on both faces: the target looks up the inside of the shaft.
            Material material;
            Shader unlit = opaque ? Shader.Find("Universal Render Pipeline/Unlit") : null;
            if (unlit != null)
            {
                material = new Material(unlit);
            }
            else
            {
                if (opaque)
                    Debug.LogError("OphanBeamShaft: URP Unlit shader not found; the beam core falls back to the soft material.");
                material = new Material(Resources.Load<Material>("GrabBeam"));
            }
            material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            _renderer.sharedMaterial = material;
            _go.SetActive(false);
        }

        public bool Visible
        {
            get => _go.activeSelf;
            set => _go.SetActive(value);
        }

        /// <summary>
        /// Lays the shaft from <paramref name="eye"/> to <paramref name="end"/>, <paramref name="eyeWidth"/> across at the
        /// eye and <paramref name="endWidth"/> at the end, fading to nothing over its last <paramref name="fadeMetres"/>.
        /// </summary>
        public void Draw(Vector3 eye, Vector3 end, float eyeWidth, float endWidth, Color colour, float fadeMetres)
        {
            Vector3 along = end - eye;
            float length = along.magnitude;
            if (length < 0.01f)
                return;
            _go.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(along / length));

            if (_opaque && _renderer.sharedMaterial.HasProperty("_BaseColor"))
                _renderer.sharedMaterial.SetColor("_BaseColor", colour);
            float fadeStart = 1f - Mathf.Clamp01(fadeMetres / length);
            float[] at = { 0f, fadeStart, 1f };
            float[] alpha = { colour.a, colour.a, 0f };
            for (int ring = 0; ring < Rings; ring++)
            {
                float radius = Mathf.Lerp(eyeWidth, endWidth, at[ring]) * 0.5f;
                if (_opaque && ring == Rings - 1)
                    radius = 0f;
                var ringColour = new Color(colour.r, colour.g, colour.b, alpha[ring]);
                for (int side = 0; side < Sides; side++)
                {
                    float angle = side * Mathf.PI * 2f / Sides;
                    _vertices[ring * Sides + side] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, at[ring] * length);
                    _colours[ring * Sides + side] = ringColour;
                }
            }
            _mesh.vertices = _vertices;
            _mesh.colors = _colours;
            _mesh.RecalculateBounds();
        }

        public void Destroy()
        {
            Object.Destroy(_renderer.sharedMaterial);
            Object.Destroy(_mesh);
            Object.Destroy(_go);
        }

        private static int[] Triangles()
        {
            var triangles = new int[(Rings - 1) * Sides * 6];
            int t = 0;
            for (int ring = 0; ring < Rings - 1; ring++)
            {
                for (int side = 0; side < Sides; side++)
                {
                    int a = ring * Sides + side;
                    int b = ring * Sides + (side + 1) % Sides;
                    int c = a + Sides;
                    int d = b + Sides;
                    triangles[t++] = a; triangles[t++] = c; triangles[t++] = b;
                    triangles[t++] = b; triangles[t++] = c; triangles[t++] = d;
                }
            }
            return triangles;
        }
    }
}
