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

## Footsteps and movement foley (Phase A2)

`StepAudio` is added at run time by `AudioDirector.Discover` to every player (found every 5 s) and every
guard (from `CastleGuard.Active`), so no prefab was edited. A step is one stride of ground covered:
the stride is 0.9 to 2 m depending on speed (`StepMath.Stride`), so a sprint steps faster and standing
still makes no sound. Loudness is 0.4, 0.7 or 1.0 by pace (under 2.2, under 4.5, faster m/s), the same
three-way split as the crouch, walk and run reach of `FootstepNoiseEmitter`. The plan asked to reuse
`EmitStep` where a character already reports a step; nothing does. `FootstepNoiseEmitter.OnFootstep`
is called only by the free-look playtest controller, and the real player emits no step noise at all, so
the sounds and the noise the guards hear are not yet the same event. That is a gap.

Surface comes from a ray down from the character. A castle room is one floor collider named after its
room (`BronzeLevyBarracks(Clone)`) with one material per pigment, so the room name decides and the
ground outside is recognised by its material (`BaileyEarth`). `SurfaceLookup` keeps a keyword list
(barracks and archive are wood, kitchen and chapel tile, foundry metal, courtyard and yard earth, shed
rushes, cistern water) and defaults to stone. The list is a judgement about what each room's floor is
made of, not read from any data, and it is the first thing to change if a room sounds wrong. Read in a
Bronze Age raid, the steppers found stone, earth, tile, water, metal and rushes under them.

The local player's steps play flat (2D) at 35 percent; everyone else's play in 3D. Guards add
`foley_gear_<linen|leather|mail|plate|bronze_plate>_move` at 40 percent of the step, and the hound
uses `foley_step_hound`. Jump plays when a grounded player rises faster than 2 m/s. A landing plays
`foley_player_land` from 2.5 m/s of fall and `foley_player_land_heavy` from 10; an ordinary jump
lands at about 8 m/s, which the first version called heavy, so the threshold moved. The Velox cast
plays `foley_player_dodge` (the dodge key is gone; Velox is the only dash). Steps use their own pool of
12 sources so a crowd of guards cannot use up the 32 general ones.

## Physics impacts (Phase A2)

`ImpactAudio` listens to `Item.Impacted`, the one hook added to gameplay code: a static event
declared on `Item` and raised on the first line of its `OnCollisionEnter`, before that method's own
early returns. It picks `phys_impact_<material>_<light|heavy>` from the piece's material and speed
(heavy at 3 kg or 6 m/s), plays `phys_impact_body` when the thing it hit has health, and scales
volume by speed up to 8 m/s. Impacts under 1.2 m/s are ignored, and a piece repeats no faster than
every 0.12 s.

Material is read from the piece's name by `LootMaterials` (a keyword list, first match wins, stone
by default), since loot has no material field and the data assets were not edited. A test requires that
every `LootItem` asset in the project matches a keyword, so a new piece added without one fails the
suite. Breaks come from `LootValue.Ruined`, which every break passes through on every peer, so
`LootPickup.BreakItem` did not need touching: glass, wood, book and coin-spill by material, the large
glass break for a mirror, the liquid break for an amphora, and the ceramic crack for metal and stone.

A piece that has just hit something is tracked for 6 s (up to 8 pieces). While it is on the floor and
moving it drives a `LoopBus` slot: `phys_roll_loop` when it spins, otherwise the scrape for its
material. The bus has six pooled sources, one loop per piece, fading in over 0.1 s and out over 0.25 s.

**The `stashing` branch.** The one gameplay edit is two additive hunks in `Item.cs`: the event
declaration after `Mass`, and one raise line at the top of `OnCollisionEnter`. The plan expected a
second hook in `LootPickup.cs`; it was not needed, so that file is untouched. When branch `stashing`
(the owner's loot work, which also changes `Item.cs`) merges, check that the raise line is still at
the top of `OnCollisionEnter` and that the method still takes a `Collision`.

## Guard voices (Phase A2, issue #42)

`GuardVoices.Resolve` maps a guard's prefab name to its Age, voice and armour (16 enemies across the
four Ages; the older prototype prefabs get no voice, as section 3.9 of `audio.md` decided). Lines are
`vo_<age>_<voice>_<line>`; the hound has growl, bark, bite and yelp, plus a panting loop within 12 m
while calm, and one howl from the nearest hound when the alarm reaches Roused.

