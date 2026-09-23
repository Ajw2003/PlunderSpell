# Castle revamp (started 2026-09-23)

The user's brief, after playing the generated castle:

> the castle pieces and its layout could use a revamp and to actually be walked through to ensure
> they are navigable. Currently the castle rooms all feel sort of disjointed with no real theming
> throughout other than medieval, with stairs that go nowhere and doors that go nowhere, floating
> castle pieces and no env hazards or sensible locations for loot to spawn.

Every phase is verified in the Editor: an overhead render, a contact sheet of every room type from
eye height, a navigation audit, and screenshots in `docs/generated/castle-survey-2026-09-23/`.
Each phase is committed and pushed on its own.

## What the survey found (before)

Overhead: `docs/generated/castle-survey-2026-09-23/01-overhead-before.png`. Rooms from eye height:
`02-rooms-before-sheet.png`.

- **A grid of identical boxes.** A 7×7 block of 12 m rooms, every one the same grey open-roofed
  shell with an archway on all four sides and 2–5 small props. Room types are picked per cell by
  zone and weight, not by what's next to them, so a kitchen sits between a stable and a throne
  room. Courtyards are simply missing boxes.
- **Floating battlements.** `build_wall_straight` and `build_drawbridge` run `crenellations` around
  the whole 12 m cell, but those pieces only have a wall on one side: three sides of merlons hang in
  mid-air over every wall run. The ArmouredCourtyard has a free-standing slab.
- **Stairs to nowhere.** `KeepStairwell` and `CryptStairwell` are spiral stairs in a one-storey
  castle; they climb into open sky.
- **Doors to nowhere.** No `CastleDoor` exists in any prefab or scene. Porta has nothing to open,
  and the alarm's lockdown locks nothing.
- **Loot on the walls.** The planner puts loot on curtain-wall cells, which are open strips between
  the rampart and the rooms, and on bare floor rather than on furniture.
- **No hazards.** Nothing in the castle can hurt or expose a player except guards.

## The navigation audit (phase 1, done)

`Tools/Plunderspell/Audit Castle Navigation (Play mode)` (`Assets/_Project/Scripts/Editor/CastleAudit.cs`)
builds the castle for five seeds in CastleBench and checks 25 floor points per room, and every
loot piece, against the real baked NavMesh from the spawn. `Tools/castle_overlay.py` draws the
result over an overhead render. Before any fix (`audit-before.md`, `04-walkable-floor-before-seed12345.png`):

| Seed | Rooms you cannot enter | Floor you can walk to |
|---|---|---|
| 777 | 4 / 44 | 88% |
| 2024 | 33 / 44 | 22% |
| 31337 | 33 / 44 | 23% |
| 90210 | 38 / 44 | 13% |
| 12345 | 43 / 44 | 1% |

The cause: set-pieces are placed through the middle of rooms, so they cut the walk between
archways. The castle is entered through one archway at the gate; when the room behind it is, say, a
StableBlock whose stalls run wall to wall, nearly the whole castle is sealed off. Guards path on the
same NavMesh, so they are stuck the same way. `CastlePathValidator` never saw it because it checks a
rasterised grid, not the real geometry. Loot fares no better: 4–7 pieces per seed land outside any
room and 5–10 inside geometry.

## Phases

1. **Audit harness.** An Editor tool that generates castles for a set of seeds and reports:
   whether every room and every loot point is reachable on the NavMesh from the spawn, loot points
   that fall outside a room or inside geometry, and any mesh island floating above its module's
   floor. The asset build gains the same floating-island check so it can't regress. Output: a
   report plus overhead and room renders.
2. **Fix what's broken.** Crenellations only where there is a wall; no free-standing slabs; stairs
   that lead somewhere or are removed; loot only inside rooms, on authored anchor points (tables,
   shelves, plinths) instead of the bare floor.
   **Done 2026-09-23.** On seeds 777, 2024, 31337, 90210 and 12345: 0 rooms unreachable, 100% of
   floor reachable, and 0 loot pieces unreachable, outside a room or inside geometry
   (`docs/generated/castle-survey-2026-09-23/audit-after-loot-anchors.md`). Captures of loot on
   furniture and the rebuilt keep stair: `docs/generated/playtest-2026-09-23/loot-*.png`,
   `keep-stair-L.png`, `keep-gallery-chest.png`.
3. **Themed wings.** Rooms are grouped into wings that read as places: a service wing (kitchen,
   storehouse, well, stables), a military wing (barracks, guard room, smithy, armoured yard), the
   ceremonial inner ward (great hall, chapel), the keep (throne, solar, bedchamber, treasury) and the
   crypt below it. Each wing gets its own floor/trim accents and banners, so walking between them
   reads as moving through a building. Courtyards become outdoor spaces with purpose.
4. **A crypt you walk down into.** The crypt sits a storey below the keep, reached by a real
   stairwell; this also gives the "stairs that go nowhere" a place to go.
5. **Doors and hazards.** Real doors in the archways between wings and on vaults (lockable, so
   Porta, Frango and the lockdown all mean something), plus a small set of telegraphed hazards
   (e.g. noisy pressure plates, fire, a gatehouse murder hole) tied to #35 and #44.

Phases 3–5 change how the game plays. Each lands as its own commit so any of them can be turned
back without losing the others.
