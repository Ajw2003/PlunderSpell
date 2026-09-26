import subprocess, json, os

ISSUES = []
def add(title, labels, body):
    ISSUES.append((title, labels, body))

add("PCG: rooms do not connect and the floor plan does not read as a castle",
    "pcg,bug",
"""Reported as item 1.

## Problem
The generated layout does not play as a castle. Rooms are placed on the grid but adjacent modules
do not line up into connected interior space, and the resulting flow (crypt outward to extraction)
is not legible or interesting to move through.

## Context
`Assets/_Project/Scripts/Runtime/Castle/ProceduralCastleGenerator.cs` places modules on concentric
Chebyshev rings, attaching each to an already-placed 4-neighbour. That guarantees the *metadata*
layout is 4-connected (and `CastlePathValidator` passes), but says nothing about whether the
*meshes* form connected rooms:

- Rooms are 11.2 m in a 12 m cell, so there is a ~0.8 m gap between every pair of neighbours.
- `CastleRoomModule` prefabs carry `SocketPoint` children, but the generator places by grid cell and
  never matches sockets. Several modules have 0, 1 or 2 sockets (`Bastion`, `WallCorner` and
  `WallStraight` have none), so door openings are not guaranteed to face a neighbour.
- Ring candidate cells that fail the "has a placed neighbour" test are skipped, which is what leaves
  the scattered, disconnected look.

Closely related to the floating/spacing issue; likely fixed together but tracked separately because
the acceptance criteria differ.

## Acceptance criteria
- Adjacent placed modules share a wall or a doorway; no unintended gap between neighbours.
- Socket matching (or an equivalent rule) drives placement so an opening always faces an opening.
- A player can walk from the crypt chamber to the extraction exit entirely through interior space.
- The existing `CastleGeneratorTests` determinism and reachability tests still pass.""")

add("Player is too tall for the rooms (scale mismatch between character and architecture)",
    "gameplay,bug",
"""Reported as item 2.

## Problem
The player capsule is too tall relative to room interiors, so the castle reads as cramped and
doorways/ceilings do not fit a human-scale character.

## Context
- Player is built in `Assets/_Project/Scripts/Editor/RaidSceneBuilder.cs`: `PlayerHeight = 2f`,
  `PlayerRadius = 0.4f`, `EyeHeight = 1.6f`.
- Measured room outer heights (renderer world bounds) by zone:
  Crypt 2.9 m, OuterBailey 3.5 m, InnerWard 4.1 m, Keep 4.9 m, CurtainWall 6.1 m.
  Those include floor and roof thickness, so usable interior is lower still.
- Enemy heights for comparison: Watchman 1.78 m, VaultWarden 2.38 m, GildedColossus 3.41 m. The
  Colossus does not fit in a crypt room at all.

## Acceptance criteria
- Decide the canonical metre scale for a character and record it in `docs/4-systems/`.
- Player, enemies and room interiors agree on that scale; the Colossus fits where it can spawn.
- Doorway heights clear the player with headroom.""")

add("No crosshair - cannot tell where you are aiming",
    "ui,enhancement",
"""Reported as item 3.

## Problem
There is no aiming reticle, so spell targeting and loot interaction are guesswork.

## Context
`LootInteractor` raycasts from the eye transform and spells resolve against an overlap query from
the caster, so there *is* a well-defined aim direction - it is simply never drawn.
The raid HUD is IMGUI (`Assets/_Project/Scripts/Runtime/UI/RaidHud/RaidHudView.cs`).

## Acceptance criteria
- A crosshair is drawn at the aim point while in `GameState.Playing`.
- It reflects interaction state (e.g. changes when loot is in reach and focusable).
- It is hidden in menus, the lair and on pause.""")

add("Mouse is not locked or hidden during play",
    "ui,bug",
"""Reported as item 4.

## Problem
The cursor stays free and visible during a raid, so looking around fights the OS pointer and the
cursor can leave the window.

## Context
`FreeLookPlaytestController` (`RogueAi.Playtest`) drives the camera but nothing sets
`Cursor.lockState` / `Cursor.visible`.

## Acceptance criteria
- Cursor is locked and hidden in `GameState.Playing`.
- Cursor is released and visible in `MainMenu`, `Lair`, `Paused`, `Inventory`, `Settings`.
- Focus loss / alt-tab does not leave the cursor in the wrong state.""")

