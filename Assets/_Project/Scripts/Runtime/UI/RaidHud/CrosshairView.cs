using UnityEngine;

namespace Plunderspell.UI
{
    /// <summary>Centre-screen crosshair, drawn with IMGUI. See docs/4-systems/combat-bench.md, "The
    /// bench has its own crosshair".</summary>
    public class CrosshairView : MonoBehaviour
    {
        [Tooltip("Hide the crosshair (e.g. for screenshots).")]
        [SerializeField] private bool _visible = true;

        private const float k_size = 9f;
        private const float k_thickness = 2f;

        private static readonly Color k_idleColour = new Color(1f, 1f, 1f, 0.75f);
        private static readonly Color k_activeColour = new Color(1f, 0.85f, 0.35f, 1f);

        private Texture2D _fill;

        /// <summary>True while the crosshair should show its "over something" look. A scene with
        /// nothing to interact with (the bench) just never sets this and stays idle.</summary>
        public bool HasTarget { get; set; }

        private void OnGUI()
        {
            if (!_visible || Plunderspell.Core.GameServices.GameState == null)
                return;
            if (Plunderspell.Core.GameServices.GameState.CurrentState != Plunderspell.Core.GameState.Playing)
                return;

            EnsureTexture();
            Draw(HasTarget);
        }

        private void Draw(bool hasTarget)
        {
            float centreX = Screen.width * 0.5f;
            float centreY = Screen.height * 0.5f;

            Color previous = GUI.color;
            GUI.color = hasTarget ? k_activeColour : k_idleColour;

            if (hasTarget)
            {
                // Four ticks pulled back off centre: an open bracket around what you are looking at.
                float inner = k_size * 0.6f;
                float outer = k_size * 1.5f;
                DrawLine(centreX - outer, centreY - k_thickness * 0.5f, outer - inner, k_thickness);
                DrawLine(centreX + inner, centreY - k_thickness * 0.5f, outer - inner, k_thickness);
                DrawLine(centreX - k_thickness * 0.5f, centreY - outer, k_thickness, outer - inner);
                DrawLine(centreX - k_thickness * 0.5f, centreY + inner, k_thickness, outer - inner);
            }
            else
            {
                DrawLine(centreX - k_size, centreY - k_thickness * 0.5f, k_size * 2f, k_thickness);
                DrawLine(centreX - k_thickness * 0.5f, centreY - k_size, k_thickness, k_size * 2f);
            }

            GUI.color = previous;
        }

        private void DrawLine(float x, float y, float width, float height) =>
            GUI.DrawTexture(new Rect(x, y, width, height), _fill);

        private void EnsureTexture()
        {
            if (_fill != null)
                return;

            _fill = new Texture2D(1, 1);
            _fill.SetPixel(0, 0, Color.white);
            _fill.Apply();
        }
    }
}
