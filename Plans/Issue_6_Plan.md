# Plan: Issue 6 - Player and Architecture Scale Mismatch

## Exhaustive Outline
Currently, the player character is too tall compared to the castle rooms. The rooms feel cramped, and the character barely fits through doorways. Additionally, some enemies, like the large boss enemies, are too tall to even fit inside certain rooms like the crypt. The goal of this task is to establish a standard measurement scale for the game. We will adjust the heights of the player, the enemies, and the rooms so everything looks natural and correctly sized.

## Step by Step Execution Instructions

1.  **Define a Standard Scale:**
    Create or update a documentation file in `docs/4-systems/` that states the official height for a standard character. For example, determine that a standard human character should be a specific height like 1.8 meters.

2.  **Adjust Player Height:**
    Open the script `RaidSceneBuilder.cs`. Update the `PlayerHeight`, `PlayerRadius`, and `EyeHeight` values to match the new standard scale.

3.  **Adjust Enemy Heights:**
    Update the heights for all enemy character models (such as the Watchman, VaultWarden, and GildedColossus) to match the new scale. Ensure the GildedColossus is tall but still short enough to fit inside the rooms where it will spawn.

4.  **Adjust Room and Doorway Heights:**
    Review the 3D models for all castle rooms (Crypt, OuterBailey, InnerWard, Keep, CurtainWall). Scale up the room heights or adjust their ceilings so the interior space provides comfortable headroom above the player. Scale the doorways so the player and enemies can walk through without their heads clipping into the frame.

## Verification Steps
1.  Launch the game and enter a generated castle level.
2.  Walk the player character through several rooms and doorways in each zone (Crypt, OuterBailey, etc.).
3.  Spawn the largest enemy (GildedColossus) inside the smallest room (Crypt).
4.  Observe the character and enemies relative to the ceilings and doorways.

## Completion Checks
*   [ ] A standard character height is documented in the `docs/4-systems/` folder.
*   [ ] The player character has comfortable headroom inside all rooms and when passing through all doorways.
*   [ ] The largest enemy can spawn and move freely inside the smallest room it is allowed to enter.
*   [ ] All character and environment models visually agree on a consistent scale.


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
