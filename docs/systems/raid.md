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

- **The scene the loop runs in is assembled from authored assets** — 25 castle room prefabs, 5 loot
  prefabs and 10 enemy prefabs, catalogued in three ScriptableObjects. See
  [raid-scene-assembly.md](raid-scene-assembly.md) for how the scene, the prefabs and the enemy
  roster are built and verified.

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

## How loot settles

`LootSpawner.SpawnFor` freezes every spawned rigidbody (`isKinematic = true`, velocities zeroed),
then starts a coroutine that waits `m_settleDelay` (0.5 s by default) and calls `ReleaseSpawned`.
A rigidbody that begins a frame overlapping a wall is depenetrated with enough impulse to throw it
out of the castle; holding it frozen until the rooms have stopped arriving means it only ever falls
the short distance to the floor.

`ReleaseSpawned` skips anything carried or broken — a carried item is kinematic on purpose, and
un-freezing it would drop it out of the carrier's hand socket. It is public so a test can settle the
haul without waiting the delay out in real time.

## Carrying and extracting

There were two pickup systems on the same objects. Loot prefabs carried **both** `LootPickup` (a
networked carry with two-person rules and fragility) and `Item` (the physics grab/carry/throw the
`PlayerStateMachine` drives through `ItemManager`). As of 2026-09-18 the `Item`/`ItemManager` path
is the live one.

### Leaving: stand on the pad

