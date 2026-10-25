# Raw research 1 of 4: what PlunderSpell has today (2026-10-25)

Stage 1 of the core-loops work (`docs/plans/core-loops/README.md`). Raw facts only; the analysis
comes in stage 2. Every fact names the branch and file it was read from, because `main`
(last merge 2026-10-03, PR #246) is three weeks behind the work.

## Where the newest work lives

| Branch | Last commit | Holds |
|---|---|---|
| `main` | 2026-10-03 | Everything up to the playability pass (#246) |
| `staging-2026-10-11` | 2026-10-11 | Integration branch; open PRs target it |
| `AjsAgent/fear-wick` | 2026-10-15 | The Wick built (#417), 33 commits past staging, no open PR found |
| `AjsAgent/fear-guards` | 2026-10-15 | One more guard per ground-floor zone (#420), 3 commits past staging |
| `AjsAgent/fear-pass-handoff` | 2026-10-15 | PR #414: the Wick and Ophan art, clips, import rules, fear plan |
| `AjsAgent/lair-items-378` | 2026-10-18 | PR #422: Lair loot keeps its place across sessions |

Roadmap headline on `main` (`docs/3-state/ProjectState.md`, measured 2026-09-28): about 42%. M4
"Lair and Market" was 5% then; it has moved a lot since on the branches above.

## The loop as built

Read from `docs/7-design/diegetic-ui-lair-market.md` and `docs/4-systems/market.md` on
`AjsAgent/fear-wick`.

1. **Lair** (a physical vaulted cellar): century dial picks the Age, ledger book shows the money,
   four strongboxes (one per wizard), portal arch, Market door.
2. **Raid**: a procedurally generated castle (curtain wall, bailey, inner ward, keep, crypt). Loot
   has weight in kg, worth and fragility. 20 art-bible plunder items, 44 to 53 placed per raid.
   Noise wakes guards; an alarm runs Calm, Suspicious, Alerted, Hue and Cry. A portal closes on a
   timer; only what crosses it comes home.
3. **Haul pile**: carried loot lands in the Lair as physical objects and is saved (PR #422).
4. **Market**: four vendors (Fence, Goldsmith, Pardoner, Antiquarian) haggle with three words,
   *Plus*, *Satis*, *Vale* (keys 1, 2, 3 or voice). Vendor interest by loot category (Metal, Holy,
   Curio, Arms, Other): 1.3 for the vendor's own category, 0.7 otherwise, the Fence 1.0 for all.
   Nightly mood, patience 1 to 4, a second visit opens 10% lower.
5. **Coins** are physical pouches (0.01 kg a coin). Dropped in your strongbox they fill your purse.
6. **The Collector** takes an equal share of a single debt from each present wizard's purse at each
   setting out. The debt grows 50 per setting out.

## What coins can buy today

Nothing. No buy code exists on any branch read: `git grep` for shop, scroll, learned-spell or buy
code under `Assets/_Project/Scripts/Runtime` on `AjsAgent/fear-wick` and `AjsAgent/lair-items-378`
found only `VendorLines.WillNotBuy`. The diegetic design says "Buying happens at the same stalls:
weapons, tools and scrolls" (`diegetic-ui-lair-market.md`, "Coins and the debt"), but it is not built.

## The approved fear direction (owner, 2026-10-11)

From `docs/7-design/fear-pass.md` on `AjsAgent/fear-wick`, status approved (#386):

- Raids should feel like Lethal Company: hard to get in, hard to leave alive.
- **Guards have two forms.** Human first; on confirmed sight or being hit, a one-way turn into a
  true form (a horror). Human form can be stunned by a hard head blow, never killed. True form:
  run and hide. One catch kills.
- **Every guard type has a counter**, hinted at by its behaviour.
- **Spells are earned.** New save: Porta only. Somnus bought early in the Market. Velox and Saltus
  mid-game. Offensive spells late, locked for now. Every spell backfires with a real cost.
- **More guards on the ground floors**, so the threat is visible from the gate.
- **The debt goes.** A creature, the **Ophan of Hours**, demands specific artifacts every 3 to 5
  raids (tunable). Unpaid, every raid is hunted: it arrives 90 s before the portal closes, passes
  through walls, catches you. Carrying its artifact: it takes it and you live. Empty-handed: you die.
- Death loses what you carried and unread scrolls; learned spells and the Lair stay. Nobody starts
  over.
- Spells come from the Market (some) and ancient texts found in castles (special ones).
- Still open: which later spells come from where; whether a thrown object can stun a human guard.

## Enemies: the roster

16 enemies modelled and rostered, 4 per Age (`docs/art/bronze.md`, `high.md`, `late.md`,
`powder.md`). Each Age has one patrol, one ranged, one heavy and one special type.

| Age | Patrol | Ranged | Heavy | Special |
|---|---|---|---|---|
| Bronze (c. 1200 BC) | Palace Levy | Wall Slinger | Dendra Champion | Keeper of the Flame |
| High Medieval (c. 1250) | Lantern Warden | Castle Crossbowman | Household Knight | Alaunt War-hound |
| Late Medieval (c. 1450) | Sallet Halberdier | Handgunner | Gothic Man-at-Arms | Pavisier |
| Age of Powder (c. 1620) | Palace Guard | Musketeer | Cuirassier | Petardier |

Decision 2026-09-24: "each Age's enemies are its household: people and animals, no monsters". The
fear pass (2026-10-11) overrides that for true forms.

## Enemies: how they behave in code

One guard brain for all 16 (`Assets/_Project/Scripts/Runtime/Guards/`, on `AjsAgent/fear-wick`).
States: Patrol, Investigate, Chase, Combat, HoldBelow (throws stones when it cannot reach you),
Stunned, Slept, OnFire, Incapacitated, Turning, Dead. Types differ by tuning only: ranged if the
tuning has a projectile prefab (`GuardRangedAttack.cs:33`), otherwise melee; plus speed, health,
damage. The alarm director gives 1 melee and 1 ranged attack turn per player
(`AttackTurnMediator`); the rest hold a ring.

**The one exception is the Wick** (#417, `docs/4-systems/fear.md`): the Lantern Warden turns when it
confirms sight, is hit, or is woken from Somnus by a cast's noise. The turn takes 1.6 s (the first
0.9 s is the window to run). The Wick sees only inside its own fire's light (6 m all round),
stalks at 1.4 m/s, hunts at 4.2 m/s, kills on a catch, never dies (one health point is kept). It
drops its lantern, dark, where it turned. Status: built, co-op checked, not yet merged.

The Ophan of Hours is modelled with five clips (PR #414); its hunt is planned (#418), not built.

## Spells

`SpellId.cs` (`AjsAgent/fear-wick`): primary casts Ignis, Frango, Levo, Aurum Voco, Somnus, Porta,
Velox (dash) and Saltus (high jump and slam). The enum's "extended lexicon" section holds 37 more
words (Aqua, Terra, Ventus, Lux, Tenebris, Glacies, Fulmen, Sanguis, Animus, Fortis, Caecus, Sanus,
Tempus, Verto, Crescere, Calor, Frigus, Murus, Porta Magna, Scutum, Hasta, Arcus, Flamma, Unda,
Saxum, Silva, Noctis, Dies, Vita, Mors, Bellum, Pax, Aurum, Argentum, Ferrum, Ignis Magna, Tonitrus
Magna) plus 8 misfire outcomes. These words are named in the enum; whether each has an effect was
not checked for this file.
Spells cost mana (100-point pool, 2.5/s regen). Every spell is known from the start today; earning
them is approved but not built.

## Progression and reward today

- Between raids: loot becomes coins; coins pay the Collector. No purchases, no unlocks, no levels.
- Weapons found in a raid can be carried to another Age (pitch §6); a weapon rack exists in the
  Lair design.
- The fear pass plans: spells learned from scrolls, the quota, and "eventually, maybe, you can
  fight the creature" as the long-term goal.
- No cosmetics, no achievements, no daily or seeded challenge, no run modifiers.
