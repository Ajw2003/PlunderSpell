namespace Plunderspell.Core
{
    /// <summary>Process-wide access point for the plain-C# gameplay systems the UI binds to.</summary>
    public static class GameServices
    {
        public static GameStateManager GameState { get; private set; }
        public static PlayerStats PlayerStats { get; private set; }
        public static ExtractionController Extraction { get; private set; }
        public static bool IsInitialized { get; private set; }

        /// <summary>
        /// True only while the player is in control of the world. Input-reading components gate on
        /// this so a menu cannot be navigated with the character walking around behind it.
        /// Uninitialised counts as not playing; <see cref="Initialize"/> runs from
        /// <c>RuntimeInitializeOnLoadMethod</c> before any scene component's Update.
        /// </summary>
        /// <summary>
        /// True on the machine that owns the simulation: the host, or anyone playing offline. Pausing
        /// only freezes time there. The raid replaces this with its network check once it exists;
        /// until then (menus, benches) this machine is its own authority.
        /// </summary>
        public static System.Func<bool> IsSessionAuthority = () => true;

        /// <summary>The network session the menus drive, or null in scenes that play offline.</summary>
        public static ICoopSession Coop { get; set; }

        public static bool IsPlaying =>
            GameState != null && GameState.CurrentState == Core.GameState.Playing;

        /// <summary>The one "is this a safe place right now" rule: <see cref="GameStateManager.InSafePlace"/>. Installed as Interfaces.Damage.SafePlace, which Damage.Apply and LootPickup breakage check.</summary>
        public static bool IsSafePlace => GameState != null && GameState.InSafePlace;

        public static void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            GameState = new GameStateManager();
            PlayerStats = new PlayerStats(maxMana: 100);
            Extraction = new ExtractionController(extractionDurationSeconds: 8f);
            Interfaces.Damage.SafePlace = () => IsSafePlace;
            IsInitialized = true;
        }
    }
}
