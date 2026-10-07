# Market and haggling

The Market yard is where the haul becomes coins. Design: `docs/plans/diegetic-ui-lair-market.md`
("The Market, and haggling"); decisions: `docs/6-decisions/Decisions.md` 2026-10-07 (coins come only
from selling; *Plus* is the only ask; keys first, voice later).

## Pieces

| Piece | File | Owns |
|---|---|---|
| Haggle rules | `Runtime/Market/Haggle.cs`, `HaggleRules.cs` | Pure C# (no Unity): limit, opening offer, *Plus* / *Satis* / *Vale*, patience, the 10% lower opening on a second visit. `dotnet test Tools/MarketRules/MarketRules.Tests.csproj` |
| Counter | `Runtime/Raid/SellCounter.cs` | One counter and its vendor: finds a piece resting on the counter top, opens the haggle, takes the answer, banks the sale |
| Lines | `Runtime/Raid/VendorLines.cs` | Each vendor's colour and subtitle wording (placeholder text) |
| Prefab | `Editor/MarketYardForge.cs` | Adds a `SellCounter` to each `MarketCounter`: the vendor is the one whose stall model (cart, stall, booth, cabinet) stands nearest; a trigger box on the top; a capsule figure (no collider) a metre behind towards the stall; a `TextMesh` subtitle above it |

## How one haggle runs

1. A loot piece lies still inside the counter's trigger box (not carried, speed under 0.2 m/s) and
   that vendor has not refused it tonight. The vendor says the opening offer as a subtitle.
2. Within 3 m of the counter, the local player presses **1 Plus**, **2 Satis**, **3 Vale**
   (`SellCounter.Answer` is the same call, for tests and tools). Saying the word aloud does the same: `HaggleVoiceRouter.Hear` calls `SellCounter.Speak` (`SellCounter.cs:150`) on the nearest counter whose `ListeningDistance` (`:90`) is finite (haggle open, player within 3 m); see voice.md.
3. *Plus*: the offer rises 10% (`Raised`), or the vendor refuses (`Refused`) and loses patience; at
   zero patience he will not take that piece for the rest of the night (`WillNotBuy`). The Fence has
   patience 1, so his first over-limit *Plus* ends it.
4. *Satis*: `LairHubManager.BankSale(coins)` (coins are the figure said, rounded), the piece is removed
   from the haul pile (`HaulLanding.Remove`, so the save drops it) and from any raid spawner, and
   destroyed. *Vale*: "Farewell"; lifting the piece off and putting it back opens 10% lower.
5. A piece taken off the counter without a word just closes the haggle.

## The numbers, as wired