add("Player can still move and look around while the main menu is open",
    "ui,bug",
"""Reported as item 5.

## Problem
Movement and mouse-look stay active behind the main menu and the lair, so the world moves while a
menu has focus.

## Context
The menu flow now exists (main menu -> lair -> Set Out -> raid, see
`docs/4-systems/raid-scene-assembly.md`), but nothing gates player input on `GameState`. The player
object and its `FreeLookPlaytestController` / `PushToCastController` are always enabled.

## Acceptance criteria
- Movement, look, interaction and casting only respond in `GameState.Playing`.
- Input is restored on returning to `Playing` from `Paused` / `Inventory`.
- Gating is driven from game state, not by disabling individual components ad hoc.""")

add("Enemies have no animations",
    "animation,enhancement",
"""Reported as item 6.

## Problem
Enemies slide around the NavMesh in a static bind pose - no idle, walk, chase, attack or death.

## Context
All 10 enemy models are rigged (19 bones each, recorded in
`Assets/Models/Enemies/enemy_manifest.json`) and import as `animationType = Generic` with a
`SkinnedMeshRenderer`, but **no `Animator` and no clips**. The prefabs in
`Assets/_Project/Prefabs/Enemies/` are built by `EnemyPrefabForge`, which deliberately adds no
animator yet.

`CastleGuard` already exposes what an animator would need: `GuardAlertState`
(Patrolling / Investigating / Chasing / ...), `IsIncapacitated`, and a `StateChanged` event.

## Acceptance criteria
- Each enemy has an `AnimatorController` with at least idle / locomotion / attack / death.
- Locomotion is driven by `NavMeshAgent` velocity; state changes hook `CastleGuard.StateChanged`.
- `EnemyPrefabForge` wires the animator so re-forging prefabs does not drop it.""")

add("No animations for the player or for doors",
    "animation,enhancement",
"""Reported as item 7.

## Problem
The player has no visible body or hand animation (including no cast gesture), and doors pop between
open and closed with no motion.

## Context
- `PushToCastController` already has `_handAnimator` / `_isCastingBool` fields wired for a cast
  animation - they are simply never assigned, and the player is a primitive capsule built in
  `RaidSceneBuilder`.
- Doors implement `IOpenable` / `IHandOpenable` and are opened by interaction and by the `PORTA`
  spell; the state change is instant.

## Acceptance criteria
- Player has a first-person hand/arm rig with idle and cast-gesture states driven by
  `PushToCastController.IsCasting`.
- Doors animate open/closed over time, and `PORTA` drives the same animation.""")

add("No visual feedback for taking or dealing damage",
    "vfx,enhancement",
"""Reported as item 8.

## Problem
Nothing on screen indicates that damage happened - no hit flash, no hurt vignette, no hit marker,
no damage numbers. Combat is unreadable.

## Context
`IHealth` exists and `CastleGuard` implements it (`CurrentHealth`, `MaxHealth`), and
`StatusEffectReceiver` applies burn / stun / sleep. Damage is applied but never presented.
Blocked in practice by the health/damage model issue.

## Acceptance criteria
- Taking damage produces a clear screen-space response.
- Dealing damage produces a clear response on the target (flash / impact / hit marker).
- Death is visually distinct from merely being hurt.""")

add("No visual feedback for spells",
    "vfx,enhancement",
"""Reported as item 9.

## Problem
Casting a spell produces no visible effect. Confirmed in play mode: `[SpellCast] Local cast Ignis`
logs to console and resolves against targets, but nothing renders.

## Context
Eight spells exist as assets in `Assets/_Project/Data/Spells/` (IGNIS, FRANGO, LEVO, AURUM VOCO,
TONITRUS, SOMNUS, CADAVER SURGE, PORTA) and resolve through `SpellCastingSystem` / `MisfireEngine`.
`LootPickup` has an unassigned `_brokenVfx` field, so a hook already exists for at least one.

Misfires in particular are invisible, which makes the whole misfire mechanic unreadable.

## Acceptance criteria
- Each of the eight spells has a distinct cast effect and an effect at the point of resolution.
- A misfire is visually distinct from a successful cast.
- Volume (whisper / normal / shout) is reflected in the effect's scale or intensity.""")

