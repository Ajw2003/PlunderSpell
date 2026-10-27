using UnityEngine;

namespace Plunderspell.Audio
{
    /// <summary>
    /// Starts the audio layer the first time any scene loads, the way UIBootstrapper starts the UI. The
    /// SoundBank is a preloaded asset (set by Plunderspell > Audio > Rebuild SoundBank), so a build has
    /// it in memory and needs no Resources folder. The Editor only has it if something loaded it, so
    /// there it is loaded from the preloaded list.
    /// </summary>
    public static class AudioBootstrapper
    {
        private static AudioDirector s_root;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (s_root != null)
                return;

            SoundBank bank = FindBank();
            if (bank == null)
            {
                Debug.LogWarning("[Audio] No SoundBank is loaded; the game will be silent. Run Plunderspell > Audio > Rebuild SoundBank.");
                return;
            }

            var go = new GameObject("AudioRoot");
            Object.DontDestroyOnLoad(go);
            s_root = go.AddComponent<AudioDirector>();
            s_root.Initialize(bank);
            AudioOutputDevices.ApplySaved();
        }

        /// <summary>The SoundBank, or null if there is none.</summary>
        public static SoundBank FindBank()
        {
            SoundBank[] banks = Resources.FindObjectsOfTypeAll<SoundBank>();
            if (banks.Length > 0)
                return banks[0];
#if UNITY_EDITOR
            foreach (Object preloaded in UnityEditor.PlayerSettings.GetPreloadedAssets())
                if (preloaded is SoundBank bank)
                    return bank;
#endif
            return null;
        }
    }
}
