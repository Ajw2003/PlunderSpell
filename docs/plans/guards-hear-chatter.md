# Plan: guards hear what players say

**Status: code written 2026-09-30 on `claude/guards-player-chatter-plan-ac93ff`; not compiled, not run.** Approved by the owner with all recommended options. Step 8 is untested. Differences from the plan: see `docs/5-today/Today.md`.
Roadmap: M5 "Household awake" (`docs/2-roadmap/Roadmap.md`). It is the second of three stages
discussed on 2026-09-30:

1. mimic mode: guards replay disguised clips of your voice;
2. **guards hear chatter** (this plan);
3. a local language model that picks what guards say back.

The owner chose to go straight to stage 2. Stages 1 and 3 are out of scope here. Stage 3 builds
on this one, because every transcript this plan produces is what a model would read.

---

## Summary, in plain language

### What the player gets

Today the game listens to your microphone only while you hold the cast key, and only for spell
words. With this feature switched on, the game keeps listening between casts and writes down what
you say to your friends. That talk counts as noise in the castle, the same way footsteps and
spells already do.

- **Talk normally** and a guard in the next corridor hears you and comes to look.
- **Shout** and guards hear you from much further away. A shout can wake a guard that a sleep
  spell put down.
- **Whisper** and only a guard standing right next to you hears it. Whispering becomes a stealth
  skill.
- **Walls muffle you**, exactly as they already muffle every other noise.
- **Guards remember the words.** Each guard keeps the last thing it overheard. In stage 3, that
  is what lets a guard answer "go left" with something about the left corridor.
- **You can see what you were heard saying.** A short caption appears at the bottom of your
  screen, where the spell captions already go. For example: *"go left" - overheard by 2 guards*
  or *"go left" - nobody heard*. That tells you whether talking just gave you away.

### Privacy: off unless you switch it on

- A new line in Settings, **Guards hear my voice: Off / On**. It starts **Off** for everyone.
- While it is off, nothing changes from today: the game listens only while the cast key is held.
- Everything runs on your own computer. No audio is recorded to disk and nothing goes to the
  internet.
- **Audio never leaves your machine.** Only the written words do.
- **In co-op, the written words go to one other machine: the host's.** The host's computer runs
  the guards, so it has to know what the guards heard. The words are used there and then
  forgotten, apart from each guard's one remembered line, which ends with the raid.
- Your friends never see your transcript. Only you see your captions.

### What it costs

- **No new download.** It reuses the speech recogniser the game already ships (Vosk, a free
  offline speech-to-text engine) and its existing 68 MB English model.
- **Accuracy will be rough at first.** This small model is fine for "go left" or "grab the
  gold", but it will mishear mumbling and noisy rooms. A better model is an option (decision 1
  below).
- **Some processor time.** Listening all the time costs more than listening only while a key is
  held. It hasn't been measured yet; measuring it is a step in the plan.

### Two problems found while planning

Both are recorded here because they affect how this plan gets built and checked.

1. **In co-op, a friend's spell-casting voice never reaches the host's guards.** When a player
   who is not the host speaks a spell, their "voice noise" is played only into the copies of the
   guards on their own screen. Those copies don't think; the host's copies do, and they never
   hear it. The castle's alarm does hear it, because it forwards the noise to the host, but the
   guards don't come to look. This plan sends chatter to the host properly, so chatter won't have
   this problem. The existing spell-voice bug is a separate fix (decision 5).
2. **The repo's automatic test checker is currently broken.** `Tools/Headless/verify.sh` fails
   before running any test: 31 build errors in files this plan doesn't touch, all features the
   checker's stand-in versions of Unity don't cover yet (render pipeline, `LineRenderer`,
   `PhysicsMaterial`, and others). Seen on 2026-09-30 at commit `0119891`. Until that is fixed,
   the new tests in this plan can only run inside the Unity Editor.

### Decisions for the owner

The plan uses the **recommended** option in each case. Change any that read wrong.

| # | Question | Options | Recommended |
|---|---|---|---|
| 1 | Which speech recogniser writes down chatter? | (a) the small model the game already ships; (b) Vosk's larger English model, about 130 MB more, noticeably better at free speech; (c) Whisper (whisper.cpp), the best accuracy but a new native library and heavier on the processor | **(a) now.** Changing it later is one line. Try (b) if play shows too many mishearings |
| 2 | Does whispering really hide you? | Whisper reaches about 1.5 m; or whisper is ignored entirely | **About 1.5 m.** Close enough that pressing yourself past a guard while whispering is still a risk |
| 3 | Do other players see your captions? | Only you; or everyone in the raid | **Only you.** The host's machine gets the words (it has to) but doesn't display them |
| 4 | Is the setting remembered? | Remembered between sessions; or reset to Off every launch | **Remembered**, like the microphone choice |
| 5 | The co-op spell-voice bug above | Fix it inside this work; or file it as its own issue and fix it separately | **Separately**, as a small issue, so this change stays reviewable. It uses the same route to the host that this plan builds |
| 6 | The broken test checker | Fix it first; or build this and test in the Editor | **Build this, test in the Editor**, and file the checker as its own issue. Fixing it means extending stand-ins for about a dozen Unity features, unrelated to voice |

