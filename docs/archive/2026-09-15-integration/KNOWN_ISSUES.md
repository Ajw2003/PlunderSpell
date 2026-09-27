# Known issues found during the 2026-09-15 branch integration

## PlayMode screenshot test hung/threw under `-batchmode` — `WaitForEndOfFrame` is not supported in batchmode at all

**Symptom:** `Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode ...` either
hung indefinitely (observed: 29 minutes with zero further log output before being killed) or,
on a later attempt, exited fast with the test marked failed. Both are the same underlying cause
surfacing two different ways.

**Original (wrong) theory:** the first pass at this diagnosis blamed `-nographics` specifically,
reasoning that `WaitForEndOfFrame` needs a real rendering surface. That was disproven directly:
dropping `-nographics` (keeping the real GPU) reproduced the *exact same* indefinite hang at the
identical point in the log. The real constraint is `-batchmode` itself, independent of graphics.

**Actual root cause, confirmed from Unity's own error text** (found by writing a minimal
step-by-step diagnostic `[UnityTest]` and bisecting): Unity Test Framework raises this directly
as a test failure once it detects a `WaitForEndOfFrame` yield during a batchmode run:

```
Unhandled log message: '[Exception] Exception: UnityTest yielded WaitForEndOfFrame,
which is not evoked in batchmode.'
```

`-batchmode` never presents a frame — there's no window to finish presenting — so anything that
yields on frame-presentation (`WaitForEndOfFrame`, and by extension
`ScreenCapture.CaptureScreenshot`, which schedules its capture for end-of-frame) either blocks
forever waiting for a callback that will never fire, or gets caught by the test framework's log
check and fails the test, depending on scheduling timing. This is a fundamental incompatibility,
not a flag to tune.

**Second, independent issue found in the same test:** the UI's root `Canvas` is created in
`RenderMode.ScreenSpaceOverlay` (see [UIFactory.CreateRootCanvas](../../../Assets/_Project/Scripts/Runtime/UI/UIFactory.cs)).
Overlay-mode canvases composite directly to the display and are not captured by rendering any
particular `Camera` to a `RenderTexture` — so even a from-scratch headless-safe capture using
`Camera.Render()` would produce a blank image for this UI unless the canvas is temporarily
switched to `ScreenSpaceCamera` mode against the capture camera.

**Fix applied** (in [UIScreenshotPlayModeTests.cs](../../../Assets/_Project/Scripts/Tests/PlayMode/UIScreenshotPlayModeTests.cs)):
replaced `WaitForEndOfFrame` + `ScreenCapture.CaptureScreenshot` with a dedicated capture
`Camera` rendering synchronously to a `RenderTexture` (`Camera.Render()` + `ReadPixels` +
`EncodeToPNG`), with the target `Canvas` temporarily switched to `ScreenSpaceCamera` mode
pointed at that camera for the duration of the test. No waiting on frame presentation at all,
so it's compatible with batchmode. Verified: all 34 PlayMode tests pass
(`results_ui-system-playmode.xml`, `total="34" passed="34" failed="0"`), and the 7 produced
screenshots (`UI_Verification_Screenshots/`) were visually inspected — real rendered UI (title,
menu buttons, etc.), not blank frames.

**Process lesson, independent of the Unity-side cause:** the agent running this integration
initially ran the long Unity invocation as a *synchronous, foreground* command with a 10-minute
timeout and no streamed output. A blocking foreground call gives no visibility while it runs and
cannot be interrupted or checked on mid-flight — the user had to kill the task manually to regain
the agent's attention, and had no way to tell a real hang apart from normal long-running work.
Fix applied going forward: every Unity batchmode invocation in this integration runs in the
background with active log-tailing (via a Monitor watching for progress/error markers) instead
of a blind synchronous wait, so a stall is visible and actionable within seconds, not minutes.
