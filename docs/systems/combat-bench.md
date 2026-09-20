# Combat bench

A one-room arena for trying a weapon, a spell or an enemy in about two seconds, instead of starting
a raid and walking a castle until something turns up. Issue
[#18](https://github.com/Ajw2003/PlunderSpell/issues/18).

| | |
|---|---|
| Scene | `Assets/_Project/Scenes/CombatBench.unity` |
| Built by | `Assets/_Project/Scripts/Editor/CombatBenchSceneBuilder.cs` — *Tools ▸ Plunderspell ▸ Build Combat Bench Scene* |
| Runtime | `CombatBench`, `CombatBenchHud` (assembly `RogueAi.Playtest`) |
| Screenshots | `Assets/_Project/Scripts/Editor/CombatBenchScreenshotForge.cs` → `docs/generated/combat-bench-screenshots/` |
| Tests | `Assets/_Project/Scripts/Tests/Runtime/CombatBenchTests.cs` |

## How it works

A 40 m walled box, the raid's own player rig at the centre, an `AlarmFSMManager` so a guard's shout
escalates the way it does in a raid, and an `ArmingSword` on the floor two metres in front of the
player. `CombatBench` holds the `EnemyRoster` — the same asset the raid garrisons its castle from —
and spawns from it on demand. `CombatBenchHud` is the panel: enemy, count (1–8), starting alert
state, Spawn, Clear. `F1` hides it.

Enemies spawn evenly around a 7 m ring centred on the player, each rotated to face the middle, so a
spawn of five is five things already looking at you rather than five things stacked on one point
for physics to fling apart.

### The bench puts itself into play

`CombatBench.Start` calls `GameServices.GameState.ChangeState(GameState.Playing)`. This is
load-bearing, not convenience. `GameStateManager` starts at `MainMenu`, every input path gates on
`GameServices.IsPlaying`, and `UIBootstrapper` creates the main menu in *every* scene it loads — so
without this line the bench opens with a menu over it and a body that will not move.

### Why the raid's rig, not the playtest harness

The bench is built with `PlayerStateMachine` + `PlayerInputController`, the pair `RaidScene.unity`
carries — not `FreeLookPlaytestController`.

Melee runs through `PlayerStateMachine.Attack` → `ItemManager.TryMeleeSwing` →
`MeleeWeapon.TrySwing`. `FreeLookPlaytestController` has no attack path at all, so a bench built on
it could exercise spells and nothing else, and would prove nothing about the melee it exists to
test. `ItemManager` and `EventManager` are scene-owned singletons, so the builder places both;
without `ItemManager` in the scene, `ItemManager.Instance` is null and a swing silently does
nothing.

### The bench carries its own spell VFX

`CombatBenchSceneBuilder` gives the player the same voice-cast pipeline `RaidPlayer.prefab`
carries — `PushToCastController` and `SpellCastingSystem`, wired to the project's one
`SpellLexicon.asset` via `SpellCastingSystem.SetLexicon` — and builds a `SpellVfxDirector`
(pointed at `Bolt.prefab`) directly into the scene, rather than leaving either to a manual
"Tools ▸ Plunderspell ▸ Install Spell VFX" pass. The bench and the raid are proving the same
system, not two different ones: a spell that works on one works on the other.

This matters because the builder rebuilds the scene from `NewScene(EmptyScene)` every run — a
component added by a separate one-off tool (like the old `SpellVfxInstaller` pass) would be
silently wiped the next time someone rebuilds the bench. Building it in-line means the bench
can never regress to "no caster, no VFX" just by being regenerated.

### The bench has its own crosshair

`RaidHudView`'s crosshair moved into its own `CrosshairView` component (`Assets/_Project/Scripts/
Runtime/UI/RaidHud/CrosshairView.cs`) so the bench can carry it without carrying the rest of
`RaidHudView` — debt, banked gold, the alarm bar, "The Lair"/"Raiding" phase text — none of which
mean anything on a scene with no `RaidDirector` or `LairHubManager`. `CombatBenchSceneBuilder` adds
`CrosshairView` directly; `RaidHudView` now requires and delegates to it instead of drawing its own.
One crosshair implementation, not two that could drift apart. It stays idle (no bracket swap) on the
bench: nothing there sets `CrosshairView.HasTarget`, since the bench has no `LootInteractor`.

### The panel is IMGUI

For the same reason `RaidHudView` is: the bench is a code-built scene with no authored prefabs, and
IMGUI needs none. The cost is the same too — `Camera.Render()` never invokes `OnGUI`, so the
screenshot forge photographs the arena and the enemies in it but **can never show the panel**. See
`docs/Decisions.md`, "The crosshair is IMGUI, and therefore invisible to the screenshot test".

## Invariants

- **The bench never ships enabled in a raid.** It is one scene and two components in
  `RogueAi.Playtest`; nothing in `RogueAi.Raid` references it.
- **`CombatBench.ClearSpawned` removes only what the bench spawned.** The arena, the player and the
  sword are not in `Spawned` and must not be destroyed by a Clear.
- **The roster is shared, not copied.** The bench spawns from
  `Assets/_Project/Data/Enemies/EnemyRoster.asset`, so an enemy that works on the bench is the same
  prefab the raid garrisons with.

## Traps

- **`CastleGuard.SetAlertState` is a dev and test seam, not gameplay.** It writes `_state`, a
  PurrNet `SyncVar`, directly. That is safe on the bench because there is no network session, and
  wrong anywhere the AI owns its own transitions.
- **Guards steer, they do not path.** The bench has no baked NavMesh, so `CastleGuard` falls back
  to `Steer` — straight-line movement that walks into walls. Expected here; see `raid.md`.
- **`Spawn` is edit-mode safe but `Start` is not.** The screenshot forge calls `Spawn` on a scene
  that is open but not playing, so the spawn works while the play-state change does not. Anything
  the bench does that depends on `Start` having run will not hold in a forge capture.

## Verification

Both run headlessly on Unity 6000.3.15f1. The capture must **not** pass `-nographics`.

```
Unity.exe -batchmode -quit -projectPath <path> \
  -executeMethod RogueAi.EditorTools.CombatBenchSceneBuilder.BuildCombatBenchScene
Unity.exe -batchmode -quit -projectPath <path> \
  -executeMethod RogueAi.EditorTools.CombatBenchScreenshotForge.CaptureAll
```

`CombatBenchTests` covers the play-state change, the spawn count, the ring spacing, the starting
alert state, and that Clear empties the list.

## Not done

- **No main-menu button.** `Plans/Issue_18_Plan.md` asks for one. `MainMenuScreen` is shipping UI
  and the scene is not in `EditorBuildSettings`, so the bench is opened from the Project window or
  the Tools menu for now.
- **No ranged weapon on the bench.** There is nothing to put there yet — issue
  [#39](https://github.com/Ajw2003/PlunderSpell/issues/39).
