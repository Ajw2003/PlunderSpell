# Hue and cry swarm, before and after #264 (2026-10-05)

Co-op, Editor host + Development build, seed 3508293, `Tools/Unity/coop_swarm_check.sh` (hue and cry raised with 6 stand-in witnesses).
Samples: `swarm-before-264-samples.txt`, `swarm-diag-264-samples.txt` (before, with floor split), `swarm-after-264-samples.txt`.
Cause: `CastleLockdown` bars every door at Hue and Cry, the nav graph treated a barred door as closed, so 9 of 17 guards got `Blocked(DoorClosed)` (159 other-floor + 90 same-floor events in 60 s) and went back to Patrolling.
Fix: a barred door costs like a locked one for guards, and guards open it when they walk into it.

| t (s) | before within10 / within20 | before states | after within10 / within20 | after states |
|---|---|---|---|---|
| 0 | 0 / 1 | Patrolling=10 Investigating=7 | 0 / 1 | Investigating=17 |
| 5-6 | 0 / 2 | Patrolling=14 Chasing=2 Investigating=1 | 0 / 0 | Investigating=13 Patrolling=2 Chasing=2 |
| 11 | 0 / 4 | Patrolling=12 Investigating=3 Chasing=2 | 0 / 4 | Investigating=16 Chasing=1 |
| 17 | 1 / 5 | Patrolling=12 Chasing=1 Combat=2 Investigating=2 | 5 / 8 | Investigating=7 Patrolling=5 Chasing=2 Combat=3 |
| 23 | 2 / 5 | Patrolling=12 Combat=2 Investigating=2 Chasing=1 | 7 / 12 | Chasing=3 Combat=6 Investigating=3 Patrolling=5 |

Both runs end in GameOver (players killed) after the last row at 25-45 s, so later samples read 0 live guards.
Remaining: 5 guards on other floors still Patrolling at 17-23 s, and `Obstacle` blocks (48 in 60 s: 39 other-floor, 9 same-floor) are not explained; a follow-up issue.
