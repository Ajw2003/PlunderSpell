# Staging playtest 2 — carry feel, loot amount, Late centre room

Status: **done on `claude/staging-2026-09-24`, 2026-09-24; awaiting the user's retest.** Choices
are the user's, from three questions asked after investigation. Results at the end.

## 1. Carrying: body rigid, mouse weighted

**Finding.** In a raid, items are carried by `ItemManager` + `Item` (physics drag), not by
`LootPickup`'s hand socket: `RaidScene.unity` has an `ItemManager` and no `LootInteractor`, and
`RaidPlayer.prefab` has neither. So the grip fix in `staging-followups-2026-09-24.md` part B was in
a path raids don't use. Each physics step `Item.FixedUpdate` steers the item's velocity toward a
point on the crosshair ray, capped by grip strength (`_gripStrength`, 180 N) and `_maxHoldSpeed`.
Walking moves that point with the camera, so the item has to chase it through the same capped
spring as mouse aim, which makes it lag and swing on WASD. It also steers the item's *pivot*
(`_rb.position`, the model's base) to the point, which is why items read as gripped at the bottom.

**Change.**
- The item inherits the holder body's velocity directly, outside the grip-strength cap. The
  capped, weighted spring only closes the gap the mouse opens (aim, scroll depth). Heavy things
  still lag and sag when you swing them, but not when you walk.
- The point steered to the crosshair is the item's grip: `LootPickup.GripPoint` if present,
  otherwise the mesh centre.
- Tests (PlayMode): a held item on a holder strafing at walking speed stays within a few
  centimetres of its target; a mouse-driven jump in the target still lags more for a heavy item
  than a light one; the grip point, not the pivot, ends at the target.

## 2. Loot: about double, richer inward

**Finding** (`audit_loot_counts.cs`, 5 seeds per era): 44 rooms and about 22 items per raid in
every era. `LootPlacementPlanner` places at most one item per room, while rooms carry 114–204
loot anchors per castle. The crypt centre gets exactly one item.

**Change.** Per-zone counts, drawn per room and never more than the room's anchors, each item on
its own anchor: Outer Bailey 0–1, Inner Ward 1–2, Keep 1–3, and the crypt centre fills every
anchor (the richest item plus others). Target about 45 per raid. It stays a pure function of
(layout, table, seed), so every peer still plans the same haul. The planner's existing tests,
updated, plus one for the count and the no-shared-anchor rule.

## 3. Late Medieval centre room: rebuild with the effigy centred

**Finding** (`centre-rooms.png`): the centre cell is always the `CryptChamberFinal` role. The
Tholos (Bronze) and the High Medieval chamber both centre on something. The Late `LateEffigyCrypt`
has its effigy and hearse in a corner and an empty middle.

**Change.** In `Tools/ArtBible/rooms/generators/LateMedieval` and
`Tools/AssetPipeline/castle_builders_late_crypt.py`, move the effigy tomb and hearse to the room's
centre and ring it with candle stands, then re-export the FBX, re-run Forge Era Content, and update
its loot anchors. Blender 5.2 is installed; the pipeline was tested on 4.0.2, so a version failure
gets reported, not worked around silently.

## Verify

Compile clean; EditMode and PlayMode suites pass; `audit_loot_counts.cs` shows about double;
`centre-rooms.png` re-rendered; the carry tests above. Record results here and in `docs/5-today/Today.md`.

## Results (2026-09-24)

| Commit | What |
|---|---|
| `e4fcb8b` | 1: `Item` rides with the holder's body and steers its grip; `ItemManager` measures reach to the grip; `CarryFeelTests` |
| `9b182c0` | 1: loot re-forged so `Item` has the grip point too |
| `833815a` | 2: several items per looted room, one per anchor; crypt centre full; `LootAmountTests` |
| `804d12b` | 3: Effigy Crypt re-laid; crypt's richest item on its first anchor |

- **1, carrying.** Red first: an 8 kg item strafed at 4 m/s trailed 0.447 m; the grip ended 0.400 m
  from the hand point (the base was being steered). After: within 5 cm, grip on the point, and a
  14 kg item still lags a 1 kg one on a mouse swing. The walking test's holder interpolates, as the
  real player does (`PlayerStateMachine.cs:138`); without that it measured 0.080 m, which is exactly
  one physics step of an interpolated item against a raw holder, not lag.
- **2, loot.** `audit_loot_counts.cs`, 5 seeds per era, after part 3: Bronze 48.2, High Medieval
  43.8, Late 52.6, Powder 43.8 items per raid (all about 22 before). Crypt 4 (Late 6), keep 11-15,
  inner ward 18-23, outer bailey 8-10.
- **3, Late centre.** A tomb in the middle is not possible in this kit: `validate_in_blender.py`
  rejects any prop below 2.0 m within 1.60 m of either centre line, the walkway between archways.
  So the user's "effigy centred" became: the founder's effigy as a flush gilt monumental brass at
  the centre, a madder hearse with a candle crown over it above head height on four posts at the
  walkway's edge, and the raised effigy tomb beside the walkway. Blender 5.2 rebuilt the unchanged
  room byte-identically first, so the version is not a factor. The new room: 992/1600 tris, all
  checks pass, a second build byte-identical. `render_previews.py` has no staleness check and
  re-rendered every preview (and stopped at a Powder room with no model); only this room's preview
  was kept, the rest restored.
- **Tests:** EditMode 25/25, PlayMode 186/186 (`playtest2-*-tests.json`).
