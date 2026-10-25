# What the content mods teach PlunderSpell

Analysis of `research/content-mods-raw.md`, written 2026-10-25. It answers the owner's question:
what makes the most popular content mods work, and what can PlunderSpell build in so it does not
have to rely on a mod community?

## What the popular content mods have in common

1. **Your friends are the content.** The biggest Lethal Company content mod is Skinwalkers (17.7 M),
   which makes monsters speak in your friends' recorded voices. Mirage (6.4 M) and R.E.P.O.'s Mimic
   (4.1 M) do the same. None of them understands speech; they replay clips. It is cheap to build and
   makes the most frightening, most clipped moments in these games.
2. **Looking like yourself is progression.** Emotes and cosmetics are the most-downloaded content on
   both Thunderstore lists: TooManyEmotes 18 M, More Suits 17 M, MoreCompany 27 M (lobbies and
   cosmetics), MoreHead 11 M. TooManyEmotes sells emotes in the game's own store, so cosmetics become
   something to spend money on.
3. **One monster, one trick, one counter.** Mimics (14.3 M) is one monster with one disguise and a
   few ways to spot it. CoilHeadStare (5.6 M) changes one behaviour. Small, readable rules give the
   most fear for the least work.
4. **Upgrades that answer a named threat.** Lategame Upgrades (10.7 M) has about 50 purchases, and
   the memorable ones counter a specific monster: Silver Bullets for the Ghost Girl, Beekeeper for
   bees, the Fedora Suit for Butlers. That is the owner's "money buys counters" in practice.
5. **Contracts give a run a purpose.** Lategame Upgrades' five contracts (defuse, hack, exterminate,
   exorcise, rescue) turn a scavenging run into a job with a twist. FTB Quests (227 M) does the same
   for Minecraft modpacks. This matches the owner's vendor quests.
6. **Greed sets off an event.** FacilityMeltdown (4.6 M) turns taking the most valuable object into
   a 2-minute escape. "More risk (and reward)" in one rule.
7. **Items with behaviour.** LethalThings' plush that may set the house on fire, SillyValuables'
   250+ valuables with effects, Alex's Mobs' rule that every mob has a purpose. A piece of loot that
   does something is worth more than ten that only have a price.
8. **Creatures tied to places, with random events and summoned bosses.** Lycanites Mobs (29.2 M) ties
   spawns to biomes and actions (mine ore, a Geonach appears), runs optional random waves, and has
   bosses you summon at an altar with a key. Twilight Forest (210.7 M) gates areas behind bosses.
9. **A casino is a popular add-on.** LethalCasino (3.9 M) puts slots, roulette and blackjack on the
   Company moon, and you bet scrap.
10. **Everything is data and config.** Lycanites sets every spawn in JSON; Lategame Upgrades has one
    large config the host syncs; LethalCasino's odds are configurable. Content mods stand on shared
    libraries (LethalLib, REPOLib). That is what lets thousands of people add content.

## What PlunderSpell can build in

| Lesson | In PlunderSpell |
|---|---|
| Friends are the content | The owner's cursed object: whoever carries it is copied, and a horror walks the castle in their voice. A clip-replay mimic like Mirage, recorded locally, no speech understanding. Earlier work to reuse: `claude/voice-mimicry-improvements-7a0b29` and `claude/guard-mimic-prototype` |
| Cosmetics are progression | Wizard hats, robes and colours, and emotes, bought in the Market; the wizard design already has a colour each and a name on the hat |
| One monster, one trick | Each Age's household gets one true form with one sense and one counter, like the Wick |
| Upgrades answer threats | Market stock named by the threat it counters: the water spell for fire, a ward for the cursed object, a hood that dims your light for the Wick |
| Contracts | Vendor quests for a named piece, a quality, sometimes a variant (the owner's direction) |
| Greed sets off an event | Lifting a treasury's best piece can start an event, such as the vault sealing or a fire spreading |
| Items with behaviour | Platinum, gold, cursed and flammable variants, each with a rule (the owner's direction) |
| Places, events, bosses | Age-specific events and an occasional boss, picked at random per raid |
| A casino | Blackjack in its own corner of the Market, from the owner's existing code |
| Data and config | Variants, events, quests, vendor stock and guard tuning as data assets (Unity ScriptableObjects or JSON), so one person can add content without code, and modders can later |

The last row is the "less with more" rule and the base for the Thunderstore stretch goal: if the
game's own content is data, a mod is just more data.
