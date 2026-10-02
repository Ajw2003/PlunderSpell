# #200 after the friction and guard-ignore fix (after-200d)

TOTAL guards 20 observed_s 1811.3 stuck_s 1.7 parked_s 0.0 nodest_s 0.0 moving_s 1809.6 standing_s 1.7 now Patrolling=20
Kinematic 65.0 of 1773.9; dynamic unfixed 370.4 of 1784.0; now 1.7 of 1811.3.

Changes: guard colliders get a shared zero-friction PhysicsMaterial (Minimum combine, bounce 0) in `CastleGuard.Awake`;
`IgnoreOtherGuards` (called from `OnEnable`) ignores collisions between guards' colliders while keeping players and walls.
The watch now counts a guard as stuck only when its body speed is under 0.2 m/s and desiredVelocity is non-zero
(`guard_watch.cs`), so the number is not strictly the same measure as before; in the unfixed run 206 of 222 stuck rows
already met that test, so the drop is not an artefact of the stricter test.

Cause breakdown: 1 stuck row (1.7 s), see `after-200d-stuck.txt`. Everything else is moving.

Sinking: floor probe at (42.38, -24.6) found only `Ground@y0.00`; the guard stood on the outdoor ground at y 0.0 (the castle
floor is 0.31). Not sinking, no lower floor. At (-31.75, 4.6): `LateArtilleryYard@y1.34`, `Ground@y0.00`.

Levo: landed, onMesh True, kinematic False, then moved 1.14 m and 5.62 m. Frango (2 m shove): moved 3.89 m in the first
second (shove plus patrol), 5.57 m in the next 4 s: the guard resumes; the exact shove distance is still not isolated.
