# Handoff 2026-10-07: the diegetic raid UI, from #318

**For:** a fresh agent on aj's PC (aj may be away; work in order of gameplay impact, no need to ask). **Branch:**
`claude/rimlight-318` has one WIP commit (`1391e480`) on top of staging; `claude/staging-2026-10-07` is clean and pushed
(at `46359ec2`). Read first: `CLAUDE.md`, `docs/1-landing/README.md`, this file, then `docs/plans/diegetic-ui-lair-market.md`.

## Where things stand

- **The event bus move is finished** (#297, steps #298-#305): one bus, `EventManager`; rules in `docs/4-systems/core.md`,
  "The event rule". Nothing else is called a bus (`EnemyDirectorListener`, `LoopPool`).
- **Diegetic raid UI** is parent #315, steps #316-#327. Done and merged into staging: the watch (#324, hold `T`), the grimoire
  (#325, hold `Tab`), and stripping the old HUD (#326). Fires (#316) and light sources (#317) were mostly built already; the
  colour logic is `CastleAtmosphere`/`FireRules`; three modules that had no fire at Calm got one, with a guard test.
  Still open: #316's crackle and chapel-bell sounds (optional), #317 (more torches where it reads dark: ask aj where).
- **On the raid screen now:** health/mana, the crosshair, damage feedback, and until their world cues exist the interaction
  prompt, the ranged weapon line, the chant bar and the heard-phrase caption.
- **In progress, #318 (rim-light):** `LootHighlight` is verdigris, `LootInteractor` lights the focused door handle and skips
  broken pieces (commit `1391e480`, compiles, untested). **Left to do in #318:** in `RaidHudPresenter.BuildInteractPrompt`
  return `""` for everything except the "needs two" lines (they stay until grip marks, #319); keep `HudAndInteractionTests`
  line ~276 passing; add a test that a focused door gets a `LootHighlight` and a broken piece does not; look at it in a raid.

## Next, in order of gameplay impact (aj's instruction)

1. **#318** finish (above). 2. **#320** the haul is the pile on the portal pad, extraction closes the portal around you
(ring of light on the floor, chime). 3. **#323** casts as glowing letters (`PhraseResolved`, `CastResolved`; teammates see
them, #48). 4. **#322** off-hand light and runes (`MicLevelChanged`, `ChantProgressChanged`, `CastingStateChanged`).
5. **#319** two grip marks on heavy pieces. 6. **#321** the weapon shows its own state (`RangedWeaponStatusChanged`).
7. **#327** a co-op playtest with someone new: needs a human. Each step removes the HUD element it replaces from
`RaidHudView` once its cue exists. One branch per issue off staging (`claude/<topic>-<issue>`), `Refs #N`, never `Closes`.

## How to work here (learned the hard way)

- **Unity:** aj's own Editor is on `PlunderSpell-lair`, a different folder. Tools in `Tools/Unity/` need an Editor on THIS
  folder: open one visibly (`Unity.exe -projectPath <this folder>`), then open `Assets/_Project/Scenes/RaidScene.unity`
  in it (`EditorSceneManager.OpenScene` through `Tools/Unity/eval.sh`), or `host_udp` fails with "Index was outside the
  bounds". A second Editor opened for this work may still be running: close it with `EditorApplication.Exit(0)` through
  `eval.sh` when done, or ask. Batch mode (`Unity.exe -batchmode -nographics -runTests ...`) works when no Editor has the folder.
- **Tests:** `bash Tools/Unity/run_tests.sh <class or prefix> PlayMode|EditMode`. The filter is a substring, not a pattern: `a|b`
  matches nothing and spins. Baseline: PlayMode 441, EditMode 3 known failures (`ArtAssetImportTests`, `LootAmountTests...`,
  `LootBalanceTests...TooHeavyToLift`). A different load-sensitive physics test fails in about one full run in three
  (`CarryFeelTests...WalkingDoesNotJolt`, `GuardChaseTests...RangedGuardRaisesTheAttackSignal`); re-run it alone before blaming a change.
  A full headless batch run also fails the cursor test and the raid-scene lexicon test on plain staging.
- **Looking at it:** `bash Tools/Unity/hud_events_check.sh <label> [--build|--no-build]` runs a co-op raid (Editor host plus
  built client), reads both HUD models, fires a hit, raises the watch and the grimoire by event and saves screenshots to
  `docs/generated/hud-events-303/`. Read the screenshots: the watch ring was wrong until a screenshot showed it. A visual
  change gets a before and after.
- **Shell:** a Bash command containing three single quotes in a heredoc dies ("unexpected EOF"). Write Python/C# with the Write
  tool and run the script file. Read files as UTF-8 (`PYTHONUTF8=1`). Many files are CRLF: keep their line endings.
- **House rules in effect:** edit in place, never rewrite a file (aj stopped a builder doing it); commit on your own branch,
  `Committed by AJ's agent`; merge `--no-ff` into staging and push; report commits as GitHub links; update the doc tier that
  changed (`docs/4-systems/raid.md` has the screen section); say what was not checked and why, and run the check if it can run;
  never name a local path in an issue; issues are closed by aj after testing, never by a PR.
- **Not verified yet, for aj:** the Settings mic meter bar with a person speaking at the whisper/shout marks; the earlier
  handoff's Unity checks (mic switch, Profiler checks, model scale and materials); #324 and #325 pressed with real keys by a person
  (the tests press them, the screenshots raise them by event); torch placement from #317 seen in a raid.

## Suggested skills

`unity:unity-cli` (driving the Editor), `superpowers:test-driven-development` (each cue: test the event, then the effect),
`superpowers:verification-before-completion` (before saying a step is done), `ponytail:ponytail` (smallest in-place diff).
