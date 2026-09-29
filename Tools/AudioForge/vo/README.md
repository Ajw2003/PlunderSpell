# Recording the guard voices

Every human enemy in the game has a voice: 15 voices, 29 short takes each, **435 files**. Right now
they are made-up placeholder babble (`Assets/_Project/Audio/VO/`). This folder is everything
needed to replace them with friends' voices.

## Who reads what

One person can easily do two to four voices by changing how they speak (gruff, nasal, old, posh,
bored). With four friends that's about four voices each, roughly **10–15 minutes of recording per
voice** including retakes.

| Age | Sheet | Voices |
|---|---|---|
| Bronze (c. 1200 BC) | [line-sheets/bronze.md](line-sheets/bronze.md) | levy, slinger, champion, keeper |
| High Medieval (c. 1250) | [line-sheets/high.md](line-sheets/high.md) | warden, crossbowman, knight |
| Late Medieval (c. 1450) | [line-sheets/late.md](line-sheets/late.md) | halberdier, handgunner, manatarms, pavisier |
| Age of Powder (c. 1620) | [line-sheets/powder.md](line-sheets/powder.md) | guard, musketeer, cuirassier, petardier |

Every voice in an Age reads the same 29 lines; the **character note** at the top of each sheet
says how that voice should sound. Nobody needs to get the old languages right. Confidence beats
accuracy, and a mangled line fits the game's comedy.

## How to record

- **Any mic is fine**: a USB mic, a headset, or a phone's voice-memo app held 20 cm away.
- **Record somewhere dead**: a wardrobe full of clothes, under a duvet, or a small carpeted room.
  Avoid bathrooms and kitchens, where echo would put every guard in the same tiled room.
- **Leave a second of silence** before and after each take. Don't worry about trimming; the build
  trims.
- **Perform it bigger than feels natural.** Shouts should really be shouted (turn the mic gain down
  first so it doesn't distort). Whispers and mutters should be quiet.
- **One file per take**, named exactly as in the sheet's **File** column, e.g.
  `vo_high_knight_chase_01.wav`. WAV is best; `.flac`, `.ogg` and `.mp3` also work. If the app
  only exports one long recording, send that instead, and say so.

## Permission

Each performer sends this line once (a text or email is enough), and it's kept alongside the
recordings:

> I agree that recordings of my voice made for Plunderspell may be used, edited and sold as
> part of the game and its marketing, without further payment. — *name, date*

## Getting the files into the game

1. Put the correctly named files into `Tools/AudioForge/final/`.
2. From the repo root, record them with their performer, rebuild, and check:

```bash
python3 Tools/AudioForge/audioforge.py promote --scan --licence "Recorded by <name>, release given <date>" --origin "voice session"
python3 Tools/AudioForge/audioforge.py build "vo_*"
python3 Tools/AudioForge/audioforge.py check
```

   The last line should read `OK: 0 problem(s)`. Run the `promote --scan` line once per
   performer, right after dropping in their files, so each file gets the right name in
   `final/LICENCES.csv`. Any file not named like a manifest row is refused, with its name printed.
3. `python3 Tools/AudioForge/audioforge.py preview` rebuilds the listening page
   (`docs/generated/audio-preview/index.html`), where the new takes show a gold "recorded" tag.

The build turns each take to mono, trims it and sets its level. Per-Age EQ (Bronze drier and warmer,
Powder brighter) is a planned step, not built yet.
