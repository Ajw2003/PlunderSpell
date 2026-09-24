import json
P="/home/user/PlunderSpell/docs/art/data/late.json"
age=json.load(open(P))
age["enemies"]=[
{
 "slug":"sallet-halberdier","name":"Sallet Halberdier","role":"patrol",
 "zones":["CurtainWall","OuterBailey"],"height_m":1.82,
 "summary":"The common guard of the Burgundian house: blackened sallet and bevor, brigandine under a saltire tabard, halberd at the slope — always walking with a partner.",
 "description":"The first thing to understand about this fellow is that there are two of him. Sallet and bevor, a brigandine studded with tinned rivets, a white livery tabard with the ragged red saltire of the house, and a halberd with a pierced blade and a nasty spike; he walks a round written in a book and he walks it with a partner ten paces behind. Knock one down and the other has already seen you do it. He is the patrol of the Late Medieval castle, on the walls and in the outer bailey, and he is the reason you learn to count footsteps.",
 "silhouette":"A rounded helmet sweeping to a long tail behind the head, a pale tabard crossed by a dark X, and a pole with a flag-like axe blade standing beside him 25 cm above the helmet — tail, X, blade.",
 "build":[
  "Body: 1.82 m to the sallet crown, eyes 1.65 m (behind the sight-slit at 1.66 m), shoulders 0.50 m wide; upright, square posture, halberd grounded at the right foot.",
  "Sallet: blackened plate, rounded skull 0.24 m wide × 0.20 m tall with a low central ridge, one-piece, drawn back into a tail 0.28 m long sweeping down to 1.55 m; sight-slit 0.12 × 0.012 m cut in the brow; 12 lining rivets round the rim.",
  "Bevor: chin-and-throat plate 0.20 m wide × 0.18 m, rising to meet the sallet's lower edge at the mouth (face fully hidden), one gorget lame below; strapped round the neck, 3 rivets on the strap.",
  "Brigandine: fustian-covered plate coat (#4A3A2C) from neck to 0.80 m, 0.44 m wide at the chest; small overlapping plates shown only by rows of tinned rivet heads (8 mm, 4 cm pitch), front-opening with 2 buckles.",
  "Livery tabard: white wool (#D6CDB6) over the brigandine, 0.40 m wide, open sides, hem at 0.82 m and ragged/dagged by wear; ragged red saltire (Burgundian cross of St Andrew, #9E2A2F) 0.07 m bars corner to corner, knotted stubs along the edges; same saltire on the back.",
  "Belt: leather 4 cm at 1.02 m, iron frame buckle; a 0.30 m baselard dagger in a sheath on the right hip.",
  "Spaulders: 3 lames each, blackened plate, 0.16 m wide cap over the shoulder, reaching 1.40 m; small pauldron plate on the left only.",
  "Arms: riveted mail sleeves (8 mm rings, #6D6E6C) from spaulder to wrist, blackened plate couter 0.10 m dia at the elbow; russet leather gloves (no gauntlets).",
  "Legs: dark wool hose (#3A2F26), blackened poleyns (knee cop 0.12 m with a small side fan) at 0.52 m; no greaves.",
  "Shoes: turned leather ankle shoes, 0.28 m long, rounded toe, a turned-down cuff at the ankle.",
  "Halberd: 2.08 m overall — ash shaft (#8A7456) 1.93 m, 3.2 cm dia, iron shoe 5 cm; head 0.30 m: square spike top, axe blade 0.20 × 0.16 m with a pierced trefoil hole, back fluke 0.08 m; 2 langets 0.18 m nailed down the shaft.",
  "Wear: rust bloom at every rivet on the sallet and brigandine, the tabard hem grubby to 0.3 m up, polished bands on the halberd shaft at the grip heights (1.0 and 1.3 m)."
 ],
 "materials":[
  {"name":"Plate steel","hex":"#2E2F31","notes":"Sallet, bevor, spaulders, couters, poleyns: blackened-from-the-hammer finish, roughness 0.55; bright only on raised edges and the ridge; rust bloom at rivets."},
  {"name":"Livery white","hex":"#D6CDB6","notes":"Tabard wool: roughness 0.9, grime gradient to the hem, dagged edges in alpha; 0.5 m cloth tiling."},
  {"name":"Livery red","hex":"#9E2A2F","notes":"The ragged saltire: applied wool, slightly raised in the normal; the household colour, not a danger cue."},
  {"name":"Brigandine","hex":"#4A3A2C","notes":"Fustian over plates: faint rectangular plate bumps in the normal, tinned rivet heads bright (#8E9194 highlight) in rows."},
  {"name":"Mail","hex":"#6D6E6C","notes":"Sleeves: 8 mm riveted ring normal tile at 0.1 m; dark AO in the links, oiled sheen."},
  {"name":"Hose","hex":"#3A2F26","notes":"Wool knit, mud to the knee; shoe leather on the same map, cracked at the toe crease."},
  {"name":"Ash","hex":"#8A7456","notes":"Halberd shaft: straight grain, hand-polish bands at 1.0 and 1.3 m."}
 ],
 "rig":{
  "skeleton":"Unity Humanoid (Mecanim) with extra bones: halberd (prop bone on RightHand), sallet (child of Head, detachable), sallet_tail (for the knock-back), tabard_front / tabard_back (spring chains ×2 each), baselard (child of Hips).",
  "animations":[
   "idle_slope — 3 s loop, halberd grounded, weight shifts, head turns to check on his partner every 6 s",
   "walk_round — 1.1 m/s, halberd at the slope on the right shoulder, paced to a partner 10 m behind (shared timeline)",
   "slope_halberd_arch — 0.4 s additive, tilts the halberd back so its tip drops from 2.08 to 1.95 m under the 2.59 m OuterBailey arches",
   "alert_turn — 0.6 s, head snaps to noise first, the halberd comes off the shoulder as the body follows",
   "call_partner — 1.0 s, raises the left hand and calls; the partner's AI is alerted even out of sight",
   "search_pair — 3 s, sweeps the halberd tip low through shadows while the partner covers",
   "run_to_noise — 3.3 m/s, halberd levelled at the hip",
   "attack_thrust — 0.8 s, two-handed thrust with the spike, 2.4 m reach",
   "attack_hook — 1.2 s, reaches past the target, hooks with the fluke and drags back (pulls a player 1 m)",
   "attack_chop — 1.1 s, overhead cut with the axe blade",
   "hit_react — 0.5 s stagger, the sallet tail knocks askew",
   "death_fall — 1.5 s, drops to the knees then forward; the halberd falls clear"
  ]
 },
 "breakables":[
  "Sallet: knocked off on a head hit above 6 m/s, rolls on its tail; underneath is an arming cap and a shaven head.",
  "Bevor: strap cuts at the second head hit, bevor hangs by one side (damage state) — his face is exposed.",
  "Halberd: drops as a physics prop (4 st, two-handed; players can use it to hook items off shelves).",
  "Tabard: two damage states — slashed (saltire half torn off) and scorched.",
  "Loot: a purse of 8–16 coin at the belt."
 ],
 "budget":"≤ 8k tris (body + brigandine 5k, sallet + bevor 0.9k, halberd 0.9k, tabard 1k), one 2048² set (Albedo, Normal, packed ORM).",
 "dont":[
  "Don't give him a full harness of white plate — he is a paid guard, not a knight; brigandine and a few blackened pieces.",
  "Don't paint the saltire straight and clean — it is the Burgundian ragged staff cross, knotted.",
  "Don't let the 2.08 m halberd drive his collider or the arch test: he is 1.82 m and the halberd slopes.",
  "Don't use any gold on him, and no madder — his red is livery red #9E2A2F.",
  "Don't animate him alone: every patrol clip has a partner-aware variant."
 ],
 "concept":"concept/late/sallet-halberdier.svg"
},
{
 "slug":"handgunner","name":"Handgunner","role":"ranged",
 "zones":["CurtainWall","InnerWard"],"height_m":1.78,
 "summary":"A quilted man in a kettle hat with a hand-gonne on an oak tiller and a lit slow match — slow to load, loud, and very bad news at twelve paces.",
 "description":"Here is the future, and it smells of rotten eggs. A padded linen jack, a steel kettle hat, a horn flask of powder and a pouch of lead, and on his shoulder a wrought-iron tube on an oak tiller that he fires by putting a glowing cord to a hole in it and hoping. He is slow — forty seconds to load, and the match gives him away in the dark — but when he fires, the whole ward hears it, and whatever he hits stays hit. He never works alone: find the pavise and you will find him behind it.",
 "silhouette":"A wide round-brimmed hat, a stiff quilted body, a thick stick on the shoulder angled up past the hat, and a small red spark bobbing at his hip — the spark first.",
 "build":[
  "Body: 1.78 m to the kettle-hat crown, eyes 1.64 m under the brim, shoulders 0.48 m; slightly stocky, the weight on the back foot.",
  "Kettle hat: steel, crown 0.22 m dia × 0.14 m with a raised ridge, brim 0.43 m dia turned down 12°, rolled edge; 8 lining rivets round the crown base.",
  "Padded jack: linen (#8C7A5E) quilted in 28 vertical rows 3 cm wide from the stand collar to the hem at 0.82 m, 0.46 m wide; laced up the front with 6 crossings of leather thong; high stiff collar 8 cm; quilted sleeves in rings.",
  "Livery badge: a 0.10 × 0.12 m white cloth patch on each upper sleeve with the red saltire (#9E2A2F), stitched on at 1.35 m.",
  "Belt: leather 4 cm at 1.02 m with iron buckle; shot pouch 0.12 × 0.14 m on the right hip holding 18 mm lead balls; a flap and a toggle.",
  "Powder flask: cow-horn 0.26 m long hung at the left hip on a cord, iron caps at both ends, a spring spout.",
  "Bandolier: buff leather strap 5 cm wide over the left shoulder to the right hip carrying the gun sling.",
  "Handgonne: wrought-iron barrel 0.32 m long, 5 cm bore, octagonal outside 0.075 m across, reinforced with 3 hoops; touch-hole on top of the breech with a sooted flash-pan; a hook lug 6 cm under the muzzle end to brace on a merlon or pavise rim.",
  "Tiller: oak 0.80 m long, 5 × 4 cm section, bound to the barrel by 3 iron bands, tail cut flat; held under the right arm or over the shoulder.",
  "Slow match: hemp cord 1.2 m coiled at the chest (on a toggle), the lit end 0.1 m clipped in the left hand; glowing tip and a thin smoke plume.",
  "Legs: dark wool hose, knee-high leather boots turned down with a 5 cm cuff at 0.50 m.",
  "Wear: powder smut on the right hand, face and jack front, singe marks on the jack cuff, soot plume round the touch-hole."
 ],
 "materials":[
  {"name":"Padded jack","hex":"#8C7A5E","notes":"Quilted linen: vertical channels in the normal (3 cm), roughness 0.9, dirt gradient to the hem, powder-smut decals on the chest."},
  {"name":"Kettle steel","hex":"#2E2F31","notes":"Hat: dark hammered finish, roughness 0.5, bright rim; rain-streak rust from the rivets."},
  {"name":"Gun iron","hex":"#3F3C38","notes":"Barrel, hoops, tiller bands: forge-black, roughness 0.6, heavy soot at the breech and muzzle; pitted."},
  {"name":"Oak tiller","hex":"#6B4F33","notes":"Straight grain, darker oily hand-wear at the grip and tail."},
  {"name":"Leather","hex":"#5A4331","notes":"Belt, bandolier (buff, lighter), pouch, boots: roughness 0.6, cracking at bends."},
  {"name":"Livery red","hex":"#9E2A2F","notes":"Saltire badges only."},
  {"name":"Match","hex":"#C4542E","notes":"Emissive tip of the slow match (and muzzle flash VFX) — the only madder on him, a fire cue."}
 ],
 "rig":{
  "skeleton":"Unity Humanoid (Mecanim) with extra bones: gun (prop bone, switchable parent RightHand / chest shoulder socket), match_cord (4-bone chain from LeftHand, lit tip), flask and pouch (spring, child of Hips), hat (child of Head, detachable), jack_skirt ×2 (spring).",
  "animations":[
   "idle_shoulder — 3 s loop, gun on the shoulder, blows on the match every 5 s (tip brightens)",
   "walk_post — 1.0 m/s, gun at the shoulder, match hand held away from the flask",
   "alert_turn — 0.7 s, swings round toward noise, gun comes down to the hip",
   "brace_and_aim — 1.2 s, rests the hook lug on a merlon, pavise rim or table edge, tiller under the arm, sights along the barrel",
   "fire — 0.8 s, match to the touch-hole, 0.4 s hang-fire hiss, bang, recoil shoves him back 0.2 m; smoke cloud 3 m that lingers 6 s",
   "reload — 40 s loop (interruptible): swabs, pours powder from the horn, rams wad and ball with a rod from his belt, primes the pan",
   "call_pavisier — 1.0 s, shouts for his shield partner, who moves to cover him",
   "retreat_behind_pavise — 1.2 s, crouches to 1.25 m behind the 1.30 m pavise",
   "club — 1.0 s, melee: swings the gun by the tiller like a club",
   "hit_react — 0.5 s, stumbles, the match may drop (see breakables)",
   "death_fall — 1.6 s, backwards; the lit match lands and can ignite spilled powder"
  ]
 },
 "breakables":[
  "Kettle hat: knocked off above 6 m/s, rolls on its brim.",
  "Powder flask: a hit (or IGNIS) near it at the hip ignites it — a 1.5 m flash, sets his jack burning and knocks him down.",
  "Slow match: drops on a hit to the left arm; while unlit he cannot fire (relights at a brazier or torch, 5 s).",
  "Handgonne: drops as a physics prop (3 st); players can fire it once if it is loaded.",
  "Loot: a pouch of 5–10 coin and 6 lead balls."
 ],
 "budget":"≤ 8k tris (body + jack 5.2k, gun 1k, hat 0.6k, flask/pouch/match 1.2k), one 2048² set (Albedo, Normal, packed ORM) + match emissive mask.",
 "dont":[
  "Don't give him a matchlock or a trigger — it is a hand-gonne; the match goes to the touch-hole by hand.",
  "Don't make the barrel long and slim like a musket: 0.32 m, fat and hooped.",
  "Don't put madder on anything but the match tip and the flash; his red is livery red on the badges only.",
  "Don't let him fire more than once in a fight — the reload is his weakness and the game depends on it."
 ],
 "concept":"concept/late/handgunner.svg"
},
{
 "slug":"gothic-knight","name":"Gothic Man-at-Arms","role":"heavy",
 "zones":["InnerWard","Keep"],"height_m":1.95,
 "summary":"A full harness of fluted white Gothic plate, armet and sparrow-beak visor, with a poleaxe — the thing in the Keep you walk around.",
 "description":"Twenty-five kilograms of the finest steel Augsburg can hammer, fluted like a cathedral and polished like a mirror, with a gentleman inside it who was raised from infancy for exactly this conversation. Armet with a sparrow-beak visor, cusped plackart, fan couters and poleyns, pointed sabatons, and a poleaxe with an axe, a four-pointed hammer and a spike, because he likes to have options. He is slow to wake and slow to turn, and your spells mostly slide off him. Do not fight him; do not be seen by him; if you must, make him walk through a door he is too proud to duck.",
 "silhouette":"A tall, bright, spiky man-shape: a round helmet with a pointed beak, big fluted shoulders, a pointed waist, and a short axe on a pole at his side — glitter before shape.",
 "build":[
  "Body: 1.95 m to the armet crown (a tall man plus plate), eyes 1.72 m behind the visor slit, shoulders 0.62 m wide over the pauldrons; stands square, poleaxe grounded at the right side.",
  "Armet: rounded skull 0.25 m wide × 0.30 m tall with a low comb; hinged cheek-pieces closing at the chin, a wrapper plate strapped over the jaw; sparrow-beak visor projecting 0.10 m, twin sight slits 0.08 × 0.01 m, breaths (6 holes, 8 mm) on the right side only; a 0.07 m dia rondel on a 0.05 m stem at the back.",
  "Pauldrons: fluted plate, 0.24 m wide, 4 radiating flutes, reaching down the upper arm to 1.30 m; besagew discs 0.09 m dia at the armpit.",
  "Breastplate + cusped plackart: globose breastplate 0.42 m wide; plackart rising from the waist to a cusped point at 1.52 m, 7 fan flutes; a lance-rest bolt hole on the right.",
  "Fauld and tassets: 3 lames at 1.03–0.95 m, two pointed tassets 0.18 m wide × 0.24 m with 3 flutes each, hanging to 0.82 m.",
  "Arms: rerebrace and vambrace tubes, fan couters with a 0.10 m fluted wing on the outside of the elbow; mitten gauntlets with a flared, fluted cuff 0.11 m.",
  "Mail voiders: mail (#6D6E6C) visible at the armpits and a mail skirt showing between the tassets.",
  "Legs: cuisses with 2 flutes, fan poleyns at 0.50 m with a fluted side wing 0.10 m, full greaves.",
  "Sabatons: 7 lames each, pointed poulaine toe extending 0.08 m past the foot (0.34 m long in all).",
  "Livery sash: red wool (#9E2A2F) 0.08 m wide over the right shoulder to the left hip, knotted at the hip with 0.30 m tails.",
  "Poleaxe: 1.70 m overall; ash haft (#8A7456) 3.5 cm, square iron butt spike; head 0.32 m — axe blade 0.14 × 0.18 m one side, 4-point hammer face the other, top spike 0.18 m; langets 0.20 m and a 0.11 m rondel hand guard at 1.10 m.",
  "Straps: leather (#5A4331) at every hinge — show buckles on pauldrons, cuisses and greave hinges.",
  "Wear: polished-bright high points, darker flute bottoms with soot and old oil, scratches on the plackart point and the poleyn wings; no rust."
 ],
 "materials":[
  {"name":"White harness","hex":"#8E9194","notes":"Polished plate: metallic 1.0, roughness 0.2 on high points to 0.45 in flute bottoms; strong specular silhouette; micro-scratches on the plackart and sabatons."},
  {"name":"Mail voiders","hex":"#6D6E6C","notes":"8 mm riveted rings, normal tile 0.1 m, oiled dark sheen."},
  {"name":"Livery sash","hex":"#9E2A2F","notes":"Soft wool, knot and tails as cloth; the household colour, not a danger cue."},
  {"name":"Ash haft","hex":"#8A7456","notes":"Poleaxe haft: polished at the two grips (0.9 and 1.3 m)."},
  {"name":"Straps","hex":"#5A4331","notes":"Buckled leather at every hinge, darker oiled edges."},
  {"name":"Soot","hex":"#1E1B17","notes":"Crevice dirt in the flutes and lame overlaps (AO multiplier), never on the high points."}
 ],
 "rig":{
  "skeleton":"Unity Humanoid (Mecanim) with extra bones: visor (hinge on Head, opens 70°), rondel (child of Head), poleaxe (prop bone on RightHand), tasset_L / tasset_R and sash_tail ×2 (spring), pauldron_L / pauldron_R (twist helpers to stop shoulder interpenetration).",
  "animations":[
   "idle_stand — 4 s loop, poleaxe grounded, a slow breath visible in the pauldrons",
   "walk_heavy — 0.9 m/s, steel-on-steel footstep audio events every step",
   "alert_turn_slow — 1.1 s, turns the whole body (he cannot turn his head in the armet)",
   "visor_raise — 0.8 s, pushes the visor up to look closer (face exposed for 3 s)",
   "charge — 3.0 m/s, poleaxe levelled, 4 s max, can't turn more than 20°/s",
   "attack_spike_thrust — 0.9 s, top-spike thrust, 2.0 m reach",
   "attack_hammer — 1.4 s, overhead blow with the hammer face; staggers and knocks down",
   "attack_axe_sweep — 1.3 s, wide horizontal sweep at waist height",
   "shrug_spell — 0.6 s additive, flinches and keeps coming when hit by a light spell",
   "hit_react_heavy — 0.7 s, rocks back only on a heavy blow (TONITRUS, a dropped portcullis)",
   "stand_up — 2.4 s, struggles up from prone (his window of vulnerability)",
   "death_topple — 2.0 s, falls straight like a tree, 1.2 m radius crash noise"
  ]
 },
 "breakables":[
  "Visor: FRANGO or a heavy blow tears the visor off its pivots (damage state) — his head then takes normal damage.",
  "Rondel and sash: purely visual damage states (rondel bent, sash cut).",
  "Pauldrons: the left pauldron detaches at the third heavy hit, exposing mail; his guard drops on that side.",
  "Poleaxe: drops as a physics prop (5 st, two-handed).",
  "Loot: a gold signet ring (orpiment, 40 coin) on the right gauntlet's finger — the only gold on him, and only because you can steal it."
 ],
 "budget":"≤ 12k tris (harness 8.5k, armet 1.2k, poleaxe 0.8k, sash + mail 1.5k), one 2048² set (Albedo, Normal, packed ORM).",
 "dont":[
  "Don't make him black or rusty — white harness, polished; he is the brightest thing in the castle except the plunder.",
  "Don't round off the Gothic points: cusped plackart, pointed tassets, poulaine toes and fan wings are his silhouette.",
  "Don't give him a shield or a surcoat over the plate.",
  "Don't let him fit through the Crypt or OuterBailey archways; he is posted to InnerWard and Keep only (the 1.70 m poleaxe passes 2.88 m arches upright).",
  "Don't use orpiment anywhere but the stealable signet."
 ],
 "concept":"concept/late/gothic-knight.svg"
},
{
 "slug":"pavisier","name":"Pavisier","role":"special",
 "zones":["CurtainWall","OuterBailey"],"height_m":1.80,
 "summary":"A liveried man with a 1.30 m pavise painted with the house saltire, who plants it for the Handgunner — the two of them are one enemy.",
 "description":"This is the Age's signature threat, and it is not a man so much as a partnership. The pavisier carries a great painted shield taller than your chest, plants it on its spiked feet with a prop behind it, and crouches there while his friend with the hand-gonne reloads in perfect safety. Pied white-and-red livery, an open sallet so he can see where you went, a falchion for when you arrive; a crossbow bolt still stuck in the shield as a souvenir of the last lot. Separate him from his gunner and he is just a man with a very large board; leave them together and they will take the gate from you a pace at a time.",
 "silhouette":"A tall pale rectangle with a big red X beside a man in a half-white half-red coat — the shield reads first and hides everything behind it.",
 "build":[
  "Body: 1.80 m to the sallet crown, eyes 1.65 m, shoulders 0.48 m; one hand always on the pavise rim.",
  "Open sallet: blackened plate (#2E2F31), rounded skull 0.24 m wide, no visor — face open from brow to chin; short tail 0.18 m sweeping back; lining rivets round the rim.",
  "Mail standard: riveted mail collar (#6D6E6C) 0.10 m tall round the neck and over the shoulders to 1.48 m.",
  "Pied livery coat: wool, split vertically — white (#D6CDB6) on his right, red (#9E2A2F) on his left — close-fitting to the waist, pleated skirt to 0.80 m; sleeves counterchanged (red on the white side, white on the red side).",
  "Mail hem: the hem of a mail shirt showing 4 cm below the coat skirt at 0.76 m.",
  "Belt: leather 4 cm at 1.02 m, iron buckle; falchion in a leather scabbard on the left hip, 0.75 m overall, broad single-edged blade 0.06 m widening to the clipped point.",
  "Legs: dark wool hose, knee boots with a turned cuff at 0.52 m, leather (#5A4331).",
  "Pavise: 1.30 m tall × 0.62 m wide × 0.04 m, curved in plan (0.05 m sagitta), with a raised central spine 0.10 m wide running full height; poplar boards (#B09A74) under a gesso face (#D6CDB6).",
  "Pavise face: painted ragged red saltire (0.10 m bars) corner to corner and a black briquet (Burgundian fire-steel) device 0.16 m wide at the crossing, with red sparks painted round it; 4 ball-shot dents and gesso chips.",
  "Pavise edge and feet: iron binding strip (#3F3C38) 2 cm round the rim; two spiked iron feet 0.05 m at the base corners that bite into turf.",
  "Pavise back: 3 rawhide grip loops on the spine at 0.35, 0.75 and 1.10 m; a hinged poplar prop leg 0.85 m that swings down to stand the pavise at 12° lean.",
  "Stuck bolt: a crossbow bolt 0.35 m buried in the pavise's side edge at 0.65 m, fletching intact — a trophy, not a weapon.",
  "Wear: gesso chipped at the corners and round the dents, the saltire faded where hands grip, mud on the lower 0.2 m of the pavise."
 ],
 "materials":[
  {"name":"Poplar","hex":"#B09A74","notes":"Pavise boards and prop: soft, fine grain, visible at chips and the back face; roughness 0.8."},
  {"name":"Gesso white","hex":"#D6CDB6","notes":"Pavise ground and the white of the livery: chalky, roughness 0.9, chipping mask exposing poplar, crazing at the edges."},
  {"name":"Livery red","hex":"#9E2A2F","notes":"The saltire paint (flatter, cracked) and the red half of the coat (wool)."},
  {"name":"Iron binding","hex":"#3F3C38","notes":"Pavise rim, feet, briquet device paint: dark, roughness 0.6, dented at the corners."},
  {"name":"Sallet","hex":"#2E2F31","notes":"Blackened plate, bright on the rim only."},
  {"name":"Mail","hex":"#6D6E6C","notes":"Standard and hem: 8 mm rings, oiled."},
  {"name":"Leather","hex":"#5A4331","notes":"Belt, scabbard, boots, rawhide grips (lighter, translucent edges)."}
 ],
 "rig":{
  "skeleton":"Unity Humanoid (Mecanim) with extra bones: pavise (prop bone, switchable parent LeftHand / world when planted), pavise_prop (hinge, 0–35°), falchion (prop, sheath on Hips / RightHand), sallet (child of Head, detachable), coat_skirt ×4 (spring).",
  "animations":[
   "idle_shield — 3 s loop, pavise carried upright at the left side, hand on the rim",
   "walk_carry — 1.0 m/s, pavise held in front at 20° lean; keeps within 2 m of his Handgunner",
   "alert_turn — 0.7 s, swings the pavise round toward the noise before his head",
   "plant_pavise — 1.0 s, stamps the spiked feet in, kicks the prop down; creates 0.62 m of full cover for two",
   "crouch_behind — 0.8 s, drops to 1.25 m behind the 1.30 m pavise, peeks round the edge",
   "advance_pavise — 1.6 s cycle, lifts, steps 1 m forward, re-plants — the pair creeps up on a hiding place",
   "shove — 0.9 s, pavise bash, pushes a player 1.5 m and staggers",
   "draw_falchion — 0.6 s, when his gunner is down or the target is within 2 m",
   "attack_falchion — 0.8 s, forehand cut",
   "hit_react — 0.5 s; hits on the pavise play a thud and a chip VFX instead",
   "death_fall — 1.6 s; a planted pavise stays standing as cover the players can use"
  ]
 },
 "breakables":[
  "Pavise: 3 damage states — chipped, split (a crack down one board), and broken in two at 60 damage or one FRANGO; IGNIS burns it in 10 s.",
  "Pavise prop: FRANGO or a shove snaps the prop; the planted pavise falls flat and the Handgunner is exposed.",
  "Sallet: knocked off above 6 m/s.",
  "Falchion: drops as a prop (1 st, one-handed).",
  "Loot: none beyond 4–8 coin — his value is the cover he removes."
 ],
 "budget":"≤ 8k tris (body 5k, pavise + prop 1.6k, sallet 0.6k, falchion 0.4k, bolt 0.1k), one 2048² set (Albedo, Normal, packed ORM).",
 "dont":[
  "Don't make the pavise a knight's heater shield — tall, rectangular, curved, with a central spine and spiked feet.",
  "Don't give him a ranged weapon; the stuck bolt is decoration.",
  "Don't use madder on the painted sparks — they are livery red; madder is reserved for real fire.",
  "Don't let the pavise count toward his height or collider: he is 1.80 m and the pavise is a separate 1.30 m prop.",
  "Don't let him wander more than 2 m from his gunner while both live."
 ],
 "concept":"concept/late/pavisier.svg"
}
]
json.dump(age,open(P,"w"),indent=1,ensure_ascii=False)
