# Plan — footsteps, physics impacts and guard voices (Phase A2 of `audio.md`)

Status: **decided 2026-09-29, being built.** The owner's word: these three are integral to the
game's feel, so they move ahead of Phases B–F. They were listed as out of scope in
[`audio-in-game-layer.md`](audio-in-game-layer.md); the layer that plays sounds is built
([`4-systems/audio.md`](../4-systems/audio.md)). The files are already in `Assets/_Project/Audio/`:
18 `foley_*`, 27 `phys_*`, and 51 per Age plus the hound's `vo_*` (guard voices are synthesised
placeholders until friends' recordings replace them through `Tools/AudioForge` `promote`; nothing
here changes when they do).

## Scope

### 1. Footsteps and movement foley (`foley_*`)

- One component on every walking character (local player, remote players, guards, the hound) that
  plays a step each time the character has covered one stride while grounded. Distance-based, not
  timer-based, so a sprint steps faster and standing still is silent. It does not need animation
  events. Where a character already reports a step (`FootstepNoiseEmitter.EmitStep`), use that
  instead so the sound and the noise the guards hear are the same event (`audio.md` §1 rule 1).
- Surface from what the character stands on: `foley_step_stone|wood|earth|rushes|tile|metal|water`.
  Find what the castle floors actually carry (physic material, tag, renderer material name, room
  type) and write one lookup that maps it; default is stone. The lookup is a pure function with a
  test.
- Loudness and reach follow the stance (crouch quieter, sprint louder) in step with the gameplay
  noise strengths already used by `FootstepNoiseEmitter`. Guards heavier in armour add the
  `foley_gear_<class>_move` layer at a lower level; the hound uses `foley_step_hound`.
- Jump, land (light and heavy by fall speed) and dodge: `foley_player_jump|land|land_heavy|dodge`,
  from the player state machine's existing states or events.
- The local player's own steps play 2D at low level; everyone else's play 3D at their position.

### 2. Physics impacts (`phys_*`)

- When a loose item hits something, play `phys_impact_<material>_<light|heavy>` at the contact
  point, chosen by material and impact speed. Also `phys_impact_body` (a body), `phys_impact_cloth`,
  `phys_impact_book`, `phys_impact_coins`. Volume and reach scale with impact speed (`audio.md`
  §1 rule 1: what you hear is what the guards hear).
- Breaks: `phys_break_ceramic|glass|glass_large|wood|book|liquid` and `phys_coin_spill`, by the
  broken piece's material, replacing the single ceramic break sound used today.
- Loops: `phys_scrape_stone|wood|metal_loop` while a piece is dragged along the floor above a
  speed floor, `phys_roll_loop` while it rolls. One loop per piece, faded in and out, pooled.
- **Material.** Loot has no material field today. Do not edit data assets. Add one lookup in
  `Plunderspell.Audio`, keyed by the piece's name and weapon type (for example "Gold", "Coin",
  "Chalice" → gold or bronze, "Sword", "Mace" → metal, "Reliquary", "Psalter" → book), with a stone
  default, and a test that every loot piece in the project resolves to something.
- **Hooks.** A single additive event on `Item` (raised from the existing `OnCollisionEnter`, which
  already computes the impact speed) and on `LootPickup.BreakItem`. Keep each edit to one event
  declaration and one raise line: the owner has unmerged work on `Item.cs`, `LootPickup.cs`,
  `LootInteractor.cs` and `LootSpawner.cs` on branch `stashing`, and a small additive edit merges
  cleanly. Do not reformat or restructure those files.

### 3. Guard voices (`vo_<age>_<guard>_<line>`, `vo_hound_*`) — issue #42

- One line per awareness state and event, from the guard's own events (`CastleGuard.StateChanged`
  is built and unused; `Attacked`; the damage path for hurt and death; the grab and throw events
  for `grabbed` and `thrown`; the sleep status for `asleep`): `murmur` (calm idle, occasional),
  `alert`, `chase`, `search`, `lost`, `attack`, `hurt`, `death`, `asleep` (periodic while asleep),
  `grabbed`, `thrown`. Hound: `pant_loop`, `growl`, `bark`, `bite`, `yelp`, `howl`,
  `whimper_grabbed`.
- The Age comes from the raid's era; the guard type maps to the manifest's archetype names (Bronze
  `levy`, `slinger`, `champion`, `keeper`; read the other three Ages' names from
  `Tools/AudioForge/manifest.csv`). The mapping is a pure function with a test that every name it
  can produce exists in the `SoundBank`.
- Limits so a crowd is readable: at most one line per guard per two seconds, at most six voice
  lines at once (nearest win), `murmur` only when the guard is calm and no line played for eight
  seconds. Voices play 3D at the guard's head, on the Creatures group.
- Guards are simulated on the host. Check that each client raises the same events; if the state is
  synced but the event is not raised on clients, derive the voice from the synced state on the
  client instead. Two players must not hear the same guard twice, and nothing goes over the network
  (`audio.md` §1 rule 6).

## Not in scope

Grab beam, ambience beds, alarm loops, the Lair/Downed/Paused snapshots, listen open/close cues,
reverb zones, the combat-bench cycle toggle, and any change to the sound files themselves.

## Hard constraints

- Stage and commit by explicit path only. The working tree was clean at the start; the owner's
  loot work is on branch `stashing`, not here.
- Before any recompile or test run, eval `EditorApplication.isPlaying`; if true, stop and ask.
- Extend `AudioDirector`/`SoundNames` and the `SoundBank` tests; do not fork a second player.
  Pool everything; no per-frame allocation in the step, loop and voice paths.
- Match existing style (CRLF, Allman, `_camelCase`). Long reasoning goes in
  `docs/4-systems/audio.md`, not in comments.

## Verify

- Recompile with 0 errors; EditMode tests for each lookup (surface, material and speed, guard and
  line) plus a test that every name they can produce is in the bank; the existing 9 audio tests
  still pass; the full EditMode run's two known failures (`ArtAssetImportTests`,
  `LootBalanceTests`) stay the only failures. Say whether each of the two fails without the audio
  changes (check it against the merge commit `84732e04`, or say why not).
- Play mode through the real flow: walk the player (drive movement through the real controller or
  input, not by setting a flag) and read which step clips play on which group; drop and throw a
  piece and read the impact and break clip; wake a guard through the alarm and read the voice line
  per state. Where a path can only be reached by raising an event from a script, say so.
- Docs: `docs/4-systems/audio.md` (tables and the gap list), `docs/plans/audio.md` status,
  `docs/3-state/ProjectState.md`, `docs/5-today/Today.md`.
