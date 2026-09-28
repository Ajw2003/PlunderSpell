# Project State

**Headline: ~40% against the roadmap in `docs/2-roadmap/Roadmap.md`** (re-measured 2026-09-26).
The roadmap grew that day from the pitch's four milestones to eight, running to a game a stranger
can play (Decisions, "The roadmap runs to a game a stranger can play"). Against the old four this
headline read ~65%; nothing was lost, the definition of done got bigger. Each share below is a
judgement until that milestone's acceptance is checked, and only M0's has been.

| Milestone | Weight | Share done | Why |
|---|---:|---:|---|
| M0 Fork clean | 5 | 100% | Acceptance checked |
| M1 Prove the voice | 10 | ~70% | Works for one person on one machine; #50 never measured |
| M2 Vertical slice | 20 | ~70% | Loop plays solo and over UDP; #55 never run; its listed issues open |
| M3 Other Ages | 15 | ~60% | Bronze and Late have their own rooms; High Medieval and Powder borrow |
| M4 Lair and Market | 15 | ~5% | Debt is a number on the Lair screen; no market, no 3D Lair |
| M5 Household awake | 10 | ~30% | Guards patrol, investigate noise, chase and search; the raid player's footsteps are always a walk (`FootstepNoiseEmitter.cs:85`); crouch and run exist only on the playtest controller |
| M6 Castle fights back | 10 | ~5% | Doors and hazards are layout tags; revamp phases 3-5 not started |
| M7 Final art and perf | 15 | ~15% | Hit and spell feedback, the build tool, quality levels, a partial settings menu; no animation |
| **Total** | 100 | **≈ 41%** | |

Before 2026-09-26: every one of the four old milestones' code had been written, merged and
tested, the raid ran on real authored art, and two of the four acceptance criteria had never been
checked the way they are defined.

**2026-09-22 update — "code-complete" and "actually playable" turned out to be different claims.**
Phase 1 of `docs/plans/GitIssues/Priority_Queue.md` was closed out this session purely against code
and the headless test harness (`Tools/Headless/verify.sh`), which compiles and unit-tests the real
gameplay sources but cannot load a real `.unity` scene, render anything, or drive real input — see
`Tools/Headless/README.md`, "What it does and does not prove." The user then actually played the
standalone build produced for #53 and found the raid itself broken: casting doesn't work by voice
or keybind, there's no way to tell if the player or an enemy is taking damage, and extraction/return
to the Lair doesn't work. Root-caused and filed as a new top-priority Phase 0 — see
`docs/plans/GitIssues/Priority_Queue.md`'s Phase 0 section and issues #102 (epic), #100, #14
(reopened), #101. The headline "65%" and the milestone table below predate this finding and should
be read with it in mind: M2's code is not merely "acceptance unchecked," a real play session
surfaced it as not actually functional yet.

