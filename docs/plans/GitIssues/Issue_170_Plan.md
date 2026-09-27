# Plan: Issue 170 - Automated two-PC co-op test over Steam

## Exhaustive Outline
The one-PC check (`Tools/Unity/coop_carry_check.sh`, built for #169) runs the host in the Editor and
a Development build on localhost. It tests game logic but not Steam (relay, lobby, invites, rich
presence, overlay) or real network delay. The owner has two PCs, each signed into its own Steam
account, and works alone: nobody can sit at both keyboards. The goal is one command that runs a
real co-op session across both PCs over Steam, driven entirely by the agent, and prints PASS or
FAIL per step with screenshots and output from both sides.

It serves three issues: #169's final acceptance (shared carry over Steam), #167 (Steam features
work from Build and Run but not from the exe) and #168 (hosting after joining a friend is broken).

Facts the plan rests on, checked against the code on 2026-09-27:
- The command line can host or join over UDP (`-coop-host`, `-coop-join <address>`) and join a Steam
  lobby on a cold launch (`+connect_lobby <id>`), in `CoopSession.cs`. There is no command-line way
  to host over Steam.
- Invites go through `SteamMatchmaking.InviteUserToLobby` (`CoopSession.cs:290`).
- With App ID 480, accepting an invite while the game is closed launches Spacewar, so the build must
  already be running on the joining PC (`docs/4-systems/net.md`, "Traps").
- The Steam overlay is off unless Steam launched the game; the automation must not depend on it.

## Step by Step Execution Instructions

1.  **Answer the open question: can one session reach both PCs?**
    On PC A, with a Development build (Pipeline runtime on) running on PC B, try
    `unity command --runtime Plunderspell eval` against PC B's build. Record the result in
    `docs/4-systems/net.md`, "Testing it". This decides step 3's shape:
    - **Reachable:** one Claude Code session on PC A drives both builds.
    - **Local only:** a Claude Code session on each PC, each driving its own build, coordinating as
      in step 3.

2.  **Add a command-line Steam host and a scripted-join hook.**
    Add `-coop-host-steam` to `CoopSession`'s command-line handling: create the friends-only lobby
    and host over `SteamTransport`, as the Host Co-op button does with Steam running. Add
    `-coop-invite <steamId64>` so the host invites the other account once the lobby exists. Log the
    lobby id in a `[Coop]` line the automation can read. These are Development-only test hooks: no
    menu change.

3.  **Coordinate the two sides.**
    Both sides follow one scenario file (`Tools/Coop/scenarios/<name>.json`): a list of steps, each
    naming which side acts (`host`, `client` or `both`), the action (an `Tools/Unity/eval/` script)
    and what to read afterwards. Each side writes its readings and screenshot paths to a result
    file after each step.
    - **One session:** the session runs each step on the named side directly.
    - **Two sessions:** the sessions pass step numbers and results through a folder both PCs can
      reach (a git branch pushed and pulled between steps, or a shared network folder), each
      waiting for the other's "step N done" before going on. Pick whichever works on the owner's
      network; record the choice in net.md.

4.  **Write the runner: `Tools/Coop/run_two_pc.sh <scenario>`.**
    In one command:
    - builds the same Development build for both PCs (Pipeline runtime on, then set back off and
      the `preloadedAssets` line reverted, as net.md warns) and gets it onto PC B;
    - launches the build on both PCs, each under its own Steam account, PC B first so it is running
      before any invite (the App ID 480 trap);
    - starts PC A with `-coop-host-steam -coop-invite <PC B's Steam ID>`, and has PC B accept the
      invite through the Steam API (`GameLobbyJoinRequested_t`), not the overlay;
    - runs the scenario's steps, compares both sides' readings, and saves each side's screenshots
      and log under `docs/generated/coop-steam-<date>/`;
    - closes both builds and prints one PASS or FAIL line per step, plus the network round-trip
      time it saw.
    The two Steam IDs and PC B's address live in one local, untracked config file the runner
    reads; the runner prints exactly which value is missing if it is absent.

5.  **Write the scenarios.**
    - `shared_carry`: #169's acceptance: both players carry one piece, one lets go, one leaves
      mid-carry. Positions agree on both sides within a tolerance that allows for the measured
      round-trip time.
    - `rehost_after_join` (#168): PC B joins PC A, leaves, then hosts; PC A joins PC B. Both reach
      the Lair, both can move, both see each other.
    - `exe_launch` (#167): launch PC A from the built exe (not Build and Run) and host over Steam.
      Steam signs in, the lobby is created and PC B joins.

6.  **Document it.**
    `docs/4-systems/net.md`, "Testing it": how to run the runner, what the config file holds, and
    what PASS and FAIL mean. Update its plain copy.

## Verification Steps
1.  From PC A, with nobody at PC B, run `Tools/Coop/run_two_pc.sh shared_carry`. Both builds launch,
    PC B joins over Steam and the scenario prints a line per step.
2.  Run it twice in a row. Both runs give the same PASS/FAIL results.
3.  Break it deliberately (close PC B's build mid-scenario). The runner reports which step failed
    and why, and still closes the remaining build.
4.  Run `rehost_after_join` and `exe_launch`. Each reproduces #168 and #167 as reported, or passes if
    they have been fixed.
5.  Check the screenshots and logs from both PCs are under `docs/generated/coop-steam-<date>/` and
    committed.

## Completion Checks
*   [ ] Whether one session can reach both PCs is answered and recorded in net.md.
*   [ ] `-coop-host-steam` and `-coop-invite` start a Steam session from the command line.
*   [ ] `Tools/Coop/run_two_pc.sh` runs a scenario across both PCs with nobody at either keyboard.
*   [ ] Scenarios exist for #169's shared carry, #167 and #168.
*   [ ] Both sides' screenshots and output are saved and committed.
*   [ ] `docs/4-systems/net.md` and its plain copy explain how to run it.


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
