"""
Files the moodboard-gap-closure backlog (docs/plans/moodboard-gap-closure.md) to GitHub issues.

Same mechanism as Tools/mkissues.py (which filed #5-#25, the 2026-09-16 playtesting backlog):
`gh issue create` via subprocess, run locally where `gh` is already authenticated. This script
writes its own manifest (docs/generated/github-issues-moodboard-gap.json) rather than
docs/generated/github-issues.json, so it never overwrites the existing one.

Unlike mkissues.py, this script also ensures every label it uses exists first (`gh label create
--force`, which is idempotent), because several of these are new labels the repo hasn't used
before ("market", "lair", "content", "design", "juice", "accessibility", "testing", "combat",
"lighting", "networking", "settings", "performance", "build", "decision-needed").
"""
import subprocess, json, os

LABEL_COLORS = {
    "gameplay": "0e8a16", "ui": "1d76db",
    "enhancement": "a2eeef", "animation": "fbca04", "vfx": "d876e3", "art": "e99695",
    "epic": "3e4b9e", "audio": "f9d0c4", "content": "bfd4f2",
    "design": "c5def5", "market": "fef2c0", "lair": "fad8c7", "juice": "ff9f1c",
    "accessibility": "0052cc", "testing": "b60205", "combat": "e11d21",
    "lighting": "fbca04", "networking": "5319e7", "settings": "cccccc",
    "performance": "b60205", "build": "000000", "decision-needed": "e11d21",
}

ISSUES = []
def add(title, labels, body):
    ISSUES.append((title, labels, body))

# ── EPIC: The Mystical Market ────────────────────────────────────────────────

add("EPIC: The Mystical Market pillar does not exist",
    "epic,market",
"""## Problem
`docs/plunderspell.md`'s mood board devotes a full pillar to the Mystical Market (the
Spellmonger's Stall, the Smith's Stall, the Alchemist's Stall, the Curiosity Dealer) with specific
rules: it always costs more than finding the same thing in a dungeon, it remembers what it's sold
a player, and whatever's left after the debt is settled is spent here. None of it exists in the
codebase - no scene, no shop UI, no stall data, nothing in `docs/4-systems/`.

## Context
This is separate from the 2026-09-16 playtesting backlog (#5-#25), which was filed against the
raid loop; the market sits at the lair, which that backlog never touched. See
`docs/plans/moodboard-gap-closure.md` §2.4.

## Done when
The three issues under this epic (market hub + UI, stall wares/pricing, mark-up + spend-leftover
rule) are closed, and a player can walk from the lair into the market, buy something with debt
already owed, and see it remembered on a later visit.""")

add("Build the market hub - a place, a shop UI, and persistent \"stays bought\" state",
    "market,ui,enhancement",
"""## Problem
There is nowhere in the game to spend gold on anything but the debt. No market scene, no shop
screen, no purchase flow.

## Context
The pitch (`docs/plunderspell.md`, "The Mystical Market" section) describes the market as bound to
the lair the same way the lair itself persists between sessions: "buy a thing once and it stays
bought, even for a fellow who wanders in a month from now, poorer than you and grateful for it."
`LairHubManager` already tracks debt and banked gold (`docs/4-systems/raid-scene-assembly.md`); a
market needs to read and write against the same persistent state.

## Acceptance criteria
- A market location or menu is reachable from the lair.
- Four stalls are represented (see the wares-content issue for what each sells).
- A purchase is deducted from banked gold and persists across sessions, per player.
- Re-entering the market shows what was already bought as already owned, not re-purchasable.""")

add("Author wares and pricing for all four market stalls",
    "market,content,design",
"""## Problem
Even once a market hub exists (see the epic), there is no data for what it actually sells. The
pitch names categories, not a manifest:
- Spellmonger's Stall - unlearned incantations, syllable coaching
- Smith's Stall - blade/reach/heft upgrades
- Alchemist's Stall - one-use consumable potions and tonics
- Curiosity Dealer - gadgets, curios, upgrades of no fixed category

## Context
The eight spells are already fully defined (`Assets/_Project/Data/Spells/`), so "unlearned
incantations" likely means gating spells the player hasn't cast successfully yet behind a
purchase - that gating rule doesn't exist yet either and needs deciding as part of this item, not
assumed.

## Acceptance criteria
- Each stall has at least a handful of concrete, priced items as data assets.
- Smith's-stall upgrades have somewhere to apply to (see the melee-weapon-system issue - this may
  be blocked on that existing first).
- Pricing reflects the pitch's stated rule: always more than the dungeon-found equivalent.""")

