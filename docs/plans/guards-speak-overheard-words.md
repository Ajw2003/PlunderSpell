# Plan: mimic mode — guards speak in stitched-together player words

Written 2026-09-30 for the owner to implement; replaces the same-day "repeat the whole line" version
of this file (git history has it). Builds on PR #177 (guards overhear player chatter), which works in
co-op: `[Chatter] Heard "..."` in the log, the words reach the host, and `CastleGuard.Overhear`
stores them (`LastOverheard`, `Overheard` event).

**The idea** (Lethal Company-style, no LLM needed): every word a player says between casts is cut
out of the recording and kept in a word bank. Guards build new sentences from the bank with a Markov
chain, join the word clips with short crossfades (the choppy "ransom note" sound is the point), and
say them in a disguised version of the player's voice. The more people talk, the bigger the bank and
the stranger the sentences.

**Where it stands** (checked 2026-09-30): speech between casts is heard and transcribed (step 1's
listening half). Nothing below that is built.

## Prototype (built 2026-09-30, branch `claude/guard-mimic-prototype`)

The owner asked for the quickest free route to something testable: speak, and a guard speaks back
with your words at random pitches, chosen by a small local language model with no account. Built:

- **Words and audio:** the chatter recogniser now asks Vosk for word timings (`SetWords(true)`) and
  keeps the last 10 s of the voice it was fed; each `ChatterReport` carries the words, their times and
  the audio behind them (`VoskVoiceInputService.CutChatterAudio`).
- **Word bank:** `Audio/Mimic/MimicWordBank.cs` cuts each word heard with confidence 0.5+ (30 ms
  padding, 10 ms fades, 0.12-1.2 s), newest four clips per word, 300 words, for the play session.
- **Local model:** `Audio/Mimic/LocalLanguageModel.cs` starts llama.cpp's `llama-server.exe` (MIT, CPU
  build b11295) with Qwen2.5 0.5B Instruct Q4_K_M (Apache-2.0) from `StreamingAssets/LLM/` on
  127.0.0.1:8791, and stops it when the game object goes. A GBNF grammar allows only banked words, 2 to
  8 of them. The model file (491 MB) is not in git: `Tools/LLM/fetch_llm.ps1` downloads it.
- **Reply:** `Audio/Mimic/GuardMimic.cs` answers each line from the nearest awake guard within 25 m
  (needs 2+ banked words), stitching clips with `MimicStitcher` (each word at a random pitch
  0.75-1.3, 30 ms crossfades, 40-120 ms gaps), played from the guard. **Only the speaker hears it.**
  The model starts as soon as a raid has guards and "Guards hear my voice" is on. A "Guard mimicry
  (prototype)" tick box in `SoundFocusSettings` mutes it.

Checked: `GuardMimicTests` (4) pass; the model alone answers in 0.1-0.45 s with only allowed words;
`Tools/Unity/coop_mimic_check.sh` (co-op, the joining player fed four words as a recording) gave
"the gold is where the the is where" from a guard beside them, 1.0 s then 0.7 s to think once warm
(4.7 s when the model had to start first). Not checked by me: real speech through the microphone,
where the word times must line up with the kept audio (Vosk counts time from the recogniser's
start; if they do not line up the log says "fall outside the kept audio"). Known: with a tiny bank
the model repeats words ("the the the").

## Privacy, decided up front

PR #177 sends only written words. Mimic mode sends short clips of a player's voice to the other
players, so it has its own opt-in: Settings row **"Guards may use my voice"**, off by default, only
available while "Guards hear my voice" is on. Open decision for the owner before step 4: does the
bank last one raid (nothing saved) or carry over between sessions (voices saved to disk, said
plainly in the opt-in)? The plan assumes **one raid** until decided.

## Steps

1. **Word timings** (`Assets/_Project/Scripts/Runtime/Voice/VoskVoiceInputService.cs`). Call
   `SetWords(true)` on the chatter recogniser where it is created (`EnsureChatterRecognizer`). Vosk's
   result JSON then carries `result: [{word, start, end, conf}]`, times in seconds from the start of
   the utterance. Parse them in `EmitChatter` and add `WordTiming[] Words` to `ChatterReport`
   (`IChatterSource.cs`). Test: parse a saved Vosk result string into the right words and times.

2. **Keep the audio behind each line** (same file). In the pump, where `_chatterRecognizer.AcceptWaveform`
   is fed, also append those samples (after gain) to a buffer; hand it over and clear it in
   `EmitChatter`. Cap at 5 s of 16 kHz mono. Add `float[] Samples` to `ChatterReport`.

