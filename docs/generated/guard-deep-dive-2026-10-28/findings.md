# Guard deep dive — findings (2026-10-28)

What the castle guards actually do today, read from the code on `AjsAgent/guard-deep-dive-0031e3` (cut from
`AjsAgent/gym-with-demand-page` at `9df7214e`), and the root cause of each symptom aj saw in the co-op playtest of
2026-10-28. Nothing was run in game for this document: every claim below is from reading the code and prefabs, cited
to `file:line`. Paths are relative to `Assets/_Project/Scripts/Runtime/` unless they start with `Assets/` or `docs/`.

The proposal built on these findings is `docs/plans/guard-awareness.html` (content in `guard-awareness.page.json`).

## 1. How it works today

### The loop

- `Guard.Update` runs only on the host (`Guards/Core/Guard.cs:216-221`). Each tick it looks (`Sight.Update`), runs the
  current state, then applies shove and lift (`Guard.cs:225-231`).
- A state returns its own next state (`Guards/States/GuardState.cs:6-11`). The states are Patrol, Investigate, Chase,
  Combat, HoldBelow, Stunned, Slept, OnFire, Dead and Turning (`Guards/States/GuardStateSet.cs:12-24`).
- Status effects (sleep, stun, levitation, fire) and death interrupt whatever runs (`Guard.cs:265-276`).

### Sight

- Twelve looks a second, phased per guard (`Guards/Senses/GuardSightThrottle.cs:14`, `:21-25`).
- Range is the prefab `SightRange` (14 m on most prefabs, 14-20 m across them) times `GuardBrain.BaseSightScale`
  1.5 (`Guards/GuardBrain.cs:109`), times the alarm stretch: x1.15 Stirred, x1.4 Roused, x1.75 Hue and Cry
  (`GuardBrain.cs:23-32`, applied at `GuardSight.cs:62`). A 14 m prefab therefore sees 21 m when Calm, 36.75 m at
  Hue and Cry. A castle room is 12 m across (`docs/4-systems/castle.md:153`).
- The cone is one flat wedge: 110 degrees side to side (`GuardTuning.cs:19`, half of it tested at `GuardBrain.cs:113-121`)
  and 80 degrees up or down (`GuardBrain.cs:103`). It points along the guard's **body** forward (`GuardSight.cs:119`);
  there is no head turn. Inside the wedge a sighting is instant; outside it nothing is seen, even at arm's length.
- A sighting needs one clear ray to head, middle or feet (`GuardSight.cs:105-113`). The ray stops at **any** collider
  on `GeometryLayers` that is not part of the target (`GuardSight.cs:123-138`). Every guard prefab sets
  `GeometryLayers` to Default only (`m_Bits: 1`, e.g. `Assets/_Project/Prefabs/Enemies/AgeOfPowder/PalaceGuard.prefab`),
  and Default holds walls, props, loot and other guards alike.
- While the alarm is Calm, nobody is seen for the first 20 s of a raid (`Guards/Senses/GuardArrivalGrace.cs:14`,
  `GuardSight.cs:55`).

### Hearing

- A noise reaches a guard through `NoiseBroadcaster`: an overlap sphere of the noise's radius, then each sound-blocking
  wall on the straight line halves the strength, up to three walls (`Acoustics/NoiseBroadcaster.cs:350-386`,
  `:397-417`). Footstep radii are 1.5 m creeping, 4 m walking, 8 m running (`Acoustics/FootstepNoiseEmitter.cs:35-37`).
- A heard noise over the alarm's threshold (`GuardBrain.cs:65-76`) becomes a lead at the noise's **exact origin**
  (`Guards/Core/GuardLeads.cs:38`). The guard is never told a direction or an entry point; it is handed the true spot,
  even through two walls, and routes straight to it.
- A guard's own first-sighting cry carries 18 m at strength 0.4 (`Guards/Core/GuardCry.cs:14-18`). It leads other
  guards to where the crier stood (`GuardLeads.cs:37`).

### Leads, Patrol and Investigate

- `GuardLeads` keeps one lead, the strongest: noise up to 1, hue and cry and cry 2, own sighting 3
  (`GuardLeads.cs:14-24`).
- Patrol walks three random points 3-8 m from the post with a 1 s pause at each (`GuardTuning.cs:39-48`). A sighting or
  any lead sends it to Investigate (`Guards/States/PatrolState.cs:49-58`).
