<!-- plain copy of: docs/4-systems/atmosphere.md @ 5c6ebf85b97ee569463ce704836a2e4832fdf9f0 -->

# Atmosphere: the castle at night

Full technical doc: [atmosphere.md](../../4-systems/atmosphere.md)

## What it is
The castle's night look: fire in fog, a faint moon, and lighting that brightens and reddens as
the alarm rises.

## Why it matters
It is a main selling point, and the players' cue for how much danger they are in.

## How it works
1. Colours shift together whenever the alarm's state changes.
2. Fog covers the screen, lit by the nearest fires.
3. Fires are placed by the castle builders; quality settings set how many cast shadows.
4. Surfaces pick up detail and firelight.
5. The portal dims and gutters near the end, capped against fast flashing.

## Risks and safeguards
- **Portal flashing too fast.** A test checks it stays under three flashes a second.
- **Colours reading too dark.** They must be entered in the right format.
- **Editing code mid-raid.** This has crashed the editor before, so rebuild tools leave play mode
  first.

## Related
- [Alarm and Acoustics](alarm.md)
- [Castle](../../4-systems/castle.md) *(no plain copy yet)*

## Left out
File names, exact numbers, and rebuild commands.
