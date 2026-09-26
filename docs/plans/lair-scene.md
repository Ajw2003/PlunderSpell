# Proposal: the Lair as its own scene

Rendered version with floor plan and diagrams: `docs/plans/lair-scene.html`.

Status: **proposal, awaiting approval** — nothing here is built yet. Written 2026-09-26 against
`integration/staging-2026-09-15` at `ee13c2d`.

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

The design bible asks for much more than that: *"Damp, yours, and permanent. Set the words on your
tongue, choose a blade, choose a century, and decide how greedy you are feeling"* (§3), *"one
candle, one fire, falling away into black — the only safe frame in the whole game"* (§9), *"a portal
per unlocked Age"* (plan, Step 5), and *"carry him out and he lives again at the lair"* (§5).

---

## 2. The proposal in one paragraph

Build `LairScene.unity` as a small, walkable, first-person room — a vaulted cellar that stands
outside of time — where the crew spawns, sees the debt, practises their words, takes a weapon off
the rack, and sets out by all standing in a lit portal arch together. Every function is a physical
object you walk up to, never a menu, because *nothing in this world is a menu* (bible §5). When a
raid resolves, everyone is carried back to this room, and what came home is visible in it. The
host's lair is the crew's lair.

---

## 3. Visuals

### The place

A barrel-vaulted stone undercroft, about 16 m × 10 m, 4.5 m to the crown of the vault. Damp brick,
standing water in the low corners, a drip you can hear. A stair climbs eleven steps from one corner
and stops at a wall — **there is no outside**. That is the joke and the lore in one prop: the Lair
stands outside of time, so the only ways out are the portals. It also means the scene needs no
exterior art, sky or skybox.

### Floor plan

```
                         N  (far wall: the Ages)
   ┌──────────────────────────────────────────────────────────┐
   │   ╔═══╗        ╔═══╗        ╔═══╗        ╔═══╗           │
   │   ║ I ║        ║II ║        ║III║        ║IV ║           │  portal arches, one per Age
   │   ╚═══╝        ╚═══╝        ╚═══╝        ╚═══╝           │  (locked ones bricked up)
   │         ·  ·  ·  lapis ready-ring before the lit arch    │
   │                                                          │
   │  [RACK]                                     ( PRACTICE ) │  W: armoury rack
   │  weapons                                    (  CIRCLE  ) │  E: chalk ring, targets
   │  on hooks                                   pots, dummy  │
   │                                                          │
   │  [TROPHY SHELVES]         (HEARTH)          [LEDGER]     │  hearth = spawn + the one fire
   │   best haul per raid      spawn ×4          lectern +    │  ledger = debt, one candle
   │                           straw pallet      strongbox    │
   │ ▟▟▟ stair to nowhere                                     │
   └──────────────────────────────────────────────────────────┘
                         S  (you spawn facing north)
```

### Light — honest sources only

Three light sources and nothing else, per bible §9 ("nothing is lit by nothing"):

| Source | Where | Colour | Job |
|---|---|---|---|
| The hearth | centre-south, low | warm, `#6E5320` falling to `#2A2012` | the key light; matches the moodboard's Lair swatch (`docs/plunderspell-moodboard.html:226`, radial light low and left of centre) |
| One candle | on the ledger lectern | small, warm | makes the debt readable, and nothing else |
| The lit portal | the selected arch | Ground Lapis `#7A6AA0` thrown **upward** onto faces | the bible's Portal frame; the only cold light in the room |

Everything outside those pools falls to Bone Black `#14120E`. No ambient fill, no fog light.

### Palette discipline (no new pigments)

- **Orpiment** (gold) only on gold: the trophy shelf and the benefactor's strongbox. Nothing else in
  the room is yellow.
- **Verdigris** on everything you can use: the interact glint on rack hooks, the lectern, the arch
  keystones.
- **Lapis** on the voice: the practice circle's chalk ring when you speak, and the portals.
- **Madder** for the debt figure, written in red ink in the ledger — and for the hearth's embers.
- **Vellum** for all written text (ledger pages, the practice readout). Never white.

### Art production

The existing Blender pipeline (`Tools/AssetPipeline/`, `room_kit.py` already has floor, wall and
door shells) builds the kit from the existing 4×4 palette atlas — about eleven new specs in
`asset_specs.py`: the undercroft shell, stair-to-nowhere, hearth, lectern + ledger, strongbox,
weapon rack, trophy shelf, portal arch (open and bricked variants), straw pallet, practice dummy,
clay pot. Contact sheet and previews render through the same pipeline and get committed.

---

## 4. Functionality — the six stations

