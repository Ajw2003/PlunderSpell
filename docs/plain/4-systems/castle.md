<!-- plain copy of: docs/4-systems/castle.md @ ed02e7fb81ee12b3496e4c2119ab1d4be61f70f8 -->

# Castle

Full technical doc: [castle.md](../../4-systems/castle.md)

## What it is
The system that builds the fortress a raid happens in, from one starting number called a seed,
so every player's game builds the exact same building.

## Why it matters
If two players' castles came out different, the game could not agree on a shared floor plan
during a raid. If a castle came out unwalkable, players could be trapped inside with no way to
the exit.

## How it works
1. A single seed drives every decision, so the same seed always builds the same castle.
2. The generator raises a closed outer wall with one gatehouse, then fills the inside with rooms,
   leaving some open as courtyards.
3. Courtyards are removed one at a time, but only if the rest of the castle can still be walked
   as one connected space.
4. Every room has a doorway on all four sides so neighbouring rooms always connect; any doorway
   left facing nothing gets sealed shut.
5. A pathfinding check walks from the start to the exit once the castle is built. If a layout
   fails, the game tries again with the next seed rather than a fresh random one.
6. Only that seed travels across the network. Every player's game builds its own copy locally
   from it, so nothing about the castle's shape itself needs to be sent.
7. As the alarm escalates, doors lock and then bar shut, and never unlock again during that raid.
8. The open strip just inside the outer wall has doorways into the castle every so often, on every
   side, and a team that arrives on that strip always arrives in front of one, facing it.

## Risks and safeguards
- **Players unable to reach the exit.** A layout is only accepted once a pathfinding check
  confirms it can be walked start to end.
- **Players on different machines building different castles.** Only the seed is sent, and
  building from it must never depend on anything random or on timing, or players quietly drift
  out of sync with no error shown.
- **A room or wall piece drifting into its neighbour's space.** Every piece must fit its grid
  square exactly, checked when the art is built.
- **The previous castle's leftovers blocking the new one.** Old pieces are fully cleared before
  the next castle is built in the same moment, or navigation and doors from the last raid can seal
  off the new one.

## Related
- [Raid](raid.md)
- [Alarm and Acoustics](alarm.md)
- [Scale](scale.md)

## Left out
Exact sizes and counts, file and class names, the loot-anchor and stair rules, and the automated
tests that guard each invariant.
