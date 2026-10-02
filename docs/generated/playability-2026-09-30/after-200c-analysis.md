# #200 dynamic-body change: why guards get stuck (measurement only, nothing fixed)

Run `after-200c` (co-op, 90 s sampling, seed 3508293), data in `after-200c-stuck.txt` (222 stuck samples, one row each;
fields: position, state, destination, body velocity, `agent.desiredVelocity`, onMesh, agent-to-body gap, ground normal,
what the guard's capsule touches). Script: `Tools/Unity/eval/guard_watch.cs` (`dump`), `Tools/Unity/coop_guard_check.sh`.

TOTAL guards 20 observed_s 1784.0 stuck_s 370.4 parked_s 0.0 nodest_s 1.7 moving_s 1411.9 standing_s 372.1 now Patrolling=20
(before the dynamic body: stuck_s 65.0 of 1773.9; the first dynamic run: 378.9. The two dynamic runs agree.)
The 222 rows total 374.2 guard-seconds (a row is one ~1.7 s sample), 18 of 20 guards appear, the worst guard alone has 67.3 s.

## The one fact that matters
In 206 of 222 rows (347.4 s) the body velocity is under 0.5 m/s while `agent.desiredVelocity` is 1 to 2.7 m/s. The agent wants
to go; the body is not going. In every row onMesh is True and the agent-to-body gap is under 0.1 m, so the agent is not
desynced from the body: it is following a body that the world is stopping. Slope is 0 in every row (flat floor,
normal (0,1,0)); no row is stairs.

## Cause breakdown (guard-seconds, rows)
| cause | seconds | rows | note |
|---|---|---|---|
| wall / static castle geometry | 227.5 | 135 | capsule overlaps a static collider (castle room mesh, TreadwheelWell, GreatHall) and body velocity is ~0 while desired is non-zero: pressed into it |
| guard vs guard | 93.0 | 55 | another guard's capsule overlaps; the pair jams (a finite-mass body can no longer pass through its neighbour) |
| nothing touched | 53.7 | 32 | 16 rows (26.8 s) still have body velocity over 0.5 m/s, so these are slow or turning guards that the 0.2 m/s sample test calls stuck; the rest are one-frame stops |
| props / loot | 0 | 0 | none touched |
| stairs / slope | 0 | 0 | none |
| off-mesh / agent desync | 0 | 0 | none |

## Example rows
Static geometry (pressed in, desired 2.4 m/s, body 0):
- `guard -123566 ... pos (42.38, 0.01, -24.67) Searching dest (33.30,0.33,-32.87) distDest 12.2 vel (0,0,0) desired (-2.37,0,0.98) onMesh True agentGap 0.09 ground Ground slope=0 touching [STATIC:LateTreadwheelWell(Clone)]`
- `guard -123022 ... pos (14.33, 0.31, 4.59) Searching dest (16.83,0.33,4.17) distDest 2.5 vel (0,0,0) desired (2.68,-0.01,-0.46) touching [STATIC:LateGreatHall(Clone)]`
Guard vs guard (same spot, a neighbour overlaps, 18.4 m from its goal):
- `guard -123566 ... pos (42.38, 0.02, -24.63) Patrolling dest (24,0,-24) distDest 18.4 vel (0,0,0.02) desired (-1.32,0,0.12) touching [STATIC:LateTreadwheelWell, GUARD:SalletHalberdier]`
- `guard -124118 ... pos (42.99, 0.00, -25.12) Investigating dest (36,0.31,-25.32) distDest 7.0 vel (0.04,0,0.08) desired (-0.49,0,0.94) touching [GUARD:Handgunner]`
Nothing touched (moving, classed stuck by the sample window):
- `guard -123154 ... pos (-24.94,0.31,4.60) Searching distDest 7.4 vel (1.37,0,1.98) desired (1.42,0,2.09) touching []`
- `guard -123426 ... pos (-1.16,0.31,33.22) Patrolling distDest 9.3 vel (0.27,0,-1.30) desired (0.29,0,-1.41) touching []`

## Concentration
One guard stands at (-31.75, 0.31) for 67.3 s (40 rows, Patrolling); one at (42.38, ~0.02, -24.6) (the TreadwheelWell, y 0.0 so
outside the castle floor at 0.31); others sit at fixed spots for 20 to 40 s. These are few fixed places, not a spread of
random hits, so it is geometry-specific.

## What this suggests (not tested, nothing changed)
The NavMesh edge is not the same shape as the colliders: the agent plans a path hugging a corner or a wall, the dynamic
capsule (radius 0.39, plus the extra capsule the guard prefab carries) is stopped by the collider, and nothing slides it
along, because the old transform-writing agent ignored colliders entirely and the new velocity push does not steer around
them. Guard-vs-guard is the same effect between two bodies. Candidate checks for the next round: compare the NavMesh agent
radius with the collider radius at (-31.75, 0.31) and (42.38, -24.6); look at the guard layer in the collision matrix.

## Levo and Frango on the host (same Play session, after the totals)
- Levo: `levo started on guard -122494 at (6.64,0.31,0.03)`; 9 s later `airborne False onMesh True kinematic False y 0.31 movedSinceLast 2.09`, 4 s after that `movedSinceLast 1.09`, state Patrolling. It landed on the NavMesh, went back to a dynamic body and moves again: PASS.
- Frango (`IShovable.Shove(forward * 2 m)` on the same guard): `movedSinceLast 0.55` after 1 s, then `0.17` after 4 more s, still Patrolling. The 2 m expected shove is not shown: it moved 0.55 m in the first second (the guard also patrols, and 0.17 m over the next 4 s says it is slow or stopped again, quite possibly a stuck spot like the above). Inconclusive: no reading of the position straight after the burst and no wall-free control, so the shove distance is UNVERIFIED.
