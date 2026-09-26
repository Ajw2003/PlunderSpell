# Raid

The game loop: Lair → castle → haul → extraction → Lair, with the takings applied to the debt.
Everything else in the project is a system; `RogueAi.Raid` is what makes them a game.

Assemblies: `RogueAi.Raid` (loop, spawning), `RogueAi.Guards` (the garrison), `RogueAi.UI` (HUD),
`RogueAi.Status` (spell-inflicted conditions), `RogueAi.Playtest` (a body to walk around in).

## How it works

- **`RaidDirector`** owns the loop and the phase (`InLair` / `Generating` / `Raiding` /
  `Extracting` / `Resolved`). `StartRaid(era)` re-arms the extraction zone, rolls a seed, builds a
  castle, plans and spawns the haul and the garrison, resets the alarm and starts the clock.
  `CallExtraction()` ends it early; the zone's tally comes back through `ExtractionResolved`, which
  banks the worth against the debt.

- **One seed drives the whole raid.** It is chosen on the server, replicated by
  `CastleNetworkManager`, and feeds the castle layout, the loot plan and the garrison — three
  independent RNG streams derived from it, so changing one cannot shift the others. Four peers build
  an identical raid without a byte of layout or loot data crossing the wire.

- **`LootPlacementPlanner` / `GuardPlacementPlanner`** are pure functions of (layout, table, seed).
  No scene, no components, no time — which is what makes the placement *rules* assertable rather
  than eyeballed. `LootSpawner` / `GuardSpawner` are the halves that touch the scene, and only the
  server runs them.

- **`RaidBootstrapper`** starts a raid when the scene runs and gives a playtester the two controls
  nothing else provides (F5 extract, F6 go again). Thin on purpose: every decision belongs to the
  director; this only decides when to ask.

- **`CastleGuard`** hears (`INoiseListener`), sees (cone + line-of-sight raycast) and can be shut
  down by Somnus, Tonitrus and Ignis through `StatusEffectReceiver`. Every decision it makes is
  delegated to **`GuardBrain`**, which is pure.

- **`RaidHudPresenter`** gathers the raid into a plain `RaidHudModel`; `RaidHudView` draws it with
  IMGUI. What the player is *told* is logic and is tested; how it is drawn is not.

## Invariants

- **A raid never starts in a castle you cannot walk out of.** `RaidDirector.GenerateWalkable`
  validates crypt→exit reachability and walks the seed forward (`seed + 1`, not a fresh random
  number) until one passes, so the seed it reports is the seed it replicates.

- **The extraction room holds no loot and no guards.** Free treasure at the exit would delete the
  carry, and a guard standing on it would turn every raid into the same fight.

- **The crypt final chamber always holds the richest entry in its zone.** There must always be a
  reason to go all the way in.

- **The zone is re-armed on every `StartRaid`.** It carries the previous raid's result until then;
  a raid that starts against a completed zone cannot be left.

- **Broken loot is worth nothing, and loot outside the zone is worth nothing.** Both are what make
  the carry the game.

## Traps

- **Offline is not "client".** PurrNet's `isServer` is false on an *unspawned* object as well as on
  a client, so `if (!isServer) return;` silently disables a system in single-player. Every authority
  check in this project reads `if (isSpawned && !isServer) return;` — an unspawned object is its own
  authority. This bit the extraction clock (never counted down), trigger tracking, and voice casting
  (`OnSpawned` never fires offline, so nothing ever subscribed and the entire game did nothing).

- **`[RequireComponent]` runs the dependency's `Awake` first.** A component added to satisfy a
  requirement wakes up *before* the component that required it exists, so resolving a sibling in
  `Awake` finds nothing. `StatusEffectReceiver` resolves its `IHealth` lazily for exactly this
  reason; guards silently took no burn damage until it did.

- **A conjured `LootItem` must be an instance, not the shared asset.** Aurum Voco's worth varies
  with cast volume, and writing it onto the template rewrites every other pile in the raid.
