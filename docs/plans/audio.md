# Plan — Sound and music

Status: **approved and in progress** (2026-09-27; decisions in §8). Phase A's asset side is built:
`Tools/AudioForge/` makes every sound below (1,013 files, placeholders where the real source isn't
in yet) into `Assets/_Project/Audio/`. Its [README](../../Tools/AudioForge/README.md) is the live
status. *2026-09-29:* the in-game layer (§7.3) is now partly built, as decided in
[`audio-in-game-layer.md`](audio-in-game-layer.md): mixer, SoundBank, `AudioDirector`,
`MusicDirector`, the Casting dip and the Settings sliders. What plays, and everything that does not
yet, is in [`docs/4-systems/audio.md`](../4-systems/audio.md). Nobody has listened to it.

Every sound effect and music track Plunderspell needs, each with a name, what makes it play, and
where the file will come from. The second half covers how to generate or source them, in what
order, and the decisions the user needs to make first.

## 0. Where audio stands today

- **The game is completely silent.** There are no `.wav`/`.ogg`/`.mp3` files anywhere under `Assets/`. The only
  audio code is microphone capture (`Voice/VoskVoiceInputService.cs:59`) and an unused footstep
  `AudioSource` field (`Acoustics/FootstepNoiseEmitter.cs:53`). `docs/3-state/ProjectState.md:179`
  says the same.
- **Existing issues this plan covers:** #22 (all SFX/VFX + mixer), #42 (guard voices per awareness state),
  #48 (teammates hear a cast before it lands), #56 (Master/Music/SFX sliders). The Roadmap's M7
  acceptance (`docs/2-roadmap/Roadmap.md:213`) is the target: "every swing, throw, hit, cast and death
  has both a visible and an audible response".
- **The events audio needs already exist.** Almost every sound below can be driven by an event the game already fires, so
  audio does not need to reach into gameplay code:
  `AlarmFSMManager.AlarmStateChanged` (`Alarm/AlarmFSMManager.cs:72`),
  `CastleGuard.StateChanged` / `.Attacked` (`Guards/CastleGuard.cs:114,120`),
  `SpellCastingSystem.PhraseResolved` / `.CastResolved` (`Spells/SpellCastingSystem.cs:458,461`),
  `CastleDoor.OpenStateChanged` (`Castle/CastleDoor.cs:55`),
  `ExtractionZone.HaulInZoneChanged` / `.ExtractionResolved` (`Extraction/ExtractionZone.cs:64,71`),
  `LootPickup.BreakItem` (`Loot/LootPickup.cs:225`, next to the `_brokenVfx` hook).
- **This container can't generate audio yet.** It has no numpy, ffmpeg, sox or fluidsynth (checked
  2026-09-27). The generator in §7.2 has to install them first.

## 1. Rules the whole audio set follows

