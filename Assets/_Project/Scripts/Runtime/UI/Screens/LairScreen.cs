using Code.Scripts.EventSystems;
using Plunderspell.Core;
using Plunderspell.Inventory;
using Plunderspell.Lair;
using UnityEngine;
using UnityEngine.UI;

namespace Plunderspell.UI.Screens
{
    /// <summary>
    /// Between raids: what you owe, what you have banked, and which era you set out in.
    ///
    /// Reads <see cref="LairHubManager"/> directly rather than through a pushed model. The lair is
    /// plain serialized state with no network authority, so a read here cannot race anything, and an
    /// event channel for three numbers would be more machinery than the screen is worth.
    /// </summary>
    public class LairScreen : UIScreen
    {
        private static readonly HistoricalEra[] Eras =
        {
            HistoricalEra.BronzeAge,
            HistoricalEra.HighMedieval,
            HistoricalEra.LateMedieval,
            HistoricalEra.AgeOfPowder,
        };

        // Each Age's stratum numeral, date and one-line character come from the pitch bible.
        private static readonly string[] Strata = { "Stratum I", "Stratum II", "Stratum III", "Stratum IV" };
        private static readonly string[] Dates = { "c. 1200 BC", "c. 1250", "c. 1450", "c. 1620" };
        private static readonly string[] Blurbs =
        {
            "Painted plaster, grain stores, and kings who are also gods. Fire runs faster here.",
            "Curtain walls, spiral stairs, and a chapel worth more than everything around it.",
            "Fortresses within fortresses, built by men who had you in mind. They hunt in pairs.",
            "Glass by the acre and magazines of black powder. One stray Ignis ends the evening.",
        };

        // Positions are the mockup's .lr-* classes, in 1920x1080 canvas units.
        private const float Side = 120f;
        private const float BodyTop = 430f;
        private const float LedgerTop = 196f;
        private const float LedgerHeight = 160f;
        private const float CardWidth = 598.5f;
        private const float CardHeight = 186f;
        private const float CardOutline = 2f;
        private const float CompanyWidth = 440f;
        private const float CompanyPadding = 22f;
        private const float CompanyGap = 14f;
        private const float FooterButtonBottom = 56f;
        private const float SetOutWidth = 420f;
        private const float SetOutHeight = 76f;
        private const int MaxFriendsShown = 12;

        private Text _debt;
        private Text _gold;
        private Text _lastRaid;
        private Text _debtNote;
        private Text _session;
        private Text _summary;
        private Text _waiting;
        private GameObject _setOut;
        private GameObject _invite;
        private RectTransform _friendList;
        private RectTransform _companyPanel;
        private float _friendListHeight;
        private LairHubManager _lair;

        private readonly Image[] _cardBorders = new Image[4];
        private readonly Image[] _cardFills = new Image[4];
        private readonly Text[] _cardDates = new Text[4];

        protected override void OnBuild()
        {
            UIFactory.CreateBackdrop(transform);

            BuildHeader();
            BuildLedger();
            BuildEras();
            BuildCompany();
            BuildFooter();
        }

        private void BuildHeader()
        {
            RectTransform eyebrow = UIFactory.CreateEyebrow(transform, "Eyebrow", "Between raids", withRule: false);
            UIFactory.PlaceTopLeft(eyebrow, Side, 72f, 600f, 24f);

            var title = UIFactory.CreateText(transform, "Title", "The Lair", UITheme.Title, UITheme.Text, TextAnchor.MiddleLeft, UIFonts.DisplayHeavy);
            UIFactory.PlaceTopLeft(title.rectTransform, Side, 72f + 24f + 14f, 900f, 83f);

            _debtNote = UIFactory.CreateText(transform, "DebtNote", string.Empty, UITheme.Label, UITheme.TextDim, TextAnchor.MiddleRight, UIFonts.Mono);
            UIFactory.PlaceTopRight(_debtNote.rectTransform, Side, 72f + 24f + 14f + 83f - 24f, 900f, 24f);
        }

