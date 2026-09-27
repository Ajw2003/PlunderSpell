# #127: spell bursts in a standalone build, 2026-09-26

A Development build of `RaidScene` (`Build/Issue127/`, not committed: builds are gitignored), run
windowed and driven through the Pipeline runtime with `unity command --runtime Plunderspell eval`.
A solo raid was started through `GameServices.GameState` by reflection, then each spell's visual
was raised with `SpellCastingSystem.AnnounceForTesting`, 3 m in front of the camera, the burst put
at a quarter of its life, and the screen captured.

| Capture | Burst material |
|---|---|
| `build-Frango.png`, `build-Ignis.png`, `build-Somnus.png` (before) | `Hidden/InternalErrorShader`, unsupported: a magenta sphere |
| `build-fixed-Frango.png`, `build-fixed-Ignis.png`, `build-fixed-Somnus.png` (after) | `Universal Render Pipeline/Unlit`, supported, queue 3000 |

In the "before" captures the player had already been killed by a guard, so "YOU DIED" covers the
middle. Frango's colour is a pale blue-white, which the warm fog turns white.

`make-burst-material.cs` made `Assets/_Project/Resources/SpellBurst.mat`.
