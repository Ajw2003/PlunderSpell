# Merge plan — the three art branches of 2026-09-24

Status: **merged to `claude/staging-2026-09-24` for testing; not in `main`.** Approved by the user
on 2026-09-24, with two changes: merge into a staging branch first so `main` stays in a working
state until it has been playtested, and keep the AllEnemies prefabs.

## Staging result (2026-09-24)

`claude/staging-2026-09-24`, branched from `origin/main` (`30bbd94`):

| Commit | What |
|---|---|
| `817184c` | merge `era-content-integration` (no conflicts) |
| `3c84958` | merge `dreamy-curie-jnrkbu` (`RaidDirector.cs` resolved as in the trial; `ProjectState.md` entries rewritten as one, since each contradicted the other; `Today.md` kept every entry) |
| `9612057` | merge `castle-bench-rooms-mwucab` (only `ProjectState.md`'s M3 row conflicted; both halves kept) |
| `d951c12` | `.meta` files for castle-bench's 19 FBX, which that branch never committed |
| `fecf85b` | UI verification screenshots the PlayMode run re-captured |

Before the merge, `dreamy-curie-jnrkbu` got `90b02bf`, which tracks the `.meta` files for
`ArtBibleJson.cs` and `EnemyBodyProfile.cs`.

Checked in the live Editor on the staging branch: no compile errors
(`compilationFailed: false`, 0 console errors), both branches' code present (`ApplyEraContent`,
`RaidContext`, `EnemyRoster.Entry.Era`). **EditMode 26/26 passed. PlayMode 183/183 passed.** The raw
results are in `docs/generated/merge-audit-2026-09-24/staging-*-tests.json`. None of the follow-up
work below has been done, so testing will show the 15 roster fallback warnings, the 6 unrostered
enemies and the 19 unprefabbed Late rooms.

Three branches had commits on 2026-09-24. This plan says what each one holds, what overlaps,
the order to merge them in, and what still has to happen once they are in. Every claim marked
**(Editor)** was checked in a live Unity 6000.3.15f1 Editor, driven by the Unity CLI, against a
trial merge of all three branches (`claude/trial-merge-2026-09-24`, commit `bc8a6f5`). The
scripts that were run and the screenshots they made are in
[`docs/generated/merge-audit-2026-09-24/`](../generated/merge-audit-2026-09-24/README.md).

## What each branch holds

| Branch | Based on | Holds | Scripts touched |
|---|---|---|---|
| `claude/dreamy-curie-jnrkbu` | `main` tip (`30bbd94`) | All 16 art-bible enemies modelled and rigged by ArtForge (6 new, 10 rebuilt in place, same GUIDs). 16 hand-made `Assets/Models/ArtBible/AllEnemies/*.prefab`, scale-checked. Animation and engine plans (docs only, approved). | `EnemyRoster` gets per-entry `Era` + `AnyEra` and an era-filtered `PickForZone`; `GuardSpawner.SpawnFor(…, era)`; `RaidContext`; `RaidDirector` era replication; `CastleGuard` + `EnemyBodyProfile` + `GuardAttackSignal`; `ArtBibleJson` |
| `claude/era-content-integration` | `main` tip (`30bbd94`) | `EraContentCatalogue` and `Tools/Plunderspell/Forge Era Content`. Forged, for four eras: 29 Bronze room prefabs, 10 Late Medieval room prefabs, 20 loot item prefabs + 4 loot tables, 10 gameplay enemy prefabs + 4 rosters. `RaidScene` points at the catalogue. | `EraContentCatalogue`, `EraContentForge`, `RaidDirector` era replication + `ApplyEraContent`, `EraContentTests` |
| `claude/castle-bench-rooms-mwucab` | old `main` (`6f94205`, 72 commits behind) | The Late Medieval castle set completed as art: 15 more rooms + 4 door plugs as FBX, with room sheets, generators, previews and a contact sheet. **No prefabs, no registry entries.** | none (`CastleLootAnchors.json` only) |

## Duplicate work

1. **Era replication in `RaidDirector` was written twice**, by dreamy and by era. Both add the same
   `SyncVar<HistoricalEra> _era` and the same `Era => _era.value`. This is the only real merge
   conflict (3 hunks). They don't compete: era calls `ApplyEraContent(Era)` (swaps in that era's
   whole roster, loot table and room registry), while dreamy publishes `RaidContext` and passes
   `Era` to `GuardSpawner`. The resolution keeps both. See the trial merge's commit `c8441de` for
   the exact result, which compiles **(Editor)**.
