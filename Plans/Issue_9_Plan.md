# Plan: Issue 9 - Input Blocking During Menus

> **Correction (2026-09-18).** Steps 1-2 below name `FreeLookPlaytestController` and
> `PushToCastController`. `FreeLookPlaytestController` is not in `RaidScene.unity` — it is the
> ItemGym bench harness. The raid's player is `PlayerStateMachine` plus `PlayerInputController`,
> and that is where the gate has to go. Gating the two scripts named below, as originally written,
> leaves the shipping player ungated. See docs/6-decisions/Decisions.md, "Issue 9's gate belongs on the raid's
> player, not only on the playtest harness".

## Exhaustive Outline
Currently, the player character can still walk around and look around even when the main menu or other screens are open. This causes the game world to move in the background while the player is trying to navigate a menu. The goal of this task is to ensure that all player movement, camera movement, spell casting, and object interaction are completely disabled whenever a menu is open. These controls should only function when the player is actively playing the game.

## Step by Step Execution Instructions

1.  **Centralize Input Checks:**
    Open the scripts that handle player input, specifically `FreeLookPlaytestController` (for camera and movement) and `PushToCastController` (for spell casting). Ensure they have access to the central `GameState` manager.

2.  **Add Game State Gating:**
    In each of the input-handling scripts, add a check at the very beginning of the update or input-reading functions. The code should verify if the current state is `GameState.Playing`. If the state is anything else (such as `MainMenu`, `Paused`, `Inventory`, or `Lair`), the function should immediately exit without processing any input.

3.  **Prevent Component Toggling:**
    Ensure you are checking the game state variable directly rather than enabling and disabling the script components themselves. Disabling components can cause issues with initialization or state recovery when the menu closes.

4.  **Verify State Restoration:**
    Ensure that when the menu is closed and the game state returns to `GameState.Playing`, the input scripts naturally resume reading input without requiring any special restart logic.

## Verification Steps
1.  Launch the game and enter a playable level.
2.  Open the pause menu or inventory.
3.  Attempt to move the character, look around with the mouse, cast a spell, or interact with an object. Verify nothing happens.
4.  Close the menu.
5.  Attempt to move and act again. Verify normal gameplay resumes smoothly.

## Completion Checks
*   [ ] Player movement and camera looking are disabled when a menu is open.
*   [ ] Spell casting and interaction are disabled when a menu is open.
*   [ ] All controls immediately restore and function correctly when returning to active gameplay.
*   [ ] Input blocking relies on the `GameState` variable rather than enabling/disabling script components.


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