---

## How it works (technical)

### The data path

```
microphone (already open all raid; VoskVoiceInputService.WarmUp)
  └─ MainThreadPump.Update → ReadMicrophone()
       ├─ cast key held  → cast recogniser (grammar = spell words)          [unchanged]
       └─ otherwise, if chatter on → chatter recogniser (free-form, same Model)
              └─ end of utterance (AcceptWaveform == true) → ChatterReport
                   └─ IChatterSource.ChatterHeard
                        └─ PlayerChatterRelay (on the local player's body, owner only)
                             ├─ offline: resolve here
                             └─ online:  [ServerRpc] ReportChatter(text, volumeByte, mouthPosition)
                                   └─ server: NoiseBroadcaster.BroadcastSpeech(...)
                                        ├─ every INoiseListener → OnNoiseHeard(NoiseEvent{Type=Speech, Transcript})
                                        │     (guards investigate, alarm rises — existing rules)
                                        └─ every IEavesdropper → Overhear(evt) → true if awake to hear words
                                   └─ [TargetRpc] TellSpeaker(speaker, text, guardsWhoUnderstood)
                                        └─ PlayerChatterRelay.ChatterResolved (static event)
                                             └─ RaidHudView caption
```

### Why this shape

- **One model, two recognisers.** A `VoskRecognizer` is cheap next to a `Model`. The 68 MB model
  is already loaded once, off the main thread, in the constructor. A second recogniser built from
  it without a grammar gives free-form English. No second model load, no new files.
- **Cast speech never becomes chatter.** Samples go to exactly one recogniser. While the cast key
  is held, it's the cast recogniser, as today. When the key goes down, the chatter recogniser is
  flushed (`FinalResult()`, emitted, `Reset()`), so "watch out—" said just before a cast is
  still reported as chatter and the spell word isn't.
- **The host decides.** Guards are server-authoritative (`CastleGuard` ticks only
  `if (isServer)`). So chatter follows the same owner → `[ServerRpc]` → server-resolves route as
  `SpellCastingSystem.ServerCast`, not the local-only `FootstepNoiseEmitter` route that causes
  problem 1 above.
- **Volume crosses the network as a `byte`.** `docs/4-systems/net.md`, "RPC arguments must be
  types PurrNet's code generation registered": `CastVolume` failed to pack once and silently
  dropped every networked cast.
