namespace Plunderspell.Core
{
    public enum GameState
    {
        MainMenu,
        Lair,
        Playing,
        Paused,
        Settings,
        GameOver,
        Victory,
        /// <summary>Walking the Lair room: cursor captured, no screen. Appended last so saved ints keep their meaning.</summary>
        LairRoom
    }
}
