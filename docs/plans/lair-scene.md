# Proposal: the Lair as its own scene

Rendered version with floor plan and diagrams: `docs/plans/lair-scene.html`.

Status: **proposal, revision 2, awaiting approval** — nothing here is built yet. Written 2026-09-26
against `integration/staging-2026-09-15` at `ee13c2d`.

**Revision 2 (2026-09-26), after review:** the whole haul now comes home through the portal as
objects, not just weapons, and nothing is sold automatically. Every item is carried to a station and
either sold at the Buyer's Hatch or stored in the Hoard (weapons on the rack). The Lair gains a door
to the Mystical Market. The auto-picked trophy shelf is gone; the Hoard replaces it. See §4 and the
new decisions D5–D6.

---

## 1. What exists today, and the gap

The Lair is currently a *number*, not a *place*.

- `LairHubManager` (`Assets/_Project/Scripts/Runtime/Lair/LairHubManager.cs`) holds three values —
  debt (starts at 500, +50 every time you set out), banked gold, and the selected era — in
  `PlayerPrefs`. It is a bare component on an empty GameObject inside `RaidScene`
  (`RaidSceneBuilder.cs:307`).
- `RaidPhase.InLair` exists (`RaidPhase.cs:10`), but nothing is rendered during it.
  `RaidBootstrapper` auto-starts a raid 0.25 s after Play and F6 goes straight into the next one, so
  in practice the player never stands in the Lair at all.
- There is no scene transition anywhere in the project. The only `LoadScene` call is
  `PlayerDeadState.cs:13`, which reloads the current scene on death.
- The era choice changes nothing: `ProceduralCastleGenerator` never reads it. Every era builds the
  same medieval castle.
- `EditorBuildSettings` lists only `TestScene`. `RaidScene` is not in the build either.
- The engineering plan (`docs/plans/plunderspell.md:163`) called for "JSON to
  `Application.persistentDataPath`, host-authoritative". What was built is local `PlayerPrefs`,
  and in co-op every player currently keeps their own separate debt.
- **Loot is turned into gold at the moment of extraction.** `ExtractionZone.ComputeWorth` sums the
  worth of every unbroken `LootPickup` in the zone, `RaidDirector.ApplyResult` banks that number
  against the debt, and `LootSpawner.Clear()` destroys the objects. The items themselves never
  reach the Lair.
- **The Mystical Market does not exist on any branch.** It is designed in GitHub issues #26–#29
  (filed from `claude/amazing-ritchie-ga4w1t`, `Tools/mkissues_moodboard_gap.py`): four stalls
  (Spellmonger, Smith, Alchemist, Curiosity Dealer), prices always above the raid-found
  equivalent, "buy a thing once and it stays bought", and "whatever's left after the debt is
  spent here". The same audit (`docs/plans/moodboard-gap-closure.md` §2.5 on that branch) records
  that the Lair there is a menu screen (`LairScreen`), not a place — this proposal replaces that.

The design bible asks for much more than that: *"Damp, yours, and permanent. Set the words on your
tongue, choose a blade, choose a century, and decide how greedy you are feeling"* (§3), *"one
candle, one fire, falling away into black — the only safe frame in the whole game"* (§9), *"a portal
per unlocked Age"* (plan, Step 5), and *"carry him out and he lives again at the lair"* (§5).

---

## 2. The proposal in one paragraph

Build `LairScene.unity` as a small, walkable, first-person room — a vaulted cellar that stands
outside of time — where the crew spawns, sees the debt, practises their words, takes a weapon off
the rack, and sets out by all standing in a lit portal arch together. Every function is a physical
object you walk up to, never a menu, because *nothing in this world is a menu* (bible §5).