add("Market mark-up and \"spend the leftover after the debt\" are unimplemented rules",
    "market,gameplay,enhancement",
"""## Problem
Two specific economic rules from the pitch have no code behind them yet: buying at the market
always costs more than finding the same item in a raid, and whatever gold is left over once the
debt payment is taken is the player's to spend here freely.

## Context
Debt payment already exists (`RaidDirector`/`ExtractionResolved` banks worth against the debt per
`docs/4-systems/raid.md`). This item is about the market-specific slice: computing "leftover after
debt," and a price-comparison rule between market goods and their dungeon-found equivalents (which
requires dungeon-found equivalents to exist - see the era-specific-loot-catalogue issue for why
most of them currently don't).

## Acceptance criteria
- Debt is paid first, automatically, before anything is spendable at the market.
- Market prices for an item that also exists as raid loot are provably higher than that loot's
  raid worth.""")

# ── EPIC: The Lair is a menu ─────────────────────────────────────────────────

add("EPIC: The Lair is a menu screen, not the physical place the pitch describes",
    "epic,lair,art",
"""## Problem
The pitch's Lair is physical and lived-in: "damp, yours, and permanent," lit by "one candle, one
fire, falling away into black - the only safe frame in the whole game" (`docs/plunderspell.md`
§9). What's built (`LairScreen`, per `docs/4-systems/raid-scene-assembly.md`) is a UI screen showing
debt, banked gold and the four eras - there is no 3D lair space anywhere in `Assets/Models` or
`Assets/_Project/Art` (which holds only `Models/Castle`, `Models/Loot`, `Models/Weapons` and
`Models/Enemies` - no lair models at all).

## Context
See `docs/plans/moodboard-gap-closure.md` §2.5. The lair is also where the (currently nonexistent)
market lives - see the market epic - so building the lair as a place and building the market are
likely one body of work, not two.

## Done when
The three issues under this epic (3D space, lighting/mood pass, in-fiction debt/hoard
representation) are closed, and stepping "into" the lair is a first-person space, not a menu
transition.""")

add("Build a 3D Lair space to replace the menu screen",
    "lair,art,enhancement",
"""## Problem
There is no lair scene, room, or model set. `LairScreen` (see `docs/4-systems/raid-scene-assembly.md`
"Getting into a raid") is a 2D UI screen, not a place the player's character stands in.

## Context
Every other location in the game (castle rooms, market once built) is a first-person physical
space per the pitch's whole design language ("nothing in this world is a menu" - §5). The lair
should follow the same rule rather than being the one exception.

## Acceptance criteria
- A modelled lair space exists (at minimum: one room, damp/lived-in per the pitch's description).
- The player's first-person character can walk into it rather than opening a menu.
- Existing lair functionality (debt, banked gold, era selection, Set Out) is reachable from inside
  it, not lost in the conversion.""")

add("Lair lighting and mood pass - \"one candle, one fire, falling into black\"",
    "lair,art,lighting",
"""## Problem
The pitch names the lair as one of six specific lighting reference frames and calls it "the only
safe frame in the whole game" - a deliberate contrast with every other lit space. No lighting
rig, concept reference or mood target exists for it.

## Context
Blocked on the 3D-lair-space issue existing first. See `docs/plunderspell.md` §9 ("Six frames the
game should be able to produce") and `docs/plans/moodboard-gap-closure.md` §2.7 for the other five
frames, which are tracked separately (see the lighting-reference issue).

## Acceptance criteria
- The lair reads visually distinct from every raid interior - warmer, dimmer, one clear light
  source.
- A single candle and a single fire are the stated light sources per the pitch; no other lighting
  in the space.""")

add("Debt and hoard have no in-fiction representation - only HUD numbers",
    "lair,ui,enhancement",
"""## Problem
The debt and the hoard are the emotional core of the game's premise (the pitch spends its whole
third truth on the debt), but currently exist only as numbers on the lair UI screen.

## Context
Once a 3D lair exists (see that issue), the hoard in particular has an obvious physical
representation - what you've actually carried home, visible in the space, not just a number.

## Acceptance criteria
- The debt is represented as something in the lair's fiction, not only a HUD figure (a ledger, a
  visible marker of how deep in debt the player is, etc. - exact form is a design call).
- Carried-home loot is visible/present in the lair space in some form, not only tallied.""")

