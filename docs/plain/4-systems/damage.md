<!-- plain copy of: docs/4-systems/damage.md @ 5f4dd59271858e482ca906e811b61c530b94cc55 -->

# Damage

Full technical doc: [damage.md](../../4-systems/damage.md)

## What it is
The one path anything in the game uses to hurt a player, guard or enemy, plus the on-screen
feedback that shows the player what just happened. It also covers carrying and towing loot,
since weight and impact both feed into damage.

## Why it matters
If damage went through more than one path, some hits would land invisibly, with no number, flash
or blame shown, and players would not trust what they saw happening to them. Carrying loot also
has to feel physical, or heavy items would not seem worth the risk of hauling them out.

## How it works
1. Every source of harm, weapons, spells, guards, fire, thrown objects, goes through the same
   call, which only reports a hit if health actually dropped.
2. Each hit remembers both what physically struck and who is to blame, so a thrown item still
   blames its thrower for a few seconds after leaving their hand.
3. A hit shows as a rising number coloured by who dealt it, a red flash on what was struck, a
   short health bar, and a hit marker on the crosshair.
4. Getting hurt yourself reddens the screen edge and names what hit you, merging repeated hits
   from the same source into one growing number.
5. Dying ends the raid without banking anything, dropping the player back at the safe hub; going
   out again starts fresh at full health.
6. A held item hangs from where it was grabbed on a springy pull, keeping its own orientation as
   the holder turns.
7. Items too heavy to lift are towed behind the holder on an invisible rope instead, slowing the
   holder's walk and trailing behind them realistically.
8. Every sword can be swung. A hit does a set amount plus more for a heavier blade, so three or
   four hits bring down a guard; heavier swords hit harder but swing slower.
9. The view shakes a little when you are hit, when you land a hit, when someone casts a spell
   near you (a shout more than a whisper), and when your leap spell slams into the ground. Only
   your own view moves; time never pauses, because every player shares the same raid.

## Risks and safeguards
- **A hit bypassing the shared path.** Nothing else in the game is allowed to apply damage
  directly, so any new attack must be wired through the one call or it deals silent, unreadable
  damage.
- **Loot barely settling on the floor dealing damage.** Impacts below a minimum speed are
  ignored, unless the player is actively holding or just threw the item.
- **Walking into loot double-counting the player's own speed as impact damage.** Only the item's
  own speed going into the hit counts, not the player's.
- **Fragile loot shattering from being bumped rather than struck.** Breakage is judged by the
  speed straight into the point of contact, not the raw closing speed, so an item sliding along
  the floor is not re-broken at every corner.
- **A carried item swinging into someone.** This still counts as a real hit, scaled by the item's
  speed and heft, and blamed on whoever was carrying it.

## Related
None. This doc does not point to another system doc directly, though it shares the damage-taking
bodies with the raid loop and the combat bench.

## Left out
File and class names, exact numbers (speeds, forces, weights, cooldowns), the beam-and-rope
carry math, and the automated tests behind each safeguard.
