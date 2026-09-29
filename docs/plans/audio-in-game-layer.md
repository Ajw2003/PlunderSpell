# Plan — the in-game audio layer (Phase A of `audio.md`)

Status: **decided 2026-09-29, being built.** This is §7.3 and Phase A of [`audio.md`](audio.md) made
concrete. The assets exist (`Assets/_Project/Audio/`, 1,013 files, from `Tools/AudioForge/`, merged
into this branch as `84732e04`); no game code plays them. The Settings screen's Music and Effects
sliders store a number and do nothing (`SettingsScreen.cs:175,177`); Master sets
`AudioListener.volume`.

## Scope (what is built)

1. **Assembly `Plunderspell.Audio`** (`Assets/_Project/Scripts/Runtime/Audio/`). It may reference the
   assemblies that raise the events it listens to. Nothing those assemblies contain may reference
   `Plunderspell.Audio` (no cycle). The UI assembly may reference it (sliders).
2. **`AudioMixer`** `Assets/_Project/Audio/Plunderspell.mixer` with the tree in `audio.md` §2: Master →
   Music, SFX (Spells, Weapons, World, Foley, Creatures, Ambience), UI. Exposed parameters in dB:
   `MasterVolume`, `MusicVolume`, `SfxVolume`. Snapshots `Default` and `Casting` (−9 dB on Music and
   SFX). `Lair`, `Downed`, `Paused` are out of scope.
3. **`SoundBank`** (ScriptableObject) `Assets/_Project/Audio/SoundBank.asset`: one entry per manifest
   name with its clips (`<name>_NN.ogg`), mixer group (the manifest's `bus` column), 2D or 3D, loop,
   noise class. An editor command (`Plunderspell > Audio > Rebuild SoundBank`) regenerates it from
   `Tools/AudioForge/manifest.csv` and the clips on disk. It never edits the manifest or a clip.
4. **`AudioDirector`**: a pooled set of `AudioSource`s (no create-and-destroy at play time), started
   the way `UIBootstrapper` starts the UI. `Play(name, position)` picks a random variant, nudges pitch
   within ±5% (`audio.md` §2), routes to the entry's group. It subscribes to events that already exist
   and plays the **M7 acceptance set**: melee swing, throw, hit, spell cast, fizzle, misfire, death,
   door open/close, loot break, alarm state change. Plus the UI button hover, click and back sounds.
   Where an event does not exist, the sound is listed as a gap in `docs/4-systems/audio.md`; the
   gameplay code is not edited to add a hook unless that file is clean in `git status` (see below).
5. **`MusicDirector`**: title loop on the main menu; Lair music in the Lair; in a raid, the current
   Age's stems faded by `AlarmState`; stingers on alarm change and the portal warning. Cross-fades,
   not bar-line quantised (that is Phase E). Where a stem or loop is still a placeholder, play it.
6. **Casting snapshot**: transition to `Casting` when the push-to-cast key is held, back on release
   (`audio.md` §1.3). It hooks into `Voice/PushToCastController`.
7. **Settings sliders** drive the three exposed mixer parameters (linear 0..1 to dB, 0 maps to −80 dB),
   are applied at start from the saved values, and keep the same `PlayerPrefs` keys. `Master` keeps
   working. A slider tick sound on change is not required.
8. **No audio over the network**: every machine plays from events that are already synced.

## Not in scope

Phases B–F of `audio.md` (better library files, AI spell sounds, guard voices, real music, the mix
pass), footsteps, grab beam, physics impacts, guard voices, ambience beds, reverb zones, the `Lair`,
`Downed` and `Paused` snapshots, and the combat-bench cycle toggle. Each is listed as a gap in
`docs/4-systems/audio.md` so it is not lost.

## Hard constraints

- **Do not edit, stage or commit** these files, which have someone else's uncommitted changes:
  `EraContentForge.cs`, `Item.cs`, `LootInteractor.cs`, `LootPickup.cs`, `LootSpawner.cs`,
  `RaidHudPresenter.cs`, `LootGripTests.cs`, `HudAndInteractionTests.cs`, `LootAcousticsTests.cs`,
  `LootSettleAndInputGatingTests.cs`. Stage by explicit path. The loot-break sound therefore hooks an
  existing event or is listed as a gap.
- Before any Unity recompile or test run, eval `EditorApplication.isPlaying`; if true, stop and ask.
  Use `unity command` with `--caller plugin --skill unity-cli`.
- Code style: match the surrounding files (CRLF line endings, Allman braces, `_camelCase` fields).
  `Assets/_Project/Scripts/Tests/` for tests.

## Verify

- Recompile; Console has 0 errors.
- EditMode tests: every manifest name has an entry with at least one clip; the dB mapping (0 to −80,
  1 to 0, 0.5 to about −6); the pool reuses sources and never grows past its size; a name with no
  entry logs once and does not throw.
- In the Editor, Play: Play Solo, Lair, Set Out; confirm through a debug read (which `AudioSource`s
  are `isPlaying`, on which mixer group) that a UI click, an alarm change, and the music state play.
  Nobody can listen from a script; say so, and leave the listening pass to the owner.
- Docs: `docs/4-systems/audio.md`, its row in `docs/4-systems/README.md` and the landing table,
  `audio.md` and `ProjectState.md` status lines, and `docs/5-today/Today.md`.
