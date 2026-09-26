using RogueAi.Alarm;
using RogueAi.Raid;
using UnityEngine;

namespace RogueAi.UI
{
    /// <summary>
    /// Draws the HUD with IMGUI.
    ///
    /// This is deliberately not a uGUI canvas. The game needs to be playable and legible now, from a
    /// scene built by code, without anyone authoring prefabs first — and an IMGUI pass costs one
    /// component and no assets. When the art pass arrives, swap this for a canvas that reads the same
    /// <see cref="RaidHudModel"/>; nothing above it has to change.
    /// </summary>
    [RequireComponent(typeof(RaidHudPresenter))]
    public class RaidHudView : MonoBehaviour
    {
        [Tooltip("Hide the HUD (e.g. for screenshots).")]
        [SerializeField] private bool _visible = true;

        private RaidHudPresenter _presenter;
        private GUIStyle _label;
        private GUIStyle _big;
        private Texture2D _barBackground;
        private Texture2D _barFill;

        private void Awake() => _presenter = GetComponent<RaidHudPresenter>();

        private void OnGUI()
        {
            if (!_visible || _presenter == null)
                return;

            EnsureStyles();
            RaidHudModel model = _presenter.Build();

            const float pad = 12f;
            float width = Mathf.Min(360f, Screen.width - pad * 2f);

            // Top-left: phase, clock, alarm.
            GUILayout.BeginArea(new Rect(pad, pad, width, 200f));
            GUILayout.Label(PhaseLine(model.Phase), _label);

            _big.normal.textColor = model.TimerIsCritical ? Color.red : Color.white;
            GUILayout.Label(model.TimerText, _big);

            GUILayout.Label(model.AlarmText, _label);
            DrawBar(GUILayoutUtility.GetRect(width - pad, 10f), model.AlarmFill, AlarmColour(model.Alarm));
            GUILayout.EndArea();

            // Top-right: the money.
            GUILayout.BeginArea(new Rect(Screen.width - width - pad, pad, width, 80f));
            GUILayout.Label($"Debt {model.Debt:0}   Banked {model.BankedGold:0}", _label);
            GUILayout.EndArea();

            // Centre: the interact prompt and what you are carrying.
            if (!string.IsNullOrEmpty(model.InteractPrompt))
            {
                var promptRect = new Rect(Screen.width * 0.5f - 150f, Screen.height * 0.55f, 300f, 24f);
                GUI.Label(promptRect, model.InteractPrompt, Centered(_label));
            }

            if (!string.IsNullOrEmpty(model.CarriedLootName))
            {
                string carrying = model.CarriedNeedsTwo
                    ? $"Carrying {model.CarriedLootName} (two-person)"
                    : $"Carrying {model.CarriedLootName}";
                GUI.Label(new Rect(Screen.width * 0.5f - 150f, Screen.height - 60f, 300f, 24f),
                    carrying, Centered(_label));
            }

            // Bottom-centre: the last cast, so a misfire is unmissable.
            if (!string.IsNullOrEmpty(model.LastCastLine))
            {
                GUIStyle style = Centered(_label);
                style.normal.textColor = model.LastCastLine.StartsWith("MISFIRE")
                    ? new Color(1f, 0.4f, 0.2f)
                    : Color.white;
                GUI.Label(new Rect(Screen.width * 0.5f - 200f, Screen.height - 32f, 400f, 24f),
                    model.LastCastLine, style);
            }
        }

        private static string PhaseLine(RaidPhase phase)
        {
            switch (phase)
            {
                case RaidPhase.InLair: return "The Lair";
                case RaidPhase.Generating: return "Building the castle…";
                case RaidPhase.Raiding: return "Raiding";
                case RaidPhase.Extracting: return "Extracting";
                case RaidPhase.Resolved: return "Raid over";
                default: return phase.ToString();
            }
        }

        private static Color AlarmColour(AlarmState state)
        {
            switch (state)
            {
                case AlarmState.Stirred: return new Color(0.95f, 0.85f, 0.2f);
                case AlarmState.Roused: return new Color(0.95f, 0.55f, 0.1f);
                case AlarmState.HueAndCry: return new Color(0.9f, 0.2f, 0.15f);
                default: return new Color(0.4f, 0.7f, 0.45f);
            }
        }

        private void DrawBar(Rect rect, float fill, Color colour)
        {
            GUI.DrawTexture(rect, _barBackground);

            Color previous = GUI.color;
            GUI.color = colour;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fill), rect.height), _barFill);
            GUI.color = previous;
        }

        private GUIStyle Centered(GUIStyle from) => new GUIStyle(from) { alignment = TextAnchor.MiddleCenter };

        private void EnsureStyles()
        {
            if (_label != null)
                return;

            _label = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            _label.normal.textColor = Color.white;

            _big = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold };
            _big.normal.textColor = Color.white;

            _barBackground = SolidTexture(new Color(0f, 0f, 0f, 0.5f));
            _barFill = SolidTexture(Color.white);
        }

        private static Texture2D SolidTexture(Color colour)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, colour);
            texture.Apply();
            return texture;
        }
    }
}
