<!-- plain copy of: docs/4-systems/raid.md @ 53bbf36e7930f686ae67dc8f70da6c8b54afc3a3 -->

# Raid

Full technical doc: [raid.md](../../4-systems/raid.md)

## What it is
The game's main loop: leave the safe hub, raid the castle, carry out what you can, escape, and
bank what you brought against the debt. Every other system exists to serve this loop.

## Why it matters
This is the game. If the loop breaks, nothing else in the project matters: players cannot start
a raid, cannot tell what they are carrying is worth, or cannot get out with it.

## How it works
1. One starting number, the seed, drives the whole raid: the castle's layout, where loot goes,
   and where guards are posted, each from its own independent stream so changing one never shifts
   another.
2. The scene is built from hand-made rooms, loot and enemies, described in the raid scene
   assembly doc.
3. Loot and guard placement are worked out as plain calculations with no scene involved, which
   makes their rules easy to check automatically rather than just eyeballed.
4. Newly spawned loot is held still for a moment before physics takes over, so a piece that
   spawned slightly inside a wall settles gently instead of being flung across the room.
5. Players arrive and leave through a portal inside the castle rather than a gate; anyone still
   outside it when the clock runs out is left behind, along with whatever they were carrying.
6. Standing on the escape point starts a short countdown; finishing it, or the raid's clock
   running out, banks whatever loot is there.
7. A guard's attack is limited to firing every so often, not every frame in contact, and guards
   give newly arrived players a short grace period before they can be spotted.

## Risks and safeguards
- **Players trapped in an unwalkable castle.** A castle is only accepted once it is confirmed
  walkable from start to exit; if not, the game tries again with the next seed.
- **Guards ambushing players the instant they arrive.** No guard is posted or patrols near the
  arrival point, and a new raid gives a short grace period before guards notice anyone.
- **A guard's contact attack acting like an instant, unfair kill.** Every attack has a cooldown
  so it cannot fire again immediately.
- **Loot being worth nothing, or double-counted.** Value always comes from one dedicated place on
  each loot piece, attached automatically to everything that spawns, never guessed from the
  object itself.
- **The escape point still holding a finished raid's result.** It is reset at the very start of
  every new raid.
- **The exit room being an easy free reward or a guaranteed fight.** It is kept free of both loot
  and guards on purpose.
- **A feature working online but silently doing nothing offline, or the reverse.** Every check for
  who is in charge of a piece of game logic treats an offline game as its own authority, not as
  neither the host nor a guest.

## Related
- [Raid scene assembly](raid-scene-assembly.md)

## Left out
File and class names, exact numbers (ranges, cooldowns, timers, item counts), the older loot
system being phased out, and the automated tests behind each safeguard.
