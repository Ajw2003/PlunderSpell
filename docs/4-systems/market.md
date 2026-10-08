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
| Prefab | `Editor/MarketYardForge.cs` | Adds a `SellCounter` to each `MarketCounter`: the vendor is the one whose stall model (cart, stall, booth, cabinet) stands nearest; a trigger box on the top; a capsule figure (no collider) a metre behind towards the stall; a chalk slate on the counter (`CounterSlateBuilder.cs`) |

## How one haggle runs

1. A loot piece lies still inside the counter's trigger box (not carried, speed under 0.2 m/s) and
   that vendor has not refused it tonight. The vendor's opening offer is written on his counter's slate.
2. Within 3 m of the counter, the local player presses **1 Plus**, **2 Satis**, **3 Vale**
   (`SellCounter.Answer` is the same call, for tests and tools). Saying the word aloud does the same: `HaggleVoiceRouter.Hear` calls `SellCounter.Speak` (`SellCounter.cs:150`) on the nearest counter whose `ListeningDistance` (`:90`) is finite (haggle open, player within 3 m); see voice.md.
3. *Plus*: the offer rises 10% (`Raised`), or the vendor refuses (`Refused`) and loses patience; at
   zero patience he will not take that piece for the rest of the night (`WillNotBuy`). The Fence has
   patience 1, so his first over-limit *Plus* ends it.
