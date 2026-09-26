# Voice

The first pillar of the pitch: casting is speaking, not pressing a button. `Plunderspell.Voice`
provides that input and nothing downstream of it — spell resolution belongs to `Spells`.

## What it owns

Turning a held key + microphone audio into a `VoiceRecognitionResult` (normalised text,
confidence, RMS amplitude, loudness bucket) and handing it to whatever subscribes
(`SpellCastingSystem`, in `Plunderspell.Spells`). Nothing else — Voice does not know what a spell word
means or what happens when one misfires.

## How it works

- **`IVoiceInputService`** (`IVoiceInputService.cs`) is the only surface consumers see:
  `StartListening()` / `StopListening()` / `OnPhraseRecognized`. Two implementations exist behind
  it, chosen automatically:
  - **`VoskVoiceInputService`** — the real path. The 68 MB model
    (`Assets/StreamingAssets/VoskModels/small-en-us/`) loads once, off the main thread, when the
    service is created. The microphone (`MicrophonePicker`: the Settings choice, else Windows'
    default) is opened at 16 kHz **once**, by `WarmUp()` when the caster hands over its vocabulary
    as the player spawns, and stays open until the pump is destroyed (leaving Play mode, quitting).
    Holding the cast key only marks where in the recording to start listening; a hidden
    `MainThreadPump` reads the new samples every frame on the main thread and feeds them to one
    reused recogniser; releasing flushes `FinalResult()`. Opening and closing the device per press
    froze the game for ~1.2 s on the first press (recogniser build plus open) and ~90 ms on every
    release (`Microphone.End`); now press and release take 2–9 ms (measured 2026-09-25). Loudness is the **peak** RMS across
    the whole hold. All Vosk and `Microphone` calls are wrapped in `#if !HEADLESS`.
  - **`MockVoiceInputService`** — the keyboard: `1`–`8` while holding the cast key (Shift for the
    misfire word, Ctrl to whisper).
  - **`CombinedVoiceInputService`** — both at once, so a player with a microphone can still cast
    from the number keys.
- **`VoiceServiceLocator.Current`** auto-registers on first access: `HEADLESS`, batch mode, a null
  graphics device, no microphone, or no model installed → keyboard only; otherwise, **in the Editor
  as well as a build**, speech + keyboard. `VoiceServiceLocator.Keyboard` always returns the
  keyboard service, whichever is current. Any caller (tests included) can override with
  `Register()` before first use.
- **The native library** is the `Vosk` 0.3.38 NuGet package's `Vosk.dll` + `libvosk.dll` (and its
  three MinGW runtime DLLs) in `Assets/_Project/Scripts/Runtime/Voice/Plugins/x86_64/`. The
  `VOSK_PRESENT` scripting define (Standalone) compiles out `VoskPluginStub.cs`, the throwing
  stand-in the headless harness still uses.

### Latin through an English model
<!-- ref:5ea6 -->

The small English model can only output words in its own vocabulary, and the spell words are Latin.
Free-form, it hears IGNIS as "agnes" and FRANGO as "franco" — which were the *misfire* spellings,
so before this every correctly spoken spell misfired. Each `SpellWord` therefore carries `HeardAs`
(English spellings that mean "said it right", e.g. "igneous") and `MisfireHeardAs` (e.g.
"egg nice"). `SpellLexicon.BuildHeardVocabulary()` maps every one back to the lexicon word — a
misfire spelling maps to the word's first `AltPronunciations` entry — and `SpellCastingSystem` hands
that map to the voice service (`IPhraseVocabularyTarget.SetVocabulary`). Vosk is then run with a
**grammar** of just those phrases plus `[unk]`, which is far more accurate than free-form, and
reports the canonical word; `MisfireEngine` resolves it exactly as it resolves a keyboard cast.

