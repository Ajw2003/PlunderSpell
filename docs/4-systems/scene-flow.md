# Scene flow: Bootstrap, Lair and Raid

**Status: built, co-op checked (2026-10-29, #536).** `SceneSplitTool` split RaidScene into `Bootstrap.unity`,
`Lair.unity` and `RaidScene.unity`, first in the build in that order; the Ophan Gym (`OphanGym.unity`) is a raid scene
of its own, rebuilt from RaidScene and loaded in its place when the co-op launcher sets SceneFlow's override. `SceneFlow`
(Bootstrap) keeps exactly one of the Lair and the raid scene loaded (Decisions, "One scene at a time, behind a loading
screen"): setting out unloads the Lair and loads a fresh raid scene, a raid ending unloads it and loads the Lair. Each
machine keeps its loading screen up until its own castle is built or it is in the Lair. The screen fades in over
0.2 s in the scene being left, and nothing unloads until every machine has confirmed its screen is up (3 s timeout,
logged); it holds 0.4 s in the new scene, then fades out over 0.4 s (`SceneFlow.CoverUntilReady`). Before this,
an observers RPC that PurrNet does not run at once on the host let a frame of empty scene show (aj's clip,
2026-10-29). Co-op run `split-2` (Editor
host, built client): both machines built the same castle (63 rooms, 246 breakable parts), and after the return both
had 0 rooms, 0 parts and 0 debris effects. `LairHubManager` (campaign data and saves) lives in Bootstrap with the raid
director. A client is built from the whole build list, because PurrNet names scenes by build index.

## The three scenes

| Scene | Loaded | Holds |
|---|---|---|
| `Bootstrap.unity` | First, never unloaded | Network manager, game state, event manager, the players |
| `Lair.unity` | On top of Bootstrap while players are home | Lair room and Market yard 1000 m and 1100 m out, their doors, the portal, ledger, century dial, strongbox, market counters, haul landing |
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
