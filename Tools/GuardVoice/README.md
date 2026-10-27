# Guard voice recording

One person records ONE set of guard lines per historical Age (bronze, high, late, powder): 29 lines
per Age, 116 in total. Everything is read in one natural voice. The game disguises that one voice
into many distinct guards later, so do not do different characters.

The line text comes from `Tools/AudioForge/vo/make_line_sheets.py` (the single source). This folder
adds a tone direction for every line and a recorder page that walks you through them.

## Steps

1. Regenerate the script and page (only needed if the lines change):
   `python Tools/GuardVoice/make_recording_script.py`
2. Open `Tools/GuardVoice/record.html` by double-clicking it in Chrome or Edge.
3. Pick an Age. Click **Choose output folder** and pick `Tools/GuardVoice/takes/<age>/`
   (make the folder first, for example `Tools/GuardVoice/takes/bronze/`). Each kept take is saved
   there straight away. If your browser cannot pick a folder, each take downloads instead.
4. For each line: press **Space** to start, say the line, **Space** to stop, listen back with
   **Play back**, then **Enter** (Keep and next) or **Redo**. **Previous**, **Skip** and the line
   list on the right let you jump around; progress is remembered per Age.
5. Turn the mic gain DOWN before lines marked LOUD, and back up for quiet ones. The page warns on
   clipping, on a take that is too short, and on a nearly silent take.

Files are named `vo_<age>_base_<situation>_<NN>.wav` (for example `vo_bronze_base_chase_01.wav`).
The human-readable script, with the tone for every line, is in `recording-script/<age>.md`.

How the files get into the game is handled later by the assistant.

## Recording tips

- Any mic is fine: a USB mic, a headset, or a phone held 20 cm away.
- Record somewhere dead: a wardrobe full of clothes, under a duvet, or a small carpeted room.
  Avoid bathrooms and kitchens.
- Leave a second of silence before and after each take. The build trims.
- Perform bigger than feels natural. Shouts should really be shouted (gain down first);
  whispers and mutters should be quiet.
- Words in *(brackets)* in the old line sheets are directions or translations: never say them.

## Permission

Each performer sends this line once (a text or email is enough), and it's kept alongside the
recordings:

> I agree that recordings of my voice made for Plunderspell may be used, edited and sold as
> part of the game and its marketing, without further payment. — *name, date*

## Stand-in clips (computer voices)

Until real takes exist, every line has a computer-voice stand-in in `takes-tts/<age>/`, named exactly like a
real take, so a recording simply replaces it. Windows' David and Zira read the lines with a tone per
situation; snores are synthesised because no voice can snore. To rebuild and check them, from any folder:

```powershell
powershell -ExecutionPolicy Bypass -File Tools\GuardVoice\make_tts_base.ps1
python Tools/GuardVoice/check_takes.py Tools/GuardVoice/takes-tts
python Tools/GuardVoice/make_listen_page.py
```

You should see `Wrote 108 text-to-speech clips`, `Wrote 8 snore clips`, then `Checked 116 takes.` and
`0 problem(s).` Open `takes-tts/listen.html` to hear them. `check_takes.py` also checks your real takes
(`python Tools/GuardVoice/check_takes.py Tools/GuardVoice/takes/powder powder`).
`check_intelligibility.ps1` plays them into Windows' speech recogniser as a rough check that words are
recognisable; it scores archaic words and grunts low by design.

## Files

- `make_tts_base.ps1`, `make_snores.py`: build the stand-in clips and `takes-tts/tts-manifest.csv`
- `check_takes.py`, `check_intelligibility.ps1`, `make_listen_page.py`: check them and build the listening page

- `make_recording_script.py`: generator (Python 3, stdlib only, deterministic)
- `record.template.html`: the page source; `record.html` is generated from it with the lines embedded
- `record-lines.json`: the lines with tone, loudness, minimum length and file name
- `recording-script/<age>.md`: readable script per Age
- `screenshots/`: screenshots of the page taken during testing

## Not verified

Tested in the built-in browser with a stubbed microphone (an oscillator stream), on both the
AudioWorklet path (served from `http://127.0.0.1`) and the ScriptProcessor fallback (plain file
view, no worklet available there). Meter, take length, playback, WAV encode and decode and the
file name were checked. NOT tested:

- A real microphone (permission prompt, real levels, clipping on real shouts).
- `showDirectoryPicker` (choosing a folder and writing files into it).
- The download fallback producing a file on disk (the code path ran, but the saved file was not inspected).
- Opening `record.html` from `file://` in an actual Chrome or Edge window (the built-in pane
  showed it as a snapshot, where `navigator.mediaDevices` is missing).