add("No usable health or damage model - nothing is distinguishable in play",
    "gameplay,enhancement",
"""Reported as item 10.

## Problem
There is no health or damage the player can perceive or act on. You cannot tell whether you are
hurting an enemy, whether you are being hurt, or how close anything is to dying.

## Context
The pieces exist but are not a system:
- `IHealth` with `CurrentHealth` / `MaxHealth`; `CastleGuard` implements it with `_maxHealth` tuned
  per enemy in `EnemyPrefabForge.Specs` (55 for a WarHound up to 400 for the GildedColossus).
- `StatusEffectReceiver` implements burn / stun / sleep and can kill a guard over time.
- `PlayerStats` exists in `Plunderspell.Core`.

There is no player damage intake, no shared damage pipeline, and no presentation.

## Acceptance criteria
- One damage pathway used by both player and enemies.
- Player health is visible on the HUD and can reach zero with a defined consequence.
- Enemy health is readable in-world (bar, stagger, or equivalent).""")

add("No carryable objects placed in the level - picking anything up is not discoverable",
    "gameplay,bug",
"""Reported as item 11.

## Problem
There is nothing obviously carryable in the world, and picking loot up does not read as available.

## Context
Loot *is* spawning - a raid places ~19 pieces across all five authored prefabs (CopperPot,
SilverPlate, GoldenGoblet, HeavyChest, AncientRelic), and `LootInteractor` supports take / drop and
two-person carry for heavy items. Two things make it unusable in practice:
1. Pieces are physically small (a goblet is 0.10 x 0.19 x 0.11 m) and scattered at room centres.
2. Many are flung out of position at spawn - see the loot-spawn physics issue.

There is also no prompt or highlight when a pickup is in reach, and no crosshair to aim with.

## Acceptance criteria
- Loot is visible and identifiable in a room at a glance.
- An in-reach pickup is highlighted, with a prompt naming it and the key.
- Heavy items communicate that they need a second pair of hands before you try.""")

add("Main menu has no art",
    "art,ui,enhancement",
"""Reported as item 12.

## Problem
The main menu is a flat colour background with default-styled text and buttons. It sets no tone for
the game.

## Context
`Assets/_Project/Scripts/Runtime/UI/Screens/MainMenuScreen.cs`, built from `UIFactory` primitives
with colours from `UITheme`. The same applies to the new `LairScreen`.

## Acceptance criteria
- Menu has a background treatment (art, or a rendered scene behind the UI).
- Title, buttons and version label use a deliberate visual style rather than factory defaults.
- The lair screen is consistent with it.""")

add("Only one era (High Medieval) has art and models",
    "art,enhancement",
"""Reported as item 13.

## Problem
`HistoricalEra` offers four eras and the lair lets you pick any of them, but only High Medieval has
assets, so the other three produce the same castle and the same enemies.

## Context
- `HistoricalEra`: BronzeAge, HighMedieval, LateMedieval, AgeOfPowder.
- `LairScreen` presents all four and `RaidDirector.StartRaid(era)` accepts them.
- Nothing downstream varies by era: `CastleRoomRegistry` (25 rooms), `EnemyRoster` (10 enemies) and
  `RaidLootTable` are era-agnostic. Weapon prefabs in `Assets/_Project/Prefabs/Weapons/` do span
  eras (BronzeSword, Longsword, Matchlock, FlintlockPistol, ...) but are not wired into a raid.

## Acceptance criteria
- Either era selection drives era-specific rooms / enemies / loot, or unimplemented eras are
  disabled in the lair with a clear "coming soon" state.
- If eras are kept, the catalogues gain an era dimension so a raid can be filtered by it.""")

add("No way to test combat",
    "gameplay,enhancement",
"""Reported as item 14.

## Problem
There is no quick way to exercise combat. Reaching an enemy means launching a raid, finding a guard
somewhere in a 40+ room castle, and hoping the encounter works.

## Context
Guard behaviour is unit-tested (`RogueAi.Tests.GuardTests`, 18 tests) but that covers `GuardBrain`
logic, not a playable encounter. A `TestScene` and `TestSceneBuilder` already exist to build on.

## Acceptance criteria
- A combat test scene or menu entry that spawns chosen enemies in a flat arena with the player.
- Enemy type, count and alarm level selectable without editing code.
- Reachable from a single menu item or scene, documented in `docs/4-systems/`.""")

