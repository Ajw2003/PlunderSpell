# Project State

**Headline: ~65% against the roadmap in `docs/Roadmap.md`.** Every milestone's code has been
written, merged, and passes an automated test suite; the raid now runs on real authored art
instead of primitives. Two of the four milestones' acceptance criteria have never actually been
checked the way they're defined, and a 21-item playtesting backlog (filed 2026-09-16, all still
open — see below) is the clearest evidence of the gap between "compiles and passes tests" and
"plays like the pitch."

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
| M2 — The vertical slice | Code complete, real art wired in, acceptance unchecked | ✅ merged; the raid scene now assembles from 25 castle rooms, 5 loot prefabs and 10 enemy prefabs instead of primitives (`docs/systems/raid-scene-assembly.md`), and the menu → lair → raid → lair flow is live (`fc22668`) | ❌ — 116/116 automated tests pass; no record of four real people playing a raid together, and the 2026-09-16 playtesting backlog (below) found 21 rough edges standing between the built loop and something you'd hand a friend |
| M3 — Open the other Ages | Scaffold only; Bronze Age castle art built | 🟡 `HistoricalEra` enum + plumbing only. Castle art: the Bronze Age set is built — 25 rooms and wall pieces plus 4 door plugs, each modelled to a reference sheet ([`docs/art/rooms/BronzeAge.md`](art/rooms/BronzeAge.md)); Late Medieval and Age of Powder are in progress ([`docs/plans/era-castle-rooms.md`](plans/era-castle-rooms.md)). None of it is wired into the generator yet | ❌ — see below, the data model can't produce era-specific content yet |

**2026-09-24 — art bible plunder modelled.** All 20 plunder items from the art bible
(`docs/art/`) now exist as validated, textured models under `Assets/Models/ArtBible/Items/`,
built by `Tools/ArtForge/`. They are not wired into any loot table or prefab yet, and have
no Unity `.meta` files (Unity creates them on first import). The art bible's 12 structures
are specified and drawn but not modelled here (another agent owns them).

**2026-09-24, later — art bible enemies modelled.** All 16 enemies are rigged, textured models under
`Assets/Models/ArtBible/Enemies/`, built by ArtForge. They have Unity-Humanoid bone names, skins
blended across at most 4 bones, and posed review sheets in `docs/art/models/`. `python3
Tools/ArtForge/build.py enemies` gives `16 built, 0 crashed` / `All models passed validation.` on two
consecutive runs. They are not wired into the roster or any prefab. They have no animation clips,
spring bones or `.meta` files.

## The one thing that is not what it looks like

**Choosing an era in the Lair does nothing to the raid you get.** `RaidDirector.StartRaid(era)`
takes a `HistoricalEra`, stores it, and forwards it to `LairHubManager.SelectEra`. It reads as a
finished feature — the Lair has era selection UI-adjacent state, `RaidDirector` has an `Era`
property, everything compiles and the tests pass. But `CastleRoomRegistry` (see
`docs/systems/castle.md`) tags every room module only by `CastleZone`, with no era field at all,
and neither the loot planner nor the guard planner branch on era anywhere (`grep -rn
"HistoricalEra" Assets/_Project/Scripts/Runtime/Castle Assets/_Project/Scripts/Runtime/Loot
Assets/_Project/Scripts/Runtime/Guards` returns nothing — still true as of this pass). Every raid,
in every era, currently builds from the same single room set, loot table and guard roster. M3's
acceptance criterion — "a different era produces a measurably different raid" — is not close to
met; it needs a schema change (`CastleRoomModuleData.Era`, era-keyed loot/guard tables) before
it's even possible, not just more content.

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
  unfixed defect in `docs/systems/raid-scene-assembly.md` ("Traps") — roughly 2–7 of ~19 pieces
  per raid. An attempted floor-raycast fix made it worse (15/22) and was reverted; a real fix needs
  the spawner to find a clear resting spot while keeping the placement planner pure.
- **#5 / #19 — rooms don't connect / modules float and snap inconsistently.** Filed against the
  same generator `docs/systems/castle.md` describes; `CastlePathValidator` guarantees the *layout*
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
[`docs/plans/moodboard-gap-closure.md`](plans/moodboard-gap-closure.md). Headline: the voice and
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
  casting, the extraction clock, trigger tracking — see `docs/systems/raid.md`,
  `docs/systems/voice.md` and `docs/systems/alarm.md`) because `if (!isServer) return;` is true on
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
wrong: RaidScene uses `RaidPlayer.prefab`, which is fully wired (see `docs/Decisions.md`). **Not yet
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
`docs/systems/net.md`.

**2026-09-23, castle revamp phases 1–2 done.** The castle is audited on its real NavMesh
(`CastleAudit.cs`). Every room and all floor are reachable on five seeds, and loot now spawns on
furniture (tables, chests, shelves, altars) inside rooms, all of it reachable. Phases 3–5 (themed
wings, a sunken crypt, real doors and hazards) are not started; see `docs/plans/castle-revamp.md`.
`ScaleInvariantTests` had measured castle modules on the wrong axis since they were written and
passed only by accident; it now measures height.