- **worth** = `LootValue.Worth` (the item's `Worth`, 0 once ruined). A piece has no partial
  condition yet, so a ruined piece is worth nothing and the vendor says so.
- **interest** = `HaggleRules.Interest(vendor, category)` (#313), from the piece's `LootItem.Category`
  (`LootCategory`: Metal, Holy, Curio, Arms, Other; the enum lives in `Runtime/Market/HaggleRules.cs` so the
  rules stay free of Unity). The Fence 1.0 for everything; the Goldsmith 1.3 for Metal; the Pardoner 1.3
  for Holy; the Antiquarian 1.3 for Curio and Arms; otherwise 0.7. Tested in `HaggleTests`. Not yet applied:
  "out of its era" (no era on a piece). Checked solo, 2026-10-07: one golden goblet (Metal, worth 150) opened
  at 100 (limit 181.2) at the Goldsmith's and at 60 (limit 94.1) at the Pardoner's; `Tools/Unity/eval/market_category_offers.cs`.
- **categories**: set by `Editor/LootCategoryForge.cs` (menu *Tools/Plunderspell/Set Loot Categories*), which holds the
  table below; the generated placeholders under `Data/Generated` each exist twice (`<name>` and `<name> 1`), same category.

| Category | Pieces |
|---|---|
| Metal | Silver Service Tureen, Gold Death-Mask, Oxhide Ingot, Tripod Cauldron, Coin Coffer, Silver Ewer, Gilded Nef, Jewelled Hat-Badge, Copper Pot, Golden Goblet, Conjured Coin, Silver Plate, Crown of the Founder, Tin Cup |
| Holy | Arm Reliquary, Gilded Altarpiece, Illuminated Psalter, Glass Reliquary |
| Curio | Astrolabe, Cabinet of Curiosities, Nautilus Cup, Venetian Mirror, Faience Hippopotamus, Ancient Relic |
| Arms | Parade Armour on its Stand, Arming Sword, Bronze Sword, Crossbow, Flintlock Pistol, Longsword, Matchlock, Pavise Shield, Plate Helm, Powder Grenade, Round Shield |
| Other | Sealed Amphora, Banker's Ledger, Rolled Tapestry, Heavy Chest, Gilded Chest |

## The test save slot (#313)

The checks that play a campaign (`coop_lair_check.sh`, `coop_carry_check.sh`, `hud_events_check.sh`) play in
`SaveSlots.TestSlot` (99), not the owner's slot 1. The Main Menu steps through slots 1 to `SaveSlots.Count` (3) only, so it
never offers 99; `SaveSlots.Active` accepts it and every other out-of-range number still clamps. `Tools/Unity/test_slot.sh`
remembers the active slot, wipes slot 99 (campaign and haul pile) and makes it active before Play, and puts the old one
back when the check ends (its exit trap), so every run starts from an empty pile. If a check is killed outright the
active slot stays 99 until the next check or `SaveSlots.Active = 1`. Other Editors on this PC share the same
PlayerPrefs, so a run elsewhere that is not on the test slot can still change slot 1.
- **mood**: one roll per vendor per night. A night runs from setting out to setting out
  (`RaidPhaseChanged` to `Generating`); each counter draws a seed then and derives its mood from it.
- **Opening roll**: seeded from the night and the piece, so the same piece opens the same all night
  and coming back is exactly 0.9 times the first offer.
- **Walked away** and **refused** are remembered per piece instance on the counter, and forgotten at
  the next night.

## What must stay true

- The sale runs on the server (solo and host) only; see "Co-op" below.
- `NetworkPrefabs.asset` is unchanged: the counters are scene network objects, not prefabs.
- Each haggle's lines live in `VendorLines`, not in the component.

## Co-op (#314)

- `SellCounter` is a PurrNet `NetworkBehaviour` on each `MarketCounter`, with a scene `NetworkIdentity`
  that `MarketYardForge.AddSellCounter` adds (`MarketYardForge.cs:177`). The prefab sits in `RaidScene`, so
  PurrNet numbers it as a scene object; no network prefab, so `NetworkPrefabs.asset` does not change.
- The server owns the `Haggle` and the sale. `Decides` (`SellCounter.cs:84`) is "unspawned or server":
  those sides open, answer and sell as before. A client's `Speak` (`SellCounter.cs:124`, what keys
  1/2/3 call) sends the word with `[ServerRpc(requireOwnership: false)] WordToServer` (`:133`); a client
  reads the keys only while a line is showing and it stands within 3 m.
- Every line is said once on the server (`Say`), shown there and sent to all by `[ObserversRpc]
  LineToObservers` (`:242`), so host and client read the same text. Each side clears it after 6 s.
- `BankSale` runs on the server only; the piece is removed from the pile save and destroyed there, which
  despawns it on every client.
- The subtitle stands half a metre in front of the vendor (toward the counter) at chest height, under
  the stall roof (`MarketYardForge.cs:164`). Each counter top has a 4 cm rim (`AddLip`, `:182`) so a piece set
  near the edge stays on it.
- Checked: `Tools/Unity/coop_lair_check.sh` (selling part, `eval/coop_lair.cs`): the host puts a piece on the
  Goldsmith's counter, the client's Plus then Satis go through `Speak`; both sides show the same lines,
  the host's gold plus debt moves by the coins sold, the piece is gone on both. Logs and the client's
  capture of the counter in `docs/generated/coop-lair-2026-10-07/`. The 1/2/3 keys themselves are still
  not machine-checked.

## Checked

Solo Play, 2026-10-07: `Tools/Unity/eval/market_spawn_piece.cs` puts a golden goblet on a counter,
`SellCounter.Answer` drives it; captures in `docs/generated/market-haggle-2026-10-07/`. The 1/2/3 key
path is not machine-checked: legacy `Input` cannot be injected from the Editor tools.
