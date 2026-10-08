using System.Collections;
using Code.Scripts.EventSystems;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Plunderspell.UI
{
    /// <summary>
    /// A plain camera that renders only while no other camera does: the main menu and the Lair have
    /// no player, so no camera at all, and the Editor's Game view printed "No cameras rendering"
    /// under a menu that worked (#131). It draws nothing but a dark backdrop and switches itself off
    /// the moment the raid's player camera exists. Created by <see cref="UIRoot"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class BackdropCamera : MonoBehaviour
    {
        /// <summary>The clear colour: a warm near-black. The night fog renderer feature runs on every
        /// camera, this one too, so on screen it reads as the fog's dim brown.</summary>
        public static readonly Color BackdropColour = new Color(0.05f, 0.04f, 0.035f);

        private Camera _camera;

        /// <summary>True while this camera is the one rendering the screen.</summary>
        public bool IsRendering => _camera != null && _camera.enabled;

        private void Awake()
        {
            _camera = gameObject.AddComponent<Camera>();
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = BackdropColour;
            _camera.cullingMask = 0;
            _camera.depth = -100f;
            UpdateEnabled();
        }

        // A camera comes or goes when a scene loads or unloads or the game changes mode (a raid starts, a player dies), so those are the moments to look (and once more a frame later, when Unity has enabled
        // the new camera), instead of counting cameras every frame (#304).
        private void OnEnable()
        {
            EventManager.Instance?.Subscribe(this, (Plunderspell.Core.GameStateChanged e) => RecheckSoon());
            SceneManager.sceneLoaded += OnSceneChanged;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDisable()
        {
            EventManager.Instance?.UnsubscribeFromAllEvents(this);
            SceneManager.sceneLoaded -= OnSceneChanged;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private void OnSceneChanged(Scene scene, LoadSceneMode mode) => RecheckSoon();

        private void OnSceneUnloaded(Scene scene) => RecheckSoon();

        /// <summary>Decides now whether this camera should render, and again next frame.</summary>
        public void RecheckSoon()
        {
            UpdateEnabled();
            if (isActiveAndEnabled)
                StartCoroutine(RecheckNextFrame());
        }

        private IEnumerator RecheckNextFrame()
        {
            yield return null;
            UpdateEnabled();
        }

        private void UpdateEnabled()
        {
            // allCamerasCount counts enabled cameras, this one included while it is on.
            int others = Camera.allCamerasCount - (_camera.enabled ? 1 : 0);
            _camera.enabled = others == 0;
        }
    }
}
