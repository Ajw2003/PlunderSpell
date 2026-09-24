# Systems

One document per runtime-critical system: what it owns, how it works, what must stay true, and
what has already cost someone a day. Start with `core.md`, then `raid.md` — it's the loop
everything else feeds.

| System | Owns |
|---|---|
| [`core.md`](core.md) | Singleton base, event bus, state machine contract — everything else sits on this |
| [`net.md`](net.md) | Steam invite plumbing (cold launch, rich presence) alongside PurrLobby |
| [`voice.md`](voice.md) | Turning held-key + microphone audio into a recognised phrase |
| [`spells.md`](spells.md) | Resolving a phrase into a spell, a misfire, or a fizzle |
| [`castle.md`](castle.md) | Deterministic seed-driven castle layout, path validation, seed replication |
| [`alarm.md`](alarm.md) | Noise propagation and the castle-wide alarm state machine |
| [`raid.md`](raid.md) | The loop itself: Lair → castle → haul → extraction → Lair, plus the guards, HUD, status effects and playtest harness that serve it |
| [`raid-scene-assembly.md`](raid-scene-assembly.md) | Wiring the authored castle/loot/enemy art into the playable scene: the three catalogue ScriptableObjects, NavMesh timing, Blender-to-Unity orientation, and the menu → lair → raid flow |
| [`enemy-asset-pipeline.md`](enemy-asset-pipeline.md) | Generating the enemy roster's meshes/rigs/textures from Python |
| [`Tools/ArtForge/README.md`](../../Tools/ArtForge/README.md) | Turning the art bible (`docs/art/`) into game models on top of EnemyForge: blueprint API, part kinds, validation, review sheets, traps. Tool-level doc, kept beside the code; all 20 plunder items built, no structures or enemies |
| [`damage.md`](damage.md) | The one damage pathway, who gets blamed, and the feedback that makes every hit readable |
| [`combat-bench.md`](combat-bench.md) | The one-room arena for trying a weapon, spell or enemy without starting a raid |
| [`scale.md`](scale.md) | The metre: the 1.8m standard human, per-zone room and archway heights, enemy sizes, and where the player spawns |

## Considered and folded into another doc

- **Guards, UI (`RaidHud`), Status, Playtest** — each is its own assembly, but none has an
  invariant or trap that doesn't belong to the raid loop they exist to serve. `raid.md` documents
  them under its own "Assemblies" line rather than splitting four thin docs off a loop that only
  makes sense read together.
- **Loot, Extraction** — same reasoning: `LootPlacementPlanner`/`GuardPlacementPlanner` and
  `ExtractionZone` are covered by `raid.md`'s invariants (loot worth, the extraction room's no-loot
  rule) because their behaviour only matters in terms of the loop, not standalone.
- **Lair, Inventory** — the debt/haul-carry economy around a raid. Thin enough (two small
  assemblies, no independent invariant beyond "what came back gets banked") that it rides along
  with `raid.md` rather than getting its own page.

## Considered and left out entirely

- **Player, Enemies** — generic Rigidbody/FSM movement and combat infrastructure ported from the
  predecessor project (`RogueLikeSlop@ThirdPerson`), the same shape and vintage as `Core`. Left
  out because it has no Plunderspell-specific design decision to record beyond what `core.md`
  already says about the state-machine contract it's built on — a system doc here would mostly
  restate the code, which is exactly what this tier is supposed to avoid.
