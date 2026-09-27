Vosk small English model
========================

This folder holds the extracted contents of vosk-model-small-en-us-0.15
(from https://alphacephei.com/vosk/models/). It is committed to git (decided
2026-09-22, see docs/6-decisions/Decisions.md), so a fresh clone and every build can
recognise speech with no extra step.

To replace or re-fetch it: Plunderspell > Voice > Download Vosk Small Model.

At runtime VoskVoiceInputService loads the model from:
  Application.streamingAssetsPath + "/VoskModels/small-en-us/"

The upstream model's own README sits beside this file as README (no extension).
