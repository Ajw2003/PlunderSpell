using System.Collections;
using System.IO;
using NUnit.Framework;
using Plunderspell.Core;
using Plunderspell.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace Plunderspell.Tests.PlayMode
{
    /// <summary>
    /// Automated visual proof: cycles through every GameState and screenshots the result to
    /// ./UI_Verification_Screenshots/. Run via Window > General > Test Runner (Play Mode tab) in the
    /// Editor, or headless with:
    /// Unity -batchmode -projectPath &lt;path&gt; -runTests -testPlatform PlayMode
    ///   -testResults UI_Verification_Screenshots/playmode-results.xml -logFile - -quit
    ///
    /// Note the absence of -nographics: this test needs a real graphics device and skips itself
    /// without one, so a -nographics run leaves the committed screenshots untouched.
    ///
    /// Captures via a dedicated camera rendered synchronously to a RenderTexture rather than
    /// ScreenCapture.CaptureScreenshot/WaitForEndOfFrame: batchmode never presents a frame, so
    /// anything waiting on frame presentation hangs or throws ("UnityTest yielded
    /// WaitForEndOfFrame, which is not evoked in batchmode."). The UI's Canvas also renders in
    /// ScreenSpaceOverlay mode (see UIFactory.CreateRootCanvas), which composites straight to the
    /// display and would not appear in a plain camera render — so the canvas is switched to
    /// ScreenSpaceCamera against the capture camera for the duration of this test.
    /// </summary>
    public class UIScreenshotPlayModeTests
    {
        private const string OutputFolder = "UI_Verification_Screenshots";
        private const int CaptureWidth = 1920;
        private const int CaptureHeight = 1080;

        private Camera _captureCamera;
        private RenderTexture _captureRenderTexture;

        [UnityTest]
        public IEnumerator CapturesAllUIScreens()
        {
            // Under -nographics there is no graphics device, so RenderTexture.Create fails and every
            // ReadPixels returns a blank frame. Running on would overwrite the committed reference
            // screenshots with grey rectangles, so this capture only runs where it can actually render.
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Assert.Ignore("No graphics device (-nographics); UI capture would overwrite the " +
                              "committed screenshots with blank frames.");
            }

            var directory = Path.Combine(Directory.GetCurrentDirectory(), OutputFolder);
            Directory.CreateDirectory(directory);

            yield return null;

            var uiRoot = Object.FindFirstObjectByType<UIRoot>();
            Assert.IsNotNull(uiRoot, "UIRoot was not bootstrapped into the play mode scene.");

            var canvas = uiRoot.GetComponentInChildren<Canvas>();
            Assert.IsNotNull(canvas, "UIRoot has no Canvas to capture.");

            SetUpCaptureCamera(canvas);

            yield return CaptureState(GameState.MainMenu, "01_MainMenu", directory);
            yield return CaptureState(GameState.Playing, "02_HUD", directory);
            yield return CaptureState(GameState.Paused, "03_PauseMenu", directory);

            yield return CaptureState(GameState.Settings, "05_Settings", directory);

            GameServices.PlayerStats.AddGold(250);
            yield return CaptureState(GameState.Victory, "06_Victory", directory);
            yield return CaptureState(GameState.GameOver, "07_GameOver", directory);

            TearDownCaptureCamera();

            Assert.IsTrue(File.Exists(Path.Combine(directory, "01_MainMenu.png")), "Expected a screenshot file to have been written.");
        }

        private void SetUpCaptureCamera(Canvas canvas)
        {
            var cameraGo = new GameObject("UIScreenshotCaptureCamera");
            _captureCamera = cameraGo.AddComponent<Camera>();
            _captureCamera.clearFlags = CameraClearFlags.SolidColor;
            _captureCamera.backgroundColor = Color.black;
            _captureCamera.cullingMask = ~0;

            _captureRenderTexture = new RenderTexture(CaptureWidth, CaptureHeight, 24);
            _captureCamera.targetTexture = _captureRenderTexture;

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _captureCamera;
            canvas.planeDistance = 1f;
        }

        private void TearDownCaptureCamera()
        {
            if (_captureCamera != null)
            {
                Object.Destroy(_captureCamera.gameObject);
            }

            if (_captureRenderTexture != null)
            {
                _captureRenderTexture.Release();
                Object.Destroy(_captureRenderTexture);
            }
        }

        private IEnumerator CaptureState(GameState state, string fileName, string directory)
        {
            GameServices.GameState.ChangeState(state);
            yield return null;

            _captureCamera.Render();

            var previousActive = RenderTexture.active;
            RenderTexture.active = _captureRenderTexture;
            var texture = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0);
            texture.Apply();
            RenderTexture.active = previousActive;

            File.WriteAllBytes(Path.Combine(directory, fileName + ".png"), texture.EncodeToPNG());
            Object.Destroy(texture);

            yield return null;
        }
    }
}