# ── EPIC: Icons ───────────────────────────────────────────────────────────────

add("EPIC: No icon set exists anywhere - spells, weapons, loot, eras, status effects",
    "epic,art,ui",
"""## Problem
Every piece of UI that would need an icon currently has none. Confirmed by folder listing:
`Assets/_Project/Art` contains only `Models/` and one `Textures/PlunderspellPalette.png` (a
palette swatch, not icon art) - no `Sprites/`, `Icons/` or `UI/` art folder exists anywhere in the
project.

## Context
This blocks UI work broadly, not just one screen: a spellbook/loadout screen needs spell icons, an
inventory needs weapon and loot icons, the lair's era selection needs four era icons, and any
status-effect display (burn/stun/sleep from `StatusEffectReceiver`, see `docs/4-systems/spells.md`)
needs status icons.

## Acceptance criteria
- At minimum: 8 spell icons, one per weapon in the final roster, one per loot category, 4 era
  icons, and icons for the status effects `StatusEffectReceiver` implements (burn, stun, sleep,
  and whatever else is added).
- Icons follow the palette discipline from `docs/plunderspell.md` §9 (verdigris for
  glowing/arcane/touchable things, orpiment reserved for gold/value only, nothing else permitted
  to read as gold).""")

# ── Ages / eras content ──────────────────────────────────────────────────────

add("Era-specific hazards from the pitch are entirely unimplemented",
    "gameplay,content,design",
"""## Problem
The pitch names one signature hazard per era - fire runs faster in the Bronze Age; the High
Medieval stairs "turn clockwise, and not in your favour"; Late Medieval guards "hunt in pairs";
one stray IGNIS near a Powder-age magazine "ends the evening." None of this appears in
`docs/4-systems/castle.md`, `alarm.md` or `raid.md` - the only hazard-adjacent system documented is
the generic noise/alarm escalation, which applies identically regardless of era.

## Context
Distinct from issue #17 ("Only one era has art and models"), which is about rooms/loot/enemies.
This is about era-specific *rules*, which need a hook into the castle/alarm/guard systems rather
than just more art. See `docs/plans/moodboard-gap-closure.md` §2.3.

## Acceptance criteria
- At least one hazard per era is implemented and distinguishable in play.
- Each hazard is documented in the relevant `docs/4-systems/*.md` file per the existing convention
  (owns / how it works / invariants / traps).""")

add("Era-specific loot catalogue is unauthored - five generic pieces stand in for the pitch's four eras",
    "content,art,design",
"""## Problem
`Assets/_Project/Data/Loot/` and `Assets/_Project/Prefabs/Loot/` hold five generic pieces
(CopperPot, SilverPlate, GoldenGoblet, HeavyChest, AncientRelic) used for every era and every raid.
The pitch names an entirely different, era-specific catalogue: ingots, faience, ceremonial bronze,
sealed amphorae (Bronze); reliquaries, altar plate, illuminated psalters, coin (High Medieval);
Burgundian plate, tapestry, banking ledgers, jewels (Late Medieval); cabinets of curiosity,
mirrors, astrolabes, silver services (Powder). The mood board's own signature prop, the two-person
**Altarpiece**, has no model at all.

## Context
Overlaps issue #17's scope but is specifically about loot content rather than rooms/enemies; filed
separately because it's blocked on different work (art authoring vs. wiring an era dimension into
`CastleRoomRegistry`/`EnemyRoster`). See `docs/plans/moodboard-gap-closure.md` §2.2 and §2.3.

## Acceptance criteria
- Each era has at least a handful of era-appropriate loot prefabs distinct from the current five.
- The Altarpiece (or an equivalent two-person, unusually large prize) exists as a modelled,
  two-person-carry loot item.
- Worth still climbs toward the crypt/chapel per the existing zone-worth rule in
  `docs/4-systems/raid-scene-assembly.md`.""")

# ── Weapons & melee combat ────────────────────────────────────────────────────

