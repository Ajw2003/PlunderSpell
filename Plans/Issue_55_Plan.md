# Plan: Issue 55 - M2's acceptance criterion has never been run - a real four-player raid start to finish

## Exhaustive Outline
The goal is to conduct a complete play session of a raid with four real players from beginning to end. Currently, the game relies entirely on computer-run tests to check if things work, but real people have not played it yet. This task involves organizing the play session, running it on the playable version of the game, and documenting the results in the project state document.

## Step by Step Execution Instructions

1.  **Schedule the Play Session:**
    Coordinate a time with four available people to play the game together.
2.  **Prepare the Game:**
    Ensure the latest playable version of the game is ready and shared with all four players.
3.  **Play the Raid:**
    Have all four players join the game and attempt to complete the raid from the beginning to the end.
4.  **Record the Outcome:**
    Update the `docs/3-state/ProjectState.md` file with the results of the session, noting whether the group won or lost, and documenting the reasons why.

## Verification Steps
1.  Verify that all four players can successfully join the same game.
2.  Confirm that the play session covers the entire raid from start to finish.
3.  Check `docs/3-state/ProjectState.md` to ensure the session outcome is accurately written down.

## Completion Checks
*   [ ] Four real people play a raid together, start to finish, on the actual built game.
*   [ ] The outcome (completed / failed, and why) is recorded against the M2 acceptance criterion in `docs/3-state/ProjectState.md`.


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
