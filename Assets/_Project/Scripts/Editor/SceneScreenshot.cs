using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Renders the open scene from an arbitrary camera to a PNG. Shared by every screenshot forge so
    /// there is one capture path rather than a copy per tool.
    /// </summary>
    public static class SceneScreenshot
    {
        public const int DefaultWidth = 1600;
        public const int DefaultHeight = 900;

        /// <summary>True when this Unity can render. Batchmode with <c>-nographics</c> cannot, and
        /// every capture would silently write a blank frame.</summary>
        public static bool HasGraphicsDevice => SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;

        /// <summary>
        /// Renders from <paramref name="position"/>/<paramref name="rotation"/> and writes a PNG to
        /// <paramref name="filePath"/>. The temporary camera is destroyed before returning, so the
        /// scene is left exactly as it was found.
        /// </summary>
        public static void Capture(Vector3 position, Quaternion rotation, bool isOrthographic,
            float orthographicSize, string filePath, int width = DefaultWidth,
            int height = DefaultHeight)
        {
            var cameraGo = new GameObject("ScreenshotCamera");
            cameraGo.transform.SetPositionAndRotation(position, rotation);

            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.06f, 0.08f);
            camera.orthographic = isOrthographic;
            camera.orthographicSize = orthographicSize;
            camera.fieldOfView = 70f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 1000f;

            var renderTexture = new RenderTexture(width, height, 24);
            camera.targetTexture = renderTexture;
            camera.Render();

            RenderTexture previousActive = RenderTexture.active;
            RenderTexture.active = renderTexture;

            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            texture.Apply();

            RenderTexture.active = previousActive;
            File.WriteAllBytes(filePath, texture.EncodeToPNG());

            Object.DestroyImmediate(texture);
            camera.targetTexture = null;
            renderTexture.Release();
            Object.DestroyImmediate(renderTexture);
            Object.DestroyImmediate(cameraGo);
        }
    }
}
