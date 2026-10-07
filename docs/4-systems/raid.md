# Raid

The game loop: Lair → castle → haul → extraction → Lair, with the takings applied to the debt.
Everything else in the project is a system; `Plunderspell.Raid` is what makes them a game.

Assemblies: `Plunderspell.Raid` (loop, spawning), `Plunderspell.Guards` (the garrison), `Plunderspell.RaidHud` (HUD),
`Plunderspell.Status` (spell-inflicted conditions), `Plunderspell.Playtest` (a body to walk around in).

## How it works

- **`RaidDirector`** owns the loop and the phase (`InLair` / `Generating` / `Raiding` /
  `Extracting` / `Resolved`). `StartRaid(era)` re-arms the extraction zone, rolls a seed, builds a
  castle, plans and spawns the haul and the garrison, resets the alarm and starts the clock.
  `CallExtraction()` ends it early; the zone's tally comes back through the `ExtractionResolved` event, which
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
  down by Somnus, Frango and Ignis through `StatusEffectReceiver`. Every decision it makes is
  delegated to **`GuardBrain`**, which is pure.

- **`RaidHudPresenter`** gathers the raid into a plain `RaidHudModel`; `RaidHudView` draws it with
  IMGUI. What the player is *told* is logic and is tested; how it is drawn is not. Since #303 the presenter has no
  `Update`: it reads every source once on enable (`Refresh`, also what the public `Build()` does for tests) and then
  reassembles the model only when a bus event says a value changed (phase, clock, alarm state and level, debt, banked
  gold, haul, carried item, loot and door focus, ranged weapon, cast, local player, game state, mana, cast key, chant).
  The view draws `presenter.Model` and reads no other system. The presenter leaves the bus when disabled
  (`RaidHudEventTests`).

## How loot settles

`LootSpawner.SpawnFor` freezes every spawned rigidbody (`isKinematic = true`, velocities zeroed),
then starts a coroutine that waits `m_settleDelay` (0.5 s by default) and calls `ReleaseSpawned`.
A rigidbody that begins a frame overlapping a wall is depenetrated with enough impulse to throw it
out of the castle; holding it frozen until the rooms have stopped arriving means it only ever falls
the short distance to the floor.

`ReleaseSpawned` skips anything carried or broken — a carried item is kinematic on purpose, and
un-freezing it would drop it out of the carrier's hand socket. It is public so a test can settle the
haul without waiting the delay out in real time.

`LootSpawner` also checks each spawned piece's height against the headroom above it (#272): a piece that would poke into a ceiling (the 1.65 m altarpiece on a crypt niche or stair) moves to the room's next loot anchor that fits, else onto the room floor.

## Carrying and extracting

There were two pickup systems on the same objects. Loot prefabs carried **both** `LootPickup` (a
networked carry with two-person rules and fragility) and `Item` (the physics grab/carry/throw the
`PlayerStateMachine` drives through `ItemManager`). As of 2026-09-18 the `Item`/`ItemManager` path
is the live one.

### Arriving and leaving by portal (2026-09-25)

Night atmosphere step 0 (`docs/plans/night-atmosphere.md`, section 6). The team no longer starts
by the gatehouse, and nothing exists outside the curtain wall.

- **Arrival.** `CastleArrivalPlanner.ChooseModule`
  (`Assets/_Project/Scripts/Runtime/Castle/CastleArrivalPlanner.cs:34`) picks a module in the
  curtain strip, outer bailey or inner ward from the raid seed, on its own RNG stream, never the
  gatehouse. On the strip, only in front of an entrance into the castle (#140; `castle.md`,
  "Entrances from the strip"). `CastleSpawnResolver.ResolveArrival` (`CastleSpawnResolver.cs:77`) finds a clear
  standing point there and falls back to the old gate spawn, with a warning, if nothing qualifies.
  Every peer derives the same point from the seed, so nothing new is networked.
- **The portal is the `ExtractionZone`.** `RaidDirector.OpenPortal` (`RaidDirector.cs:441`) calls
  `ExtractionZone.PlaceAsPortal` (`ExtractionZone.cs:90`), which moves the zone to the arrival and
  shrinks its trigger to 4 × 4 × 4 m. The `PortalOpened` event is published with the floor point.
- **Players ring it.** Each stands `RaidDirector.PlayerRingRadius` (3.5 m, `RaidDirector.cs:107`)
  from its centre by owner number, outside the trigger, facing it (`PlayerStateMachine.FaceYaw`),
  except on the curtain strip, where they face the entrance (`RaidDirector.FacingTarget`).
