namespace Plunderspell.Core
{
    /// <summary>
    /// What the menus need from the network session, without the UI assembly referencing PurrNet
    /// or Steamworks. <c>RogueAi.Net.CoopSession</c> implements it and registers itself in
    /// <see cref="GameServices.Coop"/>; scenes without one (the benches) leave it null and play
    /// offline.
    /// </summary>
    public interface ICoopSession
    {
        /// <summary>One line for the menu: what the session is doing, or why co-op is unavailable.</summary>
        string Status { get; }

        /// <summary>True while hosting or joined, solo included.</summary>
        bool IsInSession { get; }

        /// <summary>True while hosting a Steam lobby that friends can be invited to.</summary>
        bool CanInvite { get; }

        /// <summary>Raised whenever <see cref="Status"/> or the flags above change.</summary>
        event System.Action Changed;

        /// <summary>Starts a session only this machine can join.</summary>
        void PlaySolo();

        /// <summary>Starts a session friends can join: a Steam lobby when Steam is running, otherwise
        /// a LAN host.</summary>
        void HostCoop();

        /// <summary>Opens Steam's invite dialog for the current lobby.</summary>
        void InviteFriends();

        /// <summary>Ends the session and leaves any lobby.</summary>
        void Leave();
    }
}
