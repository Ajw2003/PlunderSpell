using Plunderspell.Core;
using UnityEngine;
using Theme = Plunderspell.UI.UITheme;

namespace Plunderspell.UI
{
    /// <summary>
    /// The pocket watch, as flat UI for now (#324, owner 2026-10-06; the 3D model is #283): hold <c>T</c> and a face
    /// shows a ring of portal-light that empties anticlockwise as the way home narrows, with one hand at what is
    /// left. In the last minute the ring turns madder and pulses. Replaces the always-on raid clock. It draws from
    /// <see cref="RaidHudPresenter.Model"/> and reads no other system.
    /// </summary>
    [RequireComponent(typeof(RaidHudPresenter))]
    public sealed class WatchView : MonoBehaviour
    {
        private const float k_referenceHeight = 1080f;
        private const int Ticks = 60;
        private const float Radius = 70f;
        private const float TickLength = 13f;
        private const float PulseSeconds = 2.4f;

        // Panel position in 1080p units: low on the left, where an off-hand would hold it.
        private static readonly Vector2 Centre = new Vector2(190f, 800f);

        private RaidHudPresenter _presenter;
        private float _scale = 1f;
        private GUIStyle _label;
        private Texture2D _white;
        private readonly string[] _minuteText = new string[61];

        private void Awake() => _presenter = GetComponent<RaidHudPresenter>();

        /// <summary>The ring's lit ticks (0..60) for what is left of a raid, rounded up so a started minute still shows.</summary>
        public static int LitTicks(float remaining, float total)
        {
            if (total <= 0f || remaining <= 0f)
                return 0;
            return Mathf.Clamp(Mathf.CeilToInt(remaining / total * Ticks), 0, Ticks);
        }

        /// <summary>What the caption under the face says: whole minutes left, or that it is under one.</summary>
        public string CaptionFor(float remaining)
        {
            if (remaining <= 0f)
                return "the way is shut";
            if (remaining <= 60f)
                return "under a minute";
            int minutes = Mathf.Min(60, Mathf.CeilToInt(remaining / 60f));
            return _minuteText[minutes] ??= minutes + " min";
        }

        private void OnGUI()
        {
            if (_presenter == null || Event.current.type != EventType.Repaint)
                return;
            RaidHudModel model = _presenter.Model;
            if (!model.WatchUp || model.State != GameState.Playing)
                return;

            float scale = Screen.height / k_referenceHeight;
            if (scale <= 0f)
                return;

            EnsureStyles();
            _scale = scale;
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            Draw(model);
            GUI.matrix = previous;
        }

        private void Draw(RaidHudModel model)
        {
            // The panel: a hairline round the surface, as the spell list had.
            var panel = new Rect(Centre.x - 100f, Centre.y - 100f, 200f, 222f);
            Fill(panel, Theme.Line);
            Color surface = Theme.Surface;
            surface.a = 0.94f;
            Fill(new Rect(panel.x + 1f, panel.y + 1f, panel.width - 2f, panel.height - 2f), surface);

            int lit = LitTicks(model.TimeRemaining, model.TimeTotal);
            bool critical = model.TimerIsCritical;
            float pulse = critical ? 0.775f + 0.225f * Mathf.Cos(Time.time * (Mathf.PI * 2f / PulseSeconds)) : 1f;
            Color litColour = critical ? Theme.Danger : Theme.Voice;
            litColour.a = pulse;

            for (int i = 0; i < Ticks; i++)
                DrawTick(i, i < lit ? litColour : Theme.Line);

            DrawHand(lit, critical ? Theme.Danger : Theme.Text);

            string caption = CaptionFor(model.TimeRemaining);
            Vector2 size = _label.CalcSize(new GUIContent(caption));
            GUI.contentColor = critical ? Theme.Danger : Theme.TextDim;
            GUI.Label(new Rect(Centre.x - size.x * 0.5f, panel.yMax - 30f, size.x + 2f, size.y), caption, _label);
            GUI.contentColor = Color.white;
        }

        // Tick i sits at i * 6 degrees clockwise from twelve o'clock, so the lit run 0..n-1 shortens anticlockwise.
        private void DrawTick(int index, Color colour)
        {
            Matrix4x4 saved = GUI.matrix;
            GUIUtility.RotateAroundPivot(index * (360f / Ticks), Centre * _scale); // the pivot is in screen pixels, before the layout scale
            Fill(new Rect(Centre.x - 1.5f, Centre.y - Radius, 3f, TickLength), colour);
            GUI.matrix = saved;
        }

        private void DrawHand(int lit, Color colour)
        {
            Matrix4x4 saved = GUI.matrix;
            GUIUtility.RotateAroundPivot(lit * (360f / Ticks), Centre * _scale);
            Fill(new Rect(Centre.x - 1f, Centre.y - (Radius - TickLength - 6f), 2f, Radius - TickLength - 6f), colour);
            GUI.matrix = saved;
            Fill(new Rect(Centre.x - 3f, Centre.y - 3f, 6f, 6f), colour);
        }

        private void Fill(Rect rect, Color colour)
        {
            GUI.color = colour;
            GUI.DrawTexture(rect, _white);
            GUI.color = Color.white;
        }

        private void EnsureStyles()
        {
            if (_label != null)
                return;
            _label = new GUIStyle { font = UIFonts.Mono, fontSize = 14, alignment = TextAnchor.MiddleLeft, richText = false };
            _label.normal.textColor = Color.white;
            _white = UITextures.White;
        }
    }
}
