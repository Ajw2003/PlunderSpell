# Analysis: what the hits get right, where they fall down, and what PlunderSpell should take

Stage 2 of the core-loops work (`docs/plans/core-loops/README.md`), written 2026-10-25. It reads the
six raw research files in `docs/plans/core-loops/research/` and answers the owner's question:
how does PlunderSpell give steady, incremental progress toward a real end goal, not just survival?
The page version is `analysis.html` beside this file. Charts are in `charts/`.

## The short answer

The recent hits fall into two families.

- **Chaos first** (Lethal Company, R.E.P.O., Content Warning, PEAK). Four friends, a dangerous
  place, physics and proximity voice. These make the best clips and spread the fastest, but they
  have no long goal. Lethal Company has no ending at all, by design.
- **Progress first** (How to Fish, Schedule I, Cookie Clicker, Scritchy Scratchy, Balatro,
  Vampire Survivors). A number that keeps going up, purchases that change how you play, and a far
  goal. These are the games the owner loves for "consistent dopamine".

How to Fish sold 10 million copies in under two months because it is both: co-op chaos on top of
an incremental ladder. Its weakness is that the ladder is short. It has three islands, so once
they are done there is nothing left to climb, and it lost 78% of its players in its second month.

PlunderSpell already has the chaos half (voice, weight, physics, the Wick) and an approved far goal
(defeat the Ophan of Hours). **What it lacks is the middle: the hour-to-hour ladder.** Coins can be
earned and haggled for, but they buy nothing yet. That is the first thing to fix.

## Every hit loses most of its players; updates bring them back

Monthly average players on Steam, as a share of each game's best month (chart:
`charts/retention.svg`; data: `research/progression-and-retention-raw.md`).

| Game | Best month | Latest | Left |
|---|---:|---:|---:|
| Lethal Company | 107,897 (Dec 2023) | 2,654 (Sep 2026) | 2% |
| R.E.P.O. | 105,384 (Mar 2025) | 16,858 (Sep 2026) | 16% |
| PEAK | 62,372 (Aug 2025) | 17,835 (Mar 2026) | 29% |
| How to Fish | 148,647 (Aug 2026) | 32,419 (Sep 2026) | 22% after one month |

Every bump in those lines after launch came within a month of new content, such as R.E.P.O.'s
November 2025 cosmetics update (53,071 average, up from 20,076). A fix-only patch did not bring
How to Fish's players back (DLCompare, 11 Sep 2026). Two lessons follow. A viral launch is a spike,
not a base. And what keeps a base is something new to reach for, which is exactly what an
incremental ladder supplies between content drops.

## Strengths and weaknesses, game by game

| Game | Strength to take | Weakness to avoid |
|---|---|---|
| Lethal Company | Each monster has its own sense and a counter you learn, so knowledge is progress; a rising quota with a deadline; selling early is safe but cheap (33% to 100% by day) | No ending and nothing carried between runs; 2% of its best month remains |
| R.E.P.O. | Fragile physics loot; a shop between levels; a killed monster drops an orb worth $1k to $8k that can still break on the way out (a threat becomes a gamble) | Upgrades last only for one run; no long goal |
| Content Warning | The game's own output is a shareable video; the free launch day put it in 6.2 million libraries | A three-day quota reset wipes progress |
| PEAK | Daily map for everyone; one summit as a clear goal; badges unlock cosmetics | Six fixed levels; 74% fall from its best month by Feb 2026 |
| How to Fish | Every purchase opens the next thing (gun, then boss, then island, then rarer fish); rare variants; trick-shot multipliers; a casino on the side | Three islands; "fish, kill, sell" repeats; weak solo; one reader was done after two evenings |
| Schedule I | Systems feed each other (product, mixing, customers, workers, property, regions); a rival's demand every 2 to 3 weeks | Management can bury the action |
| Cookie Clicker | Costs rise 15% per building while income rises too; golden cookies are random spikes; ascension resets for a permanent bonus, so there is always a "next" | No ending; play becomes watching |
| Scritchy Scratchy | Every scratch is a tiny bet; dearer tickets pay more; penalty symbols make you choose when to risk it | Once robots scratch for you, the hands-on part dies (top critical review, 13.4 h) |
| Balatro | A clear end (ante 8), then unlocks and harder stakes for the next run | Its gambling look cost it a PEGI 18 rating and YouTube age gates |
| Buckshot Roulette | Known odds, hidden order; double or nothing | Thin progression |
| Vampire Survivors | A level-up choice every minute; weapons evolve; gold buys permanent stats | Permanent stats make it easier, not more skilful |
| Gamble With Your Friends | Shared bank and a shared debt make every bet a group decision | Narrow: the whole game is the gamble |