add("Modules float and snap inconsistently - grid spacing must be exact",
    "pcg,bug",
"""Reported as item 15.

## Problem
Structures connect and disconnect between seeds, and some sit at heights they should not. Placement
should land on an exact grid, not approximately.

## Context
`ProceduralCastleGenerator` uses `cellSize = 12f` and places at
`new Vector3(cell.x * cellSize, 0f, cell.y * cellSize)`, so the *origin* spacing is exact - but:
- Room meshes are 11.2 m wide in a 12 m cell, so there is a systematic ~0.8 m gap at every join.
- Module footprints are not uniform: `Drawbridge` is 11.2 x 19.8 m and `WallCorner` is 12.3 x 12.4 m,
  so they cannot tile a 12 m grid the way the others do.
- Room heights vary by zone (2.9 m to 9.7 m) with no rule for how a taller module meets a shorter one.

Room roots now sit correctly at y = 0 after the orientation fix (verified: 38/38 rooms with
`minY ~= 0`), so any remaining floating is placement, not the prefab transforms.

## Acceptance criteria
- Module footprints are exact multiples of the cell size, or the cell size matches the modules.
- Neighbouring modules touch with no gap.
- Nothing rests above y = 0 unless deliberately elevated, and that is expressed in data.
- Determinism tests still pass.""")

add("Loot is flung across the map by physics at spawn",
    "physics,bug",
"""Reported as item 16 (which proposes the fix), and found independently while wiring the authored
loot prefabs.

## Problem
Spawned loot is ejected hard by PhysX and ends up in the air or outside the world, instead of
resting where it was placed.

## Measured
Pieces outside a sane y range (below -0.5 m or above 12 m) after a raid settles:

| Configuration | Out of position |
|---|---|
| Baseline (current) | 7 / 19 |
| `ScatterRadius` 3 m -> 1 m | 5 / 18 |
| Raycast-for-floor settling (reverted) | 15 / 22 - **worse** |

Worst observed positions: y = -1945 (falling forever) and y = +33.

## Cause
`LootPlacementPlanner` is a **pure** function by design - deterministic, no scene access, so every
peer derives the same haul from the replicated seed. It therefore knows a room's centre but not the
shape of its mesh, and places loot 0.5 m above the room origin scattered up to 3 m. Against the old
flat placeholder floor that was always safe; against real room geometry a piece can spawn inside a
wall or a prop, and a rigidbody created overlapping a collider is depenetrated at speed. Reducing
scatter barely helped, which means props at the room centre are the dominant cause.

An earlier attempt to raycast for the floor made it worse by landing loot on room roofs, and was
reverted.

`Physics.SyncTransforms()` is already called in `LootSpawner.SpawnFor` before spawning, because the
rooms are instantiated in the same frame and PhysX does not otherwise see their colliders.

## Proposed fix (from the report)
Spawn loot kinematic / with physics disabled for ~0.5 s, then hand it to physics, so PhysX never
sees an overlapping body at creation. Note `LootPickup` already manages `isKinematic` for carrying
and nothing currently un-freezes placed loot, so this needs an explicit release rather than just
spawning kinematic.

## Constraints
- `LootPlacementPlanner` must stay pure and deterministic; any scene probing belongs in `LootSpawner`.
- `RogueAi.Tests.RaidLoopTests` covers plan/spawn agreement and must keep passing.

## Acceptance criteria
- 0 of ~19 pieces out of position across several seeds.
- Loot rests visibly on a surface inside its room.
- Determinism tests still pass.""")

add("No portal - no asset and no way to travel to or from the lair",
    "art,gameplay,enhancement",
"""Reported as item 17.

## Problem
There is no portal asset and no portal in the world. Extraction is an invisible trigger volume, and
the return to the lair is a UI state change rather than something in the fiction.

## Context
- `ExtractionZone` is built in `RaidSceneBuilder` as a `BoxCollider` trigger with a flat green quad
  as a marker, one cell outside the curtain wall.
- Returning to the lair happens through `GameState.Lair` when `RaidDirector.RaidResolved` fires -
  see `docs/4-systems/raid-scene-assembly.md`.

## Acceptance criteria
- A portal model exists and is placed at the extraction point.
- Entering it is what triggers extraction, replacing the bare trigger box.
- It reads as active/inactive to match extraction availability.""")

add("No VFX or SFX anywhere - spells, attacks, UI and casting failures are all silent",
    "vfx,audio,enhancement",
"""Reported as item 18.

## Problem
The game has no visual or audio feedback of any kind: no spell effects, no enemy attack effects, no
UI sounds, and nothing at all for a casting failure.

## Context
- There is an acoustics *simulation* (`RogueAi.Acoustics`: `AcousticEmitter`, `NoiseEvent`,
  occlusion attenuation) that guards and the alarm listen to - but it is a gameplay signal and plays
  no audible sound.
- `MisfireEngine` has a full set of near-miss words per spell (AGNIS for IGNIS, FRANCO for FRANGO,
  ...) and resolves them, but a misfire is neither seen nor heard, so the mechanic is invisible.
- `LootPickup._brokenVfx` is an unassigned hook for shattering loot.

## Acceptance criteria
- Spell cast, spell resolution and misfire each have a sound and an effect.
- Enemy attacks and hits have both.
- UI (button press, menu transition, extraction called) has audio feedback.
- Audio routes through mixer groups rather than raw `AudioSource` volume.""")

