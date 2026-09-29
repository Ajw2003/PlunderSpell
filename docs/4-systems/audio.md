# Audio

What plays sound in the game, how it is wired, and what is still silent. The plan it came from is
[`docs/plans/audio-in-game-layer.md`](../plans/audio-in-game-layer.md) (Phase A of
[`docs/plans/audio.md`](../plans/audio.md)); the files it plays are built by
[`Tools/AudioForge/`](../../Tools/AudioForge/README.md). Nobody has listened to the result: every
check below reads what is playing and on which mixer group, and none of it can judge how it sounds.

## What exists

- **Assembly `Plunderspell.Audio`** (`Assets/_Project/Scripts/Runtime/Audio/`). It references the
  assemblies that raise the events it hears (Alarm, Castle, Extraction, Guards, Loot, Player, Raid,
  Spells, Voice, Core, Foundation). None of those reference it, so there is no cycle. The UI
  assembly references it, for the button sounds and the sliders.
- **`Assets/_Project/Audio/Plunderspell.mixer`.** The group tree from `audio.md` section 2: Master,
  then Music, SFX (Spells, Weapons, World, Foley, Creatures, Ambience) and UI. Exposed volumes in
  dB: `MasterVolume`, `MusicVolume`, `SfxVolume`, and `UiVolume`. There are two snapshots, `Default`
  and `Casting`.
