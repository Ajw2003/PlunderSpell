using Plunderspell.Core;
using Plunderspell.Spells;
using UnityEngine;
using UnityEngine.InputSystem;
using Theme = Plunderspell.UI.UITheme;

namespace Plunderspell.UI
{
    /// <summary>
    /// The grimoire, as flat UI for now (#325, owner 2026-10-06; the 3D model is #282): hold <c>Tab</c> and a book opens.
    /// The left page lists the eight spells with their keys and mana cost; the right page is the one you have turned to
    /// (scroll to turn): the first teaches the keys, then one page per spell with its word, cost and what it does, and the
    /// last few casts in the margin. Replaces the always-on spell list and the last-cast line. It draws from
    /// <see cref="RaidHudPresenter.Model"/> and reads no other system.
    /// </summary>
    [RequireComponent(typeof(RaidHudPresenter))]
    public sealed class GrimoireView : MonoBehaviour
    {
        private const float k_referenceHeight = 1080f;
        private const float BookWidth = 820f;
        private const float BookHeight = 520f;
        private const float PageInset = 34f;

        /// <summary>The eight spell words in key order; mirrors <c>MockVoiceInputService.keybindMap</c>.</summary>
        public static readonly string[] Words =
        {
            "IGNIS", "FRANGO", "LEVO", "AURUM VOCO", "VELOX", "SOMNUS", "SALTUS", "PORTA",
        };

        /// <summary>Page 0 is the keys; pages 1 to 8 are the spells.</summary>
        public const int PageCount = 9;

        private static readonly string[] KeyLines =
        {
            "Hold [V]   open the mic, say a word",
            "Press 1-8  cast without a mic",
            "Shift shout · Ctrl whisper · C creep",
            "[E] use or lift  ·  [Q] drop",
            "Hold [RMB] aim, [G] fire a crossbow",
            "Hold [T]   the pocket watch",
            "Hold [Tab] this book: both hands busy,",
            "so you walk slower and cannot lift.",
        };

        private RaidHudPresenter _presenter;
        private int _page;
        private GUIStyle _mono12, _mono14, _mono15, _display23, _display38, _body20;
        private Texture2D _white;

        private void Awake() => _presenter = GetComponent<RaidHudPresenter>();

        /// <summary>The page after turning <paramref name="delta"/> leaves, kept inside the book.</summary>
        public static int Turn(int page, int delta) => Mathf.Clamp(page + delta, 0, PageCount - 1);

        private void Update()
        {
            if (_presenter == null || !_presenter.Model.GrimoireOpen)
                return;

            Mouse mouse = Mouse.current;
            if (mouse == null)
                return;
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll > 0f)
                _page = Turn(_page, -1);
            else if (scroll < 0f)
                _page = Turn(_page, 1);
        }

        private void OnGUI()
        {
            if (_presenter == null || Event.current.type != EventType.Repaint)
                return;
            RaidHudModel model = _presenter.Model;
            if (!model.GrimoireOpen || model.State != GameState.Playing)
                return;

            float scale = Screen.height / k_referenceHeight;
            if (scale <= 0f)
                return;

            EnsureStyles();
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;
            Fill(new Rect(0f, 0f, width, k_referenceHeight), new Color(0f, 0f, 0f, 0.35f));
            Draw(model, new Rect((width - BookWidth) * 0.5f, 250f, BookWidth, BookHeight));
            GUI.matrix = previous;
        }

        private void Draw(RaidHudModel model, Rect book)
        {
            Fill(book, Theme.Line);
            Color surface = Theme.Surface;
            surface.a = 0.97f;
            Fill(new Rect(book.x + 1f, book.y + 1f, book.width - 2f, book.height - 2f), surface);
            Fill(new Rect(book.center.x - 0.5f, book.y + 24f, 1f, book.height - 48f), Theme.Line);

            DrawContents(model, new Rect(book.x + PageInset, book.y + 28f, book.width * 0.5f - PageInset * 2f, book.height - 56f));
            DrawPage(model, new Rect(book.center.x + PageInset, book.y + 28f, book.width * 0.5f - PageInset * 2f, book.height - 56f));

            Label(book.x + PageInset, book.yMax - 26f, "scroll to turn the page  ·  let go of Tab to close", _mono12, Theme.TextFaint);
        }

