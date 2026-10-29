# Scene flow: Bootstrap, Lair and Raid

**Status: planned, not built.** Approved 2026-10-29 (Decisions, "Design approved: the Lair and the Market in their own
scene"). Until it is built, the Lair and the Market are rooms inside `Assets/_Project/Scenes/RaidScene.unity`, placed
by `Assets/_Project/Scripts/Editor/RaidSceneRooms.cs:15-16` at x = 1000 m and 1100 m and joined by `RoomTravel`
teleports, and no gameplay code loads a scene.

## The three scenes

| Scene | Loaded | Holds |
|---|---|---|
| `Bootstrap.unity` | First, never unloaded | Network manager, game state, event manager, the players |
| `Lair.unity` | On top of Bootstrap while players are home | Lair room and Market yard at the origin, their doors, the portal, ledger, century dial, strongbox, market counters, haul landing |
| `RaidScene.unity` | On top of Bootstrap during a raid | Castle generator, loot and guard spawners, the Ophan, extraction |

Both machines change scenes together through PurrNet's scene module
(`Assets/PurrNet/Runtime/CoreModules/Scenes/ScenesModule.cs`, `LoadSceneAsync`, unused before this).

## The loop

1. The game starts in Bootstrap and loads the Lair scene.
2. While players are in the Lair, the next raid scene loads in the background, switched off.
3. The portal switches the raid scene on and puts players at its arrival point; the Lair scene unloads.
4. A raid ends (extraction, or everyone down): extracted loot is handed to the haul pile, the Lair scene loads, and the
   raid scene unloads. Everything in it goes, whatever spawned it.

## What crosses between scenes

- **Players** live in Bootstrap and are never despawned.
- **Purses, debt, haul pile** are saved per slot (`LairHubManager.cs:87-130`, `HaulPileSave.cs`) and rebuilt by the
  Lair scene.
- **Extracted loot** is a network object in the raid scene; extraction hands it to the haul pile before the raid scene
  unloads.
- **Links** between objects in different scenes go through static registries or events, never a scene search. The
  five searches to replace are listed in the parent issue.

## Steady frames

Scene loads are asynchronous and in the background; the owner's rule is no hitches, so each scene change logs its
longest frame, and the co-op check fails above 17 ms.