- **Nothing lingers from the last raid** (#143, 2026-09-26). Placing a player also clears their
  status effects (`RaidDirector.ClearCarriedOverState`): a player who died burning used to set out
  on the next raid still alight, losing 32 health before they could move. Health itself is only
  reset for a player who died; a survivor keeps their wounds.
- **Left behind.** When the clock runs out, whoever is outside the portal is not saved and what
  they carry is lost. `RaidDirector.cs:478` counts them and the Lair's last-raid line ends
  "· N left behind".
- **Sealed.** `CastleBoundary.Rebuild` (`CastleBoundary.cs:29`) puts four invisible 40 m walls on
  the curtain wall's outer face, called from `RaidDirector.SealCastle` every raid. RaidScene's
  ground covers only the ring plus a 4 m apron.

### Lobby size (2026-09-26, #154)

The garrison grows with the lobby, so a four-player raid is not tuned for one. The server passes
`RaidDirector.LobbySize` (everyone connected in a session, else 1) to `GuardSpawner.SpawnFor`, which
scales guard density by `1 + 0.35 × (players − 1)` and each guard's health by
`1 + 0.25 × (players − 1)` (`CastleGuard.ScaleHealth`, applied once at spawn). Damage per hit does
not scale: each player is hit as hard as a solo one. Both rates are Inspector fields on
`GuardSpawner` (`_extraGuardsPerPlayer`, `_extraHealthPerPlayer`); the numbers are this change's
defaults, not playtested. A zone's chance of a guard is capped at 1, so the crypt saturates first.
`LobbyScalingTests` (PlayMode) covers the scale, the count over 20 castles and the health. Live on
seed 100454263: 1 player met 8 guards at 80 health, 2 players 8 at 100, 4 players 15 at 140. Not
tried: an actual four-machine lobby.

### Loot in the portal (2026-09-26, #158)

Loot put down inside the portal stops moving and cannot break, for the players' sanity and so a
growing pile costs no physics. `ExtractionZone.UpdateRestingLoot` tells each piece as it enters or
leaves (`Interfaces.IPortalResting`). `Item.SetInPortal` freezes the body (kinematic) once it has
settled, below 0.15 m/s, so a dropped piece still lands first; picking it up (`StartDragging`) or it
leaving the portal frees it. `LootPickup.SetInPortal` makes `WouldBreak` false and `Break()` a no-op,
so neither a fall, a knock nor a spell breaks it there. `PortalRestTests` (PlayMode) covers settling,
not freezing in mid-air, being picked up, and not breaking. Live: a Gold Death Mask dropped in the
portal froze; a 500 N·s shove, a spell break and a 50 m/s strike moved it 0.000 m and left it whole;
picked up it was free, and outside the portal it was an ordinary body again.

Known gap: the loot planner still keeps only the gatehouse clear, so loot can lie in the arrival
room, next to the exit, with no guard near it.

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

`ExtractionZone` tracks `LootValue` in its trigger and publishes `HaulInZoneChanged(worth, pieces)` as
loot enters and leaves. `RaidHudPresenter` takes the worth and piece count from that event into the model and the
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

## The Lair room (2026-10-06, #309)

Sessions start, and extraction/death returns, in the walkable Lair room, not the flat screen.

- `GameState.LairRoom` (`GameState.cs`): cursor captured, input accepted, Lair music, no screen, RaidHud hidden.
  `GameState.Lair` still means "the Lair screen is open" (cursor free).
- Entered from Play Solo (`MainMenuScreen.cs:198`), hosting/joining (`CoopSession.cs`), a resolved raid
  (`RaidBootstrapper.cs:87`) and the game-over button (`GameOverScreen.cs:89`).
- `LairRoomSpawner.cs:45` stands the local player at `PlayerSpawns/Spawn{owner}` (retried in `Update` until the body exists),
  only on arrival: `IsArrival` (`LairRoomSpawner.cs:23`) is false when coming from the Lair screen, so closing the
  ledger leaves the player where they stood (until 2026-10-07 it snapped them back to the portal).
- `LairPortalTrigger.cs:16`: the local player walking in while in LairRoom, host or solo, sets `Playing`.
- `LairLedgerHandle.cs:16` reads E itself and opens the Lair screen when the main camera's centre ray hits the table or the book within 3 m (`IsLookedAt`, `LairLedgerHandle.cs:23`). It does not go through `LootInteractor`: the raid player carries none, and picks loot up with the mouse through `ItemManager`. Esc in
  LairRoom does the same (`GameFlowInput.cs`); the Lair screen's "Back to the Room" returns.
- The Market door: `RoomTravel.cs:23` reads E when the camera looks at the door's own collider (the leaf is part of the
  cellar mesh) and `Travel` (`RoomTravel.cs:38`) stands the player at the Market's `PlayerSpawns/Spawn{owner}`. The
  Market's `LairExit` trigger across its south way in (`RoomTravel.cs:29`) brings them back to `MarketDoorArrivals`,
  just inside the Lair's door. The state stays `LairRoom` throughout.
