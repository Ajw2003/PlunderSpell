namespace Plunderspell.Core
{
    /// <summary>Process-wide access point for the plain-C# gameplay systems the UI binds to.</summary>
    public static class GameServices
    {
        public static GameStateManager GameState { get; private set; }
        public static PlayerStats PlayerStats { get; private set; }
        public static InventorySystem Inventory { get; private set; }
        public static ExtractionController Extraction { get; private set; }
        public static bool IsInitialized { get; private set; }

        public static void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            GameState = new GameStateManager();
            PlayerStats = new PlayerStats();
            Inventory = new InventorySystem(capacity: 20);
            Extraction = new ExtractionController(extractionDurationSeconds: 8f);
            IsInitialized = true;
        }
    }
}
