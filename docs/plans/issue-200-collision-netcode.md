# #200 — why guards shove players through walls: collision and netcode (2026-10-01)

Read from code and prefabs, not yet reproduced in a running game. Fix so far: bd322ca2. A chasing
guard now stops 0.8 of its strike range short of the target.

## The parts

- **Players are owner-authoritative.** Each player's own machine simulates it as a dynamic
  Rigidbody (mass 1, capsule r 0.5, discrete collision). On every other machine the copy is
  kinematic and is moved by the replicated transform:
  `Assets/_Project/Scripts/Runtime/Net/PlayerNetworkOwnership.cs:137` sets
  `_rigidbody.isKinematic = !mine`.
- **Guards are server-authoritative.** Their NetworkTransform has `_ownerAuth: 0` and interpolation
  on. On the server the NavMeshAgent moves the transform directly. On clients the agent is off
  (`CastleGuard.OnSpawned`) and the replicated transform moves the guard. Some guard prefabs have a
  Rigidbody, made kinematic in `CastleGuard.Awake` (ManAtArms). Others have none (PalaceGuard), so
  their collider is a static collider being moved by hand. Either way, physics treats the guard
  as immovable.
- **Walls are hollow.** All 100 castle MeshColliders under `Assets/_Project/Prefabs/Castle` are
  non-convex (`m_Convex: 0`), so a wall is a surface, not a solid.
- **Depenetration:** the project's maximum is 10 m/s (`ProjectSettings/DynamicsManager.asset`).
- **Nothing un-sticks a player** who ends up inside or behind geometry.

## Mechanisms, most likely first

1. **Client view: the guard is placed into the player by the network.** On the server, the
   client's player is a kinematic copy that is a little out of date. A kinematic guard and a
   kinematic player never collide, so on the server the guard can walk straight into where it
   thinks the player is. Before bd322ca2 it aimed exactly there. That position reaches the client
   one more delay later, and the guard is moved into the client's dynamic player. The guard
   cannot be moved, so physics pushes only the player out, at up to 10 m/s, and with a wall behind
   the player that push goes through the wall's surface. bd322ca2's 1.6 m stop-short margin
   (melee) covers most of the delay but does not guarantee it. A player moving toward the guard,
   or a slow connection, still lets them overlap on the client.
2. **Host view: the agent walks the guard into the host's own player.** Same push, with no
   network involved. bd322ca2 addresses this directly.
3. **"Stuck forever."** Because walls are hollow, once a player's capsule is past the surface,
   nothing pushes it back. The player sits inside the wall shell or in the void behind it, and no
   recovery exists. The ground snap (`PlayerStateMachine.cs:489`, `MovePosition`) keeps it there
   rather than freeing it.

## What would confirm each

- 1: a co-op run with a guard chasing the *client* into a wall, logging the client player's
  per-frame position. Expect a jump of more than about 0.3 m in one physics step at the moment the
  guard's replicated position overlaps it.
- 2: the same on the host. bd322ca2's test already shows the overlap before the fix.
- 3: put a player capsule just past a castle wall surface and step physics. Expect it to stay
  there.

## Fix options (owner to choose)

- **A. Guards never physically push players.** Put guard colliders and player colliders on layers
  that do not collide, and rely on stop-short plus attack range for spacing. This removes
  mechanisms 1 and 2 entirely, whatever the network delay. The cost: players and guards can
  overlap visually if someone walks into a guard.
- **B. Cap the shove.** Give the player Rigidbody a low `maxDepenetrationVelocity` (about 1–2 m/s),
  so an overlap pushes gently instead of launching the player. This makes the push smaller but
  does not remove it.
- **C. Un-stick.** If the owner's player is overlapping static geometry, or is more than about
  1 m off the NavMesh for over about 0.5 s, move it to the nearest NavMesh point. This is the
  safety net for mechanism 3 and any other cause, such as spells or loot.

The recommendation is A plus C: A removes the cause and C guarantees nobody is stuck forever.

## Host side: why physics lets it happen (added 2026-10-01, owner asked for the cause, not a workaround)

The owner rejected A/B/C: the physics should stop this, so find why it does not. The host-side
chain, read from code and settings (a repro test is next):

1. **The guard is not a physics participant.** The NavMeshAgent writes the guard's transform every
   `Update`. Its body is kinematic (ManAtArms) or absent (PalaceGuard: a bare collider moved by
   hand). PhysX gives both infinite mass and never lets a contact stop them. The agent's own
   avoidance knows nothing about the player, who is neither an agent nor a NavMeshObstacle, so its
   path runs straight through the player's capsule.
2. **A dynamic body between two immovable ones cannot be resolved.** With the player against a
   wall, the guard (immovable) pushes from one side and the wall (static) blocks the other. The
   solver cannot satisfy both contacts and the kinematic one always wins, so each physics step
   the guard's movement (up to 4.5 x 1.4 = 6.3 m/s at the hue and cry, about 0.13 m per 0.02 s
   step) presses the player further into the wall. Depenetration out of the wall is capped at 10 m/s
   (`DynamicsManager.asset`) and is undone by the next push.
3. **The wall is thinner than the player.** Walls are 0.5 m (`Tools/AssetPipeline/room_kit.py:34`
   `WALL_T`) and the player's capsule radius is 0.5 m (1 m wide). Once the capsule's centre passes
   the middle of the wall, only 0.75 m from first touching, the shortest way out of the overlap is
   the far side, and depenetration throws the player through.
4. **Then nothing brings them back.** The wall MeshColliders are non-convex, which makes them
   hollow surfaces, and the player's discrete collision detection does not sweep back across the
   face it crossed.

So the physics is doing what it is told. The faulty input is an immovable guard driven into a
dynamic player. The physics-correct fix is to make the guard a real participant: a dynamic
Rigidbody with finite mass, moved by velocity toward the agent's planned position
(`agent.updatePosition = false`, `agent.nextPosition` kept in step with the body). Contacts then
stop the guard at the player, the wall wins over the guard, and the solver never has to choose.
The kinematic body came in for #104 (agent and physics fighting gave jitter); driving the body by
velocity and syncing the agent to it is the standard way to have both.

Confirm with a play-mode test: real 0.5 m wall, player capsule r 0.5 dynamic against it, guard
moved by its agent at chase speed into the player, step physics. Expected before the fix: the
player ends up past the wall's far face. After: the player stays on the near side and the guard
stops on contact.