        private void BuildLedger()
        {
            // A LineSoft frame with 1px gaps between the cells, so the seams read as hairlines.
            var frame = UIFactory.CreateImage(transform, "Ledger", UITheme.LineSoft);
            UIFactory.PlaceTopStretch(frame.rectTransform, Side, Side, LedgerTop, LedgerHeight);

            float unit = (1920f - Side * 2f - 2f - 2f) / 4f;
            RectTransform owed = BuildLedgerCell(frame.rectTransform, "OwedCell", 1f, unit, "Owed");
            RectTransform banked = BuildLedgerCell(frame.rectTransform, "BankedCell", 1f + unit + 1f, unit, "Banked");
            RectTransform last = BuildLedgerCell(frame.rectTransform, "LastRaidCell", 1f + (unit + 1f) * 2f, unit * 2f, "Last raid");

            AddFigure(owed, "DebtLabel", unit, out _debt);
            AddFigure(banked, "GoldLabel", unit, out _gold);

            _lastRaid = UIFactory.CreateText(last, "LastRaidLabel", string.Empty, 25, UITheme.TextDim, TextAnchor.UpperLeft, UIFonts.Body);
            UIFactory.PlaceTopLeft(_lastRaid.rectTransform, 30f, 60f, unit * 2f - 60f, 80f);
        }

        private static RectTransform BuildLedgerCell(RectTransform frame, string name, float x, float width, string eyebrowText)
        {
            var cell = UIFactory.CreateImage(frame, name, UITheme.Surface);
            UIFactory.PlaceTopLeft(cell.rectTransform, x, 1f, width, LedgerHeight - 2f);

            RectTransform eyebrow = UIFactory.CreateEyebrow(cell.rectTransform, "Eyebrow", eyebrowText);
            UIFactory.PlaceTopLeft(eyebrow, 30f, 26f, width - 60f, 24f);
            return cell.rectTransform;
        }

        private static void AddFigure(RectTransform cell, string name, float width, out Text figure)
        {
            RectTransform row = UIFactory.CreateFigureRow(cell, name, UIFonts.Display, 64, UITheme.Value, "COIN", 18, 10f, 64f, out figure);
            UIFactory.PlaceTopLeft(row, 30f, 60f, width - 60f, 64f);
        }

        private void BuildEras()
        {
            float width = CardWidth * 2f + 3f;

            RectTransform eyebrow = UIFactory.CreateEyebrow(transform, "ErasEyebrow", "Set out for");
            UIFactory.PlaceTopLeft(eyebrow, Side, BodyTop, width, 24f);

            var frame = UIFactory.CreateImage(transform, "Eras", UITheme.LineSoft);
            UIFactory.PlaceTopLeft(frame.rectTransform, Side, BodyTop + 24f + 18f, width, CardHeight * 2f + 3f);

            for (int i = 0; i < Eras.Length; i++)
                BuildEraCard(frame.rectTransform, i);
        }

