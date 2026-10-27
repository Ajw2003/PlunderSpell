<!-- claude-projects-attribution: {"github_login":"Ajw2003"} -->
_Requested by **Ayden**_

Step 2 of the Ophan's demand loop: one tuning asset for every number the demand uses, and raids that last 5 minutes.

Refs #433

**Manual co-op test still owed.** Every check below was run by a script. Nobody has played this by hand yet.

## What changed

- **New asset, `OphanDemandTuning`** (`Assets/_Project/Scripts/Runtime/Core/GameFlow/OphanDemandTuning.cs`, shipped as `Assets/_Project/Resources/OphanDemandTuning.asset`). It holds every field from #433 with the agreed defaults. Each field has a tooltip in the style of `TrueFormTuning`, and each of the 13 numbers has an Inspector range. `HuntEveryRaidWhileOwed` is the only field without a range, because it is an on/off switch.
- **Raid length comes from the asset.** `ExtractionZone.RaidLength` now reads `RaidSeconds`, which is 300 seconds. The zone's own serialized `RaidDurationSeconds` field (600 seconds by default) is removed. `SetRaidDuration` stays as an override that only tests use.
- **`RaidSceneBuilder`** no longer sets its own raid length, because the asset owns that number now.
- **Why the asset is in the `Plunderspell.Core` assembly, not `Plunderspell.Raid`.** The Raid assembly already depends on the Extraction assembly. If the extraction zone read an asset from Raid, the two would depend on each other in a loop.

## What was kept, changed and dropped

| Where the raid length was used | What happened |
|---|---|
| `ExtractionZone.RaidDurationSeconds` (serialized, 600 by default) | **Dropped.** The three raid scenes already stored 300 in this field, so their raids were already 5 minutes long. Those stored values are now ignored, and Unity removes them the next time each scene is saved. |
| `ExtractionZone.RaidLength`, read by the HUD and the pocket watch | **Changed:** it now returns `RaidSeconds`. |
| The clock reset and the minimum time before you can leave | **Kept:** both read `RaidLength`. |
| `SetRaidDuration`, called by tests with 125, 90, 45 and 600 | **Kept**, as an override for tests. |
| `RaidSceneBuilder.RaidSeconds` (300) and its `SetRaidDuration` call | **Dropped.** |

## Checks on the feature branch (Unity 6000.3.15f1)

| Check | Result |
|---|---|
| Compile | Clean |
| EditMode | 264 passed, 0 failed, 3 skipped. That is the baseline of 261 plus 3 new `OphanDemandTuningTests`, which check the defaults, check that every number has a range and sits inside it, and check that the numbers agree with each other. |
| PlayMode | 529 of 529 passed |
| `raid_length_check.sh feature-r2` (new) | PASS. At the start of the raid the pocket watch read "5 min" with all 60 ticks lit. The raid then ended on its own clock after 300.2 game seconds. To save time, the check ran at 10× speed until 12 seconds were left, then at normal speed. The guards were removed first so that none could end the raid early. |
| `fear_wick_check.sh ophan-433-feature` (co-op) | PASS |
| `coop_lair_check.sh ophan-433-feature` (co-op) | PASS all: 27 checks passed, with 0 error lines in the client's log |

The pocket watch shows whole minutes ("5 min"), not "5:00", because that is the watch's own format.

- Screenshot of the watch at the start of the raid: `docs/generated/ophan-demand-tuning-433/feature-r2-watch-start.png`
- Logs: `docs/generated/ophan-demand-tuning-433/`

Opened by AJ's agent
