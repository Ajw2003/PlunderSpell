# W3: the household and its true forms

A self-contained brief from the core-loops proposal (`docs/plans/core-loops/proposal/proposal.md`, approved by the owner before work starts). Anyone, on any of the owner's accounts, can start from this file alone.

## Goal

Every enemy type is a real threat with one sense, one attack, one tell and one counter, the pattern the Wick
set. Phase 1 builds one true form per Age.

## What exists

One guard state machine for all 16 enemy types (`Guards/GuardBrain.cs`; states Patrol, Investigate, Chase,
Combat, HoldBelow, Stunned, Slept, OnFire, Incapacitated, Turning, Dead), differing only by tuning. The Wick, the
Lantern Warden's true form, on `AjsAgent/fear-wick` (#417): turns on sight, on a hit or when woken from Somnus by a
cast's noise; 1.6 s turn with a 0.9 s window to run; sees only within its own 6 m of fire light; 1.4 m/s stalking,
4.2 m/s hunting; kills on a catch; never dies; drops its lantern where it turned. The art bible's 16 enemies are in
`docs/art/data/*.json`.

## You own

`Guards/`, `Enemies/`, `Alarm/`, `Raid/EnemyRoster.cs`, `Raid/GuardSpawner.cs`, `Raid/GuardPlacementPlanner.cs`,
new tools whose behaviour is about guards (hood, hand mirror, sounding rod, meat, chalk marks), the true forms'
art in `Ajw2003/claude-art-pipeline`; their tests; `docs/4-systems/fear.md` (it arrives with the Wick) and a new `docs/4-systems/guards.md`.

## You must not touch

`Market/` and `Shop/` (W1), `Loot/`, `Atmosphere/Fire*` and `Spells/` (W2), `Raid/RaidDirector.cs`, `Lair/` and
`Events/` (W4).

## Contracts you provide

- `TrueFormTurnedEvent`, and `ITurnAll` (turns every guard at once, for the full moon).
- A `TrueFormDef` data asset per form: sense, speeds, tell cues, counter hooks.

## Contracts you use

- `IIgnitable` and `FireSpreadEvent` (W2), for the Hearth-Mother.
- `IOwnedContent` (W1), for tools.

## Steps

**Week 0**
1. Get the Wick merged into `staging-2026-10-11`, with its review sheets.

**Phase 1 (weeks 1 to 3)**
2. `TrueFormDef` as data, and the Wick moved onto it with no change in behaviour.
3. The Hearth-Mother (Keeper of the Flame, Bronze): sees heat; throws fire pots; Aqua douses her pots.

**Phase 2 (weeks 4 to 6)**
4. The Visor (Sallet Halberdier, Late Medieval): moves only when nobody looks at it.
5. The Doorman (Palace Guard, Powder): waits as a door; Porta's knock shows which doors are real; chalk marks.
6. `ITurnAll` for the full moon.

**Phase 3 (weeks 7 to 9)**
7. The next true forms in the owner's order, from the roster in the proposal.

Every new form's look is designed with the owner before it is modelled, through the art pipeline.

## Done when

Four true forms (the Wick, the Hearth-Mother, the Visor, the Doorman) each turn, hunt by their own sense, show
their tell and fall to their counter in a raid, with tests for each sense and counter, review sheets for each
model, and screenshots.

## How to work (every stream)

- **Read first.** `CLAUDE.md`, `docs/1-landing/README.md`, the proposal
  (`docs/plans/core-loops/proposal/proposal.md`), and the shared contracts
  (`docs/4-systems/core-loops-contracts.md`, written by W4 in week 0).
- **Branch.** Start from `staging-2026-10-11` after the Wick (`AjsAgent/fear-wick`), #414 and #422 are merged.
  Work on `AjsAgent/core-loops-w3-household`. Open pull requests into `staging-2026-10-11`, one per step, with `Refs #N`, never
  Closes or Fixes.
- **Issues.** Each step below is a child issue of this stream's issue (`Part of #N`), labelled
  `AjsAgent created this`, a category, and `in progress` while you work it; `needs review` when the owner should
  look.
- **Stay in your files.** Edit only the files and folders listed under "You own". When you need another
  stream's system, use the event or interface in the contracts. If a contract is missing, ask W4 for it; do
  not edit another stream's files.
- **House rules.** Systems talk through `EventManager` events. `Update` only for genuine per-frame work,
  marked `// per-frame: <reason>` (`docs/4-systems/core.md`). Tunable numbers live in ScriptableObject data
  assets, never in code. The host is the authority: state changes go through `ServerRpc` and reach clients by
  `ObserversRpc` or a synced value (PurrNet).
- **Proof.** Every step: EditMode tests for the rules, a run in the editor or the headless harness, and
  before-and-after screenshots for anything you can see. Art goes through the art pipeline with a review sheet.
- **Records.** Update your system's document in `docs/4-systems/`, add a line to `docs/5-today/Today.md`, and
  a dated entry in `docs/6-decisions/Decisions.md` for any decision the owner makes.
