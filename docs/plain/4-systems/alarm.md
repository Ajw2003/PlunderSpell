<!-- plain copy of: docs/4-systems/alarm.md @ 196aefc41080e0ce6ca4bea2b95c3d7e6f34724a -->

# Alarm and Acoustics

Full technical doc: [alarm.md](../../4-systems/alarm.md)

## What it is
Acoustics carries a noise to whoever might hear it. Alarm turns those noises into one shared,
castle-wide alert level.

## Why it matters
Without this, guards would either miss trouble or notice everything. Once fully raised, the
alarm must stay raised, or players could hide it out and lose the tension of the raid.

## How it works
1. A noisy event, like a footstep or a shout, broadcasts from its position.
2. Each wall cuts the sound's strength in half; a sound too weak by the time it arrives is
   dropped.
3. Anyone still in range adds that strength to the castle's one shared alert level.
4. The level maps to a state: Calm, Stirred, Roused, or Hue and Cry, full alert.
5. Once Roused is reached, the state cannot drop again, even if the level falls. Only a new raid
   resets it.
6. Guards chasing or attacking a player report straight to the alarm, so several guards fighting
   at once can raise it unaided.
7. A guard who spots you shouts, and every guard near it heads to where you were seen. When the
   alarm reaches its highest state, every guard within 40 metres of a player heads for them.

## Risks and safeguards
- **Alarm creeping up from the last raid.** Every new raid resets it, with a grace period first.
- **Sound passing through walls unheeded.** A setting must be configured correctly. *Still
  open:* nothing checks that it is.
- **Alarm misbehaving offline.** Its core logic has no networking, so it works alone.

## Related
- [Castle](../../4-systems/castle.md) *(no plain copy yet)*
- [Raid](raid.md)

## Left out
Exact numbers, the wall-counting method, and how state syncs between players.