- **"Overheard by N guards" counts guards that understood, not listeners.** The alarm is also an
  `INoiseListener` and hears nearly everything, so a raw listener count would always be at least
  1. A new `IEavesdropper` interface (in Acoustics, because Acoustics can't reference Guards)
  lets `CastleGuard` answer "did I actually take in the words?" An asleep or stunned guard
  answers false.
- **The setting lives in Core.** `SettingsScreen` (UI) and the voice/relay code can't reference
  each other, which is the same reason `AudioInputSettings` already lives in Core. A static
  `Changed` event there lets a mid-raid toggle take effect without reading `PlayerPrefs` every
  frame.

### Tunables (starting values, to adjust in play)

| Spoken at | Classified by (existing `VoiceUtility`) | Radius | Strength | Effect on guards (existing `GuardBrain` rules) |
|---|---|---:|---:|---|
| Whisper | peak RMS < 0.1 | 1.5 m | 0.3 | investigates only at point-blank or once the castle is Stirred or higher |
| Normal | 0.1 – 0.4 | 6 m | 0.5 | investigates; wakes a sleeping guard with no wall between |
| Shout | > 0.4 | 14 m | 0.9 | investigates through one or two walls; wakes sleepers |

- Existing walls rule: each wall halves strength, up to 3 walls. Below 0.05 is inaudible
  (`AcousticEmitter`).
- An utterance whose peak RMS is below **0.02** is dropped as background noise. On near-silence,
  Vosk's free-form mode tends to output stray short words ("the", "huh").

---

## Execution steps

Every step names its files. Paths are under `Assets/_Project/Scripts/` unless they start with
`docs/` or `Tools/`. Each step is one commit, `feat:`/`test:`/`docs:` as the coding philosophy
asks.

### Step 1 — the setting (Core)

`Runtime/Core/GameFlow/AudioInputSettings.cs`
- Add `public const string GuardsHearChatterKey = "Settings.GuardsHearChatter";`.
- Add a `public static bool GuardsHearChatter { get; set; }` property, backed by
  `PlayerPrefs.GetInt(key, 0) == 1`. The default is **false**.
- Add `public static event Action<bool> GuardsHearChatterChanged;`, raised by the setter only when
  the value actually changes.

### Step 2 — chatter as a voice-layer concept (Voice)

New file `Runtime/Voice/IChatterSource.cs`:
- `public readonly struct ChatterReport { string Transcript; float PeakRms; CastVolume Volume; }`.
- `public interface IChatterSource { bool ChatterEnabled { get; set; } event Action<ChatterReport> ChatterHeard; }`.
- A pure `public static bool IsWorthReporting(string transcript, float peakRms)` (on a small
  static class `ChatterFilter`): false for empty or whitespace, or peak RMS below
  `MinChatterRms = 0.02f`.

`Runtime/Voice/VoskVoiceInputService.cs` implements `IChatterSource`:
- Fields (inside `#if !HEADLESS`): `_chatterRecognizer`, `_chatterPeakRms`, `_chatterGain`.
- `ChatterEnabled` setter: on → build the free-form recogniser (`new VoskRecognizer(model, 16000)`,
  a sibling of `EnsureRecognizer`), `OpenMicrophone(MicrophonePicker.Resolve())`, `EnsurePump()`,
  and set `_lastSamplePosition` to the current mic position so nothing from before the switch
  is read. Off → dispose the chatter recogniser.
- `ReadMicrophone()`: today's early return `if (!IsListening ...)` becomes
  `if (!(IsListening || ChatterEnabled) ...)`. The sample read, gain and RMS stay shared. Then
  branch: `IsListening` → the existing cast path (unchanged); else → the chatter recogniser.
  `AcceptWaveform` returning true means Vosk found the end of an utterance → `EmitChatter(Result())`.
- `StartListening()`: before it moves `_lastSamplePosition`, read the mic once more into the
  chatter recogniser, then flush it (`FinalResult()` → `EmitChatter`, then `Reset()`).
- `EmitChatter(json)`: parse with the existing `VoiceRecognizerJson`, apply
  `ChatterFilter.IsWorthReporting`, raise `ChatterHeard` with `VoiceUtility.ClassifyVolume(peak)`,
  then reset `_chatterPeakRms` and refresh `_chatterGain` from `AudioInputSettings.MicGain`
  (once per utterance, not per frame).
- A test seam next to `RecognizeSamples`: `RecognizeChatterSamples(short[])` returns the
  transcript, so the recorded WAV fixtures can check free-form accuracy.

`Runtime/Voice/CombinedVoiceInputService.cs`: implements `IChatterSource` by forwarding to
`Speech`.

`Runtime/Voice/VoiceServiceLocator.cs`: no change. Consumers use
`VoiceServiceLocator.Current as IChatterSource`. Keyboard-only machines have no chatter source,
and the feature quietly doesn't exist there. The relay logs that once, so it isn't a silent
failure.

### Step 3 — speech as a noise that carries words (Acoustics)

`Runtime/Acoustics/INoiseListener.cs`
- Append `Speech` to `NoiseType`, **at the end**, so stored and networked integer values don't
  shift.
- Add `public string Transcript;` to `NoiseEvent`, plus a constructor overload that takes it.
  The existing three-argument constructor leaves it null.
- New interface in the same file:
  `public interface IEavesdropper { bool Overhear(NoiseEvent speech); }`.

`Runtime/Acoustics/NoiseBroadcaster.cs`
- Move the body of `Broadcast` into a private core that also takes a transcript and counts
  listeners that are `IEavesdropper` and returned true from `Overhear`.
- `Broadcast(...)` keeps its exact signature and behaviour.
- New `public static int BroadcastSpeech(Vector3 origin, float radius, float strength, string transcript, int listenerLayerMask, int geometryLayerMask)`
  returns the number of guards that understood.

New file `Runtime/Acoustics/PlayerChatterRelay.cs` (`NetworkBehaviour`):
- Serialized tunables: the radius and strength table above; `_listenerLayers`, `_geometryLayers`,
  matching `AcousticEmitter`'s.
- Pure statics for tests: `RadiusFor(CastVolume)`, `StrengthFor(CastVolume)`.
- Subscribing follows `SpellCastingSystem`'s owner-or-offline pattern: `OnSpawned` if `isOwner`,
  and `Start` if `!isSpawned`. It subscribes to `IChatterSource.ChatterHeard` and
  `AudioInputSettings.GuardsHearChatterChanged`, and sets
  `source.ChatterEnabled = AudioInputSettings.GuardsHearChatter`. Unsubscribe and switch it off in
  `OnDisable`/`OnDestroy`, so leaving a raid stops listening.
- `HandleChatter(report)`: if the setting is off, return (belt and braces). Offline → `Resolve`
  locally. Online → `ReportChatter(report.Transcript, (byte)report.Volume, MouthPosition)`.
- `[ServerRpc(requireOwnership: true)] ReportChatter(string, byte, Vector3, RPCInfo info = default)`:
  cap the transcript at 200 characters (don't trust a client), run `Resolve`, then
  `[TargetRpc] TellSpeaker(info.sender, transcript, understood)`. The `[TargetRpc]` form copies
  `Net/DamageRelay.cs:101`.
- `public int Resolve(string transcript, CastVolume volume, Vector3 origin)`: public and
  network-free, like `SpellCastingSystem.ResolveSlam`, so tests need no transport.
- `public static event Action<ChatterOutcome> ChatterResolved;`, where
  `ChatterOutcome { Transcript, Volume, GuardsWhoUnderstood }`.
- The mouth position is the child camera's position (the player's eye), falling back to
  `transform.position + up * 1.6`.

`Runtime/Acoustics/Plunderspell.Acoustics.asmdef`: already references `PurrNet.Runtime` and
`Plunderspell.Voice`; add `Plunderspell.Core` for the setting.

### Step 4 — guards take in the words (Guards)

`Runtime/Guards/CastleGuard.cs`
- Implement `IEavesdropper`.
- `public string LastOverheard { get; private set; }` and `public int OverheardCount { get; private set; }`.
- `public event Action<string> Overheard;`: the hook stage 3 (the language model) and any bark
  audio will attach to.
- `Overhear(NoiseEvent speech)`: false if `IsIncapacitated` (runs after `OnNoiseHeard`, so a
  shout that woke the guard counts) or the transcript is empty. Otherwise it records, raises
  `Overheard`, and returns true.
- No reset needed: `GuardSpawner` spawns a fresh garrison each raid, so a guard's memory ends
  with its raid. (`CastleGuard` has no per-raid reset method; checked 2026-09-30.)
- `OnNoiseHeard` itself is **unchanged**: speech is just a noise to investigate, under the
  existing `GuardBrain.ShouldInvestigate` thresholds.

### Step 5 — the player body carries the relay

- `Prefabs/RaidPlayer.prefab`: add a `PlayerChatterRelay` next to `FootstepNoiseEmitter`. It must
  be on the prefab, not added at runtime, because PurrNet registers network behaviours at spawn.
  Done by editing the prefab YAML with the new script's GUID (its `.meta` is committed with the
  script), then confirmed in the Editor in step 8.
