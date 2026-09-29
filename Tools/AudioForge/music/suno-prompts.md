# Suno prompts — the six non-adaptive tracks

The adaptive raid music (16 stems, 4 per Age) can't come from Suno: the layers have to share one
tempo, key and bar grid, and Suno makes whole songs. These six tracks only need to be good loops,
which is what Suno is for. Placeholder versions of all six already play in the game
(`Assets/_Project/Audio/Music/`), so nothing is blocked while these are made.

**Before you start:** make the songs on a **paid** Suno plan (Pro or Premier). Suno's terms give
commercial rights only to songs *made while subscribed*. Upgrading later does not cover songs
made on the free plan. Check the current terms on suno.com before shipping; they have changed
before.

## How to make each one

1. In Suno, choose **Custom**, switch **Instrumental** on, paste the **Style** line into "Style of
   Music", and use the **Title** given.
2. Generate. Suno gives two versions each time; generate until one fits the **Listen for** line.
   Budget about 4–6 generations per track.
3. Download the keeper as **WAV** (MP3 works if WAV isn't offered).
4. File it and rebuild. Three commands, in a terminal opened at the repo root (the folder that
   holds `Assets/`). Taking the title track as an example, with the download saved as
   `herald-overture.wav` in that same folder:

```bash
python3 Tools/AudioForge/audioforge.py promote herald-overture.wav mus_title_loop_01 --licence "Suno Pro, commercial use (AI-generated)" --origin "Suno: The Herald's Overture"
python3 Tools/AudioForge/audioforge.py build "mus_*"
python3 Tools/AudioForge/audioforge.py check
```

   The last line should end `OK: 0 problem(s)`. On Windows, type `python` instead of `python3`.

The build crossfades the end into the start so it loops. If the loop point sounds wrong, trim
the WAV to a clean phrase ending (Audacity is enough) and promote it again.

---

### 1. `mus_title_loop_01` — main menu

- **Title:** The Herald's Overture
- **Style:** `medieval instrumental, lute, hurdy-gurdy, recorder, tabor drum, pompous and slightly comic, grand fanfare energy played by a shabby tavern band, E minor, 66 bpm, loopable`
- **Listen for:** a theme that is a little too grand for the band playing it. The pitch's herald
  oversells; the music should too. No vocals, no modern drums, no synths.

### 2. `mus_lair_loop_01` — the Lair

- **Title:** Damp, and Ours
- **Style:** `quiet medieval instrumental, solo lute and soft hurdy-gurdy drone, one candle, warm but shabby, E minor, 66 bpm, sparse, intimate, loopable`
- **Listen for:** the same mood as the title, stripped down. Safe, damp, a bit sad about the debt.
  It plays for minutes at a time between raids, so nothing may stick out.

### 3. `mus_lair_after_loss_loop_01` — the Lair after a failed raid

- **Title:** The Debts Fall Due
- **Style:** `slow sad medieval instrumental, solo hurdy-gurdy, mournful drone, E minor, 56 bpm, sparse, loopable`
- **Listen for:** lonelier than #2. One instrument if you can get it.

### 4. `mus_results_success_loop_01` — results screen after a good haul

- **Title:** Four Wizards Count Their Gold
- **Style:** `jaunty medieval tavern reel, lute, recorder, tabor, hand claps, celebratory, slightly drunk, D dorian, 112 bpm, loopable`
- **Listen for:** a dance. It plays under a coin-counting tally, so leave room above 2 kHz for the
  coin chimes.

### 5. `mus_results_failure_loop_01` — results screen after a bad night

- **Title:** Nothing Came Home
- **Style:** `slow tipsy medieval reel, detuned lute and wheezy recorder, melancholy but funny, A minor, 56 bpm, loopable`
- **Listen for:** a sad joke, not a tragedy. The game is a comedy even when you lose.

### 6. `mus_credits_01` — credits

- **Title:** Plunderspell (The Whole Theme)
- **Style:** `medieval folk instrumental, full band: lute, hurdy-gurdy, shawm, recorder, sackbut, tabor and frame drum, rousing, E minor, 80 bpm`
- **Listen for:** the title theme at full size. The only track allowed to run 3+ minutes and to end
  instead of loop.

---

## What still needs a person, not Suno

The 16 adaptive raid stems (`mus_raid_<age>_<layer>`) and the per-Age alarm stingers
(`sting_alarm_*`) are marked **C** (composed) in the manifest. That's the one decision still open:
commission a composer, or compose them yourself from the placeholder sketches. See
`docs/plans/audio.md` §8.