Each station is one bible verb, made into an object.

### 4.1 The Hearth — spawn, gather, revive

- Four spawn points in a half-ring around the fire. Everyone lands here on first load and on every
  return from a raid.
- **The Lair is safe.** Health cannot drop below 1 here; a misfired Ignis still sets your beard
  alight, it just cannot kill you. This is the "only safe frame".
- **Left behind.** A downed player carried into the extraction zone is already counted as saved
  (`ExtractionZone`). In the Lair, saved players wake at the hearth. Players who were *not* carried
  out wake on the straw pallet beside it — see decision **D3** for what that costs.

### 4.2 The Ledger — the debt, visible

- A lectern with an open book. Walk up and it shows, in handwriting: current debt (madder), gold
  banked, and one line per past raid: era, seed, worth carried home, players saved.
- The "you set out, so the debt rose by 50" line is written the moment the crew commits at the
  portal, so the cost of going is on the page before you go.
- Beside it, the benefactor's strongbox. Purely visual in this proposal; the economy stays exactly as
  `LairHubManager.ApplyExtractionResult` computes it today, which is already tested.
- When the debt reaches zero, the ledger shows the existing `DEBT_CLEARED` endgame stub as a closed
  book. What happens after that is out of scope.

### 4.3 The Portals — choose a century, set out together

- Four arches on the north wall, one per `HistoricalEra`. An unlocked arch is dark stone with a
  verdigris keystone; the **selected** one is lit lapis. A locked arch is bricked up.
- **Choose**: walk up to an arch and press `E` — or say `PORTA` at it, which is the portal word.
  This calls the existing `LairHubManager.SelectEra`.
- **Set out**: a ready-ring is chalked on the floor in front of the lit arch. When **every connected
  player** is inside it, a 3-second countdown runs (the arch brightens); anyone stepping out cancels
  it. At zero the host loads the raid. Solo, that is just you walking in.
