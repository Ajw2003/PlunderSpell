# Plan — a free sound-sourcing pipeline (search, tailor, review)

Status: **decided 2026-09-29, being built.** Replaces the ElevenLabs step; the reasoning is in
`docs/6-decisions/Decisions.md`, "Sounds come from free libraries and our own search". The menu theme
(`mus_title_loop`) is untouched by all of this.

## What a spike showed (2026-09-29, this machine)

`laion/clap-htsat-unfused` (Apache-2.0) on the RTX 5070 loaded in 12 s and embedded all 389 Kenney
files in 10 s. Text search returned the right recording when the library holds it ("a book page
turning" → `bookFlip1` at 0.58; "footsteps on stone" → `footstep04`; "chapel bell" → the bell
impacts) and wrong files at low scores (0.2–0.3) when it does not (stone door, boiling oil, sword on
flesh, fire, magic). So the score can report a gap. Spike script: not kept; the design below is the
result.

## What else was checked (2026-09-29)

- **Free CC0 magic and spell recordings exist** (this corrects the first version of the Decision):
  OpenGameArt "80 CC0 RPG SFX" (9 spell sounds), OpenGameArt "RPG Sound Pack", Kenney "Sci-fi
  Sounds" and "Digital Audio". Downloaded and registered as library roots (`bda0c262`).
- **The free Sonniss GDC 2026 bundle holds little magic.** Its track list (a Google Sheet, 1,008 rows)
  was read through a summariser, so the count is a lower bound: about 6 magic files, 1 shimmer loop
  and about 6 fire files, from the libraries "Emotion and Magic", "Fantasy Game 2", "Elemental
  Mutation Whooshes and Impacts", "Campfire - Bonfire FX" and a few others. Seventeen vendors in all,
  including Epic Stock Media and Cinematic Sound Design. Sonniss sells full magic libraries
  separately; those are not free.
- **Mixkit** (66 magic sounds): commercial use, including video games, no attribution; it does not
  allow redistributing the files. Read from a third-party summary; the licence text itself was not
  reachable. Pixabay bars standalone distribution (its own summary page).

## Pipeline

```
library roots ─► index (CLAP embeddings of ≤10 s windows) ─► search(prompt or brief) ─► top-N candidates
                                                                     │
        manifest rows still placeholders ─► coverage report          ▼
                                                            review page ─► decisions.json ─► picks.csv ─► build
```

1. **Index** (`Tools/AudioForge/finder/index.py`). Reads a list of library roots from
   `Tools/AudioForge/finder/roots.json` (absolute paths allowed, outside the repo; `Tools/AudioForge/library/kenney`
   is the first). Every audio file is cut into windows of at most 10 s (hop 5 s) and embedded with
   CLAP at 48 kHz. Incremental: unchanged files (path, size, mtime) are not re-embedded. Output goes
   to `Tools/AudioForge/finder/index/<root-name>.npz` plus a `.csv` of file, window start, duration.
   Use the GPU when present, the CPU otherwise. Never write anything into a library root.
2. **Search** (`finder/search.py`, and `audioforge.py find "<text>" [--top N]`). Cosine similarity
   of the text embedding against the windows, best window per file, filtered by duration limits and
   by the category's length rule (`forge/categories.py`). Prints file, window, score, duration and
   the audit flags of that window (`forge/audit.py` rules).
3. **Coverage** (`audioforge.py coverage`). Runs every placeholder row (not guard voices, not
   music) through search using its `ai_prompt` or `brief`, and writes
   `docs/generated/audio-coverage/coverage.csv` (sound, best score, best file) and `README.md`
   (how many of the 149 are covered above a stated threshold, the threshold and how it was chosen,
   and the sounds no library covers). Choose the threshold from data: score the existing `L` rows'
   chosen Kenney files against their own briefs and use what separates hits from misses; state the
   number and the check.
4. **Tailor.** A recipe layer `lib:<root-name>/<relative path> [start=s end=s gain=dB shift=ratio lp=Hz hp=Hz at=s]`
   in `forge/recipes.py` or wherever `kenney:` layers are resolved, so a picked window becomes a
   normal recipe that composes with the existing layers, per-category levels and the audit. Add
   what is missing and small: trim to a window, fade in and out, and a loop cross-fade helper
   (equal-power, tail folded over the head) so a picked ambience can become a `loop=1` sound.
5. **Review page** (`audioforge.py review [glob]` → `docs/generated/audio-review/index.html`, one
   self-contained page, no server). Per sound: its brief, the current file, and the top five
   candidates each with a player, score, window and audit flags; buttons Keep, Reject and a note;
   choices saved in `localStorage` and exported by an "Export decisions" button as
   `review-decisions.json`. Playing a candidate must play only its window.
6. **Apply** (`audioforge.py apply review-decisions.json`). Writes the kept candidates to
   `Tools/AudioForge/picks.csv` (`name, variant, recipe`), which the manifest build reads so a pick
   overrides the row's recipe and sets its `final` to `L`; then `build`, `check` run as usual and the
   licence of the source root goes into `build_report.csv` and `final/LICENCES.csv` as today.

## Not built now

Stable Audio Open or any generator (needs an account; listed in the Decision); music generation;
downloading libraries (the owner approves each download; Sonniss GameAudioGDC is pending the
repository-visibility decision); guard voices.

## Hard constraints

- Windows, Python 3.13 in UTF-8 mode (`audioforge.py` already restarts itself in it). `torch` (nightly
  cu128), `transformers`, `numpy`, `scipy`, `soundfile` are installed; **do not pip install anything
  else** without asking. No `librosa` or `torchaudio`.
- The CLAP model is in the local Hugging Face cache. Do not download other models.
- The library files stay where they are; raw third-party audio is never copied into `Assets/`
  except through `build` after a pick. Never commit files from outside `Tools/AudioForge/library`
  that are not CC0 or otherwise cleared: each library root in `roots.json` carries a `licence` field
  and `commit` true/false, and the index of a root with `commit` false is git-ignored. Only
  `kenney` is `commit: true` now.
- Stage by explicit path. Commit and push after each working piece, with the trailer
  `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`.
- Do not touch anything under `Assets/` except through `audioforge.py build`, and do not run a
  build that changes committed audio files without saying which files changed and why.
- The Unity Editor is open on this project; nothing here needs it. If a build changes audio under
  `Assets/`, the Editor reimports them; check `EditorApplication.isPlaying` is false first.

## Verify

- `find` returns the expected file for at least eight queries drawn from the `L` rows already in the
  manifest (their recipe's Kenney file should appear in the top five for that row's brief); report
  the hit rate honestly, including misses.
- `coverage` runs over the placeholders and the README states the covered count.
- The review page opens in a browser, shows players and scores, and Export decisions writes the JSON
  (check it in the built-in browser: screenshot and read the exported file). Playback quality is not
  checkable without listening; say so.
- `apply` on a small hand-made decisions file changes one row, `build` and `check` pass, and
  `git diff` shows only the expected files.
- Docs: `Tools/AudioForge/README.md` (new commands), `docs/plans/audio.md` §7.1 and §7.2 status,
  `docs/3-state/ProjectState.md`, `docs/5-today/Today.md`.
