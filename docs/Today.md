# Today

**2026-09-25, night — the night atmosphere is built (steps 0-4 of five) on `claude/night-atmosphere`.**
Fog with fire halos, alarm-state blending, fires from pipeline anchors, the `Plunderspell/Surface`
shader, the dressed outer bailey and courtyards, the sealed gate, Low/Medium/High with a Deck
default and a Graphics setting. Looks checked by eye against the Blender renders:
`docs/generated/night-atmosphere-2026-09-25/wip11/`. Navigation audit with dressing: 44/44 rooms,
100% floor. Not done: volumetric fog (step 5), vertex soot bake, profiler timings, a play-through
by a person. PlayMode suite not run. How it works: `docs/systems/atmosphere.md`.

---

**2026-09-25, evening — night atmosphere build started on `claude/night-atmosphere`; step 0
(portal, sealed castle) done.** Branched off `claude/staging-2026-09-24` to build all of
[`docs/plans/night-atmosphere.md`](plans/night-atmosphere.md). Step 0: raids arrive at a seeded
spot inside the walls through a portal that is also the way out, anyone outside it when the clock
runs out is left behind, the gatehouse and wall tops are sealed, and the outside is gone. Verified
in the live Editor by playing solo raids through the menu calls: arrival, extraction (saved 1),
left behind (Lair line "· 1 left behind"), and the navigation audit from the portal (44/44 rooms,
100% floor, 5 seeds). Captures: `docs/generated/portal-arrival-2026-09-25/`. Also fixed
`Tools/Unity/run_tests.sh` (a Python 3.13 syntax error), and added `recompile.sh` and
`capture.sh` beside it.

---