`GuardVoiceDirector` reads triggers from state every guard already replicates: its alert state, its
attack count and its health. That is deliberate. `CastleGuard.StateChanged` is raised only on the
host (in `EnterState`), so a client would have heard nothing from it. Reading synced state means a
client hears what the host hears, nothing crosses the network, and no guard speaks twice on one
machine. The lines: alert (to Investigating), chase, search, lost (back to patrol from an alerted
state), attack (attack count rose), hurt (health fell), asleep (on falling asleep and every 6 s while
asleep) and murmur. Limits: one line per guard per 2 s; a murmur only from a calm guard, 8 to 20 s
apart and never within 8 s of another line; six voices at once, nearest first (a new line takes a free
voice, else replaces the farthest playing one if that is farther than the new one); nothing beyond 45 m.
Voices play in 3D at the guard's head on the Creatures group.

Death cannot be read from health: `CastleGuard.TakeDamage` destroys the guard the moment health reaches
zero, on every peer. A guard that vanishes while the raid phase is `Raiding` is treated as dead and
speaks its death line from where it last stood. A guard destroyed for any other reason during a raid
would do the same; none is known.

## Finding things that raise events

Static events (`CastResolved`, `PhraseResolved`, `Damage.Dealt`, `LootValue.Ruined`) are subscribed in
`OnEnable`. The rest belong to objects that appear later, so `AudioDirector.Discover` runs once a
second and subscribes to any it has not met: the raid director, the alarm, the extraction zone, the
push-to-cast controller, and every guard in `CastleGuard.Active`. Doors have no such list, so while the
raid phase is `Raiding` they are searched every 5 s with `FindObjectsByType`, which allocates one
array each time (never per frame). A door built after a search is heard within 5 s, which is well
inside the time it takes to reach it.

## Reduced sound set (playtest)

What plays is set by hand in **`Assets/_Project/Resources/SoundFocusSettings.asset`**: select it in
the Project window and edit it in the Inspector. Changes apply at once, in Play mode too (checked
2026-09-30: ticking Music flipped `mus_title_loop` from muted to playing on the next call, no reload).

- **Filter On**: untick to hear every sound; the lists are then ignored.
- **Groups**: each has a label, a **Play** tick box and name prefixes (`sfx_spell_`, `phys_`, ...). The
  group with the longest matching prefix decides, so a narrower group can carve a piece out of a
  wider one.
- **Overrides**: exact sound names that ignore their group (the five spell sounds still waiting on a
  replacement are here, muted).
- **Play Everything Else**: for a sound no group or override names.