add("No melee combat system exists - reach/heft/noise stats aren't a swing mechanic yet",
    "gameplay,combat,enhancement",
"""## Problem
The pitch's whole framing of weapons is melee-first: "a sword is merely a heavy object with a
sharp end that you swung hard" (§5), and every weapon in the arsenal table (§8) carries Reach,
Heft and Noise stats. What's built is physics-based throwing with velocity-scaled impact damage
(`docs/plunderspell.md` §10) - there is no documented swing/attack input, no reach-based hit
detection, and no system in `docs/4-systems/` that reads a weapon's Reach or Noise stat during a
melee swing.

## Context
This blocks the entire weapon roster from mattering as melee weapons rather than just throwables.
See `docs/plans/moodboard-gap-closure.md` §2.1 and §4.

## Acceptance criteria
- A melee swing input exists, distinct from throwing.
- Reach determines hit range; Heft affects swing speed/damage; Noise feeds the existing acoustics
  system (`RogueAi.Acoustics`) the same way other noise events do.
- At least one weapon (the Arming Sword, per the pitch's own description as "the honest one") is
  fully wired end to end as a reference implementation.""")

add("Weapon roster doesn't match the pitch's twelve named weapons",
    "art,content,design",
"""## Problem
The pitch names twelve specific weapons with individual flavour text and stats (§8): Khopesh,
Sling, Oxhide Shield (Bronze); Arming Sword, Flanged Mace, Crossbow (High Medieval); Poleaxe, Hand
Cannon, Caltrops (Late Medieval); Wheellock Pistol, Rapier, Grenado (Powder). What's built
(`Assets/_Project/Prefabs/Weapons/`) is eight differently-named models: BronzeSword, Longsword,
Matchlock, FlintlockPistol, PaviseShield, PlateHelm, PowderGrenade, RoundShield. Missing entirely:
Khopesh, Sling, Oxhide Shield, Arming Sword, Flanged Mace, Crossbow, Poleaxe, Caltrops, Rapier.
One is a mechanism mismatch, not just a naming difference: the pitch specifies a **Wheellock**
pistol (period-correct for c. 1620); the built model is a **Flintlock**, a different and later
firing mechanism.

## Context
Also blocked-on by the melee-combat-system issue for anything beyond throwing. See
`docs/plans/moodboard-gap-closure.md` §2.1.

## Acceptance criteria
- The shipped roster is reconciled with the pitch's twelve names, either by renaming/re-authoring
  existing models or explicitly deciding to diverge (record the decision in `docs/6-decisions/Decisions.md` if
  so, per house convention).
- The wheellock/flintlock mismatch is resolved one way or the other, not left silently wrong.
- Each weapon carries Reach/Heft/Noise data matching its §8 table entry.""")

add("Ranged weapons have no aim or fire implementation",
    "gameplay,enhancement",
"""## Problem
The arsenal includes ranged weapons (Sling, Crossbow, Hand Cannon, Wheellock Pistol) with their own
stats and one-shot-then-vulnerable pacing described in the pitch's flavour text ("one bolt, then a
long and terrible pause... you are simply a man holding a plank"). No aiming, firing, ammunition or
reload system is documented anywhere in `docs/4-systems/`.

## Context
Blocked on the weapon-roster-content issue for the actual models, and likely shares an input/aim
layer with the melee-combat-system issue.

## Acceptance criteria
- At least one ranged weapon (Crossbow, per its central role in the High Medieval kit) is fully
  playable: aim, fire, the "long and terrible pause" reload window, noise emitted appropriately.
- Ammunition or reload state is visible to the player.""")

add("Weapon and loot icons for UI do not exist",
    "art,ui",
"""## Problem
No item icon of any kind exists for a weapon or loot piece - see the icon-set epic. Filed as its
own item because it's specifically blocking any inventory/loadout screen for weapons, independent
of the icon epic's other categories (spells, eras, status).

## Acceptance criteria
- One icon per weapon in the final (reconciled) roster.
- One icon per loot piece/category.
- Consistent with the icon-set epic's palette discipline.""")

# ── Bestiary ──────────────────────────────────────────────────────────────────