**2026-09-25 — a throwaway "calm" night-look preview in RaidScene.** Built the quick preview asked
for after the look-anchoring session below: `RogueAi.Atmosphere.NightLookPreview`
(`Assets/_Project/Scripts/Runtime/Atmosphere/NightLookPreview.cs`, new `RogueAi.Atmosphere` asmdef)
darkens RaidScene, adds a runtime URP Volume (ACES, bloom, colour grade, vignette), and places
primitive braziers, wall torches and stand-in props along the generated castle's curtain wall,
values taken from `Tools/LookSamples/render_look_samples.py`'s `LOOKS["calm"]`. Wired into
`RaidScene` and saved via the live Editor (`unity` CLI). Verified in Play mode: `RaidDirector.Castle`
built and the scene rendered dark with warm fire pools and no compile/runtime errors from this
code (one pre-existing, unrelated PurrNet loot-spawn error appears because this scene's solo Play
mode never starts a NetworkManager or spawns a player — captures below use a temporary camera at
`CastleSpawnResolver.ResolveSpawn`, not the player's own camera). Two tuning rounds against
`docs/generated/look-samples-2026-09-24/calm.png` (fog/ambient/exposure warmed up). Captures:
`docs/generated/night-look-preview-2026-09-25/spawn-view.png` and `bailey-view.png`. Stand-in only —
see `docs/ProjectState.md` for what it doesn't do; superseded once
`docs/plans/night-atmosphere.md` steps 1-2 are built.

---

**2026-09-24, night — the look is anchored.** The user asked to polish before building more, and to
settle the aesthetic first. Decided, section by section: night, warm fire glowing in fog under a
faint moon; a castle that is calm, then lights up and reddens with each alarm state; one stylised
surface shader; an outer bailey dressed inside the wall; fog, post-processing and Low/Medium/High
quality levels that run on a Steam Deck. The spec is [`docs/plans/night-atmosphere.md`](plans/night-atmosphere.md);
nothing is built yet. The look samples were rendered in Blender (`Tools/LookSamples/`) because the
Unity Editor was stuck on a "Recovering Scene Backups" dialog, which still needs a person to click
Yes.
**2026-09-24, evening — playtest 2 on staging: carrying, loot amount, the Late centre.** The user's
second test on `claude/staging-2026-09-24` found three things, all fixed there.
- **Carrying.** Held items lagged and swung when walking. Raids carry through `ItemManager`/`Item`,
  a strength-capped pull toward the crosshair, and walking was going through the same pull as the
  mouse. Now the body carries the item and only the mouse is weighted. This is also where the grip
  fix belonged; last round's was in a path raids don't use.
- **Loot.** Raids held about 22 items, one per room at most. Now 44-53: several per room, one per
  anchor, and the crypt centre is full.
- **The Late Medieval centre.** The Effigy Crypt's tomb sat in a corner. It's re-laid with a gilt
  floor brass under a candle hearse. The walkway rule rules out a tomb in the middle.
EditMode 25/25, PlayMode 186/186. Details:
[`docs/plans/staging-playtest-2-2026-09-24.md`](plans/staging-playtest-2-2026-09-24.md).

---

**2026-09-24, evening — staging follow-ups done: every enemy and room wired in, grips fixed, old
inventory gone.** On `claude/staging-2026-09-24`, at the user's request after their first test. All 16
enemies are in their era's roster, with no fallback warnings, and all 29 Late Medieval pieces are
registered. Carried items were held by their base *and* tipped on their side; they're now upright
and held at a grip point placed from the art bible. The Tab grid inventory is removed. That removal
exposed a hidden dependency: PurrNet had only been generating `HistoricalEra`'s serializer because
of the unused `PlayerInventory`, so the type is now registered explicitly. EditMode 24/24, PlayMode
183/183. Next: the user retests staging; enemy animation is the next job. Details:
[`docs/plans/staging-followups-2026-09-24.md`](plans/staging-followups-2026-09-24.md).

---

**2026-09-24, evening — the three art branches are merged to staging.** Merged era, then dreamy,
then castle-bench into `claude/staging-2026-09-24` rather than `main`, so the user can playtest
before `main` changes. In the live Editor it compiles, and EditMode 26/26 and PlayMode 183/183
pass. Next: the user tests staging. The follow-ups (roster era tags, the 6 new enemies, 19 Late
rooms) are listed in the [plan](plans/merge-2026-09-24-art-branches.md) and not started.

---

**2026-09-24, evening — audit of the day's three art branches, and a merge plan.** Checked
`dreamy-curie-jnrkbu`, `era-content-integration` and `castle-bench-rooms-mwucab` in a live Editor
against a trial merge of all three (`claude/trial-merge-2026-09-24`); it compiles. All 16 enemies
are modelled and none is animated. 10 are in raid rosters; 6 are in none. The 20 loot items and 29
Bronze rooms are wired in. 19 of the 29 Late Medieval rooms are model-only. The one piece of
duplicate work is era replication in `RaidDirector`, written on two branches. Plan, awaiting
approval: [`docs/plans/merge-2026-09-24-art-branches.md`](plans/merge-2026-09-24-art-branches.md).
Screenshots and scripts: `docs/generated/merge-audit-2026-09-24/`.

**2026-09-24, later — art-bible enemies in the engine, E0–E4, as code (not yet run in Unity).**
`docs/plans/artbible-enemies-in-engine.md`: an import postprocessor (`ArtBibleModelImporter`:
Humanoid with an explicit bone map, the hound Generic, URP Lit with ×9 emission, linear data maps),
a prefab + roster forge that reads the art bible's JSON (`Tools/Plunderspell/Forge Art Bible Enemies +
Roster`), era gating (`EnemyRoster.PickForZone(zone, era, rng)`, `RaidDirector.Era` replicated,
`RaidContext` for the castle generator later), and a replicated attack signal on `CastleGuard`.
The household four leave the roster when the forge runs; the supernatural six stay, Crypt only, in
every Age. `Tools/Headless/verify.sh` had stopped compiling since the co-op work; the shims were
extended and it now builds and runs (217 tests: 187 pass, 26 fail in known fidelity gaps, 4
skipped); every new test passes. Nothing was run in the Editor: importer, forge, the avatar check,
the scale tests on forged prefabs, CombatBench and co-op are all UNTESTED. Steps are in
`docs/systems/raid-scene-assembly.md`, "Verification".

---

**2026-09-24, later — all 16 art bible enemies are modelled.** The session resumed from
`docs/art/HANDOFF.md`. Every enemy passes two consecutive full builds, and every review sheet was
checked against its concept. The main find: Blender's heat weighting can silently leave every vertex
on one bone, and validation used to pass that. `validate.py` now fails it. It caught the Dendra
Champion, which had never blended; the champion is fixed. A shared retry
(`rig.smooth_weights_with_retry`) covers the random form of the failure. Details are in the Traps
section of `Tools/ArtForge/README.md`.

---

**2026-09-24 — the eras are different raids now.** The user asked for the existing
enemies, items and structures to spawn in the game, so that two ages can be told apart even
unfinished. New: `EraContentCatalogue` (runtime) and
*Tools ▸ Plunderspell ▸ Forge Era Content* (`EraContentForge`). The forge made prefabs, loot
items, tables, rosters and registries for all four eras, and `RaidScene` now points at the
catalogue. Driven in the live Editor through Main menu → Lair → Set Out, each era spawned only its
own art. Bronze Age: 20 kinds of Bronze room, Bronze loot, Levies, Slingers and Champions. High
Medieval: the original castle, with knights, crossbowmen, wardens and hounds. Every guard was on
the NavMesh. Screenshots: `docs/generated/era-integration-2026-09-24/`. Details and limits:
`docs/systems/raid-scene-assembly.md`, "Eras".

---

**2026-09-24, end of session — enemies mid-run.** ArtForge builds rigged enemies; Lantern Warden and Alaunt War-hound are done, and 14 more were in progress when usage ran out. Resume from [`docs/art/HANDOFF.md`](art/HANDOFF.md).

---

**2026-09-24, later — the art bible's 20 plunder items are real models.** New tool
`Tools/ArtForge/`, built on EnemyForge's parts, bake, rig and export code, turns
`docs/art/data/*.json` into game models. All 20 items (5 per Age) are built:
`python3 Tools/ArtForge/build.py items` prints `20 built, 0 crashed` / `All models passed
validation.` on two consecutive runs. Output is FBX + glTF + `.blend` + baked textures in
`Assets/Models/ArtBible/Items/<Age>/<Name>/`, listed in `artforge_manifest.json`. Each
item has a review sheet in `docs/art/models/<age>/<slug>.png`, with the concept sheet
beside four renders of the model; every sheet was checked by eye against its concept.
Structures and enemies were deliberately not built (the user has another agent on
structures). Known gaps: no normal maps, LODs or damage-state meshes; silver and steel
render too warm on the review sheets. The full list is in `Tools/ArtForge/README.md`
under "Not done yet". Also: `__pycache__/` and `*.pyc` are now gitignored and
untracked, because EnemyForge's tracked caches dirtied the tree on every import.

---

**2026-09-24 — the art bible: structures, enemies and plunder for all four Ages.**
Built from the pitch mood board, per the user's request. Each Age has 3 structures, 4 enemies and
5 plunder items, and every one has a concept sheet (SVG + 2400×1600 PNG) and a handoff spec
detailed enough for a 3D artist to build from. The specs give sizes against the 1.80 m standard,
materials with hex colours, rig and animation lists, triangle budgets, and `LootItem` values.
Start at `docs/art/BRIEF.md`. The per-Age sheets are `docs/art/{bronze,high,late,powder}.md`, and
the companion mood board is `docs/generated/plunderspell-art-bible-moodboard.html`. Everything is
generated by `python3 Tools/ArtBible/build_art_bible.py` from `docs/art/data/*.json`. The SVG
generators the drawing agents used are kept in `Tools/ArtBible/generators/`.

Later the same day: the overlapping labels on the powder magazine, musketeer, curiosity cabinet and
tureen sheets are fixed. The Powder generator's `lib.py` now draws labels after all linework, on a
dark backing plate. On the mood board, each card's "Full handoff sheet" is now an expandable panel
holding the whole spec. The old links pointed at `../art/<age>.md`, which does not exist once the
page is published on its own. One fault remains: the Great Hall tapestry is 2.7 m tall, but the
Rolled Tapestry is drawn 3.40 m long; the spec says to scale the roll to 2.70 m.

---

**2026-09-20, later — the headless harness didn't work, so #14's changes were re-verified for real.**
The first pass below shipped labelled `UNTESTED:` because no Unity or .NET SDK was reachable.
Installing `dotnet-sdk-8.0` via `apt` (the earlier `dot.net` install script is proxy-blocked here;
`apt` is not) turned up a real, unrelated bug: `Tools/Headless/verify.sh` has never actually worked
in this repo — its own `.csproj` files were gitignored from the first commit and never committed.
Full account in `docs/Decisions.md`, "The headless harness's own project files were never
committed". Fixed the gitignore rule, reconstructed the three project files, and filled the shim
gaps that running it for the first time exposed (see `Tools/Headless/README.md`).

**Result: the health/damage HUD work below is now actually verified.** `verify.sh` compiles both
the Runtime and Editor projects clean (0 errors) and runs 148/162 tests green. All four new tests
in `HudAndInteractionTests.cs` pass. The twelve failures are pre-existing shim-fidelity gaps
unrelated to this change (real `.unity` scene loading, off-screen rendering, real asset import,
prefab-instance correlation — catalogued in the harness README) — confirmed by reading each
failure's message, none mention `RaidHud`, `CastleGuard`, or health.

---

**2026-09-20 — issue #14: wire up the health/damage presentation that was already half-built.**
Worked from `Plans/Priority_Queue.md` Phase 1, on branch `claude/amazing-ritchie-ga4w1t`.

## Done

- **Found and PR'd orphaned work.** `origin/claude/playable-loop-fixes` had two commits
  (`46e8936`, `2bbe81b`) pushed after PR #95 merged, never opened as their own PR: a real fix for
  `PlayerStateMachine.SpellBook` being an unserializable auto-property, the legacy-`Input`-vs-new-
  Input-System split in `PushToCastController`/`MockVoiceInputService`, an action-disposal leak, and
  a test-isolation bug. Confirmed by grep that current `main` still has the broken auto-property.
  Opened [#96](https://github.com/Ajw2003/PlunderSpell/pull/96) rather than merging it myself.
- **Issue #14 (health/damage model), the presentation half.** The damage pipeline was already
  unified — `IHealth.TakeDamage` is the one pathway `MeleeWeapon`, `NetworkedProjectile` and
  `CastleGuard`'s attack all go through, and `PlayerStateMachine` already tracks health and dies —
  but neither health was visible. `RaidHudModel`/`RaidHudPresenter`/`RaidHudView` now carry and draw
  the player's health bar, and `CastleGuard` self-registers into a new static `Active` list (mirroring
  the existing `Intruders` pattern) so `RaidHudView` can project an in-world health bar over every
  living guard. See `docs/Decisions.md`, "Enemy health bars are IMGUI, projected from world space",
  for why that path was chosen over wiring up the existing but unused `HealthBar.cs` uGUI component.
- Added `Test_TheHudShowsPlayerHealth`, `Test_TheHudReadsFullWithNoPlayerWired`,
  `Test_TheHudListsEveryLivingGuardAsAnInWorldHealthBar` and
  `Test_ADeadGuardStopsContributingAHealthBar` to `HudAndInteractionTests.cs`.

## Not verified — no toolchain available this session

Neither Unity nor a .NET SDK is installed in this container, and the proxy policy blocks fetching
either (`builds.dotnet.microsoft.com` denied). `Tools/Headless/verify.sh` is the documented
fallback but needs the SDK it couldn't download. **This code has not been compiled or run** — it
was reviewed by re-reading every edit, not verified against a real toolchain. Whoever picks this up
next should run Unity batchmode (or `Tools/Headless/verify.sh`) before treating #14 as closeable:
`PlayerStateMachine` in particular has never been instantiated bare (without its prefab's other
components) in a test before, and the two new tests that do so (`MakePlayerStateMachine`) are the
main compile/behaviour risk.

## Deliberately not done

- **The player-death consequence** (`PlayerDeadState.Enter()` just reloads the active scene) was
  left as-is. It technically satisfies "a defined consequence," but it's a hard scene reload with no
  feedback — worth a follow-up but out of scope for wiring up presentation that already existed.
- **Did not touch `#37` (melee) or `#39` (ranged aim/fire)**, the other two Phase-1 gaps — #14 was
  chosen first because both of those already assume a working damage/health model.

---

**2026-09-18 — Phase 1 verification pass.** Worked `Plans/Priority_Queue.md` Phase 1 in order,
checking each issue against the code rather than against its plan, on branch
`claude/playable-loop-fixes`.

## Done

- **Resolved the two plan documents disagreeing.** `Plans/Priority_Queue.md` (later, and written
  against "get each individual feature completed first to see if the game is fun mechanically
  before doing any more artwork") is the live order; `docs/plans/playable-state-backlog.md` is
  marked superseded for ordering and kept as the issue map. Its 55 per-issue plan links all pointed
  at `docs/plans/issues/`, which does not exist — the plans are in `Plans/`. Relinked, and `Plans/`
  is now in `docs/README.md`'s moving-parts table.
- **Fixed issue 9 on the component the raid actually uses.** It had been implemented against
  `FreeLookPlaytestController`, which is not in `RaidScene.unity` — the raid carries
  `PlayerStateMachine` + `PlayerInputController`, which had no gate at all. See `docs/Decisions.md`,
  "Issue 9's gate belongs on the raid's player, not only on the playtest harness".
- **Closed 5 issues on evidence**, not on a code read: #5, #9, #19, #20, #25. 55 open → 50.
- Regenerated the 13-image castle screenshot set as the visual evidence behind #5/#19/#25.

## Verified, on this machine

Unity 6000.3.15f1 batchmode: compile exit 0 with zero `error CS`; EditMode 12/12; PlayMode 117/118.
The one PlayMode failure (`Test_TheCursorFollowsTheGameStateAndIsFreedByLosingFocus`) was confirmed
to fail identically on unmodified HEAD — `Cursor.lockState` cannot be `Locked` with no interactive
window. It is a batchmode artifact and should not be read as a red suite.

## Deliberately not closed

- **#7 (crosshair)** and **#8 (cursor lock)** are both implemented and both unverifiable headlessly
  — IMGUI is invisible to `Camera.Render()`, and cursor lock needs a real window. They need one
  interactive play session, not more code.
- **#15 (loot discoverable)** — `LootHighlight` works and #20 is fixed, but the discoverability
  claim is visual and no capture of the focused-vs-unfocused state was made.
- **#6 (player too tall)** — only partial evidence (clear headroom under the archway at 1.65m eye
  height in `seed-12345-eye.png`); not measured against every room type.

## Surfaced, not today's job

- **`ItemGym.unity` cannot currently be used as a combat bench** (#18): nothing in it reaches
  `GameState.Playing`, so the gated body cannot move until the bootstrapped Main Menu → Lair →
  Descend route is walked, and no enemy prefab is in the scene to swing at.
- **`EditorBuildSettings` lists only `TestScene`** — `RaidScene` is not in the build list, so #53
  is more than "nobody pressed Build".
- **#37 melee only fires when the player has no `SpellBook`** — `PlayerStateMachine.Attack()` casts
  a spell if one is present and never reaches `TryMeleeSwing`.

---

**2026-09-16 — documentation audit day.** No gameplay code changed. `main` had gained the
raid-scene-assembly work, the menu → lair → raid flow, the castle orientation fix and a 21-issue
playtesting backlog since anyone last touched `docs/`, and `docs/` itself had tier 4
(`docs/systems/`) and `docs/plans/` but no tier 1, 2, 3, 5 or 6, and no `archive/` or `generated/`.

## Done

- Found `origin/claude/repo-status-check-hjp6z7`: an unmerged branch that had already written
  tiers 1, 2, 3, 5 and three missing tier-4 docs (`voice.md`, `castle.md`, `alarm.md`), cut from a
  point 11 commits behind current `main`. Adopted its tier-4 work as-is (the systems it covers
  hadn't changed) and rewrote the rest for what's shipped since. See `docs/Decisions.md`'s first
  entry for the full reasoning.
- Wrote tier 6 (`docs/Decisions.md`) from scratch — 8 real, dated decisions mined from commit
  history and the existing plan/system docs, none invented.
- Added `docs/systems/raid-scene-assembly.md` to the tier-4 index (`docs/systems/README.md`) — it
  already existed on `main` but was never indexed.
- Created `docs/generated/` and moved the three standalone HTML previews
  (`castle-generator-visualization.html`, `plunderspell-moodboard.html`, `ui-preview.html`) into
  it from the `docs/` root, plus the live GitHub-issues manifest
  (`.agent_reports/github-issues.json` → `docs/generated/github-issues.json`), updating
  `Tools/mkissues.py`'s output path and every cross-reference into the moved files.
- Moved the remaining `.agent_reports/` paper trail (the 2026-09-15 branch-integration narrative
  and test results — the docs-scaffold branch had already moved these on its own copy, but that
  branch was never merged so `main` still had the original `.agent_reports/` at the repo root)
  into `docs/archive/2026-09-15-integration/`, with the same "not actually committed" correction
  to `FINAL_MERGE_SUMMARY.md`'s self-reference that the abandoned branch had already worked out.
- Updated `docs/ProjectState.md`: the raid now runs on real authored art rather than primitives,
  the menu/lair/raid flow is live, and the 2026-09-16 issue backlog (21 open items, verified live
  via `gh issue list`) is now the concrete evidence behind M2's "acceptance unchecked" status.
- Fixed the one real drift already known from the abandoned branch: `docs/plans/plunderspell.md`'s
  own "Status" section still said "Planning complete; no implementation yet" despite M0–M3 having
  merged weeks ago. Pointed it at `docs/ProjectState.md` instead.
- Added a pointer from the project's `CLAUDE.md` to `docs/README.md`.

## Deliberately not done

- Did not write per-file doc comments or touch any `.cs` file beyond the two path corrections
  above (`Tools/mkissues.py`'s output directory) — this was a documentation-structure pass, not a
  code-quality pass.
- Did not attempt to verify M1/M2's acceptance criteria myself (real-microphone accent testing, a
  real four-player session), and did not triage the 21 open GitHub issues individually — both need
  a human (and, for the issues, actual engineering time), not a docs pass. Flagged as unchecked /
  open in `docs/ProjectState.md` instead of guessing at status.

## Surfaced, not today's job

- `docs/ProjectState.md` → "Choosing an era does nothing to the raid you get" — still true,
  re-verified by grep this pass. `CastleRoomRegistry` has no era field, so M3 needs a data-model
  change before it can produce era-specific content.
- The 21-issue playtesting backlog (`docs/generated/github-issues.json`, GitHub issues #5–#25) is
  a real, current punch list — #20 (loot physics fling) and #5/#19 (room connectivity/floating
  modules) are the ones most likely to block a real four-player playtest of M2's acceptance
  criterion.
- No real Unity Editor player build has ever been produced for this project — reconfirmed by grep
  (`BuildPipeline.BuildPlayer` appears nowhere in `Assets/`). Whether it actually runs as a
  standalone build is still unknown.

## Next, in order

1. Triage the 21 open GitHub issues against the milestones in `docs/Roadmap.md` — several (loot
   fling, room connectivity, player spawn placement) block a credible M2 playtest; others (VFX/SFX,
   menu art) don't block the loop but do block "something you'd hand a friend."
2. Get an actual four-player raid played start to finish and record what happened, rather than
   relying on the automated PlayMode suite as a stand-in for "is this fun" — same for M1's
   real-microphone, multi-accent recognition measurement.
3. Only after 1–2: decide the `CastleRoomRegistry`/loot/guard schema change M3 actually needs.

---

## Later, same day — moodboard gap-closure pass

A second pass, requested directly: audit the built game against `docs/plunderspell.md` and the
mood board pillar by pillar (not against the raid loop, which the morning's 21-item backlog
already covers), write up the findings, and file GitHub issues for everything needed to close the
gap — "exhaustive," including content that has no art or systems work behind it yet at all.

### Done

- Read the pitch bible, the mood board, every tier-3/4/5/6 doc, and walked `Assets/`,
  `Assets/_Project/{Art,Data,Prefabs,Scripts}` and `Tools/` on disk (folder-listing depth, not a
  line-by-line code read — see the plan doc's "What this pass did not check").
- Wrote up the full findings, pillar by pillar, as
  [`docs/plans/moodboard-gap-closure.md`](plans/moodboard-gap-closure.md).
- Filed 34 new GitHub issues via `Tools/mkissues_moodboard_gap.py`, following the same
  `gh issue create` mechanism as `Tools/mkissues.py`; manifest at
  `docs/generated/github-issues-moodboard-gap.json` once run.
- Pointed `docs/README.md` and `docs/ProjectState.md` at the new plan doc and backlog.

### Deliberately not done

- Did not open individual `.cs` scripts or `.asset` YAML to verify implementation details beyond
  what the existing tier-4 docs already cite — flagged explicitly in the plan doc rather than
  presented as more thoroughly checked than it was.
- Did not resolve the one open creative-direction question the audit surfaced (the bestiary's
  thematic split between household guards and arcane/fantasy enemies) — that needs a human
  decision, not more analysis; filed as its own issue with a `decision-needed` label rather than
  guessed at.
- Did not run `Tools/mkissues_moodboard_gap.py` against the real repo — this cloud session has no
  `gh` authentication or GitHub write path. The script was dry-run twice against a mocked `gh` to
  validate its logic (34 issues, no duplicate titles, every label it uses gets created first, valid
  JSON manifest written) but the actual `gh issue create` calls are untested until run locally.

### Surfaced, not today's job

- The Mystical Market (the pitch's fourth named pillar) doesn't exist in the codebase at all — not
  a raid-loop gap, a whole unbuilt system. See the plan doc §2.4.
- The Lair is built as a UI screen, not the 3D "damp, yours, and permanent" place the pitch
  describes. See the plan doc §2.5.

### Next, in order

1. Run `Tools/mkissues_moodboard_gap.py` locally (where `gh` is already authenticated) to actually
   file the 34 issues.
2. Decide the bestiary question (its own filed issue) before any more art/animation/audio work
   lands on the five divergent enemies — reworking them later is more expensive than deciding once.
3. Interleave the new backlog with the existing 21-item one per
   `docs/plans/moodboard-gap-closure.md` §4's suggested triage order.
