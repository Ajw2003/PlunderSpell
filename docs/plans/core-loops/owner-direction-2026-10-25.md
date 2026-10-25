# The owner's direction for the core loops (2026-10-25)

Written down from the owner's message of 2026-10-25, 21:55 UTC, after the stage 2 analysis
(`analysis/analysis.md`). It answers most of that page's four choices and adds new direction. Stage 3
(the proposal) is built on this file. The owner's own words are in quotes where the wording matters.

## The guiding rule

"Less with more": easy-to-tweak, expandable systems that give the most gameplay nuance for the least
effort. Prefer one data-driven system (variants, events, quests) that many items and Ages reuse over
one-off features.

## The shape of the loop

- **Largely How to Fish's approach**: random variants, effects and events, a shop, and weapons that
  each give a different play style.
- **Numbers on screen**: worth "goes up or down visibly on your screen", getting larger step by step
  so you can buy larger upgrades, tools and weapons, and see your progress in steps.
- **Lethal Company's choice of destination**: you choose where you go. Each Age has its own threats,
  which need their own counters, strategies or upgrades.
- **"The environment always fights back"**, and each object needs its own strategy.

## Loot variants

| Variant | Effect (owner's words where given) |
|---|---|
| Platinum | worth more, more fragile |
| Gold | a step below platinum in worth, more durable |
| Cursed | does various things and needs a certain spell to counter; the obvious one is Lethal Company's mask (it turns the holder into a Masked) |
| Flammable | catches fire (see Fire below) |

## Cursed objects and voice mimicry

- A cursed object can work like Lethal Company's mask, especially with a mimic mod, "and the horror
  that creates".
- The local voice mimicry worked on earlier comes back, "stripped back to what the mimic mod for
  Lethal does", for now. (Earlier work: branches `claude/voice-mimicry-improvements-7a0b29`, tag
  `archive/voice-mimicry-improvements-7a0b29`, and `claude/guard-mimic-prototype`.)

## Fire

- "There is tons of fire." Things are flammable: carrying a book into a torch sets it ablaze.
- A water spell, or water in set places that can spawn, makes fire "not an immediate death sentence
  but a likely one".

## Vendors give quests

- Like How to Fish, vendors give quests for specific items they want, pay by the item's quality, and
  may ask for specific variants.

## The Ophan and luck

- The Ophan never asks for a specific variant unless there is a way to guarantee getting it.
- Optional progression may be gated by chance. Required progression must never depend on luck in a
  way that can soft-lock the company.

## What money buys

- Spells, tools and weapons that counter certain challenges, so you progress further and more easily.
- Skins, cosmetics and emotes: look into them as a large part of progression, as in similar games.

## Gambling

- **Blackjack first.** The owner already has blackjack code in their project "The Fall of Golden Bay
  2D". (That repository is not in this session's scope yet; find it with `list_repos` before reusing.)
- Cards for quick gambling in a separate area of the Market.
- Perhaps later, "hail Mary" mechanisms that can save a run.

## Stakes

- Review the stakes: what losing brings, what winning brings, consequences for your actions, and proper
  risk and reward.

## Events and bosses

- Random in-world events add nuance, like How to Fish's random bosses or the Lycanites Mobs mod for
  Minecraft.

## A living world: events from your actions and at random (owner, 22:07 UTC)

- More focus on the environment triggering events. The game already tells its story through the
  environment (the fires show how worried you should be).
- **Your actions have consequences in the world**, like FacilityMeltdown: an act sets off an event.
- **Random events, like Lycanites Mobs.** The owner's examples:
  - plague rats that spread through the castle;
  - the Ophan deciding to show up early;
  - a full moon that turns every enemy, as the Wick turns;
  - an invasion: new enemies pour in through the front gate and the players are caught in the middle
    of the fighting.
- "I want this world to feel alive, and dangerous, and for your actions to have clear consequences."

## Mods

- Mods help explain why these games spike, fall sharply and still keep a dedicated base. Lethal Company,
  Minecraft and R.E.P.O. are "infinitely more fun modded", and that community keeps them alive.
- Easy mod support with Thunderstore is a stretch goal.
- "We can't rely on that." First: research the most popular **content** mods (not performance or
  quality-of-life ones), analyse what makes them work, and what PlunderSpell can learn and apply.

## How this answers the analysis page's four choices

| Choice | Answer from this message |
|---|---|
| What coin buys | spells, tools and weapons that counter challenges, plus cosmetics and emotes (close to option A) |
| Gambling | blackjack and cards in a separate area of the Market, later hail Mary mechanisms (option B, a separate den, plus hail Mary) |
| After the Ophan | a new cycle, harder and richer, that can be expanded with future content (option A, 22:20 UTC) |
| Campaign length | about 15 to 25 hours (option A, 22:20 UTC) |

## Event conveyance (owner, 22:20 UTC)

- "The fire can't be the only visual, there needs to be bespoke conveyance for each event and that
  needs to be planned and designed, not just blanketly addressed." Each event gets its own sights and
  sounds that warn of it, designed one by one.