- The host can force departure with `F6` (keeps today's playtest key working).

### 4.4 The Armoury Rack — choose a blade

- Hooks on the west wall, one per weapon the crew owns. Walk up, `E` takes it off the hook and calls
  `PlayerInventory.Equip`; `E` again hangs it back up.
- The rack starts with the starter kit from `Assets/_Project/Data/Inventory/` (decision **D4**).
- **Honest limitation:** nothing in a raid can currently carry a weapon *home* — no raid code picks
  up an `InventoryItem`. The rack is built to hold new ones and the save records them, but filling
  it from raids is separate raid-side work, not part of this proposal.

### 4.5 The Practice Circle — set the words on your tongue

- A chalk ring on the east side with targets: a clay pot on a stool (Frango), a candle (Ignis), a
  crate (Levo), a straw dummy (Somnus, Tonitrus). Targets reset a few seconds after they break.
- While you stand in the ring, a vellum readout floats above it: the word the recogniser heard, its
  confidence, your loudness, and whether it would have been a clean cast or a misfire.
- This doubles as the **M1 voice bench** the roadmap already asks for (`docs/plans/plunderspell.md`,
  Verification step 3): word, confidence and latency, logged per utterance. It is the same screen.
- Casting is the real spell system: `PushToCastController` + `SpellCastingSystem`, with the mock
  recogniser on `1`–`8` as today. There is no alarm in the Lair, so noise costs nothing here.

### 4.6 The Trophy Shelf — the lair remembers

- After each raid, the single most valuable item that came home is placed on the shelf as a static
  model, with a small vellum label (era, date, worth). Oldest drops off after twelve.
- This is the cheapest possible version of *"a lair that remembers what came back"*: one loot id per
  raid in the save file, no physics, no economy.

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

### Persistence and co-op

- New plain-C# `LairSaveFile` (JSON at `Application.persistentDataPath/lair.json`): debt, gold,
  selected era, unlocked eras, rack contents, trophy ids, raid log. On first run it migrates the
  three existing `PlayerPrefs` keys so nobody's current debt resets.
- New `LairNetworkState` (NetworkBehaviour): the host's save is the truth; debt, gold, era, unlocks,
  rack and trophies are replicated to clients as SyncVars. Clients never write their own save while
  connected (decision **D1**).

### Files

| Path | Change |
|---|---|
| `Scripts/Runtime/Lair/LairHubManager.cs` | singleton, JSON save, `DepartureRequested` event, new state |
| `Scripts/Runtime/Lair/LairState.cs` | extend with unlocks, rack, trophies, raid log |
| `Scripts/Runtime/Lair/LairSaveFile.cs` | **new** — JSON read/write + PlayerPrefs migration (pure C#) |
| `Scripts/Runtime/Lair/LairNetworkState.cs` | **new** — host → client replication |
| `Scripts/Runtime/Lair/DepartureReadyCheck.cs` | **new** — pure: who is in the ring, countdown, cancel |
| `Scripts/Runtime/Lair/Stations/{Hearth,LedgerLectern,DeparturePortal,ArmouryRack,PracticeCircle,TrophyShelf}.cs` | **new** — thin scene components over the pure logic |
| `Scripts/Runtime/Session/SessionFlow.cs` + `RogueAi.Session.asmdef` | **new** — scene swapping and the loop between them |
| `Scripts/Runtime/Raid/RaidDirector.cs` | resolve the lair singleton at raid start |
| `Scripts/Runtime/Raid/RaidBootstrapper.cs` | stand down when `SessionFlow` exists |
| `Scripts/Editor/LairSceneBuilder.cs` | **new** — *Tools ▸ Plunderspell ▸ Build Lair Scene*, same "built from code" convention as `RaidSceneBuilder` |
| `ProjectSettings/EditorBuildSettings.asset` | add `LairScene` (index 0) and `RaidScene` |
| `Tools/AssetPipeline/asset_specs.py` + builders | the eleven lair kit pieces, previews, contact sheet |
| `docs/systems/lair.md` | **new** — how it works, invariants, traps |
| `docs/systems/raid.md`, `README.md` | the loop now runs through two scenes; "Play it" starts in the Lair |

### Tests

Pure logic gets EditMode tests that also run under `Tools/Headless/verify.sh`:

- `LairSaveFile` round-trips, and migrates PlayerPrefs values exactly once.
- `DepartureReadyCheck`: departs only when all connected players are in; cancels when one leaves;
  a player disconnecting mid-countdown does not strand the rest.
- Trophy selection picks the highest-worth unbroken item; the shelf caps at twelve.
- Era lock rules (per D2).

PlayMode tests for the swap itself: Lair → Raid → Lair keeps one `LairHubManager`, debt rises on
departure and falls on return — the same assertions `FullRaidIntegrationTests` makes today, across
two scenes instead of one.

**Verification caveat:** this sandbox has no Unity, no .NET SDK and no Blender (`which` finds none
of them). Implementation here could only be written, not compiled, tested or rendered. The C# would
need compiling and the tests running either in a session that has Unity (the `.agent_reports/`
results show one existed on 2026-09-15) or on your machine, and I would label it `UNTESTED` until
then.

---

## 6. Phasing — approve any prefix

| Phase | Delivers | Size |
|---|---|---|
| **A — The room and the loop** | Lair scene from the builder (grey-box), hearth spawn, ledger, portals with ready-ring, `SessionFlow` swap both ways, JSON save + migration, host-owned co-op state | the bulk: ~10 files + tests |
| **B — The stations** | Armoury rack, practice circle with voice readout, trophy shelf | ~4 files + tests |
| **C — The art pass** | Lair kit through the Blender pipeline, lighting to the moodboard, previews committed | pipeline specs + builder swap-in |

A alone gives you a real Lair you walk around in and leave from. B makes it the bible's Lair. C
makes it look like one.

---

## 7. Decisions I need from you

| # | Question | My recommendation |
|---|---|---|
| **D1** | In co-op, whose debt is it? | **The host's.** One lair, one debt, replicated. The alternative (everyone keeps their own) means four different ledgers in one room. |
| **D2** | Which Ages are open? | **High Medieval only**, the other three bricked up, until the generator actually builds different castles per era. Offering the Bronze Age arch and delivering a medieval castle is a lie the player will notice. |
| **D3** | What does a player left behind cost? | **Wakes on the pallet, and the benefactor bills the crew +25 debt for fetching the body.** Keeps "carry your friend out" a real choice without permadeath. The alternative is free revival. |
| **D4** | What is on the rack at the start? | **One period-appropriate weapon each for the open Age** (Longsword, Round Shield). The full eight-item list makes the anachronism progression pointless before it starts. |

## 8. Considered and rejected

- **The Lair as a menu screen.** Cheaper, but directly contradicts pillar 2 ("nothing in this world
  is a menu") and loses the only safe frame.
- **The Lair as a room inside `RaidScene`.** Avoids scene loading, but you asked for a separate
  scene, and it would leave the castle's garrison, alarm and loot state sharing a scene with the hub.
- **Physically carrying loot to the strongbox to pay the debt.** On-theme, but it moves the economy
  off the tested server-side tally into physics. Worth revisiting after Phase B.
