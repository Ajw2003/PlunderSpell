# Branch consolidation (plan, 2026-09-25): awaiting approval

Replaces the order in [`merge-2026-09-24-art-branches.md`](merge-2026-09-24-art-branches.md). Most
of that plan has already happened on `claude/staging-2026-09-24`.

## Where every branch stands (fetched 2026-09-25)

| Branch | Ahead of staging | Behind staging | What its unmerged commits hold |
|---|---|---|---|
| `main` | 0 | 79 | — (staging contains all of main) |
| `claude/staging-2026-09-24` | — | — | main + the art merge: all 16 enemies prefabbed and in their Age's roster, with roster entries tagged by Age |
| `claude/dreamy-curie-jnrkbu` | 14 | 40 | Enemy engine E0–E4 (import settings, Age filter, attack signal, a second prefab/roster forge, tests); AnimForge and the Lantern Warden's first 21 clips; headless harness repairs; your house-rules handoff doc |
| `claude/era-content-integration` | 8 | 77 | Night-atmosphere spec, plan and look renders (docs and renders only) |
| `claude/trial-merge-2026-09-24` | 2 | 24 | Merge commits only. It was a verification branch, and holds nothing of its own |
| `claude/castle-bench-rooms-mwucab`, `claude/coop-weapons`, `claude/resizable-window` | 0 | many | Fully merged already |

## Trial (done, in a throwaway checkout)

staging + dreamy + era, merged in that order:
- **dreamy into staging:** no conflicts. `RaidDirector` ends with one Age field, era's content swap
  and dreamy's `RaidContext` side by side.
- **era:** conflicts only in `docs/1-landing/README.md` and `docs/5-today/Today.md`, where both sides added entries.
  Resolved by keeping both.
- **Headless build:** clean, after the harness fixes now committed on dreamy (`a43d041`).
- **Tests:** 185 passed, 35 failed. Every one of the 35 also fails on staging alone or on dreamy
  alone. They are harness gaps (real prefabs, scenes, physics). None is caused by the merge.

## Proposed order

1. Merge `claude/dreamy-curie-jnrkbu` into `claude/staging-2026-09-24`.
2. Merge `claude/era-content-integration` into staging, keeping both sides of the two docs.
3. Open the Unity editor on staging. Let it compile and reimport. `ArtBibleModelImporter` will
   reimport `Assets/Models/ArtBible` as Humanoid, which changes those `.meta` files. Commit that.
4. Fast-forward `main` to staging. Staging contains all of main, so no merge commit is needed.
5. Delete the branches that are fully merged, only once you say so: trial-merge, castle-bench-rooms,
   coop-weapons, resizable-window, and afterwards staging, era and dreamy.
6. New work (the enemy animation phases) continues on one fresh branch off the new `main`.

## Decisions that follow the merge (not blocking it)

- **Two enemy forges.** Staging's `EraContentForge` already built and posted all 16 enemies. The
  prefab/roster half of dreamy's `ArtBibleEnemyForge` duplicates it. Recommended: remove that half.
  Keep dreamy's importer, Age filter, attack signal and tests.
- **`Assets/Models/ArtBible/AllEnemies/*.prefab`:** keep them as the art and scale reference, or
  delete them.
