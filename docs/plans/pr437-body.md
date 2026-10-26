<!-- claude-projects-attribution: {"github_login":"Ajw2003"} -->
_Requested by **Ayden**_

One staging branch that carries the three open pull requests into main, tested after each merge.

Refs #414, #436, #422, #432

## What is merged, in order

`AjsAgent/staging-2026-10-26` starts from `staging-2026-10-11`. It does not change that branch or main.

1. **#436 "The Lantern Warden turns into the Wick in raids".** This already contains every commit of #414 ("Fear pass: the Wick and the Ophan of Hours, art, clips and import rules"), so #414 comes in with it. The merge was clean, and #414's own conflict against staging does not come up.
2. **#422 "Lair loot keeps its place, saved and restored on events with no per-frame code".** One conflict, in `docs/5-today/Today.md`, where both sides added a diary entry at the top. I kept both entries, newest first. There were no code conflicts.

Merging into main is clean (`git merge-tree` reports no conflicts, and main has no commits the branch lacks).

## Checks run (Unity 6000.3.15f1, this repo's `Tools/Unity` scripts)

| Round | Compile | EditMode | PlayMode | Co-op check |
|---|---|---|---|---|
| 1: staging + #436 | clean | 250 pass, 0 fail, 3 skipped | 528 / 528 | `fear_wick_check.sh`: the Wick shows on both machines, the lantern's position agrees, a catch kills; the 2 "client error lines" are stack frames containing the word `SocketError`, not errors |
| 2: + #422 | clean | 261 pass, 0 fail, 3 skipped | 528 / 528 | `coop_lair_check.sh`: PASS all (26 checks), 0 client error lines |
| 3: + the Wick-catch fix | clean | 261 pass, 0 fail, 3 skipped | 529 / 529 (one new test) | `fear_wick_check.sh` PASS twice: the client sees the caught host dead at health 0, and the host sees the caught client dead at health 0; `coop_lair_check.sh`: PASS all (27 PASS lines, 0 FAIL), 0 client error lines |

The PurrNet network prefab list was unchanged after each compile.

**Why EditMode reads 250, not the 254 baseline.** The 254 came from #422's own branch (`docs/generated/lair-pile-2026-10-18-editmode.log`), not from staging. Staging alone passes 243. #436 adds 7 (`FearImportTests`), which gives 250, and #422 adds 11 (`HaulLandingTriggerTests` 7, `HaulPileSaveTests` 4), which gives 261. No test was removed or renamed. `ScaleInvariantTests`, the one test file #436 changes, has 7 tests on both sides.

Logs are in `docs/generated/staging-2026-10-26/` (round 1 and 2 logs were left untracked by `*.log` in `.gitignore`; they are committed now). Screenshots are in `docs/generated/fear-wick-2026-10-15/staging-2026-10-26-r1-*` and `docs/generated/coop-lair-2026-10-25/staging-2026-10-26-r2-*`.

## Fixed on this branch: a caught player now dies on both machines

**The bug.** When the Wick caught a player in co-op, only that player's own machine knew it. #436's own run (`run4-console.txt`) shows it, so it predates this merge. The run before the fix (`staging-2026-10-26-before-run.log`) narrowed it down. The owner's "down" flag did reach the other machine, and the wizard did collapse there. But that machine's copy of the player stayed `dead = false` at health 100, so the host's raid code (`RaidDirector.CountLivingPlayers`, `ExtractionZone`) counted a dead teammate as alive.

**The fix.** A new method, `PlayerStateMachine.FollowOwnerDown`, is called from `PlayerNetworkOwnership.ShowDownPose` on every body this machine does not own. When the flag goes down, the copy's health drops to 0 and it dies; when the flag comes back up, it revives. That is 5 lines in `PlayerNetworkOwnership.cs` and 18 in `PlayerStateMachine.cs`. The mechanism is described in `docs/4-systems/net.md`, "Spectating and a party wipe".

**The proof.**
- A new PlayMode test, `PlayableLoopTests.Test_ATeammatesCopyFollowsItsOwnerDownAndBackUp`, checks that a copy dies with the flag and revives with it.
- `fear_wick_check.sh` now prints each player's `dead`, `netDown`, `drawnDown` and `shown` on both machines. After a catch, the other machine turns to look at the body and both machines take a screenshot. The check fails if the other machine still sees the caught player alive or standing.
- Before the fix the check FAILED: the host read the caught client as `health=100 dead=False`. After the fix it PASSED both ways. In `-after`, where the host was caught, the client read `health=0 dead=True netDown=True drawnDown=True`. In `-after2`, where the client was caught, the host read the same.
- The screenshots are `docs/generated/fear-wick-2026-10-15/staging-2026-10-26-{before,after,after2}-caught-player-{host,client}.png`.
- In the before run, the Wick caught the host 3 seconds after the client, so both "before" shots show the "You died" screen.
- The collapse animation itself was never broken: it already played before the fix.

The decision log has a new entry: nothing broken reaches main.

## Not checked

- The demand loop itself: it is not on this branch.
- Playing by hand: everything above is scripted.
- `lair_pile_check.sh`: only the co-op lair check ran.

## After this merges

#414, #436 and #422 target `staging-2026-10-11`, so they will stay open. Close them once this is merged.

Opened by AJ's agent

