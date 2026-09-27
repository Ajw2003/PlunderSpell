# Staging follow-ups — 2026-09-24

Status: **done on `claude/staging-2026-09-24`, 2026-09-24; awaiting the user's retest.** Results
at the end.

Follows [`merge-2026-09-24-art-branches.md`](merge-2026-09-24-art-branches.md). Enemy animation
is deliberately **out of scope** (the user's call: a separate job, next).

Work happens in the worktree `PlunderSpell-trialmerge` (checked out on
`claude/staging-2026-09-24`), where a Unity 6000.3.15f1 Editor is open and drivable with the
Unity CLI (`unity command … --project-path "<worktree>" --caller plugin --skill unity-cli`).
`main` is not touched.

## A. Merge follow-ups

1. **Roster era tags.** In `Assets/_Project/Scripts/Editor/EraContentForge.cs`,
   `ForgeEnemies`, set `Era = era` on both `new EnemyRoster.Entry { … }` initialisers. Today every
   forged entry keeps the default `HighMedieval`, so Bronze, Late and Powder raids hit the any-Age
   fallback in all 5 zones (15 warnings; see `docs/generated/merge-audit-2026-09-24/`).
2. **Warning text.** `EnemyRoster.WarnFallback` tells the reader to run
   `Tools/Plunderspell/Forge Art Bible Enemies`, which does not exist. Point it at
   `Tools/Plunderspell/Forge Era Content (rooms, loot, enemies)`.
3. **The 19 Late Medieval pieces.** `EraContentForge.LateRooms` is hard-coded to 10 rooms. Add
   castle-bench's 15 rooms and wall pieces and its 4 door plugs, with zones from
   `docs/art/rooms/LateMedieval.md` / `docs/art/rooms/data/LateMedieval/`, mirroring how the Bronze
   Age set (including its door plugs) is registered. Once the Late set covers every zone, the
   High Medieval fillers (`GatehouseModule`, `WallStraight`, …) should drop out of the Late registry.
4. **Re-run `Forge Era Content`.** This also posts the 6 enemies no roster has yet, because the
   forge reads ArtForge's manifest. It also applies era's pending "keep each era's weapons in its
   loot table" change, which was never re-run.
5. **High Medieval and Age of Powder rooms: not done, deliberately.** High Medieval's rooms *are*
   the scene default, so an explicit registry would change nothing. Age of Powder has no room art
   yet (26 rooms unmodelled). Record this rather than inventing content.

## B. Grip points (user issue 1)

Symptom: every item is held by its base. Cause: `LootPickup.ParentToHandSocket` sets
`localPosition = _handLocalOffset`, which is zero on every forged prefab, so the item's pivot
(which ArtForge puts at the base of the model) goes in the hand.

Fix, per item, from the art bible's `grab` text (`docs/art/data/<age>.json`, `items[].grab`):

- Give `LootPickup` an optional serialized `Transform _gripPoint`. When it's set, parent so the
  grip point lands on the hand socket (account for the item's local scale and rotation, which is
  identity under the socket). When it's not set, keep today's behaviour exactly. Check the
  dual-carry joint path (`CreateCarryJoint`) still anchors sensibly.
- `EraContentForge` gives each forged loot prefab a `GripPoint` child, placed from a small
  per-slug table of the primary one-hand grip in model space, derived from the `grab` prose and
  `dimensions` (e.g. nautilus cup: the stem, 0.08 m above the foot; arm reliquary: mid-sleeve
  ~0.22 m; ewer: the handle; parade armour: the post at ~0.6 m). Where the prose gives no single
  point, use the centre of the renderer bounds. Put the table beside the forge code, with a
  one-line source note per row.
- Tests: an EditMode test that every forged loot prefab has a grip point inside its renderer
  bounds. A test that after pickup the grip point's world position equals the hand socket's.

## C. Remove the old inventory (user issue 2)

Unused by the carry-based loot loop, but still reachable: Tab during a raid opens its grid. Remove:

- `Core/GameFlow/InventorySystem.cs`, `UI/Screens/InventoryScreen.cs`,
  `Inventory/PlayerInventory.cs` (never referenced), and their `.meta` files
- `GameServices.Inventory` and its construction; `UIRoot`'s inventory screen
- `GameFlowInput`'s Tab toggle
- `GameState.Inventory` and every check on it (`RaidBootstrapper`, `RaidHudView`, `UIRoot`,
  `PausePolicy`, tests that list or enter states)
- `Tests/EditMode/InventorySystemTests.cs`, the `04_Inventory` capture in
  `UIScreenshotPlayModeTests`, `UI_Verification_Screenshots/04_Inventory.png`
- Doc mentions in `docs/` (systems pages, README tables) that describe it as live

**Keep `InventoryItem`**: it's the loot item data asset (10 asset references, used by the forge and
`MeleeWeaponStats`). Check whether `GameState`'s serialized int values are stored in any
asset/scene before removing an enum member from the middle.

