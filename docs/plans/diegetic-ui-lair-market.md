# Proposal: the raid HUD goes into the world, the Lair becomes a place, and the Market haggles

**Status: proposal, 2026-10-06. Nothing here is built. For the owner to review.** Issue #284, part
of #174; relates to #26–#31, #33, #108, #130. Concept art: `docs/art/concept/diegetic/`,
`docs/art/concept/raidview/`, `docs/art/concept/lair/`. Illustrated version of this page:
[`docs/generated/diegetic-proposal/index.html`](../generated/diegetic-proposal/index.html).

## In one paragraph

During a raid, the screen shows only the crosshair and damage feedback. Everything else you need
lives in your hands or in the castle. A grimoire you open holds your spells. A pocket watch you
check shows the portal's time. The castle's own fires show how awake the household is. A light in
your off-hand shows how loudly the microphone hears you. Gold leaves the raid altogether: what you
carry through the portal lands in a physical Lair. You haul it through a door into the Market and
sell it to four vendors who haggle, so the night's takings depend on how well you bargain. Debt is
settled at the Lair, in a ledger with one column per wizard.

## Why

- The owner asked for it (#174, and on 2026-10-06: "the rest of the UI should go away and be
  replaced with diegetic replacements").
- The pitch already argues for it: "It also lets a man read his own greed without a single number
  on the screen" (`docs/plunderspell.md`, §5). The raid HUD today is mostly numbers.
- The Lair is a menu screen (#30, #31) and the Market does not exist (#26, #27). Both are on the
  M4 roadmap. Building them as places gives the gold somewhere physical to land.
- Haggling turns selling into a game, where today the haul is banked automatically. It is also
  the first use of the voice outside combat.

## What stays flat on screen (owner's decision)

- **Crosshair** (`CrosshairView`): unchanged.
- **Damage feedback** (`DamageFeedbackView`): vignette, hit marker, damage numbers, enemy health
  bars, hurt lines. Unchanged.

## Every raid HUD element, and what replaces it

Taken from `RaidHudModel` and `RaidHudView` on `main` (b2195d5f). There are 17 elements; each is
kept, replaced or dropped, and none is left out.

| # | Today (screen) | Proposed | Where you see it |
|---|---|---|---|
| 1 | Phase text ("RAIDING") | **Drop.** Where you are says it. | — |
| 2 | Raid timer | **Pocket watch.** Hold `T` to raise it; the lid opens. The face is a ring of portal-light that empties as the way home narrows. | Off-hand |
| 3 | Timer's last minute (critical) | **Watch ticks aloud,** the second hand stutters and the ring turns madder. It also chimes at 5 min and 1 min, so you can hear it without looking. | Off-hand + sound |
| 4 | Alarm state (Calm / Suspicious / Alerted / Hue and Cry) | **The castle's fires.** Calm: steady amber. Suspicious: the flames lean and gutter. Alerted: madder-red light, louder crackle. Hue and Cry: red flare, embers, and the chapel bell tolls. | Every hearth, brazier and torch |
| 5 | Alarm level (the bar's fill) | **How red the fires are,** blended continuously with the level. | Fires |
| 6 | Interaction prompt ("E — pick up") | **A verdigris rim-light** on what you can use, within reach. The first-time instructions are written in the grimoire's first page, not on screen. | The object |
| 7 | Has an interaction target | Same rim-light (the crosshair's existing "over something" change stays as well). | The object, crosshair |
| 8 | Carried item's name | **Drop.** You can see what you hold. | — |
| 9 | "Needs two" | **Two grip marks glow** on a heavy piece when you look at it. When you try it alone, the existing grab beam goes red and strains. | The object, grab beam |
| 10 | Debt | **Leaves the raid.** The Lair's ledger. | Lair |
| 11 | Gold banked | **Leaves the raid.** Each wizard's strongbox in the Lair. | Lair |
| 12 | Haul in the portal (worth, pieces) | **The pile on the portal pad.** It is physical already; the number goes. Its worth is learned when you sell it. | Portal pad |
| 13 | Ranged weapon status (loaded / reloading) | **The weapon:** a bolt sits on the crossbow when it is loaded, and you see and hear it spanned when reloading. | The weapon |
| 14 | Spell list panel | **Grimoire.** Hold `Tab` to raise it, scroll to turn pages. One spell per page, its word written large. Pages are blank until a spell is learned, then ink in (#108, progressive unlocks). | Both hands |
| 15 | Last cast line (clean / misfire / fizzle) | **Glowing letters** of what you said rise briefly from your off-hand: lapis for clean, madder for a misfire, grey smoke for a fizzle. The grimoire's margin keeps the last few casts. Teammates see the letters as well (#48). | Off-hand, world |
| 16 | Mic loudness meter (whisper ← → shout) | **A lapis light in the off-hand palm.** Its size is the loudness; whisper is an ember, and a shout throws sparks. | Off-hand |
| 17 | Chant progress (keyed cast) | **Runes circle the off-hand** and fill as the word is chanted. | Off-hand |

Also on screen today and outside `RaidHudModel`: the extraction countdown ("stay on the pad").
**Replaced by** the portal visibly closing around you: a ring of light on the floor shrinks, and
the watch chimes on completion (#46, #164).

**Concept:** `docs/art/concept/raidview/raid-view.svg` (the same frame, before and after) and
`docs/art/concept/raidview/alarm-fires.svg` (the four states).

### The two held items

**Grimoire** (`docs/art/concept/diegetic/grimoire.svg`, model #282).
- Quarto, 0.24 × 0.18 × 0.06 m: oak boards in worn calfskin, brass corners and a clasp, vellum
  pages. Lapis ink for the spell words.
- Reading it takes both hands: you walk at 60% speed and cannot carry anything. You can still
  cast, because casting is by voice. *Open decision 4.*
- Pages: a contents page, then one page per spell (its word, what it does, a woodcut of it), then
  the margin notes (last casts, first-time hints).
- Modelled as two meshes so Unity can swing the cover without a rig. A page-turn is a third,
  single-page mesh.

**Pocket watch** (`docs/art/concept/diegetic/pocket-watch.svg`, model #283).
- 52 mm brass hunter case with a hinged lid, crown and bow, on a short chain.
- The face does not show clock time. It shows a ring of lapis portal-light, divided into the
  raid's minutes, that empties anticlockwise. One hand points at what is left. *Open decision 5.*
- Raising it takes the off-hand, so you can check it while carrying something one-handed.

## The Lair, as a place

**Concept:** `docs/art/concept/lair/lair.svg`.

A vaulted cellar outside time, about 14 × 10 m, lit by one fire and one candle and falling away
into black (the pitch's first reference frame). What is in it:

- **The portal arch.** Loot carried through at extraction lands here, on the flagstones, as
  real objects (not a number). Dead teammates carried out get up here (pitch §5).
- **The hearth.** The one fire.
- **The ledger table.** A great ledger, one column per wizard (#130: the debt is per player). It
  shows what each owes, what is due tonight, and what each has paid. The entries are written by
  hand as payments land.
- **Four strongboxes**, one per wizard, at the foot of the table. Coins you put in are banked.
- **The century dial.** An orrery of four rings, one per Age. Turn it to choose where the portal
  opens. This replaces the Age picker on the Lair screen.
- **The weapon rack.** Whatever you brought home from one Age, to take into another (pitch §6).
- **The Market door.** A low door in the back wall onto the Market.
- **Ready up**, as a place: everyone puts a hand on the portal stones, and when all four are
  touching it, a three-count starts (the countdown asked for in #232).

The main menu, settings and lobby stay flat screens; only the Lair screen becomes a room.

## The Market, and haggling

**Concept:** `docs/art/concept/lair/market.svg` (the street) and `docs/art/concept/lair/haggle.svg`
(one haggle, step by step).

A lantern-lit yard between the Ages, around a well, with four stalls. You carry your loot to them
by hand; it weighs what it weighed in the castle. Each stall has a slate board chalked with what
it wants tonight.

| Vendor | Buys | Temperament | Pays more for |
|---|---|---|---|
| **The Fence** | anything | Impatient, fair-ish. Barely haggles, pays at once. | Nothing: the baseline price |
| **The Goldsmith** | metal | Slow and exact. Weighs everything on brass scales and pays by weight; dents matter little, because he melts it down. | Gold and silver by the stone |
| **The Pardoner** | relics, church plate, psalters | Pious until money is mentioned. | Holy pieces, intact |
| **The Antiquarian** | curios, arms from other Ages | Patient, fickle, a collector. | Anything out of its own century, in good condition |

### One haggle

1. **Put the piece on the counter.** The vendor looks it over and names an opening offer, below
   what it is worth to them. The coins are pushed across the counter as he says it.
2. **Answer with a word.** This is a voice game, so the words are part of the lexicon, like
   spells, and each can also be bound to a key (#57):
   - ***Plus*** — "more". Ask for a higher price.
   - ***Satis*** — "enough". Take the coins on the counter.
   - ***Vale*** — "farewell". Pick your piece up and leave.
3. **The vendor answers.** Below his limit, he meets you or splits the difference. Above it, he
   refuses and his patience drops. When his patience runs out, he will not buy that piece tonight.
4. **Patience is shown by the vendor, not a meter.** Fingers tapping, a pipe puffed harder, a
   look, a line of speech, with subtitles for every line.

### The numbers behind it

All values are per piece; the coefficients are starting points to tune.

- **Worth** `W` = the piece's `LootValue` × its condition (the damage it took on the way out).
- **Limit** `L` = `W` × the vendor's interest in it (0.6–1.5, higher for its own category and for
  its slate board) × tonight's mood (0.85–1.15). Hidden.
- **Opening offer** = `L` × 0.55–0.7.
- **Each *Plus*** asks 10% above the coins on the counter. At or below `L`, he accepts: at once
  for a small ask, otherwise by meeting you halfway. Above `L`, patience drops by 1, or 2 if you
  asked for more than 1.2 × `L`.
- **Patience**: the Fence 1, the Goldsmith 3, the Pardoner 3, the Antiquarian 4.
- **Walking away** (*Vale*) and coming back the same night: the opening offer is 10% lower.
  He remembers.

A good haggler gets most of `L`. A greedy one leaves with the piece still in their arms.

### Coins and the debt

- Coins are physical: a pouch per wizard that grows heavier. You bank coins by putting them in your
  strongbox. You can hand coins to a friend (#130: pay their debt, or don't).
- **When the debt is taken:** the Collector calls at the Lair when you are back from the Market,
  takes what is due from each strongbox and writes it in the ledger. What is left is yours to spend.
  *Open decision 1: #29 asks for the debt to be paid first, automatically, before anything is
  spendable.*
- **Buying** happens at the same stalls: weapons, tools and scrolls, always priced above the
  same thing's raid worth (#29's mark-up rule).

## What has to be modelled (the Blender 5 session)

| Model | For | Pieces | Issue |
|---|---|---|---|
| Grimoire | Raid | body, cover, page | #282 |
| Pocket watch | Raid | case, lid, hour hand, minute hand | #283 |
| Off-hand glow, rune ring, glowing letters | Raid | effects, not meshes | new |
| Fire-state lighting | Raid | lighting/particles on existing fires (overlaps #278) | new |
| Lair cellar | Lair | room shell, vault, hearth | new (#31) |
| Ledger table, ledger, strongbox ×4 | Lair | props | new (#33) |
| Century dial (orrery) | Lair | four rings that turn | new |
| Weapon rack, Market door | Lair | props | new |
| Market yard, well, four stalls, slate boards | Market | room shell + props | new (#27) |
| Goldsmith's scales | Market | beam and two pans | new |
| Coin, coin stack, pouch | Lair/Market | props | new |
| Four vendors | Market | characters, rigged, idle and talking animations | new; the biggest single cost |

## In what order

Each phase can ship alone and be played.

1. **Raid diegetics.** Grimoire, pocket watch, fire-state lighting, off-hand glow, rim-light, then
   delete the HUD elements they replace. Until phase 2, the debt and haul are shown on the
   existing Lair screen, not in the raid.
2. **The Lair as a room.** Cellar, portal landing pile, ledger and strongboxes, century dial,
   ready-up at the portal. Appraisal is automatic for now (the Fence's price), so the loop keeps
   working.
3. **The Market.** Yard, stalls, haggling with placeholder vendors (a figure behind the counter
   plus subtitles), coins, the Collector, buying.
4. **The vendors as characters.** Models, animation and their tells; voiced lines.

## Risks

- **New players will not know the keys.** The grimoire's first page teaches them, and the book
  opens by itself the first time a raid starts. This needs a playtest.
- **Fires as the alarm only work where there are fires,** so a guard room with no hearth gives no
  reading. Every zone needs at least one light source that follows the alarm, which the castle
  generator would have to guarantee.
- **Without numbers, balance is harder to feel.** Debug overlays stay available in development
  builds.
- **Recognising haggling words** depends on the voice recogniser handling three new words. They
  are added to the lexicon and tested exactly like spell words.
- **Scope.** Phase 4 alone is four characters. Phases 1 to 3 are worth building without it.

## Open decisions for the owner

1. **Debt order.** Is the debt taken before you can spend (#29 as written), or after the Market,
   by the Collector (this proposal)?
2. **Haggling input.** Voice words (*Plus / Satis / Vale*, also bindable to keys), or mouse
   buttons only?
3. **Coins.** Physical pouches you carry and hand over, or a simple per-wizard purse total?
4. **Reading the grimoire.** Does it slow you and stop you carrying, as proposed?
5. **The watch face.** A portal-light ring (proposed), or ordinary clock hands counting down?
6. **Keys.** `Tab` for the grimoire and `T` for the watch, or something else?
