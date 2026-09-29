using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// The raid's stand-in listener: on exactly while no enabled camera carries an enabled listener,
    /// so the raid has one listener before this machine's player spawns and never two after.
    /// Created by <see cref="RaidDirector"/>. See docs/4-systems/audio.md, "The listener".
    /// </summary>
    [DisallowMultipleComponent]
    public class RaidListenerFallback : MonoBehaviour
    {
        private const int CameraBufferSize = 16;

        private readonly Camera[] _cameras = new Camera[CameraBufferSize];
        private AudioListener _listener;

        /// <summary>True while this fallback is the listener the raid hears through.</summary>
        public bool IsListening => _listener != null && _listener.enabled;

        private void Awake()
        {
            _listener = gameObject.AddComponent<AudioListener>();
            UpdateEnabled();
        }

        private void LateUpdate() => UpdateEnabled();

        private void UpdateEnabled()
        {
            _listener.enabled = !AnotherCameraListens();
        }

        // Counts only listeners on cameras: every other listener a raid creates (the player camera,
        // the spectator camera) lives on one, and Camera.GetAllCameras returns only enabled cameras
        // without allocating.
        private bool AnotherCameraListens()
        {
            int count = Camera.GetAllCameras(_cameras);
            for (int i = 0; i < count; i++)
            {
                Camera camera = _cameras[i];
                if (camera == null || camera.gameObject == gameObject)
                    continue;
                if (camera.TryGetComponent(out AudioListener ears) && ears.enabled)
                    return true;
            }
            return false;
        }
    }
}
