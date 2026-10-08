using Code.Scripts.EventSystems;
using System;
using UnityEngine;
using Theme = Plunderspell.UI.UITheme;

namespace Plunderspell.UI
{
    /// <summary>
    /// Draws the HUD with IMGUI. Since the diegetic raid UI (#315) it is only what the world does not yet say for itself:
    /// the prompt under the crosshair, the ranged weapon's state, the chant, and what was heard while casting. The clock is
    /// the watch (<see cref="WatchView"/>), the spell list the grimoire (<see cref="GrimoireView"/>), the alarm the castle's
    /// fires; debt and banked gold are on the Lair screen.
    ///
    /// This is deliberately not a uGUI canvas. The game needs to be playable and legible now, from a
    /// scene built by code, without anyone authoring prefabs first — and an IMGUI pass costs one
    /// component and no assets. When the art pass arrives, swap this for a canvas that reads the same
    /// <see cref="RaidHudModel"/>; nothing above it has to change.
    /// </summary>
    [RequireComponent(typeof(RaidHudPresenter))]
    [RequireComponent(typeof(CrosshairView))]
    public class RaidHudView : MonoBehaviour
    {
        [Tooltip("Hide the HUD (e.g. for screenshots).")]
        [SerializeField] private bool _visible = true;

        // Everything is laid out in 1080p units (the mockup's CSS pixels) and scaled to the screen.
        private const float k_referenceHeight = 1080f;

        private static readonly Color ShadowColour = new Color(0.04f, 0.03f, 0.02f, 0.85f);
        private static readonly string KeyboardCasting = "● " + Theme.Tracked("Keyboard casting · press 1–8", 14);
        private static readonly string ListeningOn = "● " + Theme.Tracked("Listening on", 14) + " ";

        private RaidHudPresenter _presenter;
        private CrosshairView _crosshair;

        // Styles are white so a colour can be chosen per draw (GUI.contentColor) without touching them.
        private GUIStyle _mono12;
        private GUIStyle _mono14;
        private GUIStyle _mono15;
        private GUIStyle _mono17;
        private GUIStyle _display26;
        private GUIStyle _italic28;
        private Texture2D _white;
        private readonly GUIContent _measure = new GUIContent();

        // Text that only changes when its number does, so a steady HUD makes no garbage.
        private string _promptSource;
        private bool _promptHasKey;
        private string _promptKey = string.Empty;
        private string _promptRest = string.Empty;
        private string _chantWord;
        private string _chantLine = string.Empty;
        private string _listenDevice;
        private string _listenLine = string.Empty;

        private void Awake()
        {
            // The HUD only draws labels and fills: no GUILayout, nothing clickable. Skipping the Layout
            // pass, and every event but Repaint, halves IMGUI's text work and its garbage (#245).
            useGUILayout = false;
            _presenter = GetComponent<RaidHudPresenter>();
            _crosshair = GetComponent<CrosshairView>();
            // The held-key panels live beside the HUD; a scene built before they existed gets them here.
            if (GetComponent<HudHoldKeys>() == null)
                gameObject.AddComponent<HudHoldKeys>();
            if (GetComponent<WatchView>() == null)
                gameObject.AddComponent<WatchView>();
            if (GetComponent<GrimoireView>() == null)
                gameObject.AddComponent<GrimoireView>();
        }

        // The last phrase the voice service produced, so a misheard word reads differently from a
        // dead microphone.
        private const float k_captionSeconds = 3.5f;
        private string _caption = string.Empty;
        private Color _captionColour = Color.white;
        private float _captionAt = float.NegativeInfinity;

        private void OnEnable()
        {
            EventManager.Instance?.Subscribe(this, (Plunderspell.Spells.PhraseResolved e) => OnPhrase(e.Report));
            EventManager.Instance?.Subscribe(this, (Plunderspell.Acoustics.ChatterResolved e) => OnChatter(e.Outcome));
        }

        private void OnDisable()
        {
            EventManager.Instance?.UnsubscribeFromAllEvents(this);
        }

        /// <summary>The caption for a line the player just said between casts, and its colour. Pure, for tests.</summary>
        public static string ChatterCaptionFor(Plunderspell.Acoustics.ChatterOutcome outcome, out Color colour)
        {
            string said = $"\"{outcome.Transcript.ToLowerInvariant()}\"";
            int n = outcome.GuardsWhoUnderstood;
            if (n <= 0)
            {
                colour = new Color(0.6f, 0.68f, 0.78f);
                return $"{said}  -  nobody heard";
            }
            colour = new Color(1f, 0.75f, 0.25f);
            return n == 1 ? $"{said}  -  overheard by a guard" : $"{said}  -  overheard by {n} guards";
        }

        private void OnChatter(Plunderspell.Acoustics.ChatterOutcome outcome)
        {
            _caption = ChatterCaptionFor(outcome, out _captionColour);
            _captionAt = Time.time;
        }

