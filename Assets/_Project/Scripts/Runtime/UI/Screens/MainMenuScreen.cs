using Code.Scripts.EventSystems;
using Plunderspell.Core;
using Plunderspell.Lair;
using UnityEngine;
using UnityEngine.UI;

namespace Plunderspell.UI.Screens
{
    public class MainMenuScreen : UIScreen
    {
        // Positions are the mockup's .mm-col, .mm-right and .mm-foot, in 1920x1080 canvas units.
        private const float ColumnLeft = 180f;
        private const float ColumnTop = 130f;
        private const float ColumnWidth = 700f;
        private const float WordmarkLine = 131f; // 156 * 0.84, the mockup's line height
        private const float SigilSize = 150f;
        private const float FooterBottom = 60f;

        protected override void OnBuild()
        {
            UIFactory.CreateBackdrop(transform);

            BuildColumn();
            BuildLexicon();
            BuildFooter();
        }

        private void BuildColumn()
        {
            RectTransform eyebrow = UIFactory.CreateEyebrow(transform, "Eyebrow", "A heist in four centuries \u00B7 up to four wizards");
            UIFactory.PlaceTopLeft(eyebrow, ColumnLeft, ColumnTop, ColumnWidth, 24f);

            // Two rows so the second word can be a different colour; each is centred in its own line box.
            float wordmarkTop = ColumnTop + 24f + 26f;
            AddWordmarkLine("Title", "PLUNDER", UITheme.Text, wordmarkTop);
            AddWordmarkLine("TitleSecond", "SPELL", UITheme.Interactive, wordmarkTop + WordmarkLine);

            float hookTop = wordmarkTop + WordmarkLine * 2f + 24f;
            var hook = UIFactory.CreateText(transform, "Hook", "Say the word. Carry what you can. Get out before the house wakes.",
                29, UITheme.TextDim, TextAnchor.UpperLeft, UIFonts.BodyLight);
            UIFactory.PlaceTopLeft(hook.rectTransform, ColumnLeft, hookTop, 640f, 90f);

            var buttonListGo = new GameObject("ButtonList", typeof(RectTransform));
            buttonListGo.transform.SetParent(transform, false);
            var buttonListRect = (RectTransform)buttonListGo.transform;
            UIFactory.PlaceTopLeft(buttonListRect, ColumnLeft, hookTop + 82f + 40f, 520f, 64f * 5f + 10f * 4f);
            UIFactory.AddVerticalLayout(buttonListRect, spacing: 10f, padding: new RectOffset(0, 0, 0, 0), childAlignment: TextAnchor.UpperLeft);

            var buttonSize = new Vector2(520f, 64f);
            BuildSaveRow(buttonListRect);
            UIFactory.CreateButton(buttonListRect, "PlayButton", "Play Solo", OnPlayClicked, buttonSize, ButtonKind.Primary, "ENTER");
            UIFactory.CreateButton(buttonListRect, "HostButton", "Host Co-op", OnHostClicked, buttonSize, ButtonKind.Secondary, "STEAM");
            UIFactory.CreateButton(buttonListRect, "SettingsButton", "Settings", OnSettingsClicked, buttonSize);
            UIFactory.CreateButton(buttonListRect, "QuitButton", "Quit", OnQuitClicked, buttonSize, ButtonKind.Quiet);
        }

        /// <summary>The save slot picker: a stepper through the slots and a Reset that asks twice.</summary>
        private void BuildSaveRow(RectTransform list)
        {
            const float resetWidth = 130f;
            const float gap = 10f;
            var rowGo = new GameObject("SaveRow", typeof(RectTransform));
            rowGo.transform.SetParent(list, false);
            var row = (RectTransform)rowGo.transform;
            row.sizeDelta = new Vector2(520f, 64f);

            RectTransform stepper = UIFactory.CreateStepper(row, "SaveSlot", () => StepSlot(-1), () => StepSlot(1),
                new Vector2(520f - resetWidth - gap, 64f), out _saveValue);
            UIFactory.PlaceTopLeft(stepper, 0f, 0f, 520f - resetWidth - gap, 64f);

            Button reset = UIFactory.CreateButton(row, "ResetSaveButton", "Reset", OnResetClicked, new Vector2(resetWidth, 64f), ButtonKind.Quiet);
            UIFactory.PlaceTopRight(reset.GetComponent<RectTransform>(), 0f, 0f, resetWidth, 64f);
            _resetLabel = reset.GetComponentInChildren<Text>();
            RefreshSaveLabel();
        }

        private Text _saveValue;
        private Text _resetLabel;
        private bool _resetArmed;

        private void StepSlot(int step)
        {
            int slot = ((SaveSlots.Active - 1 + step) % SaveSlots.Count + SaveSlots.Count) % SaveSlots.Count + 1;
            LairHubManager lair = FindFirstObjectByType<LairHubManager>();
            if (lair != null)
                lair.LoadSlot(slot);
            else
                SaveSlots.Active = slot;
            _resetArmed = false;
            RefreshSaveLabel();
        }

