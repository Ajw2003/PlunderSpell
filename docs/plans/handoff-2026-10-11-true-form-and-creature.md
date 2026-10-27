# Handoff: the two-form guard, then the creature (2026-10-11)

For the next agent. Two pieces of the approved fear pass, done **one after the other**: first the
two-form guard, then the creature. Each starts with a short look-and-behaviour design the owner
approves, then the build. The spell lockdown (part 1 of the slice) is separate work and not in this
handoff.

## Where this stands (updated 2026-10-12, later)

- **All the art for both is done; the import rules are written but untested; the rest of the
  engine work has not started.** The Wick (#406):
  model plus nine clips. The Ophan of Hours (#411, #412): model plus five clips. Every one has a
  sheet that was opened and a review record in `docs/art/reviews/`. Nothing has been imported into
  Unity yet: the cloud container this was built in has no editor.
- **Designs approved:** the Wick (`docs/7-design/fear-pass.md`, "The true form") and the Ophan,
  allowed lapis (`fear-pass.md`, "The creature"; Decisions, 2026-10-11, "The Ophan may use lapis").
- **Still open with the owner:** Part A, how many more ground-floor guards (question 4 below);
  Part B, questions 2-4 (where the artifacts are placed, how it hunts, what "spoils taken" looks
  like). Ask them before the work that depends on each one, not before starting.
- **Branch:** `AjsAgent/fear-pass-handoff`, with `staging-2026-10-11` merged in on 2026-10-12 (one
  conflict, in `Decisions.md`, where both sides had added an entry; kept both). In a pull request
  into `staging-2026-10-11` since 2026-10-12, with the art and the import rules.

### Pick up here: the engine work, in order

Read **"Working in the engine"** just below first. Then:

1. ~~**Check the import rules in Unity** (#413)~~ **Done 2026-10-14**: all four steps passed (Today,
   2026-10-14; logs in `docs/generated/fear-import-413/`). One finding for step 3: put the Ophan's
   Animator on its `TheOphanOfHours_Rig` child, because its clips bind from the armature down and set
   a x100 scale on whatever object holds the Animator.
2. **The Wick in the game** (Part A): prefab, Animator, the turn on the Lantern Warden, the
   true-form state, the "guard turned" event, tests, a co-op check. Ask the guard-count question
   before touching guard placement.
3. **The Ophan in the game** (Part B): prefab and Animator first (needs no owner answers), then the
   quota once questions 2-4 are answered.
4. Each later piece goes in its own pull request into `staging-2026-10-11` (never `main`),
   `Refs #386` (never Closes/Fixes), saying what was and was not checked in Unity.

## Working in the engine (written 2026-10-12)

Everything in this section was read from the code on this branch; none of it has been run in Unity.

### The import rules: written, not yet run (#413, PR into staging)

The three problems found on 2026-10-12 are fixed in code on this branch. **None of it has been
compiled or run in Unity**: the cloud container has no editor. The four changed C# files parse with
no syntax errors under Roslyn (Microsoft's C# compiler, syntax only, no Unity references), which is
all that could be checked here. Written up in `docs/4-systems/raid-scene-assembly.md`, "The fear pass".

- `ArtBibleModelImporter.KindOf`: under `Enemies/Fear/`, names in
  `ArtBibleEnemyCatalog.FearGenericRigModels` (the Ophan) import **Generic** with root `Root`; the
  rest (the Wick) import Humanoid. The Ages' `GenericRigModels` is untouched, so
  `Test_TheGenericRigListMatchesTheArtBible` still holds.
- `Animations/Humanoid_Fear.fbx` is `Kind.HumanoidClips` (as the player's clips);
  `Animations/TheOphanOfHours_Generic.fbx` is a new `Kind.GenericClips` (Generic, avatar from the
  file, root `Root`). Both get clip names from the take, loop flags from `LoopingFearClips` and
  events from `FearClipEvents` (`Footstep` L/R, `AttackHit`, `PropDetach` LanternRing; the
  parameter is the event's string parameter).
- Emission: `ArtBibleEnemyCatalog.IsEmissiveModel` reads the Ages' list and a new
  `FearEmissiveModels` (the Ophan). The validator uses it too.
- `_FireMask` is in `LinearTextureSuffixes`. The importer's version went 1 -> 2 so materials rebuild.
- Tests: `Tests/Editor/FearImportTests.cs` (7): classification of the four fear files, the Ages'
  rules unchanged, the fire mask linear, who glows, the fear lists against `fear.json` and the
  ArtForge manifest, loop flags and events against both clip manifests.

**First thing in Unity (in this order):**
1. Recompile (`bash Tools/Unity/recompile.sh`) and fix anything the real compiler finds.
2. `bash Tools/Unity/run_tests.sh FearImportTests EditMode`, then the whole EditMode suite (the
   baseline was 243 pass / 3 skipped).
3. Reimport `Assets/Models/ArtBible/Enemies/Fear` and the two clip files; run
   `ArtAssetImportTests.ArtBibleModelsImportWithTheirOwnedSettings`.
4. Look, and keep screenshots: the Ophan's Inspector says Generic and its pupil glows; the Wick's
   avatar is valid; `TheWick_FireMask` has sRGB off; each clip file lists its takes by clip id, the
   loops ticked, and the events on `wick_stalk`, `wick_catch`, `warden_turn` and `ophan_take`;
   `ophan_arrive` previewed on the Ophan starts as a point. The validator skips `Animations/`
   (`ArtAssetImportValidator.cs:198`), so the clip files are checked by eye only.

Still open by design: **nothing receives these events yet.** The Wick's and the Ophan's prefabs
need a component with `Footstep(string)`, `AttackHit()` and `PropDetach(string)`, or Unity logs
"has no receiver" each time one fires. `wick_flinch` and `wick_stoop` are additive: set that on their
Animator layer. Takes are `<armature>|<clip>` (`ReferenceHuman|wick_hunt`,
`TheOphanOfHours_Rig|ophan_take`); the importer strips the prefix.

### The Ophan's clips, for the Animator

Generic rig, 18 bones. **Root never moves**: the NavMeshAgent moves it, and it never touches the
floor (lowest mesh point 0.12 m over every clip). Sheets and movies in `docs/art/anim/fear/ophan_*`.
- `ophan_arrive` (4 s) grows it from 2 % at heart height with a **scale** curve on `Core`. Keep scale
  curves on in the import; confirm in the preview that frame 0 is a point, not the full creature.
  It ends on `ophan_hover_turn`'s first frame, so arrive into hover needs no blend.
- `ophan_hover_turn` (6 s) and `ophan_advance` (4 s) loop seamlessly (every wheel makes whole
  turns). Drive the blend between them by the agent's speed.
- `ophan_fix_gaze` (1.5 s) gives the snap and the stopped wheels; the eye looks 25° aside and 18°
  down, a canonical direction. Aim `Eye` at the real player in code (a look-at on that bone in
  `LateUpdate`, which is per-frame work and allowed, see below).
- Not on the model or in the clips, all engine VFX: the lapis core glow, the echoes, the lightning,
  the floor's hour rings, the stone pulled back to quarry rock.

### The Wick's clips, for the Animator

As in "The art you build on" below. `warden_turn` plays on the Lantern Warden (its own humanoid
avatar), the rest on the Wick. `wick_stalk` is authored for 1.4 m/s and `wick_hunt` for 4.2 m/s on
the Wick; play them at agent speed / authored speed.

### How to work, on this project

- **Animator by code:** `WizardPlayerSetup.cs` builds the wizard's Animator Controller from its clips
  (`Plunderspell/Wizard/Build Animator Controller`, `:32`) and installs it (`:117`). Copy that shape
  for the Wick and the Ophan, so nothing is set by hand in the Inspector. Enemy prefabs are forged by
  `EnemyPrefabForge.cs` (`Tools/Plunderspell/Forge Enemy Prefabs + Roster`, `:89`).
- **New rule from staging (Decisions, 2026-10-11, "Per-frame code only for what changes every
  frame"):** `Update`, `LateUpdate` and `FixedUpdate` are only for work that changes every frame, each
  marked `// per-frame: <reason>`. Everything else is an event, a timer, or removed. The turn, the
  quota and the creature's arrival are events on `EventManager`, not polled flags. See
  `docs/4-systems/core.md`, "When Update is allowed".
- **Unity commands** (all from the repo root, never `cd` or `git -C`): check the Editor is not in
  Play mode (`Tools/Unity/README.md`, "Before anything"), then `bash Tools/Unity/recompile.sh`, then
  `bash Tools/Unity/run_tests.sh <filter> EditMode` and `PlayMode`. Never pipe them through `tail` or
  `head`. Keep logs with `git add -f docs/generated/<name>.log`.
- **Look at the result:** a screenshot of each model imported (the Wick standing, the Ophan
  floating), the clips playing in the preview, before-and-after screenshots of the guard turning, all
  opened and looked at before saying anything works. The Unity baseline on staging was compile clean,
  EditMode 243 pass / 3 skipped, PlayMode 518/518 (`docs/3-state/ProjectState.md`); keep it there.
- **To rebuild the art** (only if a clip or model must change): see `Tools/ArtForge/README.md`,
  "The fear pass's clips". Needs `python3.11` with `bpy` 5.0.1 (`python3.11 -m pip install bpy==5.0.1
  pillow`); `anim_wick.py build` and `anim_ophan.py build` each take seconds, a full review about
  twenty minutes.

## Read first

1. [`docs/7-design/fear-pass.md`](../7-design/fear-pass.md): the approved design. Its "The first
   slice" section is the spec; the "Decided" bullets above it are the owner's own words, condensed.
2. `docs/6-decisions/Decisions.md`, "2026-10-11 — Raids are about fear".
3. [`docs/4-systems/alarm.md`](../4-systems/alarm.md): how guards see, hear, chase and attack today,
   especially "Sight and hearing rules (#238)" and "The fresh guard core (#206)".
4. `docs/art/WORKFLOW.md` and the `art-pipeline` skill, before any modelling.

Issues: parent #380, this work is #386. Make one child issue per step below (`Part of #386`,
labels `AjsAgent created this` plus a category) before writing code; a hook blocks source edits
until they exist.

## Branches and how to work here (learned the hard way on 2026-10-11)

- **Target:** every pull request goes into `staging-2026-10-11`. Never into `main`.
- **Your branches:** `AjsAgent/<topic>`, cut from `staging-2026-10-11`. These are agent-owned: you
  may commit and push them without asking. `claude/` branches are the old naming; do not make new ones.
- **Never `cd` or `git -C`** in a shell command. The guard refuses any command whose repo it cannot
  follow, and a refusal must not be routed around. Run everything from the repo root.
- **No worktree isolation for builders.** A builder in `.claude/worktrees/...` must `cd` to work,
  which the guard refuses, and Unity is only open on the main folder. Work in
  `C:/Users/aj/Desktop/GameDev/PlunderSpell` on your branch, one builder at a time.
- **Unity:** the Editor runs on the main folder. Check it is not in Play mode before recompiling
  (`Tools/Unity/README.md`, "Before anything"). Use `bash Tools/Unity/recompile.sh` and
  `bash Tools/Unity/run_tests.sh <filter> EditMode|PlayMode`. Never pipe them through `tail`/`head`.
  Write their output to a log under `docs/generated/` if you want to keep it (`*.log` is ignored,
  so add it with `git add -f`).
- **Leave alone** the Unity import noise in the tree: the two wizard `.fbx.meta` files, the
  Spectral font asset, `ProjectSettings.asset`, `UI_Verification_Screenshots/01_MainMenu.png` and
  `docs/generated/nav-graph-2026-10-02/timings.txt`. Commit only the paths you changed.
- **Co-op by default:** every in-game check runs the Editor as host plus a built client, as the
  existing `Tools/Unity/*_check.sh` scripts do (`coop_lair_check.sh`, `wizard_coop_check.sh` are
  good models).

## Part A: the two-form guard

### What is decided
- The first guard to get a true form is the common High Medieval guard. In the roster
  (`Assets/_Project/Data/Enemies/EnemyRoster_HighMedieval.asset`) the most common entry is the
  **Lantern Warden** (`Assets/_Project/Prefabs/Enemies/HighMedieval/LanternWarden.prefab`, weight
  12 in two zones). **Confirm with the owner that this is the guard they mean** before modelling.
- It turns, one way and for the rest of the raid, when it confirms it has **seen** a player or when
  it is **hit**. Hearing only makes it investigate, still human.
- A missed or too-loud Somnus wakes its target, which sees the caster at once and turns.
- Human form: a hard blow to the head stuns it and knocks it down. It is never killed.
- True form: faster, sharper senses, **kills on a catch**, never turns back. Run and hide is the only
  answer.
- The turn raises a fear moment on every machine: a reveal animation, sound, camera, music.
- More guards stand on the ground floors, so the threat is seen from the gate and underestimated.

### What the owner approved (2026-10-11), and what is still open
1. **The look: approved, the Wick.** The man turned into his own lantern, burning: 3.10 m, a cage of
   bone for a head with the kettle hat as its cap, ribs opened round a candle, the glaive grown into
   one arm, 0.40 m fingers on the other hand. Written up in `fear-pass.md`, "The true form".
2. **The reveal: approved.** 1.6 s in four beats: seen (0 s), the face splits in four and the
   lantern drops (0.4 s), the coat bursts and he grows (0.9 s), turned (1.6 s). The first 0.9 s,
   while it is busy turning, is the one moment to run. The lantern is **left where it fell**, dark.
3. **The counter: approved.** It sees only by its own fire: out of its light you are hidden, in it
   you are seen.
4. **Still open: how many more ground-floor guards**, and whether that is per Age or High Medieval
   only. Ask the owner before the guard-placement change.

### The art you build on (done, #406)
- **Spec:** `docs/art/data/fear.json` (`enemies[wick]`), a `fear` group outside the Ages' rosters
  of four. Height 3.10 m (body 3.04 m), Outer Bailey and Curtain Wall like the warden; listed in
  `docs/4-systems/scale.md`.
- **Model:** `Assets/Models/ArtBible/Enemies/Fear/TheWick/TheWick.fbx` (+ `.blend`, glTF,
  `Textures/`), 10,596 tris (budget 12k). Unity Humanoid bones plus `Cage` (on Head), `Hat` (on
  Cage) and `Blade` (on Hand.R), 23 in all. Built by
  `python3.11 Tools/ArtForge/build.py enemies --age fear --only wick`; review sheet
  `docs/art/models/fear/wick.png`.
- **Fire is painted geometry for now** (owner, 2026-10-11): three-tone flame tongues, no glow,
  rigid on the bone they sit on, to be replaced by a fire shader later. The shader's mask is already
  baked: `Textures/TheWick_FireMask.png` (white = flames, grey = smoulder); preview
  `docs/art/models/fear/wick-fire.png`.
- **Clips:** `Assets/Models/ArtBible/Animations/Humanoid_Fear.fbx` (reference skeleton, Humanoid),
  listed in `Tools/ArtForge/anim_spec.json` (layer `fear`) and
  `Animations/fear_anim_manifest.json`; built by `python3.11 Tools/ArtForge/anim_wick.py build`,
  sheets and MP4s in `docs/art/anim/fear/`. All in place (the NavMeshAgent moves it):

  | Clip | Length | Plays on | Events / notes |
  |---|---|---|---|
  | `warden_turn` | 0.9 s | the Lantern Warden | `PropDetach` LanternRing at 0.444 (0.4 s): the lantern drops as a loose physics prop |
  | `wick_emerge` | 0.7 s | the Wick | swap the model at the end of `warden_turn`; rears up, settles into the hunch |
  | `wick_idle` | 3.0 s loop | the Wick | |
  | `wick_stalk` | 1.4 s loop | the Wick | agent 1.4 m/s; `Footstep` L 0.0, R 0.5 |
  | `wick_hunt` | 0.7 s loop | the Wick | agent 4.2 m/s; `Footstep` L 0.0, R 0.5 |
  | `wick_search` | 2.5 s loop | the Wick | the head (and its light) sweeps 74 deg each way |
  | `wick_catch` | 1.2 s | the Wick | `AttackHit` at 0.417 (0.5 s): the kill |
  | `wick_flinch` | 0.4 s additive | the Wick | on a hit; it does not stop |
  | `wick_stoop` | 0.5 s additive | the Wick | held by layer weight under arches: crown 2.33 m |

- **Import rules written, not run (#413):** `Humanoid_Fear.fbx` and the `_FireMask` now have rules
  (see "The import rules" above). Not done, all Unity: the Wick's prefab and Animator, the model swap
  at 0.9 s, the lantern detach on the warden, and the fire shader.

### Where it goes in the code
- `Assets/_Project/Scripts/Runtime/Guards/Core/Guard.cs` (267 lines): the guard. Its states are in
  `Guards/States/` and gathered by `GuardStateSet.cs` (Patrol, Investigate, Chase, Combat,
  HoldBelow, Stunned, Slept, OnFire, Dead). The design calls for a one-way **True form** state on
  the same guard object, swapping the visual to a second model and its tuning to a faster,
  sharper-sensed one, and networked so every machine sees the turn.
- Senses: `Guards/Senses/GuardSight.cs`, `GuardHearing.cs`. The trigger is "sight confirmed or hit".
- Health and death: `Guards/Core/GuardHealth.cs`, `GuardDeathPlayback.cs`. The design says a guard
  is never killed now; stun already exists (`StunnedState.cs`).
- Attacks: `Guards/Core/GuardAttackTurn.cs`, `Alarm/AttackTurnMediator.cs`. A true form's catch
  kills outright.
- Guard counts per floor come from the castle's guard placement (see `docs/4-systems/alarm.md`
  and `docs/4-systems/castle.md` for where posts are planned).
- Events go through `EventManager` only (Decisions, 2026-10-06): publish a "guard turned" event for
  the fear moment; views (camera, music, sound) subscribe.
- Enemy art pipeline: `docs/4-systems/enemy-asset-pipeline.md`; animations:
  `docs/plans/artbible-enemy-animations.md`; the Wick's art and clips: "The art you build on" above.
- The turn in code: play `warden_turn` on the warden, detach its lantern at the `PropDetach` event
  and leave it lying, swap to the Wick model at 0.9 s and play `wick_emerge`, then the true-form
  state runs `wick_stalk` / `wick_hunt` at 1.4 / 4.2 m/s. Raise the "guard turned" event at 0 s so
  the camera, sound and music start with the turn.

### Done when
Tests for the turn rules (sight turns, hearing does not, hit turns, never turns back, catch kills,
human form stuns and never dies); ~~the true-form model with a review sheet you opened and a review
record~~ (done: `docs/art/models/fear/wick.png`, records in `docs/art/reviews/`); the Wick imported
in Unity with its clips playing, a screenshot looked at; a co-op check script in which a guard sees the client,
turns on both machines and kills on a catch; before-and-after screenshots you looked at; the full
EditMode and PlayMode suites green; `docs/4-systems/alarm.md` updated; a pull request into
`staging-2026-10-11` with `Refs #386` (never Closes/Fixes).

## Part B: the creature (after Part A is merged)

### What is decided
- The debt and the Collector are removed. A creature demands specific **artifacts**.
- Its demand, the quota, falls due every **3 to 5 raids** by default, from a tuning asset, and is
  configurable. The Lair's **ledger book** shows the demand, with a sketch of each artifact, in place
  of the debt pages.
- Wanted artifacts are marked loot in the castles. Carried home and placed in the Lair, they pay the
  quota and the next demand appears.
- Unpaid when the count runs out: every raid is a **hunt** until it is paid. The creature arrives
  physically in the castle as the portal's clock runs low, and each hunted raid's spoils are taken.
- Caught **carrying its artifacts**: it takes them, the quota counts as paid, you live. Caught
  **empty-handed**: you die.
- Death loses what you carried and any unread spell scrolls; learned spells and the Lair stay. The
  portal closing leaves anyone inside behind. Nobody ever starts over: it is purgatory, escaped only
  by one day growing strong enough to defeat a celestial being, together.
- The Lair and the Market stand outside time and are always safe. Coin now only buys things in the
  Market.

### What the owner approved, and what is still open
1. **What the creature is and looks like: approved, the Ophan of Hours** (2026-10-11). After
   Ezekiel and Isaiah: a wheel within a wheel 5.8 m across, rims full of eyes, six wings (11 m
   across), lightning, one great eye at its heart with a portal for a pupil; 9.6 m tall, larger than
   the castle, passing through walls (the stone pulled back to quarry rock and closing behind it).
   Allowed lapis. Concept `docs/art/concept/fear/ophan-v2-*.png`; written up in `fear-pass.md`,
   "The creature". Its model is built and reviewed (#411): `Assets/Models/ArtBible/Enemies/Fear/TheOphanOfHours/`,
   11,378 tris of 16k, 9.46 m tall, a generic rig of 18 bones, sheet `docs/art/models/fear/ophan.png`.
   Not on the model, for the engine: the lapis core glow, the echoes, the lightning, the floor's hour
   rings, the stone pulled back to quarry. Its import rule (Generic, its own fear-folder
   list, its glow) is written but not yet run in Unity (#413, "The import rules" above). Its five clips are built
   and reviewed (#412): `Animations/TheOphanOfHours_Generic.fbx`. Its counter: none (carry what it
   asked for, or reach the portal first).

Still open:
2. **Where the artifacts are placed in the Lair** to pay the quota (an altar, the portal, the ledger
   desk) and how the next demand is chosen (which artifacts, per Age).
3. **How it hunts** once it arrives: speed, senses, whether it can be hidden from, how long before the
   portal closes it appears.
4. **What "spoils are taken" looks like** on a hunted raid (taken at the portal, or vanishing from the
   Lair on return).

### Where it goes in the code
- `Assets/_Project/Scripts/Runtime/Lair/LairHubManager.cs` (360 lines): `TotalDebt`
  (`LairHubManager.cs:33`), the debt's save keys (`:19`, `:91`, `:109`, `:121`), the per-session
  increase (`:31`) and the Collector line (`:63`). The quota replaces these; keep it per save slot
  through `SaveSlots.Key` like everything else.
- `Assets/_Project/Scripts/Runtime/Market/CollectorRules.cs` (40 lines): the Collector's shares. Goes.
- `Assets/_Project/Scripts/Runtime/Raid/LairLedgerBook.cs` (77 lines) and
  `Assets/_Project/Scripts/Runtime/Lair/LedgerPageText.cs` (40 lines): the ledger's pages. They show
  the demand instead of the debt.
- `Assets/_Project/Scripts/Runtime/Lair/LairEvents.cs`: `DebtChanged` and friends; replace with
  quota events on `EventManager`.
- `Assets/_Project/Scripts/Runtime/Raid/RaidDirector.cs`: the raid's flow and clock. The creature's
  arrival hangs off the portal's clock running low.
- `Assets/_Project/Scripts/Runtime/Raid/HaulLanding.cs`: how the haul lands in the Lair (being
  changed by #378 to keep each piece's place; take that change before starting).
- Loot categories exist since #313 (`docs/4-systems/market.md`); an artifact is loot marked as
  wanted.
- Inventory before removing anything: the debt and Collector touch the Lair screen's old ledger, the
  Market's sale flow and `coop_lair_check.sh`. List keep/change/drop with file:line before building,
  and open an issue for anything dropped that should come back.

### Done when
Tests for the quota (counts down per raid, paid by placing artifacts, unpaid makes raids hunts,
caught-with-artifacts pays, caught-empty kills, death keeps learned spells); the creature's model
with a review sheet and review record; a co-op check in which the quota is paid on both machines and
one in which the creature arrives and catches the client; screenshots looked at; full suites green;
`docs/4-systems/market.md` and `raid.md` updated, the Lair's `docs/3-state/ProjectState.md` M4 row
rewritten; a pull request into `staging-2026-10-11` with `Refs #386`.

## Before you stop, each time
Update `docs/5-today/Today.md` and `docs/3-state/ProjectState.md`, commit on your branch, push, and
say in the pull request what was and was not checked in Unity.
