# Castle rooms for the other three Ages (started 2026-09-24)

The user's brief:

> Help me fill in the remaining rooms in each age to match the current setup of the castle in the
> castle bench scene. Each age should have the same amount of rooms to choose from, or more if
> warranted. Please check the current convention, then plan out each piece, and then model them.

## What "the current setup" is

`CastleBench.unity` builds its castle with `ProceduralCastleGenerator` from
`Assets/_Project/Data/Castle/CastleRoomRegistry.asset`. That registry holds one room set, and it
is High Medieval in all but name. `HistoricalEra` (`Assets/_Project/Scripts/Runtime/Inventory/HistoricalEra.cs`)
has four Ages, but no room is tagged with one (`docs/ProjectState.md`, "The one thing that is not
what it looks like"). The current set, one mesh each, built by `Tools/AssetPipeline/castle_builders.py`:

| Zone | Pieces |
|---|---|
| CurtainWall (5) | GatehouseModule, WallStraight, WallCorner, Bastion, Drawbridge |
| OuterBailey (5) | StableBlock, BlacksmithShop, BarracksBunk, WellCourtyard, StorehouseRoom |
| InnerWard (5) | GreatHallMain, ChapelRoom, KitchenRoom, GuardRoomInner, ArmouredCourtyard |
| Keep (5) | ThroneRoomKeep, TreasuryVault, RoyalBedchamber, LordsSolar, KeepStairwell |
| Crypt (5) | CryptAntechamber, TombCorridor, BurialVault, CryptChamberFinal, CryptStairwell |
| Door plugs (4) | DoorPlugOuterBailey, DoorPlugInnerWard, DoorPlugKeep, DoorPlugCrypt |

That makes 20 enclosed rooms, 5 curtain-wall pieces and 4 door plugs, 29 modules in all. This
plan gives Bronze Age, Late Medieval and Age of Powder the same 29 each. Age of Powder gets one
extra Keep room (30), because the art bible puts two of its three signature structures in the Keep.

## The convention every new piece follows

These come from the code, not from memory. Each was checked against the file named.

- **Grid.** Every module fills one 12 m × 12 m cell exactly (`room_kit.FOOTPRINT`), is bottom-flush at
  Z = 0 and centred on X/Y. `validate_in_blender` fails anything that reaches past ±6.05 m.
- **Heights are the same in every Age.** `ZONE_HEIGHT` (CurtainWall 5.2, OuterBailey 3.6, InnerWard
  4.0, Keep 4.6, Crypt 3.0) is measured against the 1.80 m human (`docs/systems/scale.md`), and the
  archway size is derived from it. The art bible uses the same figures. Keeping them identical
  means every Age's archways, door plugs and prefab socket positions match, and
  `ScaleInvariantTests` holds without change.
- **Enclosed rooms** start from `_shell` (floor + four walls + an archway centred on every side). The
  generator places rooms without looking at their geometry, so a room must open on all four sides.
  A side facing nothing is sealed at placement by that zone's door plug.
- **Room layout grammar** (`castle_builders.py`, enforced by `validate_castle_layout`):
  - a clear cross, `|x| < 1.6` or `|y| < 1.6`, up to 2 m above the floor;
  - furniture goes in the four corner quadrants, backed against the walls;
  - nothing floats: every mesh island rests on the ground or touches something that does. Rooms
    are open-roofed, so nothing hangs from a ceiling.
- **Loot anchors.** Each room registers at least two loot anchors on furniture (`_anchor`, or the
  `_table`/`_chest`/`_shelf`/`_sarcophagus` helpers, which register one each). They land in
  `Assets/_Project/Data/Castle/CastleLootAnchors.json`.
- **Curtain-wall orientation.** The generator yaws wall pieces by fixed rules, so every Age's wall
  pieces use the same local layout: the wall on the **south** side (corner: south + west), the gate
  opening in the south wall with the interior open to the north, and the drawbridge-slot deck
  running inward from the south edge. Battlements only where there is a wall under them.
- **Flat colour from the 16-pigment atlas.** `palette.py` is full (4×4), and changing the atlas
  would re-UV every committed asset. Each Age maps the art bible's material hexes to the nearest
  existing pigment (the table below). The art bible's texture sets are for a later hand-made art
  pass, which would replace these meshes.
- **Paint-box overlap.** Everything that sits flush on something else goes through
  `room_kit.paint_box`/`_box`, so no two islands share an exact vertex position.
- **Triangle budgets** sit at about 1.5–2× the reported count, as for the existing pieces.

## Where the kit overrides the art bible

The art bible (`docs/art/`, branch `claude/dreamy-curie-jnrkbu`, commit `4de86a9`) specifies each Age's
three signature structures for a hand-built art pass, at up to 25k triangles per cell. The kit
builds the procedural, grid-legal version of the same room. Where the two disagree, the kit wins,
because its rules are what make the generated castle walkable
(`docs/generated/castle-survey-2026-09-23/`):

| Structure | Art bible | Kit version |
|---|---|---|
| Lion Gate | 2.60 m passage between two cyclopean masses | as specified: the masses on the south edge, the passage centred |
| Megaron | 3.6 m hearth with a raised rim, centred | the hearth is a flat painted ring (walkable); the four columns stay on their 5 m square |
| Megaron | throne in the centre of the east wall, no east door | throne against the east wall in the NE quadrant, facing west; all four archways open |
| Pithos Magazine | four N–S jar rows, 1.2 m aisles | two short rows per quadrant (24 jars in all, as specified), cross kept clear |
| Crooked Barbican | L-shaped passage, gates off-centre | gate centred on the south wall (it must meet the bridge); a baffle wall inside forces the dog-leg |
| Great Hall (Late) | dais along the whole north wall, tapestry over the north door | dais split across the NW/NE quadrants; tapestry in two halves either side of the archway |
| Counting House | counting table in the centre of the room | table in the SE quadrant; strong room in the NW, as specified |
| Powder Magazine | east/west archways bricked up, a door in the north | all four archways open (the kit's rule); racks in the quadrants |
| Long Gallery | chandelier on a chain | standing candelabra (no ceiling to hang from) |
| Kunstkammer | crocodile hung from a beam, table centred | crocodile laid along a case top; table in a quadrant |

## Per-Age palette (pigment names from `palette.py`)

| Role | Bronze Age | Late Medieval | Age of Powder |
|---|---|---|---|
| Room walls | `bronze` (ochre plaster); the Crypt `vellum_dim` (limestone tombs) | `vellum_dim` (dressed sandstone) | `vellum` (lime plaster) |
| Curtain-wall stone | `vellum_dim` (cyclopean limestone) | `vellum_dim` | `vellum_faint` (rampart stone) |
| Brick / mud-brick | `leather` | `leather` | `leather` |
| Timber | `oak` | `oak` | `line` (black walnut); `oak` for parquet and rough work |
| Metal | `bronze` | `line` (blackened iron), `iron` | `iron` (blued steel) |
| Cloth | `vellum` (linen), `madder` | `verdigris_lo` (tapestry), `madder` (cloth of estate) | `madder` (murrey), `vellum` |
| Accent | `madder` (haematite columns), `verdigris_lo` (fresco) | `madder` | `orpiment` (gilt) |
| Floors OB / IW / Keep / Crypt | `leather` / `ash_hi` / `vellum_faint` / `bone_black` | `ash_hi` / `vellum_faint` / `oak` / `bone_black` | `ash_hi` / `vellum_faint` / `oak` / `bone_black` |
| Trim band OB / IW / Keep / Crypt | `leather` / `verdigris_lo` / `madder` / `vellum_dim` | `leather` / `verdigris_lo` / `madder` / `ash` | `oak` / `line` / `orpiment` / `iron` |
| Curtain-wall trim | `leather` (mud-brick parapet) | `leather` (brick frieze) | `vellum_dim` (cordon) |

The pitch bible keeps orpiment for gold and value, madder for fire, blood and alarm, and verdigris
and lapis for the arcane. So the new pieces use orpiment only on things worth stealing (and Powder
gilt), madder for hearths, red cloth and painted columns, and never lapis or bright verdigris.

## Order of work: a sheet for every room first, then the model

Added 2026-09-24 at the user's request, after the first kit Megaron came out a painted box next to
the art bible's Megaron sheet. Every room and curtain-wall piece gets a reference sheet before it
is modelled, at art-bible depth: a section and a plan, a 1.80 m figure for scale, labelled sockets
and loot points, a palette strip, and a JSON spec with real dimensions. The model is then built to
the sheet, and `build_room_sheets.py --models` holds it there by comparing the loot anchors. Rules
and commands: [`docs/art/rooms/README.md`](../art/rooms/README.md). Door plugs are plain slabs and
have no sheet.

## File layout

- `Tools/AssetPipeline/castle_builders_bronze.py`, `castle_builders_late.py`,
  `castle_builders_powder.py`: each Age's palette, zone tables, `room_shell` and door plugs.
- `Tools/AssetPipeline/castle_builders_<age>_<zone>.py` (`curtain`, `bailey`, `ward`, `keep`,
  `crypt`): that Age's pieces for one zone, so zones can be built in parallel without touching
  each other's files.
- `docs/art/rooms/`: the room sheets (`data/<Age>/<Key>.json`, `concept/<Age>/<Key>.svg`), drawn
  by `Tools/ArtBible/rooms/generators/<Age>/<Key>.py`.
- `Tools/AssetPipeline/asset_specs.py`: `BRONZE_CASTLE_SPECS`, `LATE_CASTLE_SPECS` and
  `POWDER_CASTLE_SPECS`, each entry with a `kind` of `wall` / `room` / `plug` and its `zone`.
- FBX go to `Assets/_Project/Art/Models/Castle/<HistoricalEra>/`. Keys carry the Age prefix
  (`Bronze…`, `Late…`, `Powder…`) because the manifest, previews, loot anchors and RoomIds are all
  keyed by name.
- Previews: `Tools/AssetPipeline/previews/<Key>.png`, plus one contact sheet per Age
  (`previews/_contact_sheet_<Era>.png`).

## The pieces

Every room lists its set-pieces by quadrant (NE = +x +y in Blender space; north is +y) and where
its loot anchors sit. "Role" names the High Medieval room it stands in for, so each zone offers
the same mix of rooms in every Age.

### Bronze Age, Mycenaean citadel, c. 1200 BC (`Castle/BronzeAge`)

The first Age: low and warm, ochre plaster, mud-brick, bronze, red-painted columns that taper
downwards, horns of consecration on the parapets, and everything dry enough to burn.

**CurtainWall**

| Key | Role | Build |
|---|---|---|
| BronzeLionGate | Gatehouse | Art bible. Two cyclopean masses 4.4 (E–W) × 3.2 (N–S) × 4.1 m on the south edge, 0.3 m in from E/W, each 4–6 irregular stacked blocks; a 2.6 m passage between them. Lintel 4.4 × 3.2 × 0.5 at 3.74 m, a three-course corbelled relieving triangle with the lion slab (a triangular prism) on top to 5.2 m. Mud-brick parapet with rounded merlons on the masses' south edges. Two open timber door leaves folded back against the jambs, inside. A stone stair on the east mass's north face up to the wall-walk. Bronze torch rings. |
| BronzeWallStraight | WallStraight | A cyclopean wall 2.4 m thick on the south edge, 4.1 m to a plastered wall-walk, built as two or three courses of irregular blocks of varying width. A 1.1 m mud-brick parapet with rounded merlons on the outer edge. Sling slots as dark insets. |
| BronzeWallCorner | WallCorner | The same thick wall on south and west (west stops at south's inner face), with a rectangular cyclopean tower 4.4 × 4.4 m at the SW corner rising 1.4 m above the walk, parapet on its two outer sides. |
| BronzeBastion | Bastion | A rectangular projecting tower 7 × 4.5 m on the south edge, 6 m tall, cyclopean courses, with horns of consecration (two up-curved horns on a plinth) along its parapet: the Age's skyline marker. |
| BronzeGateApproach | Drawbridge | The approach ramp: the thick wall with a 4.2 m opening, a paved limestone strip with cart ruts (flat, walkable) running 5 m inward, and a free-standing stele beside the path. |

**OuterBailey (the lower town's workshops)**

| Key | Role | Build | Loot anchors |
|---|---|---|---|
| BronzeChariotShed | StableBlock | NW: a chariot (box car on a two-wheel axle, wheels as thin cylinders, draught pole resting on a stand). North wall: two horse stalls with clay mangers (NE). SE: fodder bundles. SW: a harness rack. | chariot car, fodder stack |
| BronzeFoundry | BlacksmithShop | NW: round clay furnace (cone), leather bag bellows beside it. NE: a stone mould bench with crucibles. SE: stacks of oxhide ingots on a pallet. SW: a charcoal heap and a water trough. | ingot stack, mould bench |
| BronzeLevyBarracks | BarracksBunk | Low clay sleeping benches with reed mats along the E and W walls in all four quadrants. Spear racks on the north wall, figure-of-eight oxhide shields (two stacked discs) on the south wall. Water jars. | two benches (kit bags), a chest |
| BronzeCistern | WellCourtyard | SE: a raised circular cistern head with a stone surround and a corbelled lid half-drawn. NW: water jars (hydriae) on a stand. SW: a stone trough. NE: a clay basin on a pedestal. | jar stand, basin |
| BronzeOilPress | StorehouseRoom | NW: a lever press: a long beam from a wall socket over a stone press bed, stone weights hanging on a rope that rests on the floor. NE/SE: amphora racks (amphorae standing in a timber rack). SW: small pithoi. | press bed, amphora rack tops |

**InnerWard (the palace)**

| Key | Role | Build | Loot anchors |
|---|---|---|---|
| BronzePithosMagazine | StorehouseRoom (palace) | Art bible. Four clay benches 1.2 m wide × 0.3 m high, one per quadrant, each carrying two rows of three pithoi (lathe jars, 1.7 m above the bench, rope bands). West jars are grain (`leather`), east jars oil (`madder`, lidded). A scribe's bench with tablets in the SW corner. Two step-ladders leaning on jars. | scribe's bench, jar lids ×2 |
| BronzeFrescoCourt | GreatHallMain | Four tapered red columns at (±2.4, ±2.4). Fresco bands (flat panels) on every wall section. Plastered benches along all walls in the quadrants. Two painted offering tables. | offering tables ×2 |
| BronzeShrine | ChapelRoom | NE: a stepped bench altar with horns of consecration, clay idols with raised arms, an offering table. NW: a double axe (labrys) on a stepped stand. SE/SW: low benches for worshippers. | altar, offering table |
| BronzePalaceKitchen | KitchenRoom | NW: a round raised hearth with a tripod cauldron over it. NE: a domed bread oven (cone). SE: a bench of grinding querns. SW: cooking pots and jars on a bench. | quern bench, pot bench |
| BronzeTabletArchive | GuardRoomInner | NE/NW: clay benches stacked with tablet baskets. SE: the scribe's table and stool. SW: the archive guard's post: a spear rack and a stool, and a sealed-jar row. | table, bench tops |

**Keep (the wanax's quarters)**

| Key | Role | Build | Loot anchors |
|---|---|---|---|
| BronzeMegaron | ThroneRoomKeep | Art bible. A flat painted hearth ring 3.6 m across at the centre. Four red tapered columns with black cushion capitals on the 5 m square. A throne with a wavy crest against the east wall in the NE quadrant, facing west, with painted griffin panels either side. Clay benches along the N and S walls in the quadrants. Two bronze tripods by the hearth, three offering tables. | throne foot, offering tables |
| BronzeTreasury | TreasuryVault | Oxhide-ingot stacks on pallets (SE, SW). A plinth with the gold death-mask (NE). Bronze tripods and a shelf of faience (NW). Timber chests with bronze trim. | ingots, plinth, chests |
| BronzeQueensHall | RoyalBedchamber | NW: a bed frame on legs with linen and fleeces. NE: a warp-weighted loom against the north wall (frame, clay weights hanging to the floor). Dolphin fresco panels. SE: a small hearth. SW: a chest. | bed, chest |
| BronzeBathRoom | LordsSolar | NE: a painted clay bathtub on a low plinth, jars beside it. NW: a stone basin on a pedestal. SE: a painted bench. SW: oil flasks on a shelf. | bench, shelf |
| BronzeMegaronStair | KeepStairwell | The kit's L-stair to a gallery (stone flights, timber gallery), horns of consecration on the gallery rail, a chest on the gallery. | gallery chest, a brazier bench |

**Crypt (tholos tombs and shaft graves)**

| Key | Role | Build | Loot anchors |
|---|---|---|---|
| BronzeDromos | CryptAntechamber | Ashlar masses stepping inward along the E and W walls (the dromos walls), a grave stele in each quadrant, amphora offerings on the floor. Painted runner down the N–S axis. | two stele bases |
| BronzeGraveCircle | TombCorridor | A ring of upright slabs (Grave Circle A) in four arcs, one per quadrant, gapped at the four axes. Shaft-grave cover slabs inside each arc. | cover slabs |
| BronzeLarnaxVault | BurialVault | Painted clay larnakes (chest coffins on four short legs with gabled lids), two per quadrant, and storage jars. | larnax lids |
| BronzeTholos | CryptChamberFinal | Corbelled beehive courses: rings of blocks stepping inward as they rise, in the four quadrants, gapped at the axes. A bier with the gold death-mask in the NE quadrant, gold offerings, bronze braziers. | the bier, an offering table |
| BronzeShaftStair | CryptStairwell | The kit's stair-to-dais, a larnax on the dais, jars below. | dais larnax |

### Late Medieval, Burgundian fortress, c. 1450 (`Castle/LateMedieval`)

Fortresses within fortresses: dressed sandstone lined with brick, machicolations, keyhole
gun-loops, conical tower roofs, pavises and the first guns.

**CurtainWall.** Late wall pieces keep their outer face 0.45 m inside the cell edge, so their
machicolated parapets can project outward and still stay inside the cell. All five share that
line, so the wall stays continuous.

| Key | Role | Build |
|---|---|---|
| LateBarbican | Gatehouse | Art bible, adapted. The south wall with a 2.6 × 3.0 m gate between two octagonal towers with conical roofs. Inside, a baffle wall forces the dog-leg. A deck on piers over the first leg with murder-hole gaps, and a hot-sand cauldron and fire basket on it. Oak gate leaves folded back. |
| LateWallStraight | WallStraight | Sandstone wall with a brick frieze, a machicolated parapet (a corbel row carrying the parapet forward), keyhole gun-loops, a timber wall-walk on corbels on the inner face. |
| LateWallCorner | WallCorner | South + west walls and an octagonal corner tower with a machicolated crown and a conical roof. |
| LateBastion | Bastion | An artillery bulwark: a low, wide round tower with a ring of gunports at the base, a thick parapet with wide embrasures, and a bombard on the platform. |
| LateDrawbridge | Drawbridge | A bascule drawbridge: the gate wall, with two counterweight arms pivoting over the gate and chains down to the deck, which runs inward. |

**OuterBailey**

| Key | Role | Build | Loot anchors |
|---|---|---|---|
| LateArtilleryYard | StableBlock | NW: a bombard on a timber sledge. NE: pyramids of stone gunstones. SE: powder kegs. SW: a rammer and sponge rack, spare wheels. | keg tops, a cart |
| LateGunFoundry | BlacksmithShop | NW: a brick furnace with a chimney. NE: a casting pit with an upright clay mould and an A-frame crane. SE: gun barrels on trestles. SW: anvil and tub. | trestle, anvil |
| LateHandgunnerBarracks | BarracksBunk | Pallet beds in all four quadrants, a handgun rack on the north wall, pavises propped on the south wall, a dice table. | table, chests |
| LateBrewhouse | StorehouseRoom | NW: a copper kettle on a brick furnace. NE: an open mash tun. SE/SW: barrels on their sides on cradles, malt sacks. | barrel cradle tops |
| LateTreadwheelWell | WellCourtyard | SE: a treadwheel crane over a well-head: a vertical wheel (a rim of 8 segments, spokes, axle) on A-frame supports, along the east wall. NW: a handcart. SW: buckets and a trough. | cart, trough edge |

**InnerWard**

| Key | Role | Build | Loot anchors |
|---|---|---|---|
| LateCountingHouse | GuardRoomInner (steward's office) | Art bible, adapted. NW: a brick strong room (walls, a barrel-vault cap, an iron-latticed door) holding two iron-bound chests. SE: the counting table with a chequered cloth, a balance and coin stacks. NE: a ledger case against the north wall. SW: a floor strongbox hatch (flat) and coin sacks. Barred window panel on the east wall. | table, strong-room chests, ledger case |
| LateArmouryHall | ArmouredCourtyard | Plate-armour stands in NE/NW, halberd and poleaxe racks on the E/W walls, a grinding wheel (SE), an armourer's table (SW). | table, chest |
| LateSpitKitchen | KitchenRoom | NW: a hooded hearth on the west wall with a long spit on firedogs. NE: a dresser with pewter. SE: a chopping block and table. SW: a flour bin, barrels. | table, dresser |
| LateChantryChapel | ChapelRoom | NE: an altar with a gilded triptych. NW: a tomb chest with a recumbent effigy. SE: a parclose screen corner. SW: a prie-dieu and candle stands. | altar, tomb chest |
| LateLibrary | GreatHallMain (household hall) | Sloped lectern desks with chained books in all four quadrants, tall book presses against the N and S walls, a reading table. | lecterns, table |

**Keep**

| Key | Role | Build | Loot anchors |
|---|---|---|---|
| LateGreatHall | ThroneRoomKeep | Art bible, adapted. Oak dais in NW and NE, the high table on the NE dais with the lord's chair under a cloth of estate and canopy. The dorsal tapestry in two halves either side of the north archway. A hooded hearth on the west wall (SW section). Window embrasure panels on the east wall. Two oak benches, a standing candle stand. | high table, dais |
| LateJewelHouse | TreasuryVault | A stepped plate buffet displaying gilt plate (NE), iron-bound chests (SE, SW), a latticed strong cupboard (NW). | buffet steps, chests |
| LateStateBedchamber | RoyalBedchamber | NW: a tester bed with posts, canopy and side curtains. A chest at its foot. NE: a tapestry panel and prie-dieu. SE: a close-stool and a table. | bed, chest, table |
| LateTapestrySolar | LordsSolar | NE: a desk with a sloped top. NW: a hearth with a high-backed settle. SE: a cupboard. SW: a tapestry and a chest. | desk, chest |
| LateTurretStair | KeepStairwell | The kit's L-stair to a gallery (sandstone flights, oak gallery), a gun-loop panel, a chest on the gallery. | gallery chest |

**Crypt (undercroft and dungeon)**

| Key | Role | Build | Loot anchors |
|---|---|---|---|
| LateUndercroft | CryptAntechamber | Four squat vault piers at (±2.5, ±2.5) with springer blocks. Wine casks on cradles along the E and W walls. | cask cradles |
| LateOubliette | TombCorridor | Barred cells against the E and W walls in the quadrants (bars and a lintel), stocks (NW), shackles on the wall, a floor grate (flat). | stocks bench, a cell shelf |
| LateCharnelHouse | BurialVault | Ossuary shelves in all four quadrants: skull rows (small spheres) on long-bone stacks. | shelf tops |
| LateEffigyCrypt | CryptChamberFinal | NE: the founder's tomb chest with a gilded effigy under a four-post canopy (the prize). Lesser tomb chests in the other quadrants. A candle hearse. | tomb top, lesser tombs |
| LateUndercroftStair | CryptStairwell | The kit's stair-to-dais, a cask on the dais. | dais |

### Age of Powder, Habsburg palace-fortress, c. 1620 (`Castle/AgeOfPowder`)

Lime plaster, black walnut, blued steel, glass by the acre, gilt everywhere, and black powder under
the ballroom. The walls are low, sloped and angular, built to take cannon.

**CurtainWall**

| Key | Role | Build |
|---|---|---|
| PowderRavelinGate | Gatehouse | A monumental gate through a thick rampart with a sloped talus: rusticated pilasters either side of the arch, a triangular pediment above. Two sentry boxes inside. |
| PowderRampart | WallStraight | A low, thick rampart with a talus (triangular prism) on the outer face, a round cordon moulding, a parapet with one wide embrasure, and a cannon on a garrison carriage on the inner platform. |
| PowderSalientCorner | WallCorner | The rampart on south and west meeting at a pointed salient, with a garita (hexagonal sentry turret on a corbel cone) at the point. |
| PowderArrowheadBastion | Bastion | A pentagonal arrowhead bastion (two faces meeting at a point, two flanks), embrasures on the faces, two cannons, a shot pyramid. |
| PowderGabionBridge | Drawbridge | The rampart with a gate opening, a railed timber bridge deck running inward, gabion baskets flanking the entry, a cheval-de-frise pulled aside. |

**OuterBailey**

| Key | Role | Build | Loot anchors |
|---|---|---|---|
| PowderCoachHouse | StableBlock | NW: a coach (a body on four spoked wheels). NE: a horse stall. SE: harness pegs and a saddle stand. SW: hay. | coach seat, saddle stand |
| PowderGunPark | BlacksmithShop | Two field cannons on wheeled carriages (NW, NE), shot pyramids (SE), a rammer and sponge rack and cartridge chests (SW). | cartridge chests |
| PowderMusketeerBarracks | BarracksBunk | Bunks in all four quadrants, musket racks on the north wall, forked rests, a table with powder flasks. | table, chests |
| PowderCooperage | StorehouseRoom | A shaving horse (NW), stacked staves and hoops (NE), finished barrels (SE), a barrel being raised on a firing basket (SW). | barrel heads |
| PowderOrangery | WellCourtyard | Potted orange trees in square tubs (a trunk and a sphere crown), a small basin fountain (SE), glazed panels on the south wall, benches. | fountain rim, bench |

**InnerWard**

| Key | Role | Build | Loot anchors |
|---|---|---|---|
| PowderMagazine | StorehouseRoom (garrison) | Art bible, adapted. Four barrel racks (three tiers, one per quadrant), half-kegs by the south archway, canvas-covered sand banks and water buckets, a lamp-box panel on the east wall, a canvas runner. All four archways open. | rack tops, keg heads |
| PowderBallroom | GreatHallMain | NE: a musicians' dais with a balustrade. Standing candelabra in each quadrant, gilt chairs along the walls, mirrors on the walls, a parquet runner. | dais, side tables |
| PowderBaroqueChapel | ChapelRoom | NE: a tall gilded retable altar (columns and a pediment). NW: a pulpit on a stem. SE/SW: pews. An organ case against the west wall. | altar, pulpit |
| PowderCopperKitchen | KitchenRoom | NW: a brick range with fire holes. NE: a dresser with copper pans. SE: a big table. SW: a spit jack and a sugar-loaf shelf. | table, dresser |
| PowderParterreCourt | ArmouredCourtyard | Four parterre beds (low box hedges) in the quadrants, each with a small fountain or a topiary cone, urns on pedestals, benches. | urn tops, fountain rims |

**Keep (six rooms, one more than the other Ages)**

| Key | Role | Build | Loot anchors |
|---|---|---|---|
| PowderLongGallery | LordsSolar (gallery) | Art bible, adapted. Two tall window panels (walnut frames, glass) on the east wall either side of the archway, with window seats. Two Venetian mirrors on the west wall. Four portraits on the N and S walls, each with a walnut side table under it. Pilasters, a walnut wainscot, a parquet runner, standing candelabra. | side tables ×4, window seats |
| PowderKunstkammer | TreasuryVault (cabinet) | Art bible, adapted. Walnut wall cases (a cupboard base and a glazed upper case) along the wall runs in every quadrant. A crocodile laid along the north case top. Two globes on stands (NW, SE). A central-style table with the curiosity cabinet moved to the NE quadrant. | table, case bases |
| PowderAudienceChamber | ThroneRoomKeep | NE: a throne under a baldachin on a stepped dais behind a balustrade rail. Portraits, a carpet on the approach. | dais foot, side table |
| PowderParadeBedchamber | RoyalBedchamber | NW: a state bed in an alcove behind a balustrade. A writing cabinet (NE), a mirror, a chest (SE). | bed, cabinet |
| PowderSilverVault | TreasuryVault | Iron Augsburg money chests (SE, SW), shelves of silver plate and tureens (NE), a screw coin press (NW). | chests, shelves |
| PowderGrandStair | KeepStairwell | The kit's L-stair to a gallery with a balustrade, a bust on a plinth, a chest on the gallery. | gallery chest, plinth |

**Crypt (casemates and the family vault)**

| Key | Role | Build | Loot anchors |
|---|---|---|---|
| PowderCasemate | CryptAntechamber | Heavy piers and vault springers along the walls, cannonball racks, a bricked gun embrasure panel. | ball racks |
| PowderCountermine | TombCorridor | Timber shoring frames against the E and W walls in the quadrants, a mine charge (a keg stack with a fuse), a listening drum, spoil baskets, tools. | charge kegs, drum |
| PowderFamilyVault | BurialVault | Ornate metal sarcophagi on feet with cushion lids, two per quadrant. | lids |
| PowderImperialTomb | CryptChamberFinal | NE: a monumental double sarcophagus on lion feet with a crown cushion on top (the prize), under a baldachin on four posts. Urns and candle stands. | sarcophagus top, urn |
| PowderCryptStair | CryptStairwell | The kit's stair-to-dais, a small metal sarcophagus on the dais. | dais |

### Door plugs, every Age

`BronzeDoorPlug<Zone>`, `LateDoorPlug<Zone>` and `PowderDoorPlug<Zone>` for OuterBailey, InnerWard,
Keep and Crypt: the same size as the High Medieval plug for that zone (the archway is the same),
in that Age's wall pigment.

## Verification

1. `python3 Tools/ArtBible/build_room_sheets.py --models` passes: every sheet validates, and every
   model's loot anchors match its sheet.
2. `Tools/AssetPipeline/run_pipeline.sh` passes for all modules: gate 1 (Blender: transforms,
   manifold, UV, budget, footprint, floating islands, walkway), gate 2 (trimesh), previews, and the
   contact sheets.
3. The High Medieval set does not change: all 42 pre-existing assets report `unchanged`.
4. The per-Age contact sheets are reviewed by eye, beside the room sheets, and each Age reads as its own place.

## Not in this pass (needs the Unity Editor)

The meshes are the deliverable here. Making them playable needs three more steps, all in the
Editor:

1. `CastleRoomModuleData.Era` (a `HistoricalEra` field) and `ProceduralCastleGenerator` filtering
   by it, so a raid in a given Age draws from that Age's set. This is the schema change
   `docs/ProjectState.md` calls out for M3.
2. Prefabs for each new FBX (a `MeshCollider` per piece plus `CastleRoomModule` with sockets),
   the same way `CastleDoorPlugForge` builds the plug prefabs, and registry entries for them.
3. `Import Castle Loot Anchors`, then the navigation audit (`CastleAudit.cs`) in each Age.
