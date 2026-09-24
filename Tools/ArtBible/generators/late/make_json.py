import json

age = {
    "slug": "late",
    "name": "The Late Medieval",
    "year": "c. 1450",
    "stratum": "III",
    "headline": "Plate, powder and suspicion",
    "intro": ("Mark the century where the builders finally took us personally. Every gate here is crooked on purpose, "
              "every vault has holes in its ceiling for the sand, and the guards walk to a timetable pinned inside the gatehouse. "
              "It is also the first century where the household keeps powder of its own — loud, smoky, wildly inaccurate, and aimed at you. "
              "The Burgundian court is the richest on earth, so the loot is the best we have yet stolen: gilt-etched harness, "
              "tapestries worth a manor, bankers' ledgers, jewels the size of a thumbnail. And, hear me, they hunt in pairs now: "
              "a gun behind a painted shield, a halberd walking beside a halberd. Split them, or be split."),
    "palette": [
        {"name": "Dressed sandstone", "hex": "#A88B64", "use": "ashlar walls, gate passages, hall and counting-house masonry"},
        {"name": "Blackened plate", "hex": "#2E2F31", "use": "the guards' sallets and harness, door straps, grilles, gun iron"},
        {"name": "Burgundian livery red", "hex": "#9E2A2F", "use": "CLOTH ONLY: the ragged saltire on white livery (#D6CDB6), sashes, the cloth of estate — never fire or alarm, which stay madder"},
        {"name": "Brick", "hex": "#8A4B32", "use": "vault infill, gun-loop linings, strong-room skin, hearth backs, roof tile"},
        {"name": "Tapestry wool", "hex": "#4F5E3A", "use": "millefleur tapestry grounds, the counting cloth; browns #6B5238 and buff #A8905E go with it"},
        {"name": "Soot", "hex": "#1E1B17", "use": "vault crowns, hood throats, gun muzzles, plate crevices"}
    ],
    "structures": [
        {
            "slug": "crooked-barbican",
            "name": "The Crooked Barbican",
            "zone": "CurtainWall",
            "footprint": "12 × 12 m, one cell",
            "height_m": 5.2,
            "summary": "A bent-entrance gate: in at the south, a vaulted dog-leg, out through the east — watched the whole way from above and from the side.",
            "description": ("Nobody walks straight into a Burgundian castle, and the Crooked Barbican is why. You enter by the outer gate, find yourself in a "
                            "vaulted passage with seven square holes in its ceiling, and discover at the end of it that the way on turns sharply right, "
                            "past a guardroom whose keyhole loops look straight down the corridor you are standing in. Hot sand comes through the holes, "
                            "handgun balls through the loops, and whatever you are carrying has to make the corner. It is a gate built by men who had "
                            "you, specifically, in mind."),
            "build": [
                "Floor slab 12.0 × 12.0 × 0.30 m (module standard); outer walls 1.50 m thick dressed sandstone ashlar, courses 0.50 m, blocks 0.6–1.2 m long, rising 5.20 m above the slab top.",
                "Leg 1 of the passage: 2.60 m wide (x 1.7–4.3 m) running south→north from the outer gate to y 9.3 m; Leg 2: 2.60 m wide (y 6.7–9.3 m) running west→east to the inner gate. The 90° dog-leg is the whole point — keep the inside corner square, no chamfer.",
                "Passage vault: pointed barrel vault, intrados crown +3.80 m, springing at +3.30 m; brick infill (#8A4B32) above the ashlar springers; five transverse ribs 0.25 m wide at 1.9–2.0 m centres; heavy soot in the crown.",
                "Fighting deck on the vault fill at +4.30 m (0.30 m slab), with inner parapets 0.40 m thick × 0.90 m tall on both sides of Leg 1; outer walls carry the 5.20 m wall-walk parapet.",
                "Murder-holes: seven 0.30 × 0.30 m square shafts through vault and deck (four along Leg 1 at y 2.4/4.0/5.6/7.2, three along Leg 2 at x 5.5/7.5/9.5); each shaft is a separate sub-mesh with a madder-lit bottom face for the 'hot sand' VFX.",
                "Keyhole gun-loops (×7): 0.20 m round hole under a 0.90 × 0.08 m sighting slit, brick-lined splays 0.60 m wide on the inner face; two flank the outer gate (south face), two look from the guardroom into Leg 1, one on the east face, two on the west face over the approach.",
                "Guardroom 4.7 × 3.7 m (x 5.8–10.5, y 1.5–5.2) with a 1.0 m door onto Leg 2; bench, rack for handgonnes and a brazier.",
                "Outer gate: 2.60 × 3.00 m two-leaf oak door (leaves 1.30 × 2.95 × 0.10 m), vertical boards on horizontal ledges, 18 iron studs per leaf, two strap hinges; a slot-machicolation 0.25 m wide cut through the gate arch above it.",
                "Inner gate: 2.60 × 3.00 m single oak leaf with a wicket 0.80 × 1.70 m, barred from inside with a 0.20 m oak draw-bar in wall sockets.",
                "Stair block (x 5.0–11.0, y 9.8–11.0): straight flight of 12 steps 0.50 × 0.36 m rising to the deck at +4.30 m.",
                "Dressing: two wall torches in Leg 1, a hot-sand cauldron (0.8 m iron) and brazier on the deck by the second murder-hole, soot streaks above every loop."
            ],
            "sockets": [
                "Door (outer gate), south face centred at x 3.0 m: 2.60 × 3.00 m, leaves hinge inward",
                "Door (inner gate), east face centred at y 8.0 m: 2.60 × 3.00 m",
                "Murder-hole ×7: 0.30 × 0.30 m, vault crown +3.80 → deck +4.30 m, positions as in build",
                "Arrow-loop (keyhole gun-loop) ×7: 0.20 m hole centred +1.20 m above the floor, slit to +2.10 m",
                "Stair: 12 risers north block → deck +4.30 m; deck is open (CurtainWall, no lid)",
                "Guardroom door: 1.00 × 2.20 m onto Leg 2 at x 8.0–9.0 m"
            ],
            "materials": [
                {"name": "Dressed sandstone ashlar", "hex": "#A88B64", "notes": "tiling 2 m, courses 0.50 m; crisp arrises, chipped at hand height; roughness 0.8"},
                {"name": "Rubble core (cut faces)", "hex": "#7A6A52", "notes": "only where the cell meets a neighbour or a section cap; coarse, 1 m tiling"},
                {"name": "Brick lining", "hex": "#8A4B32", "notes": "vault infill and gun-loop splays; 0.25 × 0.08 m bricks, dark mortar; soot-blackened in the vault crown"},
                {"name": "Oak gate", "hex": "#6B4F33", "notes": "vertical boards 0.20 m, grain vertical; worn pale at hand height; roughness 0.7"},
                {"name": "Blackened iron", "hex": "#2E2F31", "notes": "studs, straps, hinges, cauldron; rust bloom at the bottom 0.3 m"},
                {"name": "Soot", "hex": "#1E1B17", "notes": "vertex-colour/decal pass: vault crown, above loops and torches, murder-hole mouths"}
            ],
            "gameplay": [
                "The dog-leg forces two-carrier loot (altarpiece, rolled tapestry, parade armour) to be turned in the corner — it takes 3–4 s and is exactly where the murder-holes above Leg 2 sit.",
                "Murder-holes are an alarm response: a handgunner or halberdier on the deck drops hot sand (area damage, 2.4 m radius, 1.5 s delay after the madder glow). FRANGO on the deck from below collapses the nearest shaft rim and blocks it.",
                "Gun-loops are sight-lines, not openings: handgunners fire through them; a thrown object or IGNIS through a loop reaches the guardroom.",
                "Both gate leaves are breakable (FRANGO, 2 casts) and flammable (IGNIS, burns 20 s); TONITRUS slams them shut.",
                "The guardroom is the patrol schedule's anchor: halberdier pairs start and end their route here."
            ],
            "budget": "≤ 25k tris per cell (vault + ribs ~8k, gates 2k each, deck/parapets 5k); one sandstone tiling set, one brick set, one trim sheet for loops/ribs/door iron",
            "concept": "concept/late/crooked-barbican.svg"
        },
        {
            "slug": "great-hall",
            "name": "The Great Hall",
            "zone": "Keep",
            "footprint": "12 × 12 m, one cell",
            "height_m": 4.6,
            "summary": "The duke's hall: dais and high table under a cloth of estate, a tapestry the width of the room, a hooded hearth, and an arch-braced oak roof.",
            "description": ("Behold where the household eats, and where it keeps its best things in plain sight. The high table stands on its dais "
                            "under a cloth of estate, and behind it hangs a millefleur tapestry eight metres wide that costs more than the village outside. "
                            "The fire in the hooded hearth is the only honest light, which means the far end of the hall is dark, which means the "
                            "tapestry hides the north door very nicely. Everything in here is flammable. You will be tempted."),
            "build": [
                "Floor slab 12 × 12 × 0.30 m; walls 1.00 m thick dressed sandstone ashlar, 4.60 m above the slab top (Keep clear height). Interior 10 × 10 m, flagstone floor 0.60 × 0.60 m.",
                "Roof: four arch-braced trusses at 3.0 m centres (y 2, 5, 8 m + gable). Oak wall posts 0.25 × 0.25 m standing on sandstone corbels at +2.80 m, rising to the wall plate at +4.60 m.",
                "Arch braces 0.22 × 0.20 m, a two-centred curve springing from the post feet at +3.10 m and meeting under the collar; collar beam 5.1 × 0.25 × 0.20 m at +6.20 m; king strut to the ridge.",
                "Principal rafters at 28° from the wall centre-lines to the ridge at +7.52 m, two purlins per side, clay tile covering (#8A4B32) 0.20 × 0.12 m. BREAKS THROUGH the 4.60 m module lid by 2.92 m: author it as a separate 'roof cap' mesh the generator places only where no module sits above; the braces, posts and wall plate below 4.60 m stay in the room mesh.",
                "Low-roof fallback (for stacked layouts): same posts and braces, braces meeting a flat tie-beam at +4.45 m under a boarded ceiling — no cap. Keep both in the kit.",
                "Dais: 9.2 × 2.4 × 0.35 m oak platform along the north wall with a 0.05 m nosing; high table 7.0 × 0.80 m, top at +0.78 m above the dais, trestle legs, linen cloth hanging 0.35 m on the front.",
                "Lord's chair: oak, back 1.65 m tall, centred under the cloth of estate — a red wool hanging 1.6 × 2.5 m with a dagged tester 2.0 × 0.3 m.",
                "Dorsal tapestry: 8.0 × 2.7 m wool, millefleur green ground, brown hunting band, hung from an iron rod on 17 rings at +4.10 m; it covers the north archway (as a separate cloth-simulated or skinned mesh so it can be pulled down and rolled).",
                "Hearth: in the west wall at y 1.9–4.1 m; opening 1.55 × 1.80 m, brick fireback, sandstone jambs 0.35 m and a mantel shelf at +1.80 m; hood projects 1.35 m into the room and tapers into the wall to +4.60 m; firedogs and a log bed.",
                "Two tall two-light windows in the east wall (y 2.4–3.6 and 8.3–9.7), 0.60 × 2.20 m lights, splayed embrasures with stone seats; iron candle wheel (1.4 m, 5 lights) hanging from each central collar; two trestle tables 5.4 × 0.8 m in the body of the hall."
            ],
            "sockets": [
                "Door S: archway 2.60 × 3.31 m centred on the south wall",
                "Door N: archway 2.60 × 3.31 m centred on the north wall, behind the dorsal tapestry",
                "Door E and Door W: archways 2.60 × 3.31 m centred (the hearth is offset south of Door W to clear it)",
                "Window ×2: east wall, 0.60 × 2.20 m lights, sill +1.00 m",
                "Hearth: west wall, 1.55 × 1.80 m opening (light socket for the fire)",
                "Roof-cap socket: top of wall plates at +4.60 m (optional cap mesh to +7.52 m)"
            ],
            "materials": [
                {"name": "Dressed sandstone", "hex": "#A88B64", "notes": "tiling 2 m, courses 0.50 m; warm-lit from the hearth side, soot on the hood"},
                {"name": "Oak (roof and furniture)", "hex": "#6B4F33", "notes": "trim sheet: posts, braces, purlins; long grain along members; smoke-darkened toward the ridge"},
                {"name": "Tapestry wool", "hex": "#4F5E3A", "notes": "millefleur tile 0.40 m; browns #6B5238, buff #A8905E, livery-red and white flowers; soft, roughness 1.0"},
                {"name": "Linen tablecloth", "hex": "#D6CDB6", "notes": "creased folds every 0.6 m; wine stains; roughness 0.9"},
                {"name": "Cloth of estate", "hex": "#9E2A2F", "notes": "red wool velvet-look, cloth only; slight nap sheen"},
                {"name": "Brick and roof tile", "hex": "#8A4B32", "notes": "fireback and roof cap; fireback heavily sooted"},
                {"name": "Soot", "hex": "#1E1B17", "notes": "hood throat, fireback, roof underside above the candle wheels"}
            ],
            "gameplay": [
                "The dorsal tapestry is flammable (IGNIS) and stealable: pulled down it becomes a Rolled Tapestry loot item; while it hangs it hides the north archway.",
                "Fire spreads: tapestry → cloth of estate → tablecloth → trestles over ~40 s; the roof does not burn in a raid's timescale, but the smoke fills the hall above +3.0 m.",
                "The hearth is the room's only honest light; TONITRUS or a thrown bucket kills it and the far end goes dark.",
                "Loot staging: the Gilded Nef sits on the high table; plate on the trestles; the dais is 0.35 m — trip hazard for anyone carrying backwards.",
                "Gothic Man-at-Arms posts here (Keep); the doors are 3.31 m tall, so he never ducks."
            ],
            "budget": "≤ 25k tris for the room (trusses 6k, hearth 2k, dais/table/chair 3k, tapestry 1.5k); roof cap ≤ 4k extra; sandstone tiling set, oak trim sheet, tapestry 1024² tile",
            "concept": "concept/late/great-hall.svg"
        },
        {
            "slug": "counting-house",
            "name": "The Counting House",
            "zone": "InnerWard",
            "footprint": "12 × 12 m, one cell",
            "height_m": 4.0,
            "summary": "The banker's room: a chequered counting table with its balance, ledger shelves, a barred window, a brick strong room behind an iron-bound door, and a strongbox under the floor.",
            "description": ("Here is the room the whole castle exists to protect, and it knows it. The Italian agent counts at a chequered cloth under one "
                            "candle, the ledgers climb the north wall in four tiers, and the good money sleeps behind an iron-bound door with three locks "
                            "and in a box under a flagstone that only he knows lifts. The window has bars. The door has bars. Even the ledgers have chains. "
                            "Bring a crowbar of a word."),
            "build": [
                "Floor slab 12 × 12 × 0.30 m; outer walls 1.00 m dressed sandstone ashlar rising 4.00 m (InnerWard clear); oak ceiling: joists 0.20 × 0.25 m at 1.5 m centres under 0.05 m boards at +4.00 m.",
                "Strong room in the north-west corner: interior 2.9 × 3.2 m (x 1.0–3.9, y 7.8–11.0), walls 0.80 m, faced in brick (#8A4B32) and brick barrel-vaulted to +3.00 m inside; no windows.",
                "Iron-bound door: 1.10 × 2.25 × 0.12 m oak leaf in a 1.30 × 2.35 m sandstone frame; diagonal lattice of 0.06 m iron straps at 0.30 m pitch, 28 studs, a lock plate 0.27 × 0.40 m, a hasp with a 0.16 m padlock, and a barred judas 0.40 × 0.25 m at eye height.",
                "Counting table: 4.0 × 1.2 × 0.78 m oak on four square legs; counting cloth of chequered green wool (0.25 m squares, #4F5E3A / #34422A) hanging 0.35 m with a buff fringe; bench 3.4 × 0.25 m behind.",
                "Table dressing: brass-less iron balance (beam 0.60 m, pans 0.20 m dia, pillar 0.55 m), five coin stacks of gold (orpiment — they are loot) 0.10 m dia × 0.08–0.16 m, an open ledger, one iron candlestick.",
                "Ledger shelves: oak press 3.2 × 0.40 × 3.3 m on the north wall (x 7.6–10.8), four shelves at 0.72 m pitch filled with calf-bound ledgers (0.07–0.14 m spines) and rolled bills in pigeonholes; two coin sacks at its foot.",
                "Barred window: east wall, 0.55 × 1.30 m light, sill +1.30 m, splayed embrasure; grille of 2 vertical × 3 horizontal 0.03 m iron bars leaded into the stone.",
                "Floor strongbox: oak chest 0.90 × 0.55 × 0.63 m with three iron bands and a hasp, sunk in a pit to −0.55 m below the floor top at x 8.4–9.4, y 2.7–3.3; the slab mesh needs the pit; lid flush under a hinged flagstone 1.04 × 0.60 m.",
                "Two more iron-banded chests (1.0 × 0.6 m) inside the strong room; the ledger 'Livre des changes' chained at the table end."
            ],
            "sockets": [
                "Door S, Door E, Door W: archways 2.60 × 2.88 m centred",
                "Door N: archway 2.60 × 2.88 m centred, opening into the passage beside the ledger press",
                "Door (strong room): 1.10 × 2.25 m, iron-bound, south face of the strong room at x 1.9–3.0 m",
                "Window: east wall, 0.55 × 1.30 m, barred, sill +1.30 m",
                "Floor hatch: 1.04 × 0.60 m flagstone over the strongbox pit (−0.55 m)"
            ],
            "materials": [
                {"name": "Dressed sandstone", "hex": "#A88B64", "notes": "tiling 2 m; cooler, less sooted than the hall; clean arrises"},
                {"name": "Brick lining", "hex": "#8A4B32", "notes": "strong room skin and vault; tight joints, a little efflorescence low down"},
                {"name": "Oak", "hex": "#6B4F33", "notes": "door, table, press, chests; polished dark at hand height on the table edge"},
                {"name": "Blackened iron", "hex": "#2E2F31", "notes": "door lattice, bars, chest bands, balance; wear to bare metal on the lock plate and hasp"},
                {"name": "Counting cloth", "hex": "#4F5E3A", "notes": "chequer tile 0.50 m (2 × 2 squares); worn nap where coins slide"},
                {"name": "Coin gold", "hex": "#C9A227", "notes": "ORPIMENT: only on stealable coin stacks; simple cylinders with an edge bevel"},
                {"name": "Soot", "hex": "#1E1B17", "notes": "candle soot on the ceiling boards over the table only"}
            ],
            "gameplay": [
                "The strong-room door takes three FRANGO casts (one per lock) or a key from the Gothic Man-at-Arms; each cast is loud.",
                "The floor strongbox is invisible until AURUM VOCO or a player stamps the flagstone; LEVO lifts the flagstone; the chest inside is 6 st.",
                "The window bars break to FRANGO (one cast) — the only way out that is not a door, and too small for anything over 4 st of loot.",
                "IGNIS on the ledger shelves destroys the Banker's Ledger (worth 0) but lights the room; the counting cloth burns.",
                "Coin stacks on the table are loose loot; knocking the table scatters them."
            ],
            "budget": "≤ 25k tris (room 9k, door 1.5k, press + books 6k, table set 3k, chests 2k); sandstone + brick tiling, one trim sheet for iron and oak",
            "concept": "concept/late/counting-house.svg"
        }
    ],
    "enemies": [
        {
            "slug": "sallet-halberdier",
            "name": "Sallet Halberdier",
            "role": "patrol",
            "zones": ["CurtainWall", "OuterBailey"],
            "height_m": 1.82,
            "summary": "The common guard, in sallet, bevor and brigandine under a livery tabard, walking his route to the minute — always with a partner.",
            "description": ("The halberdier is the reason you learn to tell the time. He and his fellow walk the wall to a schedule pinned in the gatehouse, "
                            "twenty paces apart so that one of them is always looking at whatever the other just walked past. His halberd is two metres of "
                            "ash with an axe, a spike and a hook on the end, and he knows exactly how to use all three. Kill one quietly and the other "
                            "notices in eleven seconds; that is the game."),
            "silhouette": "A dome with a long swept tail over a bulky square torso, and a tall pole with an axe-head flag beside it — the sallet's tail and the halberd head read in the dark before anything else.",
            "build": [
                "Sallet: blackened steel skull 0.31 m dia × 0.22 m, long tail sweeping back 0.28 m past the neck, a 0.25 × 0.015 m sight slit cut at brow level, low comb 0.12 m, five liner rivets round the brim.",
                "Bevor: 0.25 m wide × 0.23 m tall chin-and-throat plate strapped at the back of the neck, a central ridge and one lame at the throat, 3 rivets per side.",
                "Brigandine: fustian-covered (#4A3A2C) coat of small plates, 0.46 m across the chest tapering to 0.32 m at the waist, skirt flaring to 0.44 m at 0.80 m height; tinned iron rivets 10 mm in triangles of three, rows every 0.05 m.",
                "Livery tabard over it: white wool 0.30 m wide front and back panels, open sides, from shoulder to 0.77 m, with the Burgundian ragged saltire in livery red (knotted branch arms 0.05 m wide).",
                "Spaulders: three-lame shoulder defences 0.16 m wide, top plate domed; couters with small fan; poleyns 0.13 m with side fans at the knee.",
                "Sleeves and upper arms in riveted mail, 8 mm rings, 0.09 m wide limbs; russet leather gloves.",
                "Belt 0.035 m with iron buckle; a baselard in a leather scabbard (0.25 m) on the left hip.",
                "Hose dark brown wool, ankle shoes with a turned-down cuff, dusty at the toe.",
                "Halberd (separate prop, prop_R bone): ash shaft 1.93 × 0.034 m with iron shoe; head 0.46 m — axe blade 0.21 × 0.28 m with a pierced trefoil, a 0.11 m top spike, a back fluke 0.08 m, langets 0.33 m down the shaft; overall 2.08 m.",
                "Carry: at 'order' the halberd stands to 2.08 m; through OuterBailey archways (2.59 m) he walks it at the slope (tip 1.95 m) — no crouch needed for the body (1.82 m).",
                "Wear: edge highlights on the sallet brim and spaulder rims, soot flecks on the tail, dirt up the tabard hem to 0.15 m."
            ],
            "materials": [
                {"name": "Blackened plate", "hex": "#2E2F31", "notes": "sallet, bevor, spaulders, poleyns; cold rim highlight, rubbed bright on edges; roughness 0.45"},
                {"name": "Livery white wool", "hex": "#D6CDB6", "notes": "tabard ground; soft folds from the belt; grime on the hem"},
                {"name": "Livery red", "hex": "#9E2A2F", "notes": "ragged saltire only; cloth, not metal; slightly faded"},
                {"name": "Brigandine fustian", "hex": "#4A3A2C", "notes": "cloth over plates with a grid of tinned rivets #8E8C84; roughness 0.9"},
                {"name": "Riveted mail", "hex": "#6D6E6C", "notes": "tiling 0.05 m ring pattern, normal-map driven; dark between rings"},
                {"name": "Hose and leather", "hex": "#3A2F26", "notes": "hose, belt, gloves, shoes; shoes scuffed pale at the toe"},
                {"name": "Ash shaft", "hex": "#8A7456", "notes": "straight grain; hand-darkened at 0.9–1.1 m"}
            ],
            "rig": {
                "skeleton": "Humanoid (Unity Mecanim) with extra bones: prop_R (halberd), sallet_tail (1 bone, slight lag), tabard_F and tabard_B (2 each for the cloth panels), scabbard_L.",
                "animations": [
                    "patrol_walk — 1.4 m/s, halberd at the slope on the right shoulder, measured step",
                    "patrol_pause — 3.0 s, stops at a waypoint, turns the head left then right, looks for the partner",
                    "partner_call — 1.2 s, raises the free hand and calls; triggers the partner's alert",
                    "alert_turn — 0.6 s, snaps the head toward noise first, body follows, halberd comes to guard",
                    "guard_advance — 1.1 m/s, halberd levelled at chest height, short steps",
                    "thrust — 0.7 s, spike thrust, 2.3 m reach",
                    "hook_pull — 1.0 s, hooks a player's arm or carried loot and drags it 1 m toward him",
                    "chop — 1.1 s, overhead axe, heavy, breaks carried fragile loot",
                    "port_through_arch — 0.8 s, dips the halberd to 1.95 m for a 2.59 m arch",
                    "stagger — 0.9 s, knocked back by TONITRUS or a thrown object",
                    "death_fall — 1.6 s, sallet rolls free",
                    "sleep_slump — SOMNUS: leans on the halberd, sinks to one knee"
                ]
            },
            "breakables": [
                "Sallet detaches on death or a heavy hit (rolls free; not loot)",
                "Halberd drops as a physics prop — players can pick it up and LEVO it, it breaks doors",
                "Tabard tears at the belt at 50% health (swap to a torn tabard mesh)",
                "Bevor strap snaps (bevor hangs from one side)"
            ],
            "budget": "≤ 8k tris (body 6.5k, halberd 0.6k, sallet+bevor 0.9k); one 2048² set",
            "dont": [
                "Don't give him a visored bascinet or great helm — the sallet's long tail is the read",
                "Don't make the saltire gold or madder: it is livery red cloth",
                "Don't let the halberd pass through arches — he slopes it; the prop is longer than he is",
                "Don't use verdigris or lapis anywhere on the costume",
                "Don't make him lone: the pair spacing (20 paces) is part of the design"
            ],
            "concept": "concept/late/sallet-halberdier.svg"
        },
        {
            "slug": "handgunner",
            "name": "Handgunner",
            "role": "ranged",
            "zones": ["CurtainWall", "InnerWard"],
            "height_m": 1.78,
            "summary": "A padded-jack gunner with a hand cannon on an oak tiller and a burning match — loud, slow, inaccurate, and devastating when it lands.",
            "description": ("The century's great novelty, and a dreadful one. The handgunner carries a tube of iron on a stick, a horn of powder and a "
                            "length of smouldering cord, and it takes him a count of twenty to make it go bang. When it does, the whole castle hears it, "
                            "the room fills with smoke, and the ball goes roughly where he was looking. He is never alone: find the painted pavise and "
                            "you have found his gun behind it."),
            "silhouette": "A broad-brimmed kettle hat over a puffed, vertically quilted body, and a stubby tube held diagonally across the chest — with a glowing orange point and a thread of smoke at his hip.",
            "build": [
                "Kettle hat: blackened steel, dome 0.24 m dia × 0.14 m with a low central ridge, brim 0.43 m across drooping 0.03 m at the sides, 4 liner rivets.",
                "Face exposed below the brim: weathered, moustache, the brim's shadow over the eyes.",
                "Padded jack: buff linen #8C7A5E quilted in vertical channels every 0.036 m (28 rows), 0.48 m across at the chest, skirt to 0.78 m; laced front with 6 criss-cross points; high padded collar 0.12 m.",
                "Quilted sleeves, horizontal channels every 0.05 m, puffed at the upper arm to 0.12 m dia; white livery patches 0.075 × 0.12 m with a small red ragged saltire on both upper arms.",
                "Handgonne (prop_R): wrought-iron barrel 0.32 m, 0.09 m at the breech tapering to 0.07 m, three reinforcing rings and a flared muzzle; touch-hole pan on top of the breech; hook lug under the barrel 0.07 m for bracing on a merlon.",
                "Tiller: oak 0.80 × 0.045 m, two iron bands at the barrel socket and one at the butt; total gun length 1.11 m, 5 kg.",
                "Slow match: hemp cord 1.5 m looped over the left wrist, burning end glowing madder (the only madder on him — it is a danger cue), 7-circle smoke wisp; spare coil of match on the bandolier.",
                "Bandolier: buff leather strap 0.05 m from left shoulder to right hip with 4 iron rivets; cow-horn powder flask 0.22 m with iron caps at the right hip; leather shot pouch 0.10 × 0.13 m (18 mm lead balls) at the left hip.",
                "Belt, hose, knee garters, ankle shoes as the halberdier.",
                "Soot on the muzzle, breech and right glove; powder-grey smudges on the jack front."
            ],
            "materials": [
                {"name": "Padded jack linen", "hex": "#8C7A5E", "notes": "quilt channels in the normal map; stitched lines darker #3A3024; grime at cuffs"},
                {"name": "Kettle hat steel", "hex": "#2E2F31", "notes": "blackened; brim edge worn bright"},
                {"name": "Gun iron", "hex": "#3F3C38", "notes": "hammer-forged, pitted; heavy soot at muzzle and pan; roughness 0.6"},
                {"name": "Oak tiller", "hex": "#6B4F33", "notes": "hand-polished at grip points; grain along length"},
                {"name": "Leather and horn", "hex": "#5A4331", "notes": "bandolier, pouch, flask (horn slightly translucent, banded)"},
                {"name": "Livery red", "hex": "#9E2A2F", "notes": "sleeve saltires only; cloth"},
                {"name": "Match glow", "hex": "#C4542E", "notes": "madder emissive on the cord tip only; flickers 4 Hz"}
            ],
            "rig": {
                "skeleton": "Humanoid (Unity Mecanim) with extra bones: prop_R (gun, parented to the right hand with a left-hand IK target on the barrel), match_L (3-bone chain for the cord), flask_R (1 bone, swings).",
                "animations": [
                    "idle_port — gun diagonal across the chest, match hand blows on the cord every 6 s",
                    "patrol_walk — 1.3 m/s, gun sloped on the shoulder",
                    "alert_turn — 0.6 s, head first, then brings the gun to port",
                    "brace_on_wall — 0.8 s, hooks the lug over a merlon or pavise edge",
                    "aim — 1.5 s, sights along the barrel; aim cone narrows slowly (inaccurate: ±6° spread)",
                    "fire — 0.4 s, touches match to the pan; 0.3 s hang-fire, then recoil 0.3 m and a smoke cloud 3 m",
                    "reload — 18 s, swabs, pours from the flask, rams, primes; vulnerable throughout",
                    "hide_behind_pavise — 0.5 s, crouches to 1.20 m behind the partner's pavise",
                    "club_swing — 1.0 s, melee with the tiller when rushed",
                    "misfire_flinch — 1.2 s, the pan flashes without firing (10% of shots)",
                    "death_fall — 1.5 s, gun clatters, match keeps smouldering on the floor",
                    "sleep_slump — SOMNUS: sits against the wall, match cord still lit"
                ]
            },
            "breakables": [
                "Gun drops as a physics prop (not loot); a dropped gun still smoulders the match 10 s and can ignite tapestry",
                "Powder flask detaches: IGNIS on it explodes (1.5 m radius) — the handgunner's weak point",
                "Kettle hat knocks off",
                "Match extinguished by TONITRUS or water: he cannot fire until relit (8 s)"
            ],
            "budget": "≤ 8k tris (body 6.3k, gun 0.7k, bandolier/flask/pouch 0.6k); one 2048² set",
            "dont": [
                "Don't give him a matchlock or trigger — 1450 is touch-hole and hand-held match",
                "Don't make the gun accurate or quiet: the noise and smoke are the mechanic",
                "Don't put madder anywhere except the burning match",
                "Don't make him fight alone in the open — his design assumes the pavisier's shield"
            ],
            "concept": "concept/late/handgunner.svg"
        },
        {
            "slug": "gothic-knight",
            "name": "Gothic Man-at-Arms",
            "role": "heavy",
            "zones": ["Keep", "InnerWard"],
            "height_m": 1.95,
            "summary": "A man in full fluted Gothic white harness with an armet and a poleaxe — the one you hear coming and do not fight.",
            "description": ("Here is the finest thing a German armourer ever made, filled with the least forgiving man in the household. He wears "
                            "thirty kilos of fluted white harness that turns a sword like a roof turns rain, and he carries a poleaxe that ends "
                            "arguments at both ends. He is slow, he is loud, and he cannot see well through the sparrow-beak visor — which is the "
                            "only good news. Do not stand and trade; lead him into the hall and set fire to something."),
            "silhouette": "A tall, narrow-waisted steel figure with huge fluted shoulders, a pointed beak of a face, pointed feet and a short axe-and-hammer polearm — a gleaming shape against the dark, lighter than every other guard.",
            "build": [
                "Armet: rounded skull 0.27 m dia rising to 1.95 m, low comb, a wrapper over the chin, a sparrow-beak visor pivoting on rivets at the temples with two eye slits 0.10 × 0.012 m and 5 breaths on the right side, a 0.08 m rondel disc on a stem at the back of the neck.",
                "Breastplate: two-piece Gothic, narrow waist (0.32 m) flaring to 0.50 m at the chest, a cusped plackart rising to a point at 1.43 m, radiating flutes (4 per side), a lance-rest bracket on the right breast.",
                "Fauld of three lames at 1.05–1.17 m; two pointed tassets 0.19 × 0.27 m each with three flutes; a mail skirt between the legs; culet of three lames behind.",
                "Pauldrons: large and asymmetric (left 10% bigger), three lames each, fluted; round besagews 0.10 m at the armpits.",
                "Arms: rerebraces, couters with big fan wings 0.08 m, vambraces, and fluted gauntlets with flared pointed cuffs.",
                "Legs: fluted cuisses, poleyns with fluted side fans, greaves with a central ridge, sabatons of 7 lames with a pointed (poulaine) toe 0.24 m forward of the ankle.",
                "Livery sash: red wool 0.05 m wide, across the fauld, knotted at the left hip with two tails to 0.87 m.",
                "Poleaxe (prop_R): ash haft 1.40 m with steel langets 0.45 m, rondel hand-guard 0.11 m dia at 1.10 m, head of axe blade 0.20 × 0.22 m, four-point hammer 0.07 m, top spike 0.11 m, butt spike; overall 1.70 m.",
                "Proportions: 1.95 m (1.08 × standard); fits the InnerWard archway (2.88 m) and Keep archway (3.31 m) upright with the poleaxe held vertical — no ducking.",
                "Edges: every plate edge has a 3 mm turned roll picked out bright; soot and fingerprints in the flutes; scratches on the breast at sword height."
            ],
            "materials": [
                {"name": "White harness steel", "hex": "#8E9194", "notes": "polished, anisotropic streaks along the flutes; metallic 1, roughness 0.25; darker in crevices; reads lighter than any other enemy"},
                {"name": "Mail voiders", "hex": "#6D6E6C", "notes": "at the armpits and crotch only; 0.05 m ring tile"},
                {"name": "Livery sash", "hex": "#9E2A2F", "notes": "red wool, cloth only"},
                {"name": "Ash haft", "hex": "#8A7456", "notes": "straight grain, dark at the grips"},
                {"name": "Straps and points", "hex": "#5A4331", "notes": "buckle leathers at every articulation; iron buckles"},
                {"name": "Soot in the flutes", "hex": "#1E1B17", "notes": "AO + soot mask inside every flute and lame overlap"}
            ],
            "rig": {
                "skeleton": "Humanoid (Unity Mecanim) with extra bones: prop_R (poleaxe, left-hand IK on the haft), visor (hinge, opens 70°), tasset_L and tasset_R, sash_tail (2 bones).",
                "animations": [
                    "idle_guard — poleaxe grounded, weight shifts every 4 s, plate creaks",
                    "heavy_walk — 1.0 m/s, clanking footfalls audible at 20 m",
                    "alert_visor — 1.0 s, raises the visor to look, lowers it to fight (blind cone narrows vision to 60° when down)",
                    "charge — 3.2 m/s for 2 s, then must stop and recover 1.5 s",
                    "overhead_axe — 1.4 s, 2.0 m reach, splits carried loot and knocks players down",
                    "hammer_hook — 1.0 s, hooks and pulls a player's legs",
                    "spike_thrust — 0.8 s, short thrust from the half-haft grip",
                    "body_check — 0.6 s, shoulder barge that throws carried loot from the player's arms",
                    "stagger_heavy — 1.2 s, only from TONITRUS or a two-carrier item thrown into him",
                    "fall_and_rise — knocked over: 3.5 s on the floor, 2.0 s to get up (the window to run)",
                    "death_collapse — 2.2 s, falls forward, armet rolls"
                ]
            },
            "breakables": [
                "Armet detaches after heavy hits (reveals the face; vision cone widens back to normal)",
                "Pauldrons and couters can be knocked loose by FRANGO — each piece lost makes him 5% faster and 10% easier to stagger",
                "Poleaxe drops on stagger_heavy; it is too heavy for LEVO by one player (7 st), two can throw it",
                "Carries the strong-room key on his belt: drops on death (the Counting House door)"
            ],
            "budget": "≤ 12k tris (armour 9.5k, poleaxe 0.8k, sash 0.4k, body underlayer culled); one 2048² set",
            "dont": [
                "Don't blacken him — the white harness is how he reads apart from the guards",
                "Don't use orpiment gilding on his harness: none of it is stealable (that is the Parade Armour's job)",
                "Don't give him a great sword or a shield; the poleaxe is the read",
                "Don't make him fast; the charge is short and ends in a stop",
                "Don't exceed 1.95 m with any crest or plume — the armet top is the ceiling"
            ],
            "concept": "concept/late/gothic-knight.svg"
        },
        {
            "slug": "pavisier",
            "name": "Pavisier",
            "role": "special",
            "zones": ["OuterBailey", "InnerWard"],
            "height_m": 1.80,
            "summary": "A shield-bearer in pied livery who carries a huge painted pavise and plants it as moving cover for the handgunner — the pair mechanic.",
            "description": ("Lo, the other half of the gun. The pavisier carries a door — a painted poplar pavise as tall as his chest, propped on its "
                            "own leg — and plants it wherever his handgunner wants to be, then stands behind it with a falchion. Together they advance "
                            "across a courtyard like a small, slow, ill-tempered castle. The pavise stops a spell dead. Smash it with FRANGO, or take it "
                            "away with LEVO and use it yourselves; either way, the gun is suddenly standing in the open."),
            "silhouette": "A man beside a tall slab with a raised centre ridge and a painted red X, bolt shafts sticking out of it; when planted, the slab leans back on a thin prop leg.",
            "build": [
                "Open sallet (no visor, so he can see to plant): blackened steel 0.29 m dia, short tail 0.12 m; mail standard (collar) 0.36 m across the shoulders.",
                "Pied livery coat: wool, wearer's right half white, left half red, sleeves counterchanged; knee-length pleated skirt to 0.72 m with 6 box pleats; over a mail shirt whose hem shows 0.06 m.",
                "Falchion (0.62 m) in a leather scabbard on the right hip; short leather boots to 0.34 m with turned cuffs.",
                "Pavise: 1.30 m tall × 0.62 m wide (0.66 at the arched top, 0.56 at the foot), poplar boards 0.03 m thick glued and covered in canvas and gesso, with a raised central ridge 0.15 m wide × 0.035 m proud running full height.",
                "Pavise face: gesso white field, Burgundian ragged saltire in livery red across the whole board, a black painted briquet (fire-steel) and flint on the ridge with red sparks; flaked paint showing poplar in 16 places; two ball strikes and one crossbow bolt stuck through (0.30 m out the front).",
                "Pavise edge: iron binding 0.01 m all round with 10 dome rivets; two iron spikes 0.05 m at the foot corners.",
                "Pavise back: three rawhide grip loops, and a hinged poplar prop leg 0.85 m pinned at 0.80 m height that swings out to hold it at 4–5° lean.",
                "Carry pose: pavise held by its top grip on the left side, bottom 0.05 m off the floor; planted pose: prop out, pavise leaning back, pavisier crouched behind to 1.20 m.",
                "Height 1.80 m standing; carrying, the pavise rises to 1.35 m — both pass the OuterBailey archway (2.59 m) without crouching."
            ],
            "materials": [
                {"name": "Poplar board", "hex": "#B09A74", "notes": "under the paint: shows in flakes and at strike holes; light, soft grain"},
                {"name": "Gesso white", "hex": "#D6CDB6", "notes": "painted field; crazed crackle normal; grime at the foot"},
                {"name": "Livery red paint", "hex": "#9E2A2F", "notes": "saltire and briquet sparks; painted, slightly worn — not madder"},
                {"name": "Iron binding", "hex": "#3F3C38", "notes": "edge band, rivets, foot spikes, prop pin"},
                {"name": "Sallet steel", "hex": "#2E2F31", "notes": "open sallet; worn bright on the brim"},
                {"name": "Mail", "hex": "#6D6E6C", "notes": "standard and shirt hem; ring tile 0.05 m"},
                {"name": "Leather", "hex": "#5A4331", "notes": "grips, scabbard, boots, gloves"}
            ],
            "rig": {
                "skeleton": "Humanoid (Unity Mecanim) with extra bones: prop_L (pavise; becomes a separate physics object when planted), pavise_prop (hinge for the leg), coat_skirt_F/B (2 each), scabbard_R.",
                "animations": [
                    "carry_walk — 1.2 m/s, pavise on the left, held by the top grip, head turned to his gunner",
                    "plant — 1.1 s, drives the foot spikes in, kicks the prop out; pavise becomes static cover",
                    "crouch_cover — holds behind the planted pavise at 1.20 m, peeks every 3 s",
                    "uproot_advance — 1.4 s, lifts the pavise and moves 3–4 m toward the gunner's next point, then plants again",
                    "shield_bash — 0.8 s, shoves with the pavise face, knocks a player 2 m",
                    "falchion_cut — 0.9 s, when separated from the pavise",
                    "pavise_stolen_react — 0.7 s, stumbles, draws the falchion, calls for the gunner to fall back",
                    "alert_turn — 0.6 s, head first, then swings the pavise toward the noise",
                    "stagger — 0.9 s",
                    "death_fall — 1.5 s, pavise topples face down",
                    "sleep_slump — SOMNUS: sits behind the pavise"
                ]
            },
            "breakables": [
                "Pavise: 3 damage states — whole; cracked (after one FRANGO: a split along the ridge, a third of the board hangs); shattered (second FRANGO: breaks into 4 boards, the prop leg and the bolt drop)",
                "Pavise can be stolen: LEVO or a grab lifts it (4 st, two-handed) and players can plant it as their own cover",
                "Pavise burns: IGNIS catches the canvas/gesso, 12 s to char through",
                "Open sallet knocks off"
            ],
            "budget": "≤ 8k tris (body 5.8k, pavise 1.4k incl. prop and bolt, falchion 0.3k); one 2048² set (pavise on its own 1024² if split)",
            "dont": [
                "Don't make the pavise metal — it is painted wood; FRANGO and IGNIS must visibly work on it",
                "Don't paint the pavise with orpiment gold; the device is black and red",
                "Don't let him fight at range; he has no missile weapon — his job is cover",
                "Don't separate him from the handgunner in spawn: they are placed as a pair, 2–4 m apart"
            ],
            "concept": "concept/late/pavisier.svg"
        }
    ],
    "items": [
        {
            "slug": "parade-armour",
            "name": "Parade Armour on its Stand",
            "worth": 900, "bulk": 13, "fragility": 6, "artifact": False,
            "dimensions": "0.72 × 0.72 × 1.90 m (W × D × H), on its cross-foot",
            "summary": "A gilt-etched Burgundian harness mounted on an oak stand: heavy, awkward, and it comes apart when dropped.",
            "description": ("The duke's second-best armour, the one for being looked at, stands in the hall on an oak post like a patient guest. "
                            "Every plate edge carries a band of etched gilding, and the gilding is the loot. It takes two of you, it will not fit "
                            "through anything narrower than an archway, and if you drop it the harness bursts off its straps across the floor like "
                            "a startled knight. Pick up the pieces; each is worth something, the whole far more."),
            "build": [
                "Stand: oak cross-foot of two beams 0.72 × 0.08 × 0.08 m, halved together, with a centre block 0.14 × 0.14 × 0.08 m; post 0.06 m square to 1.40 m; a shoulder yoke hidden in the pauldrons; iron straps at the joints.",
                "Harness (one mesh per detachable piece): sallet 0.31 m with a gilt brow band 0.025 m and a bevor; gorget; breastplate with cusped plackart and radiating flutes; fauld of three lames; two pointed tassets; pauldrons; arm defences hanging to 0.75 m; gauntlets.",
                "Gilt etching: three vertical bands 0.036 m wide on the breast (centre and ±0.105 m), and gilt borders on every plate edge 2–3 mm wide; etched pattern is a running scroll (normal + a black-line albedo detail).",
                "Gilt rubbed back to white steel where hands have lifted it: breast bands at 1.20–1.40 m, pauldron rims.",
                "Velvet-faced shoulder straps (crimson, 0.03 × 0.09 m) with gilt buckles; arming points of red cord.",
                "Scale reads as a man: sallet crown at 1.90 m with the stand; the tassets end at 0.78 m and the oak post shows below them.",
                "Pieces on break: sallet+bevor, 2 pauldrons, 2 arm defences, breastplate+fauld+tassets, stand = 7 physics pieces."
            ],
            "materials": [
                {"name": "White harness steel", "hex": "#8E9194", "notes": "polished; anisotropic along the flutes; metallic 1, roughness 0.25"},
                {"name": "Gilt etching", "hex": "#C9A227", "notes": "ORPIMENT — this gold is the loot; bright on the bands, worn to steel at grip points"},
                {"name": "Oak stand", "hex": "#6B4F33", "notes": "waxed, dark in the joints; grain along members"},
                {"name": "Velvet facing", "hex": "#7A2228", "notes": "crimson strap covers; nap sheen; cloth only"},
                {"name": "Stand iron", "hex": "#3F3C38", "notes": "joint straps and bolts on the cross-foot"},
                {"name": "Leathers", "hex": "#5A4331", "notes": "internal straps visible at the armpits and tasset hangers"}
            ],
            "grab": "Two carriers: one at each end of the cross-beam (left and right feet at 0.06 m), or one at the post (0.45 m) and one at the yoke under the pauldrons (1.45 m).",
            "breaks": "Dents first, then above 6 m/s it scatters into 7 pieces (sallet, 2 pauldrons, 2 arm defences, cuirass with tassets, stand); each piece is its own loot item worth a share (sallet 120, cuirass 300, each pauldron 90, each arm 70, stand 20 — total 760, less than the whole). Nothing spills.",
            "budget": "≤ 5k tris (dual-carry), 1024² set; each break piece closed and capped",
            "concept": "concept/late/parade-armour.svg"
        },
        {
            "slug": "rolled-tapestry",
            "name": "Rolled Tapestry",
            "worth": 650, "bulk": 11, "fragility": 999, "artifact": False,
            "dimensions": "3.40 × 0.45 × 0.45 m (length × diameter)",
            "summary": "Eight metres of millefleur wool rolled on itself and lashed with hemp: unbreakable, unwieldy, and very flammable.",
            "description": ("The finest thing in the hall, rolled up like a carpet in a hurry. A Tournai tapestry is worth a small manor, and it weighs "
                            "rather like one: two of you, one at each end, and the corners of the Crooked Barbican were not designed with you in mind. "
                            "You cannot break it. You can, with one careless IGNIS, turn it into the most expensive torch in Burgundy."),
            "build": [
                "Roll: cylinder 3.40 m long × 0.45 m diameter (~14 turns of 3 mm cloth); 16-sided, both end caps modelled with a spiral groove.",
                "Outer turn: millefleur green field with red, white and buff flowers (albedo tile 0.40 m), brown/buff guard border visible at the ends.",
                "Linen wrapper: 1.15 m band round the middle third, loose-sewn with visible seams, slightly larger radius (+0.01 m).",
                "Four hemp lashings (0.02 m cord) at 0.47, 1.30, 2.30 and 3.05 m from the near end, each a torus with a knot and a 0.25 m trailing end.",
                "Loose outer flap 0.8 × 0.6 m at the near end that drapes onto the floor (a separate skinned flap, 2 bones) showing the border and warp fringe.",
                "Soft deformation: the roll sags 0.03 m in the middle when carried (blendshape)."
            ],
            "materials": [
                {"name": "Field green wool", "hex": "#4F5E3A", "notes": "millefleur tile 0.40 m; soft, roughness 1.0; woven-weave normal"},
                {"name": "Wool brown", "hex": "#6B5238", "notes": "guard border and hunting figures"},
                {"name": "Wool buff", "hex": "#A8905E", "notes": "flowers, border scroll, warp fringe"},
                {"name": "Wool red", "hex": "#9E2A2F", "notes": "flower dots; livery red, cloth — not madder"},
                {"name": "Linen wrapper", "hex": "#C9BFA6", "notes": "creased, seam lines, a few dirt marks"},
                {"name": "Hemp cord", "hex": "#8A7456", "notes": "twisted rope normal, 0.02 m"}
            ],
            "grab": "Both ends — one carrier each, hands in the end spirals or on the outermost lashing (0.47 m and 3.05 m from the near end).",
            "breaks": "Does not break (fragility 999). It burns: IGNIS catches it, flames travel along the roll at 0.2 m/s and worth falls 10% per second while alight; a burnt roll is 0 coin and leaves a charred stub. TONITRUS or dropping it rolls it 2–3 m.",
            "budget": "≤ 5k tris (dual-carry), 1024² set",
            "concept": "concept/late/rolled-tapestry.svg"
        },
        {
            "slug": "bankers-ledger",
            "name": "Banker's Ledger",
            "worth": 140, "bulk": 1.5, "fragility": 999, "artifact": False,
            "dimensions": "0.30 × 0.22 × 0.09 m (W × D × H), plus 0.60 m of chain",
            "summary": "A calf-bound ledger of the Medici agent's accounts, torn off its desk chain, stuffed with sealed letters of credit.",
            "description": ("Not gold, but what gold is written down in. The banker's ledger lists who owes the duke what, and the letters of credit "
                            "tucked in its leaves can be turned into coin in any city with an Italian on a street corner. It is light, it is modest, "
                            "and it is chained to the counting table — or was, until you pulled. Keep it well away from IGNIS."),
            "build": [
                "Book block 0.288 × 0.208 × 0.066 m of rag paper (~480 leaves), fore-edge and tail with page-line normal detail.",
                "Oak boards 0.012 m covered in brown calf, blind-tooled double frame and a diagonal cross; rounded spine 0.015 m.",
                "Five blackened iron bosses (0.04 × 0.025 m domes) at the corners and centre of the front board; a paper title label 0.10 × 0.03 m 'Livre des changes'.",
                "Two iron clasps: leather straps from the top board catching iron pins on the bottom board's fore-edge.",
                "Library chain: 22 iron links (each 0.035 × 0.02 m) from a staple on the back-left corner, 0.60 m long, ending in the wrenched desk staple with an oak splinter still on it (skinned chain, 6 bones).",
                "Two letters of credit (folded paper 0.07 and 0.05 m wide) sticking 0.03 m out of the fore-edge, each with a red-brown wax seal on a parchment tag."
            ],
            "materials": [
                {"name": "Calf cover", "hex": "#5A3A26", "notes": "tooled lines darker; corners worn pale; roughness 0.7"},
                {"name": "Oak boards", "hex": "#6B4F33", "notes": "only visible at worn corners and the splinter"},
                {"name": "Rag paper", "hex": "#D8CCAE", "notes": "page edges; slightly grubby at the fore-edge where thumbed"},
                {"name": "Blackened iron", "hex": "#2E2F31", "notes": "bosses, clasps, chain; rubbed bright on boss domes"},
                {"name": "Sealing wax", "hex": "#8A3A2A", "notes": "two seals; wax, not madder; slight gloss"}
            ],
            "grab": "One hand, anywhere on the cover; the chain trails and can snag on doors (1 s delay).",
            "breaks": "Does not shatter (fragility 999). IGNIS ruins it: the ledger chars in 3 s and its worth drops to 0; the chain falls away as scrap. Dropping it does nothing but spill the two letters of credit (they stay part of the item).",
            "budget": "≤ 1.5k tris (hand-held), 1024² set",
            "concept": "concept/late/bankers-ledger.svg"
        },
        {
            "slug": "jewelled-hat-badge",
            "name": "Jewelled Hat-Badge",
            "worth": 1200, "bulk": 0.2, "fragility": 999, "artifact": True,
            "dimensions": "0.065 × 0.014 × 0.080 m (W × D × H, with the pearl drop)",
            "summary": "A gold quatrefoil brooch with white enamel roses, a table-cut balas ruby and seed pearls — the smallest, dearest thing in the castle.",
            "description": ("Behold the whole raid in a thumbnail. The duke wore this on his hat at Arras, and it is worth more than the harness, the "
                            "tapestry and the ledger together. You could lose it in your beard. You very nearly will. When AURUM VOCO sounds it "
                            "outshines the treasury, which is how you will know it is in the room at all."),
            "build": [
                "Frame: gold quatrefoil of four lobes (lobe radius 0.015 m on 0.0135 m offsets), 2 mm thick, with a beaded rim of 0.6 mm grains.",
                "Inner field of white opaque enamel inset 0.8 × the frame; four green enamel leaves on the diagonals with gold midribs.",
                "Four enamel roses en ronde bosse, one on each lobe: five white petals 2.6 mm round a gold centre, raised 1.5 mm.",
                "Centre: square table-cut balas ruby 12 mm, set diamond-wise in a gold box collet with four claws; a bright table highlight.",
                "Seven seed pearls 5.5 mm on the rim (lobe tips and notches); the bottom lobe carries a gold loop, a gold cap and a pearl drop 11 × 13 mm.",
                "Back: flat gold plate with a hinged pin 0.05 m and a C-catch; built as a real-scale mesh (the game may up-scale it 1.5× for pickup readability — note in the prefab).",
                "Wear: paler gold on the high points of the lobes where it has been pinched."
            ],
            "materials": [
                {"name": "Gold", "hex": "#C9A227", "notes": "ORPIMENT — high polish, metallic 1, roughness 0.15; paler rub on the high points"},
                {"name": "White enamel", "hex": "#E8E2D4", "notes": "glassy, roughness 0.1, slight subsurface"},
                {"name": "Green enamel", "hex": "#4F6A3A", "notes": "translucent-looking, darker at the edges"},
                {"name": "Balas ruby", "hex": "#7A1E3A", "notes": "table highlight #C85A78; refractive look faked in albedo"},
                {"name": "Pearl", "hex": "#D9D2C0", "notes": "soft sheen, rim-lit; roughness 0.3"}
            ],
            "grab": "Pinch between finger and thumb at the frame's side lobe; one hand, fits in a fist.",
            "breaks": "Does not break (fragility 999, artifact). Worst case on a hard throw the pearl drop swings; nothing detaches and nothing spills.",
            "budget": "≤ 1.5k tris (hand-held), 1024² set",
            "concept": "concept/late/jewelled-hat-badge.svg"
        },
        {
            "slug": "gilded-nef",
            "name": "Gilded Nef",
            "worth": 1100, "bulk": 4, "fragility": 3, "artifact": False,
            "dimensions": "0.52 × 0.18 × 0.56 m (L × W × H, bowsprit to stern, foot to masthead)",
            "summary": "A silver-gilt table ship that holds the duke's salt: gorgeous, heavy, and rigged with silver wire that snaps if you breathe near it.",
            "description": ("Mark the nef, the grandest salt cellar ever cast: a whole carrack in silver-gilt riding on a baluster foot, with castles fore "
                            "and aft, three masts, and two tiny sailors in the fighting top. It marks the lord's place at the high table. It is worth "
                            "a fortune intact and rather less as scrap, and its rigging is made of wire the thickness of a hair. Carry it like an egg; "
                            "throw it at nobody."),
            "build": [
                "Foot: lobed domed foot 0.17 m dia × 0.045 m with 4 gadroons per side, baluster stem 0.055 m with a gilt knop at 0.085 m, a cup 0.10 m wide under the hull.",
                "Hull: carrack hull 0.44 m long × 0.18 m beam, keel at 0.128 m, silver-gilt with four raised silver strakes; a niello scroll band 0.012 m below the gunwale; stern windows ×3.",
                "Castles: sterncastle 0.095 × 0.035 m and forecastle rising forward, both with crenellated gilt rails (5 merlons each).",
                "Deck and salt well: a hinged gilt lid 0.12 × 0.03 m between the masts over a 0.10 m salt well (spills salt when broken).",
                "Masts: main 0.30 m above the deck with a gilt fighting top (0.056 m) holding two cast sailors 0.012 m; fore 0.17 m and mizzen 0.15 m with small tops; yards with furled silver sails; a lateen yard on the mizzen; bowsprit 0.09 m.",
                "Rigging: silver wire 0.8 mm (modelled as 3-sided strips, alpha-free): stays, shrouds (4 per side on the main) and a pennant at the main masthead.",
                "Wear: gilt rubbed back to silver at the stern rail and bow (lifting points) and on the foot's rim; tarnish in the crevices."
            ],
            "materials": [
                {"name": "Silver-gilt", "hex": "#C9A227", "notes": "ORPIMENT — the gilding is the loot; high polish on the hull, rubbed to silver at grips"},
                {"name": "Silver", "hex": "#B8B6AE", "notes": "strakes, castles, masts, sails; roughness 0.2"},
                {"name": "Silver wire", "hex": "#D6D4CC", "notes": "rigging strips; bright, thin"},
                {"name": "Niello", "hex": "#2A2A28", "notes": "black inlay band and stern windows"},
                {"name": "Tarnish", "hex": "#5A5A55", "notes": "AO mask in crevices and under the castles"}
            ],
            "grab": "Two hands: one round the baluster stem (0.075 m), one under the stern castle; one carrier.",
            "breaks": "Fragile at 3 m/s: rigging snaps first, the three masts and the bowsprit shed as 4 small pieces, the hull dents, and the salt well spills a 0.5 m puff of salt; the hull+foot remain as a damaged item worth 40% (440 coin).",
            "budget": "≤ 3k tris (two-hand), 1024² set",
            "concept": "concept/late/gilded-nef.svg"
        }
    ]
}

with open("/home/user/PlunderSpell/docs/art/data/late.json", "w") as f:
    json.dump(age, f, indent=2, ensure_ascii=False)
    f.write("\n")
print("ok")