add("Bestiary thematic divergence: half the enemy roster reads as fantasy monsters, not the pitch's household",
    "design,content,decision-needed",
"""## Problem
`Assets/Models/Enemies/enemy_manifest.json` names ten enemies. Five match the pitch's household
exactly: Watchman ("walks the patrol routes, carries the lantern"), ManAtArms ("the one guard who
comes to check"), Sergeant ("leads the sweep, carries the horn"), WarHound ("the chase"). The other
five do not: SigilWisp ("scout flier, swarms"), VaultWarden ("melee grunt, armoured"), HexTurret
("static sentry - yaws to track, pitches to fire"), ArcRevenant ("elite caster - hovers, shields
allies, casts at range"), GildedColossus ("vault boss - asymmetric siege construct"). Nothing in
the pitch or mood board describes magical constructs, hovering casters or a swarming flier as
antagonists - the pitch's threat is explicitly and repeatedly human.

## Context
This is a creative-direction question, not a bug - flagged per the project's own instruction to
check in whenever work diverges from the mood-board vision. See
`docs/plans/moodboard-gap-closure.md` §2.6 for the full reasoning. Left unresolved, art/animation/
audio work on the five divergent enemies keeps compounding a fork that gets more expensive to
reconcile the longer it's deferred.

## Needs a decision on
- Rework the five divergent enemies into the household-guard register the pitch promises (e.g. a
  retainer instead of ArcRevenant, a war-machine instead of HexTurret), or
- Update the pitch/mood board to admit a deliberate supernatural register the original text didn't
  have, or
- Some explicit split (e.g. the crypt zone is allowed monsters, every zone above it is not).

Record whichever is chosen in `docs/6-decisions/Decisions.md` once made.""")

add("Guards are silent - no patrol murmur, alert bark, or chase shout",
    "audio,enhancement",
"""## Problem
`RogueAi.Acoustics` computes attenuated noise events that guards and the alarm react to
(`docs/4-systems/alarm.md`), but produces no sound a player actually hears, and guards themselves
have no vocalisation at any alert state.

## Context
Narrower than issue #18 (general "no VFX or SFX anywhere"), which is mostly about the player's own
casting/UI feedback. This item is specifically the enemies' own voice: a patrol murmur at Calm, a
challenge/alert bark at Stirred/Roused, a chase shout at HueAndCry - all states `CastleGuard`
already tracks (`GuardAlertState`, per issue #6's context notes).

## Acceptance criteria
- Each `GuardAlertState` has a distinct vocalisation.
- Vocalisations emit through the existing acoustic path (`AcousticEmitter`/`NoiseBroadcaster`) so
  they're consistent with how every other noise in the game propagates.""")

add("GildedColossus (vault boss) has no unique behaviour, telegraph, or arena design",
    "gameplay,design,enhancement",
"""## Problem
The Crypt-zone boss exists only as stats and a model (`enemy_manifest.json`: "Vault boss -
asymmetric siege construct", weight 2, height 3.4 m) - `EnemyPrefabForge` applies the same generic
`CastleGuard` tuning as every other enemy. There is no unique attack, telegraph, or arena
consideration for the one enemy the pitch's zone-worth structure (`docs/4-systems/raid.md`: "the
crypt final chamber always holds the richest entry in its zone") implies should be a set-piece.

## Context
`GildedColossus` is rare by design (weight 2 in the Crypt only, ~2 appearances per 523 spawns per
`docs/4-systems/raid-scene-assembly.md`), which makes it a poor candidate for generic guard AI - a
rare encounter is exactly the one worth spending unique design on.

## Acceptance criteria
- At least one attack or behaviour distinct from the standard `GuardBrain` pattern.
- A clear telegraph before its dangerous action(s).
- Crypt room geometry is checked against its 3.4 m height and 2.2 m width (per issue #6's own
  finding that the Colossus "does not fit in a crypt room at all" at current room scale).""")

# ── Structures & hazards ──────────────────────────────────────────────────────

add("Door/socket types (murder-hole, arrow-loop) are layout tags only, not functional hazards",
    "gameplay,design,enhancement",
"""## Problem
`docs/4-systems/castle.md` describes rooms as carrying tagged sockets - door, window, arrow-loop,
stair-up, stair-down, murder-hole - used purely for placement/connectivity matching. None of them
appear to do anything functionally distinct once placed: a murder-hole is not documented anywhere
as an actual hazard (something can drop or fire through it), and an arrow-loop is not documented
as a guard firing position.

## Context
The pitch's castle-architecture framing treats these as real defensive features, not decoration.
Turning even one (murder-hole) into an actual gameplay hazard would make the "going home is always
a fighting retreat" promise (`docs/plunderspell.md` §7) concrete rather than purely a layout shape.

## Acceptance criteria
- At least one socket type (murder-hole is the suggested first) has a real functional effect in
  play, not just a placement tag.
- Documented in `docs/4-systems/castle.md` alongside the existing socket-matching description.""")