## What makes incremental progress feel good

Drawn from How to Fish, Cookie Clicker, Scritchy Scratchy, Balatro and Schedule I.

1. **Three clocks of reward running at once.** Seconds: a catch, a scratch, a grab. Minutes: a
   purchase. Hours: a new island, region or reset. Chart: `charts/three-clocks.svg`. PlunderSpell
   has the seconds clock, half of the minutes clock, and no hours clock.
2. **Every purchase opens something.** In How to Fish a better gun beats a boss, which opens an
   island, which holds rarer fish. A purchase that only adds 5% is weak; a purchase that changes
   what you can do is strong.
3. **The systems feed each other.** Fish pay for guns, guns open islands, islands pay more. In
   Cookie Clicker buildings make cookies that buy upgrades that multiply buildings. No system is a
   dead end.
4. **Random spikes on top of a steady climb.** Golden cookies, rare fish variants, jackpots,
   R.E.P.O.'s monster orbs. The climb is earned; the spike is luck, and luck is what people clip.
5. **A goal you can always see, near and far.** The next island and the way home; the next ticket
   tier and the jackpot; the next ante and ante 8.
6. **A "next" after the end.** Cookie Clicker's ascension and Balatro's stakes restart you
   stronger or harder, so finishing is not quitting.

## The pitfalls, and how PlunderSpell can avoid each

| Pitfall | Seen in | How PlunderSpell avoids it |
|---|---|---|
| No end goal | Lethal Company, R.E.P.O., Cookie Clicker | The Ophan of Hours is already the end goal (approved 2026-10-11). Show the climb to it as a visible ladder in the Lair, with rungs the company passes |
| The ladder runs out | How to Fish (three islands) | Castles are generated fresh every raid; four Ages each with their own household; carrying weapons between Ages multiplies the combinations; a "next cycle" after the Ophan |
| Power makes it easy | Vampire Survivors, roguelites (Game Maker's Toolkit) | Most unlocks are new verbs and options (spells, tools, routes), not bigger numbers; the quota and the Ophan grow with you; each guard's counter is knowledge you learn, as in Lethal Company |
| Automation kills hands-on play | Scritchy Scratchy | Automate chores only, never the raid. Carrying, casting and escaping stay in your hands |
| Friends' schedules | How to Fish readers | One company save that any of its members can play, solo or in part, with guards scaled to the number present |
| Progress wiped by one failure | Content Warning, Lethal Company | Already decided: nobody starts over. Death loses only what you carried |
| Gambling rating and clip reach | Balatro | In-game coin only, no real money; period dice and wagers rather than slot machines and casino tables |
| Coin with nothing to buy | PlunderSpell today | Build the Market's buying side first: spells, tools, Lair rooms, and luck |

## What exists already, and what the next stage builds on

- **Keep:** the Wick's design (sees only in its own fire's light, one-way turn, one catch kills)
  as the model for every true form; haggling with *Plus*, *Satis*, *Vale*; coins as heavy pouches;
  the Ophan's quota every 3 to 5 raids; spells earned from Porta upward; death keeping the Lair.
- **Missing:** anything to buy; an hours-long ladder; random spikes beyond loot rolls; a visible
  ladder toward the Ophan; a "next" after it; any gambling.

## Questions for the owner before the proposal

The page asks these as choices, each with a suggestion. The proposal (stage 3) is built on the
answers.

1. After the Ophan is beaten: a new cycle that starts stronger and harder (suggested), or the
   campaign ends?
2. What coin mostly buys: new things to do, such as spells, tools and Lair rooms, plus a few small
   stat upgrades (suggested), or mostly stat upgrades as in R.E.P.O.?
3. How long a regular group should take to beat the Ophan: about 15 to 25 hours (suggested),
   about 8 to 10 like How to Fish, or 40 or more?
4. Gambling: wagers woven into the loop, such as dice with vendors, double-or-nothing on a haul
   and cursed relics, all in-game coin (suggested); a separate dice den only; or none?