        /// <summary>The caption text for a phrase, and how it is coloured. Pure, for tests.</summary>
        public static string CaptionFor(Plunderspell.Spells.SpellCastingSystem.PhraseReport phrase, out Color colour)
        {
            string heard = $"\"{phrase.Heard.ToLowerInvariant()}\"";
            if (phrase.NotEnoughMana)
            {
                // Mana is the voice's colour, brightened so it reads as a caption and not as a dark bar.
                colour = Color.Lerp(Theme.VoiceLo, Theme.TextDim, 0.5f);
                return $"{heard}  ->  {phrase.Word}  -  not enough mana";
            }
            if (phrase.Fizzled)
            {
                colour = Theme.TextDim;
                return $"{heard}  -  fizzled, not a spell";
            }
            if (phrase.IsMisfire)
            {
                colour = Theme.Danger;
                return $"{heard}  ->  {phrase.Word}  -  MISFIRE";
            }
            colour = Theme.Voice;
            return $"{heard}  ->  {phrase.Word}  ({phrase.Volume})";
        }

        /// <summary>
        /// Splits "Press [E] to pick up Gold Death Mask" into the key ("E") and what it does ("pick up
        /// Gold Death Mask"), so the key can be drawn in a box. False, with <paramref name="rest"/> the
        /// whole prompt, when the text is not of that form. Pure, for tests.
        /// </summary>
        public static bool SplitPrompt(string prompt, out string key, out string rest)
        {
            const string opening = "Press [";
            key = string.Empty;
            rest = prompt;
            if (string.IsNullOrEmpty(prompt) || !prompt.StartsWith(opening, StringComparison.Ordinal))
                return false;

            int close = prompt.IndexOf(']', opening.Length);
            if (close <= opening.Length)
                return false;

            key = prompt.Substring(opening.Length, close - opening.Length);
            string after = prompt.Substring(close + 1).TrimStart();
            rest = after.StartsWith("to ", StringComparison.Ordinal) ? after.Substring(3) : after;
            return true;
        }

        private void OnPhrase(Plunderspell.Spells.SpellCastingSystem.PhraseReport phrase)
        {
            _caption = CaptionFor(phrase, out _captionColour);
            _captionAt = Time.time;
        }

        private void OnGUI()
        {
            if (!_visible || _presenter == null || Event.current.type != EventType.Repaint)
                return;

            // IMGUI draws over the uGUI canvas, so an always-on HUD hides the main menu and the
            // lair behind it. Only draw once the player is actually in the world.
            RaidHudModel model = _presenter.Model;
            // The crosshair's "over something" look follows the interact target, or a piece the left mouse
            // would grab, wherever the crosshair shows (#353).
            if (_crosshair != null && CrosshairView.ShownIn(model.State))
                _crosshair.HasTarget = model.HasInteractTarget ||
                    (ItemManager.Instance != null && ItemManager.Instance.HoveredItem != null);
            if (model.State != Plunderspell.Core.GameState.Playing &&
                model.State != Plunderspell.Core.GameState.Paused)
                return;
            var gameState = Plunderspell.Core.GameServices.GameState;
            if (gameState != null && !gameState.RaidOnScreen)
                return; // paused in the Lair: no raid clock or alarm behind the pause menu

            float scale = Screen.height / k_referenceHeight;
            if (scale <= 0f)
                return;

            EnsureStyles();

            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;
            float centre = width * 0.5f;

            DrawPrompt(model, centre);
            DrawChant(model, centre);
            DrawListening(model, centre);

            GUI.matrix = previousMatrix;
        }

        // --- Centre: the prompt under the crosshair -------------------------------------------------

        private void DrawPrompt(RaidHudModel model, float centre)
        {
            const float promptMid = 592f;

            if (!string.IsNullOrEmpty(model.InteractPrompt))
            {
                if (!string.Equals(_promptSource, model.InteractPrompt, StringComparison.Ordinal))
                {
                    _promptSource = model.InteractPrompt;
                    _promptHasKey = SplitPrompt(_promptSource, out _promptKey, out _promptRest);
                }

                if (_promptHasKey)
                {
                    // The key sits in a box, then what it does, so the eye finds the key first.
                    float keyWidth = Measure(_promptKey, _mono17).x + 14f;
                    float restWidth = Measure(_promptRest, _mono17).x;
                    float left = centre - (keyWidth + 8f + restWidth) * 0.5f;
                    Frame(new Rect(left, promptMid - 13f, keyWidth, 26f), Theme.TextFaint);
                    DrawText(left + 7f, promptMid, _promptKey, _mono17, Theme.Text);
                    DrawText(left + keyWidth + 8f, promptMid, _promptRest, _mono17, Theme.Text);
                }
                else
                {
                    DrawTextCentre(centre, promptMid, _promptSource, _mono17, Theme.Text);
                }
            }

            if (!string.IsNullOrEmpty(model.RangedWeaponStatus))
                DrawTextCentre(centre, promptMid + 30f, model.RangedWeaponStatus, _mono15, Theme.TextDim);
        }