The spellings were chosen empirically: `Tools/VoiceFixtures/generate.ps1` synthesizes every word,
right and wrong, in Windows' two English voices, and `SpeechRecognitionTests` runs all 32 clips
through the real model. Current result: 16/16 correct pronunciations cast the right spell; 12/16
mispronunciations misfire. The four that don't (both FRANGO/FRANCO — the model can't hear g vs k —
and one voice each of LEVO and AURUM VOCO) cast the correct spell instead, which is the safe way to
fail. Tune a word by editing its `SpellWord` asset and re-running the test.

- **`PushToCastController`** is the hold-to-talk driver: press opens the mic, release closes it —
  closing is what triggers recognition. It only drives listening and visual feedback
  (`OnCastingStateChanged`); it deliberately does not depend on `Plunderspell.Spells` (dependency runs
  the other way), so a UI/animation change here can never touch spell logic.
- **`VoiceUtility`** is shared by both providers so they classify identically: `ClassifyVolume`
  buckets RMS into `Whisper` (< 0.1) / `Normal` / `Shout` (> 0.4); `Normalize` upper-cases, strips
  punctuation and collapses whitespace before the text ever reaches the lexicon in `Spells`.

### What you said, on screen

Added 2026-09-23 (#49). `SpellCastingSystem.PhraseResolved` fires on the caster's machine for
every phrase, including a fizzle (which casts nothing, so never reaches `CastResolved`). The raid
HUD captions it for 3.5 s: green `"igneous" -> IGNIS (Normal)` for a clean cast, orange
`"a nice" -> AGNIS - MISFIRE`, grey `"potato" - fizzled, not a spell`. The quoted text is exactly
what the recogniser output, so a mis-recognition reads differently from a real mispronunciation.
While V is held the same spot shows the open microphone and a live level meter with the
whisper/shout marks.

## Invariants

- **Both providers must classify and normalise identically.** `VoiceUtility` is the single place
  either one is allowed to do it — a provider that rolls its own would make Whisper/Shout and
  misfire matching behave differently depending on which one is active, including between a dev's
  editor session and a shipped Windows build.
- **`OnPhraseRecognized` only fires on the main thread**, and so does every `Microphone` /
  `AudioClip` call — Unity throws on those from any other thread. The previous design read the mic
  from a worker thread, threw on its first pass, and a bare `catch { break; }` hid it, so no audio
  ever reached the recogniser.
- **Every `SpellWord` needs at least one `HeardAs`** or real speech can never cast it.
  `SpeechRecognitionTests` fails if a correctly spoken word stops resolving.

## Traps

- **Never call `Microphone.Start` or `Microphone.End` on the cast key.** Both block the main
  thread (about 90 ms each on a USB headset). That was the lag on releasing V. The device stays
  open for the whole session; the Windows "microphone in use" indicator is on while playing.

- **`VoiceServiceLocator` never auto-registers at editor load**, on purpose — doing so would spawn
  a hidden driver `GameObject` in every edit-mode session with no play running. It only
  auto-registers from `RuntimeInitializeOnLoadMethod(BeforeSceneLoad)` or lazily on first
  `Current` access. Code that expects a provider to exist before play mode starts will find none.
- **This is the system the `isServer`/unspawned-object trap (see `raid.md`) hit hardest.** Before
  it was fixed, `OnSpawned` never fired offline, so nothing ever subscribed to
  `OnPhraseRecognized` in single-player — the mic opened and closed and nothing happened at all,
  with no error anywhere in the chain to point at why.
- **A machine with no microphone, or no model installed, silently downgrades to keyboard-only
  casting**, even in a real Windows build. The console says so, the player's screen does not.
- **The loudness thresholds (`Whisper` < 0.1, `Shout` > 0.4 RMS) were set without a real
  microphone.** Headset gain varies a lot; until issue #47's meter exists, check `[Vosk] Heard ...`
  lines in the log for the RMS a normal voice actually produces.
- **Unity's `Microphone` device list includes virtual devices** (e.g. "Virtual Desktop Audio").
  The service uses the system default (`null`), not `devices[0]`, for exactly this reason.