1. **What you hear matches what the guards hear.** Every sound that also sends a gameplay noise to
   the guards (`NoiseBroadcaster` / `AcousticEmitter`) plays at a loudness that scales with the same
   noise strength. Players then learn by ear what wakes the household: the flanged mace really is
   the loudest thing that isn't on fire. The **Noise** column below shows each sound's gameplay
   noise (`none`/`low`/`mid`/`high`/`max`, using the pitch's weapon-table scale).
2. **The wizards have no recorded voice.** The player's own voice, spoken into the mic, is the only
   voice a wizard has. Recorded grunts or pain cries would talk over it. Player sounds are foley
   only (cloth, bodies, footsteps, breath at most).
3. **Game audio mustn't be picked up by the mic.** Through speakers, game audio would be recorded
   while the cast key is held, and Vosk could mis-hear it. So while the cast key is held, a
   `Casting` mixer snapshot turns SFX and music down by about 9 dB, and the listen-open cue lasts
   under 80 ms so it's over before the player speaks. Headphones stay the recommended setup
   (say so in Settings).
4. **Gold has its own sound.** The pitch keeps orpiment yellow for money alone. Sound does the same:
   only gold and coin get the bright bell-metal "gold" timbre. That makes a coin chime as easy to
   read as the yellow glint.
5. **Each Age has its own instruments.** Every Age has a fixed set of instruments (§5.1). Music,
   stingers and the diegetic alarm (heard by characters in the game, not just the player) all use
   that set, so a Bronze Age raid never sounds like a Powder one.
6. **No audio goes over the network.** Each machine plays its own sounds from events that are
   already synced. This keeps the rule from `Decisions.md:441` that no audio ever leaves the
   machine.

## 2. Naming, files, and mixer

**Name pattern:** `<category>_<system>_<subject>_<action>` in lower snake case. Variants are numbered
`_01`, `_02`, and so on. The **×** column says how many variants to make; the game picks one at random
and nudges its pitch ±5% so repeats don't sound identical. Age names are written `bronze` / `high` /
`late` / `powder`.

| Prefix | Meaning | Plays as |
|---|---|---|
| `ui_` | menu and HUD feedback | 2D, UI bus |
| `sfx_` | gameplay events (spells, weapons, world) | 3D (2D for your own casts), SFX bus |
| `foley_` | bodies and objects moving (steps, cloth, armour) | 3D, SFX bus |
| `phys_` | physics impacts, scrapes, breaks | 3D, SFX bus, chosen by impact speed |
| `vo_` | enemy voices | 3D, SFX/Creatures bus |
| `amb_` | ambience beds and spot loops | 2D beds / 3D spots, Ambience bus |
| `mus_` | music (loops and stems) | 2D, Music bus |
| `sting_` | short musical cues on game events | 2D, Music bus |

**Folders:** `Assets/_Project/Audio/{UI,SFX/<system>,Foley,Physics,VO/<age>,Ambience,Music,Stingers}`.
Source masters (48 kHz / 24-bit WAV, DAW projects, generator scripts, prompt logs) are committed
next to the tool that made them (`Tools/AudioForge/`). Under the repo's commit-everything rule,
nothing is thrown away.

**Import settings:** mono for 3D sounds, stereo for music and 2D beds. Short SFX: *Decompress On Load*,
ADPCM. Loops over 10 s and all music: *Streaming*, Vorbis quality 0.6. Loudness targets: music −18
LUFS integrated, ambience beds −26 LUFS, SFX peaks at −1 dBTP. The final mix is set by ear in the M7
pass.

*2026-09-27: the committed `.meta` files set every clip to Streaming (the owner's choice), with
Normalize off. The build now bakes a level per category into each file rather than a flat −1 dBFS
peak (Decisions, "Audio levels are baked per category"); `Tools/AudioForge/forge/categories.py`.*

**Mixer (the `AudioMixer` from #22):**

```
Master
├── Music          ← #56 "Music" slider
├── SFX            ← #56 "SFX" slider
│   ├── Spells
│   ├── Weapons
│   ├── World      (physics, doors, fire, hazards)
│   ├── Foley
│   ├── Creatures  (vo_ and hound)
│   └── Ambience
└── UI             (follows the SFX slider)
```

Snapshots: `Default`, `Lair`, `Casting` (−9 dB on Music and SFX while the cast key is held; §1.3),
`Downed` (muffled low-pass plus the heartbeat), `Paused` (solo only; pausing multiplayer never stops
the game or its sound, per `Decisions.md:845`). Echo comes from Unity `AudioReverbZone`s set per room
size, not baked into the files, so one door slam works in the Chapel and in a corridor.

## 3. Sound effects: the full list

**Src** is where each file comes from (see §7): **G** = generated by the in-repo synth, **L** = CC0 or
royalty-free library, **A** = AI text-to-sound, **R** = recorded (foley or voice), **C** = composed.
The letter before the slash is the recommended source; the one after is the fallback.

### 3.1 UI and menus — `UI/Screens/*`, `UIFactory.cs`

Look and feel: vellum and ink. Paper, quill and wax-seal sounds, never sci-fi beeps.

| Name | × | Plays when | Brief | Src |
|---|---|---|---|---|
| `ui_button_hover` | 3 | pointer enters a button | a dry fingertip on vellum | G/L |
| `ui_button_click` | 3 | button pressed | a quill tap on a wooden desk | G/L |
| `ui_button_back` | 1 | back / cancel | a page flipped backwards | L |
| `ui_button_denied` | 1 | disabled button pressed | a dull wooden knock | G |
| `ui_screen_open` | 2 | a screen opens | a page turning | L |
| `ui_screen_close` | 1 | a screen closes | a book closing softly | L |
| `ui_slider_tick` | 1 | slider moves one step | a small ratchet click | G |
| `ui_toggle_on` / `ui_toggle_off` | 1+1 | toggle | a latch opening / closing | L |
| `ui_keybind_listen` | 1 | waiting for a keybind | a quill dipped in ink | L |
| `ui_keybind_set` | 1 | keybind accepted | a wax seal pressed | L |
| `ui_error` | 1 | invalid action | torn parchment | L |
| `ui_pause_open` / `ui_pause_close` | 1+1 | pause menu | a heavy curtain drawn | L |
| `ui_lobby_join` | 1 | a friend joins | a small handbell, rising | G |
| `ui_lobby_leave` | 1 | a friend leaves | the same handbell, falling | G |
| `ui_invite_received` | 1 | Steam invite arrives | a knock on a wooden door, twice | L/R |
| `ui_ready` | 1 | a player readies up | a tankard set down | L |
| `ui_mic_level_peak` | 0 | (none: the mic meter is visual-only, so no sound can bleed into the mic) | — | — |

### 3.2 Voice casting — `Voice/*`, `SpellCastingSystem.cs`

| Name | × | Plays when | Brief | Src | Noise |
|---|---|---|---|---|---|
| `sfx_voice_listen_open` | 1 | cast key pressed (local only) | under 80 ms: a soft intake of air plus a faint lapis shimmer | G | none |
| `sfx_voice_listen_close` | 1 | cast key released (local only) | the shimmer cut off | G | none |
| `sfx_voice_teammate_tell` | 3 | a teammate is mid-cast (#48), 3D at them | a rising whispered lapis hum ~300 ms; the "half a heartbeat" warning | A/G | none |
| `sfx_spell_fizzle` | 3 | no word matched (`SpellCastingSystem.cs:229`) | a wet match; a puff of dust | A/G | low |
| `sfx_spell_no_mana` | 1 | not enough mana | a dry cough of sparks | G | none |
| `sfx_spell_mana_restored` | 1 | mana back to full | a soft verdigris chime (local) | G | none |
| `sting_spell_misfire` | 3 | any misfire, played over its own sound | a crumhorn "blat", the comic twin reveal | C/A | — |

### 3.3 Spells — `Spells/Effects/PrimarySpellEffects.cs`, `MisfireSpellEffects.cs`

The eight words that are actually built. Every spell sound sits on the lapis layer: a shimmer that
tells players "that was the voice". Your own casts play 2D; everyone else's play 3D.

| Name | × | Plays when | Brief | Src | Noise |
|---|---|---|---|---|---|
| `sfx_spell_ignis_cast` | 3 | IGNIS released | a hard spit of ember, 180 ms | A | low |
| `sfx_spell_ignis_travel_loop` | 1 | the ember bolt in flight | a small hiss and flutter | A/G | low |
| `sfx_spell_ignis_impact` | 3 | bolt hits | a flame pop and crackle | A/L | mid |
| `sfx_spell_ignis_misfire` | 2 | beard catches fire | a *whumph* up close, then `sfx_status_burning_loop` | A | mid |
| `sfx_spell_frango_cast` | 2 | FRANGO | a plosive crack, like a knuckle through stone | A | mid |
| `sfx_spell_frango_shatter_stone` | 3 | masonry breaks | stone splitting, then rubble | L/A | high |
| `sfx_spell_frango_shatter_wood` | 2 | a door or furniture breaks | timber splintering | L | high |
| `sfx_spell_frango_shatter_metal` | 2 | a lock, bars or iron break | an iron snap and a ringing tail | L/A | high |
| `sfx_spell_frango_misfire` | 1 | your carried loot shatters | the crack right in your hands, then the loot's own `phys_break_*` | A | high |
| `sfx_spell_levo_cast` | 2 | LEVO | an open, rising breath-tone | A/G | low |
| `sfx_spell_levo_hold_loop` | 1 | holding an object up | a hum whose pitch follows the object's mass (heavy = low) | G | none |
| `sfx_spell_levo_release` | 1 | letting go | the hum drops away | G | none |
| `sfx_spell_levo_misfire` | 1 | caster floats off | a wobbling, rising whine, then a loop until you land | G/A | low |
| `sfx_spell_aurumvoco_cast` | 1 | AURUM VOCO | a two-beat chord of distant bells, the gold timbre (§1.4) | A/C | mid |
| `sfx_spell_aurumvoco_reveal_loop` | 1 | the 4 s gold-sight | coins "singing" through the walls, placed at each loot item | G/A | none |
| `sfx_spell_aurumvoco_end` | 1 | gold-sight ends | the bells fading out | G | none |
| `sfx_spell_aurumvoco_misfire` | 2 | the gold scatters | a coin burst plus a shrill metallic "scream" | A | max |
| `sfx_spell_velox_cast` | 1 | VELOX | a snap of air | A | low |
| `sfx_spell_velox_dash` | 2 | the dash | a whoosh past the ear | L/A | low |
| `sfx_spell_velox_misfire` | 1 | dashing the wrong way | the same whoosh, then a thud or stumble | A | mid |
| `sfx_spell_saltus_cast` | 1 | SALTUS | a coiled spring | A/G | low |
| `sfx_spell_saltus_launch` | 1 | leaving the ground | a rising whoosh | L | low |
| `sfx_spell_saltus_land_slam` | 2 | slam landing | a flagstone boom and dust | L/A | high |
| `sfx_spell_saltus_misfire` | 1 | a feeble hop and stagger | a sad little "boing", then a stumble | A/G | low |
| `sfx_spell_somnus_cast_soft` | 2 | SOMNUS whispered | a breathy lullaby exhale | A | none |
| `sfx_spell_somnus_cast_loud` | 1 | SOMNUS bellowed (it wakes the room) | a harsh blare over the same exhale | A | high |
| `sfx_spell_somnus_apply` | 2 | a target falls asleep | a soft down-pitched sigh | A/G | none |
| `sfx_spell_somnus_misfire` | 1 | the caster falls asleep | a sigh, then a thump | A | low |
| `sfx_spell_porta_cast` | 1 | PORTA | a final, heavy word-tone: a lapis tear | A/C | high |
| `sfx_spell_porta_open` | 1 | the way out rips open | fabric of time tearing, a low roar | A | high |
| `sfx_spell_porta_loop` | 1 | the torn portal stays open | a lapis drone (reuses `amb_portal_loop`, brighter) | G | none |
| `sfx_spell_porta_misfire` | 1 | the wrong door opens far away | a distant door slams, then a questioning echo | L/A | mid |

**Pitch spells not built yet.** Add them to this table when their effect class exists:
`TONITRUS` (cast / shove boom / misfire: every light dies with a whoomp) and `CADAVER SURGE`
(cast / the dead rising / porter-zombie shuffle loop / disobedient misfire). The remaining ~30
`SpellId` values (`Spells/SpellId.cs`) have no effect class and get no sound until they do.

### 3.4 Status effects — `Status/StatusEffectReceiver.cs`

| Name | × | Plays when | Brief | Src | Noise |
|---|---|---|---|---|---|
| `sfx_status_burning_loop` | 1 | anyone on fire | crackle close to cloth | L | low |
| `sfx_status_burning_out` | 1 | the fire goes out | a hiss and smoulder | L | none |
| `sfx_status_asleep_player_loop` | 2 | a sleeping wizard | soft breathing (a sound effect, not the player's voice) | R/A | low |
| `sfx_status_stagger` | 2 | stunned or staggered | a ringing-ear whine, local only | G | none |
| `sfx_status_levitating_loop` | 1 | floating out of control | the Levo misfire loop | G | none |

### 3.5 The wizard — `Player/States/*`, `FootstepNoiseEmitter.cs`

Footsteps come in two layers. The **surface** layer is shared with every enemy. The **gear** layer
depends on what the body is wearing (§3.9).

| Name | × | Plays when | Brief | Src | Noise |
|---|---|---|---|---|---|
| `foley_step_stone` | 6 | step on flagstone | soft shoe on stone | L | low |
| `foley_step_wood` | 6 | step on floorboards / stairs | a hollow creak | L | low |
| `foley_step_earth` | 6 | step on beaten clay or dirt (Bronze floors, the bailey) | a dull pat | L | low |
| `foley_step_rushes` | 6 | step on straw-strewn floors | a dry rustle | L/R | low |
| `foley_step_tile` | 6 | step on glazed tile or marble (Powder galleries) | a sharper tick | L | low |
| `foley_step_metal` | 4 | step on grates or trapdoors | a clank | L | mid |
| `foley_step_water` | 4 | step in puddles (crypt, wet bailey) | a splash | L | low |
| `foley_robe_move` | 6 | walk and run (the wizard's gear layer) | wool robe swish | L/R | none |
| `foley_player_jump` | 2 | jump | cloth whip and push-off | L | low |
| `foley_player_land` | 3 | landing | a light thump, by surface | L | low |
| `foley_player_land_heavy` | 2 | landing while carrying, or from high up | a thud and grunt of gear (not the voice) | L | mid |
| `foley_player_dodge` | 3 | dodge | a quick cloth whoosh and scuff | L | low |
| `sfx_player_hurt` | 4 | taking damage | a body thump plus a cloth hit | L | low |
| `sfx_player_hurt_heavy` | 2 | a big hit | a heavier impact, screen-shake partner | L | mid |
| `sfx_player_heartbeat_loop` | 1 | low health (local) | a slow heartbeat that speeds up | G/L | none |
| `sfx_player_downed` | 1 | a wizard goes down | the body collapses, plus `sting_player_down` | L | mid |
| `sfx_player_death` | 1 | dead for good in this raid | a collapse, then a single low bell | L+C | mid |
| `sfx_player_carried_loop` | 1 | a downed friend being carried (12 stone) | limp-body cloth drag and creak | L/R | low |

### 3.6 Grabbing, carrying, throwing — `Items/GrabBeam.cs`, `Items/Item.cs`, `ItemManager.cs`

| Name | × | Plays when | Brief | Src | Noise |
|---|---|---|---|---|---|
| `sfx_grab_beam_start` | 1 | grab beam reaches out | a verdigris zip | G | none |
| `sfx_grab_beam_loop` | 1 | holding something | a quiet hum; strains as bulk goes up | G | none |
| `sfx_grab_beam_release` | 1 | letting go | a soft snap | G | none |
| `sfx_grab_pickup_light` | 3 | lifting 0–3 stone | a quick lift | L | none |
| `sfx_grab_pickup_heavy` | 3 | lifting 4 stone or more | scrape and heave | L | low |
| `sfx_carry_strain_loop` | 1 | a two-person carry (altarpiece, 14 st) | wood-and-gilt creaks and groans | L/R | low |
| `sfx_throw_whoosh_light` | 3 | light throw | a short whoosh | L | none |
| `sfx_throw_whoosh_heavy` | 3 | heavy throw | a slow, deep whoosh | L | low |
| `sfx_creature_grabbed` | — | a living thing picked up (see `vo_*_grabbed`) | — | — | — |

### 3.7 Physics: impacts, scrapes, breaks — `Item.cs` impact path, `LootPickup.BreakItem`

Impacts are chosen by **material × impact speed** (light below a threshold, heavy above it), and the
noise sent to the guards scales with speed. This one table covers every thrown or dropped object,
including loot and weapons.

| Name | × | Material | Src | Noise |
|---|---|---|---|---|
| `phys_impact_stone_light` / `_heavy` | 4+3 | stone, pottery on stone | L | low / mid |
| `phys_impact_wood_light` / `_heavy` | 4+3 | furniture, doors, chests | L | low / mid |
| `phys_impact_metal_light` / `_heavy` | 4+3 | iron, steel, armour | L | mid / high |
| `phys_impact_bronze_light` / `_heavy` | 3+3 | bronze (ingots, cauldron: a bell-like ring) | L/A | mid / high |
| `phys_impact_gold_light` / `_heavy` | 3+2 | gold and silver-gilt (the gold timbre, §1.4) | L/A | mid / high |
| `phys_impact_ceramic_light` | 4 | faience, terracotta (hits it survives) | L | low |
| `phys_impact_glass_light` | 3 | glass and mirror (hits it survives) | L | low |
| `phys_impact_cloth` | 3 | tapestry, bodies of cloth | L | none |
| `phys_impact_book` | 3 | psalter, ledger | L | low |
| `phys_impact_body` | 4 | a thrown guard or friend | L | mid |
| `phys_impact_coins` | 3 | coin coffer, coin piles | L/A | mid |
| `phys_scrape_stone_loop` | 1 | dragging across stone | L | low |
| `phys_scrape_wood_loop` | 1 | dragging across wood | L | low |
| `phys_scrape_metal_loop` | 1 | dragging metal | L | mid |
| `phys_roll_loop` | 1 | round things rolling (amphora, grenado, tureen lid) | L | low |
| `phys_break_ceramic` | 3 | amphora, pithos, faience shattering | L | high |
| `phys_break_glass` | 3 | small glass: reliquary crystal, nautilus cup, glassware | L | high |
| `phys_break_glass_large` | 2 | the Venetian Mirror: a big, long shatter | L/A | max |
| `phys_break_wood` | 3 | cabinet, altarpiece, chest | L | high |
| `phys_break_book` | 1 | a psalter torn apart | L/R | low |
| `phys_break_liquid` | 2 | contents spill (amphora wine, ewer water), layered over the break | L | low |
| `phys_coin_spill` | 3 | coins scattering | L/A | high |
| `sfx_loot_value_lost` | 2 | loot breaks and its value is gone | a coin-drain "tink-tink-tink" dropping away (the "you know to the penny what just left the room" cue) | G/C | none |

**Which material each loot item uses** (from `docs/art/data/*.json`). No loot item needs a sound of its own:

| Age | Item (bulk, st) | Impact / break set |
|---|---|---|
| Bronze | Oxhide Ingot (4) | bronze heavy |
| Bronze | Sealed Amphora (3) | ceramic → `phys_break_ceramic` + `phys_break_liquid`, rolls |
| Bronze | Faience Hippopotamus (0.5) | ceramic light → `phys_break_ceramic` |
| Bronze | Gold Death-Mask (1) | gold light |
| Bronze | Tripod Cauldron (12) | bronze heavy, rings for a long time |
| High | Arm Reliquary (1) | gold + `phys_break_glass` |
| High | Silver Ewer (2) | gold light + `phys_break_liquid` |
| High | Illuminated Psalter (1) | book → `phys_break_book` |
| High | Coin Coffer (8) | wood heavy + `phys_impact_coins`, spills on break |
| High | Gilded Altarpiece (14) | wood heavy → `phys_break_wood`; carry uses `sfx_carry_strain_loop` |
| Late | Parade Armour on its Stand (13) | metal heavy, a long armour-collapse clatter |
| Late | Rolled Tapestry (8) | cloth; can catch fire |
| Late | Banker's Ledger (1.5) | book |
| Late | Jewelled Hat-Badge (0.2) | gold light, tiny |
| Late | Gilded Nef (4) | gold heavy, rattles on landing |
| Powder | Cabinet of Curiosities (12) | wood heavy, rattles inside → `phys_break_wood` + `phys_break_glass` |
| Powder | Venetian Mirror (6) | glass → `phys_break_glass_large` |
| Powder | Astrolabe (1.5) | bronze light (brass) |
| Powder | Silver Service Tureen (3) | gold light, lid rattles, rolls |
| Powder | Nautilus Cup (1) | glass light → `phys_break_glass` |

Loot UI hooks (`Loot/LootHighlight.cs`, `LootInteractor.cs`):

| Name | × | Plays when | Brief | Src |
|---|---|---|---|---|
| `sfx_loot_highlight` | 1 | looking at loot | a very faint gold glint (the gold timbre) | G |
| `sfx_loot_pickup_gold` | 2 | picking up a gold/coin item | a brief coin chime on top of the pickup | G/L |

### 3.8 Weapons — `Items/Weapons/*`, `Data/Loot/Weapon_*.asset`

Sounds are made per **family**; each weapon picks its family and gets a pitch/material nudge. This
covers the 10 weapon assets that exist (Arming Sword, Bronze Sword, Longsword, Round Shield, Pavise,
Plate Helm, Crossbow, Matchlock, Flintlock Pistol, Powder Grenade) and the 12 in the pitch.

| Family | Sounds (× each) | Weapons | Src | Noise |
|---|---|---|---|---|
| Blade, steel | `sfx_wpn_blade_swing` ×4, `_hit_flesh` ×3, `_hit_armour` ×3, `_hit_wood` ×2, `_hit_stone` ×2, `_clash` ×3, `_draw` ×1 | arming sword, longsword | L | low |
| Blade, bronze | same set, duller and lower (`sfx_wpn_bronze_*`) + `_bend` ×1 (the khopesh bends) | bronze sword, khopesh | L+G | low |
| Rapier | `sfx_wpn_rapier_swing` ×3 (thin whip), `_hit` ×2, `_snap` ×1 | rapier | L | none |
| Blunt | `sfx_wpn_blunt_swing` ×3 (heavy), `_hit_armour` ×3 (a huge clang), `_hit_flesh` ×2, `_hit_door` ×2 | flanged mace, poleaxe | L | high |
| Shield | `sfx_wpn_shield_bash` ×2, `_block` ×3, `_arrow_thunk` ×3, `_pavise_plant` ×1 | round/oxhide shield, pavise, plate helm (bash) | L | mid |
| Crossbow | `sfx_wpn_xbow_span_loop` ×1 (the "long and terrible pause"), `_loose` ×2, `_bolt_fly` ×1, `_bolt_hit_wood` / `_stone` / `_flesh` ×2 | crossbow | L | none |
| Sling | `sfx_wpn_sling_whirl_loop` ×1, `_release` ×2, `_stone_hit` ×2 | sling | L/R | none |
| Match firearm | `sfx_wpn_match_hiss_loop` ×1, `_fire` ×3, `_tail_small` / `_tail_large` ×1 (echo by room size), `_reload` ×1, `_flash_in_pan` ×1 (it didn't fire) | matchlock, hand cannon | L/A | max |
| Wheellock / flintlock | `sfx_wpn_wheellock_spin` ×1, `sfx_wpn_flint_click` ×1, `_fire` ×3, `_reload` ×1 | flintlock pistol, wheellock | L | high |
| Ball | `sfx_wpn_ball_ricochet` ×3, `_hit_flesh` ×2, `_hit_stone` ×2 | all firearms | L | low |
| Grenado / petard | `sfx_wpn_fuse_light` ×1, `_fuse_loop` ×1, `sfx_wpn_explosion_small` ×2, `_explosion_large` ×2, `_debris` ×2 | powder grenade, grenado, petard | L/A | max |
| Caltrops | `sfx_wpn_caltrops_scatter` ×2, `_step_on` ×2 | caltrops | L | low |
| Any | `sfx_wpn_equip` ×2, `sfx_wpn_break` ×2 | all | L | low |

### 3.9 Enemies — `Guards/CastleGuard.cs`, `GuardBrain.cs`, art-bible roster

This follows the **art-bible roster**: 16 enemies, four per Age (`docs/art/data/*.json`). The older
supernatural prototype enemies (`ArcRevenant`, `HexTurret`, `GildedColossus` …) get **no sounds**
until someone decides the bestiary question in `docs/plans/moodboard-gap-closure.md` §2.6. Building
audio for them now would be building for the wrong game if they're cut.

**Gear layer** (plays with `foley_step_*`, on every step and on body movement):

| Name | × | Worn by | Src |
|---|---|---|---|
| `foley_gear_linen_move` | 4 | Palace Levy, Wall Slinger, Keeper of the Flame | L |
| `foley_gear_leather_move` | 4 | Castle Crossbowman, Handgunner, Petardier, Musketeer | L |
| `foley_gear_mail_move` | 4 | Lantern Warden, Household Knight, Sallet Halberdier, Pavisier | L |
| `foley_gear_plate_move` | 4 | Gothic Man-at-Arms, Cuirassier, Palace Guard (partial) | L |
| `foley_gear_bronze_plate_move` | 4 | Dendra Champion (a hollow bronze clank, unmistakable) | L/A |
| `foley_step_hound` | 6 | Alaunt War-hound (claws on stone) | L |

**Signature sounds** (one or two per enemy, so each one can be recognised in the dark):

| Enemy (Age, role) | Name | × | Brief | Src |
|---|---|---|---|---|
| Palace Levy (Bronze, patrol) | uses Blade-bronze + Shield | — | spear and oxhide shield | — |
| Wall Slinger (Bronze, ranged) | uses Sling | — | the whirl gives him away | — |
| Dendra Champion (Bronze, heavy) | `sfx_enemy_dendra_boar_tusk_rattle` | 2 | a helmet of boar tusks clacking | L/R |
| Keeper of the Flame (Bronze, special) | `sfx_enemy_keeper_firepot_throw` / `_shatter` | 2+2 | a clay pot of fire bursting | L/A |
| Lantern Warden (High, patrol) | `amb_enemy_lantern_creak_loop` | 1 | a creaking lantern bail: you hear the light coming | L/R |
| Castle Crossbowman (High, ranged) | uses Crossbow | — | — | — |
| Household Knight (High, heavy) | uses Blade-steel + plate gear | — | — | — |
| Alaunt War-hound (High, special) | `vo_hound_pant_loop` ×1, `_growl` ×3, `_bark` ×4, `_bite` ×3, `_yelp` ×3, `_howl` ×1, `_whimper_grabbed` ×2 | — | a big dog; the howl doubles as a Roused call | L/R |
| Sallet Halberdier (Late, patrol) | uses Blunt | — | — | — |
| Handgunner (Late, ranged) | uses Match firearm | — | the match hiss gives him away | — |
| Gothic Man-at-Arms (Late, heavy) | uses Blunt + plate | — | — | — |
| Pavisier (Late, special) | `sfx_wpn_shield_pavise_plant` | — | the pavise slammed into the floor | — |
| Palace Guard (Powder, patrol) | uses Blade-steel | — | — | — |
| Musketeer (Powder, ranged) | uses Match firearm + `sfx_enemy_musket_rest_plant` ×1 | 1 | a musket fork planted | L |
| Cuirassier (Powder, heavy) | uses Wheellock + plate | — | — | — |
| Petardier (Powder, special) | `sfx_enemy_petard_plant` ×1 + Grenado/petard set | 1 | a bell of powder hammered onto a door | L |

**Voices** (#42). Every human enemy has a voice made from the same **line set**. Each Age speaks its
own language, so the century can be heard as well as seen. Lines don't need to be understood; tone
carries the meaning.

| Age | Language | Voices (one per enemy) |
|---|---|---|
| Bronze | Greek-sounding made-up words (Linear B style) | levy, slinger, champion, keeper (the keeper chants) |
| High | Old French / Middle English | warden, crossbowman, knight |
| Late | Middle English / Burgundian French | halberdier, handgunner, man-at-arms, pavisier |
| Powder | Early Modern English | guard, musketeer, cuirassier, petardier |

That's 15 human voices. Line set per voice (`vo_<age>_<enemy>_<line>`):

| Line | × | Plays when (`GuardAlertState`) | Example direction |
|---|---|---|---|
| `murmur` | 4 | Patrolling, every 8–20 s | bored mutter, humming, a yawn |
| `alert` | 3 | → Investigating | "Hm? Who's there?" |
| `chase` | 3 | → Chasing | "THIEVES! To me!" |
| `search` | 2 | Searching | "Come out…" |
| `lost` | 2 | back to patrol | "…rats." |
| `attack` | 3 | `Attacked` | effort grunt |
| `hurt` | 3 | damaged | pain |
| `death` | 2 | killed | short and not graphic; a bit comic |
| `asleep` | 2 | put to sleep by SOMNUS | a snore loop |
| `grabbed` | 3 | picked up, carried, throttled ("they have opinions") | indignant, muffled protest |
| `thrown` | 2 | thrown | a yell cut short by the impact |

About 29 files per voice, around **435 voice files** in total, plus the hound. That's the biggest single
block of work, which is why §7.4 plans for it separately.

### 3.10 Alarm and the household waking — `Alarm/AlarmFSMManager.cs`

The alarm is **diegetic**: characters inside the castle hear it too. Each Age raises the alarm with
its own instrument. Because the alarm latches (it never goes back down), these loops keep playing once
started. A non-diegetic `sting_alarm_*` (§4.3) plays over the top so the change is never missed.

| Name | × | Plays when | Brief (per Age) | Src |
|---|---|---|---|---|
| `amb_alarm_bronze_roused` / `_huecry_loop` | 1+1 | → Roused / Hue & Cry | a bull-horn call, then war-conch and bronze gong | L/A |
| `amb_alarm_high_roused` / `_huecry_loop` | 1+1 | same | a single chapel bell, then fast bell tolling | L |
| `amb_alarm_late_roused` / `_huecry_loop` | 1+1 | same | a watch horn, then a tocsin bell and drum | L |
| `amb_alarm_powder_roused` / `_huecry_loop` | 1+1 | same | a drum beat to quarters, then a drum roll and a signal gun | L |
| `amb_household_stir_loop` | 1 | Stirred and above | distant doors, muffled voices, dogs, 3D spot sounds scattered through the castle | L/R |
| `sfx_castle_lockdown_portcullis_drop` | 2 | `CastleLockdown` seals a way out | a chain let go, iron teeth hitting stone | L |
| `sfx_castle_lockdown_bolt` | 3 | doors lock during lockdown | a heavy iron bolt thrown | L |

### 3.11 The castle — doors, fire, hazards — `Castle/CastleDoor.cs`, `Atmosphere/FireSource.cs`

| Name | × | Plays when | Brief | Src | Noise |
|---|---|---|---|---|---|
| `sfx_door_wood_open` / `_close` | 3+3 | wooden door | an iron hinge creak; a thud | L | low |
| `sfx_door_heavy_open` / `_close` | 2+2 | gates, great doors | a long groan; a boom | L | mid |
| `sfx_door_grate_open` / `_close` | 2+2 | iron grates, cell doors | a rattle and squeal | L | mid |
| `sfx_door_locked_rattle` | 3 | trying a locked door | the handle jiggles, the bolt holds | L | low |
| `sfx_door_unlock` | 2 | unlocked | a key turning in the ward | L | low |
| `sfx_door_slam` | 2 | slammed or hit hard | a crack | L | high |
| `sfx_portcullis_raise_loop` / `_stop` | 1+1 | winching up | chain and ratchet | L | mid |
| `sfx_masonry_crumble` | 3 | walls break (FRANGO, explosions) | a stone collapse | L | max |
| `sfx_debris_settle` | 3 | after a collapse | dust and pebbles | L | low |
| `sfx_fire_ignite` | 3 | anything catches | a *whumph* | L | mid |
| `sfx_fire_tapestry_catch` | 2 | tapestry or thatch catches | a fast, rising crackle roar | L/A | mid |
| `amb_fire_candle_loop` | 1 | candles (3D, very quiet) | a faint flutter | L | none |
| `amb_fire_torch_loop` | 1 | torches and sconces | a crackle and flap | L | none |
| `amb_fire_brazier_loop` / `amb_fire_hearth_loop` | 1+1 | braziers, hearths | a deeper crackle | L | none |
| `amb_fire_blaze_loop` | 1 | a room on fire | a roar | L | low |
| `sfx_fire_extinguish` | 2 | a fire goes out | a hiss | L | none |
| `sfx_torch_drop` | 2 | a carried light is dropped ("light is a thing you carry — and may drop") | a clatter and a sputter | L | low |

**Age hazards** (#35, #44; each row is used once that hazard is built):

| Name | × | Hazard | Src | Noise |
|---|---|---|---|---|
| `sfx_hazard_fire_spread_fast` | 2 | Bronze: fire runs faster; a quicker, hungrier crackle | L | mid |
| `sfx_hazard_oil_pour` / `_sizzle` | 1+2 | High: boiling oil | L | mid |
| `sfx_hazard_murderhole_drop` | 2 | Late: stones or bolts from above; a rattle, then the hit | L | mid |
| `sfx_hazard_arrowloop_loose` | 2 | Late: arrows through the loops | L | low |
| `sfx_hazard_magazine_explosion` | 1 | Powder: the magazine goes up, the biggest sound in the game | L/A | max |
| `sfx_hazard_explosion_tail_distant` | 2 | heard from elsewhere in the castle | L/A | — |
| `sfx_drawbridge_raise_loop` / `_slam` | 1+1 | if the drawbridge (#44) gets built | L | high |

### 3.12 Portal, extraction, and the raid's end — `Extraction/ExtractionZone.cs`, `Raid/RaidDirector.cs`

| Name | × | Plays when | Brief | Src |
|---|---|---|---|---|
| `amb_portal_loop` | 1 | the portal is open (3D) | a cold lapis drone, "the only colour nature did not make" | G/A |
| `sfx_portal_open` | 1 | the portal opens in the Lair | a tear and inrush | A |
| `sfx_portal_enter` | 1 | stepping through (local, 2D) | a whoosh through time, with a muffled century on the far side | A |
| `sfx_portal_arrive` | 1 | arriving in the castle | the whoosh let out into fresh air | A |
| `sfx_portal_narrowing_loop` | 1 | the way home is closing, rising as the timer runs down | the drone tightening upward | G |
| `sting_portal_warning` | 3 | time warnings (e.g. 2 min / 1 min / 30 s) | a tolling lapis bell, more urgent each time | C/G |
| `sfx_portal_collapse` | 1 | the portal closes | an implosion | A |
| `sfx_extract_item_cross` | 3 | an item crosses the threshold (`HaulInZoneChanged`) | a coin-count chime, pitch rising with value | G |
| `sfx_extract_player_cross` | 1 | a wizard gets out | the portal whoosh, softened | A |
| `sfx_result_tally_tick` | 1 | the results screen counts up | an abacus bead / coin drop | L/G |
| `sfx_result_tally_total` | 1 | the count finishes | a heavy coin purse set down | L |

### 3.13 The Lair — `Lair/LairHubManager.cs`, `UI/Screens/LairScreen.cs`

| Name | × | Plays when | Brief | Src |
|---|---|---|---|---|
| `amb_lair_bed_loop` | 1 | always in the Lair | drips, damp stone, a far-off creak | L |
| `amb_lair_hearth_loop` | 1 | 3D at the fire | a warm crackle (the only safe fire in the game) | L |
| `sfx_lair_debt_pay` | 2 | gold paid against the debt | coins pouring into a coffer, then a quill scratch in the ledger | L/R |
| `sfx_lair_debt_due` | 1 | the debt collector's reminder | three slow knocks on the door | R/L |
| `sfx_lair_era_select` | 4 | picking a century (one per Age) | an hourglass turned, plus that Age's instrument signature | G/C |
| `sfx_lair_weapon_rack_take` | 2 | taking a weapon | a scrape off a wooden peg | L |
| `sfx_lair_revive` | 1 | a carried-home friend lives again | a warm, rising chord | C/G |

### 3.14 Combat feedback — `UI/RaidHud/DamageFeedbackView.cs`, `docs/4-systems/damage.md`

| Name | × | Plays when | Brief | Src |
|---|---|---|---|---|
| `ui_hit_confirm` | 3 | you damaged an enemy (local) | a very short thock under the world sound | G |
| `ui_hit_friendly` | 2 | you hit a teammate | a comic wooden "bonk", so blame is obvious | G/L |
| `ui_kill_confirm` | 1 | you downed an enemy | a low muted drum tap | G/C |

## 4. Music: the full list

### 4.1 Instruments per Age

Each Age gets a fixed tempo and key so its four raid layers (§4.2) stay in time and blend into each
other.

| Age | Instruments | Tempo / key | Feel |
|---|---|---|---|
| Bronze | frame drum, lyre, double aulos, bronze cymbals, low drone voice | 84 BPM, D Phrygian | hot, dusty, ritual |
| High | Gregorian-style male choir, vielle, psaltery, organ drone, tabor | 72 BPM, D Dorian | cold stone, candles |
| Late | shawm, sackbut, crumhorn, tabor and side drum, low strings | 96 BPM, A Aeolian | martial, crafty |
| Powder | harpsichord, viol consort, baroque trumpet, timpani | 108 BPM, G minor | grand, glittering, fragile |
| Lair / menus | hurdy-gurdy, lute, recorder, bowed psaltery | 66 BPM, E minor | shabby, warm, in debt |
| Voice / portal | a lapis choir pad (reversed voices, glass harmonica) | free time | the one sound "nature did not make" |

### 4.2 Tracks

| Name | Length | Plays when | Brief | Src |
|---|---|---|---|---|
| `mus_title_loop` | 2–3 min loop | main menu | "The Herald's Overture": lute and hurdy-gurdy, a pompous theme played a little too grandly (the herald oversells) | C |
| `mus_lair_loop` | 3 min loop | in the Lair | the same theme, reduced: one candle, one fire | C |
| `mus_lair_after_loss_loop` | 2 min loop | Lair after a failed raid | solo hurdy-gurdy, sadder | C |
| `mus_raid_<age>_calm` | 2 min loop | Calm | almost ambience: drone plus sparse Age instrument | C |
| `mus_raid_<age>_stirred` | same length, same bars | Stirred: added **on top of** calm | a pulse; the household is shifting in its sleep | C |
| `mus_raid_<age>_roused` | same | Roused (the alarm latches from here) | percussion ostinato, the main theme appears | C |
| `mus_raid_<age>_huecry` | same | Hue & Cry | full chase: everything plus alarm-bell hits | C |
| `mus_raid_portal_narrowing` | 1 min loop | the last minutes of the portal: a layer over any Age | a rising lapis choir and a ticking figure | C/G |
| `mus_results_success_loop` | 1 min loop | results screen, good haul | a jaunty tavern reel (the lute theme) | C |
| `mus_results_failure_loop` | 1 min loop | results screen, bad night | a slow reel, a bit tipsy | C |
| `mus_credits` | 3 min | credits (if there are any) | a full theme arrangement | C |

That's 4 Ages × 4 layers = **16 raid stems**, plus 8 other tracks. Layers are **added and never
taken away** while the raid is on, which matches the alarm's latch (a quick fade is allowed if Calm
is ever reached again before Roused).

### 4.3 Stingers (2–6 s, played over the music)

| Name | × | Plays when | Brief | Src |
|---|---|---|---|---|
| `sting_alarm_stirred` | 1 per Age | → Stirred | a single low Age-instrument note | C |
| `sting_alarm_roused` | 1 per Age | → Roused | a rising phrase that falls into the latch | C |
| `sting_alarm_huecry` | 1 per Age | → Hue & Cry | a full-ensemble crash | C |
| `sting_gold_found` | 2 | first time you see the treasury or chapel loot | an orpiment shimmer: bells and a harp gliss | C/A |
| `sting_player_down` | 1 | any teammate downed | a muted drum and low bell | C |
| `sting_extract_success` | 1 | everyone got out with a haul | a fanfare in the Lair theme | C |
| `sting_raid_lost` | 1 | everyone down or the portal gone | a sackbut sighs downwards | C |
| `sting_portal_opened` | 1 | lair → castle crossing | the lapis choir swells | C/A |
| `sting_spell_misfire` | 3 | (listed in §3.2) | a crumhorn blat | C/A |

## 5. Ambience beds

Beds are 2D stereo loops, crossfaded by zone. Spot sounds are 3D loops placed by the scene builder.
Everything is at night (`docs/plans/night-atmosphere.md`).

| Name | Plays where | Brief | Src |
|---|---|---|---|
| `amb_bronze_exterior_loop` | Bronze bailey, the Lion Gate | warm night wind, crickets, a distant sea, a goat bell | L |
| `amb_high_exterior_loop` | High: curtain wall, gatehouse | cold wind on stone, an owl, a far dog | L |
| `amb_late_exterior_loop` | Late: barbican | wet stone, drips from gutters, flags snapping | L |
| `amb_powder_exterior_loop` | Powder: gardens, facade | a light breeze, distant town bells, a fountain | L |
| `amb_room_corridor_loop` | any corridor | dead air, a faint draught | L |
| `amb_room_small_loop` | chambers | room tone | L |
| `amb_room_hall_loop` | great halls, the Megaron | a large, faintly humming space | L |
| `amb_room_stair_loop` | the Newel Stair, spiral stairs | a draught whistling round the newel | L |
| `amb_room_crypt_loop` | crypt, cellars | drips, deep sub-rumble, distant stone settling | L |
| `amb_room_chapel_loop` | the Chapel | a huge reverberant hush, a faint organ drone, candle hiss | L+C |
| `amb_room_magazine_loop` | Pithos Magazine, Powder Magazine | dry, muffled, creaks of clay or kegs | L |
| `amb_room_counting_loop` | the Counting House | quiet, with an occasional abacus click | L |
| `amb_room_kunstkammer_loop` | the Kunstkammer | a clock ticking, a clockwork curiosity whirring | L |
| `amb_room_gallery_loop` | the Long Gallery | wind rattling big glass panes (every pane is breakable loot of noise) | L |
| `amb_spot_dripping` / `amb_spot_rats` / `amb_spot_wind_gap` | scattered 3D spots | life in the stone | L |

## 6. Totals

| Block | Distinct names | Files (with variants) |
|---|---|---|
| UI, casting, combat feedback | ~30 | ~45 |
| Spells and status | ~37 | ~55 |
| Wizard, grab/carry, physics, loot | ~55 | ~150 |
| Weapons | ~50 | ~100 |
| Enemy gear, signatures, hound | ~20 | ~55 |
| Enemy voices | 15 voices × 11 lines | ~435 |
| Alarm, castle, hazards | ~40 | ~70 |
| Portal, extraction, Lair | ~22 | ~30 |
| Ambience | ~20 | ~20 |
| Music tracks and stems | 24 | 24 |
| Stingers | ~20 | ~25 |
| **Total** | **~480** | **~1,000** |

About 45% of all files are guard voices. Everything else is about 560 files.

## 7. How to generate or source them

### 7.1 Where each kind of sound comes from

| Kind | Recommended | Fallback | Why |
|---|---|---|---|
| Footsteps, cloth, physics, doors, fire, weapons (the **L** rows) | **Royalty-free library**: the Sonniss GDC Game Audio Bundle (free every year, commercial use, no attribution), Freesound filtered to **CC0 only**, Kenney audio packs (CC0) | record foley yourself (a phone and a pillow fort are enough) | These have been recorded thousands of times; generating them wastes effort. Real recordings beat AI for impacts. |
| Magic, misfires, portal, signature oddities (the **A** rows) | **AI text-to-sound**: ElevenLabs Sound Effects (paid plan for commercial rights), or **Stable Audio Open** run locally (Stability Community License: free for commercial use under $1M/yr revenue) | layer library sounds by hand in Audacity/Reaper | Nothing like these exists in libraries, and AI is good at "a lapis tear in time". |
| UI, tones, loops that follow a game value (the **G** rows) | **In-repo synth**: `Tools/AudioForge/`, Python + numpy, reproducible from committed scripts (§7.2) | Kenney UI packs | Needs exact control: the Levo hum pitch follows mass, the extraction chime rises with value. It's also free and fully regenerable, like the Blender pipelines. |
| Guard voices (§3.9) | **Record friends** in their own voices, reading a line sheet (§7.4) | AI voice (ElevenLabs voice design, paid plan) | Co-op game, co-op cast; real people shouting "THIEVES!" in bad Old French is the pitch's comedy. |
| Adaptive raid stems (§4.2) | **Commissioned composer**, briefed from §4.1 | the user composes in a DAW with free period-instrument libraries (Spitfire LABS, VSCO 2 CE (CC0), Versilian), starting from MIDI sketches I generate | Stems have to share exact tempo, key and bar length. AI music tools can't reliably produce that. |
| Title, Lair, results, credits (non-adaptive music) | same composer | AI music (Suno/Udio **paid** tier, which grants commercial use) | These only need to be good loops, which AI music can do; its legal ground has been shifting, so check current terms before shipping any. |

**Licences to avoid:** any CC-BY-**NC** sound (Freesound's NC filter); anything made with Meta
AudioGen/MusicGen (the model weights are CC-BY-NC, so their output can't safely be sold); free-tier
AI output where the terms don't grant commercial use.
**Steam:** anything made with AI must be declared in the Steam content survey. Keep a list (§7.3) so
the answer is exact.

### 7.2 `Tools/AudioForge/`: the pipeline

It works like `Tools/AssetPipeline/` and `Tools/EnemyForge/`: files are generated from committed
sources, and regenerating them gives the same result.

1. **`manifest.csv`** holds this plan's tables as data, one row per name, with columns
   `name, variants, bus, spatial, loop, src, noise, brief, status, source_ref, licence`. It's the
   single source of truth, generated from `manifest_source.py`. Where this doc's tables and the
   manifest disagree, the manifest wins (e.g. `sfx_lair_era_select` became one sound per Age).
2. **`synth/`** holds the **G** sounds as Python (numpy + scipy; the container needs
   `pip install numpy scipy soundfile` first). Each sound is a function, seeded, so output is the
   same every run.
3. **`library/`** holds the licensed source files for the **L** rows as downloaded, each with its
   licence and URL written into the manifest.
4. **`ai/`** holds the prompt used for each **A** row, which service and version made it, the date,
   and every take generated. The kept take is marked in the manifest.
5. **`process.py`** trims, fades, normalises loudness to the targets in §2 and converts to mono or
   stereo, then writes into `Assets/_Project/Audio/...` (needs ffmpeg).
6. **`check.py`** confirms every manifest row has a file, every file has a licence, loops loop
   cleanly (no click at the join), no file breaks the loudness targets, and nothing in
   `Assets/_Project/Audio/` is missing from the manifest. It runs in the headless harness so CI
   catches gaps.
7. **`preview.html`**: a generated page listing every sound with a play button and its brief, for
   reviewing a whole category by ear at once (the audio equivalent of the art review sheets).

### 7.3 In the game: the audio layer

- **`SoundBank`** (a ScriptableObject) maps each name to its clips, bus, spatial settings, volume,
  pitch range and noise class. The manifest generates its entries.
- **`AudioDirector`** listens for the existing events (§0) and plays from the `SoundBank`, drawing
  sources from a pool (never created and destroyed in the moment). Gameplay code only needs new hooks
  where no event exists yet (footsteps, swings, grab beam).
- **`MusicDirector`** listens for `AlarmStateChanged` and `RaidPhase`, and fades the four stems of
  the current Age in and out on bar lines. It also plays the stingers.
- The `Casting` snapshot hooks into the cast key press and release (§1.3).
- A combat-bench (`docs/4-systems/combat-bench.md`) toggle cycles every sound in a category, to
  hear it in the game's own acoustics.

Once built, this becomes a tier-4 doc, `docs/4-systems/audio.md`.

### 7.4 Guard voices: the recording plan

1. Write a **line sheet** per Age: the 11 lines × 2–4 takes, in that Age's language, with a
   pronunciation guide. I can write these. The Old French / Middle English don't need to be
   scholarly, just believable.
2. Cast 15 voices from friends and playtesters (one person can do two or three voices by changing
   how they speak). Record on any decent USB mic, in a closet or under a duvet (dead, echo-free
   sound), mono, 48 kHz.
3. Process: noise reduction, a little saturation, and an EQ per Age (Bronze drier and warmer, Powder
   brighter) so the voices sit in their century.
4. Contributors sign a one-line release granting use of the recordings in the game.

### 7.5 Order of work

Mechanism first, as the Roadmap says: the finished recordings wait for M7, but the systems that play
them don't have to wait.

| Phase | What | Gives |
|---|---|---|
| **A. The audio layer + placeholders** | mixer, `SoundBank`, `AudioDirector`, `MusicDirector`, snapshots; AudioForge `manifest.csv` + `synth/` + `check.py`; synth placeholders for the M7 acceptance set (swing, throw, hit, cast, fizzle, misfire, death, door, loot break, alarm change) | #22 plumbing, #56 sliders become real, the game makes noise |
| **B. Library pass** | the **L** rows: all foley, physics, weapons, doors, fire, ambience beds and alarm bells from Sonniss / CC0 | most of the game sounds right |
| **C. Spells and portal** | the **A** rows: spell, misfire, portal and teammate-tell sounds | the voice pillar is audible (#48) |
| **D. Guard voices** | line sheets → recording → processing | #42 |
| **E. Music** | composer brief from §4 → Lair and title first, then one Age's four stems (High, the "learning" Age), then the other three | adaptive music |
| **F. Mix** | loudness pass, reverb zones per room type, a playtest listening session | M7 acceptance |

## 8. Decisions

**Answered by the owner, 2026-09-27:** AI sound is fine (1); AI music for the non-adaptive tracks (2,
partly); record friends for guard voices (3). Still open: who makes the 16 adaptive raid stems and
the alarm stingers (the rest of 2), and the bestiary fork (4). The questions as first asked:

1. **Using AI.** Is AI-generated sound (and possibly music) acceptable, given the Steam disclosure it
   requires? If not, every **A** row moves to library layering or recording, which is more hand work.
2. **Music.** Commission a composer (recommended for the adaptive stems), compose it yourself from my
   MIDI sketches, or use AI music for the non-adaptive tracks only?
3. **Guard voices.** Record friends (recommended), AI voices, or synthesised gibberish (cheapest; the
   Simlish approach)?
4. **The bestiary fork** (`moodboard-gap-closure.md` §2.6) decides whether the old supernatural
   enemies need sounds at all. This plan assumes they're cut.