        private void BuildEraCard(RectTransform frame, int index)
        {
            HistoricalEra era = Eras[index];
            HistoricalEra captured = era;

            var go = new GameObject($"Era_{era}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIButtonFocus));
            go.transform.SetParent(frame, false);
            var rect = (RectTransform)go.transform;
            UIFactory.PlaceTopLeft(rect, 1f + (index % 2) * (CardWidth + 1f), 1f + (index / 2) * (CardHeight + 1f), CardWidth, CardHeight);

            // The root is the outline, the same colour as the fill until this Age is chosen. The button
            // has no target graphic, so Selectable adds no tint of its own to the chosen colours.
            var border = go.GetComponent<Image>();
            border.color = UITheme.Surface;
            go.GetComponent<Button>().onClick.AddListener(() => SelectEra(captured));

            Image fill = UIFactory.CreateImage(rect, "Fill", UITheme.Surface);
            UIFactory.Stretch(fill.rectTransform, CardOutline, CardOutline, CardOutline, CardOutline);

            var marker = UIFactory.CreateImage(rect, "FocusMarker", UITheme.Interactive);
            marker.rectTransform.anchorMin = new Vector2(0f, 0f);
            marker.rectTransform.anchorMax = new Vector2(0f, 1f);
            marker.rectTransform.pivot = new Vector2(0f, 0.5f);
            marker.rectTransform.sizeDelta = new Vector2(6f, 0f);
            marker.rectTransform.anchoredPosition = Vector2.zero;
            marker.gameObject.SetActive(false);
            go.GetComponent<UIButtonFocus>().Setup(marker.gameObject, null, Color.clear, Color.clear, null, Color.clear, Color.clear);

            const float padX = 28f;
            float inner = CardWidth - padX * 2f;

            var stratum = UIFactory.CreateText(rect, "Stratum", UITheme.Tracked(Strata[index], 14), 14, UITheme.TextFaint, TextAnchor.MiddleLeft, UIFonts.Mono);
            UIFactory.PlaceTopLeft(stratum.rectTransform, padX, 24f, inner, 23f);

            var date = UIFactory.CreateText(rect, "Date", UITheme.Tracked(Dates[index], 14), 14, UITheme.TextFaint, TextAnchor.MiddleRight, UIFonts.Mono);
            UIFactory.PlaceTopLeft(date.rectTransform, padX, 24f, inner, 23f);
            _cardDates[index] = date;

            var name = UIFactory.CreateText(rect, "Name", Label(era), UITheme.Subheading, UITheme.Text, TextAnchor.MiddleLeft, UIFonts.Display);
            UIFactory.PlaceTopLeft(name.rectTransform, padX, 24f + 23f + 6f, inner, 42f);

            var blurb = UIFactory.CreateText(rect, "Blurb", Blurbs[index], 21, UITheme.TextDim, TextAnchor.UpperLeft, UIFonts.Body);
            UIFactory.PlaceTopLeft(blurb.rectTransform, padX, 24f + 23f + 6f + 42f + 6f, inner, 62f);

            _cardBorders[index] = border;
            _cardFills[index] = fill;
        }

        private void BuildCompany()
        {
            RectTransform eyebrow = UIFactory.CreateEyebrow(transform, "CompanyEyebrow", "Company");
            UIFactory.PlaceTopRight(eyebrow, Side, BodyTop, CompanyWidth, 24f);

            RectTransform panel = UIFactory.CreateHairlinePanel(transform, "CompanyPanel", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _companyPanel = (RectTransform)panel.parent;
            UIFactory.PlaceTopRight(_companyPanel, Side, BodyTop + 24f + 18f, CompanyWidth, 200f);

            _session = UIFactory.CreateText(panel, "SessionLabel", string.Empty, UITheme.Body, UITheme.Text, TextAnchor.MiddleLeft, UIFonts.Body);

            var listGo = new GameObject("FriendList", typeof(RectTransform));
            listGo.transform.SetParent(panel, false);
            _friendList = (RectTransform)listGo.transform;
            UIFactory.AddVerticalLayout(_friendList, spacing: 6f, padding: new RectOffset(0, 0, 0, 0), childAlignment: TextAnchor.UpperLeft);
            _friendList.gameObject.SetActive(false);

            float inner = CompanyWidth - 2f - 48f;
            _invite = UIFactory.CreateButton(panel, "InviteButton", "Invite Friend", OnInviteClicked, new Vector2(inner, 52f),
                ButtonKind.Secondary, "STEAM", UITheme.Body).gameObject;
        }

        private void BuildFooter()
        {
            var rule = UIFactory.CreateImage(transform, "FooterRule", UITheme.Line);
            UIFactory.PlaceBottomStretch(rule.rectTransform, Side, Side, FooterButtonBottom + SetOutHeight + 22f, 1f);

            var back = UIFactory.CreateButton(transform, "BackButton", "\u2039 Back to Menu", BackToMenu, new Vector2(300f, 64f), ButtonKind.Quiet);
            UIFactory.PlaceBottomLeft(back.GetComponent<RectTransform>(), Side, FooterButtonBottom + 6f, 300f, 64f);

            _setOut = UIFactory.CreateButton(transform, "SetOutButton", "Set Out", SetOut, new Vector2(SetOutWidth, SetOutHeight),
                ButtonKind.Primary, "ENTER", 32).gameObject;
            UIFactory.PlaceBottomRight(_setOut.GetComponent<RectTransform>(), Side, FooterButtonBottom, SetOutWidth, SetOutHeight);

            // A friend who joined follows the host into the raid, so it gets a sentence where Set Out would be.
            _waiting = UIFactory.CreateText(transform, "WaitingLabel", "Waiting for the host to set out.", 25, UITheme.TextDim,
                TextAnchor.MiddleRight, UIFonts.BodyItalic);
            UIFactory.PlaceBottomRight(_waiting.rectTransform, Side, FooterButtonBottom, SetOutWidth, SetOutHeight);

            _summary = UIFactory.CreateText(transform, "EraSummary", string.Empty, UITheme.Label, UITheme.TextDim, TextAnchor.MiddleRight, UIFonts.Mono);
            UIFactory.PlaceBottomRight(_summary.rectTransform, Side + SetOutWidth + 26f, FooterButtonBottom + (SetOutHeight - 24f) * 0.5f, 800f, 24f);
        }

        /// <summary>Refreshed every time the screen appears, so it reflects the raid just finished.</summary>
        protected override void OnShown()
        {
            EventManager.Instance?.UnsubscribeFromAllEvents(this);
            EventManager.Instance?.Subscribe(this, (CoopChanged e) => Refresh());
            Refresh();
        }

        private void OnDisable() => EventManager.Instance?.UnsubscribeFromAllEvents(this);

        /// <summary>
        /// Steam's own invite dialog when its overlay is hooked in; otherwise (a build started outside
        /// Steam, the usual way to test) a list of online friends, each invited with one click.
        /// </summary>
        private void OnInviteClicked()
        {
            ICoopSession coop = GameServices.Coop;
            if (coop == null)
                return;
            if (coop.OverlayAvailable)
            {
                coop.InviteFriends();
                return;
            }

            bool show = !_friendList.gameObject.activeSelf;
            _friendList.gameObject.SetActive(show);
            if (show)
                BuildFriendList(coop);
            LayoutCompany();
        }

        private void BuildFriendList(ICoopSession coop)
        {
            for (int i = _friendList.childCount - 1; i >= 0; i--)
                Destroy(_friendList.GetChild(i).gameObject);

            RectTransform heading = UIFactory.CreateEyebrow(_friendList, "Heading", "Online friends", withRule: false);
            var friends = coop.OnlineFriends();
            if (friends.Count == 0)
            {
                var none = UIFactory.CreateText(_friendList, "None", "No Steam friends are online.", UITheme.Body - 2, UITheme.TextDim,
                    TextAnchor.MiddleLeft, UIFonts.BodyItalic);
                none.rectTransform.sizeDelta = new Vector2(0f, 30f);
                _friendListHeight = heading.sizeDelta.y + 6f + 30f;
                return;
            }

            // Sized to the rows rather than fixed, so the panel stays around every name. The layout
            // group does not control child heights, so a ContentSizeFitter would read zero.
            int rows = Mathf.Min(friends.Count, MaxFriendsShown);
            _friendListHeight = heading.sizeDelta.y + rows * (44f + 6f);

            for (int i = 0; i < rows; i++)
            {
                ulong id = friends[i].Id;
                UIFactory.CreateButton(_friendList, $"Invite_{id}", friends[i].Name,
                    () => coop.InviteFriend(id), new Vector2(0f, 44f), ButtonKind.Secondary, "INVITE", 21);
            }
        }

        /// <summary>
        /// Stacks the company panel's parts top to bottom and sizes the panel around them: the friend
        /// list and the Invite button come and go, so the height cannot be fixed.
        /// </summary>
        private void LayoutCompany()
        {
            const float inner = CompanyWidth - 2f - 48f;
            float y = CompanyPadding;

            UIFactory.PlaceTopLeft(_session.rectTransform, 24f, y, inner, 34f);
            y += 34f + CompanyGap;

            if (_friendList.gameObject.activeSelf)
            {
                UIFactory.PlaceTopLeft(_friendList, 24f, y, inner, _friendListHeight);
                y += _friendListHeight + CompanyGap;
            }

            if (_invite.activeSelf)
            {
                UIFactory.PlaceTopLeft(_invite.GetComponent<RectTransform>(), 24f, y, inner, 52f);
                y += 52f + CompanyGap;
            }

            float height = y - CompanyGap + CompanyPadding + 2f;
            UIFactory.PlaceTopRight(_companyPanel, Side, BodyTop + 24f + 18f, CompanyWidth, height);
        }

        /// <summary>Only the host sets out; a friend who joined follows it into the raid.</summary>
        private static void SetOut()
        {
            if (GameServices.Coop != null && !GameServices.Coop.IsInSession)
                GameServices.Coop.PlaySolo();
            GameServices.GameState.ChangeState(GameState.Playing);
        }

        private static void BackToMenu()
        {
            GameServices.Coop?.Leave();
            GameServices.GameState.ChangeState(GameState.MainMenu);
        }

        private void Refresh()
        {
            if (_session == null)
                return; // Changed can arrive before the screen is built

            ICoopSession coop = GameServices.Coop;
            bool isHostOrSolo = GameServices.IsSessionAuthority();
            _setOut.SetActive(isHostOrSolo);
            _waiting.gameObject.SetActive(!isHostOrSolo);
            _invite.SetActive(coop != null && coop.CanInvite);
            if (!_invite.activeSelf)
                _friendList.gameObject.SetActive(false);
            _session.text = coop == null ? string.Empty : coop.Status;
            LayoutCompany();

            if (_lair == null)
                _lair = FindFirstObjectByType<LairHubManager>();

            if (_lair == null)
            {
                _debt.text = "No lair in this scene.";
                _gold.text = string.Empty;
                _lastRaid.text = string.Empty;
                _debtNote.text = string.Empty;
                _summary.text = string.Empty;
                return;
            }

            LairState state = _lair.GetLairState();
            _debt.text = state.TotalDebt.ToString("N0");
            _gold.text = state.AccumulatedGold.ToString("N0");
            _debtNote.text = UITheme.Tracked($"Debt grows by {_lair.DebtIncreasePerSession:N0} each raid it stands", UITheme.Label);

            // The coop session does not say how many are in the Lair, so the summary names the Age only.
            _summary.text = UITheme.Tracked(Label(state.SelectedEra), UITheme.Label);

            string value = ColorUtility.ToHtmlStringRGB(UITheme.Value);
            string leftBehind = _lair.LastRaidLeftBehind == 0 ? string.Empty
                : $" \u00B7 {_lair.LastRaidLeftBehind} left behind";
            _lastRaid.text = _lair.LastRaidWorth < 0f ? "No raid yet."
                : _lair.LastRaidWorth > 0f ? $"Brought home <color=#{value}>{_lair.LastRaidWorth:N0} coin</color>{leftBehind}"
                : $"Came home with nothing{leftBehind}";

            ShowSelectedEra(state.SelectedEra);
        }

        /// <summary>The chosen Age gets a lighter fill, a 2px verdigris outline, and "Setting out" where its date was.</summary>
        private void ShowSelectedEra(HistoricalEra selected)
        {
            for (int i = 0; i < Eras.Length; i++)
            {
                bool chosen = Eras[i] == selected;
                _cardBorders[i].color = chosen ? UITheme.Interactive : UITheme.Surface;
                _cardFills[i].color = chosen ? UITheme.SurfaceHi : UITheme.Surface;
                _cardDates[i].text = UITheme.Tracked(chosen ? "Setting out" : Dates[i], 14);
                _cardDates[i].color = chosen ? UITheme.Interactive : UITheme.TextFaint;
            }
        }

        private void SelectEra(HistoricalEra era)
        {
            if (_lair == null)
                _lair = FindFirstObjectByType<LairHubManager>();

            _lair?.SelectEra(era);
            Refresh();
        }

        private static string Label(HistoricalEra era)
        {
            switch (era)
            {
                case HistoricalEra.BronzeAge: return "Bronze Age";
                case HistoricalEra.HighMedieval: return "High Medieval";
                case HistoricalEra.LateMedieval: return "Late Medieval";
                case HistoricalEra.AgeOfPowder: return "Age of Powder";
                default: return era.ToString();
            }
        }
    }
}
