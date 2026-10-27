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

## After the proposal was denied (owner, 23:07 UTC)

- "We need to do the design in phases, steps, not all at once." Everything should be a choice to some degree;
  first an overarching plan, then each part broken down further.
- **Worth:** diegetic, but inferable from criteria players already understand. It scales with rarity and with how
  hard and risky the piece makes extraction: heavy to move, fragile, alive. "The harder it makes your life and the
  riskier the extraction because of it, the more it is worth. Make the logic intuitive."
- **Ages:** all open from the start, as in Lethal Company. Random events may make one Age more appealing, but they
  are hidden at first; a later unlock gives foresight, so you are told in advance what event a raid has.

## Answers on the comparison page (owner, 23:21 UTC)

Given on "Principles v2 against the core loops proposal" (https://claude.ai/artifact/6wwKL72YCFBrzjWChr1XML),
reply "Comparison read: principles v2 vs core loops 1* 2* 3A 4* 5A 6A 7A". These feed area 1, the hook and the
first five minutes.

- **The 15-second line (own answer):** none of the offered lines. It must still convey what you do in the game:
  "fish, shoot fish, sell fish" tells you the loop; "shout spells, the castle hears you" does not.
- **The first spell (own answer):** find a fun spell a new save can use first that does not harm progression or
  make you overpowered. It must be fun, not make things easy, which Ignis partly does.
- **The castle hears ordinary talk:** yes by default, with an off switch (3A).
- **The first raid (own answer):** a raid runs 5 minutes at most. Start in the Lair. The first raid goes to a
  default, pre-chosen Age (as Lethal Company pre-picks a moon) that you can override. Before you leave, the Ophan's
  first demand is already visible, due at the end of the first raid rather than in 3 to 5 raids, and the Ophan
  hunts you in the last 60 seconds. The first demand must be easy to find, hard to miss, and never an item that
  cannot be found in that Age.
- **Worth during a raid:** losses shown in the world, numbers only in the Lair and Market (5A).
- **Dead wizards:** a ghost that whispers, and the castle hears it (6A).
- **Playtest:** a gate at the end of the first build phase, before the next is built (7A).

Still open for area 1, asked on the comparison thread's follow-up page (https://claude.ai/artifact/TKguGwq5PzSpeoRXCamM4u):
the loop line in the "fish, shoot fish, sell fish" shape; the first spell; whether the 5-minute limit applies to
every raid (if so, the 15 to 25 hour estimate needs redoing); the default first Age; where the first demanded piece
sits. Not to be asked again here.

## Area 1 answers on the hook options page (owner, 23:27 UTC)

Reply "Hook options: area 1 1* 2A 3A 4A 5*", given in the comparison thread.

- **First spell for a new save:** Frango (2A).
- **Every raid lasts 5 minutes at most** (3A). The 15 to 25 hour campaign estimate must be redone with this.
- **Default first Age:** High Medieval (4A), which can be overridden.
- **The first demanded piece (own answer):** deep in the vault, and the players are told so. "You must be allowed
  to fail and challenged here. Making it too easy isn't fun, making it impossible isn't either."
- **The loop line (own answer):** still open; asked for three more ideas, since none so far captures the full loop.

## The whole game loop, in the owner's words (23:32 UTC)

- "Plunder, cast spells, escape alive, sell, upgrade, and meet the Ophan's demands until you can grow strong
  enough to defeat it."
- Similar to Lethal Company, but the quota is a specific artifact rather than an amount, progression persists,
  and there is a long goal to work towards with friends.
- Loop line: "Shout spells, rob castles, feed the Ophan" is close, but it needs prior knowledge of what the Ophan
  is; the line should hint at it instead of naming it. Versions are being drafted in the comparison thread.

## The loop line, decided (owner, 23:43 UTC)

- **"Shout spells, rob castles, appease the Ophan that owns you."**
- This reverses "don't name the Ophan": a quick search explains the name, "angel" reads too positive to be scary,
  and "that owns you" carries the fear and servitude, so the name can keep its mystery.
- With this, every hook option for area 1 is answered.

## The demand loop's questions, answered (owner, 2026-10-26 03:58 UTC)

"Design approved: demand loop questions 1A 2A 3A 4A 5* 6A", on
`docs/plans/core-loops/areas/demand-loop/demand-loop.html` (https://claude.ai/artifact/9qKm7RHD8JdiWYds2Ssk5S).

1. After the first, a demand falls due every 3 to 5 raids, at random (A).
2. Failing: every raid is a hunt and its haul is taken until the demand is paid (A, the fear pass's rule).
3. The Ophan comes only when a demand is due or still owed (A).
4. When it takes the piece it leaves at once; one holder saves everyone (A).
5. (Own answer) **The demand has nothing to do with progression.** It holds off the beast, "a deterrent but never
   the solution". The Ophan starts omnipotent and undefeatable; over play you learn of a weakness through lore found
   in the world, as in Lethal Company and especially the Wesley's Moons mod. That needs writing and world-building
   first, lore that fits the world and makes the progression make sense, without making this a narrative game.
   So `DemandsToConfront` is dropped from the tuning, and **lore and world-building** becomes its own design area.
6. Players recognise the piece by the ledger's sketch plus a faint mark when near, which fades after the first few
   demands (A).

## Playtesting (owner, 2026-10-26 04:08 UTC)

- No four-player playtests for now: the owner has two Steam accounts and two PCs. Playtests are solo first, then
  two players. **The game must be fun solo**, not only survivable, because the owner tests alone.

## The demand loop's build plan, approved (owner, 2026-10-26 04:10 UTC)

"Build plan approved: demand loop 1A 2A 3A 4A 5A", on
`docs/plans/core-loops/areas/demand-loop/build-plan.html`: tracked as #431 with #432 to #435, plus #418 and #419
updated and moved under it; the Ophan gets its own designed warning (#434); the coin debt code is removed; Unity runs
in a Remote Control session on the owner's PC; the Ophan's arrival is built before the demand. The Wick's pull
request is #436.

## The Ophan's arrival, answered (owner, 2026-10-27 04:10 UTC)

The owner's reply to `areas/ophan-arrival/ophan-arrival.page.html`: "Design approved: ophan arrival 1A 2B 3* 4* 5B 6A 7*".

- **1A.** Until the demand (#419) exists, any carried piece flagged as an artifact counts as wanted.
- **2B.** With two players, it targets whoever carries the most.
- **3, the owner's own answer:** "it arrives in the sky first and hunts from the sky too, i want it to not actually catch you but to instead attack you with beams of light or something similar, it appears in the sky larger than life a threat you shouldn't believe you can defeat until the lore shows you otherwise"
- **4, the owner's own answer:** "it doesn't chase you, it hunts from the sky and had a set delay to lock on, it needs to appear a celestial level threat"
- **5B.** The sky idea (#430) wins: the Ophan stays in the sky and does not come down to hunt on foot.
- **6A.** After it kills an empty-handed player, it keeps hunting the others until the portal closes.
- **7, the owner's own answer:** "build it and warn it's coming with a loud menstrual hum and screen shake". Read as a loud *monstrous* hum (a likely typo), pending the owner's correction.

What this changes in #418: no walking, drifting or catching through walls. The Ophan appears in the sky, larger than life, and attacks with beams of light after a set lock-on delay. Its warning is a loud hum with screen shake. Follow-up questions on how the beam plays are on `areas/ophan-arrival/ophan-beam.page.html`.
