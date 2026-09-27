<!-- plain copy of: docs/4-systems/spells.md @ e3ab8462c4df92445a4daae3bcb20fa2039a087a -->

# Spells

Full technical doc: [spells.md](../../4-systems/spells.md)

## What it is
The system that turns a spoken or typed word into a spell, a misfire, or nothing at all. Say it
right and the spell happens; say it nearly right and something worse happens instead.

## Why it matters
Speaking spells is one of the game's core ideas. If casting felt unreliable, invisible, or
unfair, that idea would fail. Players need to trust that what they said, and how loudly, is what
they get back.

## How it works
1. A spoken or typed phrase is matched against the game's spell words. An exact match casts that
   spell; a near miss casts a worse, garbled version of it; nothing close does nothing at all.
2. How loudly a word is said scales both its strength and how much noise it makes, so a whisper
   is weak but quiet, and a shout is strong but wakes the castle.
3. Every cast plays a visible effect for everyone in the raid, not just the caster, kept
   separate from the actual game logic so the two cannot fall out of sync.
4. A spell aims from where the caster is actually looking, not just from their body's facing
   direction.
5. Single-target spells pick whatever is closest to the crosshair within a cone and a range;
   area spells burst wherever the crosshair is pointed, not at the caster's feet.
6. Every spell costs mana from a shared pool that refills over time; a word the pool cannot
   cover is refused outright, with nothing cast and nothing spent.
7. Casting by keyboard chants for a short moment before firing, so it is a slight disadvantage
   compared to speaking, never the fastest way to cast.
8. Two spells move the caster: one is a quick dash, the game's only dodge, and the other is a
   high jump that, with a second press of jump in mid-air, turns into a slam that hurts and
   knocks back everything around the landing. Moving the caster happens on their own machine;
   the damage a slam does is still worked out centrally.
8. Two spells move the caster: one is a quick dash, the game's only dodge, and the other is a
   high jump that, with a second press of jump in mid-air, turns into a slam that hurts and
   knocks back everything around the landing. Moving the caster happens on their own machine;
   the damage a slam does is still worked out centrally.

## Risks and safeguards
- **A spell hitting its own caster, or a misfire missing them.** An intended spell always
  excludes the caster as a target; a misfire always targets them.
- **A cast happening with no sound, letting players sneak past the alarm for free.** Every spell,
  regardless of volume, always makes noise through the normal sound system first.
- **The same spell applying its effect more than once across different players' machines.** The
  effect itself only ever runs once, centrally; what plays on each player's screen is only the
  visual, never a second copy of the effect.
- **An incompletely set-up spell casting for free instead of misfiring.** A spell with no
  configured misfire falls back to a default misfire, never to the real spell.
- **All the game's tuning numbers being buried in code.** Every spell's cost, damage, duration
  and volume behaviour lives in one editable data file, not scattered constants.
- **A dodge that went nowhere.** The dash now keeps its speed for its whole length; it used to
  stop on its very first moment, before it had moved at all.
- **A visible spell effect swallowing the caster's own camera.** A burst effect is pushed forward
  from the caster so its near edge, not its centre, lands where the caster is looking.

## Related
None. This doc does not point to another system doc.

## Left out
File and class names, exact costs, ranges and durations, the two competing casting
implementations only one of which ships, and the known visual limitations still open.
