# Handoff — the demand page in the Lair ledger (2026-10-27)

Branch `AjsAgent/demand-ledger-page-419`, cut from `AjsAgent/staging-2026-10-26`, in its own worktree
`C:\Users\aj\Desktop\GameDev\PlunderSpell-demand` (a second Unity Editor was open on it). No pull request is open, and none
goes into `main`. Credit: commits end "Committed by AJ's agent".

## Where it stands

| Issue | What | State |
|---|---|---|
| #446 | Parent: the demand shown as a picture and glowing words (part of #419) | in progress |
| #447 | Right page says only "The Ophan demands", glowing white / amber / red by raids left | **built** (`3cf2b83a`) |
| #448 | E on the book tears the drawing out; server owns who holds it; book shows a stub; back in the book on coming home | **built** (`8986b7db`, `2fca4500`) |
| #449 | Hold R to raise the held page in front of the view, still walking | **built** (`d0718e13`) |
| #450 | A dropped page (Q with empty hands, or the carrier's death) falls where they stand; F picks it up; a page left lying is back in the book at home | **not built** |
| #456 | Parent of the owner's two notes from 20:42 UTC | in progress |
| #457 | Remove the Roman numerals, column bars and ruled lines from both pages (also under the torn page) | **not built** |
| #458 | Make the tear look properly ripped (stub, raised page and dropped page get a ragged edge) | **not built** |

Decisions: `docs/6-decisions/Decisions.md`, 2026-10-27, "The demand in the ledger is a picture and three glowing words".
Mechanism and "Built" notes: `docs/4-systems/market.md`, "The ledger". Design page `docs/plans/demand-page-419/demand-page.html`,
build plan `docs/plans/demand-page-419/build-plan.html`. Pictures: `docs/generated/demand-page-2026-10-27/`.

## The code-edit lock (read this first)

The house-rules design-loop lock is shared across the whole PC, not per thread. An approval in another thread (the
Ophan work) replaces this one, which stopped the #450 builder before it changed anything. Before building, the owner must
send exactly **"Build plan approved: demand page"** with nothing after it (text after the colon is read as the feature's
name). #456 to #458 were filed after the last approval so the lock counts them.

## Checks that passed (last runs)

- `bash Tools/Unity/demand_page_check.sh` — `fails 0` (drawing, words only, three glow colours, tear by queued E, stub,
  raise with R in the Lair and in a raid, walking while raised).
- `bash Tools/Unity/coop_lair_check.sh demand-449` — `PASS all Lair checks` (includes the client tearing, both machines
  agreeing on the holder, and the page back in the book after coming home).
- PlayMode tests: `InkSketchTests` 1/1, `LedgerPageTextTests` 3/3, `LairLedgerBookTests` 2/2, `DemandPageRulesTests` 3/3,
  `PageRaiseTests` 2/2. The full EditMode and PlayMode suites have NOT been run on this branch yet.

## What the next session does

1. Get the approval phrase above from the owner.
2. **#450** — facts found by the stopped builder: Q (Drop) is handled only in `Runtime/Loot/LootInteractor.cs:69`;
   `LootInteractor.Carried == null` means empty hands; loot is picked up with E, so F (`PickUp`) is free for the page;
   `PlayerStateMachine.Die()` publishes `LocalPlayerDied` on the owner's machine only, so that machine asks the server to
   drop the page. Planned shape: a `Lying = -2` holder value in `DemandPageRules` (still counts as torn), a SyncVar drop
   point on `DemandPageKeeper`, a local view quad with a BoxCollider so `LookTarget.IsLookedAt` sees it, separate dropper
   and picker classes. Also switch `DemandPageReader` from reading the keeper each frame to `DemandPageChanged` (keep only
   its easing per-frame).
3. **#457** — the numerals, bars and ruled lines are in the book model's palette/geometry
   (`Assets/_Project/Art/Models/Lair/LairLedger.fbx`, built by `lair_builders.py build_lair_ledger`), and faint heading
   marks still show at the top of both clean sheets. Either regenerate the ledger model without them or cover the full
   page area (sheets slightly larger and higher). The page under a torn stub must be clean vellum.
4. **#458** — replace `LedgerTornStub`'s straight strip with a ragged, fibrous torn edge (an alpha-cut texture or a jagged
   mesh), and give the raised page (`DemandPageReader`) and the dropped page the matching edge on their spine side.
5. Run both checks above again, the full suites, look at every picture, update `docs/4-systems/market.md` and
   `docs/5-today/Today.md`, swap the issues' `in progress` labels for `needs review`, and send the owner the pictures.

## Loose ends

- Unity rewrites `ProjectSettings/*`, `Assets/Settings/*` and the Spectral SDF font asset when it builds the client; these
  are left uncommitted on purpose. The stray `Assets/_Project/Art/Models/Castle/AgeOfPowder*.meta` files are not this work.
- `_to_delete/` (untracked) holds the issue bodies and an unused ink shader from the first prototype.
- `LedgerPageText.LeftPage`, `RightPage` and `DemandLines` are no longer used at runtime; #419 removes the debt code.
- The TMP glow is a soft underlay, because the ledger font uses TMP's mobile shader, which has no glow pass.