- `Editor/RaidSceneBuilder.cs` (`BuildFallbackPlayer`, next to line 365): add
  `root.AddComponent<PlayerChatterRelay>();`.
- `Editor/CombatBenchSceneBuilder.cs:179` gets the same line, so the bench can test it.
- `Scenes/CombatBench.unity`, `CastleBench.unity`, `ItemGym.unity` each have an inline player
  with `FootstepNoiseEmitter`. Leave them alone unless the owner wants chatter in those benches.
  The raid is what matters.

### Step 6 — the setting and the caption (UI)

`Runtime/UI/Screens/SettingsScreen.cs`
- A button row after the gain row, labelled `Guards hear my voice: Off` / `On`, that toggles
  `AudioInputSettings.GuardsHearChatter`. Refresh it in `OnShown`.
- Grow the panel from 700 to 780 px tall and the list from 450 to 530 px, so the extra row fits.

`Runtime/UI/RaidHud/RaidHudView.cs` and `Plunderspell.RaidHud.asmdef` (add a
`Plunderspell.Acoustics` reference)
- Subscribe to `PlayerChatterRelay.ChatterResolved` beside the existing `PhraseResolved`.
- A pure `public static string ChatterCaptionFor(ChatterOutcome, out Color)` for tests:
  - `"go left" - overheard by 2 guards`, in warning amber;
  - `"go left" - overheard by a guard`;
  - `"go left" - nobody heard`, in a quiet blue-grey.
- The caption uses the same slot and timer as the spell caption, and the newest one wins.

### Step 7 — tests

Written in the existing style (`Tests/Runtime/GuardTests.cs`, `VoiceCastingTests.cs`). They run in
the Editor's Test Runner. They can't run headlessly until the checker is fixed (problem 2).