- The Lair sits at (1000, 0, 0) and the Market at (1100, 0, 0) in `RaidScene`, clear of the castle (curtain wall about
  45 m round the origin). `Tools/Plunderspell/Place Lair And Market In Raid Scene` (`RaidSceneRooms.cs`) places both
  and connects the door and the way out.
- Evidence: `docs/generated/lair-room-2026-10-06/`.

### How the haul comes home (#310)

- `ExtractionZone.ResolveExtraction` (`ExtractionZone.cs:240`) publishes `HaulExtracted` (`ExtractionEvents.cs:21`),
  server or offline only, carrying `PiecesOf` (`ExtractionZone.cs:249`): the `LootItem` of every unbroken piece in the zone.
  A piece is identified by its `LootItem` because that is what `RaidLootTable` already maps to a prefab
  (`RaidLootTable.PrefabFor`), so no new id scheme.
- `HaulLanding.cs:64` (child `HaulLanding` of the Lair room, local (4.0, 0.30, 0), built in `LairRoomForge.cs`) spawns them
  on the server through its own `LootSpawner.SpawnPile` (`LootSpawner.cs:133`): same `SpawnLoose` path, kinematic then
  released after the settle delay. Positions come from `HaulLayout.Offsets(count, start)` (`HaulLayout.cs:23`; a 7 x 3 grid,
  0.45 m apart, stacked above that). The pile belongs to its own spawner, so `RaidDirector.ApplyResult`'s
  `_lootSpawner.Clear()` leaves it, and it sits at x 1000, far from the pad.
