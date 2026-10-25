# Raw research 2 of 4: small indie games that went viral, 2020 to 2025

Stage 1 of the core-loops work. Raw data only, gathered 2026-10-25 from the web pages listed in
`sources.md`. Sales figures for indie games are mostly third-party estimates (Gamalytic, VG
Insights, Alinea Analytics); each row says which. Where two sources disagree, both are given.
Prices marked "about" for Buckshot Roulette and Phasmophobia are from general knowledge, not from a
fetched page; R.E.P.O.'s "$8" is from a headline cited on its Wikipedia page.

## The numbers

| Game | Release | Team | Price | Copies sold (source, date) | Peak players on Steam |
|---|---|---|---|---|---|
| Lethal Company | 23 Oct 2023, early access | Zeekerss, solo | $9.99 | 10 M+ (Push To Talk estimate, Jan 2024); about 15 M+ (Games Market, Dec 2025) | 100,000 (Nov 2023, Wikipedia); 240,000+ (Games Market) |
| R.E.P.O. | 26 Feb 2025, early access | semiwork (Sweden), small | about $8 | 3.1 M in under 3 weeks (Gamalytic, 17 Mar 2025); 1.5 M (VG Insights); 16.4 M in H1 2025 (Alinea) | 230,645 (SteamDB, Mar 2025); 271,000 (80.lv) |
| Content Warning | 1 Apr 2024 | Landfall, 5 people, made in a game jam | free 24 h, then $7.99 | 6.2 M claimed free in 24 h; 1 M+ sold after (Landfall, mid-Apr 2024); about 5.2 M+ (Games Market, Dec 2025) | 200,000+ in 24 h (Wikipedia); 204,000+ (Games Market) |
| PEAK | 16 Jun 2025 | Aggro Crab + Landfall, started as a one-month jam | $5 at launch, then $8 | 100 k day 1, 1 M week 1, 2 M day 9, 5 M month 1, 10 M+ by mid-Aug 2025 (developers) | 170,000+ (Games Market) |
| Schedule I | 24 Mar 2025, early access | TVGS, solo | not found | 1.8 to 2 M in a week (Gamalytic, 31 Mar 2025); 9 M in H1 2025 (Alinea) | about 414,000 first weekend (80.lv); 459,000 (TweakTown headline: most by a solo developer in Steam history) |
| Balatro | 20 Feb 2024 | LocalThunk, solo, about 2.5 years | not found | 1 M in month 1; 5 M+ by Jan 2025, about half after the Sep 2024 mobile release (publisher Playstack) | not found |
| Vampire Survivors | Dec 2021 early access, Oct 2022 full | poncle, solo at start | $4.99 | up to 7.9 M, about $23.8 M revenue (VG Insights estimate via SteamPulse) | 6,517 (SteamPulse; this conflicts with commonly reported figures and was not re-checked) |
| Buckshot Roulette | 28 Dec 2023 itch.io; 4 Apr 2024 Steam | Mike Klubnika, solo | about $3 | 1 M in 2 weeks on Steam (Apr 2024); 4 M by Dec 2024; 8 M (KitGuru headline, later) | not found |
| Phasmophobia | 18 Sep 2020, early access | Kinetic Games, small | about $14 | nearly 22 M total by Dec 2024 (Wikipedia) | 86,000+ (Oct 2020, GitHyp) |
| Mage Arena | 24 Jul 2025, early access | jrsjams | $2.99 launch discount | not found | 16,000+ active in week 1; 2,600 reviews, 92% positive in 5 days |

Market context: co-op games grossed $4.1 billion on Steam in H1 2025, an all-time high, up 11% year
over year (Alinea Analytics). Three of the top five sellers in that half were small co-op indies:
R.E.P.O., Schedule I and PEAK.

## Loop structure, game by game

**Lethal Company.** Land on a moon, enter one facility (factory, mansion or mine), collect scrap,
return to the ship before midnight or the autopilot leaves without you. Sell at the Company moon.
Profit quota every 3 days; the first quota is always 130, and each increase is
`100 × (1 + n²/16) × (random curve + 1)` where n is quotas met so far. The Company pays 33%, 53%,
77% and 100% of scrap value on days 1 to 4 of the cycle. Overtime bonus = (excess ÷ 5) + 15 per day
left. Miss the quota: the crew is jettisoned and the run ends. Credits buy tools at the terminal,
ship furniture (which adds hidden "luck" that lowers the quota) and access to dearer, deadlier
moons. Inventory: 4 scrap slots plus 1 tool; two-handed scrap blocks ladders. 12 moons.