- Investigate walks to the lead at 3.2 m/s (`GuardTuning.cs:55`), then **stands still** for 3 s and goes back to Patrol
  (`Guards/States/InvestigateState.cs:67-71`, `GuardTuning.cs:58`). Its own summary says it does not turn while it
  looks (`InvestigateState.cs:14-15`). Any `Blocked` answer makes it give up and Patrol (`InvestigateState.cs:115-116`).
  A player seen while investigating starts a Chase (`InvestigateState.cs:74-89`).

### Chase and Combat

- Chase re-plans toward the last seen spot every 0.25 s once the player has moved 0.75 m (`Guards/States/ChaseState.cs:114-125`,
  `GuardTuning.cs:62-65`) at 4.5 m/s (`GuardTuning.cs:35`). A `Blocked` answer is not an exit (`ChaseState.cs:17-18`).
- Out of sight for 0.75 s (`GuardTuning.cs:68`) the chase ends: the last seen spot becomes a sighting lead and the guard
  goes to Investigate (`ChaseState.cs:100-112`). Combat and HoldBelow end the same way (`Guards/States/CombatState.cs:112-129`;
  HoldBelow after 3 s, `Guards/States/HoldBelowState.cs:71-75`). There is no guess at where the player went.
- Within reach (2 m melee, 8 m ranged; `GuardTuning.cs:71-74`) Chase hands over to Combat (`ChaseState.cs:133-149`).
  Only **one** melee and **one** ranged guard may attack a player at a time (`Alarm/AttackTurnTuning.cs:14-17`); the
  rest hold a ring 3 m out and wait (`CombatState.cs:136-140`, `:174-178`).
- `GuardBrain.MoveSpeed`, the alarm speed bonus (x1.1 to x1.4, `GuardBrain.cs:35-59`), is **never called**: a search
  for `MoveSpeed(` finds only its definition. Every move uses the flat prefab speeds.
- The raid player walks at 6 m/s (`Assets/_Project/Prefabs/Player.prefab:220`; `docs/4-systems/alarm.md` says 5 m/s;
  either way above the 4.5 m/s chase).

### Movement and the nav map

- The director's navigation service plans and walks every guard (`Alarm/Navigation/GuardNavigationService.cs:120-177`).
  A plan that fails is answered `Blocked` at once (`:144-148`); a guard whose own step is cut by the sweep for 0.5 s is
  answered `Blocked` with reason Obstacle (`:186-192`, `Alarm/Navigation/GuardNavigationTuning.cs:143`).
- The map says `Unreachable` when start and goal are in areas not joined even with doors open
  (`Castle/Navigation/CastleGuardNavigationMap.cs:58-73`).
- Facing follows the path step only while moving, and not at all on a Combat move (`Alarm/Navigation/GuardMoverFacing.cs:25-36`).
  A standing guard keeps whatever facing it arrived with.
- After a charged beam breaks the castle, the host waits two physics steps and calls `CastleNavGraph.RefreshAfterBreak`
  (`Raid/Ophan/OphanOfHours.cs:330-342`). It clears cells whose floor went, adds cells a broken wall freed, links the two
  sides of a wall hole, rebuilds and bumps the version (`Castle/Navigation/CastleNavGraph.cs:216-275`), so moving guards
  re-plan (`GuardNavigationService.cs:196-207`). Fracture debris is drawn on the GPU with no colliders
  (`Fracture/FractureEffect.cs:109`), so loose pieces do not block guards or their sight.
- "Unreachable on foot" (`Guards/Core/GuardReachability.cs:33-47`) only asks whether the player stands more than 0.9 m
  above the floor under them. It never asks whether a **route** to that floor exists.

## 2. Root cause of each symptom

### "Guards got stuck looking at objects"

1. **Investigate stands still facing the way it walked in, for 3 s, then leaves** (`InvestigateState.cs:14-15`, `:67-71`).
   The lead spot is usually where a player ducked behind cover, so the guard arrives facing the cover and stares at
   it. It never sweeps the room, so a player 2 m to its side goes unseen.
2. **A guard that is standing never turns** (`GuardMoverFacing.cs:31-32`): the same happens on every patrol pause
   (`PatrolState.cs:55-57`) and after a `Blocked`.