- The pile persists and grows (#312, the owner's decision of 2026-10-07: extraction stops banking). `SpawnPile` adds and no
  longer clears, and nothing clears the pile at raid start. A new haul starts its layout after the pieces already there
  (`start` = live piece count, `HaulLanding.cs:112`), so it lies beside them, in layers when the floor grid is full. Known
  ceiling: after a piece is removed from the middle, a new one can land on a cell still in use.
- Saving: `HaulPileSave` (`Runtime/Lair/HaulPileSave.cs`) keeps the pile per save slot as one PlayerPrefs string, the asset
  names of the pieces' `LootItem`s joined by `|`. The asset name is the id: stable across runs, already how the loot tables
  list items, and it keeps the Lair assembly free of the loot assembly. The save is rebuilt from the pieces that exist
  (`HaulLanding.Save`, `:131`) on every change: a haul landing, a restore, `Remove` (`:58`, takes a piece out of the pile by its
  object, for a pickup or a sale) and a per-frame count check (`Update`, `:39`) that notices a piece destroyed anywhere else.
  Names no table can resolve are kept in the save, never erased.
- Restoring: on the server once it runs (`ServerReady`, `:55`: offline, or the host's server up) and again on
  `SaveSlotLoaded` (`LairEvents.cs`, published by `LairHubManager.LoadSlot`, `LairHubManager.cs:62`; the old pile is cleared
  first). Names resolve through every era's loot table in the director's `EraContentCatalogue` (`RaidDirector.EraContent`)
  plus the raid spawner's own (`AllTables`, `:153`); a mixed-era pile is spawned per table to get each prefab.
  `LairHubManager.ResetSlot` wipes the slot's pile.
- Banking moved: `ApplyExtractionResult` (`LairHubManager.cs:118`) now only records `LastRaidWorth`; gold and the debt
  pay-down are `LairHubManager.BankSale(coins)` (`:125`), for the Market. The per-raid debt tick (`OnNewSession`) is unchanged.
- Solo Play check 2026-10-06 (before #312): two pieces (worth 2400) placed on the pad, `CallExtraction`: phase Resolved,
  state LairRoom, both pieces at (1003.55, 0.31, -0.90) and (1003.55, 0.31, -1.35).
- Check 2026-10-07 (#312): solo, the saved four pieces came back on Play; extraction left gold 0 and debt 650 as they were
  (last raid worth 2400), the pile grew to six and the save listed six; `BankSale(100)` took the debt 650 to 550 (gold 0, all
  of it went to the debt); destroying one piece dropped it from the save. Co-op twice running (`coop_lair_check.sh`): pile 0
  to 2, then 2 to 4, identical on host and client. Logs `docs/generated/coop-lair-2026-10-07/run1-*`, `run2-*`. The client
  screenshot looks at a table and does not show the pile.

## Guards that can actually hurt you

`CastleGuard` could see, hear, shout and chase, but had no attack of any kind — it would run at a
player forever and never land a blow. The attack code in the project lived on
`MonsterStateMachine`, a second enemy system that is not on any of the ten enemy prefabs.

`CastleGuard.TryAttack` is called from the `Chasing` branch of `Act`, gated on range and a
cooldown. The cooldown is load-bearing: without it a guard in contact damages the player every
frame, which reads as dying instantly for no visible reason. An incapacitated guard cannot attack,
which is what gives Somnus and Frango their point.

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

## Guards that keep moving (#193, #194, #195)

Owner's rule: no waiting state, just patrol, chat, chase, kill. Before this pass a searching guard
stood on `_lastKnownIntruderPosition` for the search patience (forever at the hue and cry), a noise
only moved a patrolling guard, the hue and cry sent each guard once, and nothing noticed a guard
whose path was blocked. All the pure decisions are in `GuardBrain`; the rest is `CastleGuard`.

*Historical (#223, 2026-10-02): the next bullets describe the legacy `CastleGuard` on a NavMeshAgent. Guards
now move on the nav graph and the runtime NavMesh is gone; see `docs/4-systems/alarm.md` "Guard navigation".
`CastleGuard.cs`, `GuardMovementTests` and the `GuardBrain` rules named below were deleted on 2026-10-02 (owner's
approval); the `CastleGuard.cs:N` line numbers refer to the frozen copy `docs/reference/guard-legacy/CastleGuard.cs.txt`.
The hue and cry's re-sending is now the director's repeat (#239, alarm.md).*

- **Stuck watchdog** (`CastleGuard.WatchProgress`, `CastleGuard.cs:882`). A guard with a destination
  that covers under 0.3 m in 1.5 s (`GuardBrain.IsStuck`, `StuckProgress`, `StuckSeconds`,
  `GuardBrain.cs:87`) first gets a fresh path, then (next window) a reachable detour point 2-5 m
  from its goal, and if none exists it gives the goal up: the patrol point is skipped, an
  investigation ends, a sweep point is replaced. It is off for a guard that can see its target
  (standing to strike is not stuck) and for speed 0 (the turret).
- **Snapping** (`SnapToMesh`, `Reachable`, `CastleGuard.cs:809,820`; `MoveTo`, `:841`). Every
  destination is snapped to the nearest NavMesh point (6 m) before it is sent, and arrival is judged
  against the snapped point, so a player on a table or a noise in a wall is still reachable.
  `SetDestination` is only re-sent when the target moved over 0.75 m or the agent has no path.
- **A chaser stops short of the player** (#200; `ChaseToward`, `CastleGuard.cs:693`, called from `Act`,
  `:666`). It used to send the player's own position to the agent, so the guard walked its body into
  the player and the physics depenetration pushed the dynamic player body out sideways, against walls
  and (thin ones) through them. The destination is now a point 0.8 of the strike range (`k_strikeStopFraction`,
  `:823`; the sight range for projectile guards) short of the target, and inside that distance the
  guard stands and faces the target. `NavMeshAgent.stoppingDistance` was tried first and does not hold
  the agent (it braked into the target anyway, measured). `GuardShoveTests` reproduces it.
  **Host-side cause fixed** (#200): the guard was an immovable, infinite-mass body driven into a dynamic
  player pinned against a 0.5 m wall. `CastleGuard.Awake` (`CastleGuard.cs:160`) now gives every agent guard
  a finite-mass (80) dynamic Rigidbody, added if the prefab lacks one; on the server `FixedUpdate`
  (`:312`) sets `agent.updatePosition = false` and drives the body by the agent's `desiredVelocity`
  (horizontal, gravity kept), then syncs `agent.nextPosition` to the body, so the transform is never written
  (no #104 jitter). A client keeps it kinematic under the replicated transform. Levo hands the body back to
  the kinematic path (`UpdateLevitation`, `Land`). Frango's shove now goes through `IShovable.Shove`
  (`CastleGuard.cs:343`, `PrimarySpellEffects.cs:117`) as a short velocity burst. Test:
  `GuardShoveTests.Test_AGuardDrivenIntoAPlayerAgainstAThinWallCannotCrushThemThrough` (player centre
  reached x 16.48 through the wall before; stays on the near side now). **Open:** the co-op run after this
  change shows stuck_s 378.9 of 1778.9 (was 65.0 of 1773.9), see `after-200b-report.txt`; not yet diagnosed.
- **Searching sweeps** (`Search`, `:689`; `GuardBrain.SweepOffset`, `GuardBrain.cs:104`). The last
  known spot first, then ring points 4/6/8 m around it (100 degrees apart), skipping any that are
  off the mesh or cut off, until the search ends.
- **Investigating looks around** (`Investigate`, `:719`): on arrival it turns on the spot for
  `GuardBrain.LookAroundSeconds` (1.6 s), then returns to the route.
- **No route, no standing** (`Patrol` `:742`, `Wander` `:769`): fewer than two usable waypoints
  wanders between reachable points 3-8 m from where the guard was posted. `GuardSpawner` also gives
  a one-point route a second point (`NearbyPoint`, `GuardSpawner.cs:166`).
- **Noise steers the hunt** (`OnNoiseHeard`, `CastleGuard.cs:642`; `GuardBrain.ShouldFollowNoise`):
  a noise that passes `ShouldInvestigate` moves a searching guard's last-known spot (and restarts
  the sweep and the patience) to the noise origin; a chaser that has lost sight does the same. A
  guard that can see its target ignores it. Patrolling guards still investigate as before.
- **The hue and cry keeps hunting** (`KeepHunting`, `CastleGuard.cs:566`; `GuardBrain.ShouldHunt`, `HuntOffset`,
  `HuntDelay`): while the alarm is `HueAndCry`, every searching or investigating guard is re-sent
  every 3.0-3.9 s (staggered by instance id) to a point 3-5 m, random direction, from the nearest
  registered intruder, snapped to the mesh. Roughly-known, not exact, so players can still break away.
  Server only, like all guard AI.
- **Registries and the alarm sit on the `EnemyDirector`** (#205). Guards and players register with it
  (`CastleGuard.cs:143` reads its intruders, `IntruderTag.cs:1` registers a player in `OnEnable` and
  retries in `Start`, as the director may spawn later). `RaidDirector` still calls
  `ResetForNewRaid` on the same component; it never clears intruders, `IntruderTag` owns them. See
  `alarm.md` for the event bus and the hue and cry request.

Measured with `Tools/Unity/coop_guard_check.sh` (Editor host + Development client, seed 3508293, 20
guards, all provoked into a chase at the start, 90 s): seconds spent with a destination farther than
1 m away and under 0.2 m/s of movement went from **451.0 of 1798.9 guard-seconds (25.1%)** to **71.1
of 1768.5 (4.0%)**; seven guards were stuck 63-84 s each before, none more than 22 s after. Files in
`docs/generated/playability-2026-09-30/`. Tests: `GuardMovementTests` (20) and `GuardTests` (28).
Not checked: a player standing on furniture, and the in-game feel of the sweep (no one watched it).

## Invariants

- **No guard is posted, or patrols, within two rooms of the arrival portal**
  (`GuardPlacementPlanner.SafeEntranceRadius`, and `BuildRoute` leaves those rooms out of every
  route). One room was not enough: a guard next door walked its route through the gate.
- **For the first 20 seconds of a raid a calm garrison sees nobody** (`CastleGuard.ArrivalGraceSeconds`,
  started by `RaidDirector.StartRaid`). Guards still hear, and a raised alarm ends the grace. A
  player standing still at the spawn was first hit at about 10 seconds before this and the wider
  ring, and at 37 seconds after. Tests that tick guards call `CastleGuard.EndArrivalGrace()` first,
  because the grace is process-wide.
  Players arrive beside the portal (before 2026-09-25, inside the gatehouse); a guard next door saw them on the first frame and
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
