using Code.Scripts.EventSystems;
using System;
using Plunderspell.Alarm;
using Plunderspell.Raid;
using UnityEngine;
using Theme = Plunderspell.UI.UITheme;

namespace Plunderspell.UI
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
    [RequireComponent(typeof(CrosshairView))]
    public class RaidHudView : MonoBehaviour
    {
        [Tooltip("Hide the HUD (e.g. for screenshots).")]
        [SerializeField] private bool _visible = true;

        [Tooltip("Show the push-to-cast key and the spell list.")]
        [SerializeField] private bool _showSpellbook = true;

        // Everything is laid out in 1080p units (the mockup's CSS pixels) and scaled to the screen.
        private const float k_referenceHeight = 1080f;
        private const float k_edge = 36f;
        private const float k_pulseSeconds = 2.4f;

        // Mirrors MockVoiceInputService.keybindMap; the HUD only needs the words, not the service.
        private static readonly string[] Spellbook =
        {
            "IGNIS", "FRANGO", "LEVO", "AURUM VOCO",
            "VELOX", "SOMNUS", "SALTUS", "PORTA",
        };

        private static readonly string[] SpellKeys = { "1", "2", "3", "4", "5", "6", "7", "8" };

        private static readonly AlarmState[] AlarmStates =
        {
            AlarmState.Calm, AlarmState.Stirred, AlarmState.Roused, AlarmState.HueAndCry,
        };

        // Letter-spaced labels are built once: tracking a string allocates, and the HUD draws every frame.
        private static readonly string[] AlarmNames =
        {
            Theme.Tracked("Calm", 15), Theme.Tracked("Stirred", 15), Theme.Tracked("Roused", 15), Theme.Tracked("Hue and cry", 15),
        };

        private static readonly string[] AlarmSegmentLabels =
        {
            Theme.Tracked("Calm", 12), Theme.Tracked("Stirred", 12), Theme.Tracked("Roused", 12), Theme.Tracked("Hue & cry", 12),
        };

        private static readonly Color ShadowColour = new Color(0.04f, 0.03f, 0.02f, 0.85f);
        private static readonly string Separator = Theme.Tracked(" · ", 16);
        private static readonly string OwedLabel = Theme.Tracked("Owed", 17);
        private static readonly string BankedLabel = Theme.Tracked("Banked", 17);
        private static readonly string HoldToCast = Theme.Tracked("Hold [V] to cast", 14);
        private static readonly string ManaHeader = Theme.Tracked("Mana", 14);
        private static readonly string ListeningHeader = Theme.Tracked("Listening · speak", 14);
        private static readonly string OrNumbers = Theme.Tracked("or 1–8", 14);
        private static readonly string CastingHeader = Theme.Tracked("Casting", 14);
        private static readonly string PressNumber = Theme.Tracked("Press 1–8", 14);
        private static readonly string SpellFooter = Theme.Tracked("Shift shout · Ctrl whisper", 12);
        private static readonly string KeyboardCasting = "● " + Theme.Tracked("Keyboard casting · press 1–8", 14);
        private static readonly string ListeningOn = "● " + Theme.Tracked("Listening on", 14) + " ";
        private static readonly string CarryingLabel = Theme.Tracked("Carrying", 14);
        private static readonly string TowingLabel = Theme.Tracked("Towing", 14);
        private static readonly string TooHeavy = Theme.Tracked(" · too heavy to lift", 14);
        private static readonly string PieceLabel = Theme.Tracked("piece", 14);
        private static readonly string PiecesLabel = Theme.Tracked("pieces", 14);
        private const string NothingInPortal = "Nothing in the portal yet";

        private RaidHudPresenter _presenter;
        private CrosshairView _crosshair;

        // Styles are white so a colour can be chosen per draw (GUI.contentColor) without touching them.
        private GUIStyle _mono12;
        private GUIStyle _mono14;
        private GUIStyle _mono15;
        private GUIStyle _mono17;
        private GUIStyle _monoBold17;
        private GUIStyle _display22;
        private GUIStyle _display23;
        private GUIStyle _display26;
        private GUIStyle _display30;
        private GUIStyle _display76;
        private GUIStyle _italic24;
        private GUIStyle _italic28;
        private Texture2D _white;
        private readonly GUIContent _measure = new GUIContent();

        // Text that only changes when its number does, so a steady HUD makes no garbage.
        private int _clockSecond = -1;
        private string _clockText = string.Empty;
        private float _owedValue = float.NaN;
        private string _owedText = string.Empty;
        private float _bankedValue = float.NaN;
        private string _bankedText = string.Empty;
        private float _haulWorth = float.NaN;
        private string _haulText = string.Empty;
        private int _haulPieces = -1;
        private string _pieceText = string.Empty;
        private string _promptSource;
        private bool _promptHasKey;
        private string _promptKey = string.Empty;
        private string _promptRest = string.Empty;
        private string _carriedName;
        private bool _carriedTwo;
        private string _carriedLine = string.Empty;
        private string _chantWord;
        private string _chantLine = string.Empty;
        private string _listenDevice;
        private string _listenLine = string.Empty;
        private readonly string[] _costTexts = new string[100];

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
            if (model.State != Plunderspell.Core.GameState.Playing &&
                model.State != Plunderspell.Core.GameState.Paused)
                return;

            float scale = Screen.height / k_referenceHeight;
            if (scale <= 0f)
                return;

            EnsureStyles();

            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;
            float centre = width * 0.5f;

            DrawTopLeft(model);
            DrawTopRight(model, width - k_edge);

            if (_crosshair != null)
                _crosshair.HasTarget = model.HasInteractTarget;

            DrawPrompt(model, centre);
            DrawCarrying(model, centre);
            DrawSpellbook(model, width - k_edge);
            DrawChant(model, centre);
            DrawListening(model, centre);

            GUI.matrix = previousMatrix;
        }

        // --- Top-left: phase, clock, alarm ---------------------------------------------------------

        private void DrawTopLeft(RaidHudModel model)
        {
            const float width = 420f;
            const float segmentTop = 150f;

            // "RAIDING · CALM": the alarm's name turns madder as it climbs.
            int alarmIndex = Mathf.Clamp((int)model.Alarm, 0, AlarmNames.Length - 1);
            bool alarmed = model.Alarm == AlarmState.Roused || model.Alarm == AlarmState.HueAndCry;
            float x = k_edge;
            x += DrawText(x, 44f, TrackedPhase(model.Phase), _mono15, Theme.TextDim);
            x += DrawText(x, 44f, Separator, _mono15, Theme.TextDim);
            DrawText(x, 44f, AlarmNames[alarmIndex], _mono15, alarmed ? Theme.Danger : Theme.TextFaint);

            int second = Mathf.FloorToInt(Mathf.Max(0f, model.TimeRemaining));
            if (second != _clockSecond)
            {
                _clockSecond = second;
                _clockText = model.TimerText;
            }
            DrawText(k_edge, 98f, _clockText, _display76, model.TimerIsCritical ? Theme.Danger : Theme.Text);

            // Four segments, one per alarm state; the fill of each is its quarter of the alarm level.
            float segmentWidth = (width - 12f) * 0.25f;
            for (int k = 0; k < AlarmStates.Length; k++)
            {
                float left = k_edge + k * (segmentWidth + 4f);
                DrawBar(new Rect(left, segmentTop, segmentWidth, 10f), Mathf.Clamp01(model.AlarmFill * 4f - k),
                    Theme.AlarmColour(AlarmStates[k]), k == 3 ? Pulse() : 1f, 0.7f);
                DrawText(left, 175f, AlarmSegmentLabels[k], _mono12, k == alarmIndex ? Theme.Text : Theme.TextFaint);
            }
        }

        /// <summary>Alpha for the hue and cry segment: 1 down to 0.55 and back over 2.4 seconds.</summary>
        private static float Pulse() => 0.775f + 0.225f * Mathf.Cos(Time.time * (Mathf.PI * 2f / k_pulseSeconds));

        // --- Top-right: the money, and the haul standing in the portal -----------------------------

        private void DrawTopRight(RaidHudModel model, float right)
        {
            // The haul sits with the debt rather than near the crosshair because it is the number the
            // debt is measured against.
            if (!Mathf.Approximately(_owedValue, model.Debt) || float.IsNaN(_owedValue))
            {
                _owedValue = model.Debt;
                _owedText = model.Debt.ToString("N0");
            }
            if (!Mathf.Approximately(_bankedValue, model.BankedGold) || float.IsNaN(_bankedValue))
            {
                _bankedValue = model.BankedGold;
                _bankedText = model.BankedGold.ToString("N0");
            }

            const float gap = 10f;
            float labelOwed = Measure(OwedLabel, _mono17).x;
            float figureOwed = Measure(_owedText, _monoBold17).x;
            float separator = Measure(Separator, _mono17).x;
            float labelBanked = Measure(BankedLabel, _mono17).x;
            float figureBanked = Measure(_bankedText, _monoBold17).x;
            float x = right - (labelOwed + figureOwed + separator + labelBanked + figureBanked + gap * 2f);

            x += DrawText(x, 46f, OwedLabel, _mono17, Theme.TextDim) + gap;
            x += DrawText(x, 46f, _owedText, _monoBold17, Theme.Value);
            x += DrawText(x, 46f, Separator, _mono17, Theme.TextDim);
            x += DrawText(x, 46f, BankedLabel, _mono17, Theme.TextDim) + gap;
            DrawText(x, 46f, _bankedText, _monoBold17, Theme.Value);

            if (model.HaulPieces > 0)
            {
                if (float.IsNaN(_haulWorth) || !Mathf.Approximately(_haulWorth, model.HaulWorth))
                {
                    _haulWorth = model.HaulWorth;
                    _haulText = $"{model.HaulWorth:N0} in the portal";
                }
                if (_haulPieces != model.HaulPieces)
                {
                    _haulPieces = model.HaulPieces;
                    _pieceText = $"{model.HaulPieces} " + (model.HaulPieces == 1 ? PieceLabel : PiecesLabel);
                }

                float countWidth = DrawTextRight(right, 90f, _pieceText, _mono14, Theme.TextDim);
                DrawTextRight(right - countWidth - 10f, 88f, _haulText, _display30, Theme.Value);
            }
            else
            {
                DrawTextRight(right, 88f, NothingInPortal, _italic24, Theme.TextDim);
            }
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

        /// <summary>"CARRYING name", or "TOWING name · TOO HEAVY TO LIFT" for a piece that needs two.</summary>
        private void DrawCarrying(RaidHudModel model, float centre)
        {
            if (string.IsNullOrEmpty(model.CarriedLootName))
                return;

            const float mid = 1022f;
            string label = model.CarriedNeedsTwo ? TowingLabel : CarryingLabel;
            float labelWidth = Measure(label, _mono14).x;
            float nameWidth = Measure(model.CarriedLootName, _display22).x;
            float tailWidth = model.CarriedNeedsTwo ? Measure(TooHeavy, _mono14).x : 0f;
            float x = centre - (labelWidth + 10f + nameWidth + tailWidth) * 0.5f;

            x += DrawText(x, mid, label, _mono14, Theme.TextDim) + 10f;
            x += DrawText(x, mid, model.CarriedLootName, _display22, Theme.Value);
            if (model.CarriedNeedsTwo)
                DrawText(x, mid, TooHeavy, _mono14, Theme.TextDim);
        }

        /// <summary>
        /// While the cast key is held: which microphone is open and how loud it hears you, against
        /// the whisper and shout marks. Without it a dead or wrong microphone looks exactly like a
        /// word the game did not understand.
        /// </summary>
        private void DrawListening(RaidHudModel model, float centre)
        {
            bool casting = model.IsCasting;
            bool carrying = !string.IsNullOrEmpty(model.CarriedLootName);

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
                DrawTextCentre(centre, carrying ? 977f : 1017f, _caption, _italic28, _captionColour);
            }

            // The last cast, so a misfire is unmissable.
            if (!string.IsNullOrEmpty(model.LastCastLine))
            {
                DrawTextCentre(centre, 902f, model.LastCastLine, _mono15,
                    model.LastCastLine.StartsWith("MISFIRE", StringComparison.Ordinal) ? Theme.Danger : Theme.Text);
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

        /// <summary>
        /// The casting controls. Push-to-cast is not guessable: you hold a key to open the mic and
        /// then say (here, press) the word, so without this the spells are invisible.
        /// </summary>
        private void DrawSpellbook(RaidHudModel model, float right)
        {
            // The pause panel takes the right edge and the panel would sit on its buttons.
            if (!_showSpellbook || model.State == Plunderspell.Core.GameState.Paused)
                return;

            const float width = 340f;
            const float height = 378f;
            const float padX = 22f;
            const float rowPitch = 34f;
            float x = right - width;
            float y = k_referenceHeight - 34f - height;

            bool casting = model.IsCasting;

            // The panel is a hairline: a Line border (lapis while a cast is being heard) round the surface.
            Fill(new Rect(x, y, width, height), casting ? Theme.VoiceLo : Theme.Line);
            Color surface = Theme.Surface;
            surface.a = 0.94f;
            Fill(new Rect(x + 1f, y + 1f, width - 2f, height - 2f), surface);

            string left = HoldToCast;
            string rightHeader = ManaHeader;
            if (casting)
            {
                bool speech = model.ListenDevice != null;
                left = speech ? ListeningHeader : CastingHeader;
                rightHeader = speech ? OrNumbers : PressNumber;
            }
            Color header = casting ? Theme.Voice : Theme.TextDim;
            DrawText(x + padX, y + 30f, left, _mono14, header);
            DrawTextRight(x + width - padX, y + 30f, rightHeader, _mono14, header);
            Fill(new Rect(x + padX, y + 49f, width - padX * 2f, 1f), Theme.Line);

            // Each word with its mana cost, struck through when the pool cannot cover it right now.
            Plunderspell.Spells.SpellLexicon lexicon = _presenter.Lexicon;
            int mana = model.Mana;
            for (int i = 0; i < Spellbook.Length; i++)
            {
                int cost = lexicon != null && lexicon.FindByWord(Spellbook[i]) is Plunderspell.Spells.SpellWord word
                    ? word.ManaCost
                    : 0;
                bool poor = cost > mana;
                float mid = y + 56f + rowPitch * i + rowPitch * 0.5f;
                float wordX = x + padX + 40f;

                DrawText(x + padX, mid, SpellKeys[i], _mono14, Theme.TextFaint);
                Vector2 wordSize = Measure(Spellbook[i], _display23);
                DrawText(wordX, mid, Spellbook[i], _display23, poor ? Theme.TextFaint : Theme.Text);
                if (poor)
                    Fill(new Rect(wordX, mid, wordSize.x, 1f), Theme.TextFaint);
                if (cost > 0)
                    DrawTextRight(x + width - padX, mid, CostText(cost), _mono15, poor ? Theme.TextFaint : Theme.Voice);
            }

            float footerTop = y + 56f + rowPitch * Spellbook.Length + 6f;
            Fill(new Rect(x + padX, footerTop, width - padX * 2f, 1f), Theme.Line);
            DrawText(x + padX, footerTop + 18f, SpellFooter, _mono12, Theme.TextFaint);
        }

        private string CostText(int cost)
        {
            if (cost >= _costTexts.Length)
                return cost.ToString();
            return _costTexts[cost] ?? (_costTexts[cost] = cost.ToString());
        }

        private static string TrackedPhase(RaidPhase phase)
        {
            switch (phase)
            {
                case RaidPhase.InLair: return PhaseLair;
                case RaidPhase.Generating: return PhaseGenerating;
                case RaidPhase.Raiding: return PhaseRaiding;
                case RaidPhase.Extracting: return PhaseExtracting;
                case RaidPhase.Resolved: return PhaseResolved;
                default: return phase.ToString();
            }
        }

        private static readonly string PhaseLair = Theme.Tracked("The Lair", 15);
        private static readonly string PhaseGenerating = Theme.Tracked("Building the castle…", 15);
        private static readonly string PhaseRaiding = Theme.Tracked("Raiding", 15);
        private static readonly string PhaseExtracting = Theme.Tracked("Extracting", 15);
        private static readonly string PhaseResolved = Theme.Tracked("Raid over", 15);

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
            _monoBold17 = MakeStyle(UIFonts.MonoBold, 17);
            _display22 = MakeStyle(UIFonts.Display, 22);
            _display23 = MakeStyle(UIFonts.Display, 23);
            _display26 = MakeStyle(UIFonts.Display, 26);
            _display30 = MakeStyle(UIFonts.Display, 30);
            _display76 = MakeStyle(UIFonts.Display, 76);
            _italic24 = MakeStyle(UIFonts.BodyItalic, 24);
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
