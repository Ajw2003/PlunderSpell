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

        private Text _debt;
        private Text _gold;
        private Text _era;
        private Text _lastRaid;
        private Text _session;
        private GameObject _setOut;
        private GameObject _invite;
        private RectTransform _friendList;
        private const int MaxFriendsShown = 12;
        private LairHubManager _lair;

        protected override void OnBuild()
        {
            UIFactory.CreateFullStretchPanel(transform, "Background", UITheme.Background);

            var title = UIFactory.CreateText(transform, "Title", "THE LAIR", UITheme.TitleFontSize, UITheme.Accent);
            title.rectTransform.anchorMin = new Vector2(0.5f, 0.82f);
            title.rectTransform.anchorMax = new Vector2(0.5f, 0.82f);
            title.rectTransform.sizeDelta = new Vector2(900f, 110f);

            _debt = BuildStat("DebtLabel", 0.74f);
            _gold = BuildStat("GoldLabel", 0.695f);
            _lastRaid = BuildStat("LastRaidLabel", 0.64f);
            // Below the era buttons, not above them, or the button row covers it.
            _era = BuildStat("EraLabel", 0.31f);

            var eraRowGo = new GameObject("EraRow", typeof(RectTransform));
            eraRowGo.transform.SetParent(transform, false);
            var eraRow = (RectTransform)eraRowGo.transform;
            eraRow.anchorMin = new Vector2(0.5f, 0.5f);
            eraRow.anchorMax = new Vector2(0.5f, 0.5f);
            eraRow.sizeDelta = new Vector2(460f, 240f);
            eraRow.anchoredPosition = new Vector2(0f, 20f);
            UIFactory.AddVerticalLayout(eraRow, spacing: 10f, padding: new RectOffset(0, 0, 0, 0));

            foreach (HistoricalEra era in Eras)
            {
                HistoricalEra captured = era;
                UIFactory.CreateButton(eraRow, $"Era_{era}", Label(era),
                    () => SelectEra(captured), new Vector2(460f, 44f));
            }

            var actionsGo = new GameObject("Actions", typeof(RectTransform));
            actionsGo.transform.SetParent(transform, false);
            var actions = (RectTransform)actionsGo.transform;
            actions.anchorMin = new Vector2(0.5f, 0.18f);
            actions.anchorMax = new Vector2(0.5f, 0.18f);
            actions.sizeDelta = new Vector2(460f, 190f);
            UIFactory.AddVerticalLayout(actions, spacing: 12f, padding: new RectOffset(0, 0, 0, 0));

            _setOut = UIFactory.CreateButton(actions, "SetOutButton", "Set Out", SetOut, new Vector2(460f, 56f)).gameObject;
            _invite = UIFactory.CreateButton(actions, "InviteButton", "Invite Friend", OnInviteClicked,
                new Vector2(460f, 46f)).gameObject;
            UIFactory.CreateButton(actions, "BackButton", "Back to Menu", BackToMenu, new Vector2(460f, 46f));

            _session = BuildStat("SessionLabel", 0.05f);

            _friendList = UIFactory.CreatePanel(transform, "FriendList", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(340f, 620f), new Vector2(-200f, 0f), UITheme.PanelBackground);
            UIFactory.AddVerticalLayout(_friendList, spacing: 6f, padding: new RectOffset(10, 10, 10, 10));
            _friendList.gameObject.SetActive(false);
        }

        private Text BuildStat(string name, float anchorY)
        {
            var text = UIFactory.CreateText(transform, name, "", UITheme.BodyFontSize, UITheme.TextSecondary);
            text.rectTransform.anchorMin = new Vector2(0.5f, anchorY);
            text.rectTransform.anchorMax = new Vector2(0.5f, anchorY);
            text.rectTransform.sizeDelta = new Vector2(900f, 34f);
            return text;
        }

        /// <summary>Refreshed every time the screen appears, so it reflects the raid just finished.</summary>
        protected override void OnShown()
        {
            if (GameServices.Coop != null)
            {
                GameServices.Coop.Changed -= Refresh;
                GameServices.Coop.Changed += Refresh;
            }
            Refresh();
        }

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
        }

        private void BuildFriendList(ICoopSession coop)
        {
            for (int i = _friendList.childCount - 1; i >= 0; i--)
                Destroy(_friendList.GetChild(i).gameObject);

            UIFactory.CreateText(_friendList, "Heading", "Invite a friend", UITheme.BodyFontSize, UITheme.Accent)
                .rectTransform.sizeDelta = new Vector2(320f, 40f);
            var friends = coop.OnlineFriends();
            if (friends.Count == 0)
            {
                UIFactory.CreateText(_friendList, "None", "No Steam friends are online.", UITheme.SmallFontSize, UITheme.TextSecondary);
                return;
            }

            // Sized to the rows rather than fixed, so the background stays behind every name. The
            // layout group does not control child heights, so a ContentSizeFitter would read zero.
            int rows = Mathf.Min(friends.Count, MaxFriendsShown);
            _friendList.sizeDelta = new Vector2(_friendList.sizeDelta.x, 20f + 40f + rows * (40f + 6f));

            for (int i = 0; i < rows; i++)
            {
                ulong id = friends[i].Id;
                UIFactory.CreateButton(_friendList, $"Invite_{id}", friends[i].Name,
                    () => coop.InviteFriend(id), new Vector2(320f, 40f));
            }
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
            _invite.SetActive(coop != null && coop.CanInvite);
            if (!_invite.activeSelf)
                _friendList.gameObject.SetActive(false);
            _session.text = coop == null ? string.Empty
                : isHostOrSolo ? coop.Status : "Waiting for the host to set out.";

            if (_lair == null)
                _lair = FindFirstObjectByType<LairHubManager>();

            if (_lair == null)
            {
                _debt.text = "No lair in this scene.";
                _gold.text = string.Empty;
                _era.text = string.Empty;
                _lastRaid.text = string.Empty;
                return;
            }

            LairState state = _lair.GetLairState();
            _debt.text = $"Debt owed: {state.TotalDebt:N0} coin";
            _gold.text = $"Banked: {state.AccumulatedGold:N0} coin";
            _era.text = $"Setting out in: {Label(state.SelectedEra)}";
            string leftBehind = _lair.LastRaidLeftBehind == 0 ? string.Empty
                : $" · {_lair.LastRaidLeftBehind} left behind";
            _lastRaid.text = _lair.LastRaidWorth < 0f ? string.Empty
                : _lair.LastRaidWorth > 0f ? $"Last raid: brought home {_lair.LastRaidWorth:N0} coin{leftBehind}"
                : $"Last raid: came home with nothing{leftBehind}";
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
