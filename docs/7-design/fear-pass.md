# The fear pass: raids you barely survive

**Status: approved 2026-10-11 (#386, under #380).** Direction and all four parts of the first slice
approved by the owner on 2026-10-11. Next: an implementation plan with one issue per step.

## What the owner wants

A raid should feel like Lethal Company: breaking into the castle and leaving alive is hard, and a
good night ends with the feeling that you barely escaped. Early on the player is powerless; respite
comes only with progress. Today stealth is optional unless you are swarmed or unlucky, because
players start with every spell, Ignis does damage, guards can be killed, and raids drop weapons
(`docs/4-systems/spells.md`, #123).

## Decided (owner, 2026-10-11)

**Guards have two forms.**
- They first look like tame, ordinary castle foes.
- When one sees you or is provoked, it morphs into its true form: a Lovecraftian horror meant to
  frighten even a seasoned horror player, still inside the game's look and its period, with some
  fantasy licence.
- Human form: a hard enough blow to the head stuns it briefly and knocks it over.
- True form: run and hide is the only answer.

**Spells are earned.**
- A new player starts with **Porta** only, to open doors.
- **Somnus** is learned early. It works on guards only, and misused it costs you dearly.
- The movement spells, **Velox** and **Saltus**, unlock mid-game.
- Every offensive spell is a late-game tool, locked for now.
- Every spell has a real consequence when it backfires.
- Speaking always has a consequence, because the enemies sense the world, each in its own way (some
  by sight rather than sound), as in Lethal Company. Spell unlock order is also #379.

**Every guard type has a counter.**
- It is never easy, and sometimes it is just run and hide.
- Each type's behaviour hints at, or dictates, what its counter is, the way Lethal Company's enemies
  each have their own strategy.

**The threat is visible early.** Unlike Lethal Company, the player should see guards from the moment
they enter. More guards spawn on the ground floors, so the entry already demands stealth, and players
see the guards and underestimate them before they learn how wrong they were. Enemies should still be
able to sneak up on you.

**A true form stays turned** for the rest of the raid: every mistake makes the castle worse for good.
**One catch kills:** if a true form reaches you, you are down, so the chase is the whole fight.

**The debt goes; a creature wants artifacts.** The money debt is weak (why would time-travelling
wizards owe dues?) and is dropped, at least in its current form. Instead, a creature demands specific
artifacts. Each raid then has two goals:
1. **Main:** fetch the items the creature wants before the portal closes and what you have been
   running from catches up with you empty-handed.
2. **Second:** carry home whatever else you can, to sell in the Market for upgrades, so that
   eventually, maybe, you can fight the creature instead of always looking over your shoulder and
   risking your life for its trinkets.

**The creature hunts you through time.**
- The Lair and the Market stand outside time, so they are always safe.
- Its demand, the quota, falls due every 3 to 5 raids as a base. The number is configurable.
- If the portal closes while you are still in the castle, you are left behind.
- If the creature catches you empty-handed, you die in that raid. If it catches you carrying its
  artifacts, it takes them, the quota counts as paid, and you live.
- Dying in a castle loses every item you brought in and every item you meant to bring back. The
  castle keeps everything you don't take.
- Learned spells stay learned. A spell text you found but had not yet learned is lost with you.
- Spells are learned only in the Lair during calm, never under duress in a raid.
- If the company fails to fetch the artifacts, the creature hunts them on the next raid and takes
  all of the spoils from that raid and every one after, until what it is owed is fetched.
- Nobody starts over. It is purgatory, and the only way out is to grow strong enough to defeat a
  celestial being. That is the mountain the company climbs, and only together.

**Spells come from two places.** Some spells can only be bought in the Market; some of the later,
more special spells can only be learned by finding an ancient text in a castle.

**More of the two-form guard (owner, 2026-10-11).** A guard turns when it confirms it has seen you,
or when it is hit; hearing you only makes it suspicious, still human. A missed or too-loud Somnus
wakes its target, which sees you at once and turns.

**Somnus is bought in the Market** with coin from the first hauls.

**The creature (owner, 2026-10-11).** It is a physical thing that arrives in the castle late. Its
demand is written in the Lair's ledger book. It replaces the Collector, who goes. The first two-form
guard is the common High Medieval guard.

## The first slice (approved 2026-10-11)

### 1. Spells are earned
`SpellCastingSystem`, the save slot, the Market.
- Each save slot keeps the set of spells the company has learned. A new save knows Porta only.
- Casting an unlearned spell does nothing but fizzle as a misheard word does today, and it still
  makes its noise, so speaking keeps its cost.
- A Market vendor sells the Somnus scroll. A carried scroll teaches nothing until it is read at a
  lectern in the Lair while the Lair is calm; a scroll lost in a castle is gone.
- Somnus works only on guards. A miss, or a cast that is too loud, wakes its target, which sees you
  at once and turns.
- Offensive spells and found weapons are locked or taken out of the loot tables for now; their code
  stays for later.

### 2. The two-form guard
The existing `Guard` and its states.
- The same guard object gains a one-way **True form** state. Seeing you, or being hit, triggers it;
  hearing you only sends it to investigate, still human.
- On turning it swaps to a second model and a faster, sharper-sensed tuning, plays a reveal, and
  raises a fear event on every machine (sound, camera, music).
- A true form never turns back and kills on a catch. The human form is stunned and knocked down by a
  hard blow to the head, and is never killed.
- Built first on the common High Medieval guard, with its model and clips made through the art
  pipeline; its look is designed with the owner before it is modelled.
- More guards stand on the ground floors, so the threat is seen from the gate.

### 3. The creature and its quota
Replacing the debt and the Collector in `LairHubManager`.
- Each save slot keeps the quota: the wanted artifacts and the raids left (3 to 5 by default, from a
  tuning asset). The ledger book's pages show the demand, with a sketch of each artifact, instead of
  the debt.
- Wanted artifacts are marked loot in the castles. Carried home and placed in the Lair, they pay the
  quota and the next demand appears.
- Unpaid when the count runs out, every raid is a hunt until it is paid. The creature arrives in the
  castle as the portal's clock runs low, and each hunted raid's spoils are taken.
- Caught carrying its artifacts: it takes them, the quota is paid, you live. Caught empty-handed: you
  die.
- Death loses what you carried and any unread scrolls; learned spells and the Lair stay. The portal
  closing leaves anyone inside behind.
- The Collector and the debt shares go. Coin only buys things in the Market.

### 4. Order and checks
Spells first, then the guard, then the creature, each on its own branch as a pull request into
staging. Each gets tests for its rules, a co-op check script run with the Editor host and a built
client like the existing ones, and before-and-after screenshots looked at. The true form's and the
creature's art go through the art pipeline with a review sheet each.

Later slices: the rest of the roster as horrors, each with its own senses, behaviour and counter;
Velox and Saltus mid-game; the later spells.

## Still open (not needed for the first slice)

- Which later spells come from the Market and which only from ancient texts.
- What the creature and the true form look like (designed with the owner before modelling).
- Whether a thrown heavy object can stun a human-form guard as a blow to the head does.
