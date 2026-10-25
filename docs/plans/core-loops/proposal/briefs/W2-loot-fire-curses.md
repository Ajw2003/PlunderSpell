# W2: loot variants, fire and water, curses and the mimic

A self-contained brief from the core-loops proposal (`docs/plans/core-loops/proposal/proposal.md`, approved by the owner before work starts). Anyone, on any of the owner's accounts, can start from this file alone.

## Goal

Every piece of loot can be something more than a price: gold, platinum, flammable or cursed, each with its own
rule and counter. Fire spreads through things that burn, water puts it out, and a cursed piece can send a copy
of you, speaking in your voice, into the castle.

## What exists

Loot pieces with a category (`Loot/LootItem.cs`); loot rolled by `Raid/RaidLootTable.cs` and placed by
`Raid/LootPlacementPlanner.cs` and `Raid/LootSpawner.cs`; fires that follow the alarm
(`Atmosphere/FireRules.cs`, `FireSource.cs`, `CastleFireSpawner.cs`); a burning status
(`Status/StatusEffectReceiver.cs`); spells in `Spells/` (catalogue, lexicon, misfires). Earlier mimic work on
branches `claude/voice-mimicry-improvements-7a0b29` and `claude/guard-mimic-prototype`.

## You own

`Loot/`, `Raid/LootSpawner.cs`, `Raid/LootPlacementPlanner.cs`, `Raid/RaidLootTable.cs`, `Atmosphere/Fire*.cs`
and `CastleFireSpawner.cs`, `Status/`, the new spells Aqua and Purgo (their files in `Spells/Effects/` and their
entries in `SpellCatalogue.cs` and `SpellLexicon.cs`), a new `Mimic/` folder; their tests;
new documents `docs/4-systems/loot.md` and `docs/4-systems/fire.md`.

## You must not touch

`Market/` and `Shop/` (W1), `Guards/` and `Alarm/` (W3), `Raid/RaidDirector.cs` and `Lair/` (W4). Other spells
in `Spells/` stay as they are.

## Contracts you provide

- `LootVariant` data asset and the variant on every spawned piece.
- `FireSpreadEvent` (room, rooms burning), `IIgnitable` for anything that can catch fire.
- `ILootPinRequest`: guarantees a named piece spawns this raid (for the Ophan's demands).
- `EchoSpawnedEvent`.

## Contracts you use

- `IOwnedContent` (W1), to know whether Aqua and Purgo are owned.
- `TrueFormTurnedEvent` (W3), so the Echo uses the same kill-on-catch rule.

## Steps

**Phase 1 (weeks 1 to 3)**
1. `LootVariant` as data: gold and platinum, worth and fragility multipliers, cracks that drop worth, a cold
   shine for platinum, the roll at spawn.
2. Materials and spreading fire: every furnishing and piece has a material; burning things spread within 1.5 m
   every 3 s, 10 s through a closed door; a burning player has 6 s.
3. Water: Aqua, the Age's fixed water (cistern and jars, well and troughs, laver and rain barrels, fountain and
   fire buckets), the bucket.

**Phase 2 (weeks 4 to 6)**
4. Flammable and cursed conditions; curses as data (the Echo, the Weight, the Whisper); Purgo.
5. The Echo: clip pool on each machine from moments the microphone is already live, 1 to 4 s, in memory only,
   wiped on quit, sent to others only while the Echo exists, with an opt-out; the copy wanders and replays clips.
6. `ILootPinRequest`.

**Phase 3 (weeks 7 to 9)**
7. Moonsilver for the full moon, and the fire behaviour wildfire needs (smoke layer, burnt panels revealing
   alcoves), working with W4's event.

## Done when

A raid spawns all four variants at their chances; a flammable book catches from a torch and spreads; Aqua and
a trough put it out; a cursed piece held 40 s brings an Echo that speaks in the holder's recorded clips on every
machine and stops when Purgo cleanses it; tests green; screenshots and a short clip attached.

## How to work (every stream)

- **Read first.** `CLAUDE.md`, `docs/1-landing/README.md`, the proposal
  (`docs/plans/core-loops/proposal/proposal.md`), and the shared contracts
  (`docs/4-systems/core-loops-contracts.md`, written by W4 in week 0).
- **Branch.** Start from `staging-2026-10-11` after the Wick (`AjsAgent/fear-wick`), #414 and #422 are merged.
  Work on `AjsAgent/core-loops-w2-loot`. Open pull requests into `staging-2026-10-11`, one per step, with `Refs #N`, never
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
