# Scene flow: Bootstrap, Lair and Raid

**Status: built for solo play, co-op not yet checked (2026-10-29, #536).** `SceneSplitTool` split RaidScene into
`Bootstrap.unity`, `Lair.unity` and `RaidScene.unity`, first in the build in that order. `SceneFlow` (Bootstrap) loads
the Lair and raid scenes on the host when the server starts, and unloads and reloads the raid scene each time a raid
ends. Checked solo in the Editor: menu (Bootstrap only), Lair (player at 1005, 1.2, 1.5), set out (63 rooms), return
(raid scene reloaded, 0 rooms, 0 fracture parts or effects), set out again (63 rooms). Two changes from the plan: the
campaign manager `LairHubManager` lives in Bootstrap with the raid director (it is data and saves, no room), and the
Lair and Market rooms keep their 1000 m and 1100 m offsets and stay loaded during a raid, so the two scenes never
overlap and the Lair is not reloaded each trip. The Ophan Gym and the co-op scripts still use the old combined scene
(#542).

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
