using Plunderspell.Lair;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// Draws the demanded piece across a whole page of the Lair ledger (#419). Once, on Start, the piece's model is
    /// photographed far below the world by a camera of its own, the photo is turned to ink by <see cref="InkSketch"/>,
    /// and the page (a fresh sheet of vellum laid over the book's ruled one) shows it. Any piece can be demanded without
    /// drawn art.
    /// Prototype: the model is set by LairRoomForge until the demand itself (#419) chooses the piece.
    /// </summary>
    public class LedgerDemandSketch : MonoBehaviour
    {
        [SerializeField] private GameObject _model;
        [SerializeField] private Renderer _page;

        // The page is 0.33 x 0.47 m, so the drawing keeps that shape.
        private const int Width = 512;
        private const int Height = 730;
        private static readonly Color32 Backdrop = new Color32(255, 0, 255, 255);
        private static readonly Vector3 Studio = new Vector3(0f, -2000f, 0f);

        public Texture2D Drawing { get; private set; }

        private void Start() => Draw();

        private void OnDestroy()
        {
            if (Drawing != null)
                Destroy(Drawing);
        }

        public void Draw()
        {
            if (_model == null || _page == null)
            {
                Debug.LogWarning("[Ledger] No demanded piece to draw: the sketch page has no model or no page.", this);
                return;
            }

            // Stood on end along its longest side, since the page is tall, then turned a little: a three-quarter view.
            GameObject piece = Instantiate(_model, Studio, Quaternion.identity);
            piece.transform.rotation = Quaternion.Euler(0f, 35f, 0f) * Upright(BoundsOf(piece).size);
            Bounds bounds = BoundsOf(piece);

            var rig = new GameObject("LedgerSketchCamera");
            rig.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
            float depth = bounds.extents.magnitude;
            rig.transform.position = bounds.center - rig.transform.forward * depth * 2f;
            var camera = rig.AddComponent<Camera>();
            camera.enabled = false; // rendered once, by hand
            camera.cameraType = CameraType.Preview; // keeps the night fog, which draws only for game and scene views, off the photo
            camera.orthographic = true; // a draughtsman's view, without perspective
            camera.aspect = (float)Width / Height;
            camera.orthographicSize = OrthographicSize(bounds, rig.transform, camera.aspect) * 1.04f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = depth * 4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Backdrop;

            // A lamp from the upper left, so the hatching falls on the far side; on only for this one render.
            var lamp = new GameObject("LedgerSketchLamp").AddComponent<Light>();
            lamp.type = LightType.Directional;
            lamp.intensity = 1.6f;
            lamp.transform.rotation = Quaternion.LookRotation(Quaternion.Euler(40f, 30f, 0f) * rig.transform.forward);

            RenderTexture target = RenderTexture.GetTemporary(Width, Height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            camera.Render();
            lamp.enabled = false;

            RenderTexture before = RenderTexture.active;
            RenderTexture.active = target;
            var photo = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            photo.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            RenderTexture.active = before;
            Color32[] pixels = photo.GetPixels32();

            camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(target);
            Destroy(photo);
            Destroy(lamp.gameObject);
            Destroy(rig);
            Destroy(piece);

            Drawing = new Texture2D(Width, Height, TextureFormat.RGBA32, true)
            {
                name = "LedgerDemandSketch", wrapMode = TextureWrapMode.Clamp, anisoLevel = 4,
            };
            Drawing.SetPixels32(InkSketch.Draw(pixels, Width, Height, Backdrop));
            Drawing.Apply(true, false); // stays readable: the raised and dropped pages are cut from it (#458)
            _page.material.mainTexture = Drawing;
        }

        /// <summary>The turn that brings the longest side of a box of this size upright.</summary>
        private static Quaternion Upright(Vector3 size)
        {
            if (size.x >= size.y && size.x >= size.z)
                return Quaternion.Euler(0f, 0f, 90f);
            if (size.z >= size.y)
                return Quaternion.Euler(-90f, 0f, 0f);
            return Quaternion.identity;
        }

        /// <summary>Half the view's height that just holds the bounds' corners, seen from the camera's angle.</summary>
        private static float OrthographicSize(Bounds bounds, Transform view, float aspect)
        {
            float halfWidth = 0f, halfHeight = 0f;
            for (int corner = 0; corner < 8; corner++)
            {
                var offset = Vector3.Scale(bounds.extents, new Vector3(corner & 1, (corner >> 1) & 1, (corner >> 2) & 1) * 2f - Vector3.one);
                Vector3 seen = view.InverseTransformDirection(offset);
                halfWidth = Mathf.Max(halfWidth, Mathf.Abs(seen.x));
                halfHeight = Mathf.Max(halfHeight, Mathf.Abs(seen.y));
            }
            return Mathf.Max(halfHeight, halfWidth / aspect);
        }

        private static Bounds BoundsOf(GameObject piece)
        {
            Renderer[] parts = piece.GetComponentsInChildren<Renderer>();
            if (parts.Length == 0)
                return new Bounds(piece.transform.position, Vector3.one * 0.3f);
            Bounds bounds = parts[0].bounds;
            foreach (Renderer part in parts)
                bounds.Encapsulate(part.bounds);
            return bounds;
        }
    }
}
