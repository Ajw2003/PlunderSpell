using Plunderspell.Core;
using UnityEngine;

namespace Plunderspell.UI.Screens
{
    /// <summary>
    /// The pause menu's Invite Friend button and online-friends list (moved from the Lair screen, #359).
    /// Steam's own invite dialog when its overlay is hooked in; otherwise a list of online friends, each
    /// invited with one click. Shown only while a co-op session can invite.
    /// </summary>
    public class PauseInviteSection
    {
        // ponytail: six names fit under the buttons on a 1080 screen; scroll the list if more are wanted.
        private const int MaxFriendsShown = 6;
        private const float RowHeight = 44f;
        private const float RowGap = 6f;

        private readonly GameObject _button;
        private readonly RectTransform _list;

        public PauseInviteSection(RectTransform parent, Vector2 buttonSize)
        {
            _button = UIFactory.CreateButton(parent, "InviteButton", "Invite Friend", OnInviteClicked, buttonSize,
                ButtonKind.Secondary, "STEAM").gameObject;

            var listGo = new GameObject("FriendList", typeof(RectTransform));
            listGo.transform.SetParent(parent, false);
            _list = (RectTransform)listGo.transform;
            UIFactory.AddVerticalLayout(_list, RowGap, new RectOffset(0, 0, 0, 0), TextAnchor.UpperLeft);
            _list.gameObject.SetActive(false);
        }

        /// <summary>Shows the button only when a session can invite; closes the list otherwise.</summary>
        public void Refresh()
        {
            ICoopSession coop = GameServices.Coop;
            _button.SetActive(coop != null && coop.CanInvite);
            if (!_button.activeSelf)
                _list.gameObject.SetActive(false);
        }

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

            bool show = !_list.gameObject.activeSelf;
            _list.gameObject.SetActive(show);
            if (show)
                BuildFriendList(coop);
        }

        private void BuildFriendList(ICoopSession coop)
        {
            for (int i = _list.childCount - 1; i >= 0; i--)
                Object.Destroy(_list.GetChild(i).gameObject);

            RectTransform heading = UIFactory.CreateEyebrow(_list, "Heading", "Online friends", withRule: false);
            var friends = coop.OnlineFriends();
            if (friends.Count == 0)
            {
                var none = UIFactory.CreateText(_list, "None", "No Steam friends are online.", UITheme.Body - 2, UITheme.TextDim,
                    TextAnchor.MiddleLeft, UIFonts.BodyItalic);
                none.rectTransform.sizeDelta = new Vector2(0f, 30f);
                _list.sizeDelta = new Vector2(_list.sizeDelta.x, heading.sizeDelta.y + RowGap + 30f);
                return;
            }

            // Sized to the rows: the layout group does not control child heights, so a fitter would read zero.
            int rows = Mathf.Min(friends.Count, MaxFriendsShown);
            _list.sizeDelta = new Vector2(_list.sizeDelta.x, heading.sizeDelta.y + rows * (RowHeight + RowGap));

            for (int i = 0; i < rows; i++)
            {
                ulong id = friends[i].Id;
                UIFactory.CreateButton(_list, $"Invite_{id}", friends[i].Name,
                    () => coop.InviteFriend(id), new Vector2(0f, RowHeight), ButtonKind.Secondary, "INVITE", 21);
            }
        }
    }
}
