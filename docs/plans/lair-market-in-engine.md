# The Lair and the Market: what exists, and what is left before they can be played in Unity

**2026-10-07.** Inventory taken on `claude/staging-2026-10-07`. Design: `docs/plans/diegetic-ui-lair-market.md`
(approved 2026-10-06, #284).

## Done

| What | Where | State |
|---|---|---|
| The design, approved with every proposed answer | `docs/plans/diegetic-ui-lair-market.md` (#285) | done |
| Concept art: Lair, Market, one haggle | `docs/art/concept/lair/` | done |
| Lair models: cellar, portal arch, ledger table, ledger, strongbox, century dial (stand + 4 turning rings), weapon rack, candle | `Assets/_Project/Art/Models/Lair/` (#288) | built and validated in Blender; never imported into Unity |
| Market models: yard with well, fence cart, goldsmith stall, anvil, pardoner booth, antiquarian cabinet, counter, slate board, scales (base, tipping beam, pan), lantern, coin, coin stack, pouch | `Assets/_Project/Art/Models/Market/` (#293) | built and validated in Blender; never imported into Unity |
| Blender reference layouts of each room | `Tools/AssetPipeline/render_lair_scene.py`, `render_market_scene.py`, README "Lair" and "Market" | done; placements are the reference for the Unity scenes |
| Lair bookkeeping that already existed | `Runtime/Lair/LairHubManager.cs`, `LairState.cs` | debt, banked gold, chosen Age, saved per slot; **debt is one shared number, not per wizard** |
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
