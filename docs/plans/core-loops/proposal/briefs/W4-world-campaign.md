# W4: world events and the campaign

A self-contained brief from the core-loops proposal (`docs/plans/core-loops/proposal/proposal.md`, approved by the owner before work starts). Anyone, on any of the owner's accounts, can start from this file alone.

## Goal

The world reacts: your actions set off events, random events make each raid different, and each event warns
you in its own way. Above it, the Ophan's ladder gives the campaign a shape of 15 to 25 hours, and beating the
Ophan starts a new cycle that future content can extend. This stream also owns the shared contracts the others use.

## What exists

The raid loop (`Raid/RaidDirector.cs`), the century dial that chooses the Age (`Raid/LairCenturyDial.cs`), the
ledger book (`Raid/LairLedgerBook.cs`), the Lair state and saves (`Lair/`), lockdown doors, and the Ophan's design
(#418, #419 and `docs/7-design/fear-pass.md` on `AjsAgent/fear-wick`): a demand every 3 to 5 raids, arriving 90 s
before the portal closes, through walls; caught with its artifact it takes it and you live; caught empty-handed
you die.

## You own

New `Events/` folder, `Raid/RaidDirector.cs`, `Lair/`, `Raid/LairCenturyDial.cs`, `Raid/LairLedgerBook.cs`, new
`Core/Contracts/`, the mod loader; their tests; `docs/4-systems/core-loops-contracts.md`, a new
`docs/4-systems/world-events.md` and `docs/4-systems/campaign.md`.

## You must not touch

`Market/` and `Shop/` (W1), `Loot/`, `Atmosphere/Fire*` and `Spells/` (W2), `Guards/` and `Alarm/` (W3).

## Contracts you provide (week 0, before anything else)

`ContentDef` (the base data asset with an id and a display name), and the definitions of every interface and
event in the proposal's "Known touch points": `IOwnedContent`, `PurchaseMadeEvent`, `QuestCompletedEvent`,
`LootVariant`'s id type, `FireSpreadEvent`, `IIgnitable`, `ILootPinRequest`, `EchoSpawnedEvent`,
`TrueFormTurnedEvent`, `ITurnAll`, `WorldEventStartedEvent`, `WorldEventEndedEvent`. The other streams implement
them; you write the shapes and the document, and you change them only with the owning stream's agreement.

## Steps

**Week 0**
1. The contracts and `docs/4-systems/core-loops-contracts.md`.

**Phase 1 (weeks 1 to 3)**
2. `WorldEvent` as data: trigger, chance, Ages, warning cues (far sign, sound, world clock, guard behaviour),
   phases, counter, reward; the raid rolls at most one random event.
3. The two events chosen first (suggested: the vault seals and the full moon), each with its own designed
   warning signs from the proposal, shown to the owner as a page before they are built.
4. The Ophan's ladder as data: twelve demands, three per Age; the ledger shows the next one; the demanded
   artifact is pinned through `ILootPinRequest` so it never depends on luck.

**Phase 2 (weeks 4 to 6)**
5. The other random events: plague rats, the Ophan comes early, the invasion.
6. Ages unlocking (per the owner's choice 3) and the threat preview on the dial's plaque.

**Phase 3 (weeks 7 to 9)**
7. Wildfire and collapse, with W2's fire.
8. The Ophan's confrontation, the next cycle as a data asset, and a timed tuning pass of the campaign's length.
9. The mod loader: the same data read from JSON in a mods folder.

## Done when

Every event runs in a raid with its own far sign, sound, world clock and guard behaviour, and its counter
works; the ladder advances across all four Ages and the confrontation starts a second cycle with harder tuning;
tests green; screenshots and short clips of each event's warning attached.

## How to work (every stream)

- **Read first.** `CLAUDE.md`, `docs/1-landing/README.md`, the proposal
  (`docs/plans/core-loops/proposal/proposal.md`), and the shared contracts
  (`docs/4-systems/core-loops-contracts.md`, written by W4 in week 0).
- **Branch.** Start from `staging-2026-10-11` after the Wick (`AjsAgent/fear-wick`), #414 and #422 are merged.
  Work on `AjsAgent/core-loops-w4-world`. Open pull requests into `staging-2026-10-11`, one per step, with `Refs #N`, never
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