add("Drawbridge has no operable mechanism despite existing as a modelled room",
    "gameplay,animation,enhancement",
"""## Problem
`Assets/_Project/Art/Models/Castle/Drawbridge.fbx` and its prefab exist and are used by the castle
generator, but nothing in `docs/4-systems/castle.md` or `raid.md` describes a drawbridge as anything
other than a static room module - no raise/lower state, no gate mechanic.

## Context
A functioning drawbridge/gatehouse is a natural home for some of the "the household wakes"
escalation the alarm system already models (`docs/4-systems/alarm.md`) - e.g. barring it at Roused,
matching `CastleLockdown`'s existing one-way latch on doors.

## Acceptance criteria
- The Drawbridge module has a raise/lower state, driven by the alarm state the same way
  `CastleLockdown` already drives door locking.
- The state change is visible (animated) and audible.""")

# ── Portal & extraction feel ──────────────────────────────────────────────────

add("Portal has no closing-countdown feedback - \"the way home begins to narrow\" is invisible",
    "vfx,audio,gameplay,enhancement",
"""## Problem
The pitch's central extraction-tension beat is explicit: once the last player is in, "the way home
begins to narrow... it will not wait up for you" (§3). Extraction is currently an invisible trigger
volume with a timer value that exists in code but nothing shows a player it's counting down, let
alone narrowing.

## Context
Builds on issue #21 (no portal asset at all) rather than duplicating it - this item is specifically
about the closing/urgency feedback once a portal asset exists, and can be scoped to land right
after #21.

## Acceptance criteria
- The portal visibly and audibly communicates time remaining before it closes.
- The countdown is legible from a reasonable distance (a raid can end with players still fighting
  their way back to it).""")

# ── Voice feedback ────────────────────────────────────────────────────────────

add("No loudness meter - the whisper/normal/shout dial the pitch calls \"the whole trick\" is invisible",
    "ui,audio,enhancement",
"""## Problem
`SpellTuning.CastVolume` scales power and noise together (`docs/4-systems/spells.md`), and the pitch
calls this dial the entire trick of the voice pillar (§4: "it is asking you to be brave in your own
sitting room, and that is the whole trick"). Nothing on screen shows a player where they currently
sit between whisper and shout while holding the cast key.

## Acceptance criteria
- A visible meter or indicator tracks cast volume in real time while casting.
- It's legible enough to tell a player *why* SOMNUS just failed because they were too loud, or why
  TONITRUS landed weak because they were too quiet.""")

add("Casting is invisible to teammates - \"friends hear the word half a heartbeat before it resolves\" isn't represented",
    "networking,vfx,audio,design",
"""## Problem
The pitch's most-quoted line describes a specific co-op tension: "your fellows hear the word half
a heartbeat before the world obeys it." Per `docs/6-decisions/Decisions.md` (2026-09-11 entry), only the
resolved `SpellId` crosses the network - never audio, and nothing else. There is no networked
"a nearby player is mid-cast" tell of any kind, visual or audio, before a spell's effect appears.

## Context
This doesn't require sending raw audio (that decision, on-device recognition with no voice leaving
the machine, is a deliberate pillar per the same Decisions.md entry and shouldn't be reopened). It
needs a lightweight networked state - e.g. "Player X is casting" plus which word, replicated the
moment `PushToCastController` starts listening rather than only once `SpellCastingSystem` resolves.

## Acceptance criteria
- Nearby teammates get a visible and/or audible cue that a player is mid-cast, before the spell
  resolves.
- No raw audio crosses the network - the existing on-device-only voice decision stays intact.""")

add("Recognised phrase has no on-screen caption",
    "ui,accessibility,enhancement",
"""## Problem
Nothing echoes what `VoiceRecognitionResult` actually heard. This is both an accessibility gap and
the only practical way to tell a genuine misfire apart from a simple mis-recognition during
playtesting.

## Context
`VoiceUtility.Normalize` already produces the normalised text form used for lexicon matching
(`docs/4-systems/voice.md`) - the value exists, it's just never surfaced.

## Acceptance criteria
- The recognised phrase (post-normalisation) is captioned on screen briefly after each cast
  attempt.
- Caption is distinguishable for a clean cast, a misfire, and a silent fizzle.""")