## Verify

In the live Editor: no compile errors; `audit_era_filter.cs` shows 0 fallback warnings and every
era served by its own entries; `audit_inventory.cs` shows all 16 enemies rostered and 0 Late models
without a prefab; `audit_render.cs` re-rendered (the sheets update). EditMode and PlayMode suites
pass (baseline: 26/26 and 183/183; inventory tests removed will lower the counts, so say by how
much). Record results here and in `docs/5-today/Today.md`, then commit and push.

## Results (2026-09-24)

Done by the planning session directly, at the user's request (two executor agents had been
launched by mistake for one request and were stopped before changing anything).

| Commit | What |
|---|---|
| `61cd41b` | A1-A3: forged roster entries carry their era; the warning names the real menu item; the full Late Medieval set (25 rooms and wall pieces, 4 door plugs, stand-in roles incl. Effigy Crypt as `CryptChamberFinal`) |
| `8d8d455` | A4: Forge Era Content re-run: the 6 missing enemies posted, 19 Late prefabs built, era weapons in the loot tables |
| `acb96fb` | B: `LootPickup.AttachToSocket` keeps the spawn rotation (the Z-up correction) and puts the `GripPoint`, or the mesh centre, on the socket; the forge places `GripPoint` per item from `GripFractions`; `LootGripTests` |
| `67a326a` | B: loot re-forged with grip points; `loot-held.png` renders every item as held |
| `d579b58` | The 6 new enemy prefabs registered as network prefabs (PurrNet did it on refresh) |
| `c8d7752` | `HistoricalEra` registered with PurrNet in its own assembly (`Inventory/NetworkTypes.cs`) |
| `a22f982` | C: old inventory removed (also `ItemDefinition` and `ItemStack`, which only it used) |

**Correction, 2026-09-24 (playtest 2):** part B's grip fix went into `LootPickup`'s hand-socket
path, which raids don't use. Raids carry items through `ItemManager`/`Item`, so the user still saw
items held by their base. Fixed there in `staging-playtest-2-2026-09-24.md`, part 1.

Found along the way:

- **Items were also held tipped over.** `ParentToHandSocket` zeroed the rotation, which dropped
  the ArtForge models' 270° X import correction. Reproduced in the Editor: the Arm Reliquary,
  0.52 m tall as placed, was 0.14 m tall and 0.52 m long in the hand. Fixed with the grip change.
- **Removing `PlayerInventory` broke era replication.** It was the only network class in
  `Plunderspell.Inventory`, and PurrNet generates a type's serializer only in the assembly that declares
  it (`Assets/PurrNet/Codegen/GenerateSerializersProcessor.cs:95`) and only if that assembly sends
  it. Without it, `RaidSceneCastingTests` failed with "Failed to write value of type
  HistoricalEra". The attribute on `RaidDirector` did not help (wrong assembly). Verified both
  ways: the 2 tests pass with `PlayerInventory` restored, fail without it, and pass without it
  once `NetworkTypes.cs` registers the type.
- A5 stands: High Medieval raids use the scene default rooms, and Age of Powder has no room art.

Verified in the live Editor on the final code:

- `audit_era_filter.cs`: every era's 5 zones served by its own enemies, 0 fallback warnings (was 15).
- `audit_inventory.cs`: all 16 enemies rostered; Late Medieval 29 models, 29 prefabs.
- Forge report: Bronze and Late 25 era rooms + 4 door plugs each; 4 enemy types per era.
- **EditMode 24/24** (26 before, minus the 4 `InventorySystemTests`, plus the 2 `LootGripTests`).
  **PlayMode 183/183.** Raw results: `docs/generated/merge-audit-2026-09-24/followups-*-tests.json`.

Not verified by a person yet: how the grips feel in play. Where the prose names a handle but not
which side it is on (ewer, amphora and similar), the grip sits at the handle's height on the centre line.
