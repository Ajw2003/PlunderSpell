# Handoff: the two-form guard, then the creature (2026-10-11)

For the next agent. Two pieces of the approved fear pass, done **one after the other**: first the
two-form guard, then the creature. Each starts with a short look-and-behaviour design the owner
approves, then the build. The spell lockdown (part 1 of the slice) is separate work and not in this
handoff.

## Where this stands (updated 2026-10-12)

- **Part A's design is approved, and its art is built.** The true form is **the Wick**
  (`docs/7-design/fear-pass.md`, "The true form"). Its model and its nine clips are built and
  reviewed (#406, under #386); what remains for Part A is the code and the Unity side, below.
- **Part B's look is approved.** The creature is **the Ophan of Hours**, allowed lapis
  (`fear-pass.md`, "The creature"; Decisions, 2026-10-11, "The Ophan may use lapis"). Its model is
  built and reviewed (#411); its clips are the next art step. Its behaviour questions (2-4 under Part B) are still open.
- One question is still open for Part A: how many more ground-floor guards (4 below).
- All of this is on branch `AjsAgent/fear-pass-handoff`, not yet in a pull request.

### Pick up here (paused 2026-10-12, usage ran out for the week)

Last commit before the pause: `3f162461` (the Ophan's model). Nothing is uncommitted. In order:

1. **The Ophan's clips** (the owner has not yet said go; ask first): `hover_turn`, `advance`,
   `fix_gaze`, `take`, `arrive`. They are already rows in `Tools/ArtForge/anim_spec.json` with
   status `planned`, sources `ophan_*`, target fbx `TheOphanOfHours_Generic.fbx`. The rig is generic
   (bones in `ophan()`, `art_forge/blueprints/enemies_fear.py`), so this is not a Humanoid retarget
   like `anim_wick.py`; the hound's generic clips are the nearer model. Review every clip's sheet,
   then set the status to `fear`.
2. **Unity, which needs a machine with the editor** (none in the cloud container): give the Fear
   folder its own import rule (Ophan generic, Wick humanoid, `Humanoid_Fear.fbx` as clips, `_FireMask`
   linear). Details under "The art you build on" and Part B, question 1.
3. **Owner questions still open:** Part A's guard count (4 below); Part B questions 2-4.
4. When the branch is ready: a pull request into `staging-2026-10-11` (never `main`), body
   `Refs #386`, `#406`, `#411`.

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

- **Not done, all Unity, none of it run yet.** In `Assets/_Project/Scripts/Editor/ArtBibleModelImporter.cs`:
  the Wick's model already falls under the enemy rule (`EnemyRoot`, so `Kind.HumanoidEnemy`), but
  `Humanoid_Fear.fbx` matches no rule (only `Humanoid_Player.fbx` is special-cased), so it needs
  one like the player clips' (Humanoid, loop flags and events from `anim_spec.json`); and
  `_FireMask` is not in `LinearTextureSuffixes`, so it would import as colour (add it beside
  `_DyeMask` when the fire shader comes). Then the Wick's prefab and Animator, the model swap at
  0.9 s, the lantern detach on the warden, and the fire shader.

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
   rings, the stone pulled back to quarry. **Unity, untested here:** `ArtBibleModelImporter` sends any
   model under `EnemyRoot` that is not in `ArtBibleEnemyCatalog.GenericRigModels` down the Humanoid
   path, which a wheel cannot satisfy. Don't just add the Ophan to that list: a test
   (`Test_TheGenericRigListMatchesTheArtBible`) holds it equal to the four Ages' generic enemies.
   Give the Fear folder its own rule instead. Its clips come next. Its counter: none (carry what it
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