Added 2026-09-23 (#101). Before this a raid could only end when its clock ran out or on the
undocumented F5 key. Now a living player standing in the `ExtractionZone` runs an 8 s countdown
(`GameServices.Extraction`, the same `ExtractionController` the HUD's "Extracting — 3.5s (stay on
the pad)" bar is bound to); stepping off cancels it; finishing resolves the extraction with
whatever loot is on the pad, and the Lair shows "Last raid: brought home N coin". The countdown
cannot start in a raid's first 10 s, so a player who spawns beside the pad does not leave by
accident. F5 still works for playtesting.

The zone finds what is on it by **polling an overlap box four times a second**, not by trigger
enter/exit. Unity sends no trigger events between a kinematic body and a static trigger, and loot
is kinematic while it settles after spawning, so a piece already on the pad when it was released
never "entered". The overlap buffer grows when full: the pad sits among the gatehouse's wall and
floor colliders, and a fixed 128-slot buffer was silently dropping loot. Only an `IPlayerBody`
counts as a player saved — the old `NetworkIdentity` check counted guards too.

### Worth lives on `LootValue`

`Item` has no notion of value, so `LootValue` carries it: a small component beside `Item` on every
loot prefab. It prefers an authored `LootItem` asset when one is assigned and falls back to its own
`m_worth`, so the values already authored on the loot prefabs came across unchanged. Anything
without a `LootValue` extracts for nothing.

`LootSpawner.SpawnLoose` attaches one to everything it spawns — every path into the world funnels
through it, so there is one place this can be forgotten rather than five.

### The haul is visible while the raid runs

`ExtractionZone` tracks `LootValue` in its trigger and raises `HaulInZoneChanged(worth, pieces)` as
loot enters and leaves. `RaidHudPresenter` reads `WorthInZone`/`PiecesInZone` into the model and the
view draws it beside the debt — the number the debt is measured against. An empty pad reads
"bring loot to the pad" rather than "0 gold", because a zero looks like a broken counter.

### The transition bridge

`LootPickup` is still in the tree until the new path has been played. Two seams keep both honest
and **both are deleted with it**:

- `LootPickup.ApplyBrokenState` also calls `Ruin()` on a sibling `LootValue`. Without it a piece
  smashed through the old system still pays out.
- `ExtractionZone.TrackLoot(LootPickup)` is an overload that attaches the `LootValue` the zone now
  tallies. It exists so the seven test files still holding `LootPickup` keep asserting something
  during the transition rather than being rewritten twice.

## Guards that can actually hurt you

`CastleGuard` could see, hear, shout and chase, but had no attack of any kind — it would run at a
player forever and never land a blow. The attack code in the project lived on
`MonsterStateMachine`, a second enemy system that is not on any of the ten enemy prefabs.

`CastleGuard.TryAttack` is called from the `Chasing` branch of `Act`, gated on range and a
cooldown. The cooldown is load-bearing: without it a guard in contact damages the player every
frame, which reads as dying instantly for no visible reason. An incapacitated guard cannot attack,
which is what gives Somnus and Tonitrus their point.

### Melee by default, ranged when armed

One field decides which: leave `_projectilePrefab` empty and the guard strikes at `_attackRange`;
assign one and it fires from its **sight** range instead. That is what makes the `HexTurret` — spec'd
at patrol speed 0, "tracks and fires" — a turret rather than a guard that cannot walk.

Only the turret is armed. `Tools ▸ Plunderspell ▸ Arm The HexTurret` does it as a targeted prefab
edit, deliberately **not** through `EnemyPrefabForge`: the forge rebuilds all ten enemy prefabs and
would discard the hand-tuning they carry.

### One projectile

`Prefabs/Projectiles/Bolt.prefab` (built by `Tools ▸ Plunderspell ▸ Forge Projectile Prefab`) is the
only projectile in the game, fired by guards and spells alike. It carries `NetworkedProjectile` for
damage and lifetime, and `ProjectileTint` so one prefab can read as fire, frost or lightning without
a prefab per spell. Gravity is off — a bolt flies where it was aimed, rather than landing on the
floor between two people in a large room.

## Invariants

- **No guard is posted, or patrols, within two rooms of the entrance**
  (`GuardPlacementPlanner.SafeEntranceRadius`, and `BuildRoute` leaves those rooms out of every
  route). One room was not enough: a guard next door walked its route through the gate.
- **For the first 20 seconds of a raid a calm garrison sees nobody** (`CastleGuard.ArrivalGraceSeconds`,
  started by `RaidDirector.StartRaid`). Guards still hear, and a raised alarm ends the grace. A
  player standing still at the spawn was first hit at about 10 seconds before this and the wider
  ring, and at 37 seconds after. Tests that tick guards call `CastleGuard.EndArrivalGrace()` first,
  because the grace is process-wide.
  Players spawn just inside the extraction room; a guard next door saw them on the first frame and
  killed an idle player in ~18 s, which read as dying for no reason. The check runs after the
  density roll so the rest of the garrison stays seed-stable.
- **A guard's attack is gated on a cooldown.** Contact damage per frame is not a difficulty
  setting, it is an instant death with no readable cause.
- **Worth is tallied from `LootValue`, never from `Item` or `LootPickup`.** A piece with no
  `LootValue` is worth nothing, which is why the spawner attaches one unconditionally.
- **A raid never starts in a castle you cannot walk out of.** `RaidDirector.GenerateWalkable`
  validates crypt→exit reachability and walks the seed forward (`seed + 1`, not a fresh random
  number) until one passes, so the seed it reports is the seed it replicates.

- **The extraction room holds no loot and no guards.** Free treasure at the exit would delete the
  carry, and a guard standing on it would turn every raid into the same fight.

- **The crypt final chamber always holds the richest entry in its zone, and fills every loot anchor
  it has.** The richest goes on the room's first anchor, which the art puts at its centre. There
  must always be a reason to go all the way in.

- **A looted room holds several items, one per anchor** (2026-09-24). `LootPlacementPlanner` rolls
  the zone's density for whether a room has anything, then 1 to `RaidLootTable.MaxPerRoomFor(zone)`
  items (outer bailey 1, inner ward 2, keep 3), never more than the room's anchors and never two on
  one anchor. Inner ward and keep are always looted, the outer bailey half the time: 44-53 items a
  raid across the four eras, up from about 22 at one per room (`LootAmountTests`).

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

- **Spawned loot can be flung by physics.** The planner is pure and places loot 0.5 m above a room's
  centre, which can be inside real room geometry; PhysX then ejects it. Fixed by the settle window
  described under "How it works" — `LootSpawner` spawns every body kinematic and only hands it back
  to physics once the scene has stopped moving. The planner itself was deliberately left untouched:
  it must stay a pure calculation so every peer plans identical positions.