        // The left page: every spell, its key and what it costs, struck through when the mana pool cannot cover it.
        private void DrawContents(RaidHudModel model, Rect page)
        {
            Label(page.x, page.y + 8f, "SPELLS", _mono14, Theme.TextDim);
            Fill(new Rect(page.x, page.y + 28f, page.width, 1f), Theme.Line);

            SpellLexicon lexicon = _presenter.Lexicon;
            for (int i = 0; i < Words.Length; i++)
            {
                float mid = page.y + 64f + i * 40f;
                int cost = CostOf(lexicon, Words[i]);
                bool poor = cost > model.Mana;
                bool turnedTo = _page == i + 1;

                Label(page.x, mid, (i + 1).ToString(), _mono14, Theme.TextFaint);
                Color wordColour = poor ? Theme.TextFaint : turnedTo ? Theme.Voice : Theme.Text;
                float wordWidth = Label(page.x + 34f, mid, Words[i], _display23, wordColour);
                if (poor)
                    Fill(new Rect(page.x + 34f, mid, wordWidth, 1f), Theme.TextFaint);
                if (cost > 0)
                    LabelRight(page.xMax, mid, cost.ToString(), _mono15, poor ? Theme.TextFaint : Theme.Voice);
            }
        }

        // The right page: the keys, or one spell, then the margin with the last casts.
        private void DrawPage(RaidHudModel model, Rect page)
        {
            if (_page == 0)
            {
                Label(page.x, page.y + 8f, "THE KEYS", _mono14, Theme.TextDim);
                Fill(new Rect(page.x, page.y + 28f, page.width, 1f), Theme.Line);
                for (int i = 0; i < KeyLines.Length; i++)
                    Label(page.x, page.y + 62f + i * 28f, KeyLines[i], _mono12, Theme.Text);
            }
            else
            {
                string word = Words[_page - 1];
                Label(page.x, page.y + 8f, "PAGE " + _page, _mono14, Theme.TextDim);
                Fill(new Rect(page.x, page.y + 28f, page.width, 1f), Theme.Line);
                Label(page.x, page.y + 78f, word, _display38, Theme.Voice);

                SpellWord entry = _presenter.Lexicon != null ? _presenter.Lexicon.FindByWord(word) : null;
                if (entry != null)
                    Label(page.x, page.y + 122f, "costs " + entry.ManaCost + " mana", _mono14, Theme.TextDim);
                string description = entry != null && !string.IsNullOrEmpty(entry.Description) ? entry.Description : "Not yet written.";
                GUI.contentColor = Theme.Text;
                GUI.Label(new Rect(page.x, page.y + 142f, page.width, 150f), description, _body20);
                GUI.contentColor = Color.white;
            }

            string[] recent = model.RecentCasts;
            float margin = page.yMax - 96f;
            Fill(new Rect(page.x, margin - 14f, page.width, 1f), Theme.Line);
            Label(page.x, margin, "LAST CASTS", _mono12, Theme.TextFaint);
            if (recent == null || recent.Length == 0)
            {
                Label(page.x, margin + 24f, "none yet", _mono12, Theme.TextFaint);
                return;
            }
            for (int i = 0; i < recent.Length; i++)
            {
                bool misfire = recent[i].StartsWith("MISFIRE", System.StringComparison.Ordinal);
                Label(page.x, margin + 24f + i * 20f, recent[i], _mono12, misfire ? Theme.Danger : Theme.Text);
            }
        }

        private static int CostOf(SpellLexicon lexicon, string word) =>
            lexicon != null && lexicon.FindByWord(word) is SpellWord entry ? entry.ManaCost : 0;

        private float Label(float x, float yMid, string text, GUIStyle style, Color colour)
        {
            Vector2 size = style.CalcSize(new GUIContent(text));
            GUI.contentColor = colour;
            GUI.Label(new Rect(x, yMid - size.y * 0.5f, size.x + 2f, size.y), text, style);
            GUI.contentColor = Color.white;
            return size.x;
        }

        private void LabelRight(float right, float yMid, string text, GUIStyle style, Color colour)
        {
            float width = style.CalcSize(new GUIContent(text)).x;
            Label(right - width, yMid, text, style, colour);
        }

        private void Fill(Rect rect, Color colour)
        {
            GUI.color = colour;
            GUI.DrawTexture(rect, _white);
            GUI.color = Color.white;
        }

        private void EnsureStyles()
        {
            if (_mono12 != null)
                return;
            _mono12 = Style(UIFonts.Mono, 12, false);
            _mono14 = Style(UIFonts.Mono, 14, false);
            _mono15 = Style(UIFonts.Mono, 15, false);
            _display23 = Style(UIFonts.Display, 23, false);
            _display38 = Style(UIFonts.Display, 38, false);
            _body20 = Style(UIFonts.BodyItalic, 20, true);
            _body20.alignment = TextAnchor.UpperLeft;
            _white = UITextures.White;
        }

        private static GUIStyle Style(Font font, int size, bool wrap)
        {
            var style = new GUIStyle { font = font, fontSize = size, alignment = TextAnchor.MiddleLeft, wordWrap = wrap, richText = false };
            style.normal.textColor = Color.white;
            return style;
        }
    }
}