        // The first press arms it and the second wipes the slot, so a stray click cannot lose a save.
        private void OnResetClicked()
        {
            if (!_resetArmed)
            {
                _resetArmed = true;
                RefreshSaveLabel();
                return;
            }

            _resetArmed = false;
            int slot = SaveSlots.Active;
            LairHubManager.ResetSlot(slot);
            LairHubManager lair = FindFirstObjectByType<LairHubManager>();
            if (lair != null)
                lair.LoadSlot(slot);
            Debug.Log($"[Save] Slot {slot} reset to a new campaign.");
            RefreshSaveLabel();
        }

        private void RefreshSaveLabel()
        {
            if (_saveValue == null)
                return;
            int slot = SaveSlots.Active;
            if (LairHubManager.HasSave(slot))
            {
                LairState state = LairHubManager.Peek(slot);
                _saveValue.text = $"Save {slot} · debt {state.TotalDebt:0} · gold {state.AccumulatedGold:0}";
            }
            else
            {
                _saveValue.text = $"Save {slot} · new";
            }
            if (_resetLabel != null)
                _resetLabel.text = _resetArmed ? "Sure?" : "Reset";
        }

        private void AddWordmarkLine(string name, string word, Color colour, float top)
        {
            // Wide enough that the word never wraps: legacy Text breaks a too-long word mid-letter.
            var line = UIFactory.CreateText(transform, name, word, UITheme.Hero, colour, TextAnchor.MiddleLeft, UIFonts.DisplayHeavy);
            UIFactory.PlaceTopLeft(line.rectTransform, ColumnLeft, top, 900f, WordmarkLine);
        }

        private void BuildLexicon()
        {
            const float right = 180f;
            const float width = 560f;

            var sigil = UIFactory.CreateBackdrop(transform, "Sigil", UITextures.Sigil);
            UIFactory.PlaceTopRight(sigil.rectTransform, right, ColumnTop, SigilSize, SigilSize);
            sigil.raycastTarget = false;

            float eyebrowTop = ColumnTop + SigilSize + 26f;
            RectTransform eyebrow = UIFactory.CreateEyebrow(transform, "LexiconEyebrow", "The lexicon", ruleFirst: true);
            UIFactory.PlaceTopRight(eyebrow, right, eyebrowTop, width, 24f);

            // The eight words the game listens for, alternating dim and faint as in the mockup.
            string dim = ColorUtility.ToHtmlStringRGB(UITheme.TextDim);
            string dot = " \u00B7 ";
            string lexicon =
                $"<color=#{dim}>Ignis</color>{dot}Frango{dot}<color=#{dim}>Levo</color>\n" +
                $"Aurum Voco{dot}<color=#{dim}>Velox</color>\n" +
                $"<color=#{dim}>Somnus</color>{dot}Saltus{dot}<color=#{dim}>Porta</color>";
            var words = UIFactory.CreateText(transform, "Lexicon", lexicon, 36, UITheme.TextFaint, TextAnchor.UpperRight, UIFonts.Display);
            UIFactory.PlaceTopRight(words.rectTransform, right, eyebrowTop + 24f + 14f, width, 150f);
        }

        private void BuildFooter()
        {
            const float side = 180f;
            const float textHeight = 24f;

            var rule = UIFactory.CreateImage(transform, "FooterRule", UITheme.Line);
            UIFactory.PlaceBottomStretch(rule.rectTransform, side, side, FooterBottom + textHeight + 18f, 1f);

            _status = UIFactory.CreateText(transform, "CoopStatus", "", UITheme.Label, UITheme.TextFaint, TextAnchor.MiddleLeft, UIFonts.Mono);
            UIFactory.PlaceBottomLeft(_status.rectTransform, side, FooterBottom, 1000f, textHeight);

            var version = UIFactory.CreateText(transform, "VersionLabel", UITheme.Tracked("v0.1.0 \u00B7 prototype", UITheme.Label), UITheme.Label, UITheme.TextFaint,
                TextAnchor.MiddleRight, UIFonts.Mono);
            UIFactory.PlaceBottomRight(version.rectTransform, side, FooterBottom, 500f, textHeight);
        }

        private Text _status;

        protected override void OnShown()
        {
            EventManager.Instance?.UnsubscribeFromAllEvents(this);
            EventManager.Instance?.Subscribe(this, (CoopChanged e) => RefreshStatus());
            RefreshStatus();
            _resetArmed = false;
            RefreshSaveLabel();
        }

        private void RefreshStatus()
        {
            if (_status != null)
                _status.text = GameServices.Coop != null ? UITheme.Tracked(GameServices.Coop.Status, UITheme.Label) : string.Empty;
        }

        private void OnPlayClicked()
        {
            GameServices.Coop?.PlaySolo();
            GameServices.GameState.ChangeState(GameState.LairRoom);
        }

        /// <summary>The session moves to the Lair itself once hosting has started: over Steam that
        /// waits for the lobby to be created.</summary>
        private void OnHostClicked() => GameServices.Coop?.HostCoop();

        private void OnSettingsClicked() => GameServices.GameState.ChangeState(GameState.Settings);

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