**R.E.P.O.** Up to 6 players. Randomised modular levels. Carry valuables (physics objects that lose
value when damaged) to extraction points, fill each point's quota, escape to the truck. Surplus
above quota is money. Between levels a Service Station sells permanent upgrades (strength, stamina,
health and more), weapons, explosives, tools and consumables. Killing a monster drops an orb that
counts as a valuable and can itself be damaged: level-one monsters $2k to $5k, level-three monsters
up to $8k (PC Gamer).

**Content Warning.** Up to 4 players dive in a diving bell into the Old World with a camera, about
500 s of oxygen. Film monsters; upload to SpöökTube; views and ad money rise with how dangerous the
monster is, how big it is on screen and how it interacts with players. View quota within 3 days,
one dive per day. Miss it: equipment and money are lost and the run resets. Ad money buys lights,
medical kit, gadgets and emotes. Carry up to 3 items. 32 monsters (May 2024).

**PEAK.** Up to 4 players climb a generated mountain after a plane crash. Each player has a stamina
bar shrunk by hunger, injury, carried weight, poison and cold. Fallen players are revived with items
or at altars. Six levels in fixed order with two biome variants each for levels 2 to 5. The map
rotates every 24 hours at 17:00 UTC for everybody. Badges (achievements) unlock cosmetics. The
Scoutmaster appears if a player gets about 200 m above the group. Proximity voice chat.

**Schedule I.** Solo or co-op drug-empire management. Grow, cook, mix ingredients to raise sell
value (with joke side effects), sell to customers while avoiding police "heat". Hire workers, buy
property and front businesses. New regions unlock by gaining customers and pushing out a rival
cartel. A rival truce demands product every 2 to 3 weeks; refuse or miss it and you are attacked.

**Balatro.** A run of 8 antes, each with three blinds (small, big, boss) and a score target. The
boss blind changes a rule (disables a suit, limits hands). Skipping a blind gives a tag for the next
shop. Between blinds, a shop sells jokers, booster packs and vouchers. Score = chips × mult, and
jokers stack multiplicatively. Meta progression: new decks, harder "stakes" and new cards unlock by
meeting conditions. Endless mode past ante 8. PEGI rated it 18+ for "prominent gambling imagery",
then 12+ in Feb 2025; YouTube age-gated many Balatro videos in Mar 2025.

**Buckshot Roulette.** Russian roulette with a shotgun loaded with a known count of live and blank
shells in unknown order, against a dealer. Shoot yourself on a blank for another turn. Items from
round 2 (magnifier, beer, handcuffs and more). Double or Nothing mode: up to $70,000 per win, then
bet it on three more rounds to double it or lose it.

**Phasmophobia.** Up to 4 players investigate a haunting, gather evidence with tools, name the
ghost type, leave. Speech recognition is core: the ghost hears players speak during a hunt and
moves toward the voice; the Ouija board answers spoken questions; the Monkey Paw grants one of 10
spoken wishes, each with a cost. Money and XP unlock maps, tools and tool tiers.

**Mage Arena.** Team PvP between Sorcerers and Warlocks. Spells are cast by speaking into the
microphone (Fireball, Freeze, Magic Missiles, Wormhole). Torn Pages found on the map unlock more
spells mid-match. Capture the enemy flag to stop their respawns. Its virality is credited to
TikTok, where clips reached millions of views.

**Vampire Survivors.** Solo, 30-minute runs. The character attacks automatically; the player only
moves. Gems from kills level you up; each level-up offers a random choice of weapons and passives,
and weapon pairs evolve into stronger forms. Gold buys permanent stat upgrades between runs, and
many characters and stages unlock by hidden conditions. Chests open with a slot-machine-style
animation. (From general knowledge of the game, not from a fetched source.)

## Why each spread, as the sources say it

| Game | Reason given | Source |
|---|---|---|
| Lethal Company | Social media; "more chaotic and deadly" than Phasmophobia; proximity voice | Wikipedia |
| R.E.P.O. | Lethal Company-style loop with physics, low price, 6-player chaos, verbal coordination | Wikipedia |
| Content Warning | Free for 24 hours; the game's own output is a video; April Fools' launch | Wikipedia |
| PEAK | Co-op chaos and humour suit streaming; daily map; badges; low price | Wikipedia |
| Schedule I | Twitch and TikTok; dark humour on cosy management; mods | Wikipedia |
| Balatro | Strategic depth, "remarkably approachable", joker combinations | Wikipedia |
| Buckshot Roulette | Twitch and TikTok, then YouTube; item strategy and replay value | Wikipedia |
| Phasmophobia | Streamers in Oct 2020 (6th on Twitch that month); Halloween timing; shared clips | Wikipedia |
| Mage Arena | TikTok clips of shouted spells | Wikipedia |
| The genre ("friendslop") | Under $15; proximity chat turns panic into soundbites; about 15-second clips on TikTok, Shorts and Discord are "free, organic" marketing; low fidelity; small teams; jam-born games | Games Market, Dec 2025 |
