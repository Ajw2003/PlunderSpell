<!-- plain copy of: docs/4-systems/enemy-asset-pipeline.md @ b830869c6f50c6c9ae9fe3698a45ae740d8b08a9 -->

# Enemy asset pipeline (EnemyForge)

Full technical doc: [enemy-asset-pipeline.md](../../4-systems/enemy-asset-pipeline.md)

## What it is
A code pipeline that builds the whole enemy roster, mesh, skeleton, textures and exported
files, from a script. Nobody hand-models an enemy.

## Why it matters
Hand-modelling five enemy types would be slow and hard to keep consistent. Building them from
code means every enemy follows the same rules automatically, and a mistake can be caught before
it ever reaches the game.

## How it works
1. Each enemy is a flat list of simple shapes, each kept separate so surfaces meet cleanly.
2. Each surface type, like stone or gold, is baked into one shared material, so a review render
   shows exactly what the game loads.
3. The mesh is smoothed, laid out and rigged: every point moves with exactly one bone, since
   these enemies are rigid, not bendy.
4. The model exports in formats Unity can read, alongside its source file.
5. A checking step runs before export and stops the build if anything would break the game.

## Risks and safeguards
- **A model built the wrong way round or off its ground spot.** Checked before export: centred,
  facing the right way, untouched by leftover position or rotation.
- **A shape stealing another surface's material by mistake.** Checked against what it was built
  with.
- **A flying enemy touching the ground, or a grounded one floating.** Checked separately.
- **A model too detailed to render smoothly.** Each type has a triangle budget; going over fails
  the build.
- **Glow effects looking dim in the finished game.** The bake stores brightness in a limited
  range, so the game's material scales it back up.

## Related
None. This doc does not point to another system doc.

## Left out
File and script names, exact measurements and triangle counts, the specific software bugs this
pipeline works around, and how the rendering environment is set up.
