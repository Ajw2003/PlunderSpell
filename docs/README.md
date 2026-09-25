# Plunderspell — docs

Plunderspell is a four-player co-op heist: raid a procedurally generated castle, cast spells by
speaking Latin-ish words into a microphone, carry out what you can (it has real weight), and get
out before the alarm you triggered catches up with you. Built in Unity 6000.3.15f1 on PurrNet
(networking) and Vosk (offline speech), forked from an earlier third-person extraction-game
prototype (`RogueLikeSlop@ThirdPerson`) that this repo's own name and initial commits still
reflect.

Nobody should have to search this folder — if something isn't linked from here, that's a gap in
this document, not a reason to go grep the repo.

## The moving parts

| Path | |
|---|---|
| `Assets/_Project/Scripts/Runtime/` | all gameplay, one assembly per system |
| `Assets/_Project/Scripts/Tests/` | the test suite (runs in Unity and headlessly) |
| `Assets/_Project/Scripts/Editor/` | scene builders and dev tooling |
| `Tools/Headless/` | headless build + test harness — compiles/tests without a Unity install |
| `Tools/AssetPipeline/` | Blender-driven generation of weapon/loot/castle-module props |
| `Tools/EnemyForge/` | Blender-driven generation of the enemy roster (mesh, rig, textures) |
| `Tools/ArtForge/` | Blender-driven generation of the art bible's plunder, structures and enemies, built on EnemyForge; review sheets in `docs/art/models/` ([README](../Tools/ArtForge/README.md)) |
| `Tools/mkissues.py` | files the playtesting backlog to GitHub issues; see `docs/generated/github-issues.json` |
| `Plans/` | one exhaustive plan per GitHub issue, plus `Priority_Queue.md`, the live execution order |
| `docs/` | this tree |

## The tiers

| Tier | File | Answers |
|---|---|---|
| 1 | `docs/README.md` | What is this, where is everything (this document) |
| 2 | [`docs/Roadmap.md`](Roadmap.md) | What 0–100% means, per milestone |
| 3 | [`docs/ProjectState.md`](ProjectState.md) | Where it actually stands right now |
| 4 | [`docs/systems/`](systems/README.md) | How each runtime-critical system works |
| 5 | [`docs/Today.md`](Today.md) | What's being worked on today, and why |
| 6 | [`docs/Decisions.md`](Decisions.md) | Why a decision was made, and what it replaced |

Plus `docs/plans/` (a plan for a specific piece of work, live until executed), `docs/archive/`
(documents that were correct once and are now inert — moved, never deleted), and
`docs/generated/` (tool-produced deliverables — not hand-edited, regenerated if they need to
change).

## Systems (tier 4)

See [`docs/systems/README.md`](systems/README.md) for the full index, including what was folded
together and what was deliberately left undocumented, and why.

| System | Owns |
|---|---|
| [`core`](systems/core.md) | Singleton base, event bus, state machine contract |
| [`net`](systems/net.md) | Sessions (solo, host, join), Steam lobbies and invites, one body per connection |
| [`voice`](systems/voice.md) | Held-key + microphone audio → a recognised phrase |
| [`spells`](systems/spells.md) | Resolving a phrase into a spell, a misfire, or a fizzle |
| [`castle`](systems/castle.md) | Deterministic seed-driven castle layout and path validation |
| [`alarm`](systems/alarm.md) | Noise propagation and the castle-wide alarm state machine |
| [`raid`](systems/raid.md) | The loop: Lair → castle → haul → extraction → Lair |
| [`raid-scene-assembly`](systems/raid-scene-assembly.md) | Wiring the authored art (castle rooms, loot, enemies) into the playable scene |
| [`enemy-asset-pipeline`](systems/enemy-asset-pipeline.md) | Generating the enemy roster from Python/Blender |
| [`damage`](systems/damage.md) | One damage pathway, blame, and hit feedback you can read |
| [`combat-bench`](systems/combat-bench.md) | The one-room arena for trying a weapon, spell or enemy without starting a raid |
| [`scale`](systems/scale.md) | The 1.8m standard human, and the room, archway and enemy heights measured against it |

## Everything else worth reaching

- [`docs/plunderspell.md`](plunderspell.md) — the pitch/design bible (v0.2). Written to be read
  aloud; §9 onward is plain fact. Illustrated mood board:
  [`docs/generated/plunderspell-moodboard.html`](generated/plunderspell-moodboard.html).