3. **Props hide players** (`GuardSight.cs:130-136`): any Default-layer collider between the eye and all three aim
   points blocks the sighting, including a crate, a table edge, a barrel or another guard. A guard can look straight at
   a prop the player is half behind and see nothing.

### "Guards ignored the player because of the new floor and wall holes"

1. **Investigate drops a lead it cannot route to** (`InvestigateState.cs:115-116`). A noise or sighting across a floor
   hole, or on the far side of a gap the map now calls `Unreachable`, sends the guard straight back to Patrol.
2. **Chase freezes at a hole.** A player across a hole is not "unreachable" to `GuardReachability` (there is floor under
   them, `GuardReachability.cs:44-46`), so a melee guard stays in Chase. Every re-plan fails with `Unreachable`
   (`CastleGuardNavigationMap.cs:64-67`) and Chase ignores the failure (`ChaseState.cs:17-18`). The guard stands at the
   lip, sees the player, and does nothing. It never goes the long way, never throws, never calls help.
3. **A player who drops through a floor hole is lost within 0.75 s** of leaving the view (`ChaseState.cs:100-105`). The
   guard then investigates the spot it last saw them, which is now across the hole, and point 1 sends it to Patrol.

### "They are not aggressive enough"

1. **They cannot catch a walking player.** Chase is 4.5 m/s against a 5-6 m/s walk, and the alarm speed bonus is dead
   code (`GuardBrain.cs:35-59`, never called).
2. **Only one melee guard swings at a time** per player (`AttackTurnTuning.cs:14`); three melee guards on one player
   look like one guard fighting and two spectating (`CombatState.cs:139-140`).
3. **They give up fast:** 0.75 s out of sight ends a chase, 3 s at the spot ends the search (sections above).
4. **Leads from cries and the hue and cry are walked to at 3.2 m/s** (`InvestigateState.cs:110`), so help arrives late.

### "No proper vision cone or spatial awareness"

1. The cone is a flat 110-degree wedge tied to the body, with instant sight inside and none outside, and no awareness at
   all behind or beside the guard at any distance (`GuardBrain.cs:87-121`).
2. Hearing hands over the exact origin (`GuardLeads.cs:38`), so a guard does not reason about where a sound came
   **from**: it does not turn toward a noise, and it either routes perfectly to it or, when the route fails, gives up.
3. Nothing remembers where a player was going: no velocity, no shared last-known spot between guards, no notion of
   rooms.

### "Brain dead while the player stands right in front of them"

Five separate causes, any one of which matches what aj saw:

1. The **chase freeze** at an unroutable player (above): in Chase, seeing the player, not moving.
2. **Waiting its turn** in Combat (`CombatState.cs:139-140`): a melee guard on the ring, facing the player, not
   attacking because another guard holds the only melee turn. A turn not used is only taken back after 3 s
   (`AttackTurnTuning.cs:20`).
3. **Something on the Default layer in the line** (`GuardSight.cs:130-136`): another guard, a prop, a carried piece of loot.
4. **The 20 s arrival grace** while Calm (`GuardArrivalGrace.cs:14`): no guard sees anyone, by design.
5. **Directly beside or behind** the guard, outside the 110-degree wedge, a player is invisible at any distance.

### "Revert to patrol when you just move behind an object"

Chase ends after 0.75 s unseen (`GuardTuning.cs:68`), Investigate looks for 3 s without turning, then Patrol
(`InvestigateState.cs:67-71`). Ducking behind a pillar for about four seconds is enough to reset a guard, whatever
the alarm level.

## 3. What the proposal must not touch

- Ophan, fracture and beam code belong to the session fixing the Ophan on `AjsAgent/gym-with-demand-page`.
- The nav files changed for #478 (`CastleNavGraph`, `CastleNavGrid`, `CastleNavPortalGraph`, `CastleNavProbe`,
  `Editor/CastleNavTileBaker`) stay as they are. The suggested options need only read-only questions of the map
  (which room a point is in, which doorway a route crosses first), answered through `IGuardNavigationMap` and its
  adapter `CastleGuardNavigationMap`, which #478 did not change.

## 4. Not checked

- No in-game run backs this document; a baseline co-op recording of the five symptoms goes on the build plan before
  any change, so the QA page can show before and after.
- Player sprint speed: no sprint speed was found in the player code or prefab, so 6 m/s is taken as the top speed.
