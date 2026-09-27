# Plunderspell

A co-op voice-cast heist. Four wizards break into a procedurally generated castle, say Latin badly,
and try to carry the treasure out before the place wakes up. What you steal pays down a debt; the
debt grows every time you set out.

Unity 6000.3.15f1, PurrNet for networking, Vosk for offline speech (with a keyboard mock).

## Play it

1. Open the project in Unity.
2. **Tools ▸ Plunderspell ▸ Build Playable Raid Scene** — builds
   `Assets/_Project/Scenes/RaidScene.unity` from code: castle, haul, garrison, extraction zone,
   HUD and a player.
3. Press Play.

| | |
| --- | --- |
| `WASD` / mouse | move, look |
| `Shift` / `Ctrl` | sprint (loud) / crouch (quiet) |
| `E` / `Q` | take, open a door / drop |
| Hold `V` | open the mic — this is how you cast |
| `1`–`8` while holding `V` | speak a spell word (mock recogniser) |
| `Shift` + `1`–`8` | mispronounce it deliberately, and find out what that costs |
| `Ctrl` held | whisper — weaker, but quiet |
| `F5` / `F6` | call the extraction / go again |

Everything the scene builder generates is placeholder: primitives, flat colours, one room shape. It
is a harness for playing the game, not the art pass.

## The loop

Set out from the Lair (which raises the debt) → a castle is built from one seed → steal what you can
carry → be heard, or don't → reach the extraction zone before the clock → what you carried out pays
the debt down → go again.

Three things decide how a raid goes:

- **Volume.** Every cast is audible. A whisper is weak and nearly silent; a shout is strong and
  wakes the castle. Nothing else gives you that dial.
- **Pronunciation.** A near-miss does not fizzle — it misfires. A misfired Ignis burns *you*; a
  misfired Frango shatters what you are carrying; a misfired Porta opens the wrong door.
- **The alarm.** It latches at Roused and never decays. The castle locks its doors, then bars them,
  and guards that lose you stop giving up. Letting it max out is not a decision you can take back.

## Verify it

Unity cannot run in CI here, so the gameplay code is compiled and tested headlessly instead:

```bash
./Tools/Headless/verify.sh          # build + run the whole suite
./Tools/Headless/verify.sh --build  # compile only
```

It compiles the same source files Unity compiles and runs the same test files the Unity Test Runner
runs, against a shim of the Unity and PurrNet APIs — nothing is copied. Needs a .NET 8 SDK; the
script prints the install line if it cannot find one. See `Tools/Headless/README.md` for what this
does and does not prove.

## Where things are

| Path | |
| --- | --- |
| `Assets/_Project/Scripts/Runtime/` | all gameplay, one assembly per system |
| `Assets/_Project/Scripts/Tests/` | the test suite (runs in Unity and headlessly) |
| `Assets/_Project/Scripts/Editor/` | scene builders and dev tooling |
| `Tools/Headless/` | the headless build + test harness |
| `docs/4-systems/` | how each system works, its invariants, and its traps |

Start with `docs/4-systems/core.md`, then `raid.md` and `spells.md`.
