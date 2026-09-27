# Plan: Issue 169 - Two-person carrying and mouse-steered rotation

## Exhaustive Outline
Today one machine moves a carried piece: grabbing asks the host, and the host gives the piece to
whoever asked last (`LootPickup.RequestCarry`, installed by `NetworkCarry`). So two players cannot
share a piece, and a second player grabbing a held one takes it. Pieces over 10 kg are towed by one
player. The goal is that any number of players can hold one piece, their pulls add together, and
every machine agrees where it is. Held pieces also turn with the holder's view through physics
("mouse steering"), so long pieces such as the Rolled Tapestry sweep and lag like real objects.

Decided 2026-09-27 (`docs/6-decisions/Decisions.md`): replace how a carry is networked, keep how it
feels. `Item` and `ItemManager` stay and are adapted; the unused two-person code in `LootPickup` and
`LootInteractor` goes.

## Step by Step Execution Instructions

1.  **Build an automated two-player carry check, then confirm today's grab-steal behaviour.**
    The owner works alone, so no step may need a person at a second machine. Both players run on
    one PC and the agent drives both (`docs/4-systems/net.md`, "Testing it"): the Editor hosts, and
    a Development build with the Pipeline runtime on joins with `-coop-join 127.0.0.1`. Write
    `Tools/Unity/coop_carry_check.sh`, which in one command:
    - builds the Development client to `Build/DevTest` with the Pipeline runtime on, then sets it
      back off and reverts the `preloadedAssets` line (both traps are in net.md);
    - starts the host in the Editor and launches the client in a window;
    - drives both sides with `Tools/Unity/eval/` scripts (`unity command eval` for the host,
      `unity command --runtime Plunderspell eval` for the client) through a list of scenarios: host
      grabs a piece, client grabs a piece, both grab the same piece, one lets go, the client quits
      while holding;
    - after each scenario reads the piece's position on both sides and fails if they differ by
      more than a small tolerance, and saves a screenshot from each side under
      `docs/generated/coop-carry-<date>/`;
    - closes the client and stops Play mode, and prints one PASS or FAIL line per scenario.
    Run it before any carry code changes. The "both grab the same piece" scenario should fail today:
    that is the recorded before-state the new code must fix.

2.  **Give held pieces to the host.**
    Replace the ownership hand-off in `NetworkCarry` and `LootPickup.RequestCarry`. While a piece
    has any holder, the server controls its body. The server keeps the piece's list of holders and
    removes a holder on let-go, disconnect or death.

3.  **Send intent, not motion.**
    Split `Item`'s carry into two halves: working out the pull (grab point, target point, turn) on
    the holder's machine, and applying it to the body where the body is controlled. A holder's
    machine sends its pull to the server several times a second; the server applies every holder's
    spring force each physics step. Solo play takes the same path with one holder.

4.  **Add strengths together.**
    Make grab strength a per-player value instead of the fixed number. Two holders pulling the same
    way combine their strength; pulling in opposite directions past a limit drops the piece. The
    10 kg tow rule becomes "heavier than the holders can lift together": two players lift what one
    would tow.

5.  **Mouse steering.**
    The held piece's target rotation follows the holder's view, applied as torque, not set directly,
    so it turns at a rate set by its mass and length. Heavy and long pieces lag and sweep into
    place; light ones follow almost at once. Middle-click rotation (`ItemManager.HandleRotation`)
    stays for fine turning. With two holders, each holder's turn is one more torque input.

6.  **Remove the old two-person carry.**
    Delete the primary/secondary carry in `LootPickup` (`RequestPickup`, `RequestSecondaryPickup`,
    `CreateCarryJoint` and the fields they use) and `LootInteractor`'s pickup path, once nothing
    references them. Keep what the live game still uses from `LootPickup` (breaking, value,
    levitation).

7.  **Update the documentation.**
    `docs/4-systems/net.md` (carrying and grab beams), `docs/4-systems/damage.md` ("Weight"), and
    their copies under `docs/plain/4-systems/`.

## Verification Steps
Run `Tools/Unity/coop_carry_check.sh` after steps 2, 3, 4, 5 and 6, and commit its screenshots
and output. Nothing below needs a second person or a second PC.

1.  Run `CarryFeelTests`; every existing test still passes, with the same numbers.
2.  Solo: carry, tow and throw a light piece, a heavy piece and a weapon. Each feels as it did before.
3.  Two players (`coop_carry_check.sh`): A grabs a piece, then B grabs the same piece. Neither loses it, and both screens
    show it in the same place.
4.  Two players (`coop_carry_check.sh`): A and B lift a 12 kg piece together and walk the same way. It moves faster than
    one player towing it. Pull opposite ways: it drops.
5.  Carry the Rolled Tapestry and turn quickly. It sweeps round and settles; a goblet turns almost
    at once. Middle-click still rotates it precisely.
6.  Let go, disconnect or die while holding. The piece carries on for the other holder, or falls.

## Completion Checks
*   [ ] `Tools/Unity/coop_carry_check.sh` exists and runs both players on one PC without a person.
*   [ ] Any number of players can hold one piece, and every machine agrees where it is.
*   [ ] A second player grabbing a held piece joins the carry instead of taking it.
*   [ ] Holders pulling the same way add their strength; opposite pulls drop the piece.
*   [ ] Held pieces turn with the holder's view through physics, heavier and longer pieces more slowly.
*   [ ] The unused two-person code in `LootPickup` and `LootInteractor` is gone.
*   [ ] `CarryFeelTests` passes, and new tests cover two holders and steering.
*   [ ] The net and damage system docs and their plain copies describe the new behaviour.


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