2. **Two ways to keep a raid to its own era's enemies.** Era swaps in one roster asset per era.
   Dreamy tags each roster entry with an `Era` and filters on it. Merged, era's forge writes
   entries without setting `Era`, so every entry keeps the default `HighMedieval`. Result
   **(Editor)**: Bronze, Late and Powder raids find no entry for their own era in any of their 5
   zones, fall back to the whole pool, and log 15 `[Roster] No <era> enemy garrisons …` warnings.
   The right guards still spawn, because each roster only holds its own era. Fix: set
   `Era = era` on both `new EnemyRoster.Entry` lines in `EraContentForge.ForgeEnemies`, then re-run
   the forge.
3. **Two enemy prefab sets for the same 10 enemies.** `Prefabs/Enemies/<Era>/*.prefab` (era) have
   the collider, NavMeshAgent, `CastleGuard` and `NetworkTransform`, and are what the rosters
   spawn. `Models/ArtBible/AllEnemies/*.prefab` (dreamy, by hand) are the bare model with no
   components, at the same height as the forged ones **(Editor)**. Only the forged set is used in
   play. The AllEnemies set works as an art and scale reference; keep it or remove it (decision
   below).
4. Dreamy's fallback warning tells you to run `Tools/Plunderspell/Forge Art Bible Enemies`. **That
   menu item does not exist** on any branch. The one that fills rosters is
   `Tools/Plunderspell/Forge Era Content (rooms, loot, enemies)`. Fix the message text.

Nothing else overlaps. Castle-bench shares no files with era or dreamy apart from
`docs/3-state/ProjectState.md`, and merged with no conflicts.

## Order

1. **`era-content-integration` → `main`.** Fast-forward-able: 2 commits on the `main` tip.
2. **`dreamy-curie-jnrkbu` → `main`.** Resolve `RaidDirector.cs` as in `c8441de`, keeping both
   edits. Merge `docs/3-state/ProjectState.md` and `docs/5-today/Today.md` by hand: both sides added entries, keep
   all of them. `docs/art/HANDOFF.md` merges on its own.
3. **`castle-bench-rooms-mwucab` → `main`.** No conflicts in the trial. Merge `main` into it first
   if you want CI to run on it against today's code.
4. Commit and push the untracked `ArtBibleJson.cs.meta` and `EnemyBodyProfile.cs.meta` in the
   main checkout. Unity generated them for dreamy's new scripts, and without them the GUIDs
   change on every fresh clone.

## Still to do once merged (not duplicates; nobody has done these)

| # | Work | Why |
|---|---|---|
| 1 | Set `Era` on forged roster entries, fix the menu name in the warning, re-run **Forge Era Content** | Duplicate-work items 2 and 4 above |
| 2 | That same re-run posts the 6 new enemies (Keeper of the Flame, Gothic Man-at-Arms, Handgunner, Pavisier, Cuirassier, Petardier) | The forge reads ArtForge's manifest, which dreamy updated. None of the 6 is in a roster today **(Editor)** |
| 3 | Add castle-bench's 15 rooms + 4 door plugs to `EraContentForge.LateRooms` (the list is hard-coded to 10), and re-run | 19 Late Medieval FBX have no prefab **(Editor)**. The Late registry fills its Outer Bailey, Curtain Wall and Crypt with 19 generic High Medieval rooms (`GatehouseModule`, `WallStraight`, `CryptStairwell`, …) |
| 4 | Room registries for High Medieval and Age of Powder | Both have catalogue entries with `rooms=-` and raid in the scene's default rooms **(Editor)** |
| 5 | Era's own unfinished items: re-run the forge change that keeps each era's weapons in its loot table, and run the EditMode tests | Its commit `463949d` says neither was done |
| 6 | Animation: nothing is implemented | No enemy prefab has an Animator, and no AnimatorController exists under `Assets/_Project` or `Assets/Models` **(Editor)**. The 16 clips Unity sees are each `.blend`'s whole 10.38 s Blender timeline, imported as one take named `Scene`, not game clips. The approved plan is in `docs/generated/enemy-animation-plan/` |

## Decisions

- **The AllEnemies prefabs:** kept, as the art/scale reference (user, 2026-09-24).
- **Where to merge:** `claude/staging-2026-09-24` first; `main` only after the user has tested
  staging (user, 2026-09-24).
