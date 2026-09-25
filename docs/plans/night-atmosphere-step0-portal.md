# Night atmosphere, step 0: portal arrival and the sealed castle — implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development
> (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use
> checkbox (`- [ ]`) syntax for tracking.

**Goal:** Players arrive through a portal at a seeded random spot inside the castle, leave through
the same portal, cannot get out any other way, and nothing outside the curtain wall exists.

**Architecture:** A new pure planner (`CastleArrivalPlanner`) picks the arrival room from the layout
and the raid seed, so every peer derives the same spot with no networking. `RaidDirector.BuildCastle`
places the players round that spot, moves the existing `ExtractionZone` there as the portal, and
tells the guard planner to keep its safe ring round the arrival instead of the gatehouse. A
`CastleBoundary` ring of invisible colliders on the curtain wall's outer edge seals the gate and the
wall tops. The ground is shrunk to the castle's footprint.

**Tech Stack:** Unity 6000.3.15f1, URP 17.3, PurrNet 1.15, NUnit via the Unity Test Framework.

**Spec:** `docs/plans/night-atmosphere.md`, section 6 ("Arriving and leaving") and build order step 0.

## Global Constraints

- Drive the Unity Editor through the `unity` CLI connected to the live Editor
  (`unity command <name> --caller plugin --skill unity-cli`). Never run Unity with `-batchmode`.
  (User instruction, 2026-09-24.)
- Every `unity command` call carries `--caller plugin --skill unity-cli`.
- Never hand-edit `.unity`, `.prefab` or `.asset` YAML while the Editor is connected; change scenes
  through `unity command eval` and `unity command save_scene`.
- Code style: C# per the injected `csharp-unity-standards.md` and the surrounding files: Allman
  braces, `_camelCase` private fields, `PascalCase` public members, `k_` prefix for private
  constants (this codebase's own convention), `///` summaries that say why.
- `if (isSpawned && !isServer) return;` is the authority check. Never `if (!isServer) return;`
  (see `docs/ProjectState.md`, "The `isSpawned`/`isServer` trap").
- Determinism: anything derived from the raid seed uses its own `System.Random` stream, so adding it
  cannot shift the castle, loot or garrison.
- Commit after every task on the current `claude/` branch and push. Commit message format
  `<type>: <summary>`, ending with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- The gameplay tests (`Assets/_Project/Scripts/Tests/Runtime/`, assembly `RogueAi.Tests`) run in
  **PlayMode**. `EditMode` lists only the 26 legacy tests.

## Not in this plan (and where it goes)

- **The portal's look**: lapis glow, its light, the fog halo and the last-minute flicker. Built in
  step 2 (fire and light sources), which makes the glowing things. Until then the portal is the
  existing `Marker` slab, resized.
- **Naming who was left behind.** This plan shows how many (`· 1 left behind` on the Lair's
  last-raid line). Names need player display names wired into the Lair screen; that is its own
  small change.

## Files

| File | Change | Responsibility |
|---|---|---|
| `Tools/Unity/run_tests.sh` | create | Start a PlayMode test run in the live Editor, wait for it, print the summary and any failures, exit non-zero on failure. |
| `Assets/_Project/Scripts/Runtime/Castle/CastleArrivalPlanner.cs` | create | Pure: which module the team arrives in, and the anchor point inside it. |
| `Assets/_Project/Scripts/Runtime/Castle/CastleSpawnResolver.cs` | modify | Add `ResolveArrival`; make `InwardDirection` internal for the planner. |
| `Assets/_Project/Scripts/Runtime/Castle/CastleBoundary.cs` | create | Invisible colliders on the curtain wall's outer edge. |
| `Assets/_Project/Scripts/Runtime/Raid/GuardPlacementPlanner.cs` | modify | Safe ring centred on a given module (the arrival) instead of always the gatehouse. |
| `Assets/_Project/Scripts/Runtime/Raid/GuardSpawner.cs` | modify | Pass the safe module through. |
| `Assets/_Project/Scripts/Runtime/Extraction/ExtractionZone.cs` | modify | `PlaceAsPortal`: move and resize the zone; `CountLeftBehind`. |
| `Assets/_Project/Scripts/Runtime/Raid/RaidDirector.cs` | modify | Arrival, portal placement, players round it, boundary, left-behind count. |
| `Assets/_Project/Scripts/Runtime/Lair/LairHubManager.cs` | modify | Remember how many were left behind last raid. |
| `Assets/_Project/Scripts/Runtime/UI/Screens/LairScreen.cs` | modify | Say so on the last-raid line. |
| `Assets/_Project/Scripts/Editor/RaidSceneBuilder.cs` | modify | Scaffold ground sized to the castle; zone no longer placed outside. |
| `Assets/_Project/Scenes/RaidScene.unity` | modify via CLI | Ground scaled to the castle footprint. |
| `Assets/_Project/Scripts/Tests/Runtime/CastleArrivalTests.cs` | create | Planner, resolver, boundary, guard ring, portal tests. |
| `docs/systems/raid.md`, `docs/systems/castle.md`, `docs/ProjectState.md`, `docs/Today.md` | modify | Record the new arrival and exit. |

---

### Task 1: Test runner script for the live Editor

**Files:**
- Create: `Tools/Unity/run_tests.sh`

**Interfaces:**
- Produces: `bash Tools/Unity/run_tests.sh <filter>`, where `<filter>` is a full test or class
  name such as `RogueAi.Tests.CastleArrivalTests`. Prints `total/passed/failed`, then each failed
  test's name and message. Exit 0 when everything passed, 1 on any failure or on a timeout.

Why: `run_tests` in PlayMode returns at once with `"result": "running"`, and `test_status`
returns its report as a JSON **string** inside the JSON envelope, so a plain grep for
`"status": "completed"` never matches (seen 2026-09-24).

- [ ] **Step 1: Write the script**

```bash
#!/usr/bin/env bash
# Runs PlayMode tests in the connected Unity Editor and waits for the result.
# Usage: bash Tools/Unity/run_tests.sh RogueAi.Tests.CastleArrivalTests
set -euo pipefail

filter="${1:?usage: run_tests.sh <test or class full name>}"
cli=(--caller plugin --skill unity-cli --no-banner --format json)

unity command run_tests --mode PlayMode --filter "$filter" --async_tests true "${cli[@]}" > /dev/null

for _ in $(seq 1 120); do
    status_json="$(unity command test_status "${cli[@]}")"
    verdict="$(printf '%s' "$status_json" | python -c '
import json, sys
envelope = json.load(sys.stdin)
report = json.loads(envelope["data"]["result"])
if report.get("status") != "completed":
    print("running")
    sys.exit(0)
s = report["summary"]
print(f"total {s[\"total\"]}  passed {s[\"passed\"]}  failed {s[\"failed\"]}  skipped {s[\"skipped\"]}")
for r in report.get("results", []):
    if r.get("Status") not in ("Passed", "Skipped"):
        print(f"FAILED {r[\"FullName\"]}: {r.get(\"Message\")}")
print("FAIL" if s["failed"] or s["total"] == 0 else "PASS")
')"
    if [ "$verdict" != "running" ]; then
        printf '%s\n' "$verdict"
        [ "$(printf '%s' "$verdict" | tail -n 1)" = "PASS" ]
        exit $?
    fi
    sleep 5
done

echo "Timed out after 10 minutes waiting for the test run." >&2
exit 1
```

- [ ] **Step 2: Run it against a suite that already passes**

Run: `bash Tools/Unity/run_tests.sh RogueAi.Tests.RaidLoopTests`
Expected: `total 18  passed 18  failed 0  skipped 0`, then `PASS`, exit code 0.

- [ ] **Step 3: Run it twice more to prove the second run is not reading stale status**

Run: `bash Tools/Unity/run_tests.sh RogueAi.Tests.RaidLoopTests; bash Tools/Unity/run_tests.sh RogueAi.Tests.CastleGeneratorTests`
Expected: two summaries, the second with a different total from 18.
If the second prints 18 again, `test_status` is returning the previous run: add a
`sleep 3` before the loop and re-run.

- [ ] **Step 4: Commit**

```bash
git add Tools/Unity/run_tests.sh
git commit -m "chore: script to run PlayMode tests in the live Editor and wait for them"
git push
```

---

### Task 2: `CastleArrivalPlanner` — choose the arrival room

**Files:**
- Create: `Assets/_Project/Scripts/Runtime/Castle/CastleArrivalPlanner.cs`
- Modify: `Assets/_Project/Scripts/Runtime/Castle/CastleSpawnResolver.cs` (`InwardDirection`
  from `private` to `internal`)
- Test: `Assets/_Project/Scripts/Tests/Runtime/CastleArrivalTests.cs`

**Interfaces:**
- Produces:
  - `public static class CastleArrivalPlanner` in namespace `RogueAi.Castle`
  - `public const int NoArrival = -1;`
  - `public static bool IsArrivalZone(CastleZone zone)`: true for `CurtainWall`, `OuterBailey`,
    `InnerWard`
  - `public static int ChooseModule(ProceduralCastleData layout, int seed)`: an index into
    `layout.PlacedModules`, or `NoArrival`
  - `public static Vector3 AnchorFor(ProceduralCastleData layout, int moduleIndex)`: the room
    centre, or 3.5 m inward from a curtain-wall cell's centre

- [ ] **Step 1: Write the failing tests**

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using RogueAi.Castle;
using UnityEngine;

namespace RogueAi.Tests
{
    /// <summary>
    /// The team arrives by portal at a seeded spot inside the walls (docs/plans/night-atmosphere.md,
    /// section 6). These pin down where that spot may and may not be.
    /// </summary>
    public class CastleArrivalTests
    {
        private readonly List<Object> _spawned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _spawned)
                if (o != null)
                    Object.DestroyImmediate(o);
            _spawned.Clear();
        }

        private ProceduralCastleGenerator MakeGenerator()
        {
            var go = new GameObject("CastleGen");
            _spawned.Add(go);
            return go.AddComponent<ProceduralCastleGenerator>();
        }

        [Test]
        public void Test_TheSameSeedAlwaysArrivesInTheSameRoom()
        {
            ProceduralCastleGenerator generator = MakeGenerator();
            for (int seed = 1; seed <= 25; seed++)
            {
                ProceduralCastleData castle = generator.Generate(seed);
                int first = CastleArrivalPlanner.ChooseModule(castle, seed);
                int second = CastleArrivalPlanner.ChooseModule(castle, seed);
                Assert.AreEqual(first, second, $"Seed {seed}: every peer must pick the same arrival.");
            }
        }

        [Test]
        public void Test_TheTeamNeverArrivesInTheKeepCryptOrGate()
        {
            ProceduralCastleGenerator generator = MakeGenerator();
            for (int seed = 1; seed <= 50; seed++)
            {
                ProceduralCastleData castle = generator.Generate(seed);
                int index = CastleArrivalPlanner.ChooseModule(castle, seed);
                Assert.AreNotEqual(CastleArrivalPlanner.NoArrival, index, $"Seed {seed}: no arrival chosen.");

                ProceduralCastleData.PlacedModule module = castle.PlacedModules[index];
                Assert.IsTrue(CastleArrivalPlanner.IsArrivalZone(module.Zone),
                    $"Seed {seed}: arrived in {module.Zone}, which is too deep to start in.");
                Assert.IsFalse(module.IsExtractionExit || index == castle.ExtractionExitIndex,
                    $"Seed {seed}: the gatehouse is sealed and is never the arrival.");
            }
        }

        [Test]
        public void Test_DifferentSeedsArriveInDifferentPlaces()
        {
            ProceduralCastleGenerator generator = MakeGenerator();
            var seen = new HashSet<Vector2Int>();
            for (int seed = 1; seed <= 30; seed++)
            {
                ProceduralCastleData castle = generator.Generate(seed);
                int index = CastleArrivalPlanner.ChooseModule(castle, seed);
                seen.Add(castle.PlacedModules[index].GridPosition);
            }
            Assert.GreaterOrEqual(seen.Count, 10, "The arrival should move around the castle from raid to raid.");
        }

        [Test]
        public void Test_AnEmptyLayoutHasNoArrival()
        {
            Assert.AreEqual(CastleArrivalPlanner.NoArrival,
                CastleArrivalPlanner.ChooseModule(new ProceduralCastleData(1), 1));
            Assert.AreEqual(CastleArrivalPlanner.NoArrival, CastleArrivalPlanner.ChooseModule(null, 1));
        }

        [Test]
        public void Test_ACurtainWallArrivalStandsInsideTheWallNotOnIt()
        {
            ProceduralCastleGenerator generator = MakeGenerator();
            for (int seed = 1; seed <= 50; seed++)
            {
                ProceduralCastleData castle = generator.Generate(seed);
                for (int i = 0; i < castle.PlacedModules.Count; i++)
                {
                    ProceduralCastleData.PlacedModule module = castle.PlacedModules[i];
                    if (module.Zone != CastleZone.CurtainWall || module.GridPosition == Vector2Int.zero)
                        continue;

                    Vector3 anchor = CastleArrivalPlanner.AnchorFor(castle, i);
                    Assert.Less(new Vector2(anchor.x, anchor.z).magnitude,
                        new Vector2(module.Position.x, module.Position.z).magnitude,
                        $"Seed {seed}: a curtain-wall anchor must sit inward of the cell centre, off the wall.");
                }
            }
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `unity command recompile --caller plugin --skill unity-cli` then poll
`unity command recompile_status --caller plugin --skill unity-cli` until `completed`, then
`unity command console --level error --tail 20 --caller plugin --skill unity-cli`.
Expected: compile errors naming `CastleArrivalPlanner` (it does not exist yet).

- [ ] **Step 3: Make `InwardDirection` reachable from the planner**

In `CastleSpawnResolver.cs`, change the declaration line only:

```csharp
        internal static Vector3 InwardDirection(Vector2Int cell)
```

- [ ] **Step 4: Write the planner**

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace RogueAi.Castle
{
    /// <summary>
    /// Where the team steps out of the portal: a room chosen from the layout and the raid seed, so
    /// every peer derives the same spot without it crossing the network.
    ///
    /// Only the outer rings qualify. Starting in the keep or the crypt would skip the whole climb
    /// toward the best loot, and the gatehouse is sealed. See docs/plans/night-atmosphere.md,
    /// section 6.
    /// </summary>
    public static class CastleArrivalPlanner
    {
        /// <summary>Returned when a layout has no room the team may arrive in.</summary>
        public const int NoArrival = -1;

        /// <summary>
        /// How far in from a curtain-wall cell's centre to stand. That cell's wall runs along its
        /// outer half; the open strip of bailey is on the inner half.
        /// </summary>
        private const float k_CurtainInwardOffset = 3.5f;

        /// <summary>True for the zones a raid may start in.</summary>
        public static bool IsArrivalZone(CastleZone zone) =>
            zone == CastleZone.CurtainWall || zone == CastleZone.OuterBailey || zone == CastleZone.InnerWard;

        /// <summary>
        /// The index into <see cref="ProceduralCastleData.PlacedModules"/> the team arrives in, or
        /// <see cref="NoArrival"/>. Uses its own random stream, so it cannot shift the castle, the
        /// loot or the garrison.
        /// </summary>
        public static int ChooseModule(ProceduralCastleData layout, int seed)
        {
            if (layout?.PlacedModules == null)
                return NoArrival;

            var candidates = new List<int>();
            for (int i = 0; i < layout.PlacedModules.Count; i++)
            {
                ProceduralCastleData.PlacedModule module = layout.PlacedModules[i];
                if (module.IsExtractionExit || i == layout.ExtractionExitIndex)
                    continue;
                if (!IsArrivalZone(module.Zone))
                    continue;
                candidates.Add(i);
            }

            if (candidates.Count == 0)
                return NoArrival;

            var rng = new System.Random(unchecked(seed * 37 + 7919));
            return candidates[rng.Next(candidates.Count)];
        }

        /// <summary>The point in that module to stand the portal on, before any overlap probing.</summary>
        public static Vector3 AnchorFor(ProceduralCastleData layout, int moduleIndex)
        {
            ProceduralCastleData.PlacedModule module = layout.PlacedModules[moduleIndex];
            if (module.Zone != CastleZone.CurtainWall)
                return module.Position;

            return module.Position
                   + CastleSpawnResolver.InwardDirection(module.GridPosition) * k_CurtainInwardOffset;
        }
    }
}
```

- [ ] **Step 5: Recompile and run the tests**

Run: `unity command recompile --caller plugin --skill unity-cli`, poll `recompile_status` until
`completed`, check `unity command console --level error --tail 20 --caller plugin --skill unity-cli`
shows no `error CS`, then `bash Tools/Unity/run_tests.sh RogueAi.Tests.CastleArrivalTests`.
Expected: `total 5  passed 5  failed 0`, `PASS`.
If `Test_DifferentSeedsArriveInDifferentPlaces` fails with fewer than 10, print the count per seed
before changing the threshold; the candidate pool is roughly 30 cells, so under 10 distinct cells
in 30 seeds means the stream is not mixing.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Project/Scripts/Runtime/Castle/CastleArrivalPlanner.cs Assets/_Project/Scripts/Runtime/Castle/CastleArrivalPlanner.cs.meta Assets/_Project/Scripts/Runtime/Castle/CastleSpawnResolver.cs Assets/_Project/Scripts/Tests/Runtime/CastleArrivalTests.cs Assets/_Project/Scripts/Tests/Runtime/CastleArrivalTests.cs.meta
git commit -m "feat: choose a seeded arrival room in the castle's outer rings"
git push
```

---

### Task 3: `CastleSpawnResolver.ResolveArrival`

**Files:**
- Modify: `Assets/_Project/Scripts/Runtime/Castle/CastleSpawnResolver.cs`
- Test: `Assets/_Project/Scripts/Tests/Runtime/CastleArrivalTests.cs`

**Interfaces:**
- Consumes: `CastleArrivalPlanner.ChooseModule`, `CastleArrivalPlanner.AnchorFor`,
  `CastleArrivalPlanner.NoArrival` (Task 2).
- Produces: `public static Vector3 ResolveArrival(ProceduralCastleData layout, int seed, out int moduleIndex)`:
  the centre of a clear, player-sized standing capsule (same convention as `ResolveSpawn`), and the
  module chosen, or `NoArrival` when it fell back to `ResolveSpawn`.

- [ ] **Step 1: Write the failing tests** (append inside `CastleArrivalTests`)

```csharp
        [Test]
        public void Test_ResolveArrivalStandsInTheChosenRoom()
        {
            ProceduralCastleGenerator generator = MakeGenerator();
            ProceduralCastleData castle = generator.Generate(777);

            Vector3 point = CastleSpawnResolver.ResolveArrival(castle, 777, out int index);

            Assert.AreEqual(CastleArrivalPlanner.ChooseModule(castle, 777), index);
            Vector3 anchor = CastleArrivalPlanner.AnchorFor(castle, index);
            Assert.Less(Vector2.Distance(new Vector2(point.x, point.z), new Vector2(anchor.x, anchor.z)), 6f,
                "The standing point must stay inside the chosen 12 m cell.");
            Assert.AreEqual(CastleSpawnResolver.FloorHeight + CastleSpawnResolver.FloorClearance
                            + CastleSpawnResolver.PlayerHeight * 0.5f, point.y, 0.001f,
                "Same height convention as ResolveSpawn: the capsule's centre.");
        }

        [Test]
        public void Test_ResolveArrivalFallsBackToTheGateWhenNothingQualifies()
        {
            var layout = new ProceduralCastleData(5);
            layout.PlacedModules.Add(new ProceduralCastleData.PlacedModule(
                "ThroneRoomKeep", new Vector3(0f, 0f, 0f), Quaternion.identity, CastleZone.Keep, Vector2Int.zero));

            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning,
                new System.Text.RegularExpressions.Regex("No arrival room"));
            Vector3 point = CastleSpawnResolver.ResolveArrival(layout, 5, out int index);

            Assert.AreEqual(CastleArrivalPlanner.NoArrival, index);
            Assert.AreEqual(CastleSpawnResolver.ResolveSpawn(layout), point);
        }
```

- [ ] **Step 2: Recompile and confirm the compile error**

Same recompile and console commands as Task 2, Step 2.
Expected: `error CS0117: 'CastleSpawnResolver' does not contain a definition for 'ResolveArrival'`.

- [ ] **Step 3: Implement** (add below `ResolveSpawn` in `CastleSpawnResolver.cs`)

```csharp
        /// <summary>
        /// Where the portal opens and the team stands: a clear point in the room
        /// <see cref="CastleArrivalPlanner"/> picks for this seed. Falls back to
        /// <see cref="ResolveSpawn"/>, with a warning, when no room qualifies, so a raid always
        /// starts somewhere.
        ///
        /// Callers that have just instantiated the castle must have run
        /// <see cref="Physics.SyncTransforms"/> first, or every overlap probe reads clear.
        /// </summary>
        public static Vector3 ResolveArrival(ProceduralCastleData layout, int seed, out int moduleIndex)
        {
            moduleIndex = CastleArrivalPlanner.ChooseModule(layout, seed);
            if (moduleIndex == CastleArrivalPlanner.NoArrival)
            {
                Debug.LogWarning("[CastleSpawn] No arrival room in this layout; arriving at the gate instead.");
                return ResolveSpawn(layout);
            }

            return FirstClearStandingPoint(CastleArrivalPlanner.AnchorFor(layout, moduleIndex));
        }
```

- [ ] **Step 4: Recompile and run the tests**

Run: recompile as before, then `bash Tools/Unity/run_tests.sh RogueAi.Tests.CastleArrivalTests`.
Expected: `total 7  passed 7  failed 0`, `PASS`.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Runtime/Castle/CastleSpawnResolver.cs Assets/_Project/Scripts/Tests/Runtime/CastleArrivalTests.cs
git commit -m "feat: resolve a clear standing point in the arrival room"
git push
```

---

### Task 4: Guards keep clear of the arrival, not the gate

**Files:**
- Modify: `Assets/_Project/Scripts/Runtime/Raid/GuardPlacementPlanner.cs:62-118` and its route
  builder
- Modify: `Assets/_Project/Scripts/Runtime/Raid/GuardSpawner.cs:43-46`
- Test: `Assets/_Project/Scripts/Tests/Runtime/CastleArrivalTests.cs`

**Interfaces:**
- Consumes: `CastleArrivalPlanner.ChooseModule` (Task 2).
- Produces:
  - `GuardPlacementPlanner.Plan(ProceduralCastleData castle, int seed, float densityScale = 1f, int safeModuleIndex = -1)`.
    With `-1`, the safe ring is centred on `castle.ExtractionExitIndex`, exactly as today, so the
    two existing callers in tests are unchanged.
  - `GuardSpawner.SpawnFor(ProceduralCastleData castle, int seed, int safeModuleIndex = -1)`.

- [ ] **Step 1: Write the failing test** (append inside `CastleArrivalTests`; add
  `using RogueAi.Raid;` to the file's usings)

```csharp
        [Test]
        public void Test_NoGuardIsPostedOrPatrolsNextToTheArrival()
        {
            ProceduralCastleGenerator generator = MakeGenerator();
            for (int seed = 1; seed <= 25; seed++)
            {
                ProceduralCastleData castle = generator.Generate(seed);
                int arrival = CastleArrivalPlanner.ChooseModule(castle, seed);
                Vector2Int safe = castle.PlacedModules[arrival].GridPosition;

                foreach (GuardPlacement guard in GuardPlacementPlanner.Plan(castle, seed, 1f, arrival))
                {
                    Vector2Int at = castle.PlacedModules[guard.ModuleIndex].GridPosition;
                    Assert.Greater(Chebyshev(at, safe), GuardPlacementPlanner.SafeEntranceRadius,
                        $"Seed {seed}: a guard posted beside the portal kills the team on arrival.");

                    foreach (Vector3 stop in guard.PatrolRoute)
                    {
                        foreach (ProceduralCastleData.PlacedModule module in castle.PlacedModules)
                        {
                            if ((module.Position - stop).sqrMagnitude > 0.01f)
                                continue;
                            Assert.Greater(Chebyshev(module.GridPosition, safe), GuardPlacementPlanner.SafeEntranceRadius,
                                $"Seed {seed}: a patrol walks into the safe ring round the portal.");
                        }
                    }
                }
            }
        }

        private static int Chebyshev(Vector2Int a, Vector2Int b) =>
            Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
```

- [ ] **Step 2: Recompile and confirm the compile error**

Expected: `error CS1501: No overload for method 'Plan' takes 4 arguments`.

- [ ] **Step 3: Implement in `GuardPlacementPlanner.cs`**

Replace the `Plan` signature, its entrance lookup, and the `BuildRoute` call site. The loop body is
otherwise unchanged; `entrance` is renamed `safeCentre` throughout `Plan` and `BuildRoute`:

```csharp
        /// <summary>
        /// Plans the garrison. The gatehouse is never guarded, and no guard is posted or patrols
        /// within <see cref="SafeEntranceRadius"/> cells of <paramref name="safeModuleIndex"/>: the
        /// arrival portal, so the team is not shot while stepping out of it. Left at -1 the ring
        /// centres on the gatehouse, as it did before players arrived by portal.
        /// </summary>
        public static List<GuardPlacement> Plan(ProceduralCastleData castle, int seed,
            float densityScale = 1f, int safeModuleIndex = -1)
        {
            var placements = new List<GuardPlacement>();
            if (castle?.PlacedModules == null)
                return placements;

            // A third independent stream, so changing the garrison cannot shift the castle or the loot.
            var rng = new System.Random(unchecked(seed * 31 + 6151));

            int centreIndex = safeModuleIndex >= 0 ? safeModuleIndex : castle.ExtractionExitIndex;
            bool hasSafeCentre = centreIndex >= 0 && centreIndex < castle.PlacedModules.Count;
            Vector2Int safeCentre = hasSafeCentre ? castle.PlacedModules[centreIndex].GridPosition : default;

            for (int i = 0; i < castle.PlacedModules.Count; i++)
            {
                ProceduralCastleData.PlacedModule module = castle.PlacedModules[i];

                if (module.IsExtractionExit || i == castle.ExtractionExitIndex)
                    continue;

                // Rolled before the safe-ring check so the rest of the garrison stays where it was.
                if (rng.NextDouble() > DensityFor(module.Zone, densityScale))
                    continue;

                if (hasSafeCentre && ChebyshevDistance(module.GridPosition, safeCentre) <= SafeEntranceRadius)
                    continue;

                placements.Add(new GuardPlacement(
                    i,
                    module.Position + Vector3.up * 0.1f,
                    BuildRoute(castle, i, rng, hasSafeCentre, safeCentre),
                    module.Zone));
            }

            return placements;
        }
```

In `BuildRoute`, rename the parameters `hasEntrance, entrance` to `hasSafeCentre, safeCentre` and
the one use inside the neighbour loop accordingly. Update the `SafeEntranceRadius` summary's first
sentence to: `Rooms within this many grid cells of the arrival portal get no guard.`

- [ ] **Step 4: Pass the safe module through `GuardSpawner.SpawnFor`**

```csharp
        /// <summary>
        /// Plans and spawns the garrison for a castle. Returns the plan. <paramref name="safeModuleIndex"/>
        /// is the arrival room guards keep clear of; -1 keeps the old gatehouse ring.
        /// </summary>
        public IReadOnlyList<GuardPlacement> SpawnFor(ProceduralCastleData castle, int seed, int safeModuleIndex = -1)
        {
            Clear();

            List<GuardPlacement> plan = GuardPlacementPlanner.Plan(castle, seed, _densityScale, safeModuleIndex);
```

- [ ] **Step 5: Recompile and run the new and the old guard tests**

Run: recompile as before, then
`bash Tools/Unity/run_tests.sh RogueAi.Tests.CastleArrivalTests`, then
`bash Tools/Unity/run_tests.sh RogueAi.Tests.PlayableLoopTests`, then
`bash Tools/Unity/run_tests.sh RogueAi.Tests.RaidLoopTests`.
Expected: `CastleArrivalTests` `total 8  passed 8`; the other two print `PASS` with their previous
totals (`RaidLoopTests` 18). `Test_NoGuardIsPostedNextToTheEntrance` still passes because the
default argument keeps the gatehouse ring.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Project/Scripts/Runtime/Raid/GuardPlacementPlanner.cs Assets/_Project/Scripts/Runtime/Raid/GuardSpawner.cs Assets/_Project/Scripts/Tests/Runtime/CastleArrivalTests.cs
git commit -m "feat: keep the garrison's safe ring round the arrival portal"
git push
```

---

### Task 5: The extraction zone becomes the portal

**Files:**
- Modify: `Assets/_Project/Scripts/Runtime/Extraction/ExtractionZone.cs`
- Test: `Assets/_Project/Scripts/Tests/Runtime/CastleArrivalTests.cs`

**Interfaces:**
- Produces:
  - `public const float PortalFootprint = 4f;` (metres, square) and `public const float PortalHeight = 4f;`
  - `public void PlaceAsPortal(Vector3 floorPoint)`: moves the zone so its trigger box stands on
    `floorPoint`, `PortalFootprint` × `PortalHeight` × `PortalFootprint`; resizes a child named
    `Marker` to the same footprint when one exists.
  - `public static int CountLeftBehind(int livingPlayers, int saved)`: `Mathf.Max(0, livingPlayers - saved)`.

Why the footprint shrinks from the scene's 8 × 8 m pad: players stand in a ring round the portal
(Task 6) at 3.5 m. With a 4 m portal the ring is outside it, so nobody starts the leaving countdown
just by arriving.

- [ ] **Step 1: Write the failing tests** (append inside `CastleArrivalTests`; add
  `using RogueAi.Extraction;`)

```csharp
        [Test]
        public void Test_PlaceAsPortalStandsTheTriggerOnTheFloorPoint()
        {
            var go = new GameObject("ExtractionZone");
            _spawned.Add(go);
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(8f, 6f, 8f);
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "Marker";
            marker.transform.SetParent(go.transform, false);
            marker.transform.localScale = new Vector3(8f, 0.1f, 8f);
            ExtractionZone zone = go.AddComponent<ExtractionZone>();

            var floor = new Vector3(20f, 0.35f, -8f);
            zone.PlaceAsPortal(floor);

            Assert.AreEqual(floor, go.transform.position);
            Assert.AreEqual(new Vector3(ExtractionZone.PortalFootprint, ExtractionZone.PortalHeight,
                ExtractionZone.PortalFootprint), box.size);
            Assert.AreEqual(ExtractionZone.PortalHeight * 0.5f, box.center.y, 0.001f,
                "The trigger rises from the floor instead of being half buried in it.");
            Assert.AreEqual(ExtractionZone.PortalFootprint, marker.transform.localScale.x, 0.001f);
        }

        [Test]
        public void Test_EveryLivingPlayerOutsideTheClosedPortalIsLeftBehind()
        {
            Assert.AreEqual(2, ExtractionZone.CountLeftBehind(4, 2));
            Assert.AreEqual(0, ExtractionZone.CountLeftBehind(3, 3));
            Assert.AreEqual(0, ExtractionZone.CountLeftBehind(1, 2), "Never negative.");
        }
```

- [ ] **Step 2: Recompile and confirm the compile error**

Expected: `error CS0117: 'ExtractionZone' does not contain a definition for 'PlaceAsPortal'`.

- [ ] **Step 3: Implement** (in `ExtractionZone.cs`, after the `PiecesInZone` property)

```csharp
        /// <summary>Width and depth of the portal's trigger, in metres.</summary>
        public const float PortalFootprint = 4f;

        /// <summary>Height of the portal's trigger, in metres.</summary>
        public const float PortalHeight = 4f;

        /// <summary>
        /// Moves the zone to where the team arrived, so the way in is the way out
        /// (docs/plans/night-atmosphere.md, section 6). Called on every peer with the same point,
        /// derived from the raid seed, so nothing about it needs replicating.
        /// </summary>
        public void PlaceAsPortal(Vector3 floorPoint)
        {
            transform.position = floorPoint;

            if (TryGetComponent(out BoxCollider box))
            {
                box.size = new Vector3(PortalFootprint, PortalHeight, PortalFootprint);
                box.center = new Vector3(0f, PortalHeight * 0.5f, 0f);
            }

            Transform marker = transform.Find("Marker");
            if (marker != null)
                marker.localScale = new Vector3(PortalFootprint, marker.localScale.y, PortalFootprint);
        }

        /// <summary>Living players who were not in the portal when it closed.</summary>
        public static int CountLeftBehind(int livingPlayers, int saved) => Mathf.Max(0, livingPlayers - saved);
```

- [ ] **Step 4: Recompile and run the tests**

Run: `bash Tools/Unity/run_tests.sh RogueAi.Tests.CastleArrivalTests`, then
`bash Tools/Unity/run_tests.sh RogueAi.Tests.ExtractionHaulTests`.
Expected: `CastleArrivalTests` `total 10  passed 10`; `ExtractionHaulTests` `PASS` with its previous total.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Runtime/Extraction/ExtractionZone.cs Assets/_Project/Scripts/Tests/Runtime/CastleArrivalTests.cs
git commit -m "feat: the extraction zone can be stood up as the arrival portal"
git push
```

---

### Task 6: `CastleBoundary` — nobody leaves over the wall or through the gate

**Files:**
- Create: `Assets/_Project/Scripts/Runtime/Castle/CastleBoundary.cs`
- Test: `Assets/_Project/Scripts/Tests/Runtime/CastleArrivalTests.cs`

**Interfaces:**
- Produces:
  - `public class CastleBoundary : MonoBehaviour` in namespace `RogueAi.Castle`
  - `public const float Height = 40f;`
  - `public static float OuterEdge(int curtainWallRadius, float cellSize)`:
    `(curtainWallRadius + 0.5f) * cellSize`
  - `public void Rebuild(int curtainWallRadius, float cellSize)`: replaces its four child
    `BoxCollider`s (no renderers) with a wall 2 m thick just outside `OuterEdge`, from 1 m below
    the ground to `Height`.
  - `public static CastleBoundary EnsureOn(GameObject host)`: gets or adds the component.

- [ ] **Step 1: Write the failing test** (append inside `CastleArrivalTests`)

```csharp
        [Test]
        public void Test_TheBoundaryStopsAnythingLeavingTheCastle()
        {
            var host = new GameObject("Castle");
            _spawned.Add(host);
            CastleBoundary boundary = CastleBoundary.EnsureOn(host);
            boundary.Rebuild(4, 12f);
            Physics.SyncTransforms();

            float edge = CastleBoundary.OuterEdge(4, 12f);
            Assert.AreEqual(54f, edge, 0.001f, "Ring 4 of 12 m cells: the wall's outer face is 54 m out.");

            foreach (Vector3 direction in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
            {
                // At head height through the gate, and high enough to clear the battlements.
                foreach (float height in new[] { 1.5f, 20f })
                {
                    var origin = new Vector3(0f, height, 0f);
                    Assert.IsTrue(Physics.Raycast(origin, direction, out RaycastHit hit, 200f),
                        $"Nothing stops a player leaving {direction} at {height} m.");
                    Assert.AreEqual(edge, Vector3.Dot(hit.point, direction), 0.05f,
                        "The boundary sits on the wall's outer face, not inside the bailey.");
                }
            }
            Assert.AreEqual(0, host.GetComponentsInChildren<Renderer>().Length, "The boundary is invisible.");

            boundary.Rebuild(4, 12f);
            Assert.AreEqual(4, host.GetComponentsInChildren<BoxCollider>().Length,
                "Rebuilding replaces the walls rather than stacking another set.");
        }
```

- [ ] **Step 2: Recompile and confirm the compile error**

Expected: `error CS0246: The type or namespace name 'CastleBoundary' could not be found`.

- [ ] **Step 3: Implement**

```csharp
using UnityEngine;

namespace RogueAi.Castle
{
    /// <summary>
    /// Four invisible walls on the curtain wall's outer face. The castle is the whole world for
    /// now (docs/plans/night-atmosphere.md, section 6): the gatehouse is sealed and nothing exists
    /// past the battlements, so this stops a player walking out of the gate arch or being thrown
    /// over the wall by a spell.
    /// </summary>
    public class CastleBoundary : MonoBehaviour
    {
        /// <summary>How high the walls reach. Well above anything a spell can launch a body to.</summary>
        public const float Height = 40f;

        private const float k_Thickness = 2f;
        private const float k_BelowGround = 1f;
        private const string k_WallName = "BoundaryWall";

        /// <summary>Distance from the castle's centre to the curtain wall's outer face.</summary>
        public static float OuterEdge(int curtainWallRadius, float cellSize) =>
            (curtainWallRadius + 0.5f) * cellSize;

        /// <summary>Gets the boundary on <paramref name="host"/>, adding one if it has none.</summary>
        public static CastleBoundary EnsureOn(GameObject host) =>
            host.TryGetComponent(out CastleBoundary boundary) ? boundary : host.AddComponent<CastleBoundary>();

        /// <summary>Replaces the walls to fit a castle of this size.</summary>
        public void Rebuild(int curtainWallRadius, float cellSize)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.name == k_WallName)
                    DestroyImmediate(child.gameObject);
            }

            float edge = OuterEdge(curtainWallRadius, cellSize);
            float span = (edge + k_Thickness) * 2f;
            float centreY = (Height - k_BelowGround) * 0.5f;
            float offset = edge + k_Thickness * 0.5f;

            AddWall(new Vector3(offset, centreY, 0f), new Vector3(k_Thickness, Height + k_BelowGround, span));
            AddWall(new Vector3(-offset, centreY, 0f), new Vector3(k_Thickness, Height + k_BelowGround, span));
            AddWall(new Vector3(0f, centreY, offset), new Vector3(span, Height + k_BelowGround, k_Thickness));
            AddWall(new Vector3(0f, centreY, -offset), new Vector3(span, Height + k_BelowGround, k_Thickness));
        }

        private void AddWall(Vector3 localCentre, Vector3 size)
        {
            var wall = new GameObject(k_WallName);
            wall.transform.SetParent(transform, false);
            wall.transform.localPosition = localCentre;
            wall.AddComponent<BoxCollider>().size = size;
        }
    }
}
```

- [ ] **Step 4: Recompile and run the tests**

Run: `bash Tools/Unity/run_tests.sh RogueAi.Tests.CastleArrivalTests`.
Expected: `total 11  passed 11`. If the rebuild assertion reports 8 colliders, `DestroyImmediate`
is not reached for the children: check the name comparison before anything else.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Runtime/Castle/CastleBoundary.cs Assets/_Project/Scripts/Runtime/Castle/CastleBoundary.cs.meta Assets/_Project/Scripts/Tests/Runtime/CastleArrivalTests.cs
git commit -m "feat: invisible boundary on the curtain wall's outer face"
git push
```

---

### Task 7: Wire it into the raid

**Files:**
- Modify: `Assets/_Project/Scripts/Runtime/Raid/RaidDirector.cs` (`BuildCastle` at 196-238,
  `PlacePlayerAtSpawn` at 358-397, `OnExtractionResolved` at 399-411)
- Modify: `Assets/_Project/Scripts/Runtime/Lair/LairHubManager.cs`
- Modify: `Assets/_Project/Scripts/Runtime/UI/Screens/LairScreen.cs:201-203`
- Test: `Assets/_Project/Scripts/Tests/Runtime/CastleArrivalTests.cs`

**Interfaces:**
- Consumes: `CastleSpawnResolver.ResolveArrival` (Task 3), `GuardSpawner.SpawnFor(castle, seed, safeModuleIndex)`
  (Task 4), `ExtractionZone.PlaceAsPortal`, `ExtractionZone.CountLeftBehind` (Task 5),
  `CastleBoundary.EnsureOn`, `CastleBoundary.Rebuild` (Task 6), `ProceduralCastleGenerator.CurtainWallRadius`
  (exists, `ProceduralCastleGenerator.cs:78`).
- Produces:
  - `RaidDirector.ArrivalPoint` (`Vector3`, the capsule centre the portal stands under) and
    `RaidDirector.ArrivalModuleIndex` (`int`).
  - `RaidDirector.LastPlayersLeftBehind` (`int`).
  - `public const float PlayerRingRadius = 3.5f;` on `RaidDirector`.
  - `LairHubManager.LastRaidLeftBehind` (`int`, not persisted) and
    `public void RecordLeftBehind(int count)`.

- [ ] **Step 1: Write the failing test** (append inside `CastleArrivalTests`; add
  `using RogueAi.Lair;` and `using RogueAi.Inventory;`, the namespace of `HistoricalEra`)

```csharp
        [Test]
        public void Test_ARaidOpensThePortalInTheArrivalRoomAndSealsTheCastle()
        {
            var lairGo = new GameObject("Lair");
            _spawned.Add(lairGo);
            LairHubManager lair = lairGo.AddComponent<LairHubManager>();

            var zoneGo = new GameObject("ExtractionZone");
            _spawned.Add(zoneGo);
            zoneGo.transform.position = new Vector3(72f, 0f, 0f); // where the scene used to put it
            zoneGo.AddComponent<BoxCollider>().isTrigger = true;
            ExtractionZone zone = zoneGo.AddComponent<ExtractionZone>();

            var playerGo = new GameObject("Player");
            _spawned.Add(playerGo);

            var directorGo = new GameObject("RaidDirector");
            _spawned.Add(directorGo);
            RaidDirector director = directorGo.AddComponent<RaidDirector>();
            ProceduralCastleGenerator generator = MakeGenerator();
            director.Configure(generator, null, zone, lair, playerRoot: playerGo.transform);

            director.SetFixedSeed(4242);
            director.StartRaid(HistoricalEra.HighMedieval);

            Assert.AreNotEqual(CastleArrivalPlanner.NoArrival, director.ArrivalModuleIndex);
            Vector3 portal = zoneGo.transform.position;
            Assert.Less(Vector2.Distance(new Vector2(portal.x, portal.z),
                    new Vector2(director.ArrivalPoint.x, director.ArrivalPoint.z)), 0.01f,
                "The portal opens where the team arrives.");

            float edge = CastleBoundary.OuterEdge(generator.CurtainWallRadius, 12f);
            Assert.Less(Mathf.Max(Mathf.Abs(portal.x), Mathf.Abs(portal.z)), edge - 6f,
                "The portal is inside the walls, not out by the gate.");

            float fromPortal = Vector2.Distance(new Vector2(playerGo.transform.position.x, playerGo.transform.position.z),
                new Vector2(portal.x, portal.z));
            Assert.Greater(fromPortal, ExtractionZone.PortalFootprint * 0.5f,
                "The player arrives beside the portal, not inside it, so they do not start leaving at once.");

            Assert.IsNotNull(generator.GetComponent<CastleBoundary>(), "Every raid is sealed in.");
        }
```

- [ ] **Step 2: Recompile and confirm the compile error**

Expected: `error CS1061: 'RaidDirector' does not contain a definition for 'ArrivalModuleIndex'`.

- [ ] **Step 3: Add the state to `RaidDirector`** (beside `LastPlayersSaved`)

```csharp
        /// <summary>Where the team stepped out of the portal: a capsule centre, as the spawn resolver returns.</summary>
        public Vector3 ArrivalPoint { get; private set; }

        /// <summary>The module the team arrived in, or <see cref="CastleArrivalPlanner.NoArrival"/>.</summary>
        public int ArrivalModuleIndex { get; private set; } = CastleArrivalPlanner.NoArrival;

        /// <summary>Living players who were outside the portal when the most recent raid ended.</summary>
        public int LastPlayersLeftBehind { get; private set; }

        /// <summary>
        /// How far from the portal's centre each player stands on arrival. Outside the 4 m portal, so
        /// arriving does not start the leaving countdown.
        /// </summary>
        public const float PlayerRingRadius = 3.5f;
```

- [ ] **Step 4: Resolve the arrival and open the portal in `BuildCastle`**

Replace the single line `PlacePlayerAtSpawn();` in `BuildCastle` with:

```csharp
            // The rooms were instantiated a moment ago; without this their colliders are still at
            // their old transforms and every overlap probe reports clear.
            Physics.SyncTransforms();

            // Every peer derives the same arrival from the seed, so the portal and the team land in
            // the same place on every screen without it being sent.
            ArrivalPoint = CastleSpawnResolver.ResolveArrival(Castle, seed, out int arrivalModule);
            ArrivalModuleIndex = arrivalModule;
            OpenPortal();
            SealCastle();

            // Before the NavMesh bake and the spawners, so a guard or a loot pile is never dropped
            // on top of a player who is about to be moved there.
            PlacePlayerAtSpawn();
```

Replace `_guardSpawner?.SpawnFor(Castle, seed);` with
`_guardSpawner?.SpawnFor(Castle, seed, ArrivalModuleIndex);`.

Add these two methods beside `PlacePlayerAtSpawn`:

```csharp
        /// <summary>Stands the extraction zone on the floor under the arrival point: the way in is the way out.</summary>
        private void OpenPortal()
        {
            if (_extractionZone == null)
                return;

            float feet = ArrivalPoint.y - CastleSpawnResolver.PlayerHeight * 0.5f;
            _extractionZone.PlaceAsPortal(new Vector3(ArrivalPoint.x, feet, ArrivalPoint.z));
        }

        /// <summary>Puts the invisible boundary round the castle just built.</summary>
        private void SealCastle()
        {
            if (_generator == null)
                return;

            CastleBoundary.EnsureOn(_generator.gameObject)
                .Rebuild(_generator.CurtainWallRadius, k_CellSize);
        }

        /// <summary>The castle kit's cell size. The generator's own field is private; they must match.</summary>
        private const float k_CellSize = 12f;
```

- [ ] **Step 5: Stand the players round the portal in `PlacePlayerAtSpawn`**

Delete the `Physics.SyncTransforms();` call and the `Vector3 spawn = CastleSpawnResolver.ResolveSpawn(Castle);`
line (BuildCastle now does both), and replace the per-owner block with:

```csharp
            // Each player stands at their own point on a ring round the portal, by owner number, so
            // two bodies are never placed inside each other and nobody arrives standing in the exit.
            int index = player.TryGetComponent(out NetworkIdentity identity) && identity.owner.HasValue
                ? Mathf.Max(0, (int)(ulong)identity.owner.Value.id - 1)
                : 0;
            float angle = index * Mathf.PI * 0.5f;
            var anchor = new Vector3(ArrivalPoint.x + Mathf.Cos(angle) * PlayerRingRadius, 0f,
                ArrivalPoint.z + Mathf.Sin(angle) * PlayerRingRadius);
            Vector3 spawn = CastleSpawnResolver.FirstClearStandingPoint(anchor);
```

The rigidbody and transform assignments after it stay as they are.

- [ ] **Step 6: Count who was left behind**

In `OnExtractionResolved`, before the `if (isSpawned && !isServer)` branch, add:

```csharp
            // Every peer counts for itself: the bodies are replicated, the count is not.
            LastPlayersLeftBehind = ExtractionZone.CountLeftBehind(CountLivingPlayers(), saved);
            _lair?.RecordLeftBehind(LastPlayersLeftBehind);
```

and add the helper (add `using Interfaces;` to the file):

```csharp
        /// <summary>Living player bodies in the scene. Only called once, when a raid ends.</summary>
        private static int CountLivingPlayers()
        {
            int living = 0;
            foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (behaviour is IPlayerBody body && body.IsAlive)
                    living++;
            }
            return living;
        }
```

- [ ] **Step 7: `LairHubManager` remembers it**

Beside `LastRaidWorth` in `LairHubManager.cs`:

```csharp
        /// <summary>Players left behind when the last raid's portal closed. Not saved: it describes one evening.</summary>
        public int LastRaidLeftBehind { get; private set; }

        /// <summary>Records how many were stuck outside the portal when it closed.</summary>
        public void RecordLeftBehind(int count) => LastRaidLeftBehind = Mathf.Max(0, count);
```

- [ ] **Step 8: `LairScreen` says it**

Replace lines 201-203 with:

```csharp
            string leftBehind = _lair.LastRaidLeftBehind == 0 ? string.Empty
                : _lair.LastRaidLeftBehind == 1 ? " · 1 left behind"
                : $" · {_lair.LastRaidLeftBehind} left behind";
            _lastRaid.text = _lair.LastRaidWorth < 0f ? string.Empty
                : _lair.LastRaidWorth > 0f ? $"Last raid: brought home {_lair.LastRaidWorth:N0} coin{leftBehind}"
                : $"Last raid: came home with nothing{leftBehind}";
```

- [ ] **Step 9: Recompile and run every suite this touches**

Run, in order:
`bash Tools/Unity/run_tests.sh RogueAi.Tests.CastleArrivalTests`
`bash Tools/Unity/run_tests.sh RogueAi.Tests.RaidLoopTests`
`bash Tools/Unity/run_tests.sh RogueAi.Tests.PlayableLoopTests`
`bash Tools/Unity/run_tests.sh RogueAi.Tests.ExtractionHaulTests`
`bash Tools/Unity/run_tests.sh RogueAi.Tests.FullRaidIntegrationTests`
`bash Tools/Unity/run_tests.sh RogueAi.Tests.CoopRulesTests`
Expected: `CastleArrivalTests` `total 12  passed 12`; every other suite `PASS`.
A pre-existing test that asserts the player stands by the gatehouse is now wrong by design. Update
its assertion to the arrival point and name it in the commit body; do not delete it.

- [ ] **Step 10: Commit**

```bash
git add Assets/_Project/Scripts/Runtime/Raid/RaidDirector.cs Assets/_Project/Scripts/Runtime/Lair/LairHubManager.cs Assets/_Project/Scripts/Runtime/UI/Screens/LairScreen.cs Assets/_Project/Scripts/Tests/Runtime/CastleArrivalTests.cs
git commit -m "feat: raids arrive and leave by a portal inside a sealed castle"
git push
```

---

### Task 8: Cut the outside, then play it for real

**Files:**
- Modify: `Assets/_Project/Scripts/Editor/RaidSceneBuilder.cs:154-162` and `:183-201`
- Modify via CLI: `Assets/_Project/Scenes/RaidScene.unity`
- Create: `docs/generated/portal-arrival-2026-09-24/` (captures)

**Interfaces:**
- Consumes: everything above.

- [ ] **Step 1: The scaffold builder stops building an outside**

In `BuildGround`, replace the scale line and its comment with:

```csharp
            // The castle is the whole world: ground under the curtain ring plus a 4 m apron, and
            // nothing further. 9 cells of 12 m, a plane is 10 units across.
            ground.transform.localScale = Vector3.one * ((CellSize * 9f + 8f) / 10f);
```

In `BuildExtractionZone`, replace the position line and its comment with:

```csharp
            // RaidDirector stands the zone up as the arrival portal each raid; until then it waits
            // at the castle's centre rather than outside the wall.
            go.transform.position = Vector3.zero;
```

- [ ] **Step 2: Shrink the live scene's ground through the Editor**

Run:
```bash
unity command open_scene --path Assets/_Project/Scenes/RaidScene.unity --caller plugin --skill unity-cli
unity command eval --caller plugin --skill unity-cli --code 'var g = UnityEngine.GameObject.Find("Ground"); g.transform.localScale = UnityEngine.Vector3.one * 11.6f; var z = UnityEngine.GameObject.Find("ExtractionZone"); z.transform.position = UnityEngine.Vector3.zero; UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(g.scene); return g.transform.localScale.x + " " + z.transform.position;'
unity command save_scene --caller plugin --skill unity-cli
```
Expected: the eval prints `11.6 (0.00, 0.00, 0.00)`; `git diff --stat` shows `RaidScene.unity` changed.

- [ ] **Step 3: Run the scene tests**

Run: `bash Tools/Unity/run_tests.sh RogueAi.Tests.RaidSceneTests` and
`bash Tools/Unity/run_tests.sh RogueAi.Tests.RaidSceneCastingTests`.
Expected: `PASS` for both.

- [ ] **Step 4: Play a raid and capture it**

Enter Play mode, go Main menu → Lair → Set Out in the High Medieval era, then capture:
```bash
unity command editor_play --caller plugin --skill unity-cli
unity command capture_game_view --source screen --save_path docs/generated/portal-arrival-2026-09-24/01-arrival.png --caller plugin --skill unity-cli
```
Drive the menus with `unity command eval` calling the same methods the Lair buttons call (read
`LairScreen.cs` for the Set Out handler) rather than clicking. Then capture: `02-portal.png` looking
at the portal, `03-gate-sealed.png` from inside the gatehouse arch looking out,
`04-over-the-wall.png` with the camera raised above the battlements. For each, say in the commit
body what it shows. Expected by eye: the team beside a marker inside the walls; the gate arch
blocked (the player cannot walk through it); nothing but sky past the wall, with the ground ending
at the wall's foot.

- [ ] **Step 5: The two runs the spec asks for**

In the same Play session:
1. Pick up one loot piece, carry it into the portal, stand in it until the leaving countdown ends.
   Expected: back in the Lair, the last-raid line shows the coin brought home and no "left behind".
2. Set Out again, then set the raid clock to 2 seconds with
   `unity command eval --caller plugin --skill unity-cli --code 'var z = UnityEngine.Object.FindFirstObjectByType<RogueAi.Extraction.ExtractionZone>(); z.SetRaidDuration(2f); z.ResetForNewRaid(); return z.TimeRemaining;'`
   while standing away from the portal. Expected: the raid resolves and the Lair line ends
   `· 1 left behind`.
   If `SetRaidDuration` / `ResetForNewRaid` do not reset the running clock, read
   `ExtractionZone.cs` for the method that does and use that; do not add a new one for this.

Capture the Lair line after each as `05-lair-extracted.png` and `06-lair-left-behind.png`.

- [ ] **Step 6: The navigation audit**

Run `unity command menu --path "Tools/Plunderspell/Audit Castle Navigation (Play mode)" --caller plugin --skill unity-cli`
and read the report it writes (the path is in its console output).
Expected: every room reachable and 100% of floor reachable from the spawn on all five seeds, as in
`docs/generated/castle-survey-2026-09-23/audit-after-loot-anchors.md`. The audit paths from the
player's real position, which is now the arrival point.

- [ ] **Step 7: Commit**

```bash
unity command editor_stop --caller plugin --skill unity-cli
git add Assets/_Project/Scripts/Editor/RaidSceneBuilder.cs Assets/_Project/Scenes/RaidScene.unity docs/generated/portal-arrival-2026-09-24
git commit -m "feat: cut the outside; the castle is the whole raid"
git push
```

---

### Task 9: Documentation

**Files:**
- Modify: `docs/systems/raid.md`, `docs/systems/castle.md`, `docs/ProjectState.md`, `docs/Today.md`

- [ ] **Step 1: Tier 4.** In `docs/systems/raid.md`, replace the description of the spawn and the
  extraction pad by the gate with: the arrival (`CastleArrivalPlanner`, seeded, outer rings only),
  the portal (`ExtractionZone.PlaceAsPortal`, 4 m), the ring of players at
  `RaidDirector.PlayerRingRadius`, and the rule that anyone outside the portal when the clock runs
  out is left behind, citing `file:line` for each. In `docs/systems/castle.md`, add `CastleBoundary`
  and state that the gatehouse is sealed and no longer the spawn; keep the note that the generator
  still flags it `IsExtractionExit` and the loot planner still avoids it.
- [ ] **Step 2: Tier 3.** In `docs/ProjectState.md`, add a dated line under the M2 row: arrival by
  portal, extraction through it, castle sealed, outside removed; verified in the live Editor with the
  captures in `docs/generated/portal-arrival-2026-09-24/`.
- [ ] **Step 3: Tier 5.** Add a dated entry at the top of `docs/Today.md` saying what changed and
  linking the captures.
- [ ] **Step 4: Commit**

```bash
git add docs/systems/raid.md docs/systems/castle.md docs/ProjectState.md docs/Today.md
git commit -m "docs: raids arrive and leave by portal"
git push
```
