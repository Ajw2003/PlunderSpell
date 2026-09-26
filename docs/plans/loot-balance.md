# Loot balance pass (#142, #148)

Written and applied 2026-09-26, after the owner chose "apply as proposed". `LootBalanceTests`
(EditMode) holds the two rules that can be checked: the fragility floor, and nothing too heavy to
lift in the three outer zones.

## What is wrong now

**Too fragile.** `Fragility` is the impact speed (m/s) an item survives. A fall from height *h*
lands at √(2·9.81·*h*), so:

| Fragility | Survives a fall of about |
|---|---|
| 2 | 0.2 m |
| 3 | 0.45 m |
| 4.5 | 1 m (waist height) |
| 6 | 1.8 m |
| 8 | 3.3 m |

Nine items sit at 2–3: the faience hippopotamus, Venetian mirror, nautilus cup, silver plate,
arm reliquary, gilded altarpiece, gilded nef, golden goblet, copper pot. They broke if you set them
down carelessly. Walking into loot also broke it until 3b050db.

**Late Medieval looks empty (#148).** The castle is not short of loot: seed 777 spawned 51
pieces, and none fell through the floor. But `EraContentForge` places each era's five valuables by
worth rank (`LootPostsByRank`, `Assets/_Project/Scripts/Editor/EraContentForge.cs:157`): cheapest
at the curtain wall, dearest in the crypt. Late Medieval's two heaviest pieces are also its middle
ranks: Rolled Tapestry (11 kg, 650) and Parade Armour (13 kg, 900). Both are too heavy to lift
since #144, so they take the two busiest slots. On seed 777, 29 of the 51 pieces were one of
those two, and only 17 were small valuables. Every other era has at most one piece over 10 kg, in
the deep slots.

## Rules proposed

1. **Nothing breaks from a waist-high drop.** Fragility floor 4.5. Tiers: delicate 4.5 (glass,
   faience, mirrors), fragile 6 (gilt, parchment, reliquaries), sturdy 8+, unbreakable 999.
   Relative order within an era is kept.
2. **Effort pays.** A piece too heavy to lift (over 10 kg) is worth more than any light piece
   in its era, except the one artifact, so it ranks into the deep slots. Delicate pieces get a
   premium for the risk.
3. **One heavy piece per era**, like the other three. The Rolled Tapestry drops from 11 kg to 8 kg,
   which is liftable but strained (beam load 0.78, gold to orange).

## Proposed numbers

Changed values are **bold**. Since 2026-09-26 the weight lives only in the LootItem's Weight (kg), which sets the body's mass at spawn (`docs/systems/damage.md`, "Weight").

| Era | Item | kg | Worth | Fragility |
|---|---|---|---|---|
| Bronze | Sealed Amphora | 3 | 120 | 4 → **6** |
| Bronze | Oxhide Ingot | 4 | 180 | 999 |
| Bronze | Faience Hippopotamus | 0.5 | 420 → **700** | 2 → **4.5** |
| Bronze | Tripod Cauldron | 12 | 650 → **1000** | 999 |
| Bronze | Gold Death-Mask (artifact) | 1 | 1400 | 7 → **8** |
| High Med. | Silver Ewer | 2 | 180 | 999 |
| High Med. | Illuminated Psalter | 1 | 350 | 5 → **6** |
| High Med. | Coin Coffer | 8 | 450 → **600** | 8 |
| High Med. | Arm Reliquary (artifact) | 1 | 600 → **650** | 3 → **6** |
| High Med. | Gilded Altarpiece (artifact) | 14 | 1400 → **1800** | 3 → **6** |
| Late Med. | Banker's Ledger | 1.5 | 140 | 999 |
| Late Med. | Rolled Tapestry | 11 → **8** | 650 → **800** | 999 |
| Late Med. | Gilded Nef | 4 | 1100 | 3 → **6** |
| Late Med. | Jewelled Hat Badge (artifact) | 0.5 | 1200 | 999 |
| Late Med. | Parade Armour on its Stand | 13 | 900 → **1500** | 6 → **8** |
| Powder | Silver Service Tureen | 3 | 260 | 6 → **7** |
| Powder | Astrolabe | 1.5 | 320 | 8 |
| Powder | Venetian Mirror | 6 | 700 → **900** | 2 → **4.5** |
| Powder | Cabinet of Curiosities | 12 | 1200 → **1500** | 4 → **6** |
| Powder | Nautilus Cup (artifact) | 1 | 1600 → **1800** | 2.5 → **4.5** |
| Legacy | Copper Pot | 1 | 100 | 3 → **4.5** |
| Legacy | Golden Goblet | 2 | 150 | 3 → **4.5** |
| Legacy | Silver Plate | 1.5 | 100 | 2 → **4.5** |
| Legacy | Ancient Relic (artifact) | 5 | 500 | 3 → **6** |

Effect on placement: only Late Medieval's order changes. It becomes Ledger (wall) → Tapestry →
Nef → Hat Badge → Parade Armour (crypt only), the same shape as the other eras.

## Where the numbers live

The source is `docs/art/data/<era>.json` (`worth`, `bulk`, `fragility`). `EraContentForge`
builds the `LootItem` assets, prefab masses and the per-era `RaidLootTable_*` from it. Applying
this means editing the JSON, then writing the same values into the existing assets and tables
through the Editor. Re-running the whole forge is not needed, and it would reset hand-tuned prefab
values.