As shipped (the owner's ticks, 2026-09-30): footsteps and movement, guard and hound voices, guard
sounds, weapons, UI, spells, music, player, portal and loot, and hazards play; physics, ambience,
stingers and castle are muted, and three spell sounds waiting on round two are muted by override.
The code's defaults (`SoundFocusSettings.cs`, used only for a fresh asset) match. Physics was muted because its tone read wrong (leather on stone sounded like metal); the
triggers are unchanged and wait for replacement files. `SoundFocus` (`Assets/_Project/Scripts/Runtime/Audio/SoundFocus.cs`)
reads the asset from Resources; with the asset missing, everything plays and one warning is logged.
The check sits in `AudioDirector.Play` (after the bank lookup, so a misspelt name is still reported),
`LoopBus.Drive` and `MusicDirector.StartLayer`. Code can switch the filter off for a run without
touching the asset (`SoundFocus.Enabled = false`, undone by `SoundFocus.ClearOverride()`); the tests
and `AudioLatencyProbe` do. Tests: `SoundFocusTests` (the rules on a settings object of their own,
and the shipped asset loading).

## Latency (measured 2026-09-30)

The owner heard a delay on physics sounds. `AudioLatencyProbe` (a diagnostic, attached from an eval)
measured, in Play from `RaidScene`, solo host:

- play call to sound in the listener output: 0-22 ms for UI, footsteps, hound howl, weapon swing,
  all imported as Streaming;
- a BronzeSword dropped 1.5 m: visible mesh down at 553 ms, collision at 559 ms, sound in the output
  at 578 ms, so the sound lands 25 ms after the picture;
- the phys_ files start at once (impacts peak within 30 ms), except the scrape and roll loops, which
  swell for 0.5-2.3 s before their loudest part.

So no delay was found in Unity for a solo drop.

Co-op, the normal case (`Tools/Unity/coop_drop_latency.sh`: Editor hosts, the Development build
joins, the host lifts the piece nearest its camera 2 m and drops it, both sides log), two runs:

| Side | Piece | Visible mesh down | Collision on this machine | Sound starts |
|---|---|---|---|---|
| host | FaienceHippopotamus | 638 ms | 634 ms | 644 ms |
| client | FaienceHippopotamus | 584 ms after its fall began there | never | 404 ms (`phys_break_ceramic_01`) |
| host | OxhideIngot | 642 ms | 640 ms | 642 ms |
| client | OxhideIngot | 584 ms | never | never |

- On the host, sound and picture are within 10 ms.
- Before the fix, a client raised no collision for a piece it does not simulate, so impact sounds
  never played there, and a break arrived about 180 ms before the client's interpolated picture.

**Fix (2026-09-30).** The machine simulating a piece sends each audible impact (speed 1.2 m/s and
up, 0.12 s apart, the same floor and guard as `ImpactAudio`) through the server to everyone else
(`LootPickup.ShareImpact` → `ImpactObservers`), who raise `Item.ImpactedRemotely`; `ImpactAudio`
plays it like its own. A client draws the piece `NetworkTransform.ticksBehind` ticks behind, so it
holds a relayed impact, and applies a break (hiding the piece, its sound), that long
(`LootPickup.ReplicationDelay`). The script now stages one piece per scenario in front of the host
camera (a never-breaking piece for impact, one a 2 m drop breaks), one session. After the fix:

| Side | Scenario | Visible mesh down | Sound starts |
|---|---|---|---|
| host | impact, OxhideIngot | 639 ms | 646 ms |
| client | impact, OxhideIngot | 560 ms | 528 ms |
| host | break, FaienceHippopotamus | 648 ms | 674 ms |
| client | break, FaienceHippopotamus | 557 ms | 554 ms (impact and break together) |

Scrape and roll loops still play only on the simulating machine: a client's copy of the piece has no
velocity of its own to read.
- Unity's own output path is 0-22 ms. Anything later than that happens after Unity: the Windows
  device (Bluetooth headphones typically add 150-300 ms; a TV's HDMI audio adds its own). Not
  measured: that needs a microphone recording the speaker.

## Spell sounds from the owner's picks (2026-09-30)

The owner auditioned six p0ss candidates per spell sound on the review page and exported
`docs/generated/audio-review/review-decisions-2026-09-30.json`. Some picks were written as notes, not
Keep clicks; `review-decisions-2026-09-30-applied.json` is the file `apply` read, with those four added
(no_mana reuses fizzle's `sand.ogg`; saltus cast and launch use `spell.ogg`; porta misfire uses
`enchant.ogg`). 27 sounds, 41 files, now built from the pack (`Tools/AudioForge/picks.csv`).

- Porta cast, open and loop keep their current sounds (owner's choice).
- Ignis cast and the fireball's flight use the owner's own `IngnisBall.wav` (library root
  `ayden-own`; `docs/generated/audio-review/ingnisball-2026-09-30.json`). The spell category's length
  cap trims the cast to about 1.3 s; the flight loop is 0.86 s.
- Round two (`docs/generated/audio-review/index.html`, built with `review --candidates
  round2-candidates.json --exclude-rejected review-decisions-2026-09-30.json`): `sfx_spell_ignis_impact`
  (all six pack explosions, three also pitched down), `sfx_spell_levo_release` (the earlier candidates
  pitched down 20-45 % for bass), `sfx_spell_levo_misfire` (six not yet rejected). Those three stay
  muted by overrides in `SoundFocusSettings.asset` until picked. Hand-picked candidates are rendered
  to `docs/generated/audio-review/previews/` so a pitch shift is heard as the build makes it.
- `audioforge.py build "sfx_spell_*"` also re-rendered 10 files nobody picked (the build changed since
  they were made); those were restored from git, so unpicked sounds are exactly as before.

## The listener

In the raid scene the player's listener lives on `PlayerCamera`, which only exists once PurrNet has
spawned this machine's body and handed it ownership (`PlayerNetworkOwnership.ApplyOwnership`). Until
then the scene had no listener: Unity logged "There are no audio listeners in the scene" every frame
(193,766 lines in one Editor log) and nothing was heard until the player spawned.

`RaidDirector.Awake` now creates `RaidFallbackListener` (`Assets/_Project/Scripts/Runtime/Raid/RaidListenerFallback.cs`).
Every `LateUpdate` it switches its own listener on exactly while no enabled camera carries an enabled
listener, so the raid always has one listener and never two. It counts only listeners on cameras,
using `Camera.GetAllCameras` into a fixed buffer (no allocation per frame); every other raid listener
(player camera, spectator camera) sits on a camera. A listener added anywhere else would not be seen
and would give two.

`AudioDirector.Discover` drops its cached listener once that listener is switched off and picks the
first enabled one, so distance checks follow the player's camera after spawn instead of staying on the
fallback.

Checked 2026-09-29: Play from `RaidScene`, host, set out. Zero "no audio listeners" lines in the log
across menu, Lair and raid; every sample showed one enabled listener (`RaidFallbackListener` before
spawn, `Eye` after); `AudioDirector` held `Eye`. PlayMode test: `RaidListenerFallbackTests`.

## Output device

Unity 6000.3 has no API to list or pick an output device (checked `UnityEngine.AudioModule.xml`); it
plays to the Windows default. On 2026-09-29 that default was a TV's HDMI output while the owner wore
headphones, so the game sounded silent while Unity was playing normally.

`AudioOutputDevices` (`Assets/_Project/Scripts/Runtime/Audio/AudioOutputDevices.cs`) routes the game
through Windows' own per-app output (the Volume mixer setting EarTrumpet also uses):

- Devices come from Core Audio (`IMMDeviceEnumerator`, active render endpoints).
- The route is set with the undocumented `Windows.Media.Internal.AudioPolicyConfig` factory,
  `SetPersistedDefaultAudioEndpoint` for this process id, console and multimedia roles. The factory's
  interface id changed in Windows 10 21H2; both ids are tried. The methods are called by vtable slot
  (25 set, 26 get), so a Windows update that reorders the interface breaks this, and the warning
  "Windows refused the output route" or "not available here" says so. Empty id clears the route.
- Windows persists the route per executable path. In the Editor that is `Unity.exe`, so a route chosen
  in Play stays on the Editor after Play stops until "Windows default" is picked again.
- After routing, `AudioSettings.Reset` reopens Unity's output on the new device. Reset stops every
  source, so looping sources that were playing (music layers, fires, loops) are restarted at their
  sample position; one-shots in flight are cut.
- The choice is saved in PlayerPrefs `Settings.OutputDevice`; `AudioBootstrapper` re-applies it at
  startup when Windows' route disagrees and the device is plugged in.
- Settings > Sound > Output steps through Windows default and each device.

Checked 2026-09-29 in Play, with `Tools/Audio/audio_sessions.ps1` (lists devices and which apps play
to each): picking BlackShark moved the Unity session from HISENSE to BlackShark with music still
playing; Windows default moved it back; a saved choice was re-applied at startup. Screens:
`docs/generated/audio/settings-output-row*.png`. The reset also stops the voice microphone (checked in a raid:
recording before, not after). The next cast key press reopens it through
`VoskVoiceInputService.OpenMicrophone` (checked: recording again, 5760 samples in 0.4 s), so the only
cost is that first press paying the open again. Not checked: a built player (needs a Development build).

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
  `Items/Item.cs` or `ItemManager.cs`. Guards swing and throw audibly; the wizard does not.
- **Door kind.** Every door plays the wooden open and close; heavy, grate, locked-rattle and unlock
  sounds need the door to say what it is, and `CastleDoor` does not.
- **Your own casts play in 3D at the cast origin**, not in 2D, so they follow the camera closely but are
  not exactly centred. `CastReport` does not say whether the caster is the local player.

**Events found and deliberately not used**

- `CastleGuard.StateChanged` exists at `Guards/CastleGuard.cs:114` but is raised only on the host;
  guard voices read the replicated state instead.
- `PlayerStateMachine.LocalPlayerDied`, `RangedWeapon.Fired`, `PlayerStateMachine.SlamLanded`,
  `GoldConjured` and `GoldScattered` exist and are unused. `sting_player_down` is built into the names
  class but nothing plays it.

**Out of scope for Phase A, listed so they are not lost**

- Phases B to F of `audio.md`: better library files, AI spell sounds, guard voices, real music, the
  mix pass.
- The grab beam and grab sounds, spell travel, impact and hold loops, status loops (burning, asleep, levitating), the diegetic alarm loops
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
**Phase A2 gaps**

- The wizard's steps are not the noise the guards hear (see Footsteps); the real player never calls
  `FootstepNoiseEmitter`.
- Guard `grabbed` and `thrown` lines and the hound's `whimper_grabbed`: nothing in the code can grab or
  throw a guard, so there is no event.
- The gear layer on body movement (`foley_robe_move` and gear sounds while not stepping).
- Scrape and roll only start after a piece's first impact, so a piece dragged from rest before it has
  hit anything is silent until it bumps something. Pieces dragged by the grab beam are not detected as
  dragged at all.
- Cloth, book and coin impacts, and the ceramic and glass impacts, have one weight only, so heavy and
  light are the same for them.
- The surface list is a guess from room names; ledges, stairs and thresholds inside a room take the
  room's surface.
- A remote player's steps and jumps use the same code as a guard's; no run had a second player, so that
  path was not seen.
- The hound has not been heard in a run (the Bronze Age raid has none); its lines are tested by name only.

**Still open from Phase A**

- Making the wizard's own casts 2D, and scaling loudness by gameplay noise more finely than the
  five reach distances.

## Tests and how it was checked

`Assets/_Project/Scripts/Tests/Editor/AudioFeelTests.cs` (EditMode, 10 tests): the surface lookup and
that every step, jump, land and dodge name is in the bank; stride, loudness and landing rules; every
`LootItem` asset has a known material and every impact, break, scrape and roll name is in the bank; the
16 roster enemies each resolve to a voice and every line and gear name is in the bank; `(Clone)` and
prototype names; the line chosen for each change of alert state; and the loop bus giving one slot per
key and never more than its size.

`Assets/_Project/Scripts/Tests/Editor/AudioLayerTests.cs` (EditMode, 9 tests): every manifest name has
an entry with the manifest's number of clips and a mixer group; every group belongs to the mixer; the
dB mapping; the casting offset and its ramp; the exposed parameters and both snapshots exist; the
pool reuses 8 sources over 200 acquisitions and creates no more; an unknown name logs once and does not
throw; every name an event can return is in the bank; the music plan follows the game state.

Play-mode reads (which `AudioSource`s were playing, on which group) are in
`docs/5-today/Today.md` for the day they were taken. They show the sources, the clips and the groups.
They cannot show how anything sounds, or whether a level is right, and the listening pass is still to
do.
