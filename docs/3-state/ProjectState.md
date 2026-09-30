# Project State

**Headline: ~42% against the roadmap in `docs/2-roadmap/Roadmap.md`** (re-measured 2026-09-28;
~41% on 2026-09-26).
The roadmap grew that day from the pitch's four milestones to eight, running to a game a stranger
can play (Decisions, "The roadmap runs to a game a stranger can play"). Against the old four this
headline read ~65%; nothing was lost, the definition of done got bigger. Each share below is a
judgement until that milestone's acceptance is checked, and only M0's has been.

| Milestone | Weight | Share done | Why |
|---|---:|---:|---|
| M0 Fork clean | 5 | 100% | Acceptance checked |
| M1 Prove the voice | 10 | ~70% | Works for one person on one machine; #50 never measured |
| M2 Vertical slice | 20 | ~75% | Loop plays solo and over UDP; carrying together works (#169); #55 never run; Steam outside the Editor broken (#167, #168); #164, #170-#172 open |
| M3 Other Ages | 15 | ~60% | Bronze and Late have their own rooms; High Medieval and Powder borrow |
| M4 Lair and Market | 15 | ~5% | Debt is a number on the Lair screen; no market, no 3D Lair |
| M5 Household awake | 10 | ~35% | Guards patrol, investigate noise, chase and search, and a shout or the hue and cry calls guards in (#163); the raid player's footsteps are always a walk (`FootstepNoiseEmitter.cs:85`); crouch and run exist only on the playtest controller |
| M6 Castle fights back | 10 | ~20% | Stacked castle (crypt, ground, keep) with ramp stairs; 13 networked doors that lock at the alarm (#248, co-op checked 2026-10-04); hazards are layout tags |
| M7 Final art and perf | 15 | ~15% | Hit and spell feedback, the build tool, quality levels, post-processing, a partial settings menu; no animation; every sound file built but on a branch and not played by the game (below) |
| **Total** | 100 | **≈ 43%** | |

**Open issues, 2026-09-28:** 44, every one on a milestone (`docs/2-roadmap/Roadmap.md`). The same
day twelve that were already done or superseded were closed with a note on each: #127, #131,
#134, #140, #152, #154, #158, #163 (fixed on `main` 2026-09-26), #121 (post-processing built with
the night atmosphere), #41 (decided 2026-09-24), #126 (done but for #151 and #141) and #17 (a
duplicate of #151). The owner closed #169 on 2026-09-28 with two of its plan's steps not done:
the unused two-person code is still in `LootPickup` (`InitiateDualCarry`) and
`LootInteractor.cs`, and the plain copies of `net.md` and `damage.md` predate the new carry.

## Work not on `main` yet

Checked 2026-09-28 against every branch on `origin`. Two branches hold recent work that `main`
lacks; the rest are older and either merged in another form or abandoned.

- **`claude/playability-fixes`** (2026-09-30 to 2026-10-01, from the audio pass PR #178;
  parent #192). Contents:
  - guards keep moving (#193–#195);
  - saved settings apply at start-up (#181);
  - guards are slippery dynamic physics bodies that cannot crush a player through a wall (#200;
    co-op stuck time 1.7 of 1811 s);
  - the co-op scripts restore ProjectSettings byte-for-byte (#199).

  #211 is in (2026-10-02): the fresh guard has Stunned and Slept states (Levo holds a guard Stunned
  until it lands and pauses its mover; a loud noise wakes a sleeper early), walking guards now face
  where they go, and `EnemyDirector` is split into registry, bus, alarm and hue-and-cry classes
  (472 to 248 lines). Details and file:line in `docs/4-systems/alarm.md`.

  #212 is in (2026-10-02): the fresh guard has an OnFire state. A burning guard panic-runs to random
  reachable points, ignores leads and attacks, and on fire's end goes to Combat, Chase, Patrol or
  Investigate, or Dead if the fire killed it; stun and sleep outrank the fire. Burn damage stays in
  `StatusEffectReceiver.Tick`. Not in a prefab yet (#214). Details in `docs/4-systems/alarm.md`.

  #213 is in (2026-10-02): a dead fresh guard leaves navigation and the registries, topples (scripted
  tween, no rig yet), shrinks away in a dust puff on every peer, then the server despawns it. Not in a
  prefab yet (#214); not yet seen in live co-op. Details in `docs/4-systems/alarm.md`.

  The guard state-machine rebuild has started: #204 is in. A plain C# `StateMachine<TContext>`
  lives in Core/StateMachine, and the shared `BaseStateMachine` now runs `Exit`, which needed the
  player's Dodge, Jump and Attack exits fixed. `StateMachineTests` pass 6/6. The rest is #203's
  children #205–#214 (plan: `docs/plans/guard-fsm-restructure.md`). The handoff is
  `docs/plans/playability-pass-handoff-2026-09-30.md`.

  Bespoke navigation (plan: `docs/plans/bespoke-navigation.md`) has started. #220 is in: every
  room module carries a baked `CastleNavTile`, a 24×24 grid of 0.5 m cells with up to 2 floor
  layers and archway portals. The menu item Tools/Plunderspell/Bake Castle Nav Tiles bakes it.
  `CastleNavTileTests` pass 2/2, and the overlays are in `docs/generated/nav-tiles-2026-10-02/`.
  The Gatehouse and curtain wall pieces now bake with a virtual yard floor.

  #221 is in: `CastleNavGraph` (`Runtime/Castle/Navigation/`, 14 small classes) is stitched at
  generation (`ProceduralCastleGenerator.cs:115`).
  - Queries: path finding takes about 150 µs, the nearest-cell and reachable queries a few µs, all
    with 0 B allocated (Editor timings).
  - Reachability agrees with an Editor NavMesh on all 5 seeds wherever both call a spot floor. The
    graph calls more spots floor than the NavMesh does (Great Hall, Chapel), which is unexplained.
  - Locked doors aren't in the graph yet; that is #222.

  #222 is in: the director's guard navigation service (`Runtime/Alarm/Navigation/`, every file under
  180 lines). It takes move requests, smooths paths, sweeps a capsule before each step and keeps
  guards about 1 m apart. Door costs come from `CastleLockdown`.
  - Its tests pass 7/7. One of them is a guard stopping at a player pinned to a wall without
    moving them. Allocations are 0 B over 500 ticks with 20 guards.
  - It isn't wired into anything yet. `CastleLockdown.NavGraph` and the service's map are set by
    nobody, and the legacy guard still uses the NavMesh. The fresh guard core (#206) wires it, and
    the co-op run moves there too.

  #206 is in: the fresh guard core (`Runtime/Guards/Core`, `Senses`, `Movement`, `States`; 16 files, none over
  215 lines). It moves only through the director's navigation, which `RaidDirector` now gives the castle graph.
  - 17 new PlayMode tests pass (sight throttle, own-collider line of sight, hearing wake, a move reaching
  Arrived, health, state SyncVar, shove). The prefabs are **not** swapped: the states are placeholders until
  #207-#213, so a swap would make guards harmless. No co-op run for that reason.
  - `CastleLockdown.NavGraph` was left unset by #206, pending the owner's door decision; #207 has since
    wired it.

  #207 is in: `PatrolState` picks 3+ reachable points around the post, re-plans on Blocked and drops
  points behind barred doors. Lockdown doors are wired (`RaidDirector.cs:262`, per the 2026-10-02
  decision: locked doors cost, barred doors block). Tests pass: 7/7 PlayMode and 1/1 EditMode.
  #208 is in: `InvestigateState` consumes leads (noise, sighting, hue and cry) from `GuardLeads`.
  #209 is in: `ChaseState` follows a seen player at chase speed (throttled re-plans), ranged guards shoot
  on the move, sight lost goes to Investigate at the last seen spot.
  #210 is in: `CombatState` (Chase hands over at reach); the director's `AttackTurnMediator` gives 1 melee and
  1 ranged turn per player, others hold a ring place; one `GuardLineOfFire` check stops Chase and Combat shots
  through a teammate. Not in a prefab or a co-op run yet (the legacy guard stays until #214).

  2026-10-03 (#237): a melee guard that sees a player it cannot reach (nav map: no floor near, or feet over 0.9 m above it, for 1 s) holds below and throws
  stones (new `HoldBelowState`, `Resources/GuardStone.prefab`) and calls ranged guards to shoot, melee ones to look; PlayMode `GuardUnreachableTests` 4/4;
  the one co-op run did not work (the guard stayed on patrol and never saw the player; log in `docs/generated/guard-unreachable-2026-10-03/run.log`).
  2026-10-02 (evening, #238 part 2): hold C to creep (2 m/s, 1.5 m footstep); guards see head, body or feet, look up 80 degrees, see 1.5x
  farther and farther again as the alarm rises (#229); noise radii unchanged (`docs/4-systems/alarm.md`).
  2026-10-02 (evening): all 27 guard prefabs carry the fresh guard (#214 swap; `CastleGuard.cs`, `CastleNavMeshBaker.cs`,
  `GuardPrefabSwapTool.cs`, `GuardMovementTests` and the dead `GuardBrain` rules were deleted later that evening
  with the owner's approval). Guard navigation fixed after the owner
  saw guards stuck and standing still in co-op: Bronze Age and Late Medieval castles had an empty walk map
  (only the default registry was baked), the bailey dressing was not in the map, guards stopped dead on a
  clipped corner, and guards' bodies blocked each other. Co-op check, Late Medieval, 20 guards: stuck 4.8 s
  of 466 guard-seconds (1.0%), against 0 s moving before. Details: `docs/4-systems/alarm.md` (Guard
  navigation) and `docs/4-systems/castle.md` (Nav tiles, Nav graph). The #214 parity table is not done.
  #238: raid footsteps and landings now reach guards, sight looks up to 70 degrees, loot impacts and drags make noise by weight; heavy
  loot in game and a walk in the open are unverified, and the raid has no sneak (`docs/4-systems/alarm.md`).
  Then #223 finished: the runtime NavMesh is gone (no bake in `RaidDirector`, no `NavMeshSurface` in the three
  scenes, no obstacle on the fire props, `CastleNavMeshBaker` obsolete, since deleted, `CastleAudit` on the nav graph); the AI
  Navigation package stays for the deprecated monster. Co-op check after: stuck 13.8 s of 461.0 guard-seconds
  against 4.8 of 465.9 before, same seed (3508293); all 8 stuck samples are guards in Combat shuffling in the
  crowd at one archway, none against scenery. The four guard PlayMode tests that then failed are fixed: two
  expected one look to start a chase (the fresh guard investigates first), one relied on a teammate's body
  blocking an archer, and one found a real bug (a sleeper heard noises too quiet to wake it; fixed in
  `GuardHearing`). Full PlayMode run after: 341 of 342, the one failure the known flaky #233.
  #239: the hue and cry repeats every 3 s (`EnemyDirector._hueAndCryRepeatSeconds`) at the players' current spots
  while the alarm stays at Hue and Cry, and every guard answers it at any distance (other requests keep 40 m);
  full PlayMode 345 of 345. Co-op: 19 of 20 guards investigating at once after the alarm was raised, but the
  players died within about 20 s, so the repeat itself was not seen in play (tests cover it).
  #236: fire overrules Somnus (`StatusEffectReceiver.Ignite` wakes a sleeper, `Sleep` on a burning target does nothing, a woken guard goes to OnFire); reverses the #212 sleep-over-fire rule, stun still outranks fire. Co-op: slept guard ignited went OnFire at once.
  Guards now climb stairs: the sweep's step-over is 0.7 m (two risers); at 0.35 m no staircase in any Age
  was climbable (alarm.md, Guard navigation, "Stairs"). Seen in co-op on the Late turret stair, up and down.
  The #214 parity table is checked (`docs/plans/guard-core-inventory.md`): 36 of 39 rows match. Open, the
  owner's call: the alarm no longer speeds guards up (`GuardBrain.MoveSpeed` is called by no state).
- **`claude/voice-mimicry-improvements-7a0b29`** (2026-09-30; merged here 2026-10-06 as voices only, #280, the mimic prototype stays on its branch; issues
  #179 to #187): replaces word recognition with recorded clips. Built and tested: saved volumes checked
  against the mixer after start-up (the stale-volume fault itself was not reproduced; see
  `docs/4-systems/audio.md`), a pause segmenter, a saved clip bank and a guard voice disguise (24 EditMode
  tests), a recording script and browser recorder page, and 116 stand-in guard lines made with Windows'
  voices plus synthesised snores (`Tools/GuardVoice/takes-tts/`, checked for length, level and clipping;
  nobody has listened to them yet, `listen.html` there is for that). Guard speech swap built (#184):
  human guards now play those clips, re-voiced per guard, through the mixer; the old guard voices, guard
  sounds, guard steps and the mimic are switched off in `SoundFocusSettings.asset`. Checked solo in the
  Editor only (lines logged, rendered clip playing); not checked in co-op or by ear. Not yet wired: the
  mic-to-bank capture, the always-on mic setting.
- **`claude/carry-cleanup-169`** (1 commit, 2026-09-28, branched from today's `main`). The
  two-player carry check now moves to the most open floor within 25 m before staging, which fixed
  `client_grabs` failing now and then (the aim point landed behind a wall), and its traces record
  the combined grip. Found and not fixed, a real game bug: two holders sometimes cannot lift the
  Rolled Tapestry (`heavy_lifted_together` failed on 2 of 4 runs, always that piece; the Parade
  Armour lifts fine in the same spot). Cause not confirmed.
- **`claude/eloquent-dirac-i10hep`** (7 commits, 2026-09-27, branched before the #169 work),
  **now merged into `ccr-6bf1f02d-o8jhoy`** (`84732e04`), where the in-game layer was then built
  (2026-09-29; `docs/4-systems/audio.md`). Still not on `main`. The audio plan (`docs/plans/audio.md`: about 480 named sounds, about 1,000 files) and
  `Tools/AudioForge/`, which builds all 1,013 files into `Assets/_Project/Audio/`: 262 from a CC0
  library, 45 generated, 706 placeholders (435 of them guard voices awaiting friends' recordings).
  A measured pass re-levelled every file by category after the owner found sounds harsh; files
  outside their level window went from 452 to 3. Nobody has listened to the result, and no game
  code played any of it when the branch was written; the layer that does is described below.
  Two decisions are recorded on the branch (AI sound effects, friends' voices, AI music only where it
  does not adapt; levels baked per category with Unity's Normalize off).
- **`ccr-6bf1f02d-o8jhoy`** also holds, since 2026-09-29: the raid always has one audio listener,
  an output-device picker in Settings, three save slots on the main menu, and a reduced playtest
  sound set (spells, music, ambience muted; `docs/4-systems/audio.md`, `docs/4-systems/core.md`).
- **`claude/busy-bose-a7647f`** (1 commit, 2026-09-26): a test-only fix to
  `GuardAttackTests` so the guard's own `Update` does not swing during the test's yield frame.
  The test it touches is the "known failure" named throughout the 2026-09-26 test runs.
- Older, not worth merging as they stand: open PRs #2 (`integration/staging-2026-09-15`), #4
  (`claude/repo-status-check-hjp6z7`, the five-tier docs scaffold, since replaced by the six
  tiers) and #97 (`claude/amazing-ritchie-ga4w1t`, HUD health for #14, which is closed), plus
  `claude/damp-cave-svg-treasure-r0l6rz` (a portal illustration and bestiary concept sheets, as SVG), `claude/trial-merge-2026-09-24`
  (verification only) and `idk`.

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
| M6 — The castle fights back | Partly built | 🟡 Castles stack three floors joined by placeholder ramp stairs (#247, #255); guards spawn and patrol on every floor. 13 doors per castle where zones meet open by hand, lock at the alarm and open to Porta, the same for host and client (#248; `Tools/Unity/coop_door_check.sh`, 2026-10-04). Period stairs (#256), roofs and per-floor tuning (#250) not done; murder-holes and arrow-loops are layout tags (#44); castle revamp phases 3-5 not started | ❌ |
| M7 — Final art and performance pass | Partly built | 🟡 Build tool (#53), Low/Medium/High quality levels, a settings menu; no animation; first profiling pass 2026-10-03 (#242, `docs/generated/perf-2026-10-03/README.md`): sound streaming, guard route smoothing, HUD and fire-glow costs cut | ❌ |

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

**2026-09-30 — playability pass, guards** (`claude/playability-fixes`, #193-#195, not merged).
Guards no longer stand still: a stuck watchdog re-paths and skips unreachable points, destinations
snap to the NavMesh, searchers sweep around the last-known spot, noise steers a hunt, and the hue
and cry re-sends searching guards near the nearest player every ~3.5 s. Stuck time in a 20-guard
co-op raid fell from 25.1% to 4.0% of guard-seconds (`docs/4-systems/raid.md`, "Guards that keep
moving"). `GuardAttackTests.Test_EveryAttackBumpsTheReplicatedSignal` fails before and after this
change (the guard's own `Update` swings in the yield frame).

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
a guard dropped by Levo falls and takes about 16 damage instead of landing unhurt. Porta had
nothing to open until doors were placed on 2026-10-04 (#248). Spell bursts render in a standalone build (they were magenta
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

**2026-09-29 — the UI redesign is written, not yet seen** (`claude/ui-redesign-impl`, not merged).
`docs/plans/ui-redesign.md` is built in code: `UITheme` is the pigment list and roles, `UIFonts`
loads Eczar, Spectral and Overpass Mono, `UITextures` generates the backdrop, sigil and ring,
`UIFactory` has the new buttons, bars, slider, stepper and segmented control, every menu screen and
the vitals HUD are re-laid out, and the IMGUI raid HUD, crosshair and damage numbers use the same
roles. Nothing has been opened in Unity: the headless harness compiles the code, and three new
EditMode test files (`UIFontsTests`, `UIThemeTests`, `RaidHudPromptTests`) have not run in the
Editor. Not built: the extracted-piece list on the victory screen (no data), the Lair's "n of 4 in
the Lair" count (the co-op session does not expose one), and screenshot comparison against the
mockup.

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
  *2026-09-27:* the audio half of #22 now has its files. `Tools/AudioForge/` builds all 1,013
  planned sounds into `Assets/_Project/Audio/` (262 CC0 library, 45 generated, 706 placeholders
  including 435 guard voices awaiting recording). No game code plays any of them yet: the mixer
  and `AudioDirector`/`MusicDirector` (`docs/plans/audio.md` §7.3) were not built.
  *2026-09-29:* they are, in part. `Plunderspell.mixer`, `SoundBank.asset` (483 entries),
  `AudioDirector`, `MusicDirector`, the Settings sliders on the mixer and the casting dip exist and
  play from game events; see `docs/4-systems/audio.md` for the event table and the gap list (the
  wizard's own swing and throw, footsteps, physics impacts, ambience, guard voices and more are
  still silent). Play-mode reads show the right clips on the right groups; nobody has listened.
  *2026-09-29 (later):* footsteps by surface, jump and landing, physics impacts by material and
  speed with scrape and roll loops, and guard voices from replicated state (alert, chase, search,
  lost, attack, hurt, asleep, murmur, death; the hound's growl, bark, bite, yelp and howl) were added
  (`docs/plans/audio-feel-layer.md`). One additive event on `Item.cs` (`Item.Impacted`); it needs a look
  when branch `stashing` merges.
  The same day, after the owner reported harsh and misplaced sounds, a measured pass
  (`docs/generated/audio-audit/`) re-levelled every file by category, softened the synth recipes and
  swapped off-theme sources; flagged files went from 465 to 3. Nobody has listened to the result yet.
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
pitch's human "household" antagonists — flagged as an open creative-direction question, not a bug
(decided 2026-09-24: each Age's enemies are its household; #41 closed 2026-09-28).
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

**2026-09-27, two client-side raid bugs fixed.** A client built the castle twice as a raid
started (first in the last raid's era, then again): the seed and era were separate SyncVars and
arrived apart. They now travel as one packed value (`RaidDirector._layout`). And a client logged
"Invalid permissions when setting `_extractionComplete`" whenever a raid ended: the extraction
result RPC runs on every peer and wrote the server's SyncVar; now only the server (or an offline
game) writes it. Checked: `RaidLoopTests` 19/19, `PlayableLoopTests` 13/13, and a two-player run of
`coop_carry_check.sh --raid-end` (`raid-end-run1.log`): the client built once, in LateMedieval, and
followed the raid to Resolved with no SyncVar error. Every carry-check run now also checks the castle
was built once (`castle_built_once`, passed twice). Open: the carry check's `client_grabs` still
fails now and then by a few centimetres (0.38 m against 0.35) because its aim point sits behind a
wall in the fixed castle (seen in `frames/client_grabs.png`); a check problem, not a game one.

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