- **`Assets/_Project/Audio/SoundBank.asset`.** One entry per manifest name (483), each with its
  clips (`<name>_NN.ogg`), its mixer group (the manifest's `bus` column), 2D or 3D, loop and noise
  class. `Plunderspell > Audio > Rebuild SoundBank` regenerates it from
  `Tools/AudioForge/manifest.csv` and the clips on disk, and builds the mixer first if it is missing.
  It never edits the manifest or a clip. `Plunderspell > Audio > Build Mixer` builds only the mixer,
  and never overwrites one that exists.
- **`AudioDirector`** plays sounds from a pool of 32 `AudioSource`s made once at start. `Play(name,
  position)` picks a random variant, nudges pitch within 5 percent, routes to the entry's group and
  sets the 3D reach from the noise class (15, 20, 30, 45 and 70 metres for none to max). A name with
  no entry logs one warning and returns null. The same sound is not started twice within 40 ms.
- **`MusicDirector`** owns six looping sources: four stems and two beds. Beds cross-fade over 1.5 s;
  stems over 2 s.
- **`AudioLevels`** maps the Settings sliders onto the mixer.

## How it starts

`AudioBootstrapper` runs after the first scene loads, the way `UIBootstrapper` does. It finds the
`SoundBank` with `Resources.FindObjectsOfTypeAll`, which works because the bank is registered in
`ProjectSettings.asset` as a preloaded asset (the rebuild command does that). That keeps the bank
where the plan put it and needs no `Resources` folder. If no bank is loaded the game logs one
warning and is silent.

## Event to sound

"Seen" below means an `AudioSource` was found playing that clip on that group in a Play-mode run
(Play Solo, Lair, Set Out). Seen through the real flow: hover, click, back, the Lair and raid music,
the alarm stinger and the portal-opened stinger. Seen by raising the event from a script rather than
by playing: spell cast, misfire and its sting, fizzle, no mana, hit, player hurt, guard swing and throw
(the guard's `Attacked` event raised by reflection), and loot ruined (`LootValue.Ruin()`). Not seen at
all: door open and close (the castles built in these runs had no `CastleDoor`, so the discovery search
found nothing), the player-death sound, the item-crossing sound, extraction success and the portal
warning bell. Those run the same `Play` path, but nothing has shown them reaching it.

| What happens | Event | Sound | Group |
|---|---|---|---|
| Pointer enters a button | `UIButtonFocus.OnPointerEnter` | `ui_button_hover` | UI |
| Button pressed | listener added in `UIFactory.CreateButton` | `ui_button_click`, or `ui_button_back` when the label holds Back, Close, Cancel or Resume | UI |
| Spell cast resolves | `SpellCastingSystem.CastResolved` | `sfx_spell_<word>_cast` (Somnus: `_cast_soft` or `_cast_loud` by volume) | SFX/Spells |
| Misfire resolves | `CastResolved` with a misfire id | `sfx_spell_<word>_misfire`, and `sting_spell_misfire` | SFX/Spells, Music |
| No word matched | `SpellCastingSystem.PhraseResolved` | `sfx_spell_fizzle` at the listener | SFX/Spells |
| Word refused for mana | `PhraseResolved` | `sfx_spell_no_mana` at the listener | SFX/Spells |
| Any hit that cost health | `Damage.Dealt` | by kind: impact `phys_impact_body`, melee `sfx_wpn_blade_hit_flesh`, projectile `sfx_wpn_xbow_bolt_hit_flesh`, enemy attack `sfx_wpn_blunt_hit_flesh` | SFX/World, SFX/Weapons |
| The local player is hit | `Damage.Dealt` | `sfx_player_hurt`, or `_heavy` at 30 percent of max health or more | SFX/Foley |
| The local player dies | `Damage.Dealt` with `Killed` | `sfx_player_death` | SFX/Foley |
| A guard swings | `CastleGuard.Attacked` (melee) | `sfx_wpn_bronze_swing` in the Bronze Age, otherwise `sfx_wpn_blade_swing` | SFX/Weapons |
| A guard throws or shoots | `CastleGuard.Attacked` (projectile) | `sfx_throw_whoosh_light` | SFX/World |
| A door opens or closes | `CastleDoor.OpenStateChanged` | `sfx_door_wood_open`, `sfx_door_wood_close` | SFX/World |
| A piece of loot is ruined | `LootValue.Ruined` | `phys_break_ceramic` | SFX/World |
| The alarm rises | `AlarmFSMManager.AlarmStateChanged` | `sting_alarm_<stirred, roused, huecry>_<age>` | Music |
| The portal opens | `RaidDirector.PortalOpened` | `sting_portal_opened` | Music |
| Time runs low | `ExtractionZone.TimeRemaining` at 120, 60 and 30 s | `sting_portal_warning`, variant 1, 2, 3 | Music |
| An item enters the extraction zone | `ExtractionZone.HaulInZoneChanged` (count rose) | `sfx_extract_item_cross` | UI |
| Extraction resolves with someone saved | `ExtractionZone.ExtractionResolved` | `sting_extract_success` | Music |

The portal warning is armed only after the timer has been seen above 120 s in a raid, so a stale zero
never rings it. 
## Music

| Game state | Plays |
|---|---|
| Main menu | `mus_title_loop` |
| Lair | `mus_lair_loop` |
| Playing, raid running | the current Age's four stems, `mus_raid_<age>_calm`, `_stirred`, `_roused`, `_huecry` |
| Game over | `mus_results_failure_loop` |
| Victory | `mus_results_success_loop` |
| Paused, Settings, or Playing before the raid exists | whatever was already playing |

The stems are layered, not swapped: calm always plays, stirred joins at Stirred, roused at Roused and
hue and cry at Hue and Cry, and none of them is ever stopped while the raid runs, so a stem is already
in step when its turn comes. They start together on a scheduled time 0.1 s ahead. `MusicDirector.Choose`
is a pure function of the state and is tested.

## Sliders and the Casting dip

Settings Master, Music and Effects are linear 0 to 1 and are saved under the old `PlayerPrefs` keys
(`Settings.MasterVolume`, `MusicVolume`, `SfxVolume`). `AudioLevels` converts to decibels (0 is
minus 80, 1 is 0, 0.5 is about minus 6), applies the saved values when the director starts, and sets
the exposed parameters. Before this layer the saved Master value was not applied at start-up at all;
it is now. With no mixer loaded, Master falls back to `AudioListener.volume`.

Two things differ from the plan, and both came from measuring.

- **The Casting dip is not a snapshot transition.** In Unity 6000.3 a snapshot transition overwrites an
  exposed parameter with the snapshot's stored value. Measured in Play mode: after transitioning to
  `Casting`, `MusicVolume` and `SfxVolume` both read minus 9, whatever the sliders said, and coming
  back would have restored 0 rather than the slider. So `AudioDirector` watches
  `PushToCastController.IsCasting` and `AudioLevels.TickCasting` adds a minus 9 dB offset on top of the
  slider value (in over 0.12 s, out over 0.3 s). Measured with the cast state forced on and off:
  `MusicVolume` and `SfxVolume` went to minus 9 and back to 0, and the sliders were untouched. The
  `Casting` and `Default` snapshots still exist in the mixer, as the plan asked, and nothing uses them.
  Any snapshot added later (Downed, Lair) must change an effect parameter that is not exposed, such as
  a low-pass cutoff, or it will fight the sliders in the same way.
- **There is a fourth exposed parameter, `UiVolume`.** The plan says the UI group follows the Effects
  slider, but it is a sibling of SFX, not a child, so one parameter cannot drive both. `AudioLevels`
  sets `UiVolume` to the same value as `SfxVolume`. The dip does not touch it, as the plan says only
  Music and SFX drop.

## Finding things that raise events

Static events (`CastResolved`, `PhraseResolved`, `Damage.Dealt`, `LootValue.Ruined`) are subscribed in
`OnEnable`. The rest belong to objects that appear later, so `AudioDirector.Discover` runs once a
second and subscribes to any it has not met: the raid director, the alarm, the extraction zone, the
push-to-cast controller, and every guard in `CastleGuard.Active`. Doors have no such list, so while the
raid phase is `Raiding` they are searched every 5 s with `FindObjectsByType`, which allocates one
array each time (never per frame). A door built after a search is heard within 5 s, which is well
inside the time it takes to reach it.

## Traps

- A snapshot transition overwrites exposed parameters. See above.
- `AudioMixerController`, `AudioMixerGroupController` and `AudioGroupParameterPath` are internal to
  `UnityEditor`. `AudioMixerBuilder` reaches them by reflection; a Unity upgrade may rename them, and
  the builder throws a plain message if the type is gone. `AudioParameterPath` itself is abstract:
  the concrete one is `AudioGroupParameterPath(group, guid)`.
- `PlayerSettings.SetPreloadedAssets` is what puts the bank in `ProjectSettings.asset`; the diff is two
  lines. Deleting the bank asset without rebuilding leaves a dangling entry.
- All clips are imported as Streaming (the owner's choice, `audio.md` section 2), so the first play of
  a clip reads from disk. Nobody has measured whether that is audible on a busy frame.

## Not built (the gap list)

Everything the plan put out of scope, and every sound in the M7 set that has no hook yet.

**M7 set, missing a hook**

- **The player's own melee swing and throw.** No event exists for either; the hook would go in
  `Items/Item.cs` or `ItemManager.cs`, and `Item.cs` had someone else's uncommitted changes when this
  was built. Guards swing and throw audibly; the wizard does not.
- **Guard death and guard hurt.** `Damage.Dealt` plays the hit sound, but the death cries are
  `vo_<age>_<role>_death` and guard voices are Phase C.
- **Loot break by material.** `LootValue.Ruined` plays `phys_break_ceramic` for everything. The
  `LootPickup.BreakItem` hook named in `audio.md` section 0 is in a file with someone else's changes,
  and `LootValue` carries no material.
- **Door kind.** Every door plays the wooden open and close; heavy, grate, locked-rattle and unlock
  sounds need the door to say what it is, and `CastleDoor` does not.
- **Your own casts play in 3D at the cast origin**, not in 2D, so they follow the camera closely but are
  not exactly centred. `CastReport` does not say whether the caster is the local player.

**Events found and deliberately not used**

- `CastleGuard.StateChanged` exists at `Guards/CastleGuard.cs:114` but only guard voices would use it.
- `PlayerStateMachine.LocalPlayerDied`, `RangedWeapon.Fired`, `PlayerStateMachine.SlamLanded`,
  `GoldConjured` and `GoldScattered` exist and are unused. `sting_player_down` is built into the names
  class but nothing plays it.

**Out of scope for Phase A, listed so they are not lost**

- Phases B to F of `audio.md`: better library files, AI spell sounds, guard voices, real music, the
  mix pass.
- Footsteps, the grab beam and grab sounds, physics impacts and scrapes, spell travel, impact and
  hold loops, status loops (burning, asleep, levitating), the diegetic alarm loops
  (`amb_alarm_<age>_*`), ambience beds and spot loops, fire loops, the portal drone, reverb zones.
- The `Lair`, `Downed` and `Paused` snapshots, and the heartbeat.
- `sfx_voice_listen_open` and `_close` (needs a hook in `PushToCastController`, and the open cue must
  stay under 80 ms), `sfx_voice_teammate_tell` (#48).
- UI sounds beyond hover, click and back: slider tick, toggles, keybind, screen open and close, pause
  open and close, lobby join and leave, invite, error, denied, the loot highlight and value-lost cues.
- Results-screen tally sounds, `ui_hit_confirm` and `ui_kill_confirm`.
- Bar-line quantised music (Phase E), the Lair-after-loss loop, the credits track, the portal
  narrowing stem, and the `sting_gold_found`, `sting_player_down` and `sting_raid_lost` stingers.
- The combat-bench toggle that cycles every sound in a category.
- Making the wizard's own casts 2D, and scaling loudness by gameplay noise more finely than the
  five reach distances.

## Tests and how it was checked

`Assets/_Project/Scripts/Tests/Editor/AudioLayerTests.cs` (EditMode, 9 tests): every manifest name has
an entry with the manifest's number of clips and a mixer group; every group belongs to the mixer; the
dB mapping; the casting offset and its ramp; the exposed parameters and both snapshots exist; the
pool reuses 8 sources over 200 acquisitions and creates no more; an unknown name logs once and does not
throw; every name an event can return is in the bank; the music plan follows the game state.

Play-mode reads (which `AudioSource`s were playing, on which group) are in
`docs/5-today/Today.md` for the day they were taken. They show the sources, the clips and the groups.
They cannot show how anything sounds, or whether a level is right, and the listening pass is still to
do.
