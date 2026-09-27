<!-- plain copy of: docs/4-systems/voice.md @ 20a54550bc8ab48360c82624fb00d58528ce9057 -->

# Voice

Full technical doc: [voice.md](../../4-systems/voice.md)

## What it is
The system that turns a held key plus microphone audio into recognised words. It does not decide
what those words mean; that is the spell system's job.

## Why it matters
Casting by speaking, rather than pressing a button, is one of the game's core ideas. If speech
recognition were unreliable or silently fell back to something else, that idea would fail
without the player ever knowing why.

## How it works
1. Holding a key opens the microphone; releasing it closes it and triggers recognition.
2. Real speech recognition is used when available, with typed number keys as a fallback, or
   running alongside it, when there is no microphone or no speech model installed.
3. Speech recognition only knows English, but the spell words are Latin, so each word is taught a
   set of English spellings that count as saying it correctly or nearly correctly.
4. How loudly a word was said is measured and classified as a whisper, normal, or a shout,
   using the same method regardless of which recognition path is active. A gain setting turns a
   quiet microphone up, or a loud one down, before any of this happens.
5. What the player said, and what it resolved to, is shown on screen for a few seconds:
   a clean cast, a garbled misfire, or a silent fizzle that was not a spell at all.

## Risks and safeguards
- **The microphone freezing the game on key press or release.** It is opened once per session,
  not on every press, so pressing and releasing stays fast.
- **Two recognition methods classifying volume or text differently.** Both share one place that
  measures loudness and cleans up recognised text.
- **A correctly spoken spell never resolving.** Every spell word must have a known
  correct-sounding English spelling, checked automatically.
- **Silently falling back to keyboard-only casting.** This happens with no microphone or model
  installed, shown only in the console, a known gap.
- **Recognition never starting at all offline.** Tied to the same offline-authority bug
  documented for the raid loop, now fixed.

## Related
- [Raid](raid.md)
- [Spells](spells.md)

## Left out
File and class names, the exact loudness thresholds, the underlying speech engine and model, and
the automated pronunciation tests.