        /// <summary>
        /// While the cast key is held: which microphone is open and how loud it hears you, against
        /// the whisper and shout marks. Without it a dead or wrong microphone looks exactly like a
        /// word the game did not understand.
        /// </summary>
        private void DrawListening(RaidHudModel model, float centre)
        {
            bool casting = model.IsCasting;
            if (casting)
            {
                // Which microphone is open. The level meter lives in Settings now, where it can be tuned (#303).
                string title = KeyboardCasting;
                if (model.ListenDevice != null)
                {
                    if (!string.Equals(_listenDevice, model.ListenDevice, StringComparison.Ordinal))
                    {
                        _listenDevice = model.ListenDevice;
                        _listenLine = ListeningOn + _listenDevice;
                    }
                    title = _listenLine;
                }
                DrawTextCentre(centre, 938f, title, _mono14, Theme.Voice);
            }
            else if (Time.time - _captionAt < k_captionSeconds)
            {
                // What you said, and what it became: a clean cast, a misfire, or a fizzle (#49).
                DrawTextCentre(centre, 1017f, _caption, _italic28, _captionColour);
            }
        }

        /// <summary>
        /// A keyed cast is chanted before it fires (#116): show which word and how far along, just
        /// under the crosshair, so the wait reads as the spell gathering rather than lag.
        /// </summary>
        private void DrawChant(RaidHudModel model, float centre)
        {
            if (!model.Chanting)
                return;

            string word = model.ChantWord;
            if (!string.Equals(_chantWord, word, StringComparison.Ordinal))
            {
                _chantWord = word;
                _chantLine = $"Chanting {word}";
            }

            DrawTextCentre(centre, 640f, _chantLine, _display26, Theme.Voice);
            DrawBar(new Rect(centre - 110f, 660f, 220f, 8f), model.ChantProgress, Theme.Voice, 1f, 0.75f);
        }

        // --- Drawing helpers -------------------------------------------------------------------------

        private void Fill(Rect rect, Color colour)
        {
            GUI.color = colour;
            GUI.DrawTexture(rect, _white);
            GUI.color = Color.white;
        }

        /// <summary>A 1px outline.</summary>
        private void Frame(Rect rect, Color colour)
        {
            Fill(new Rect(rect.x, rect.y, rect.width, 1f), colour);
            Fill(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), colour);
            Fill(new Rect(rect.x, rect.y, 1f, rect.height), colour);
            Fill(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), colour);
        }

        /// <summary>A bar: a 1px Line frame, a dark inside, and a flat fill.</summary>
        private void DrawBar(Rect rect, float fill, Color colour, float alpha, float insideAlpha)
        {
            Fill(rect, Theme.Line);
            var inside = new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f);
            Color ground = Theme.Ground;
            ground.a = insideAlpha;
            Fill(inside, ground);
            if (fill > 0f)
            {
                colour.a *= alpha;
                Fill(new Rect(inside.x, inside.y, inside.width * Mathf.Clamp01(fill), inside.height), colour);
            }
        }

        private Vector2 Measure(string text, GUIStyle style)
        {
            _measure.text = text;
            return style.CalcSize(_measure);
        }

        /// <summary>Draws text with its left edge at x and its vertical centre at yMid; returns its width.</summary>
        private float DrawText(float x, float yMid, string text, GUIStyle style, Color colour)
        {
            Vector2 size = Measure(text, style);
            var rect = new Rect(x, yMid - size.y * 0.5f, size.x + 2f, size.y);
            // A shadow, because this text sits over the lit world and vellum alone washes out on a bright wall.
            GUI.contentColor = ShadowColour;
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1.5f, rect.width, rect.height), text, style);
            GUI.contentColor = colour;
            GUI.Label(rect, text, style);
            GUI.contentColor = Color.white;
            return size.x;
        }

        private float DrawTextRight(float right, float yMid, string text, GUIStyle style, Color colour)
        {
            float width = Measure(text, style).x;
            DrawText(right - width, yMid, text, style, colour);
            return width;
        }

        private void DrawTextCentre(float centre, float yMid, string text, GUIStyle style, Color colour)
        {
            float width = Measure(text, style).x;
            DrawText(centre - width * 0.5f, yMid, text, style, colour);
        }

        private void EnsureStyles()
        {
            if (_mono12 != null)
                return;

            // The same typefaces and palette as the uGUI screens (UIFactory, UITheme), so the raid HUD
            // reads as part of the game's menus rather than Unity's debug skin (#137).
            _mono12 = MakeStyle(UIFonts.Mono, 12);
            _mono14 = MakeStyle(UIFonts.Mono, 14);
            _mono15 = MakeStyle(UIFonts.Mono, 15);
            _mono17 = MakeStyle(UIFonts.Mono, 17);
            _display26 = MakeStyle(UIFonts.Display, 26);
            _italic28 = MakeStyle(UIFonts.BodyItalic, 28);
            _white = UITextures.White;
        }

        private static GUIStyle MakeStyle(Font font, int size)
        {
            // Not copied from GUI.skin.label, which carries margins and padding this layout does its own way.
            var style = new GUIStyle
            {
                font = font,
                fontSize = size,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false,
                richText = true,
            };
            style.normal.textColor = Color.white;
            return style;
        }
    }
}
