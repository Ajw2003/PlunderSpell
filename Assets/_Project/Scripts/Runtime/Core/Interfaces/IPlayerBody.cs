namespace Interfaces
{
    /// <summary>
    /// Marks a player's body. Lets systems that cannot reference the Player assembly (extraction,
    /// damage feedback) tell a player from a guard without guessing from a network identity.
    /// </summary>
    public interface IPlayerBody
    {
        /// <summary>True while the player is up and able to act.</summary>
        bool IsAlive { get; }
    }

    /// <summary>A player who can be down. Guards ignore a downed player entirely (#270).</summary>
    public interface IDownable
    {
        bool IsDown { get; }
    }

    /// <summary>The one check guards and the hue and cry share for "is this intruder down".</summary>
    public static class Downable
    {
        public static bool IsDown(UnityEngine.Component intruder)
            => intruder.TryGetComponent(out IDownable body) && body.IsDown;
    }
}