**The haul comes home as things, not as a number.** Everything carried across the extraction
threshold spills out of the portal onto the Lair floor, still heavy and still breakable. The crew
carries each piece to a station: the **Buyer's Hatch** to sell it for coin, the **Hoard** to keep
it, or the **rack** if it is a weapon. Nothing is sold unless someone sells it. Beside the Hatch, a
door opens onto the **Mystical Market**, where the coin is spent. The host's lair is the crew's
lair.

---

## 3. Visuals

### The place

A barrel-vaulted stone undercroft, about 16 m × 10 m, 4.5 m to the crown of the vault. Damp brick,
standing water in the low corners, a drip you can hear. A stair climbs eleven steps from one corner
and stops at a wall — **there is no outside**. That is the joke and the lore in one prop: the Lair
stands outside of time, so the only ways out are the portals, and the one door in the east wall,
which opens onto the Mystical Market (which stands outside of time too). The scene still needs no
exterior art, sky or skybox.

### Floor plan

```
                         N  (far wall: the Ages)
   ┌──────────────────────────────────────────────────────────┐
   │   ╔═══╗        ╔═══╗        ╔═══╗        ╔═══╗           │  portal arches, one per Age
   │   ║ I ║        ║II ║        ║III║        ║IV ║           │  (locked ones bricked up)
   │   ╚═══╝        ╚═══╝        ╚═══╝        ╚═══╝           │
   │           · ready ring ·               ( PRACTICE )      │  ring before the lit arch
   │  [ THE HOARD ]   ░░ the haul spills ░░  (  CIRCLE  )     │  NW: everything else, stored
   │  shelves, chests       out here ░░                       │  NE: chalk ring, targets
   │                                      [BUYER'S HATCH]     │  E: sell here
   │  [RACK]                                brass scales  ═══ ╪  DOOR ══► MYSTICAL MARKET
   │  weapons                                                 │  SW: weapons, stored
   │                        (HEARTH)       [LEDGER]           │
   │                        spawn ×4       strongbox          │  ledger = debt + purse
   │ ▟▟▟ stair to nowhere    straw pallet                     │
   └──────────────────────────────────────────────────────────┘
                         S  (you spawn facing north)
```

### Light — honest sources only

Four light sources and nothing else, per bible §9 ("nothing is lit by nothing"):

| Source | Where | Colour | Job |
|---|---|---|---|
| The hearth | centre-south, low | warm, `#6E5320` falling to `#2A2012` | the key light; matches the moodboard's Lair swatch (`docs/plunderspell-moodboard.html:226`, radial light low and left of centre) |
| One candle | on the ledger lectern | small, warm | makes the debt readable, and nothing else |
| The lit portal | the selected arch | Ground Lapis `#7A6AA0` thrown **upward** onto faces | the bible's Portal frame; the only cold light in the room |
| The market's lanterns | through the east door, left ajar | Verdigris `#5FA288`, spilling across the floor in a wedge | witch-lanterns on the stalls beyond: spell light, so still honest. It says "there is somewhere to spend this" from across the room |

Everything outside those pools falls to Bone Black `#14120E`. No ambient fill, no fog light.

### Palette discipline (no new pigments)

- **Orpiment** (gold) only on gold: the haul as it spills from the portal, the Hoard, the coin the
  Hatch pays out, and the benefactor's strongbox. Nothing else in the room is yellow — so the
  Hoard is literally the brightest corner of the Lair when it is full, and dark when it is empty.
- **Verdigris** on everything you can use: rack hooks, the Hatch's bell, the lectern, the arch
  keystones — and the market's lantern light through the door.
- **Lapis** on the voice: the practice circle's chalk ring when you speak, and the portals.
- **Madder** for the debt figure, written in red ink in the ledger — and for the hearth's embers.
- **Vellum** for all written text (ledger pages, the price chalked on the Hatch's slate, the
  practice readout). Never white.

### Art production

