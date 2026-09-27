"""One-off doc edit for the microphone gain (#125, 2026-09-26): voice doc, its plain copy, state,
today. Fails on its own asserts if re-run after it has applied."""
import os
import re
import subprocess

os.chdir(os.path.join(os.path.dirname(__file__), '..', '..', '..'))


def sub(p, old, new):
    s = open(p, encoding='utf-8').read()
    assert s.count(old) == 1, (p, old[:70], s.count(old))
    open(p, 'w', encoding='utf-8', newline='').write(s.replace(old, new))


p = 'docs/4-systems/voice.md'
sub(p, '''While V is held the same spot shows the open microphone and a live level meter with the
whisper/shout marks.
''', '''While V is held the same spot shows the open microphone and a live level meter with the
whisper/shout marks.

### Microphone gain

Added 2026-09-26 (#125). Settings has a **Microphone Gain** slider, 0.25x to 4x, saved as
`AudioInputSettings.MicGain` (PlayerPrefs `Settings.MicGain`, 1 when unset). `VoskVoiceInputService`
reads it when the cast key goes down and multiplies every sample by it, clipping at full scale
(`VoiceUtility.ApplyGain`), before anything else sees the audio: the recogniser, the loudness
classification and the level meter all hear the same, amplified voice. So a quiet headset can
reach a normal or shouted cast, and a hot one can be turned down so a normal voice is not a shout.
Raising it also raises background noise into the recogniser. Tests: `MicGainTests`.
''')
sub(p, '''- **The loudness thresholds (`Whisper` < 0.1, `Shout` > 0.4 RMS) were set without a real
  microphone.** Headset gain varies a lot; until issue #47's meter exists, check `[Vosk] Heard ...`
  lines in the log for the RMS a normal voice actually produces.''', '''- **The loudness thresholds (`Whisper` < 0.1, `Shout` > 0.4 RMS) were set without a real
  microphone.** Headset gain varies a lot, which is what the Settings gain slider is for: hold V
  and watch the level meter against the whisper/shout marks while adjusting it.''')

p = 'docs/plain/4-systems/voice.md'
sub(p, '''4. How loudly a word was said is measured and classified as a whisper, normal, or a shout,
   using the same method regardless of which recognition path is active.''', '''4. How loudly a word was said is measured and classified as a whisper, normal, or a shout,
   using the same method regardless of which recognition path is active. A gain setting turns a
   quiet microphone up, or a loud one down, before any of this happens.''')

p = 'docs/3-state/ProjectState.md'
sub(p, '''neither word has been tried with a real voice (`docs/4-systems/spells.md`, "Velox and
Saltus").''', '''neither word has been tried with a real voice (`docs/4-systems/spells.md`, "Velox and
Saltus"). Settings has a microphone gain slider (#125; `docs/4-systems/voice.md`, "Microphone
gain").''')

p = 'docs/5-today/Today.md'
sub(p, '''Tests before those two:''', '''Later: the owner's spell retune committed (a 10 m dash, a 13 m leap, stronger Levo). #155 closed
as a duplicate of #156. #125: a microphone gain slider in Settings, 0.25x to 4x, applied to the
audio before recognition and loudness. The Editor had closed, so this was compiled and tested with
Unity in batch mode: PlayMode 230 of 233 (the known guard test; the cursor-lock test, which needs a
window; and `RaidSceneCastingTests`' lexicon test, which then passed twice on its own), EditMode
69 pass, 3 skip, 2 fail (the known art import test; the owner's Rolled Tapestry edit). The new
Settings row's layout has not been looked at.

Tests before those two:''')

for plain, source in [('docs/plain/4-systems/voice.md', 'docs/4-systems/voice.md'),
                      ('docs/plain/3-state/ProjectState.md', 'docs/3-state/ProjectState.md')]:
    h = subprocess.run(['git', 'hash-object', source], capture_output=True, text=True).stdout.strip()
    s = open(plain, encoding='utf-8').read()
    open(plain, 'w', encoding='utf-8', newline='').write(re.sub(r'@ [0-9a-f]{40} -->', '@ ' + h + ' -->', s, count=1))
print('ok')