add("M1's acceptance criterion has never been measured - real microphone, multi-accent recognition",
    "testing,audio",
"""## Problem
`docs/2-roadmap/Roadmap.md` defines M1 done as ">90% top-1 recognition across four accents on the 40-word
lexicon, under 150 ms from word-end to effect." `docs/3-state/ProjectState.md` already states plainly that
"no real-microphone, multi-accent, latency measurement exists anywhere in the repo" - this item
exists to track that gap as a piece of actual work rather than leave it only described in prose.

## Acceptance criteria
- A real-microphone test pass is run across at least four distinct accents against the 40-word
  lexicon.
- Top-1 recognition rate and word-end-to-effect latency are both measured and recorded.
- Results are written up against the M1 acceptance criterion in `docs/3-state/ProjectState.md`.""")

# ── Juice ─────────────────────────────────────────────────────────────────────

add("No camera shake, hit-stop, or impact juice anywhere",
    "juice,vfx,enhancement",
"""## Problem
Casting, throwing, being hit, and impacts of any kind currently resolve with zero camera or
timing feedback - no shake, no hit-stop, no impact-frame weight.

## Context
Distinct from issue #8/#9 (visual feedback for damage and spells specifically) - this item is the
game-feel layer that would sit on top of those once they exist: the difference between "a hit
effect plays" and "a hit *feels* like it landed."

## Acceptance criteria
- At least casting a spell, a melee hit (once the melee system exists), and taking damage each
  have a camera-shake or hit-stop response.
- Intensity scales with the underlying event where it makes sense (e.g. cast volume, as with the
  loudness-meter item).""")

add("Throwing and swinging have no wind-up or follow-through animation weight",
    "animation,juice,enhancement",
"""## Problem
Picking up, aiming and throwing an object (`docs/plunderspell.md` §5: "seize anything, turn it
about in front of you, push it out or draw it close, then let fly") has no documented wind-up or
follow-through animation - the same gap applies to the not-yet-built melee swing.

## Context
Related to issue #11 (no player/door animations generally) but specifically about the weight and
timing of the pitch's signature "everything is a weapon" interaction, not just the existence of
any animation at all.

## Acceptance criteria
- A throw has a distinct wind-up and follow-through, not just an instant velocity application.
- The same applies to the melee swing once that system exists (see the melee-combat-system issue).""")

# ── Testing, build, performance ───────────────────────────────────────────────

add("No standalone Unity player build has ever been produced",
    "testing,build",
"""## Problem
`docs/3-state/ProjectState.md` and `docs/5-today/Today.md` both already state this plainly: there is no
`BuildPipeline.BuildPlayer()` entry point anywhere in `Assets/` (reconfirmed by folder listing this
pass), so whether the project actually runs as a standalone build - outside the Unity Editor - is
unknown.

## Context
Filed because this fact was documented but had no GitHub issue tracking it as work to be done.

## Acceptance criteria
- A build pipeline entry point exists (menu item or CI script) that produces a standalone player.
- At least one successful standalone build is produced and smoke-tested (reaches the main menu,
  can start and complete a raid).""")

add("No performance budget or profiling pass exists for authored-art raids",
    "testing,performance",
"""## Problem
A representative raid now instantiates 40-48 castle rooms (~12-15k triangles of modelled geometry,
per `docs/4-systems/raid-scene-assembly.md`'s verification numbers) plus up to ~19 loot pieces and a
full guard roster - but no frame-time, draw-call, or memory budget has ever been set or measured
against real hardware.

## Context
This becomes more urgent once the art-authoring items in this backlog land (three more eras' worth
of rooms/loot/enemies, VFX, particles) - better to establish a budget before tripling the asset
count than after.

## Acceptance criteria
- A frame-time/draw-call budget is defined for a representative raid.
- At least one profiling pass is run and recorded against it, with a clear pass/fail against the
  budget.""")

add("M2's acceptance criterion has never been run - a real four-player raid start to finish",
    "testing,gameplay",
"""## Problem
`docs/2-roadmap/Roadmap.md` defines M2 done as "four real players complete a raid together, start to finish,
through the actual built game." `docs/3-state/ProjectState.md` already states this has never happened - 116
automated tests passing is explicitly called out as not the same thing. This item tracks the
actual playtest as work, separate from the 21-item punch list (#5-#25) that a first real session
would very likely surface more of.

## Acceptance criteria
- Four real people play a raid together, start to finish, on the actual built game.
- The outcome (completed / failed, and why) is recorded against the M2 acceptance criterion in
  `docs/3-state/ProjectState.md`.""")

# ── Accessibility & settings ──────────────────────────────────────────────────