The existing Blender pipeline (`Tools/AssetPipeline/`, `room_kit.py` already has floor, wall and
door shells) builds the kit from the existing 4×4 palette atlas — about fourteen new specs in
`asset_specs.py`: the undercroft shell, stair-to-nowhere, hearth, lectern + ledger, strongbox,
weapon rack, Hoard shelving and chests, the Buyer's Hatch (hatch, brass scales, slate, bell), the
market door, portal arch (open and bricked variants), straw pallet, practice dummy, clay pot. The
loot itself already exists (`Art/Models/Loot/`). Contact sheet and previews render through the same
pipeline and get committed. The market's stalls beyond the door are the market epic's art, not
this kit's.

---

## 4. Functionality

### 4.0 The haul's journey — nothing is sold automatically

This is the change from revision 1. Today extraction turns the haul into a number and destroys the
objects. In this proposal it doesn't:

1. **Extraction records the haul.** When the raid resolves, the host writes down every unbroken
   item inside the extraction zone: which item it is, and its worth at that moment (conjured gold
   from Aurum Voco has a rolled worth, so the number travels with the item). Broken items stay
   behind as shards. **No gold is banked and the debt does not move.**
2. **The haul spills out of the portal.** Back in the Lair, every recorded item appears on the
   floor in front of the arch the crew came through, set down gently (nothing breaks on arrival).
   Weapons included — every item, not just weapons.
3. **The crew carries each piece to a station.** It is the same physics carry as in the raid: the
   altarpiece still takes two of you, and a dropped reliquary still shatters, here as anywhere.
   - **Sell it** at the Buyer's Hatch → coin.
   - **Keep it** in the Hoard, or on the rack if it is a weapon.
   - **Or leave it where it lies.** Nothing vanishes. The ledger lists it as unsorted.
4. **Spend the coin** through the door, at the Mystical Market.

The whole Lair is saved as it stands: every item, wherever it is, with its position. Quit with a
chalice on the hearthstone and it is on the hearthstone next time.

### 4.1 The Hearth — spawn, gather, revive

- Four spawn points in a half-ring around the fire. Everyone lands here on first load and on every
  return from a raid.
- **The Lair is safe.** Health cannot drop below 1 here; a misfired Ignis still sets your beard
  alight, it just cannot kill you. This is the "only safe frame".
- **Left behind.** A downed player carried into the extraction zone is already counted as saved
  (`ExtractionZone`). In the Lair, saved players wake at the hearth. Players who were *not* carried
  out wake on the straw pallet beside it — see decision **D3** for what that costs.

### 4.2 The Portals — choose a century, set out, and come home

- Four arches on the north wall, one per `HistoricalEra`. An unlocked arch is dark stone with a
  verdigris keystone; the **selected** one is lit lapis. A locked arch is bricked up.
- **Choose**: walk up to an arch and press `E` — or say `PORTA` at it, which is the portal word.
  This calls the existing `LairHubManager.SelectEra`.
