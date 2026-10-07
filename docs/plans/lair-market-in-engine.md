# The Lair and the Market: what exists, and what is left before they can be played in Unity

**2026-10-07.** Inventory taken on `claude/staging-2026-10-07`. Design: `docs/plans/diegetic-ui-lair-market.md`
(approved 2026-10-06, #284).

## Done

| What | Where | State |
|---|---|---|
| The design, approved with every proposed answer | `docs/plans/diegetic-ui-lair-market.md` (#285) | done |
| Concept art: Lair, Market, one haggle | `docs/art/concept/lair/` | done |
| Lair models: cellar, portal arch, ledger table, ledger, strongbox, century dial (stand + 4 turning rings), weapon rack, candle | `Assets/_Project/Art/Models/Lair/` (#288) | built and validated in Blender; `.meta` files committed; import checked 2026-10-06 (scale in metres, no missing materials) |
| Market models: yard with well, fence cart, goldsmith stall, anvil, pardoner booth, antiquarian cabinet, counter, slate board, scales (base, tipping beam, pan), lantern, coin, coin stack, pouch | `Assets/_Project/Art/Models/Market/` (#293) | built and validated in Blender; never imported into Unity |
| Blender reference layouts of each room | `Tools/AssetPipeline/render_lair_scene.py`, `render_market_scene.py`, README "Lair" and "Market" | done; placements are the reference for the Unity scenes |
| Lair bookkeeping that already existed | `Runtime/Lair/LairHubManager.cs`, `LairState.cs` | debt, banked gold, chosen Age, saved per slot; **debt is one shared number, not per wizard** |
| The Lair room prefab (step 2, #308) | `Editor/LairRoomForge.cs` → `Prefabs/Lair/LairRoom.prefab` | built 2026-10-06; render from the Blender camera `docs/art/models/lair/lair-unity.png` (`Tools/Unity/eval/lair_room_capture.cs`) matches `lair-assembled.png`. Axis map is `(-X, Z, -Y)`, not the README's castle rule. No portal glow sheet yet |
| Getting there and back (step 3, #309) | `GameState.LairRoom`, `Runtime/Raid/LairRoomSpawner.cs`, `LairPortalTrigger.cs`, `LairLedgerHandle.cs` | done 2026-10-06: sessions start in the room, the portal sets out, extraction returns to it, E at the ledger opens the Lair screen. Solo checked in Play mode; co-op not checked. See `docs/4-systems/raid.md` |
| The haul lands in the room (step 4, #310) | `HaulExtracted` event, `Runtime/Raid/HaulLanding.cs`, `HaulLayout.cs`, `LootSpawner.SpawnPile`, `HaulLanding` child in `LairRoomForge.cs` | done 2026-10-06: the last raid's unbroken pieces spawn on the Lair floor at x 4.0 and are replaced by the next haul or cleared when a raid starts; banking still automatic; not saved. Solo checked in Play mode (gold 2800 to 5200, 2 pieces at the landing); co-op and a capture not checked |
| The Market yard prefab (step 5, #311, first half) | `Editor/MarketYardForge.cs` → `Prefabs/Market/MarketYard.prefab`, from `Tools/AssetPipeline/placements/market.json` (written by `render_market_scene.py --placements`, so Unity and the render share one placement code) | built 2026-10-06: 47 models, 10 lantern lights, 4 spawns inside the south way in; render `docs/art/models/market/market-unity.png` matches `market-assembled.png`. Placed in `RaidScene` at (1100, 0, 0) beside the Lair (1000, 0, 0) by `Tools/Plunderspell/Place Lair And Market In Raid Scene`. Not yet reachable: the Market door and the way back are next. Windows and lanterns are not emissive yet |
| The Market door and the way back (step 5, #311, second half) | `Runtime/Raid/RoomTravel.cs`; `LairRoomForge` (door collider, `MarketDoorArrivals`), `MarketYardForge` (`LairExit`), `RaidSceneRooms` (connects them) | done 2026-10-07, solo Play mode: looked at the door from 2.2 m, travelled to the Market's Spawn1 (1101.5, 8.0) facing the well; stood in the gate, returned to (1002.2, -3.8) just inside the Lair door. Captures `docs/generated/market-door-2026-10-07/`. Co-op not checked |
| The haggle rules (step 6, #312, first part) | `Runtime/Market/Haggle.cs`, `HaggleRules.cs` (assembly `Plunderspell.Market`, no Unity references) | written 2026-10-07: limit, opening offer, Plus/Satis/Vale, patience per vendor (Fence 1, Goldsmith 3, Pardoner 3, Antiquarian 4), the 10% lower opening on a second visit. `dotnet test Tools/MarketRules/MarketRules.Tests.csproj`: 11 passed. Not wired to counters, vendors or voice yet. **Open question:** the design's "2 patience if you asked for more than 1.2 x L" can never happen, because a Plus asks 1.1 x an offer that never exceeds L; it is left out until asks can be larger (a bigger ask word, or Plus stacking) |
| Lair screen that already existed | `Runtime/UI/Screens/LairScreen.cs` | a flat menu screen; this is what the room replaces |

## Left to do, in order

1. **Import.** Open the project in Unity so the 27 new models import and get their `.meta` files; check scale (a 1.8 m
   player beside the 2.16 m Lair door) and materials. Commit the `.meta` files.
2. **Lair scene.** A scene (built by an Editor script, as the castle benches are) with the cellar and every prop placed
   as in the Blender reference, lights for the hearth and candle, colliders, and a spawn point for each player.
3. **Getting there and back.** Start a session in the Lair room instead of on the Lair screen; the portal arch starts a
   raid (today the screen's button does); extraction brings players back to the room.
4. **Loot comes home as objects.** Today the haul is turned into a number at extraction. It has to be kept as a list
   of pieces and spawned on the Lair floor in front of the portal.
5. **The Market scene** and the Market door in the Lair that leads to it, with the yard, stalls, counters and slates
   placed as in the Blender reference.
6. **Selling.** Put a piece on a counter → the vendor's offer → *Plus / Satis / Vale* (voice, bindable to keys) → coins.
   The rules are written in the design (worth, hidden limit, patience per vendor). Vendors can be a placeholder figure
   plus subtitles at first.
7. **Coins and the debt.** Coins as physical pouches; a strongbox per wizard banks them; the ledger shows each wizard's
   debt (needs the shared debt split per wizard, #130); the Collector takes what is due after the Market.
8. **Co-op.** Everything above replicated for four players over PurrNet (who holds what, coins, ledger).

Steps 1 and 2 alone give a walkable Lair; 1, 2 and 5 a walkable Lair and Market; 3 to 7 make them playable.
