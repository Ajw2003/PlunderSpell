<!-- claude-projects-attribution: {"github_login":"Ajw2003"} -->
_Requested by **Eden**_

Refs #418 (steps #439, #440, #441, #442, #443, #444)

**Manual co-op test still owed.** Nobody has played this by hand yet; every check below was run by a script.

## What this adds
The Ophan of Hours appears in the sky 60 s before the portal closes, 150 times its model and 900 m out, with its drone and screen shake. Its eye follows whoever carries the most worth and its rings spin by state. After a 3 s lock-on its slow beam drains 25 health a second until stone blocks it; after 3 s in cover it charges and fires a one-hit beam through stone that scorches that room open. A beam on an artifact's carrier lifts the piece into the sky, spares them, and sends the Ophan away. Until #419 exists, a test switch on `OphanOfHours` hunts every raid.

Changed in the last round:
- The slow beam no longer floods the target's own view: 0.8 m wide at the target, faded over its last 25 m on the target's machine.
- #443: a new held pose, `ophan_spread`, with the wings spread (13.2 m wide by 7.8 m tall); review sheet `docs/art/anim/fear/ophan_spread.png`.
- The take lifts a copy of the piece up the beam into the sky on every machine (it only flashed before).
- Fix found in co-op: after a kill, the Ophan drained the next player at once with no lock-on. `OphanHunt.Retarget` now gives every new target the full lock-on, with a test.
- New co-op check `Tools/Unity/ophan_check.sh`; `raid_length_check.sh` now switches hunts off.

## Checks
Feature branch:
- Compile clean. EditMode: 274 passed, 0 failed, 3 skipped. PlayMode: 528 of 529; the one failure was `CarryFeelTests.Test_WalkingDoesNotJoltAHeldItem` (0.080 m against 0.05 m), a timing test recorded as flaky in the live Editor since 2026-09-27 and outside this change; its class passed 20/20 on a rerun.
- `ophan_check.sh coop`: 14/14 PASS. `fear_wick_check.sh`, `coop_lair_check.sh` (27 PASS, 0 client errors) and `raid_length_check.sh` (300.5 game s) PASS.

Merged into `AjsAgent/staging-2026-10-26` locally and run again there:
- Compile clean. EditMode: 274 passed, 0 failed, 3 skipped. PlayMode: 529 of 529.
- `ophan_check.sh staging`: 14/14 PASS. Wick, Lair (27 PASS) and raid-length (301.3 game s) PASS.

Screenshots: `docs/generated/ophan-418/` (`coop-*` on the feature branch, `staging-*` on staging).

Opened by AJ's agent