- [`docs/plans/plunderspell.md`](plans/plunderspell.md) — the engineering plan the roadmap was
  built from, including the provenance note around the excluded `feature/Owen/PCG` branch.
- [`docs/plans/steam-coop-framework.md`](plans/steam-coop-framework.md) — the plan for porting the
  predecessor project's Steam co-op framework, which Plunderspell was forked from.
- [`docs/art/`](art/BRIEF.md) — the art bible: per-Age handoff sheets a 3D artist builds from
  (3 structures, 4 enemies, 5 plunder items per Age; [`bronze`](art/bronze.md) ·
  [`high`](art/high.md) · [`late`](art/late.md) · [`powder`](art/powder.md)), concept sheets in
  `docs/art/concept/`, specs in `docs/art/data/*.json`, and its own illustrated mood board
  [`docs/generated/plunderspell-art-bible-moodboard.html`](generated/plunderspell-art-bible-moodboard.html).
  Regenerate with `python3 Tools/ArtBible/build_art_bible.py` (see `Tools/ArtBible/README.md`).
  The step-by-step loop from brief to validated model (plan → sheet → audit → fix → model beside
  sheet → audit → fix → commit) is [`docs/art/WORKFLOW.md`](art/WORKFLOW.md); long runs are protected
  by `Tools/autosave.sh`.
- [`docs/plans/artbible-enemies-in-engine.md`](plans/artbible-enemies-in-engine.md) and
  [`docs/plans/artbible-enemy-animations.md`](plans/artbible-enemy-animations.md): **awaiting
  approval.** How the 16 ArtForge enemies get into a raid, and how they get animated. Review page:
  [`docs/generated/enemy-animation-plan/`](generated/enemy-animation-plan/index.html).
- [`docs/plans/night-atmosphere.md`](plans/night-atmosphere.md) — the anchored aesthetic: night,
  warm fire in fog, a castle that brightens and reddens with each alarm state; plus the outer bailey,
  the surface shader, post-processing and the Low/Medium/High quality levels. Look samples in
  `docs/generated/look-samples-2026-09-24/`.
- [`docs/plans/moodboard-gap-closure.md`](plans/moodboard-gap-closure.md) — a full audit of the
  built game against the pitch bible and mood board, pillar by pillar, plus the 34-item backlog it
  produced (`Tools/mkissues_moodboard_gap.py`,
  [`docs/generated/github-issues-moodboard-gap.json`](generated/github-issues-moodboard-gap.json)
  once filed). Flags one open creative-direction question (the bestiary's thematic split) that
  needs a decision, not just more art.
- [`docs/prompts/plunderspell-fable.md`](prompts/plunderspell-fable.md) — the build-and-test
  prompt used to drive an agent session on this project.
- [`docs/prompts/house-rules-versioncheck-fix.md`](prompts/house-rules-versioncheck-fix.md) — a
  handoff for an agent in `Ajw2003/AjsClaudeCodeTools`: the house-rules version check went silent
  when it couldn't reach GitHub (found 2026-09-24). Not PlunderSpell work.
- [`docs/generated/`](generated/README.md) — the castle-generator and UI HTML previews, and the
  live GitHub issues backlog (`gh issue list` is the source of truth for current status; the JSON
  is a point-in-time manifest of what's been filed).
- `Tools/Headless/README.md` — what the headless verification harness does and does not prove.
- `Tools/AssetPipeline/README.md` — the reproducible Blender prop pipeline.
- [`docs/archive/`](archive/README.md) — inert documents kept for the record, starting with the
  full paper trail from the 2026-09-15 branch integration.

## Conventions

- **Cite claims to `file:line`** where practical — it's what makes a doc's claims checkable
  instead of a matter of opinion.
- **A milestone is done when its acceptance criterion has been checked, not when the code
  exists.** See `docs/ProjectState.md` for the current gap between the two.
- **A doc that's gone inert moves to `docs/archive/`. It does not get deleted.** Fix the pointers
  into it rather than leaving them broken.
- **Update the tier that changed.** Tier 3 (`ProjectState.md`) moves often; tier 2
  (`Roadmap.md`) only when the definition of "done" itself changes; tier 6 (`Decisions.md`) is
  never rewritten, only appended to.