| Milestone | Status | Code | Acceptance checked? |
|---|---|---|---|
| M0 — Fork clean, cut gravity | Done | ✅ merged (`feature/m0-gravity-removal`) | ✅ — compiles, gravity restored, verified in the `.agent_reports`-era logs, now `docs/archive/2026-09-15-integration/` |
| M1 — Prove the voice | Code complete, acceptance unchecked | ✅ merged (`feature/m1-voice-casting`) | ❌ — no real-microphone, multi-accent, latency measurement exists anywhere in the repo |
| M2 — The vertical slice | Code complete, real art wired in, acceptance unchecked | ✅ merged; the raid scene now assembles from 25 castle rooms, 5 loot prefabs and 10 enemy prefabs instead of primitives (`docs/4-systems/raid-scene-assembly.md`), and the menu → lair → raid → lair flow is live (`fc22668`) | ❌ — 116/116 automated tests pass; no record of four real people playing a raid together, and the 2026-09-16 playtesting backlog (below) found 21 rough edges standing between the built loop and something you'd hand a friend |
| M3 — Open the other Ages | Era content wired in; Bronze Age and High Medieval raids differ | 🟡 The Lair's era now picks the raid's rooms, loot and garrison through `EraContentCatalogue` ([`raid-scene-assembly.md`, "Eras"](../4-systems/raid-scene-assembly.md)). Bronze Age has its own full room set, 5 items, 3 enemies. High Medieval: the original rooms, 5 items, 4 enemies. Late Medieval: its own InnerWard and Keep, 5 items, 1 enemy. Age of Powder: High Medieval rooms, 5 items, 2 enemies. Castle art: the Bronze Age and Late Medieval sets are fully modelled, 25 rooms and wall pieces plus 4 door plugs each ([`BronzeAge.md`](../art/rooms/BronzeAge.md), [`LateMedieval.md`](../art/rooms/LateMedieval.md)); Enemies: all 16 modelled and rostered. Unfinished art: all 26 Powder rooms ([`docs/plans/era-castle-rooms.md`](../plans/era-castle-rooms.md)) | 🟡 — verified 2026-09-24 in the live Editor through Lair → Set Out, one seed per era; not yet playtested by a person. Acceptance tightened 2026-09-26: every era on its own room set, so this is not met while High Medieval and Powder borrow rooms |
| M4 — The Lair and the Market | Not started | ❌ No market code (`grep -i market` finds nothing in `Assets/_Project/Scripts`); debt exists only as numbers (`LairState`, `LairScreen`) | ❌ |
| M5 — The household is awake | Partly built | 🟡 Guards patrol, investigate noise, chase and search (`GuardAlertState`); no hit reaction or crouch in the raid | ❌ |
| M6 — The castle fights back | Not started | ❌ Doors don't open and stairs can lead nowhere (#111); murder-holes and arrow-loops are layout tags (#44); castle revamp phases 3-5 not started | ❌ |
| M7 — Final art and performance pass | Partly built | 🟡 Build tool (#53), Low/Medium/High quality levels, a settings menu; no animation, no profiling | ❌ |

**2026-09-24 — art bible plunder and enemies modelled, and per-era raid content wired in.**
All 20 plunder items and all 16 enemies from the art bible (`docs/art/`) exist as validated,
textured models under `Assets/Models/ArtBible/`, built by `Tools/ArtForge/`. The enemies are rigged
with Unity-Humanoid bone names and skins blended across at most 4 bones. The era chosen in the Lair
now decides which rooms, loot and enemies a raid uses (`EraContentCatalogue`, filled by
`Tools/Plunderspell/Forge Era Content`). Wired in: the 20 items (carried by their per-item grip point, riding with the body; 44-53 placed per raid),
the Bronze Age and Late Medieval room sets, and all 16 enemies, 4 per era. Not yet: rooms of their
own for High Medieval (it uses the original set) and Age of Powder (no room art). No enemy is animated: no
Animator, no clips, no spring bones. `Assets/Models/ArtBible/AllEnemies/` holds a bare model prefab
per enemy as the art and scale reference; the prefabs raids spawn are
`Assets/_Project/Prefabs/Enemies/<Era>/`. Merged together on `claude/staging-2026-09-24` for testing
before `main`; see `docs/plans/merge-2026-09-24-art-branches.md`.

**2026-09-25 — backlog pass** (`claude/issue-backlog`, not merged). Spells cost mana
(`SpellWord.ManaCost`, a 100-point pool, 2.5/s regen) and number-key casts chant for 1.5 s
(`docs/4-systems/spells.md`, "Mana and the keyboard chant"). The pause menu no longer freezes the
world. Guards are halved in number, 0.8× speed and 0.65× damage (`GuardSpawner`). Each raid starts
calm with a 20 s grace, and guards report sightings, attacks and chases straight to the alarm
(`docs/4-systems/alarm.md`). The raid HUD is themed and its hue and cry look toned down. Open issues
re-triaged: see `docs/5-today/Today.md`. Carrying now works like R.E.P.O. (#144 phase 1): items hang
from where you grabbed them on a visible beam, and loot over about 10 kg drags
(`docs/4-systems/damage.md`, "Weight").

**2026-09-26 — backlog pass continued** (same branch, not merged). Casting no longer freezes the
game when the cast key is released: the microphone stays open for the raid (`docs/4-systems/voice.md`).
The closing portal falters gently instead of strobing (`docs/4-systems/atmosphere.md`, "Portal"). Only
an item's own motion hurts or breaks things: walking into a cauldron or bumping loot no longer
damages you or it (`docs/4-systems/damage.md`). Loot is rebalanced: nothing breaks from a waist-high
drop, heavy pieces pay more, and Late Medieval no longer fills its busy rooms with pieces too heavy to
lift ([`docs/plans/loot-balance.md`](../plans/loot-balance.md)). A player is never spawned over the
drawbridge's moat (`docs/4-systems/scale.md`, "Spawning"). Held loot keeps its orientation, weapons are held rigidly
in the hand, and pieces too heavy to lift are towed slowly behind you, slowing your walk; each loot item has one weight, its LootItem's Weight in kg, and all of it scales from that (`docs/4-systems/damage.md`,
"Weight"). The code's namespaces and assemblies are `Plunderspell.*`; the old `RogueAi` names are
gone (Decisions, 2026-09-26). Velox (a dash, the only dodge) and Saltus (a high jump that the jump
key turns into a slam) replace Tonitrus and Cadaver Surge on keys 5 and 7, checked in a raid in the
Editor; neither word has been tried with a real voice (`docs/4-systems/spells.md`, "Velox and
Saltus"). Settings has a microphone gain slider (#125; `docs/4-systems/voice.md`, "Microphone
gain"). The view shakes when you are hit, land a hit, hear a cast nearby (a shout more
than a whisper) or land a slam; there is no hit-stop, since the raid's time is shared (#51;
`docs/4-systems/damage.md`, "Camera shake"). A spell audit in a live raid (#106;
`docs/generated/spell-audit-2026-09-26/`): Somnus now finds a guard slightly off the crosshair, and
a guard dropped by Levo falls and takes about 16 damage instead of landing unhurt. Porta still has
nothing to open, because the castle places no doors (#111). Spell bursts render in a standalone build (they were magenta
error spheres), and now fade as intended (#127; `docs/4-systems/spells.md`, "What the visuals
actually look like"). The main menu and Lair no longer show "No cameras rendering" in the Editor
(#131; `docs/4-systems/raid-scene-assembly.md`, "Getting into a raid"). Every weapon and the five
original loot pieces have authored grip points, so a sword is held by its hilt (#134). The
curtain strip has entrances into the castle, the portal opens on it only in front of one, and a
team arriving there faces it (#140; `docs/4-systems/castle.md`, "Entrances from the strip").
A new raid no longer inherits fire, sleep or stun from the last (#143). Loot put down in the
portal stays put and cannot break (#158; `docs/4-systems/raid.md`, "Loot in the portal"). Guards grow
in number and health with the lobby (#154; defaults, not yet playtested with four people).
Guards now actually report to the alarm (every spawned guard had been disconnected from it), a
guard's shout sends nearby guards to you, and the hue and cry sends guards within 40 m (#163;
`docs/4-systems/alarm.md`). Stars and the moon show overhead through the fog, which still hangs over the horizon
(`docs/4-systems/atmosphere.md`, "The sky shows through overhead").

**2026-09-25 — the castle has its night look** (`claude/night-atmosphere`, not merged). Steps 1-4
of `docs/plans/night-atmosphere.md` built: see `docs/4-systems/atmosphere.md`. Open: volumetric fog
(High), vertex soot bake, Deck profiling, enemies/loot on the surface shader unseen in play,
PlayMode suite not re-run, `ArtAssetImportTests` fails (art-bible animations, enemy emissive HDR;
predates this branch).

**2026-09-25 — raids arrive and leave by portal; the castle is sealed** (branch
`claude/night-atmosphere`, night atmosphere step 0). The team arrives at a seeded spot in the outer
rings, the `ExtractionZone` is stood up there as the portal, players outside it when the clock ends
are left behind, `CastleBoundary` seals the gate and wall tops, and RaidScene has no ground beyond
the wall. Verified in the live Editor; see `docs/4-systems/raid.md`, "Arriving and leaving by portal".

**2026-09-25 — a throwaway preview of the "calm" night look exists in RaidScene.** A
`NightLookPreview` GameObject (`Plunderspell.Atmosphere.NightLookPreview`,
`Assets/_Project/Scripts/Runtime/Atmosphere/NightLookPreview.cs`) darkens `RaidScene`, retints the
DirectionalLight as a faint moon, adds a runtime URP Volume (ACES tonemapping, bloom, colour grade,
vignette), and drops primitive braziers, wall torches and stand-in props (cart+hay, crates, hay
bale) along every generated castle's curtain wall, matching the chosen look at
`docs/generated/look-samples-2026-09-24/calm.png`. It rebuilds itself whenever
`RaidDirector.Castle` changes. Turn it off by disabling the `NightLookPreview` GameObject in
`RaidScene`. **This is a stand-in only** — no alarm-state blending, no FireSource prefabs, no fire
anchors, no quality levels, no shader — and is meant to be replaced once
`docs/plans/night-atmosphere.md` steps 1-2 are actually built. Captures:
`docs/generated/night-look-preview-2026-09-25/`.

## What used to be not what it looked like

Until 2026-09-24, choosing an era in the Lair did nothing to the raid: every era built from one
room set, loot table and guard roster. That is fixed. `RaidDirector` now swaps in the era's
catalogue entry before it builds the castle (see `docs/4-systems/raid-scene-assembly.md`, "Eras").
No schema change was needed. Each era has its own registry, table and roster, so the planners
still branch only on `CastleZone`. What M3 still lacks is art, plus a person playing the eras side
by side.

**Update, 2026-09-24 (code only, not yet run in the Editor):** the era now reaches the garrison.
`EnemyRoster.Entry` carries an `Era`, `GuardSpawner.SpawnFor` passes `RaidDirector.Era` (now
replicated) to `EnemyRoster.PickForZone(zone, era, rng)`, and `RaidContext` is published for the
castle generator to read later. Rooms and loot still ignore the era, and until
`Tools/Plunderspell/Forge Art Bible Enemies + Roster` is run in the Editor the roster holds no
Bronze, Late or Powder enemies, so those raids fall back (with a warning) to the old High Medieval
guards outside the Crypt. See `docs/4-systems/raid-scene-assembly.md`, "Era reaches the raid".

## The 2026-09-16 playtesting backlog

**Update, 2026-09-22:** Phase 1 of `docs/plans/GitIssues/Priority_Queue.md` ("Core Game Loop,
Mechanics & Controls") is now closed out — verify with `gh issue list --state all --repo
Ajw2003/PlunderSpell`. Of the 16 Phase 1 issues, only #24 (the epic itself — see its own checklist,
which now tracks Phase 2 items) and #55 (the M2 four-player playtest below, which needs real humans
and can't be closed by a commit) remain open. That includes all nine items called out below as of
2026-09-16: #20, #5, #19, #6, #25, #21 (still open — no portal asset yet, tracked separately from
combat), #16/#23 (still open, Phase 4/5 art), #22 (still open, Phase 2 VFX/SFX). What's now closed
that wasn't: #5/#19 (room connectivity/grid-exact modules), #6/#25 (player scale/spawn), #20/#15
(loot physics/discoverability), #7/#8/#9 (crosshair/cursor/menu-input-gating), #14 (health/damage
model + HUD), #18 (a dedicated `CombatBench` scene), #37/#39 (melee and ranged player combat, new
this pass), #53 (a real standalone build tool, verified against a real Unity Editor install, new
this pass). The paragraphs below are the original 2026-09-16 filing and are left as historical
record of what the backlog looked like before this work landed — check `gh issue view <n>` for any
individual issue's current state rather than trusting the prose here.

Filed as GitHub issues #5–#25 (`Tools/mkissues.py`, manifest in
`docs/generated/github-issues.json`) immediately after the raid scene started assembling from real
art — so these are gaps the art exposed, not pre-art complaints. All 21 are open as of this pass
(`gh issue list --state all`); #24 is the epic tying the rest together. The ones most worth reading
before touching the raid loop:

- **#20 — loot is flung across the map by physics at spawn.** Already documented as an open,
  unfixed defect in `docs/4-systems/raid-scene-assembly.md` ("Traps") — roughly 2–7 of ~19 pieces
  per raid. An attempted floor-raycast fix made it worse (15/22) and was reverted; a real fix needs
  the spawner to find a clear resting spot while keeping the placement planner pure.
- **#5 / #19 — rooms don't connect / modules float and snap inconsistently.** Filed against the
  same generator `docs/4-systems/castle.md` describes; `CastlePathValidator` guarantees the *layout*
  is reachable, not that the *meshes* read as continuous interior space.
- **#21 — no portal asset**, **#16 — no main menu art**, **#23 — castles look bland**, **#22 — no
  VFX/SFX anywhere** — the game loop runs end to end but almost nothing in it has a finished visual
  or audio pass yet.
- **#6 / #25 — player scale and spawn placement** — the player can be too tall for the rooms, and
  can spawn inside or flush against castle geometry.

None of these are tracked against a specific milestone above; they're cross-cutting polish and
correctness gaps surfaced by actually looking at the built scene; see "Cross-cutting issues" below
for one further failure mode of the same kind.

## The moodboard gap-closure backlog

A second, separate audit pass (2026-09-16, same day as the docs-structure pass above) checked the
built game against `docs/plunderspell.md` and the mood board pillar by pillar, rather than against
the raid loop the playtesting backlog above was filed against. Full findings and reasoning:
[`docs/plans/moodboard-gap-closure.md`](../plans/moodboard-gap-closure.md). Headline: the voice and
physics-loot pillars are real and mostly match the pitch; **the Mystical Market pillar (the fourth
named pillar in the pitch) does not exist anywhere in the codebase**, the Lair is a menu screen
rather than the physical place the pitch describes, and half the built bestiary (SigilWisp,
VaultWarden, HexTurret, ArcRevenant, GildedColossus) reads as fantasy monsters with no basis in the
pitch's human "household" antagonists — flagged as an open creative-direction question, not a bug.
34 new issues are filed via `Tools/mkissues_moodboard_gap.py`
(manifest: `docs/generated/github-issues-moodboard-gap.json`), additive to and non-duplicative of
the 21-item backlog above.

## Cross-cutting issues that belong to no milestone

- **The `isSpawned`/`isServer` trap has already caused three separate silent failures** (voice
  casting, the extraction clock, trigger tracking — see `docs/4-systems/raid.md`,
  `docs/4-systems/voice.md` and `docs/4-systems/alarm.md`) because `if (!isServer) return;` is true on
  an unspawned object as well as a real client. All three known instances are fixed
  (`if (isSpawned && !isServer) return;`), but the underlying trap is a property of PurrNet's
  authority model, not something the codebase can permanently rule out — any new
  `NetworkBehaviour` is at risk of the same bug on its first offline/single-player run.
- **No real Unity Editor player build has ever been produced or checked.** There is still no
  `BuildPipeline.BuildPlayer()` entry point anywhere in `Assets/` (confirmed by grep as of this
  pass). Whether the project actually builds and runs as a standalone player is unknown. Now
  tracked as a GitHub issue in the moodboard gap-closure backlog, above.

**2026-09-23 update — Phase 0 worked in the live Editor, not headlessly.** See
`docs/plans/phase0-playable-loop.md` for the checklist and `docs/generated/playtest-2026-09-23/` for
the screenshots. Done and verified in Play mode: real voice recognition (Vosk binaries and model
committed, English heard-as grammar, the right microphone picked, a live level meter while
holding V, and a microphone choice in Settings); one damage pathway with readable feedback for
hitting, being hit and hurting yourself; death → "YOU DIED" → Lair; held objects with weight
and swing damage; standing on the pad to extract, then Lair → Set Out again. #100's diagnosis was
wrong: RaidScene uses `RaidPlayer.prefab`, which is fully wired (see `docs/6-decisions/Decisions.md`). **Not yet
done:** walking the loop in `RaidScene.unity` from the main menu; a fresh standalone build; a real
person speaking into the mic (the Whisper/Shout thresholds are uncalibrated); commenting on and
closing GitHub issues #14, #100, #101, #102.

**2026-09-23, later: first real voice cast.** The user played `CastleBench.unity` in the Editor and
cast spells by speaking into their own microphone, misfires included. This is the first time voice
casting has worked for a real person in this project (before today the engine was a stub and the
model was missing). M1 has moved from "never worked" to "works for one person on one machine".
Its acceptance criterion (multi-accent recognition, latency) is still unmeasured, and so is the
Whisper/Shout calibration against real voices. A standalone build has not been re-tested since the
fixes.

**2026-09-23, Steam co-op wired into the shipping raid (stages 1–3 of
`docs/plans/steam-coop-raid.md`).** Before this, the build could not do co-op at all: `RaidScene`
had no network manager and nothing started Steam. Now every session runs through PurrNet (solo is
a local host), Host Co-op creates a Steam lobby, the Lair invites online friends, and each player
gets their own body. Checked on this machine with two game windows over UDP, and Steam hosting in
the build. **A real Steam join between two accounts has not been tried;** that is the user's test
with a friend. The raid player now copies the CastleBench player; before that, casting in the
built raid did nothing (push-to-cast was on F19) and the camera sat too high. The user then confirmed voice casting by
microphone in the standalone build (2026-09-23). Stage 4 followed the
same day: guards and loot replicate, a client can carry loot and extract it, hits land where the
target's health lives, a client's spells aim where it looks, a dead player spectates a teammate and
the raid ends when everyone is down, and a friend's Lair shows the host's campaign. All checked with
two game windows over UDP; still untested between two Steam accounts. Then: weapons now spawn in
the castle as networked loot (found, carried, swung, sold), a crossbow shot shows on every machine,
downed bodies lie down, and guards give players at the gate a clear ring and a 20 second grace. See
`docs/4-systems/net.md`.

**2026-09-27, #169 step 1: two-player carry check built.** `Tools/Unity/coop_carry_check.sh` runs
host (Editor) and client (Development build) on one PC with nobody at the keyboard. Run twice
before any carry change, same results both times: one holder works on either side; a second
player's grab takes the piece (the first holder's pull is ignored); the piece falls when the
second holder lets go; and it despawns when a holding client quits. Results and screenshots:
`docs/generated/coop-carry-2026-09-27/`. Also found: `CarryFeelTests.Test_WalkingDoesNotJoltAHeldItem`
already failed before any #169 change (0.054 m jolt against a 0.05 m limit).

**2026-09-27, #169 step 5: held pieces turn by torque.** A held piece turns toward where its
holder faces through a torque capped at the holder's turn strength, so a goblet follows at once and
a long piece sweeps round; two holders turn it faster than one. Also fixed: two holders lifting a
heavy piece from knee height snapped their beams mid-lift (the target was more than the snap
distance away while the piece rose toward it); a hold now counts as strained only while not
closing on its target. Checked: `CarryFeelTests` 19/19 (four new), and `coop_carry_check.sh` 10/10
(`step5-run6.log`) with every scenario's frame sheet looked at, including two gold beams holding
the Rolled Tapestry up in `frames/heavy_lifted_together.png`. The earlier throw and
both-grab failures were the check's own: the old castle was cramped, and a pavise shield, taller
than a player, jammed against the wall (seen in the recording). The check now skips light pieces
over 1 m, uses a more open castle (seed 3508293) and clears the guards, which otherwise end the
raid within minutes. Still to do: removing the old two-person code (6), docs (8), the two-PC
Steam test (#170).

**2026-09-27, carry check records evidence.** `coop_carry_check.sh` now writes a per-physics-step
trace of the piece on both sides for every scenario, and for a failed one a screen recording and a
frame sheet. It uses one fixed castle and skips the build when nothing changed. The traces found
three faults in the check itself (staging into the portal, a random seed that spawned on the
extraction pad, and grabbing with a stale aim), all fixed. Latest run (`trace-run5.log`): 10/10 except
`client_throws`. The 3 kg PaviseShield hung still 0.45 m under its aim for 3 s, then the throw
moved it 0.06 m/s. It looks snagged out of the client's view, but that is not confirmed. Open, along
with three game bugs the logs showed, none part of #169: the client built the castle twice (BronzeAge,
then LateMedieval) as the seed arrived; a client logs "Invalid permissions when setting
`_extractionComplete`"; and the snap rule counts a heavy piece lagging behind its target as strain,
so lifting a heavy piece from the floor with two holders can snap (seen once, trace
`heavy_lifted_together` in `trace-run2.log`).

**2026-09-27, #169 step 4: holders' strength adds up, and opposite pulls snap.** Each holder's
grip and haul strength travel with their pull; the host adds them, so a piece too heavy for one
holder (they tow it) is lifted by two, and every holder sees the shared total. If two holders pull
a piece more than 1.5 m apart for 0.3 s, everyone lets go. A quick turn by one holder does not
snap it. Also fixed: PurrNet's `removeAuth` rule was unset, so every client refused the host's
ownership removal and logged "missing authority" errors. Checked: `CarryFeelTests` 16/16 and
`coop_carry_check.sh` 10/10, with no authority errors on either side. The check now screenshots only
failed scenarios and rebuilds only when needed; a run takes about 3 minutes. Still to do: mouse
steering (step 5), removing the old two-person code (6), docs (8), the two-PC Steam test (#170).

**2026-09-27, #169 steps 2-3: the host controls held pieces.** A piece on the beam has no owner
while held, so the host moves it and applies every holder's pull; clients send theirs with the beam
relay. A second grab joins the carry instead of taking it; a holder who lets go, quits or stops
sending is dropped within 0.5 s and the piece stays; a client's throw is applied by the host
(including in the extraction portal, which used to freeze a just-thrown piece), and a client's own
body does not collide with the piece they hold. Weapons in the hand still go to one player at a
time. Checked: `CarryFeelTests` 12/12 (the jolt test now passes too) and `coop_carry_check.sh` 7/7
on four runs in a row. Still to do: per-player strength and beam snapping (step 4), mouse steering
(5), removing the old two-person code (6), docs' plain copies (8), and the two-PC Steam test (#170).

**2026-09-23, castle revamp phases 1–2 done.** The castle is audited on its real NavMesh
(`CastleAudit.cs`). Every room and all floor are reachable on five seeds, and loot now spawns on
furniture (tables, chests, shelves, altars) inside rooms, all of it reachable. Phases 3–5 (themed
wings, a sunken crypt, real doors and hazards) are not started; see `docs/plans/castle-revamp.md`.
`ScaleInvariantTests` had measured castle modules on the wrong axis since they were written and
passed only by accident; it now measures height.
