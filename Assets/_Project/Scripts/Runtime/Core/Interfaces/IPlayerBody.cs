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
}
