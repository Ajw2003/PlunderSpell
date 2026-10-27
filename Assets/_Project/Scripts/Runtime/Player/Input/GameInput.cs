using UnityEngine;

namespace Player
{
    /// <summary>
    /// The one shared, enabled instance of the input actions, so every script reads the same keys.
    /// Created on first use; disposed when the app quits or the domain reloads (the generated actions
    /// are unmanaged and leak otherwise). Callers must not dispose it.
    /// </summary>
    public static class GameInput
    {
        private static PlayerInputs s_actions;

        public static PlayerInputs Actions
        {
            get
            {
                if (s_actions == null)
                {
                    s_actions = new PlayerInputs();
                    s_actions.Enable();
                }
                return s_actions;
            }
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void DisposeOnDomainReload() =>
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += Dispose;
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register()
        {
            // Fresh Play session with domain reload off: drop any instance left from the last one.
            Dispose();
            Application.quitting += Dispose;
        }

        private static void Dispose()
        {
            s_actions?.Disable();
            s_actions?.Dispose();
            s_actions = null;
        }
    }
}
