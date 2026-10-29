# Plan: a dark castle that lights up with the alarm, and no more shadow warnings

Status: **proposed, waiting for aj's approval** (2026-10-29). Nothing here is built yet.

Two changes, asked for together on 2026-10-29:

1. Stop the stream of "shadow resolution / shadow atlas" warnings in the Console.
2. At Calm, every room is dark and only a few outdoor torches burn. Lights come on in steps as the
   alarm climbs, so each stage looks clearly different from the one before.

Everything below was read from `AjsAgent/gym-with-demand-page` at `d3ffad88`, the newest branch,
because `main` is a month behind.

---

## Part 1: the shadow warnings

### What causes them

Unity has one fixed-size image (the "shadow atlas") that every shadow-casting lamp must fit its
shadows into. A point light needs six shadow squares, one for each direction. When the squares
don't all fit, Unity shrinks them and prints a warning, every frame.

There are two sources of overflow.

**The Lair and the Market (the main cause).** `BlenderPlacement.AddPointLight`
(`Assets/_Project/Scripts/Editor/BlenderPlacement.cs`) makes every Lair and Market lamp cast soft
shadows, and leaves its shadow size at Unity's default, which is the largest (1024). The Market yard
has 10 such lamps and the Lair room has 3. That is 13 lamps x 6 squares = 78 large squares. The
atlas holds 16 squares of that size on Medium and High, and 4 on Low. These lamps are not under the
castle's fire budget, so nothing ever turns their shadows off.

**Castle fires during a hand-over (smaller, and only on Low and High).** The castle lets only the
nearest few fires cast shadows: 2 on Low, 4 on Medium, 8 on High. Since #354, a fire's shadow fades
out over about a second instead of switching off. While one fire fades out and the next fades in,
both use the atlas, so for a moment up to twice the budget is casting:

| Level | Shadowed fires | Worst moment | Squares that fit (512 each) | Fits? |
|---|---|---|---|---|
| Low | 2 | 4 fires = 24 squares | 16 (2048 atlas) | **no** |
| Medium | 4 | 8 fires = 48 squares | 64 (4096 atlas) | yes |
| High | 8 | 16 fires = 96 squares | 64 (4096 atlas) | **no** |

I worked this out from the settings files and haven't seen the warning text from your Console.
Step 1 of the build checks it in the Editor before anything changes.

### The fix

1. **Lair and Market lamps.** Only one lamp per room casts shadows: the Lair hearth and one central
   lamp in the Market yard, both at the medium shadow size (512). Every other lamp still lights the
   room but casts no shadow. The change goes in `BlenderPlacement.AddPointLight` (a new
   `castsShadows` argument), `LairRoomForge` and `MarketYardForge`. Then I rerun those two forges so
   the `LairRoom` and `MarketYard` prefabs are rebuilt from them. No scene file is touched.
2. **Castle fire hand-over.** A fire's shadow only starts fading in once a shadow slot is free, so
   the number of fires casting shadows never goes over the budget. The lamp itself still fades in
   straight away; only its shadow waits. This goes in `FireSource` and `CastleAtmosphere`, with a
   test in `NightAtmosphereTests`.
3. **A test so this can't come back.** For each level, the test checks that budget x 6 squares fit
   the atlas, and that no prefab or scene has more always-on shadow-casting lamps than one per room.

---

## Part 2: the alarm lights the castle

### What happens today

Every castle room has two wall torches. The first is lit at Calm and the second at Stirred.
Braziers in courtyards, crypts, chapels and stairwells (43 of them) are lit at Calm. The three
hearths (blacksmith, kitchen, lord's solar) are always lit. The overall fill light (ambient) is
bright and barely changes between stages, and fire brightness only goes from 1.4 to 1.55. So Calm
already looks lit, and Calm and Stirred look almost the same.

Some rooms look dark at Calm and others don't, even though every room has a Calm torch. That's
because only the nearest 32 fires (16 on Low) actually light their surroundings; past that, a fire
shows its flame but lights nothing.

### What it becomes

| Stage | What is burning |
|---|---|
| **Calm** | Every room is dark. Only one torch per outdoor wall piece burns: the curtain walls, gatehouses (including the Lion Gate and the barbican) and bastions. No braziers, and no hearths. Courtyards are dark. |
| **Stirred** | Each room's first torch is lit, and the hearths come up. Courtyards stay dark. |
| **Roused** | Each room's second torch is lit, and every brazier indoors and out, plus the beacons. |
| **Hue and Cry** | As now: everything burns at its biggest and reddest. |

The ambient light steps up with the stages too. At Calm it is about half of today's value, then
it rises at each stage back to today's level at Hue and Cry, so the darkness at Calm is moonlit
rather than pitch black. The exact numbers get tuned in the Editor with you, using before and
after captures from the same spots (`Tools/Unity/night_captures.sh`).

### How it's built

- **The rule lives in one place.** `FireRules.LitFrom` works out each fire's lighting stage from
  its kind, whether it is outdoors, and the order it was authored in. The Blender anchor data
  stays as it is: each room's "first" and "second" torch keep their meaning, and the rule shifts
  them back a stage. The Blender assets don't need rebuilding.
- **Outdoor torches.** `CastleFireAnchor` gets an `Outdoor` flag. `CastleFireAnchorImporter` sets
  it from a single list of open-air pieces: `WallStraight`, `WallCorner`, `GatehouseModule`,
  `Bastion` and their Bronze and Late versions, `BronzeLionGate` and `LateBarbican`. Please check
  that list. Courtyards aren't on it, because you asked for torches only, not braziers.
- **Ambient light.** The four stage looks in `NightLooks.cs` get the darker Calm and the step-ups.
- **Tests.** `NightAtmosphereTests` checks that no indoor fire burns at Calm, that only the listed
  outdoor torches do, and that each stage lights strictly more fires than the one before.
- **Docs.** `docs/4-systems/atmosphere.md` gets the new ladder. A decision entry records that this
  replaces #317's "every module has a fire burning at Calm".

### Worth knowing

- Darkness doesn't help stealth yet. `LightSource` records where the light is, but guards don't
  read it, so a dark room changes how the game looks, not how guards see.
- The castle will be noticeably darker to explore at Calm. If it turns out too dark to find your
  way, the ambient level at Calm is the dial to turn, not the fires.

---

## Order of work, and keeping clear of the scene split

The "Finish the ophan and destruction" thread is splitting the Lair, Market and Raid into separate
scenes on your PC, and it edits `Bootstrap.unity`, `Lair.unity`, `RaidScene.unity` and
`OphanGym.unity`. This plan touches none of those files. It changes the Lair and Market prefabs,
the atmosphere code, `NightLooks.cs` and the anchor importer. I'll branch from that thread's latest
push so we don't diverge.

1. In the Editor: read the exact warning text, and count the warnings over a short Lair-to-raid
   run on each graphics level. That's the "before" number.
2. Part 1, items 1 to 3, then the same run again. Done when there are zero shadow warnings on Low,
   Medium and High.
3. Part 2, then captures at each of the four stages from the same spots for you to compare.
4. Docs, then commit and push.

Steps 1 to 3 need the Unity Editor, so they run on your PC.
