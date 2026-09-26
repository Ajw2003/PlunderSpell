<!-- plain copy of: docs/4-systems/combat-bench.md @ cbb360134696257d6737777a5bb06b6e0af10474 -->

# Combat bench

Full technical doc: [combat-bench.md](../../4-systems/combat-bench.md)

## What it is
A small one-room arena for trying a weapon, a spell or an enemy in a few seconds, instead of
starting a full raid and walking a castle until one turns up.

## Why it matters
Testing combat only inside a real raid is slow and unreliable, since enemies and loot might not
even appear. The bench gives a fast, repeatable way to check that combat still works.

## How it works
1. The bench opens a small walled box with the player in the middle and a sword nearby.
2. A panel lets a tester pick an enemy, how many to spawn, and the starting alert level.
3. Chosen enemies spawn in a ring around the player, already facing inward.
4. The bench reuses the raid's own player controls, spell casting and enemy list, so what works
   here works there too.
5. Guards on the bench walk in straight lines, since it has no prebuilt walkable map.
6. The scene rebuilds from scratch each time, so nothing it needs can be left out by accident.

## Risks and safeguards
- **The bench leaking into a real raid.** It lives in its own area of the code, with nothing in
  the raid system referring back to it.
- **Clearing the bench removing things it did not spawn.** Only what it spawned is tracked.
- **The bench and the raid drifting apart.** Both share the same enemy list.
- **A screenshot tool missing the on-screen panel.** A known limit, not a bug.

## Related
- [Raid](raid.md)

## Left out
File and script names, exact sizes and counts, the automated tests, and outstanding requests
like a main menu button and a ranged weapon for the bench.
