# Handoff — 2026-09-29: UI fixes, the in-game audio layer, and the sound-sourcing pipeline

For the next session (or agent) on Plunderspell. Read `docs/1-landing/README.md` first, then this.
Branch: `ccr-6bf1f02d-o8jhoy` (46 commits ahead of `origin/main`, all pushed, not merged, no PR yet).
Machine: Windows 11, RTX 5070 (12 GB), Python 3.13, ffmpeg, node, `uvx`; Unity 6000.3.15f1 with a live
Editor on the project (`unity status` shows `state: ready`). Nobody has *listened* to any audio here.

## Where things stand

| Area | State | Read |
|---|---|---|
| **UI redesign** | Seen in the Editor and fixed: letter-spacing, figure baselines, pause title, HUD shadow, spell panel hidden while paused, "No raid yet." | `docs/plans/ui-visual-fixes-2026-09-29.md`; before/after frames `docs/generated/ui-fix-2026-09-29/` |
| **Audio layer (Phase A)** | Built and tested: `Plunderspell.Audio` assembly, mixer, `SoundBank` (483 entries), `AudioDirector`, `MusicDirector`, casting dip, Settings sliders now drive the mixer | `docs/4-systems/audio.md` |
| **Audio feel (Phase A2)** | Built and tested: footsteps and movement foley, physics impacts and breaks, guard voices (placeholder recordings) | same, plus `docs/plans/audio-feel-layer.md` |
| **Sound sourcing** | ElevenLabs dropped (owner has no account). A free pipeline: CLAP text search over CC0 libraries, coverage report, `lib:` recipe layer, picks and apply, review page. **Read the status table in `Tools/AudioForge/README.md` and the top of `docs/5-today/Today.md` for exactly which stages are built and verified.** | `docs/plans/audio-sourcing-pipeline.md`; Decisions 2026-09-29 |

## Decisions waiting on the owner (do not decide for them)

1. **Sonniss GameAudioGDC (free, 7.5 GB, no account).** Its licence forbids handing the raw files to
   others and this repo is public. Options: CC0 only for now; use Sonniss with its files kept out of git;
   or make the repo private first. The free 2026 bundle holds little magic (about 6 files), so it is
   mainly for fire, doors, weapons and ambience. A test that would settle it: download one of its five
   zips (about 1.5 GB), index it, run the same searches. Ask before downloading.
2. **Which spell and portal sounds the CC0 packs cover** is measured by `audioforge.py coverage`;
   what it leaves uncovered stays synthesised.
3. **Non-adaptive music** (five tracks besides the menu theme), the 16 adaptive raid stems and the
   alarm stingers: still undecided (`docs/plans/audio.md` §8). The **menu theme `mus_title_loop` is
   perfect and is not to be replaced** (`final = G` in the manifest).
4. **Guard voices**: the owner will record friends. Today's files are synthesised placeholders.
5. **Merging this branch to `main`** and whether to open a PR.

## Things that will bite you

- **Owner's loot work** is on branch `stashing` (`032b6bff`), not here. This branch adds one event to
  `Item.cs` (`Impacted`, raised at the top of `OnCollisionEnter`); check it still applies when
  `stashing` merges (`docs/4-systems/audio.md` notes this).
- **Before any recompile or test run, eval `EditorApplication.isPlaying`.** A recompile during Play
  mode once broke PurrNet and hung the Editor. Commands (all with `--caller plugin --skill unity-cli
  --no-banner`): `unity command eval '<C# statements ending in return ...;>'`, `recompile` then
  `recompile_status`, `run_tests --mode editor --filter <name> --async_tests true` then `test_status`,
  `capture_game_view --source screen` (Play mode only, saves under `Assets/`; move captures to `docs/`).
- **Two known EditMode failures, unrelated to audio or UI:** `ArtAssetImportTests` (three animation
  FBXs not imported as models, and an emissive material not HDR) and `LootBalanceTests` (a 12 kg
  Rolled Tapestry can spawn in the outer bailey). Last full run: 118 total, 113 passed, 2 failed,
  3 skipped.
- **Windows Python** opens files as cp1252; `audioforge.py` restarts itself in UTF-8 mode. A crash in
  `manifest` once truncated `manifest.csv`: check `git diff` after regenerating.
- **Line endings:** most source files are CRLF. Edit with tools that keep them; a Python script that
  reads and writes with `newline=''` does.
- **Shell quoting:** long heredocs with quotes have failed in Git Bash here; write a script file and run it.
- **Files under `Tools/AudioForge/library/`** are committed only if CC0. `finder/roots.json` carries a
  `commit` flag per root; anything with `commit: false` must stay out of git.
- **Unverified everywhere:** door sounds (no castle built in the test runs had a door), a second
  player, the hound, the bright-wall HUD shadow, and everything about how any sound *sounds*.

## First things to do

1. `git fetch` and `git log --oneline origin/main..origin/ccr-6bf1f02d-o8jhoy`; read
   `docs/5-today/Today.md` (top three entries) and `docs/4-systems/audio.md` (the gap list).
2. Ask the owner the five questions above; do not start Sonniss, music or a merge unasked.
3. If the owner wants to *hear* the game: play Play Solo, Lair, Set Out in the Editor. Have them note
   which sounds are wrong; fix by changing the manifest row and `audioforge.py build`, not the clip.
4. Finish whichever sound-sourcing stage the README marks unbuilt or unverified, then run the review
   loop with the owner listening.