- **Set out**: a ready-ring is chalked on the floor in front of the lit arch. When **every connected
  player** is inside it, a 3-second countdown runs (the arch brightens); anyone stepping out cancels
  it. At zero the host loads the raid. Solo, that is just you walking in. The host can force
  departure with `F6` (keeps today's playtest key working).
- **Take things with you**: anything a player is holding goes through — a weapon from the rack, a
  potion bought at the market, or a stolen candlestick, if you like.
- **Come home**: the same ring is the **arrival spill**. The haul lands here, in orpiment, in the
  lapis light — the first thing you see when you get back is what you got away with.

### 4.3 The Buyer's Hatch — sell

- A shuttered hatch in the east wall, beside the market door, with a brass balance on the sill and
  a slate above it. It belongs to the market: this is where the market buys.
- **Put an item on the pan** and a price is chalked on the slate: the item's worth, as recorded when
  it came home. **Ring the bell** (`E`) to accept — a gloved hand takes the item through and slides
  the coin back. **Lift it off the pan** to decline. No menu, and no sale happens unless someone rings.
- A broken item fetches nothing; the slate says so.
- Where the coin goes — straight to the debt, to the crew's purse, or split — is decision **D5**.
- Heavy things still need carrying to the Hatch. Two-person items need two people at the pan.

### 4.4 The Hoard — keep

- The north-west corner, right beside the spill, so the shortest carry from the portal is to keep
  something: shelves, two iron-bound chests and open flagstones. Set an item in a shelf slot or
  chest and it snaps in and is listed in the ledger as kept.
- Why keep anything instead of selling it:
  - **You can sell it later.** The Hatch is always open; nothing forces a sale tonight.
  - **You can take it into a raid.** Anything you can carry goes through the portal.
  - **What the market sells you lives here.** Its "buy a thing once and it stays bought" rule
    (issue #27) needs a place to put the thing.
  - **The Lair looks like your takings.** A full Hoard is the brightest corner of the room.
- A reason to hold stock for profit (a buyer who pays more for a complete set, prices that move)
  would be market design, issues #28–#29. This proposal doesn't invent one.
- Capacity is physical: shelf slots, chest space and floor. When it is full, it is full.

### 4.5 The Armoury Rack — keep weapons, choose a blade

- Hooks on the south end of the west wall. Hang a weapon from the spill (or the market) on a free
  hook to store it; `E` on a hooked weapon takes it down and calls `PlayerInventory.Equip`; `E`
  again hangs it back.
- The rack starts with a small starter kit from `Assets/_Project/Data/Inventory/` (decision **D4**).
  A weapon can also be sold at the Hatch like anything else.
- **Limitation:** no raid currently spawns a weapon to pick up (GitHub issue #17: weapon prefabs are
  not wired into any raid). Once a raid does, weapons come home through the spill with no further
  Lair work.

### 4.6 The Market Door — the way to the Mystical Market

- A heavy door in the east wall, left ajar, green lantern light spilling through the gap. Walk
  through into the market.
- **What this proposal builds:** the door, the lane beyond it as an empty shell with four stall
  booths (Spellmonger, Smith, Alchemist, Curiosity Dealer), the crew purse the stalls will spend
  from, and a place in the save file for the market's purchases.
- **What it does not build:** the stalls' wares, prices, mark-up and "stays bought" rules. Those are
  the market epic, GitHub issues #26–#29, and plug into this.
- Whether the market is a room in the Lair scene or its own scene is decision **D6**.

### 4.7 The Ledger — the debt, the purse, and what's in the house

- A lectern with an open book. Walk up and it shows, in handwriting: current debt (madder), the
  crew's purse, and one line per past raid: era, seed, appraised worth of the haul, players saved.
- A second page: every item in the Lair — kept, on the rack, or unsorted — with its worth. It is
  the only place in the game a total is added up for you.
- The "you set out, so the debt rose by 50" line is written the moment the crew commits at the
  portal, so the cost of going is on the page before you go.
- Beside it, the benefactor's strongbox, where debt payments go (see **D5**).
- When the debt reaches zero, the ledger shows the existing `DEBT_CLEARED` endgame stub as a closed
  book. What happens after that is out of scope.

### 4.8 The Practice Circle — set the words on your tongue

- A chalk ring in the north-east corner with targets: a clay pot on a stool (Frango), a candle
  (Ignis), a crate (Levo), a straw dummy (Somnus, Tonitrus). Targets reset a few seconds after they
  break. Loot from the Hoard is not a target — a Frango aimed badly near the Hoard still breaks
  whatever it hits.
- While you stand in the ring, a vellum readout floats above it: the word the recogniser heard, its
  confidence, your loudness, and whether it would have been a clean cast or a misfire.
- This doubles as the **M1 voice bench** the roadmap already asks for (`docs/plans/plunderspell.md`,
  Verification step 3): word, confidence and latency, logged per utterance. It is the same screen.
- Casting is the real spell system: `PushToCastController` + `SpellCastingSystem`, with the mock
  recogniser on `1`–`8` as today. There is no alarm in the Lair, so noise costs nothing here.

---

## 5. Implementation

### Scene architecture

Two separate scenes, `LairScene.unity` and `RaidScene.unity`, swapped with PurrNet's networked scene
loading (`NetworkManager.sceneModule.LoadSceneAsync(name, LoadSceneMode.Single)`,
`Assets/PurrNet/Runtime/CoreModules/Scenes/ScenesModule.cs:561`), so the host changes scene and every
client follows. The PurrNet `NetworkManager` already persists across loads.

What must survive the swap is the lair's state, so `LairHubManager` moves onto a persistent session
object:

- `LairHubManager` becomes a `SingletonBase<LairHubManager>` with `PersistBetweenScenes = true`. The
  base class already destroys a second instance, so `RaidScene` can **keep** its own Lair object
  for standalone play from the editor; arriving from the Lair scene, the raid scene's copy destroys
  itself and the real one carries on.
- `RaidDirector` stops holding a serialized scene reference to the lair and resolves
  `LairHubManager.Instance` when a raid starts (the scene's copy may have just destroyed itself).

### Assemblies — avoiding a reference cycle

`RogueAi.Raid` already references `RogueAi.Lair`, so the Lair cannot reference Raid to start a raid.
Instead:

- Lair stations raise `LairHubManager.DepartureRequested(HistoricalEra)` and know nothing about raids.
- A new, small `RogueAi.Session` assembly (references Raid and Lair) owns **`SessionFlow`**: it
  listens for `DepartureRequested` → loads `RaidScene` → tells `RaidDirector.StartRaid(era)`; and it
  listens for `RaidDirector.RaidResolved` → after the summary is dismissed → loads `LairScene`.
- `RaidBootstrapper` keeps auto-starting when there is **no** `SessionFlow` (standalone raid
  playtesting) and stands down when there is one.

### The haul: from extraction zone to Lair floor

- `ExtractionZone` gains `BuildManifest(...)` beside the existing `ComputeWorth(...)`: the same
  filter (non-null, unbroken, has data), but it returns a list of `HaulEntry` — the item's catalogue
  id plus its worth at that moment — instead of a sum. `ComputeWorth` stays, for the raid summary's
  "appraised worth".
- `RaidDirector.ApplyResult` hands that manifest to `LairHubManager.AddArrivals(...)` **instead of**
  calling `ApplyExtractionResult(worth)`. Extraction no longer touches the debt or the gold.
- New `LootCatalogue` (ScriptableObject): a stable id for every `LootItem` asset, so the save file
  can name an item and the Lair can spawn it again. `LootItem` has no id field today.
- New `LairItemSpawner` (host only): on entering the Lair, spawns every saved Lair item at its saved
  position, then every new arrival in the spill area in front of the arch, as ordinary networked
  `LootPickup`s — so carrying, two-person carries, breaking and Levo all work unchanged.
- Leaving the Lair, or selling, storing or breaking an item, writes the save.

### Selling and the purse

- `LairHubManager.Sell(HaulEntry)` is the only way an item becomes coin. It is server-side (a client
  ringing the bell sends a request), pays the entry's recorded worth, and splits it per **D5**.
- `ApplyExtractionResult`'s debt maths moves into `Sell`, so the existing, tested rules for paying
  down the debt and clearing it are reused rather than rewritten.

### Persistence and co-op

- New plain-C# `LairSaveFile` (JSON at `Application.persistentDataPath/lair.json`): debt, purse,
  selected era, unlocked eras, raid log, **every item in the Lair** (catalogue id, worth, position,
  rotation, and its Hoard/rack slot if it has one), and an empty section reserved for the market's
  purchases (issue #27). On first run it migrates the three existing `PlayerPrefs` keys so nobody's
  current debt resets.
- New `LairNetworkState` (NetworkBehaviour): the host's save is the truth; debt, purse, era and
  unlocks are replicated to clients as SyncVars, and the items replicate as the networked objects
  they already are. Clients never write their own save while connected (decision **D1**).

### Files

| Path | Change |
|---|---|
| `Scripts/Runtime/Lair/LairHubManager.cs` | singleton, JSON save, `DepartureRequested` event, `AddArrivals`, `Sell`, purse |
| `Scripts/Runtime/Lair/LairState.cs` | extend with purse, unlocks, Lair items, raid log |
| `Scripts/Runtime/Lair/HaulEntry.cs` | **new** — one item that came home: catalogue id + worth |
| `Scripts/Runtime/Lair/LairItemSpawner.cs` | **new** — host spawns saved items and new arrivals |
| `Scripts/Runtime/Loot/LootCatalogue.cs` | **new** — stable id ↔ `LootItem` asset |
| `Scripts/Runtime/Extraction/ExtractionZone.cs` | add `BuildManifest` beside `ComputeWorth` |
| `Scripts/Runtime/Lair/LairSaveFile.cs` | **new** — JSON read/write + PlayerPrefs migration (pure C#) |
| `Scripts/Runtime/Lair/LairNetworkState.cs` | **new** — host → client replication |
| `Scripts/Runtime/Lair/DepartureReadyCheck.cs` | **new** — pure: who is in the ring, countdown, cancel |
| `Scripts/Runtime/Lair/Stations/{Hearth,LedgerLectern,DeparturePortal,BuyersHatch,Hoard,ArmouryRack,MarketDoor,PracticeCircle}.cs` | **new** — thin scene components over the pure logic |
| `Scripts/Runtime/Session/SessionFlow.cs` + `RogueAi.Session.asmdef` | **new** — scene swapping and the loop between them |
| `Scripts/Runtime/Raid/RaidDirector.cs` | resolve the lair singleton at raid start; hand the manifest to the Lair instead of banking worth |
| `Scripts/Runtime/Raid/RaidBootstrapper.cs` | stand down when `SessionFlow` exists |
| `Scripts/Editor/LairSceneBuilder.cs` | **new** — *Tools ▸ Plunderspell ▸ Build Lair Scene*, same "built from code" convention as `RaidSceneBuilder` |
| `ProjectSettings/EditorBuildSettings.asset` | add `LairScene` (index 0) and `RaidScene` |
| `Tools/AssetPipeline/asset_specs.py` + builders | the fourteen lair kit pieces, previews, contact sheet |
| `docs/systems/lair.md` | **new** — how it works, invariants, traps |
| `docs/systems/raid.md`, `README.md` | the loop now runs through two scenes; "Play it" starts in the Lair |

### Tests

Pure logic gets EditMode tests that also run under `Tools/Headless/verify.sh`:

- `LairSaveFile` round-trips, and migrates PlayerPrefs values exactly once.
- `DepartureReadyCheck`: departs only when all connected players are in; cancels when one leaves;
  a player disconnecting mid-countdown does not strand the rest.
- `BuildManifest` includes exactly the unbroken items in the zone, and conjured gold keeps its
  rolled worth.
- Extraction leaves debt and purse untouched; only `Sell` moves money, split per D5; a broken item
  sells for nothing; the debt-clearing rule behaves exactly as it does today.
- The save round-trips every Lair item's id, worth, position and slot.
- Era lock rules (per D2).

PlayMode tests for the swap itself: Lair → Raid → Lair keeps one `LairHubManager`, the debt rises on
departure, the haul appears in the spill, and selling it lowers the debt.

**This changes tested behaviour on purpose.** `RaidLoopTests` ("Extracted worth must reduce the
debt") and `FullRaidIntegrationTests` ("The takings pay down the debt") assert today that extraction
pays the debt. Under this proposal it doesn't, so those assertions move from "after extraction" to
"after selling at the Hatch". They are rewritten to the new rule, not deleted or skipped.

**Verification caveat:** this sandbox has no Unity, no .NET SDK and no Blender (`which` finds none
of them). Implementation here could only be written, not compiled, tested or rendered. The C# would
need compiling and the tests running either in a session that has Unity (the `.agent_reports/`
results show one existed on 2026-09-15) or on your machine, and I would label it `UNTESTED` until
then.

---

## 6. Phasing — approve any prefix

| Phase | Delivers | Size |
|---|---|---|
| **A — The room and the loop** | Lair scene from the builder (grey-box), hearth spawn, ledger, portals with ready-ring, `SessionFlow` swap both ways, JSON save + migration, host-owned co-op state | ~10 files + tests |
| **B — The haul comes home** | Extraction manifest, arrival spill, Buyer's Hatch and purse, the Hoard, every Lair item saved where it lies, the market door and empty market shell; the two existing debt tests rewritten | ~8 files + tests |
| **C — The other stations** | Armoury rack, practice circle with voice readout | ~3 files + tests |
| **D — The art pass** | Lair kit through the Blender pipeline, lighting to the moodboard, previews committed | pipeline specs + builder swap-in |

A gives you a real Lair you walk around in and leave from, but on its own it still sells the haul
automatically, as today. B is what your review asked for: the haul comes home and the crew decides
what to sell. C completes the bible's Lair. D makes it look like one. The market's stalls, wares
and prices stay with issues #26–#29, which B's door and purse are built to receive.

---

## 7. Decisions I need from you

| # | Question | My recommendation |
|---|---|---|
| **D1** | In co-op, whose debt is it? | **The host's.** One lair, one debt, replicated. The alternative (everyone keeps their own) means four different ledgers in one room. |
| **D2** | Which Ages are open? | **High Medieval only**, the other three bricked up, until the generator actually builds different castles per era. Offering the Bronze Age arch and delivering a medieval castle is a lie the player will notice. |
| **D3** | What does a player left behind cost? | **Wakes on the pallet, and the benefactor bills the crew +25 debt for fetching the body.** Keeps "carry your friend out" a real choice without permadeath. The alternative is free revival. |
| **D4** | What is on the rack at the start? | **One period-appropriate weapon each for the open Age** (Longsword, Round Shield). The full eight-item list makes the anachronism progression pointless before it starts. |
| **D5** | When an item is sold, where does the coin go? | **Half to the debt, half to the crew's purse** (the half is a tunable number). It honours the market's rule that you spend "what's left after the debt", without locking the market until the whole debt is gone. Alternatives: *all of it pays the debt until the debt is zero* (the literal reading of issue #29; the market is closed for most of the game), or *all of it to the purse, and paying the debt is its own act at the strongbox* (the most choice; the +50 per outing is then the only pressure to pay). |
| **D6** | Where is the Mystical Market? | **A second room of the Lair scene, through the east door.** No loading screen, one networked scene, and things bought there can be carried straight back to the Hoard. The alternative is its own scene, which issue #27 also allows; it costs a scene switch each way. |

## 8. Considered and rejected

- **The Lair as a menu screen.** Cheaper, but directly contradicts pillar 2 ("nothing in this world
  is a menu") and loses the only safe frame.
- **The Lair as a room inside `RaidScene`.** Avoids scene loading, but you asked for a separate
  scene, and it would leave the castle's garrison, alarm and loot state sharing a scene with the hub.
- **Selling the haul automatically at extraction** (today's behaviour, and revision 1 of this
  proposal). Rejected in review: the crew should decide what is sold and what is kept.
- **Selling from inside the market's stalls.** It would put buying and selling behind one door; the
  Hatch in the Lair wall keeps the haul in the Lair until the crew chooses, and still belongs to the
  market.
- **An automatic trophy shelf** (revision 1). Replaced by the Hoard, where the crew chooses what to
  keep.
