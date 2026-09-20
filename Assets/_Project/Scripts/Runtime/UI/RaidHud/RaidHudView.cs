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

        [Tooltip("Show the push-to-cast key and the spell list.")]
        [SerializeField] private bool _showSpellbook = true;

        private const float k_crosshairSize = 9f;
        private const float k_crosshairThickness = 2f;

        private static readonly Color k_crosshairIdleColour = new Color(1f, 1f, 1f, 0.75f);
        private static readonly Color k_crosshairActiveColour = new Color(1f, 0.85f, 0.35f, 1f);

        // Mirrors MockVoiceInputService.keybindMap; the HUD only needs the words, not the service.
        private static readonly string[] Spellbook =
        {
            "IGNIS", "FRANGO", "LEVO", "AURUM VOCO",
            "TONITRUS", "SOMNUS", "CADAVER SURGE", "PORTA",
        };

        private const float k_enemyBarWidth = 44f;
        private const float k_enemyBarHeight = 5f;

        private static readonly Color k_lowHealthColour = new Color(0.85f, 0.15f, 0.15f);
        private static readonly Color k_fullHealthColour = new Color(0.25f, 0.85f, 0.35f);

        private RogueAi.Voice.PushToCastController _pushToCast;
        private RaidHudPresenter _presenter;
        private Camera _camera;
        private GUIStyle _label;
        private GUIStyle _big;
        private Texture2D _barBackground;
        private Texture2D _barFill;

        private void Awake()
        {
            _presenter = GetComponent<RaidHudPresenter>();
            _pushToCast = FindFirstObjectByType<RogueAi.Voice.PushToCastController>();
        }

        private void OnGUI()
        {
            if (!_visible || _presenter == null)
                return;

            // IMGUI draws over the uGUI canvas, so an always-on HUD hides the main menu and the
            // lair behind it. Only draw once the player is actually in the world.
            if (Plunderspell.Core.GameServices.GameState == null)
                return;

            Plunderspell.Core.GameState state = Plunderspell.Core.GameServices.GameState.CurrentState;
            if (state != Plunderspell.Core.GameState.Playing &&
                state != Plunderspell.Core.GameState.Paused &&
                state != Plunderspell.Core.GameState.Inventory)
                return;

            EnsureStyles();
            RaidHudModel model = _presenter.Build();

            const float pad = 12f;
            float width = Mathf.Min(360f, Screen.width - pad * 2f);

            // Top-left: phase, clock, alarm, health.
            GUILayout.BeginArea(new Rect(pad, pad, width, 240f));
            GUILayout.Label(PhaseLine(model.Phase), _label);

            _big.normal.textColor = model.TimerIsCritical ? Color.red : Color.white;
            GUILayout.Label(model.TimerText, _big);

            GUILayout.Label(model.AlarmText, _label);
            DrawBar(GUILayoutUtility.GetRect(width - pad, 10f), model.AlarmFill, AlarmColour(model.Alarm));

            GUILayout.Label(
                $"Health {Mathf.CeilToInt(model.PlayerCurrentHealth)}/{Mathf.CeilToInt(model.PlayerMaxHealth)}",
                _label);
            DrawBar(GUILayoutUtility.GetRect(width - pad, 10f), model.PlayerHealthFill,
                Color.Lerp(k_lowHealthColour, k_fullHealthColour, model.PlayerHealthFill));
            GUILayout.EndArea();

            DrawEnemyHealthBars(model);

            // Top-right: the money, and the haul standing on the pad. The haul sits with the debt
            // rather than near the crosshair because it is the number the debt is measured against.
            GUILayout.BeginArea(new Rect(Screen.width - width - pad, pad, width, 80f));
            GUILayout.Label($"Debt {model.Debt:0}   Banked {model.BankedGold:0}", _label);

            GUIStyle haulStyle = _label;
            if (model.HaulPieces > 0)
            {
                haulStyle = new GUIStyle(_label);
                haulStyle.normal.textColor = new Color(1f, 0.85f, 0.35f);
            }
            GUILayout.Label(model.HaulText, haulStyle);
            GUILayout.EndArea();

            DrawCrosshair(state, model.HasInteractTarget);

            // Centre: the interact prompt, just under the crosshair so the eye never has to leave it.
            if (!string.IsNullOrEmpty(model.InteractPrompt))
            {
                var promptRect = new Rect(Screen.width * 0.5f - 250f,
                    Screen.height * 0.5f + k_crosshairSize, 500f, 24f);
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

            DrawSpellbook();

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

        /// <summary>
        /// The centre-screen crosshair: a cross while idle, an open bracket over something
        /// interactable, and nothing at all outside play. Generated in code — see docs/Decisions.md,
        /// "The crosshair is IMGUI, and therefore invisible to the screenshot test".
        /// </summary>
        private void DrawCrosshair(Plunderspell.Core.GameState state, bool hasTarget)
        {
            if (state != Plunderspell.Core.GameState.Playing)
                return;

            float centreX = Screen.width * 0.5f;
            float centreY = Screen.height * 0.5f;

            Color previous = GUI.color;
            GUI.color = hasTarget ? k_crosshairActiveColour : k_crosshairIdleColour;

            if (hasTarget)
            {
                // Four ticks pulled back off centre — an open bracket around what you are looking at.
                float inner = k_crosshairSize * 0.6f;
                float outer = k_crosshairSize * 1.5f;
                DrawLine(centreX - outer, centreY - k_crosshairThickness * 0.5f, outer - inner, k_crosshairThickness);
                DrawLine(centreX + inner, centreY - k_crosshairThickness * 0.5f, outer - inner, k_crosshairThickness);
                DrawLine(centreX - k_crosshairThickness * 0.5f, centreY - outer, k_crosshairThickness, outer - inner);
                DrawLine(centreX - k_crosshairThickness * 0.5f, centreY + inner, k_crosshairThickness, outer - inner);
            }
            else
            {
                DrawLine(centreX - k_crosshairSize, centreY - k_crosshairThickness * 0.5f,
                    k_crosshairSize * 2f, k_crosshairThickness);
                DrawLine(centreX - k_crosshairThickness * 0.5f, centreY - k_crosshairSize,
                    k_crosshairThickness, k_crosshairSize * 2f);
            }

            GUI.color = previous;
        }

        private void DrawLine(float x, float y, float width, float height) =>
            GUI.DrawTexture(new Rect(x, y, width, height), _barFill);

        /// <summary>
        /// One small bar per living guard, projected from its world position (issue #14). See
        /// docs/Decisions.md, "Enemy health bars are IMGUI, projected from world space".
        /// </summary>
        private void DrawEnemyHealthBars(RaidHudModel model)
        {
            if (_camera == null)
                _camera = Camera.main;
            if (_camera == null)
                return;

            foreach (RaidHudModel.EnemyHealthBar bar in model.EnemyHealthBars)
            {
                Vector3 screen = _camera.WorldToScreenPoint(bar.WorldPosition);
                if (screen.z <= 0f)
                    continue; // behind the camera

                // WorldToScreenPoint is bottom-left origin with y up; IMGUI is top-left with y down.
                float x = screen.x - k_enemyBarWidth * 0.5f;
                float y = Screen.height - screen.y - k_enemyBarHeight * 0.5f;

                if (x < -k_enemyBarWidth || x > Screen.width || y < -k_enemyBarHeight || y > Screen.height)
                    continue;

                DrawBar(new Rect(x, y, k_enemyBarWidth, k_enemyBarHeight), bar.HealthFraction,
                    Color.Lerp(k_lowHealthColour, k_fullHealthColour, bar.HealthFraction));
            }
        }

        /// <summary>
        /// The casting controls. Push-to-cast is not guessable: you hold a key to open the mic and
        /// then say (here, press) the word, so without this the spells are invisible.
        /// </summary>
        private void DrawSpellbook()
        {
            if (!_showSpellbook)
                return;

            const float width = 210f;
            const float lineHeight = 18f;
            float height = lineHeight * (Spellbook.Length + 2) + 12f;
            float x = Screen.width - width - 12f;
            float y = Screen.height - height - 12f;

            GUI.DrawTexture(new Rect(x, y, width, height), _barBackground);

            var style = new GUIStyle(_label) { alignment = TextAnchor.MiddleLeft };
            bool casting = _pushToCast != null && _pushToCast.IsCasting;

            style.normal.textColor = casting ? new Color(0.45f, 0.95f, 0.55f) : Color.white;
            GUI.Label(new Rect(x + 8f, y + 6f, width - 16f, lineHeight),
                casting ? "CASTING - press a number" : "Hold V to cast", style);

            style.normal.textColor = new Color(0.75f, 0.75f, 0.75f);
            GUI.Label(new Rect(x + 8f, y + 6f + lineHeight, width - 16f, lineHeight),
                "Shift = shout   Ctrl = whisper", style);

            for (int i = 0; i < Spellbook.Length; i++)
            {
                GUI.Label(new Rect(x + 8f, y + 6f + lineHeight * (i + 2), width - 16f, lineHeight),
                    $"{i + 1}   {Spellbook[i]}", style);
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