3. **Cut into word clips** (new `Assets/_Project/Scripts/Runtime/Voice/WordCutter.cs`, plain C#).
   For each word with `conf` of 0.5 or more: slice `Samples` from `start - 0.03 s` to `end + 0.03 s`,
   fade 10 ms in and out, skip clips under 0.12 s or over 1.2 s. Output `(string word, float[] clip)`.
   Test: a synthetic buffer with tones at known times cuts at those times.

4. **The word bank** (new `Assets/_Project/Scripts/Runtime/Voice/WordBank.cs`, plain C#). Per player:
   word → up to 4 clips (newest replace oldest), at most 300 words; plus every transcript kept, in
   order, for step 6. Lives on the host only, cleared when the raid ends. Test: caps hold, a repeated
   word keeps its newest four clips.

5. **Send clips to the host** (`Assets/_Project/Scripts/Runtime/Acoustics/PlayerChatterRelay.cs`). New
   codec (`Assets/_Project/Scripts/Runtime/Voice/VoiceClipCodec.cs`): 16 kHz to 8 kHz, 8-bit mu-law, so
   a 0.5 s word is 4 KB. A new `[ServerRpc] ReportWords(string[] words, byte[][] clips)` alongside
   `ReportChatter`, only when the second opt-in is on; the server caps 20 words and 10 KB per clip (a
   client is not trusted) and adds them to that player's bank. If PurrNet will not take a jagged
   array, send one word per RPC. Test: codec round trip keeps length and a sine's peak within 10 %.

6. **Stitch a sentence** (new `Assets/_Project/Scripts/Runtime/Voice/MimicSentence.cs`, plain C#). A first-order
   Markov chain over the kept transcripts: start from a word that began a line, walk to a following
   word, stop at 3-8 words or a word that ended a line; use only words that have a clip. Join the clips
   with 30 ms equal-power crossfades and 40-120 ms gaps chosen at random. Seeded `System.Random` so
   tests can pin it. Test: with a seed, the sentence is reproducible and every word is in the bank.

7. **Disguise the voice** (in `MimicSentence` or a `VoiceDisguise.cs`). First pass: resample the
   stitched clip to change pitch and speed together (0.82-0.9 of the speaker's pitch, guard-to-guard
   fixed), then a low-pass near 3 kHz and a little saturation, so it reads as a different, rougher
   voice. Pitch alone still sounds like the player: that is accepted for now. Later, if wanted: a
   formant shift (moving the resonances separately from the pitch, e.g. an LPC or phase-vocoder
   envelope shift). Voice-conversion models (RVC) are out of scope.

8. **Guards say it** (host decides, everyone plays). In `CastleGuard`, on the server: at most one mimic
   per guard per 25 s, only while awake and not attacking, a 30 % chance in place of a patrol murmur
   (`GuardLine.Murmur` in `GuardVoiceDirector`) once the bank holds 8 or more words. The host stitches
   (step 6), disguises (step 7), encodes (step 5) and sends `[ObserversRpc] SayMimic(byte[] audio,
   string text)`; each machine raises `CastleGuard.SaidMimic(guard, audio, text)`.

9. **Play it** (`Assets/_Project/Scripts/Runtime/Audio/GuardVoiceDirector.cs`). On `SaidMimic`: decode,
   `AudioClip.Create("mimic", n, 1, 8000, false)` + `SetData`, play through the voice pool at the
   guard's head (same `HearingDistance`, same `LineGapSeconds`), destroy the clip when done. Gate it
   with a **"Guard mimicry"** tick box in `SoundFocusSettings` (`Assets/_Project/Resources/SoundFocusSettings.asset`).

10. **Settings row** (`Assets/_Project/Scripts/Runtime/UI/Screens/SettingsScreen.cs` and
    `Assets/_Project/Scripts/Runtime/Core/GameFlow/AudioInputSettings.cs`): "Guards may use my voice",
    Off/On under "Guards hear my voice" in the Voice column, key `Settings.GuardsUseVoice`.

## How to check it (co-op, not solo)

- EditMode tests from steps 1, 3, 4, 5 and 6.
- Co-op: extend `Tools/Unity/coop_chatter_check.sh`. Feed the joining player's relay a
  `ChatterReport` with three words, word timings and a buffer of three tones at those times; check the
  host's bank for that player holds three words with clips. Then force one mimic from the guard beside
  them and check that **both** machines play an `AudioSource` clip named `mimic` near the guard.
- By ear, two players: talk near guards for a few minutes, then listen for guards speaking new
  sentences made of your words, lower and rougher than your own voice.

## Known limits

- The small Vosk model mishears ("oh cheese or jesus" for real speech on 2026-09-30), so word labels
  are often wrong. The clips are still the player's real sounds, so the result stays in their voice;
  the sentences just will not track what anyone meant. That suits the unhinged tone.
- Word edges from Vosk are rough; the 30 ms padding and fades hide most clicks.

## Out of scope

LLM-built sentences (a later option: a small model restricted to words in the bank); formant-correct
disguise and RVC; walls muffling speech (PR #177's empty geometry layer mask); guards acting on the
meaning of the words.
