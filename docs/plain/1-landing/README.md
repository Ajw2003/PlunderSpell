<!-- plain copy of: docs/1-landing/README.md @ b88354fd3825ab994724fbf13789ebfe9fba9bcb -->

# Plunderspell: docs

Full technical doc: [README.md](../../1-landing/README.md)

## What it is
The starting page for the project's documentation: what the game is, where everything in the
repository lives, and a map of every other document worth reading.

## Why it matters
A project this size needs one place that points at everything else, or people waste time
searching for documents that already exist. This page exists so nobody has to guess.

## How it works
1. Plunderspell is a four-player co-op heist game: raid a randomly built castle, speak spells
   aloud, carry out what you can, and escape before the alarm catches up.
2. Documentation sits in numbered tiers: what the game is, what "done" means, where the project
   stands now, how each system works, today's work, and why past decisions were made.
3. A dozen system documents, one per runtime-critical part of the game, sit under their own tier
   and are all linked from this page.
4. Plans, old documents kept for the record, and tool-generated files each get their own folder.
5. This plain-English folder mirrors those documents for readers who want the gist, not code.
6. A few conventions keep the documentation honest: a milestone counts as done only once it is
   actually checked, old documents move rather than get deleted, and links are checked after
   anything moves.

## Risks and safeguards
- **Something existing with no link to it from here.** That counts as a gap in this page, not a
  reason to go searching the repository.
- **A milestone called done because the code exists, not because it was checked.** Only a checked
  result counts; the gap between the two is tracked separately.
- **A moved document leaving broken links behind.** Links are checked with a script after any
  move, and an old document goes to an archive folder rather than being deleted.

## Related
- [Roadmap](../../2-roadmap/Roadmap.md) *(no plain copy yet)*
- [Project state](../../3-state/ProjectState.md) *(no plain copy yet)*
- [Core](../4-systems/core.md)
- [Net](../4-systems/net.md)
- [Voice](../4-systems/voice.md)
- [Spells](../4-systems/spells.md)
- [Castle](../4-systems/castle.md)
- [Alarm and Acoustics](../4-systems/alarm.md)
- [Raid](../4-systems/raid.md)
- [Raid scene assembly](../4-systems/raid-scene-assembly.md)
- [Enemy asset pipeline](../4-systems/enemy-asset-pipeline.md)
- [Damage](../4-systems/damage.md)
- [Combat bench](../4-systems/combat-bench.md)
- [Scale](../4-systems/scale.md)

## Left out
The full folder map, every plan and archive document listed, and the exact wording of each
documentation convention.
