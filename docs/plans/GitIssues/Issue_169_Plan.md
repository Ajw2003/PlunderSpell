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

## Design (fixed 2026-09-27, before building)
Checked against the code on 2026-09-27. Loot prefabs use `NetworkTransform` with owner authority
(`_ownerAuth: 1`); with no owner, PurrNet makes the server the controller
(`NetworkIdentity.IsController`), so "the host controls a held piece" means the piece has no owner.

- **A hold.** `Item` keeps a list of holds instead of one. A hold is: holder key (the local player
  or a network `PlayerID`), grip point in the item's frame, target point and its velocity, when the
  target was last stamped, the holder's view yaw, whether they are turning it on purpose and to what
  rotation, the tow feet/velocity/rope, and the holder's grip, haul and turn strengths. Solo play
  has exactly one hold, written by `ItemManager` as today.
- **Who applies it.** `Item.FixedUpdate` applies every hold only where `CanDriveHere` is true. On
  a client that is not the controller, `ItemManager` still keeps its own hold (the beam, the HUD
  and holder-collision ignoring read it) but no force is applied there.
- **Sending intent.** `CarryBeamRelay` already sends hand, aim, grip point and load 15 times a
  second. It also sends the target velocity, view yaw, rotating flag and target rotation, and tow
  data; the server writes them into that player's hold on the item. A hold not heard from for
  0.5 s is dropped, which also covers a disconnect. `PlayerID` leaving, and the holder's death,
  drop it at once.
- **Grabbing.** Grabbing a networked piece on the beam asks the server to add a hold; the server
  removes any owner (`RemoveOwnership`), so the server controls it. A second player's grab adds a
  second hold; nothing is taken from the first. Weapons held in the hand keep today's ownership
  hand-off: they are posed to one player's view every frame and cannot be shared.
- **Adding strengths.** Each holder's spring is worked out alone; gravity compensation is shared
  equally across holders, and each holder's upward part is capped by their own grip. A piece is too
  heavy to lift when its weight exceeds the holders' grips added together (one player: 100 N, so
  10 kg as before). With one hold the numbers are identical to today's, so `CarryFeelTests` holds.
- **Opposite pulls.** When a holder's held point is more than `k_beamSnapDistance` (1.5 m) from
  their target for 0.3 s, that holder's beam snaps and their hold is dropped. Two players pulling
  apart both snap, and the piece falls.
- **Mouse steering.** The wanted rotation (view yaw times the pickup orientation, or the middle-click
  target) is reached by torque: a critically damped angular spring per hold, capped by the holder's
  turn strength (N·m). Angular acceleration is torque over the body's inertia, so a long or heavy
  piece reaches its cap and sweeps round; a goblet follows almost at once.
- **Naming.** New code follows the file it is in (`_camelCase` in `Item`, `ItemManager`,
  `CarryBeamRelay`), per "existing file style wins"; `docs/UnityConvention.md`'s `m_` applies to
  new files.

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

7.  **Final acceptance over Steam on two real PCs (needs #170).**
    Once `coop_carry_check.sh` passes, run the shared-carry scenario through #170's two-PC
    automation: PC A hosts over Steam, PC B joins by invite, both players carry the same piece,
    one lets go, and one leaves mid-carry. Both sides must agree on where the piece is, allowing for
    real network delay. Save both sides' screenshots and output under `docs/generated/`. If #170 is
    not built yet, #169 stays open at this step.

8.  **Update the documentation.**
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
7.  Over Steam on two PCs (#170): the shared-carry scenario passes on both sides.

## Completion Checks
*   [ ] `Tools/Unity/coop_carry_check.sh` exists and runs both players on one PC without a person.
*   [ ] Any number of players can hold one piece, and every machine agrees where it is.
*   [ ] A second player grabbing a held piece joins the carry instead of taking it.
*   [ ] Holders pulling the same way add their strength; opposite pulls drop the piece.
*   [ ] Held pieces turn with the holder's view through physics, heavier and longer pieces more slowly.
*   [ ] The unused two-person code in `LootPickup` and `LootInteractor` is gone.
*   [ ] The shared-carry scenario passes over Steam on two real PCs (#170).
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
