<!-- plain copy of: docs/4-systems/scale.md @ 438e384464821ed7e7302bb278ce5cfb0a2317f9 -->

# Scale

Full technical doc: [scale.md](../../4-systems/scale.md)

## What it is
The one measurement every room, doorway, player and enemy in the game is sized against: a
standard human figure, and everything else described as a multiple of it.

## Why it matters
Without one shared scale, rooms would feel like doll's houses or cathedrals, enemies would poke
through ceilings, and a boss could spawn somewhere it can never fit through.

## How it works
1. A standard human stands 1.80 m tall, and every other size in the game is described relative
   to that figure, never as its own independent number.
2. Rooms grow taller from the castle's centre outward, so the innermost room is the tightest and
   the outer walls are the grandest.
3. Every doorway is sized from its room's height, always wide enough for two people and tall
   enough to clear a standard human by some margin.
4. Each enemy has its own standing height, scaled to fit the shortest room it is ever posted to,
   so nothing clips through a ceiling it is meant to stand in.
5. Automated tests measure the real models and rooms rather than trusting the numbers on paper,
   so a mismatch between the art and this document fails a test instead of shipping quietly.
6. Where a player or enemy spawns is worked out the same way everywhere: step in from the
   entrance, then search outward for the first clear, human-sized spot with solid floor beneath
   it.

## Risks and safeguards
- **An enemy too tall for the room it is meant to stand in.** No enemy is allowed to be taller
  than the shortest room it is posted to, checked automatically against the real model.
- **A rigged model's measured height being wrong.** A rigged character's stored size box is
  padded and not trustworthy; height is instead measured from its actual points, the same way
  everywhere that needs it.
- **A player or enemy landing standing on nothing, over a gap.** A candidate spawn spot is only
  used if it also has solid floor beneath it, not just clear space around it.
- **A spawn check running against stale positions.** Object positions are always refreshed
  immediately before searching for a clear spot.
- **A boss that fits in its room but not through its doorway.** This is treated as a deliberate
  design choice, a boss that fights where it spawns, not a bug, and any change to one side needs
  a matching change to the other.

## Related
- [Castle](castle.md)
- [Raid scene assembly](raid-scene-assembly.md)

## Left out
Exact heights, widths and margins for every room, enemy and archway, the rigged-model
measurement method, and the automated tests and tools behind each safeguard.
