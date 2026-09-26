<!-- plain copy of: docs/4-systems/raid-scene-assembly.md @ 8bb0321c5497e739258535201316add5cb0900d5 -->

# Raid scene assembly

Full technical doc: [raid-scene-assembly.md](../../4-systems/raid-scene-assembly.md)

## What it is
How the raid scene gets filled with real modelled rooms, loot and enemies, instead of the
placeholder boxes and cubes it once used, and which hand-made lists it draws all of that from.

## Why it matters
Modelled art is only worth having if it actually appears in the game. For a long time the raid
scene quietly built itself from primitive shapes even though real models existed, so nobody
noticed the art was never wired in. Getting this wrong means players never see the art the team
made, or a raid that silently spawns nothing in a zone.

## How it works
1. Three hand-edited lists feed a raid: which rooms exist, which loot can appear, and which
   enemies can garrison the castle. Nothing here is generated at runtime.
2. A dev tool wires those three lists into the scene and refuses to run if any list is missing,
   rather than quietly falling back to placeholder shapes.
3. Choosing an age or historical period in the game's hub swaps in that period's own rooms, loot
   and enemies, built from the same three-list pattern.
4. Every enemy prefab is a variant of its model, not a copy, so re-exporting the model keeps the
   prefab up to date automatically.
5. Which enemy actually appears is picked using its own random stream, so the choice of enemy
   never shifts the castle's layout or where loot ends up.
6. The castle's walkable map is rebuilt fresh for each generated castle, always after the rooms
   exist and before any guard spawns onto it.
7. Models arrive from a 3D tool using a different "up" direction than Unity, so each family of
   models (rooms, loot, enemies) carries its own fixed correction, applied consistently wherever
   it is placed.
8. Players now go from a main menu into a hub, choose an age, set out, then return to the hub
   afterwards, rather than starting straight into a raid.

## Risks and safeguards
- **The raid quietly falling back to placeholder art.** The dev tool that assembles a raid aborts
  and names what is missing instead of substituting a primitive shape.
- **An enemy prefab losing its link to its model.** Enemy prefabs are always variants of their
  model, never independent copies.
- **A whole zone of the castle spawning no enemies or loot at all.** Every zone is checked to have
  at least one enemy and one loot entry; a gap for the current age falls back loudly, with a
  logged warning, rather than leaving the zone empty.
- **Guards spawning before the walkable map exists.** The map is always rebuilt between the
  rooms appearing and the guards spawning; a guard spawned too early stands frozen for the whole
  raid.
- **A model importing upside down or on its side.** Each family of models applies its own axis
  correction consistently, and spawning code always adds to a model's own rotation rather than
  replacing it.
- **Loot spawning inside a wall or piece of furniture and getting flung out of the world.** Loot
  placement only knows a room's centre, not its real shape, so a small share of pieces per raid
  can still be ejected. This is a known, unfixed defect that real modelled rooms made visible.
- **A generator tool and a hand-edited scene fighting over the same file.** The dev tool now
  writes only to its own disposable copy; the real scene is edited and saved by hand.

## Related
- [Net](net.md)
- [Scale](../../4-systems/scale.md) *(no plain copy yet)*
- [Raid](../../4-systems/raid.md) *(no plain copy yet)*

## Left out
File and class names, exact prefab paths, the full per-age enemy and loot tables, import settings
for models and textures, and the manual verification steps and their results.
