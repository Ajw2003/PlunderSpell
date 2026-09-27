# Plan: Issue 18 - Combat Testing Environment

## Exhaustive Outline
Currently, to test if combat works, you have to start a full raid, explore a massive castle, and hope you run into an enemy. This is too slow for testing weapons, spells, or enemy behavior. The goal of this task is to create a dedicated, simple "Combat Test" area. This will be a flat, empty room where you can easily spawn enemies and test combat immediately. You should be able to choose which enemies spawn and how many, without having to change the game's code.

## Step by Step Execution Instructions

1.  **Create the Test Scene:**
    Open the existing `TestScene` or create a new one using the `TestSceneBuilder`. Build a simple, flat floor with walls to act as an arena.

2.  **Build a Spawning Interface:**
    Create a simple user interface panel inside this test scene. Add drop-down menus or buttons that let you select from the 10 available enemy types. Add a slider or text field to choose how many enemies to spawn. Add a toggle for their starting alarm level (e.g., Unaware or Chasing).

3.  **Implement Spawning Logic:**
    Write a script for this test scene that reads the values from the UI panel. When a "Spawn" button is pressed, the script should create the chosen enemies in the arena and set their alarm levels.

4.  **Add Menu Access:**
    Add a button to the Main Menu or a special developer menu that loads this combat test scene directly. Document how to use this new test scene in the `docs/4-systems/` folder.

## Verification Steps
1.  Launch the game and look for the new "Combat Test" button on the menu. Click it.
2.  Verify the game loads a simple, flat room.
3.  Use the new UI panel to select an enemy type (like "WarHound"), set the count to 2, and click "Spawn".
4.  Verify two WarHounds appear in the room.
5.  Engage them in combat to verify they behave normally.
6.  Change the settings on the UI panel and spawn a different enemy. Verify it works correctly without restarting or changing code.

## Completion Checks
*   [ ] A dedicated combat test scene exists.
*   [ ] A developer or tester can select the enemy type, count, and alarm level from a menu in the scene.
*   [ ] The player can instantly fight the spawned enemies.
*   [ ] The test scene is easily accessible from a main menu button.
*   [ ] The feature is documented in `docs/4-systems/`.


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
