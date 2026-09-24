# Plan: Issue 53 - No standalone Unity player build has ever been produced

## Exhaustive Outline
The game currently only runs inside the game engine editor. There is no way to create a standalone version of the game that players can run on their own computers. The goal of this task is to create a tool that builds a standalone version of the game, and then to test that this version works correctly from start to finish.

## Step by Step Execution Instructions

1.  **Create Build Tool:**
    Create a new file in the project to handle the process of building the game.
2.  **Add Build Instructions:**
    Write instructions in this file that tell the game engine how to create a standalone version of the game.
3.  **Add Menu Option:**
    Add a button or menu option to the game engine interface so the build process can be started easily.
4.  **Configure Levels:**
    Make sure the build instructions include all the necessary game levels, starting with the main menu.
5.  **Generate Build:**
    Click the new menu option to create the standalone game.

## Verification Steps
1.  Find the newly created standalone game file on your computer.
2.  Open the game using this file.
3.  Check that the game loads the main menu successfully.
4.  Start a new game session from the main menu.
5.  Complete the game session and check that the game does not crash or freeze.

## Completion Checks
*   [x] A new tool exists to handle building the game.
    (`Assets/_Project/Scripts/Editor/PlayerBuilder.cs`.)
*   [x] A button or menu option exists to start the build process.
    (`Tools/Plunderspell/Build Standalone Player`, and `PlayerBuilder.Build()` is public so it can
    also be invoked headlessly via `-executeMethod RogueAi.EditorTools.PlayerBuilder.Build`.)
*   [x] A standalone version of the game has been created successfully.
    (Ran for real: `Unity.exe -batchmode -nographics -quit -projectPath . -executeMethod
    RogueAi.EditorTools.PlayerBuilder.Build`, against the real Editor install at
    `C:\Program Files\Unity\Hub\Editor\6000.3.15f1`, matching this project's exact version.
    Produced `Build/Windows/Plunderspell.exe` + `Plunderspell_Data/` — 121MB, a real
    `BuildPipeline.BuildPlayer` output, not gitignored-and-faked. `Build/` is already covered by
    the repo's standard Unity `.gitignore` — the tool is committed, the 121MB binary output is not.)
*   [~] The standalone game opens and reaches the main menu.
    (Partially verified. Launched `Plunderspell.exe -batchmode -nographics`: the engine
    initializes, PhysX loads, all managed assemblies load, and real game code runs —
    `[VoiceServiceLocator] Auto-registered MockVoiceInputService` is a log line from
    `RogueAi.Voice`, not engine boilerplate, so `RaidScene.unity` did load and at least one
    `Awake()` ran. **Not verified**: that the main menu actually renders, because this session has
    no display/GPU (`-nographics` forces a `NullGfxDevice`) — there is no way to see or screenshot
    UI here. Needs a real machine with a display for the visual half of this check.)
*   [ ] A full game session can be started and completed in the standalone game.
    Not attempted — needs the same display/input this session doesn't have, and (per issue #55) a
    real multi-player session besides.

**Verified today (2026-09-22), against the real Unity Editor, not the headless harness or a
shim:** the whole project imports and compiles with 0 errors under Unity 6000.3.15f1 itself — this
also validated every hand-authored `.prefab`/`.asset` YAML file from issues #37 and #39
(`ArmingSword`, `Crossbow`, their stats assets), which until this pass had only ever been checked
by the headless harness's shim, never a real Editor. One real compile error surfaced that the
headless harness's `UnityEditor` shim could not have caught (`PlayerBuilder.cs` used
`BuildReport`/`BuildResult` from `UnityEditor.Build.Reporting` without the `using`) — fixed, and
`PlayerBuilder.cs` is now excluded from the headless Editor build for the same reason
`SteamInviteGateway.cs` is excluded from the Runtime one (see `Tools/Headless/README.md`).


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
