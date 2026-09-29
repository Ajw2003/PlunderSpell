using UnityEngine;

namespace Plunderspell.UI
{
    /// <summary>Centre-screen crosshair, drawn with IMGUI. See docs/4-systems/combat-bench.md, "The
    /// bench has its own crosshair".</summary>
    public class CrosshairView : MonoBehaviour
    {
        [Tooltip("Hide the crosshair (e.g. for screenshots).")]
        [SerializeField] private bool _visible = true;

        // The mockup's .rd-cross, in 1080p units: four ticks 8 long and 2 thick, pulled 3 off centre,
        // and over something usable a 34px ring in one hairline.
        private const float k_referenceHeight = 1080f;
        private const float k_tickInner = 3f;
        private const float k_tickLength = 8f;
        private const float k_thickness = 2f;
        private const float k_ringSize = 34f;

        // The ring is drawn from a texture twice its size so a 1px line stays crisp when it is scaled down.
        private const int k_ringTexture = 68;
        private const float k_ringTextureThickness = 2f;

        private static readonly Color k_idleColour = new Color(UITheme.Text.r, UITheme.Text.g, UITheme.Text.b, 0.8f);

        private Texture2D _fill;
        private Texture2D _ring;

        /// <summary>True while the crosshair should show its "over something" look. A scene with
        /// nothing to interact with (the bench) just never sets this and stays idle.</summary>
        public bool HasTarget { get; set; }

        private void OnGUI()
        {
            if (!_visible || Plunderspell.Core.GameServices.GameState == null)
                return;
            if (Plunderspell.Core.GameServices.GameState.CurrentState != Plunderspell.Core.GameState.Playing)
                return;

            float scale = Screen.height / k_referenceHeight;
            if (scale <= 0f)
                return;

            EnsureTextures();

            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            Draw(HasTarget, Screen.width * 0.5f / scale, k_referenceHeight * 0.5f);
            GUI.matrix = previousMatrix;
        }

        private void Draw(bool hasTarget, float centreX, float centreY)
        {
            Color previous = GUI.color;
            GUI.color = hasTarget ? UITheme.Interactive : k_idleColour;

            float half = k_thickness * 0.5f;
            DrawLine(centreX - k_tickInner - k_tickLength, centreY - half, k_tickLength, k_thickness);
            DrawLine(centreX + k_tickInner, centreY - half, k_tickLength, k_thickness);
            DrawLine(centreX - half, centreY - k_tickInner - k_tickLength, k_thickness, k_tickLength);
            DrawLine(centreX - half, centreY + k_tickInner, k_thickness, k_tickLength);

            if (hasTarget)
            {
                float ring = k_ringSize * 0.5f;
                GUI.DrawTexture(new Rect(centreX - ring, centreY - ring, k_ringSize, k_ringSize), _ring);
            }

            GUI.color = previous;
        }

        private void DrawLine(float x, float y, float width, float height) =>
            GUI.DrawTexture(new Rect(x, y, width, height), _fill);

        private void EnsureTextures()
        {
            if (_fill == null)
                _fill = UITextures.White;
            if (_ring == null)
                _ring = UITextures.Ring(k_ringTexture, k_ringTextureThickness);
        }
    }
}
