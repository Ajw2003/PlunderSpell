using Plunderspell.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Plunderspell.UI
{
    /// <summary>Creates the event system, gameplay services and UI hierarchy the first time any scene loads.</summary>
    public static class UIBootstrapper
    {
        private static UIRoot _uiRootInstance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_uiRootInstance != null)
            {
                return;
            }

            GameServices.Initialize();
            EnsureEventSystem();
            _uiRootInstance = CreateUIRoot();
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Object.DontDestroyOnLoad(go);
        }

        private static UIRoot CreateUIRoot()
        {
            var go = new GameObject("UIRoot", typeof(UIRoot), typeof(GameFlowInput),
                typeof(CursorLockPolicy), typeof(PausePolicy));
            Object.DontDestroyOnLoad(go);
            return go.GetComponent<UIRoot>();
        }
    }
}
