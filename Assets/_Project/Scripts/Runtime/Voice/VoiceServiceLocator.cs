using UnityEngine;

namespace Plunderspell.Voice
{
    /// <summary>
    /// Static access point for the active <see cref="IVoiceInputService"/>.
    ///
    /// Auto-registration policy (runs before the first scene loads):
    ///   - Headless/batch, no microphone, or no speech model  -> <see cref="MockVoiceInputService"/> (number keys)
    ///   - Otherwise, Editor or player                        -> <see cref="CombinedVoiceInputService"/> (speech + number keys)
    ///
    /// Any code may override the choice by calling <see cref="Register"/> before use
    /// (tests do exactly this).
    /// </summary>
    public static class VoiceServiceLocator
    {
        private static IVoiceInputService _current;

        /// <summary>The active provider. Auto-initialises on first access if nothing registered.</summary>
        public static IVoiceInputService Current
        {
            get
            {
                if (_current == null)
                    AutoRegister();
                return _current;
            }
        }

        /// <summary>True once a provider has been chosen.</summary>
        public static bool HasService => _current != null;

        /// <summary>Explicitly install a provider (overrides auto-registration).</summary>
        public static void Register(IVoiceInputService service)
        {
            _current = service;
            Debug.Log($"[VoiceServiceLocator] Registered provider: {service?.GetType().Name ?? "null"}");
        }

        /// <summary>Drop the current provider (mainly for test teardown).</summary>
        public static void Clear()
        {
            if (_current != null && _current.IsListening)
                _current.StopListening();
            _current = null;
        }

        // NOTE: we deliberately do NOT auto-register at editor load — that would spawn a
        // hidden driver GameObject in every edit-mode session. Play-mode registration runs
        // below; edit-mode / test callers get a provider lazily via Current.

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RuntimeInit() => AutoRegister();

        private static void AutoRegister()
        {
            if (_current != null)
                return;

            if (CanUseSpeech())
            {
                _current = new CombinedVoiceInputService(new VoskVoiceInputService(),
                    MockVoiceInputService.GetOrCreate());
                Debug.Log("[VoiceServiceLocator] Auto-registered speech + keyboard casting.");
            }
            else
            {
                _current = MockVoiceInputService.GetOrCreate();
                Debug.Log("[VoiceServiceLocator] Auto-registered keyboard-only casting (headless/no-mic/no model).");
            }
        }

        /// <summary>
        /// The number-key casting fallback, whichever provider is current. It is always present:
        /// alone when there is no speech, or alongside it inside <see cref="CombinedVoiceInputService"/>.
        /// </summary>
        public static MockVoiceInputService Keyboard =>
            Current as MockVoiceInputService
            ?? (Current as CombinedVoiceInputService)?.Keyboard
            ?? MockVoiceInputService.GetOrCreate();

        private static bool CanUseSpeech()
        {
#if HEADLESS
            return false;
#else
            // Batch mode (tests, CI, dedicated server) must never depend on a microphone.
            if (Application.isBatchMode)
                return false;
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                return false;
            if (Microphone.devices == null || Microphone.devices.Length == 0)
                return false;
            return VoskVoiceInputService.IsModelInstalled;
#endif
        }
    }
}
