using UnityEngine;

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

        private void LateUpdate() => UpdateEnabled();

        private void UpdateEnabled()
        {
            // allCamerasCount counts enabled cameras, this one included while it is on.
            int others = Camera.allCamerasCount - (_camera.enabled ? 1 : 0);
            _camera.enabled = others == 0;
        }
    }
}