4. *Satis*: a coin pouch holding the coins (the figure said, rounded) is put on the counter (`SellCounter.PutPouchOnCounter`,
   `SellCounter.cs:262`; see "Coins are pouches" below), the piece is removed
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
- The sale (and so the pouch's spawn) runs on the server only; the piece is removed from the pile save and destroyed there, which
  despawns it on every client.
- **Selling from the floor (#356, owner's choice).** A piece too heavy to lift alone (`Item.IsTooHeavyToLift`) can be towed
  to a counter's foot: each counter has a second trigger, `SellFoot` (`MarketYardForge.AddSellFoot`), on the floor in front
  of it on the player's side, as wide as the counter, 1.25 m deep and 1 m high. `SellCounter.OpenOnRestingPiece` looks in
  both boxes. `Tools/Unity/floor_sale_check.sh`: a 15 kg chest set there sold for 216 coin (`docs/generated/floor-sale-2026-10-07/`).
- The slate (#360) stands on each counter facing the player: a dark board, chalk-white TextMesh Pro in the ledger's Spectral
  font. Idle it names the vendor and what he buys dearly; in a haggle it shows his name, his line ("Very well. 114 coin.")
  and "1 Plus · 2 Satis · 3 Vale" (`SlateText.cs`, pure, `SlateTextTests`). `CounterSlate.cs` fades out, swaps and fades
  in over 1 s. It is fed from the same line every player already receives, so host and client read the same slate
  (`coop_lair_check.sh`). The floating `TextMesh` subtitle is gone. `Tools/Unity/slate_check.sh`,
  `docs/generated/counter-slate-2026-10-07/`. Each counter top has a 4 cm rim (`AddLip`, `:182`) so a piece set
  near the edge stays on it.
- Checked: `Tools/Unity/coop_lair_check.sh` (selling part, `eval/coop_lair.cs`): the host puts a piece on the
  Goldsmith's counter, the client's Plus then Satis go through `Speak`; both sides show the same lines,
  the host's gold plus debt moves by the coins sold, the piece is gone on both. Logs and the client's
  capture of the counter in `docs/generated/coop-lair-2026-10-07/`. The 1/2/3 keys themselves are still
  not machine-checked.

## Coins are pouches (#313, step 7 part 1)

A sale no longer banks. The vendor puts a `CoinPouch` on the counter (`CoinPouch.cs`, prefab
`Assets/_Project/Prefabs/Market/CoinPouch.prefab`, built by `CoinPouchForge.BuildCoinPouch`; the `MarketYardForge` wires
the prefab into each `SellCounter`, `MarketYardForge.cs:184`). Server-spawned, so it is a networked piece.

- Carried like loot: it has a `LootPickup`, an `Item` and a network transform. Its weight is 0.01 kg a coin with a 0.3 kg
  floor (`CoinPouch.WeightFor`, `CoinPouch.cs:21`), set on the body from the synced coin count, so a client's body weighs the
  same. A 130-coin pouch is 1.3 kg.
- **Not loot.** It has no `LootValue`, and that is the only thing extraction (`ExtractionZone`), the haul pile
  (`HaulLanding`, which saves `LootValue` items) and the counters (`SellCounter.OpenOnRestingPiece`) look for. A pouch on
  a counter never opens a haggle. Its `LootItem` (`Data/Market/CoinPouchItem.asset`, Worth 0) exists only for the carry numbers.
- **Strongboxes.** `LairRoomForge.AddStrongboxLid` (`LairRoomForge.cs:140`) gives each of the four `LairStrongbox` props a
  trigger slab on its lid carrying a `LairStrongbox` with a seat: strongbox n is seat n, the player whose owner id is n (as
  the spawns). A pouch let go inside (not in a hand) is banked on the server (`LairStrongbox.Accept`,
  `LairStrongbox.cs:29`): `LairHubManager.BankPouch(seat, coins)` (`LairHubManager.cs:177`) adds the coins to that seat's
  purse, publishes `PurseChanged`, and the pouch is destroyed (despawned everywhere).
- **Purses** are four saved numbers per slot (`Purse0` to `Purse3` in PlayerPrefs, `LairHubManager.Purse`/`PeekPurse`,
  wiped by `ResetSlot`).
- **Banking only fills the purse** (#313, second part; it no longer calls `BankSale`, `LairHubManager.cs:193`). The debt is paid
  only by the Collector (below).

## The Collector (#313, step 7 part 2)

The owner's decision (`Decisions.md`, 2026-10-07: equal shares) leaves the details open; these are the parent's defaults, to be
tuned. The debt stays one total (`LairHubManager.TotalDebt`, still growing 50 per setting out).

- **When:** once per setting out, on the host or solo, before the raid starts: `RaidDirector.StartRaid` calls `SetPresent` then
  `Collect()` (`RaidDirector.cs:229`), then `OnNewSession` raises the debt. A client's `StartRaid` returns early, so only the
  host collects.
- **Who is present:** seats with a player body (`RaidDirector.SeatsPresent`, `RaidDirector.cs:180`: owner id n is seat n-1; a lone
  offline player is seat 1). Between raids the host refreshes it twice a second (`RaidDirector.cs:173`) so the ledger can show it.
- **The arithmetic** is `Market/CollectorRules.cs` (no Unity; `Tools/MarketRules`, 8 tests): share = ceil(debt / wizards present);
  he takes min(purse, share) from each present seat, never more than the debt in all (rounding up cannot overcharge). A short
  purse is not covered by anyone else's; a friend covers a share by banking a pouch into that friend's strongbox.
- **Records:** `LairHubManager.Collect` (`LairHubManager.cs:237`) sets `PaidLast(seat)` (saved per slot: `PaidLast0` to `PaidLast3`,
  wiped by `ResetSlot`), pays the debt by the sum, logs the existing `DEBT_CLEARED` when it reaches 0, and publishes
  `PurseChanged`, `CollectorPaid`, `CollectorSpoke(line)`, `DebtChanged`. With nothing to take his line is "The Collector finds
  the purses empty." Anything left in a purse is that wizard's.
- **The ledger** (the ledger book's pages since #357/#359, `LedgerPageText.cs`; the Lair screen is gone): Owed (kept), then purses for each seat that is
  present or has a purse or paid last time (I to IV): Purse, Owes (the share due tonight), Paid (last collection), then Last raid.
  There is no Banked figure: sales no longer bank, so it would read 0 (the number still exists in the model).
- **His line** ("The Collector takes 120 from I, 80 from II.") is `LairHubManager.CollectorLine`, shown in place of the "Debt grows"
  note on the ledger book's page and logged: the company has already left the Lair when he speaks, so the book shows it on
  coming home. A spoken line is not done.
- **Clients:** a client's `LairHubManager` is its own local save, but its ledger book shows the host's ledger (#314):
  `RaidDirector._hostLedger`, one `SyncVar<string>` ("purses|paid|present|Collector line", `LairHubManager.HostLedger`), is
  shown by `ShowHostLedger`, which raises the same events as the host and saves nothing (the share is derived from debt and
  seats present). The host re-publishes on every purse or seat change. Checked twice in `coop_lair_check.sh`: after the
  Collector collects the client's debt, purses, paid, seats and line equal the host's, and the client's own slot (read from its
  PlayerPrefs, `activeslot`) is unchanged; capture `docs/generated/coop-lair-2026-10-07/run2-client-ledger.png`.
- Checked 2026-10-07: `Tools/Unity/collector_solo_check.sh` (`docs/generated/collector-solo-2026-10-07/`, ledger captures before
  and after) and `coop_lair_check.sh` twice (`docs/generated/coop-lair-2026-10-07/collector-run2-*`, `collector-run3-*`): banking
  paid no debt; with purses 100 and the sale's coins and debt 550 (share 275) the Collector took 100 and 275, the debt fell to
  225 (+50 for the raid is included), slot 1 unchanged.
- `NetworkPrefabs.asset` gained exactly one entry, the CoinPouch prefab (guid `e6524070a36f7b64b81c47f26463a0a0`),
  appended after the existing ones; no existing entry moved.
- Not done (to be an issue if wanted): a pouch not yet banked is not saved, so quitting with one on the counter loses its
  coins; a client's own `LairHubManager` does not show the host's purses (it is local state, like gold and debt today).
- Checked 2026-10-07: `Tools/Unity/pouch_solo_check.sh` (solo, test slot; log and capture of the pouch on the counter in
  `docs/generated/pouch-solo-2026-10-07/`) and `coop_lair_check.sh` twice (`docs/generated/coop-lair-2026-10-07/pouch-run1-*`,
  `pouch-run2-*`): the pouch exists on both sides with the coins sold, the host moves it into strongbox 2 (the client's
  seat), both see it gone, purse 2 grew by the sale and slot 1 was unchanged.

## Checked

Solo Play, 2026-10-07: `Tools/Unity/eval/market_spawn_piece.cs` puts a golden goblet on a counter,
`SellCounter.Answer` drives it; captures in `docs/generated/market-haggle-2026-10-07/`. The 1/2/3 key
path is not machine-checked: legacy `Input` cannot be injected from the Editor tools.
