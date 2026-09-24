import json
P="/home/user/PlunderSpell/docs/art/data/late.json"
age=json.load(open(P))
gh=age["structures"][1]
gh["build"]=[b.replace("; split into four 2.0 m panels on the rod — a stealable piece (see Rolled Tapestry).","; stealable — cut from its rings it drops as the Rolled Tapestry pickup.") for b in gh["build"]]
gh["gameplay"][0]="The dorsal tapestry burns: IGNIS from the hearth side sets it going in 4 s and it drops in 20 s — which destroys a 650-coin loot item (the Rolled Tapestry) and lights the hall for every guard in the Keep."
gh["gameplay"][1]="The tapestry hides the north door; cut it down (LEVO the rings or FRANGO the rod, 3 s) and it drops rolled as the Rolled Tapestry, and the hall gains a fourth exit."
assert any("Rolled Tapestry pickup" in b for b in gh["build"])
age["items"]=[
{
 "slug":"parade-armour","name":"Parade Armour on its Stand",
 "worth":900,"bulk":13,"fragility":6,"artifact":False,
 "dimensions":"0.72 × 0.72 × 1.90 m (W × D × H)",
 "summary":"A lord's parade harness of white steel with gilt-etched bands, mounted on an oak cross-foot stand — a two-carrier prize.",
 "description":"Every lord of consequence owns two harnesses: the one he fights in and the one he is seen in. This is the second — white steel, fluted, with bands of etched and gilded foliage running down the breast and round the sallet brow, strapped in crimson velvet with gilt buckles, and standing on its oak post in the corner of a chamber like a guest who will not leave. It is worth a great deal and weighs more than a man, so it needs two of you, a clear corridor, and no sudden ideas about stairs.",
 "build":[
  "Stand: oak post 0.06 m square × 1.40 m, mortised into a cross foot of two oak bars 0.72 m span × 0.08 × 0.08 m, halved at the centre, with a 0.14 m square collar block at the post foot; iron bolt through the collar.",
  "Shoulder yoke: stand iron (#3F3C38) T-bar 0.40 m at the top of the post that carries the pauldrons; a padded leather dummy torso (#5A4331) 0.36 m wide inside the breastplate (not visible except at the gaps).",
  "Sallet with gilt brow: white-steel sallet, skull 0.25 m wide × 0.22 m, sight-slit 0.13 m, tail 0.20 m; a 0.03 m gilt-etched brow band round the rim; a bevor below it 0.20 m wide; crown at 1.90 m.",
  "Breastplate: 0.42 m wide, globose, with 3 vertical gilt-etched bands 0.025 m wide (foliage scroll) and a cusped plackart outlined in a gilt border; fan flutes between the bands.",
  "Pauldrons: fluted white steel 0.24 m wide, gilt-etched rims 0.02 m; crimson velvet straps (#7A2228) 3 cm wide with gilt buckles at each shoulder.",
  "Arms: rerebrace, fan couter and vambrace per side hanging to 0.95 m above the floor, gilt band at each cuff; no gauntlets (they were stolen last year).",
  "Fauld and tassets: 3 lames, gilt-edged; two pointed tassets 0.18 × 0.24 m with gilt borders, hanging to ~1.00 m. No legs — the stand shows the post below the tassets.",
  "Wear: polish bright on high points; gilt rubbed back to steel on the breastplate bands' centres and the pauldron rims where hands have admired it; dust in the flute bottoms."
 ],
 "materials":[
  {"name":"White harness","hex":"#8E9194","notes":"Polished plate: metallic 1.0, roughness 0.18 high points to 0.4 in flutes; the parade finish is brighter than the Gothic Man-at-Arms' working harness."},
  {"name":"Gilt etching","hex":"#C9A227","notes":"Orpiment — the loot: etched foliage bands in the normal (0.3 mm relief), metallic 1.0, roughness 0.3; rubbed off at the centre of each band."},
  {"name":"Oak stand","hex":"#6B4F33","notes":"Post and cross foot: planed, darker at the grab heights, scuffed feet."},
  {"name":"Velvet facing","hex":"#7A2228","notes":"Crimson velvet straps and the edge facings inside the plates: sheen falloff, compressed where buckled."},
  {"name":"Stand iron","hex":"#3F3C38","notes":"Yoke, bolt, collar strap: blackened, roughness 0.6."},
  {"name":"Leathers","hex":"#5A4331","notes":"Arming points, internal straps, dummy torso: dry, cracked, visible at the plate gaps."}
 ],
 "grab":"Two carriers: one on the post at 0.55–0.70 m (GRAB · post), the other on the cross-foot ends (two GRAB points at the bar ends, 0.36 m out from centre). Carried tilted, helmet forward.",
 "breaks":"Above 6 m/s it dents (damage state, worth 700); a second impact above 6 m/s breaks the strap lines — it scatters into 7 pieces (sallet, bevor, breastplate, 2 pauldrons, fauld with tassets, stand). Each plate piece is a 1–2 st pickup worth 60–150 (the gilt pieces most); the bare stand is worth 0. Nothing spills.",
 "budget":"≤ 5k tris (plate 3.6k, stand 0.6k, straps and buckles 0.8k), 1024² set (Albedo, Normal, packed ORM).",
 "concept":"concept/late/parade-armour.svg"
},
{
 "slug":"rolled-tapestry","name":"Rolled Tapestry",
 "worth":650,"bulk":11,"fragility":999,"artifact":False,
 "dimensions":"3.40 × 0.45 × 0.45 m (W × D × H)",
 "summary":"A millefleur verdure tapestry rolled on itself, tied with hemp and part-wrapped in linen — a 3.4 m log of wool that needs two carriers.",
 "description":"Tapestry is the true wealth of a Burgundian house: it keeps the draught off, it tells everyone how rich you are, and it rolls up to travel with you from castle to castle — which is to say it rolls up to travel with us. Fourteen turns of green millefleur wool, a brown-and-buff border with a fringe hanging from one end, lashed with four hemp ties and half-wrapped in linen. It cannot be broken, only burnt, and it is exactly as awkward to carry through a crooked gate as you are imagining.",
 "build":[
  "Roll: cylinder 3.40 m long × 0.45 m dia, ~14 turns of 3 mm wool cloth visible as a spiral on both end faces (model the spiral as 2 insets + normal; hollow core 0.06 m).",
  "Millefleur field: the outer turn shows a green (#4F5E3A) ground scattered with flower sprigs 5–8 cm — red (#9E2A2F), buff (#A8905E) and cream — at ~12 cm spacing.",
  "Loose end: 0.6 m of the outer turn unrolled and hanging to the floor at one end, showing the border — a 0.10 m brown (#6B5238) guard band with a buff wave scroll and a 5 cm wool fringe.",
  "Linen wrapper: 1.15 m band of loose-sewn linen (#C9BFA6) round the middle third, running stitches, a few stains and mildew spots.",
  "Hemp ties: 4 lashings of 1 cm hemp cord (#8A7456) at 0.40, 1.15, 2.30 and 3.05 m along the roll, each knotted with a hanging 0.2 m tail.",
  "Sag: the roll sags 2 cm at the centre when carried by the ends — give it a 3-bone spine.",
  "Wear: dust on the top of the roll, fringe frayed, one tie loose."
 ],
 "materials":[
  {"name":"Field green","hex":"#4F5E3A","notes":"Wool weave normal at 5 mm, roughness 0.95; the millefleur sprig pattern as an albedo tile 0.6 m."},
  {"name":"Wool brown","hex":"#6B5238","notes":"Guard band and spiral shading between turns."},
  {"name":"Wool buff","hex":"#A8905E","notes":"Border scroll, fringe, flower highlights."},
  {"name":"Wool red","hex":"#9E2A2F","notes":"Flower sprigs; not a fire cue."},
  {"name":"Linen wrap","hex":"#C9BFA6","notes":"Coarse linen, running-stitch seams, mildew decals."},
  {"name":"Hemp cord","hex":"#8A7456","notes":"Twisted cord normal, frayed knot tails (alpha)."}
 ],
 "grab":"Two carriers, one at each end (GRAB points 0.15 m in from each end face), carried on the shoulder or at the hip; one player can drag it by one end at a crawl.",
 "breaks":"Unbreakable (999): it does not shatter or fragment. It burns: IGNIS catches it (or a dropped candle, brazier, the Handgunner's match) and while alight its worth falls 10 % per second; beaten out it keeps what is left and gains a scorched damage state; burnt out it is a 0-worth charred roll that lights its surroundings for 20 s.",
 "budget":"≤ 5k tris (roll + spiral ends 3.2k, loose end 1k, ties + wrapper 0.8k), 1024² set (Albedo, Normal, packed ORM) + fire mask.",
 "concept":"concept/late/rolled-tapestry.svg"
},
{
 "slug":"bankers-ledger","name":"Banker's Ledger",
 "worth":140,"bulk":1.5,"fragility":999,"artifact":False,
 "dimensions":"0.30 × 0.22 × 0.09 m (W × D × H), plus a 0.60 m chain",
 "summary":"The house's Livre des changes: a chained, iron-bossed account book with wax-sealed letters of credit tucked in the fore-edge.",
 "description":"The ledger says what everyone owes, to whom, and at what rate — which makes it worth rather more to certain Italians than to us, and rather more to the steward than to either. Calf over oak boards, five iron bosses, two strap clasps, a paper label in a clerk's hand reading Livre des changes, and two letters of credit on wax-sealed tags hanging from the fore-edge. It was chained to a desk; the staple came with it, splinter and all.",
 "build":[
  "Text block: rag paper, 480 leaves, 0.29 × 0.21 × 0.07 m, fore-edge showing line texture; slight cockle at the corners.",
  "Boards: oak 0.30 × 0.22 × 0.008 m each, covered in calf (#5A3A26) turned over the edges; blind-tooled double fillet frame and diagonal lines on the front board; spine on the left, 3 raised bands.",
  "Iron bosses: 5 blackened domed bosses on the front board, 3 cm dia × 1 cm, one at each corner and one at the centre (the centre one is the grab).",
  "Iron clasps: 2 strap-and-pin clasps on the front edge, iron straps 2.5 × 5 cm hooking onto pins on the back board.",
  "Paper label: 0.08 × 0.03 m pasted at the head of the front board, inked \"Livre des changes\".",
  "Letters of credit: 2 folded papers 0.07 × 0.04 m tucked in the fore-edge, each hanging a parchment tag with a red wax seal (#8A3A2A) 2.2 cm dia.",
  "Library chain: 22 iron links, 0.60 m, riveted to a hasp at the head of the back board; the far end is a wrenched iron staple 6 cm with a splinter of oak desk (4 × 1.5 cm) still on it.",
  "Wear: calf rubbed through at the corners and along the clasp edges to show oak, grease on the fore-edge where it is thumbed, ink blots on the label."
 ],
 "materials":[
  {"name":"Calf cover","hex":"#5A3A26","notes":"Smooth leather, roughness 0.55, blind-tooled lines in the normal, rubbed pale at the corners."},
  {"name":"Oak boards","hex":"#6B4F33","notes":"Visible only at worn corners and edges."},
  {"name":"Rag paper","hex":"#D8CCAE","notes":"Fore-edge leaf lines, label and letters; roughness 0.9, grease darkening on the fore-edge."},
  {"name":"Blackened iron","hex":"#2E2F31","notes":"Bosses, clasps, chain, staple: roughness 0.5, bright on the dome tops and link contacts."},
  {"name":"Sealing wax","hex":"#8A3A2A","notes":"Two seals on the credit tags: glossy, roughness 0.25, impressed device in the normal."}
 ],
 "grab":"One hand on the centre boss / spine (GRAB · one hand); the chain trails and clinks — it is a noise source when carried at a run unless the carrier holds the chain (both hands).",
 "breaks":"Unbreakable (999) to impact. IGNIS ruins it: a charred ledger is worth 0 coin and the chain drops free (a 0.5 st scrap pickup, 0 coin). Nothing spills; the letters of credit burn with it.",
 "budget":"≤ 1.5k tris (book 0.7k, chain 0.5k, clasps/bosses/tags 0.3k), 1024² set (Albedo, Normal, packed ORM).",
 "concept":"concept/late/bankers-ledger.svg"
},
{
 "slug":"jewelled-hat-badge","name":"Jewelled Hat-Badge",
 "worth":1200,"bulk":0.2,"fragility":999,"artifact":True,
 "dimensions":"0.065 × 0.014 × 0.080 m (W × D × H)",
 "summary":"A gold quatrefoil hat jewel set with a balas ruby, white enamel roses, seed pearls and a pearl drop — the Age's artifact, small enough to lose in a glove.",
 "description":"The Duke's household wears its wealth on its hats, and this one belonged to somebody who wanted to be noticed from across the hall. A gold quatrefoil no bigger than a thumb-joint, four white roses enamelled in the round, a table-cut balas ruby in four claws at the centre, seed pearls round the rim and a pearl drop swinging below. It is the most valuable thing in the Age and the lightest; it cannot break; and under AURUM VOCO it shines brighter than anything else in the room, which is either very helpful or very embarrassing depending on who else is looking.",
 "build":[
  "Quatrefoil plate: gold (#C9A227) 0.065 m across the lobes × 0.080 m tall with the drop loop, 2 mm thick; 4 lobes each 0.028 m dia; beaded rim (1 mm beads at 2.5 mm pitch).",
  "White enamel roses: 4, en ronde bosse (raised), 0.022 m dia each, 5 petals, a gold boss 4 mm at each centre; enamel #E8E2D4.",
  "Green enamel leaves: 4 small leaves 0.010 × 0.005 m between the roses, #4F6A3A.",
  "Balas ruby: table-cut, 12 mm square set as a lozenge in a raised gold collet 16 mm, held by 4 claws each with a small gold bead; stone 5 mm proud.",
  "Seed pearls: 7 on the rim at the lobe tips and cusps, 5.5 mm dia, each on a gold wire pin.",
  "Pearl drop: pear-shaped pearl 9 × 13 mm on a gold loop 5 mm dia under the bottom lobe, with a small gold cap; swings.",
  "Back: flat gold with a hinged pin 0.07 m and a catch — the side view shows the pin 6 mm behind the plate.",
  "Wear: gold polished bright on the rim beads, darker in the recesses behind the roses; one seed pearl slightly dulled."
 ],
 "materials":[
  {"name":"Gold (orpiment)","hex":"#C9A227","notes":"Metallic 1.0, roughness 0.2; the whole plate, collet, beads, loop and pin; AURUM VOCO emissive boost on this mask."},
  {"name":"White enamel","hex":"#E8E2D4","notes":"Glassy, roughness 0.15, slight translucency at petal edges."},
  {"name":"Green enamel","hex":"#4F6A3A","notes":"Leaves: glassy, roughness 0.15."},
  {"name":"Balas ruby","hex":"#7A1E3A","notes":"Transparent-ish stone, roughness 0.05, bright table facet highlight, darker pavilion."},
  {"name":"Pearl","hex":"#D9D2C0","notes":"Seed pearls and drop: roughness 0.3, soft iridescent sheen (small hue shift at grazing angles)."}
 ],
 "grab":"Pinch grab (GRAB · pinch) at the left lobe; one hand, pocketable — it goes in the carrier's purse slot, not the hands, so it does not slow them.",
 "breaks":"Unbreakable (999): it does not fracture at any speed. The pearl drop can be knocked loose by a fall above 9 m/s (a separate 0-worth pearl that the badge loses 100 coin without). Nothing spills.",
 "budget":"≤ 1.5k tris (plate + roses 0.9k, pearls 0.3k, ruby + collet 0.2k, pin 0.1k), 1024² set (Albedo, Normal, packed ORM) + AURUM VOCO glow mask.",
 "concept":"concept/late/jewelled-hat-badge.svg"
},
{
 "slug":"gilded-nef","name":"Gilded Nef",
 "worth":1100,"bulk":4,"fragility":3,"artifact":False,
 "dimensions":"0.52 × 0.18 × 0.56 m (W × D × H)",
 "summary":"A silver-gilt ship on a lobed foot that holds the lord's salt at the high table — three masts, silver-wire rigging and a hinged salt lid.",
 "description":"A nef is a ship made of money that sits in front of the lord at dinner to tell everyone which end of the table is his. This one is raised silver-gilt: a carrack with a crenellated sterncastle and forecastle, three masts, a fighting top with two tiny cast sailors, rigging of silver wire soldered at every stay, and a hinged lid in the deck under which the salt is kept. It is the finest thing on the Great Hall's high table, and the most delicate: the rigging will not survive being dropped, and the salt will go everywhere.",
 "build":[
  "Lobed foot: silver-gilt, 0.20 m dia, 8 lobes with chased radial ribs, 0.07 m tall; baluster stem 0.08 m with a round knop 0.06 m dia; niello ring at the base of the stem.",
  "Hull: raised silver-gilt, 0.40 m long at the deck × 0.18 m beam × 0.16 m deep, round-bellied; 5 horizontal silver strakes (6 mm ribs) on each side.",
  "Niello band: 0.02 m band below the gunwale, black niello (#2A2A28) inlaid with a silver wave scroll.",
  "Deck and salt lid: silver deck with an oval salt well 0.14 × 0.06 m; a hinged gilt lid over it, lifting at the mainmast foot.",
  "Sterncastle: crenellated silver box 0.10 × 0.14 × 0.05 m with 4 square gunports and a gilt merlon rail (5 merlons); forecastle a smaller silver castle 0.08 m with its own gilt rail, raked forward.",
  "Masts: silver mainmast 4 mm dia rising to 0.56 m overall, with a gilt fighting top (0.05 m dia cup, 2 cast sailors 1.5 cm) and a silver pennant; a silver platform (0.20 m dia disc) at mid-mast; foremast 0.30 m with a small top; mizzen 0.24 m at the stern.",
  "Bowsprit: gilt spar 0.12 m raked 30° from the forecastle, making the 0.52 m overall length.",
  "Rigging: silver wire 0.8 mm dia, ~14 stays and shrouds from the tops to the gunwales, soldered at each end.",
  "Wear: gilt rubbed to silver on the gunwale and the foot lobes (where hands pass it down the table), tarnish (#5A5A55) in the strake grooves and under the castles, a few salt crystals round the lid."
 ],
 "materials":[
  {"name":"Silver-gilt","hex":"#C9A227","notes":"Orpiment: hull, foot, rails, tops, bowsprit, lid — metallic 1.0, roughness 0.25; rubbed through to silver at contact points."},
  {"name":"Silver","hex":"#B8B6AE","notes":"Deck, castles, masts, strakes: metallic 1.0, roughness 0.3."},
  {"name":"Silver wire","hex":"#D6D4CC","notes":"Rigging: model as thin tubes (4 sides) or alpha cards at LOD1+, bright."},
  {"name":"Niello","hex":"#2A2A28","notes":"Black inlay band with silver scroll; roughness 0.4."},
  {"name":"Tarnish","hex":"#5A5A55","notes":"AO-driven tarnish in grooves and under overhangs."}
 ],
 "grab":"Two hands: one round the baluster stem at the knop (GRAB · stem), the other under the sterncastle (GRAB · stern). Carried upright; tipping it spills salt.",
 "breaks":"At 3 m/s the rigging snaps and the three masts and the bowsprit shed (4 small 0-worth pieces plus the fighting top), the hull dents, and the salt spills as a white particle cloud and a 0.5 m decal on the floor — the hull alone is worth 500. A second 3 m/s impact bends the stem off the foot: 2 pieces, hull 350 and foot 100.",
 "budget":"≤ 3k tris (hull + castles 1.3k, foot 0.5k, masts + tops 0.6k, rigging 0.6k), 1024² set (Albedo, Normal, packed ORM).",
 "concept":"concept/late/gilded-nef.svg"
}
]
json.dump(age,open(P,"w"),indent=1,ensure_ascii=False)
