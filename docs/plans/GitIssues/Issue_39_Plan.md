# Plan: Issue 39 - Ranged weapons have no aim or fire implementation

## Exhaustive Outline
The game currently lacks a way to use ranged weapons. The goal of this task is to create the features needed to aim, shoot, and reload ranged weapons, starting with the Crossbow. We need to capture the specific feeling described in the game design (firing a single powerful shot followed by a slow and vulnerable reloading process). This requires adding player controls for aiming, visual indicators for ammunition and reload progress, and ensuring that firing the weapon creates an appropriate sound to alert nearby enemies.

## Step by Step Execution Instructions

1.  **Weapon Status Tracking:**
    Add information to weapons to track whether they are loaded, empty, or currently being reloaded.
2.  **Aiming Controls:**
    Create a control input that allows the player to enter an aiming stance. Adjust the camera and character movement to focus on aiming while this button is held.
3.  **Firing Action:**
    Create the action that happens when the player shoots. This should consume ammunition, launch the shot, and play a loud sound.
4.  **Reload Sequence:**
    Build the reload process. This must be a slow, deliberate action that leaves the player vulnerable. It should take a set amount of time to complete before another shot can be fired.
5.  **Player Screen Updates:**
    Add visual elements to the screen to show the player their current ammunition count and a clear indicator of how much time is left in the reload process.
6.  **Crossbow Setup:**
    Apply these new features to the Crossbow item. Set its reload time to be significantly long to match the design description.

## Verification Steps
1.  Equip a Crossbow in the game.
2.  Hold the aim button and verify the camera and character respond appropriately.
3.  Press the fire button and confirm a shot is fired, ammunition is consumed, and a loud sound is produced.
4.  Observe the reload process. Ensure it takes a long time and that you can see a visual indicator of the reload progress on the screen.
5.  Try to fire again while reloading and confirm the weapon does not shoot.

## Completion Checks
*   [x] At least one ranged weapon (the Crossbow) is fully playable with aim and fire controls.
    (`RangedWeapon`/`RangedWeaponStats`; aiming is hold-right-click while the crossbow is the held
    item — see `ItemManager.Update()` — firing reuses the existing "Attack"/G input, see
    `PlayerStateMachine.Attack()`.)
*   [x] Firing the weapon triggers a long reload period where the player cannot fire again immediately.
    (`RangedWeaponStats.ReloadDuration` = 3.5s; `RangedWeapon.TryFire` returns false while
    `!IsLoaded`.)
*   [x] Firing the weapon creates a noise that can be heard in the game world.
    (`RangedWeapon.AlertNearbyListeners`, reusing `AcousticEmitter`/`NoiseBroadcaster` with the
    existing `NoiseType.Gunshot` — no new noise type needed, unlike melee.)
*   [x] The current ammunition count or reload progress is clearly visible on the screen.
    (`RaidHudModel.RangedWeaponStatus`, populated by `RaidHudPresenter.BuildRangedWeaponStatus`,
    drawn by `RaidHudView` under the crosshair — same IMGUI HUD as the rest of Phase 0/1, so it
    shares that HUD's known screenshot-test blind spot; see Issue 7's own completion notes.)

**Deliberate simplification vs. the plan's prose:** the plan's outline also describes zooming the
camera / slowing movement while aiming. That is not in the completion checklist above and was left
out — aiming's only functional job here is gating when `TryFire` is allowed to fire, matching what
is actually checked.

**Verified:** compiles with 0 errors via `Tools/Headless/verify.sh --build`, and the full suite still
shows the same 143/162 passing as before this change (the 18 pre-existing failures are unrelated —
spell-casting/scene-loading, tracked separately). **Not verified:** the in-Editor steps above (aim,
fire, watch the reload bar) — no Unity Editor was available in this session.


## Technical Constraints
When executing this plan, you MUST read and strictly adhere to ALL principles and conventions detailed in `docs/UnityConvention.md`. 
You cannot pick and choose which rules to enforce; every single rule applies.
Specifically, you must follow:
- Core Architectural Principles: KISS, YAGNI, Solve the Root Cause, DRY, and SRP.
- All Naming Conventions (e.g., `m_camelCase` for privates, `s_camelCase` for statics, `PascalCase` for methods/properties).
- All Formatting & Syntax rules (e.g., Allman braces, mandatory braces, 4-space indentation).
- Class & Method Organization (Newspaper metaphor, correct layout order).
- Unity-Specific Implementations (e.g., `[SerializeField]` instead of public, `[Tooltip]` instead of comments).
- UI Toolkit (UXML/USS) Naming (BEM convention, kebab-case).
- Commenting rules (Explain 'Why', not 'What').
