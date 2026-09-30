# Plan: guards repeat what they overheard, in the player's own voice

Written 2026-09-30 for the owner to implement. Builds on PR #177 (guards overhear player chatter),
which already works in co-op: `[Chatter] Heard "..."` in the log, the words reach the host, and
`CastleGuard.Overhear` stores them (`LastOverheard`, `Overheard` event).

**What this adds.** A guard that overheard a line says it back later, in the recorded voice of the
player who said it, pitched and filtered so it reads as the guard: a mimic. Every player near the
guard hears it. The guard's own synthesized lines stay for everything else (hurt, attack, alert).

If "replace theirs" meant something else, such as recording players' voices to replace the
placeholder guard lines (`vo_*` in the SoundBank), stop and say so: that is a different plan.

## What changes for privacy

PR #177 sends only the written words; audio never leaves the speaker's machine. This plan sends a
short clip of the speaker's voice to the other players. That needs its own opt-in: a second Settings
row, **"Guards may repeat my voice"**, off by default, only available while "Guards hear my voice"
is on. With it off, nothing below runs and the words-only behaviour is unchanged.

## Steps

1. **Keep the audio of each chatter line** (`Assets/_Project/Scripts/Runtime/Voice/VoskVoiceInputService.cs`).
   The chatter recogniser is fed from the microphone in the pump (`_chatterRecognizer.AcceptWaveform`).
   Also append those samples (after gain) to a buffer; clear it when `EmitChatter` fires. Keep at
   most the last 3 s at 16 kHz mono (48,000 floats). Add `float[] Samples` to `ChatterReport`
   (`IChatterSource.cs`). Drop the audio when the line is shorter than 0.4 s.

2. **Encode small** (new `Assets/_Project/Scripts/Runtime/Voice/VoiceClipCodec.cs`, plain C#). Down-sample
   16 kHz to 8 kHz, then 8-bit mu-law: 3 s becomes 24 KB. `byte[] Encode(float[] samples16k)` and
   `float[] Decode(byte[] data)` returning 8 kHz floats. EditMode test: a 440 Hz sine survives the
   round trip with its peak within 10 % and the same length.

3. **Send it with the words** (`Assets/_Project/Scripts/Runtime/Acoustics/PlayerChatterRelay.cs`). Add
   `byte[] audio` to `ReportChatter` (empty when the second opt-in is off). On the server, cap it at
   24 KB (a client is not trusted), then pass it through `Resolve` so `NoiseBroadcaster.BroadcastSpeech`
   can hand it to guards. If PurrNet refuses a 24 KB RPC, split into 4 KB chunks with a line id and
   reassemble on the server.

4. **The guard remembers the voice** (`Assets/_Project/Scripts/Runtime/Guards/CastleGuard.cs`). Carry the
   audio on `NoiseEvent` (next to `Transcript`) and store it in `Overhear` as `LastOverheardAudio`.
   Only the host holds it.

5. **The guard says it back** (host decides, everyone plays). In `CastleGuard`, on the server: 4-10 s
   after overhearing, if the guard is awake, not attacking, and has not mimicked in the last 20 s, call
   an `[ObserversRpc] SayOverheard(byte[] audio, string words)`. Each machine raises a static
   `CastleGuard.SaidOverheard(guard, audio, words)` event.

6. **Play it as the guard** (`Assets/_Project/Scripts/Runtime/Audio/GuardVoiceDirector.cs`). Subscribe to
   `SaidOverheard`: decode, `AudioClip.Create("mimic", n, 1, 8000, false)` + `SetData`, play through
   the voice pool at the guard's head (same `HearingDistance`, same `LineGapSeconds` so it never talks
   over its own line). Make it read as the guard: pitch 0.85-0.9 and a low-pass around 3 kHz. Destroy
   the clip when it finishes. Show `words` as a caption over the guard if one exists, else skip.

7. **Mute switch.** Add a `SoundFocusSettings` override-style bool, **"Guard mimicry"**, checked in step 6,
   so it can be switched off in the Inspector like every other group
   (`Assets/_Project/Resources/SoundFocusSettings.asset`).

8. **Settings row** (`Assets/_Project/Scripts/Runtime/UI/Screens/SettingsScreen.cs`): "Guards may repeat my
   voice", an Off/On row under "Guards hear my voice" in the Voice column; key
   `Settings.GuardsRepeatVoice` in `Assets/_Project/Scripts/Runtime/Core/GameFlow/AudioInputSettings.cs`.

## How to check it (co-op, not solo)

- EditMode: the codec test from step 2; a pure test of the step 5 timing rule.
- Co-op: extend `Tools/Unity/coop_chatter_check.sh`. After it moves a guard beside the joining player,
  feed a `ChatterReport` that carries a 1 s 300 Hz tone as `Samples`, wait 11 s, then on **both**
  machines look for an `AudioSource` in the voice pool playing a clip named `mimic`. Pass: both have
  one, near the guard.
- By ear: two players, one says a sentence near a guard with both opt-ins on; within 10 s the other
  hears it back from the guard, lower and muffled.

## Out of scope

Walls muffling speech (PR #177's known gap: the relay's geometry layer mask is empty); guards acting
on what the words mean; replacing the placeholder `vo_*` guard lines.