New `Tests/Runtime/ChatterTests.cs`:
1. `Test_ChatterIsOffByDefault`: `AudioInputSettings.GuardsHearChatter` is false with no saved
   value.
2. `Test_TurningTheSettingOffStopsTheRelaySendingAnything`: a fake `IChatterSource` raises a
   report while the setting is off → `ChatterResolved` never fires and no guard hears anything.
3. `Test_NormalSpeechDrawsANearbyGuard`: a guard 4 m away, relay `Resolve("go left", Normal, …)`
   → guard state `Investigating`, `LastOverheard == "go left"`, returns 1.
4. `Test_AWhisperIsNotHeardAcrossTheRoom`: a guard 5 m away, `Whisper` → returns 0 and the guard
   keeps patrolling.
5. `Test_AShoutCarriesThroughAWall`: a guard 10 m away behind one wall, `Shout` → returns 1.
6. `Test_ASleepingGuardDoesNotTakeInWords`: a guard asleep, `Normal` speech too quiet to wake it →
   returns 0 and `LastOverheard` stays null.
7. `Test_TheAlarmHearsSpeechButIsNotCountedAsAGuard`: an alarm plus no guards → returns 0, and
   the alarm level rose.
8. `Test_BackgroundNoiseIsNotReported`: `ChatterFilter.IsWorthReporting("the", 0.01f)` is false;
   `("go left", 0.2f)` is true.
9. `Test_ChatterCaptionReadsRight`: the three caption forms.
10. `Test_BroadcastIsUnchangedForOrdinaryNoise`: an existing `Broadcast` call returns the same
    count as before (a guard against the refactor in step 3).

A speech-recognition fixture test, in `SpeechRecognitionTests`' style:
`Tools/VoiceFixtures/generate.ps1` gains five English phrases ("go left", "grab the gold",
"there's a guard", "run", "be quiet"). A test runs them through `RecognizeChatterSamples` and
**reports** the word accuracy rather than asserting perfection. That gives the "is the small
model good enough" answer (decision 1) a number.

### Step 8 — checking it in the real game (Editor, on the owner's Windows machine)

The feature isn't done until these pass. Each was checked by reading the code, not run.

1. **Solo, setting off:** talk freely during a raid. No chatter captions, guards ignore you,
   and the Console shows no `[Chatter]` lines. That's today's behaviour, unchanged.
2. **Solo, setting on:** stand around a corner from a patrolling guard and say "hello" at a
   normal voice. A caption `"hello" - overheard by a guard` appears, and the guard walks to where
   you were.
3. **Whisper:** same place, whisper. The caption reads `nobody heard` and the guard carries on.
4. **Casting still works:** hold the cast key and say a spell word. It casts, and no chatter
   caption appears for the spell word.
5. **Processor cost:** Profiler open, setting on, talk for 30 s. Note `MainThreadPump.Update`'s
   time per frame. If it exceeds about 2 ms, move the chatter recogniser to a worker thread
   (follow-up; the recogniser instance is not shared, so that is safe).
6. **Co-op, two machines:** the client player turns the setting on and speaks near a guard.
   The host's guard reacts; only the client sees the caption.

### Step 9 — docs

- `docs/4-systems/voice.md`: a "Chatter" section (the two recognisers, the opt-in, what leaves
  the machine).
- `docs/4-systems/alarm.md`: speech as a noise type; `IEavesdropper`.
- `docs/4-systems/net.md`: the chatter RPC pair.
- `docs/3-state/ProjectState.md`: M5 row.
- `docs/5-today/Today.md`: the day's entry.
- `docs/6-decisions/Decisions.md`: a dated entry for "chatter is opt-in, words go only to the
  host, audio never leaves the machine".
- This plan's status line changes to "built", with where the build differed.

---

## Risks

| Risk | What happens | Mitigation |
|---|---|---|
| The small model mishears a lot | Captions show nonsense; guards still react to the *noise*, which is the gameplay part | Step 7's accuracy number; decision 1(b) is a drop-in swap |
| Continuous recognition costs frame time | Stutter on weak PCs | Measured in step 8.5; worker-thread follow-up is ready |
| Game audio leaks into the mic (speakers, no headset) | Guards "hear" the game's own sounds or friends on Discord | The RMS floor filters some of it. The caption makes it visible. Headset recommended in the setting's tooltip |
| The prefab YAML edit is wrong | The relay is missing from the raid player | Step 8.2 fails loudly. The fix is to add the component in the Inspector |
| A client sends a huge or abusive transcript | Host memory or log spam | 200-character cap on the server; words are never displayed on the host |
