# W1: the economy and the Market

A self-contained brief from the core-loops proposal (`docs/plans/core-loops/proposal/proposal.md`, approved by the owner before work starts). Anyone, on any of the owner's accounts, can start from this file alone.

## Goal

Coin buys things. After this stream, every raid's haul turns into a purchase the players can feel in the next
raid, the vendors give quests, and the Market has a blackjack den and a tailor.

## What exists

Selling and haggling with *Plus*, *Satis* and *Vale*; four vendors (Fence, Goldsmith, Pardoner, Antiquarian)
with category interest 1.3 for their kind and 0.7 otherwise; coins as physical pouches carried to a
strongbox; a purse per player (`docs/4-systems/market.md`). The Collector's debt (`Market/CollectorRules.cs`) is
cut by the fear pass. Nothing can be bought.

## You own

`Assets/_Project/Scripts/Runtime/Market/`, `Raid/SellCounter.cs`, `Raid/CounterSlate.cs`, `Raid/VendorLines.cs`,
`Raid/HaggleVoiceRouter.cs`, `Raid/CoinPouch.cs`, `Raid/LairStrongbox.cs`, and new folders `Shop/` and
`Gambling/` beside them; their tests; `docs/4-systems/market.md`.

## You must not touch

`Raid/RaidDirector.cs` and `Lair/` (W4), `Loot/` and `Spells/` (W2), `Guards/` and `Alarm/` (W3).

## Contracts you provide

- `IOwnedContent`: what the company owns (spells, tools, weapons, rooms, cosmetics), saved per save slot.
- `PurchaseMadeEvent`, `QuestCompletedEvent`.

## Contracts you use

- `LootVariant` and condition on a sold piece (W2), to price it.
- `ContentDef`, the base data asset (W4).

## Steps

**Phase 1 (weeks 1 to 3)**
1. Stock as data: a `StockItem` asset (kind, price, what it counters, which vendor sells it) and a stall that
   shows it on its counter.
2. Buying: say *Emo* ("I buy", a new word in the lexicon) or press the buy key at a stall; coin pouches leave
   the purse, the item goes to `IOwnedContent`. Haggling works on buying as on selling.
3. Worth on screen, per the owner's answer to choice 1 on the proposal page (suggested: in the Lair and
   Market at the moment of change).
4. Remove the Collector's debt (fear pass).

**Phase 2 (weeks 4 to 6)**
5. Vendor quests on the slate: category, Age, condition, sometimes a variant; pays 1.5 x plus a named reward.
6. Blackjack in a den: port the flow and events of `TheFallOfGoldenBay2D/Assets/Code/Scripts/Puzzles/BlackJackManager.cs`;
   add a six-deck shoe seeded from the raid seed, soft aces, pouch bets, 3 to 2 payouts, a per-Age table
   limit, up to four players, the host deals; *Plus* to hit, *Satis* to stand.

**Phase 3 (weeks 7 to 9)**
7. The Tailor, a fifth stall: hats, robe trims and colours.
8. Emotes on a wheel, bought at the Tailor.

## Done when

A new save can sell a haul, buy Somnus and a tool, see them in `IOwnedContent` after a restart, complete a quest,
and win and lose a hand of blackjack with three players, all host-authoritative, with tests green and
screenshots attached to each pull request.

## How to work (every stream)

- **Read first.** `CLAUDE.md`, `docs/1-landing/README.md`, the proposal
  (`docs/plans/core-loops/proposal/proposal.md`), and the shared contracts
  (`docs/4-systems/core-loops-contracts.md`, written by W4 in week 0).
- **Branch.** Start from `staging-2026-10-11` after the Wick (`AjsAgent/fear-wick`), #414 and #422 are merged.
  Work on `AjsAgent/core-loops-w1-economy`. Open pull requests into `staging-2026-10-11`, one per step, with `Refs #N`, never
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
