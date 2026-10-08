namespace Plunderspell.Core
{
    public enum GameState
    {
        MainMenu = 0,
        // 1 was Lair, the Lair screen, removed in #359; the other values keep their numbers.
        Playing = 2,
        Paused = 3,
        Settings = 4,
        GameOver = 5,
        Victory = 6,
        /// <summary>Walking the Lair room: cursor captured, no screen. Appended last so saved ints keep their meaning.</summary>
        LairRoom = 7
    }
}
