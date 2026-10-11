# The fear pass: raids you barely survive

**Status: design in progress (#386, under #380).** Direction set by the owner on 2026-10-11; questions
still open below. No code until the written design is approved.

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

**Spells come from two places.** Some spells can only be bought in the Market; some of the later,
more special spells can only be learned by finding an ancient text in a castle.

## First slice

1. The spell lockdown: Porta at the start, Somnus learned early, the rest locked, backfire
   consequences.
2. One two-form guard, with its true-form model and animation, added to the existing roster.
3. More guards spawning on the ground floors.

Later slices: the rest of the roster as horrors, each with its own senses, behaviour and counter.

## Open questions

- What exactly triggers the change to true form: being seen, being heard, being hit?
- How Somnus is first learned (bought, or found), and which spells come from which source.
- The creature: what it is, where it lives (the Lair?), what happens when you come back
  without its artifacts, and how the Lair's ledger, the Collector and coin pouches change.
- What does a Somnus misfire cost?
- What happens to found weapons and to throwing heavy objects at guards?
