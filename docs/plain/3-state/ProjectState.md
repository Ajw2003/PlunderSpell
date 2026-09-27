<!-- plain copy of: docs/3-state/ProjectState.md @ 132af4783d0eaef4aa44f3aae7cde972de750f3a -->

# Project state

Full technical doc: [ProjectState.md](../../3-state/ProjectState.md)

## What it is
A running record of where the project actually stands against its roadmap, kept separate from
the roadmap itself, which only describes what "done" means.

## Why it matters
Code compiling and passing automated tests is not the same as a game that plays well. This
record exists to track that gap honestly, milestone by milestone, instead of assuming finished
code means a finished feature.

## How it works
1. The project sits at roughly two fifths of the way through its roadmap. It read two thirds
   until the roadmap grew from four milestones to eight, adding a market, livelier enemies, a
   dangerous castle and a final art pass. Only the first milestone has actually been checked the
   way its own finish line defines "done".
2. A real play session found the raid itself broken in ways the automated tests had missed:
   casting not working, no way to tell if someone was taking damage, and escaping not working.
   That became the top-priority work before anything else.
3. The raid now builds from real modelled rooms, loot and enemies instead of placeholder shapes,
   and choosing a historical age changes which rooms, loot and enemies a raid uses.
4. All planned loot and enemies across every age now exist as finished, textured models. None of
   the enemies are animated yet.
5. A backlog from an early playtest, and a second audit against the game's own pitch document,
   both turned up rough edges: loot flung around at spawn, missing music and effects, and one of
   the pitch's four core ideas, a market to spend takings in, not built at all yet.
6. A repeating bug pattern, treating an offline single-player game as a network client rather
   than as its own authority, has caused several separate silent failures across different
   systems, and remains a risk for any new networked feature.
7. Voice casting, co-op over Steam, and carrying weighted loot have each been confirmed working
   for a real person, though several of their edge cases, like recognising different voices or
   two separate Steam accounts joining each other, are still unverified.

## Risks and safeguards
- **Code passing tests while the actual game is broken.** A milestone's automated tests passing
  is tracked separately from whether a real person has actually played it and confirmed it works.
- **A known bug pattern reappearing in new code.** Every place that checks who is in charge of a
  networked object is documented so a new feature does not repeat the same offline mistake.
- **Loot ejected out of the world at spawn.** Documented as a known, unfixed defect rather than
  silently accepted; an attempted fix that made it worse was reverted.
- **A core pitch idea being quietly dropped rather than decided on.** The missing market feature
  and the mismatch between some enemies and the pitch's antagonists are flagged as open questions
  needing a decision, not bugs to just fix.
- **No standalone build ever having been produced or checked.** Called out explicitly so it is
  tracked rather than assumed to work.

## Related
- [Raid scene assembly](../4-systems/raid-scene-assembly.md)
- [Atmosphere](../4-systems/atmosphere.md)
- [Spells](../4-systems/spells.md)
- [Alarm and Acoustics](../4-systems/alarm.md)
- [Damage](../4-systems/damage.md)
- [Voice](../4-systems/voice.md)
- [Raid](../4-systems/raid.md)
- [Castle](../4-systems/castle.md)
- [Net](../4-systems/net.md)
- [Scale](../4-systems/scale.md)
- [Roadmap](../../2-roadmap/Roadmap.md)

## Left out
Dated change-by-change history, exact issue numbers, specific test and file names, and the
detailed per-age breakdown of what art exists.
