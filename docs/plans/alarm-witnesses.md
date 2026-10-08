# The alarm needs witnesses (#259, 2026-10-04)

Owner's playtest: one sighting and one successful Somnus took the castle from Calm to Hue and Cry. Owner's
direction: keep the triggers and the guards' senses as they are; make the castle need more sightings and more
noise heard by more guards before it changes state. Discovery should spread the way it would in the fiction:
a guard who spots you cries for help, guards within earshot who hear the cry come over, and each one who then
sees you cries out in turn, a chain reaction once you are discovered. A castle-wide lockdown should follow only
from plausible circumstances.

## Today (`Runtime/Alarm/DirectorAlarm.cs`)

- A guard's first sighting adds 20 points (`_sightingPoints`); Stirred is 20, Roused 50, Hue and Cry 80.
- Guards chasing at once force a floor: 2 chasers force Roused (lockdown), 3 force Hue and Cry
  (`_rousedChasers`, `_hueAndCryChasers`).
- Every noise adds strength × 15 (`_noiseWeight`) through the director's own castle-wide trigger, whether or
  not any guard could hear it. A whispered spell's cast and effect noise count in full.

## Change

1. **A guard cries out when it first spots an intruder.** The cry is a sound through the normal acoustic path
   (`NoiseBroadcaster`, walls muffle it), of a new `NoiseType.GuardCry`, about 18 m. A guard that hears a cry
   goes to where the crier saw the intruder, the same way it follows any lead. If it then sees the intruder,
   that is its own first sighting, and it cries out too.
2. **The castle hears only what its guards hear.** The director stops scoring noise from its castle-wide
   trigger. A guard that hears an intruder's noise reports it; the noise scores once per guard that heard it
   (a guard reports at most once every 2 s). A guard's cry scores nothing: it is the guards talking, and its
   effect is the guards it brings.
3. **States need witnesses.** The alarm counts the distinct guards that have seen an intruder this raid.
   - Stirred: 20 points, unchanged (one guard is uneasy).
   - Roused, the lockdown: 50 points and at least 3 guards that have seen an intruder.
   - Hue and Cry: 80 points and at least 5 guards that have seen an intruder.
   - Chasers that force a state: 3 for Roused (was 2), 5 for Hue and Cry (was 3).
   Points keep accumulating while a gate is unmet; the state waits for the witnesses.

What a raid feels like then: one guard sees you and you put it to sleep before anyone comes: Stirred at
most. A guard sees you, cries, two more come and see you: lockdown. A running fight that draws five guards:
Hue and Cry.

## Unchanged

Guards' sight and hearing ranges, what they do on a sighting, the hue and cry's behaviour once raised, the
lockdown's door rules, and the latch (Roused and above never decay).