add("Settings menu completeness (audio mix, keybinds, mic device) is unverified",
    "ui,settings,enhancement",
"""## Problem
An `Assets/_Project/Scripts/.../Settings` area is referenced in the project structure, but whether
it exposes audio mixer levels, key rebinding, or microphone device selection has not been checked
against what a shipped voice-driven co-op game needs.

## Acceptance criteria
- Master/SFX/music mix levels are exposed and functional (this also depends on audio actually
  existing - see issue #18).
- Key bindings are rebindable, including every spell word's keyboard-mock binding.
- Microphone device is selectable, with a visible indicator of which device is active.""")

add("\"Every word can also be bound to a key\" has no real settings-exposed keyboard-casting mode",
    "accessibility,enhancement",
"""## Problem
The pitch states plainly that voice is never required: "every word can also be bound to a key...
nobody is locked out for being mute, or shy, or awake at three in the morning in a thin-walled
house" (§4). What exists today (`MockVoiceInputService`, per `docs/4-systems/voice.md`) is a
dev/editor/headless fallback, auto-selected when no real recognizer is available - not a real,
player-facing accessibility *option* a person can deliberately choose in a shipped build with a
working microphone.

## Context
`VoiceServiceLocator`'s selection order (editor/headless/no-mic -> mock, else Vosk) has no path for
"a player with a working mic chooses keyboard casting anyway." That's a settings/accessibility
feature, not just a fallback.

## Acceptance criteria
- A settings toggle lets a player choose keyboard casting even when a working microphone is
  available.
- Each of the 40-word lexicon's words has a clear, documented, rebindable key.""")

# ── Networking / co-op polish ─────────────────────────────────────────────────

add("Steam lobby/invite flow has no art or branding pass",
    "ui,art,networking",
"""## Problem
`RogueAi.Net`'s `SteamInviteGateway` and PurrLobby handle the invite/join mechanics (cold launch,
rich presence, friends-list "Join Game" - `docs/4-systems/net.md`), but the lobby UI itself has not
been checked against the mood board's visual language (pigment palette, Eczar/Spectral type,
vellum-and-candlelight framing).

## Acceptance criteria
- The lobby/invite UI uses the project's established palette and type per `docs/plunderspell.md`
  §9, consistent with any other screen getting an art pass (main menu, lair).""")

# ── Lighting reference ────────────────────────────────────────────────────────

add("The pitch's six reference lighting frames have no concept art or lighting targets",
    "art,lighting,design",
"""## Problem
`docs/plunderspell.md` §9 names six specific lighting frames as a production target: the Lair, the
Treasury, the Portal, the Hue & Cry, the Approach, and a Bronze Age Interior - each with a one-line
mood description. None have concept art, a defined lighting rig, or even an agreed camera setup to
check "did we hit this" against.

## Context
Issue #19 ("Castles look bland") will address material/lighting quality generically, but three of
the six frames (Lair, Portal, Approach) aren't castle interiors at all, so #19 closing won't
produce any of them. This item exists to make the six frames a trackable deliverable rather than
prose in the pitch nobody is aiming at directly.

## Acceptance criteria
- Each of the six frames has at least a lighting-reference pass (concept sketch, or an in-engine
  reference shot) that can be compared against its one-line mood description.
- The set covers all six, not just the ones #19 will touch anyway.""")

# ── file the issues ────────────────────────────────────────────────────────────

for name, color in LABEL_COLORS.items():
    r = subprocess.run(["gh", "label", "create", name, "--color", color, "--force"],
                        capture_output=True, text=True)
    if r.returncode != 0:
        print("label warning:", name, "|", r.stderr.strip()[:200])

created = []
for title, labels, body in ISSUES:
    r = subprocess.run(["gh", "issue", "create", "--title", title, "--label", labels,
                        "--body-file", "-"],
                       input=body, capture_output=True, text=True)
    if r.returncode != 0:
        print("FAILED:", title, "|", r.stderr.strip()[:200])
        continue
    url = r.stdout.strip().splitlines()[-1]
    num = url.rsplit("/", 1)[-1]
    created.append({"number": num, "title": title, "url": url})
    print("#%-3s %s" % (num, title))

out = os.path.join("docs", "generated", "github-issues-moodboard-gap.json")
os.makedirs(os.path.join("docs", "generated"), exist_ok=True)
with open(out, "w", encoding="utf-8") as f:
    json.dump(created, f, indent=2)
print("\ncreated %d issues -> %s" % (len(created), out))