add("Castles look bland",
    "art,enhancement",
"""Reported as item 19.

## Problem
The castle reads as flat grey boxes. There is no material variation, lighting interest or dressing
to make rooms feel distinct from one another.

## Context
- 25 room models exist and are used, and interiors do contain props (beds, tables, chests, an altar,
  pillars) - but they render very dark and near-monochrome in play.
- Scene lighting is a single directional light at intensity 0.9 created by `RaidSceneBuilder`; no
  local lights, no baked lighting, no ambient treatment.
- The project is on URP 17.3.0 with no post-processing stack configured.

## Acceptance criteria
- Rooms are visually distinguishable by zone (crypt vs keep vs bailey).
- Materials show variation rather than one grey.
- Lighting and post-processing give interiors depth; the Watchman's lantern is a light source.""")

add("EPIC: build the game loop end to end so there is something to actually play",
    "epic,gameplay",
"""Reported as items 20 and 21 - tracked as one issue because they are the same thing stated as
problem and as solution.

## Problem
There is no game. The systems exist and are individually tested, but nothing joins them into a loop
a player can complete and want to repeat.

## Target loop
Lair -> set out -> enter the castle -> find and pick up loot -> be found by enemies and fight or
evade -> carry the haul to the portal -> extract -> return to the lair -> takings pay down the debt
-> set out again, deeper.

## What already works
- Menu -> lair -> Set Out -> raid -> back to the lair on resolve.
- Seed-deterministic castle, loot placement and garrison, all built from authored art.
- Guards hear, see, investigate and chase; the alarm escalates Calm -> Stirred -> Roused -> HueAndCry.
- Loot has worth, bulk and fragility; heavy items need two people; extraction tallies worth and
  applies it to the debt.
- Push-to-cast resolves eight spells including misfires.

## What is missing before the loop is playable
Each is tracked separately; this issue is done when they are:
- Combat that can be won or lost - health and damage, and feedback for both.
- Loot that can be found and picked up without fighting the physics.
- A castle you can actually walk through - connectivity and grid spacing.
- A portal to extract through.
- Feedback: animation, VFX, SFX, crosshair, locked cursor.

## Acceptance criteria
- A player can complete the full loop twice in a row without an operator explaining anything.
- Each stage has a fail state as well as a success state.
- The reason to go again is legible in the fiction (the debt), not just in the numbers.""")

add("Player spawn can be inside or flush against castle geometry",
    "gameplay,bug",
"""Found while verifying the room orientation fix.

## Problem
The player spawns at a fixed world position regardless of what the seed placed there, so the
first-person view can start half inside a wall.

## Context
`Assets/_Project/Scripts/Editor/RaidSceneBuilder.cs` places the player at
`new Vector3(CellSize * 5f, PlayerHeight * 0.5f, 0f)` - a hardcoded point one cell inside the
extraction zone at `CellSize * 6f`. That was harmless when rooms were short placeholder boxes; now
that rooms are full height and correctly oriented, whatever the generator placed at that cell is
solid geometry around the spawn.

Observed in play mode: the starting view is inside/under a structure with room roofs overhead.

## Acceptance criteria
- The player spawns in open, walkable space for any seed.
- The spawn is derived from the generated layout (e.g. just outside the gatehouse, or on the NavMesh
  near the extraction point) rather than hardcoded.
- Spawning never places the player inside a collider.""")

created = []
for title, labels, body in ISSUES:
    r = subprocess.run(["gh", "issue", "create", "--title", title, "--label", labels,
                        "--body-file", "-"],
                       input=body, capture_output=True, text=True)
    if r.returncode != 0:
        print("FAILED:", title, "|", r.stderr.strip()[:200])
        continue
    url = r.stdout.strip().splitlines()[-1]
    num = url.rsplit("/", 1)[-1]
    created.append({"number": num, "title": title, "url": url})
    print("#%-3s %s" % (num, title))

out = os.path.join("docs", "generated", "github-issues.json")
os.makedirs(os.path.join("docs", "generated"), exist_ok=True)
with open(out, "w", encoding="utf-8") as f:
    json.dump(created, f, indent=2)
print("\ncreated %d issues -> %s" % (len(created), out))
