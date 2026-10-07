# Multi-floor castle, step 1: floors with ramp stairs, implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Generated castles get a keep floor above and a crypt floor below the ground floor, joined by placeholder ramp stairs, and guards and players can walk every floor (#247, part of #197).

**Architecture:**
- A level is a third grid coordinate. `PlacedModule` gains `Level`, `Storeys`, `ExitLevel` and `ExitFacing`, so existing `GridPosition` users keep compiling.
- A new pure `CastleFloorPlanner` decides which room goes on which cell and level. The generator places what it plans, at each level's height.
- The walk map registers each module at every level it spans, offsets tile heights by the module's height, and joins neighbours by matching floor heights across tile layers.
- Stairs are two primitive-built placeholder prefabs (a ramp in a two-storey box), made by an Editor forge and baked like any room.

**Tech Stack:** Unity 6000.3.15f1, C#, NUnit (EditMode and PlayMode), PurrNet (unchanged), the `unity` CLI through `Tools/Unity/*.sh`.

**Spec:** [`docs/plans/multi-floor-castle.md`](multi-floor-castle.md), sections 1, 2, 4.1-4.4 and 4.6. Concept sheets: `docs/art/castle/concept/`.

## Global Constraints

- Ground: a 5 × 5 interior inside a radius-3 curtain wall. Ring 2 is OuterBailey (16 cells, courtyards carved only here), ring 1 is InnerWard (8), and the centre is the down-stair.
- Keep (level 1): the inner 3 × 3, module roots at **y = 4.30**, floor top at **+4.60**.
- Crypt (level −1): the inner 3 × 3, module roots at **y = −3.30**, floor top at **−3.00**.
- Up-stairs: the ground cells **(−1, 0) and (1, 0)**, spanning levels 0-1. The stair head opens only toward the keep centre.
- Down-stair: the ground cell **(0, 0)**, spanning levels −1 to 0. The foot opens only **north** into the crypt.
- Final chamber: one of the 8 crypt cells around the centre, chosen by the seed's RNG.
- Only the seed crosses the network. Generation stays a pure function of the seed: no `UnityEngine.Random`, no hash-order iteration.
- Code style (`csharp-unity-standards`):
  - Allman braces; `_camelCase` private fields; one class per file;
  - comments say why, not what;
  - small single-job classes, no new file over about 200 lines.
- Unity procedure (`Tools/Unity/README.md`):
  - before any recompile or test run, check `bash Tools/Unity/eval.sh 'return UnityEditor.EditorApplication.isPlaying + " " + UnityEditor.EditorApplication.isCompiling;'` prints `False False`;
  - after a compile, `git diff --ignore-cr-at-eol --stat Assets/_Project/Net/NetworkPrefabs.asset` must print nothing.
- At most one co-op run per change, and one full PlayMode suite at the end (about 10 min). If a measuring script misbehaves, report it rather than iterate on it.
- Commit after each task on `claude/playability-fixes`, scoped to the task's files. The message ends with `Committed by AJ's agent` and references `#247`.

## File map

| File | Status | Job |
|---|---|---|
| `Runtime/Castle/CastleLevels.cs` | create | level numbers and the height each level's modules stand at |
| `Runtime/Castle/ProceduralCastleData.cs` | modify | `PlacedModule` gains `Level`, `Storeys`, `ExitLevel`, `ExitFacing`, `TopLevel` |
| `Runtime/Castle/CastleStairRule.cs` | create | whether a stair module opens toward a neighbour on a level |
| `Runtime/Castle/CastleFloorPlanner.cs` | create | pure: which room kind and zone goes on each (cell, level) |
| `Runtime/Castle/PlannedRoom.cs` | create | one planned room (a readonly struct) |
| `Runtime/Castle/ProceduralCastleGenerator.cs` | modify | place planned rooms at their level; level-aware sealing; radius fixed at 3 |
| `Runtime/Castle/CastleRoomRegistry.cs` | modify | `Stairs` list, `UpperFloorHeight`, `GetById` searches stairs |
| `Editor/CastleStairPlaceholderForge.cs` | create | builds the two ramp prefabs and registers them in all three registries |
| `Editor/CastleNavTileBaker.cs` | modify | bakes stair entries; seeds archways on the upper floor too |
| `Runtime/Castle/Navigation/CastleNavGrid.cs` | modify | module lookup per (x, y, level); column scan over levels |
| `Runtime/Castle/Navigation/CastleNavStitcher.cs` | modify | tile heights offset by module height; joins per level, across layers |
| `Runtime/Castle/Navigation/CastleNavArchwayRule.cs` | modify | `IsOpen` takes the level and applies the stair rule |
| `Runtime/Castle/CastlePathValidator.cs` | modify | module-graph search across levels instead of a flat raster (approved replacement) |
| `Runtime/Castle/CastleEntrancePlanner.cs`, `CastleDressingPlanner.cs`, `CastleArrivalPlanner.cs`, `Raid/RaidDirector.cs:438`, `Raid/GuardPlacementPlanner.cs`, loot planner | modify | ground-only and no-stair filters |
| `Tests/Editor/CastleFloorPlannerTests.cs` | create | planner rules |
| `Tests/Runtime/CastleGeneratorTests.cs` | modify | counts per level, stairs, level-aware adjacency |
| `Tests/Editor/CastleNavGraphTests.cs` | modify | stacked lookup, stairs join floors |
| `Tools/Unity/eval/stair_survey.cs`, `Tools/Unity/coop_floors_check.sh` | modify / create | the real-castle checks |

All paths under `Assets/_Project/Scripts/` unless they start with `Tools/` or `docs/`.

---

### Task 1: Levels in the layout data

**Files:**
- Create: `Runtime/Castle/CastleLevels.cs`, `Runtime/Castle/CastleStairRule.cs`
- Modify: `Runtime/Castle/ProceduralCastleData.cs` (`PlacedModule`, lines 48-68)
- Test: `Tests/Editor/CastleStairRuleTests.cs`

**Interfaces:**
- Produces: `CastleLevels.Crypt/Ground/Keep/Lowest/Count`, `CastleLevels.RootY(int level)`, `CastleLevels.Index(int level)`;
  `PlacedModule.Level`, `.Storeys`, `.ExitLevel`, `.ExitFacing`, `.TopLevel`; `CastleStairRule.Opens(PlacedModule module, int level, Vector2Int toward)`.

- [ ] **Step 1: Write the failing test** (`Tests/Editor/CastleStairRuleTests.cs`)

```csharp
using NUnit.Framework;
using Plunderspell.Castle;
using UnityEngine;

namespace Plunderspell.Tests.Editor
{
    /// <summary>A stair module opens one way on its exit level and like any room on its lobby level (#247).</summary>
    public class CastleStairRuleTests
    {
        private static ProceduralCastleData.PlacedModule UpStair()
        {
            return new ProceduralCastleData.PlacedModule("StairUp", Vector3.zero, Quaternion.identity,
                CastleZone.InnerWard, new Vector2Int(-1, 0))
            {
                Level = CastleLevels.Ground, Storeys = 2, ExitLevel = CastleLevels.Keep, ExitFacing = Vector2Int.right
            };
        }

        [Test]
        public void ExitLevel_OpensOnlyTowardItsFacing()
        {
            var stair = UpStair();
            Assert.IsTrue(CastleStairRule.Opens(stair, CastleLevels.Keep, Vector2Int.right));
            Assert.IsFalse(CastleStairRule.Opens(stair, CastleLevels.Keep, Vector2Int.up));
            Assert.IsFalse(CastleStairRule.Opens(stair, CastleLevels.Keep, Vector2Int.left));
        }

        [Test]
        public void LobbyLevel_OpensEveryWay_AndAnOrdinaryRoomAlways()
        {
            var stair = UpStair();
            Assert.IsTrue(CastleStairRule.Opens(stair, CastleLevels.Ground, Vector2Int.left));
            var room = new ProceduralCastleData.PlacedModule("Hall", Vector3.zero, Quaternion.identity,
                CastleZone.Keep, Vector2Int.zero) { Level = CastleLevels.Keep };
            Assert.IsTrue(CastleStairRule.Opens(room, CastleLevels.Keep, Vector2Int.down));
            Assert.AreEqual(CastleLevels.Keep, room.TopLevel);
            Assert.AreEqual(CastleLevels.Keep, stair.TopLevel);
        }

        [Test]
        public void LevelHeights_MatchTheDesign()
        {
            Assert.AreEqual(0f, CastleLevels.RootY(CastleLevels.Ground));
            Assert.AreEqual(4.3f, CastleLevels.RootY(CastleLevels.Keep), 1e-4f);
            Assert.AreEqual(-3.3f, CastleLevels.RootY(CastleLevels.Crypt), 1e-4f);
            Assert.AreEqual(0, CastleLevels.Index(CastleLevels.Crypt));
            Assert.AreEqual(2, CastleLevels.Index(CastleLevels.Keep));
        }
    }
}
```

- [ ] **Step 2: Run it to see it fail to compile.** Run `bash Tools/Unity/recompile.sh`. Expected: compile errors naming `CastleLevels`, `CastleStairRule`, `Level`.

- [ ] **Step 3: Write the code.**

`Runtime/Castle/CastleLevels.cs`:

```csharp
namespace Plunderspell.Castle
{
    /// <summary>
    /// The castle's storeys (#247, docs/plans/multi-floor-castle.md). A module's root sits at the bottom of its
    /// 0.30 m floor slab. The keep's slab rests on the inner ward's wall tops (0.30 + 4.00); the crypt's 3.00 m
    /// rooms end at the underside of the ground slab.
    /// </summary>
    public static class CastleLevels
    {
        public const int Crypt = -1;
        public const int Ground = 0;
        public const int Keep = 1;
        public const int Lowest = Crypt;
        public const int Count = 3;

        public const float KeepRootY = 4.3f;
        public const float CryptRootY = -3.3f;

        /// <summary>World height of the root of a module standing on <paramref name="level"/>.</summary>
        public static float RootY(int level) => level == Keep ? KeepRootY : level == Crypt ? CryptRootY : 0f;

        /// <summary>0-based index of a level, for arrays sized <see cref="Count"/>.</summary>
        public static int Index(int level) => level - Lowest;
    }
}
```

In `ProceduralCastleData.PlacedModule`, after `public bool IsCryptEntry;`:

```csharp
            /// <summary>The storey the module stands on (<see cref="CastleLevels"/>); its lowest one if it spans more.</summary>
            public int Level;
            /// <summary>How many storeys the module spans: 2 for a stair, 0 or 1 for a room.</summary>
            public int Storeys;
            /// <summary>For a stair, the level where it opens one way only (the head of an up-stair, the foot of a down-stair).</summary>
            public int ExitLevel;
            /// <summary>For a stair, the grid direction it opens toward on <see cref="ExitLevel"/>.</summary>
            public Vector2Int ExitFacing;

            /// <summary>The highest storey the module occupies.</summary>
            public int TopLevel => Level + Mathf.Max(1, Storeys) - 1;
```

The constructor is unchanged. Struct fields default to 0, which reads as "ground, one storey".

`Runtime/Castle/CastleStairRule.cs`:

```csharp
using UnityEngine;

namespace Plunderspell.Castle
{
    /// <summary>
    /// Which ways a module opens on a level (#247). Rooms open on all four sides. A stair opens like a room on
    /// its lobby level, and only toward <see cref="ProceduralCastleData.PlacedModule.ExitFacing"/> on its exit
    /// level, so each stair is one door's width at the zone change.
    /// </summary>
    public static class CastleStairRule
    {
        public static bool Opens(ProceduralCastleData.PlacedModule module, int level, Vector2Int toward)
        {
            if (module.Storeys <= 1 || level != module.ExitLevel)
                return true;
            return toward == module.ExitFacing;
        }
    }
}
```

- [ ] **Step 4: Run the tests.** Run `bash Tools/Unity/recompile.sh && bash Tools/Unity/run_tests.sh CastleStairRuleTests EditMode`. Expected: `total 3  passed 3`.

- [ ] **Step 5: Commit.** `git add` the three new files and `ProceduralCastleData.cs`, then commit `feat: castle modules carry a level, and stairs open one way on their exit level (#247)`.

---

### Task 2: The floor planner

**Files:**
- Create: `Runtime/Castle/PlannedRoom.cs`, `Runtime/Castle/CastleFloorPlanner.cs`
- Test: `Tests/Editor/CastleFloorPlannerTests.cs`

**Interfaces:**
- Consumes: `CastleLevels`.
- Produces:
  - `enum PlannedRoomKind { Room, StairUp, StairDown, FinalChamber }`;
  - `readonly struct PlannedRoom(Vector2Int cell, int level, CastleZone zone, PlannedRoomKind kind, Vector2Int exitFacing)`, with the properties `Cell, Level, Zone, Kind, ExitFacing`;
  - `CastleFloorPlanner.CurtainWallRadius = 3`, `CastleFloorPlanner.UpStairCells`, `CastleFloorPlanner.Plan(System.Random rng, float fillFraction, Vector2Int gateApproach) : List<PlannedRoom>`.

- [ ] **Step 1: Write the failing test** (`Tests/Editor/CastleFloorPlannerTests.cs`)

```csharp
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Plunderspell.Castle;
using UnityEngine;

namespace Plunderspell.Tests.Editor
{
    /// <summary>The stacked castle's floor plan (#247, docs/plans/multi-floor-castle.md section 1).</summary>
    public class CastleFloorPlannerTests
    {
        private static readonly Vector2Int GateApproach = new Vector2Int(2, 0);

        private static List<PlannedRoom> Plan(int seed) => CastleFloorPlanner.Plan(new System.Random(seed), 0.9f, GateApproach);

        [Test]
        public void EveryLevelHasItsRooms()
        {
            foreach (int seed in new[] { 1, 42, 777, 12345, -9 })
            {
                List<PlannedRoom> plan = Plan(seed);
                Assert.AreEqual(9 - 2, plan.Count(r => r.Level == CastleLevels.Keep), $"seed {seed}: keep rooms (the stair heads belong to the stairs)");
                Assert.AreEqual(9 - 1, plan.Count(r => r.Level == CastleLevels.Crypt), $"seed {seed}: crypt rooms (the centre is the stair's foot)");
                Assert.AreEqual(8, plan.Count(r => r.Level == CastleLevels.Ground && r.Zone == CastleZone.InnerWard), $"seed {seed}: ward ring incl. two stairs");
                int bailey = plan.Count(r => r.Zone == CastleZone.OuterBailey);
                Assert.That(bailey, Is.InRange(13, 16), $"seed {seed}: bailey rooms after courtyards");
                Assert.AreEqual(1, plan.Count(r => r.Kind == PlannedRoomKind.FinalChamber), $"seed {seed}: one final chamber");
            }
        }

        [Test]
        public void StairsSitWhereTheDesignPutsThem()
        {
            List<PlannedRoom> plan = Plan(42);
            var ups = plan.Where(r => r.Kind == PlannedRoomKind.StairUp).ToList();
            CollectionAssert.AreEquivalent(new[] { new Vector2Int(-1, 0), new Vector2Int(1, 0) }, ups.Select(u => u.Cell));
            foreach (PlannedRoom up in ups)
            {
                Assert.AreEqual(CastleLevels.Ground, up.Level);
                Assert.AreEqual(-up.Cell, up.ExitFacing, "an up-stair's head opens toward the keep centre");
            }
            PlannedRoom down = plan.Single(r => r.Kind == PlannedRoomKind.StairDown);
            Assert.AreEqual(Vector2Int.zero, down.Cell);
            Assert.AreEqual(CastleLevels.Crypt, down.Level);
            Assert.AreEqual(Vector2Int.up, down.ExitFacing, "the foot opens north");
        }

        [Test]
        public void FinalChamberIsACryptRingCell_AndThePlanIsDeterministic()
        {
            PlannedRoom final = Plan(777).Single(r => r.Kind == PlannedRoomKind.FinalChamber);
            Assert.AreEqual(CastleLevels.Crypt, final.Level);
            Assert.AreEqual(1, Mathf.Max(Mathf.Abs(final.Cell.x), Mathf.Abs(final.Cell.y)));
            CollectionAssert.AreEqual(Plan(777).Select(r => (r.Cell, r.Level, r.Kind)), Plan(777).Select(r => (r.Cell, r.Level, r.Kind)));
        }

        [Test]
        public void CourtyardsAreOnlyBaileyCells_AndNeverTheGateApproach()
        {
            foreach (int seed in new[] { 1, 42, 777, 12345, -9 })
            {
                var ground = new HashSet<Vector2Int>(Plan(seed).Where(r => r.Level == CastleLevels.Ground).Select(r => r.Cell));
                for (int x = -1; x <= 1; x++)
                    for (int y = -1; y <= 1; y++)
                        Assert.IsTrue(ground.Contains(new Vector2Int(x, y)), $"seed {seed}: inner cell {x},{y} carved");
                Assert.IsTrue(ground.Contains(GateApproach), $"seed {seed}: the gate approach was carved");
            }
        }
    }
}
```

- [ ] **Step 2: Run it to see it fail to compile.** Run `bash Tools/Unity/recompile.sh`. Expected: errors naming `PlannedRoom` and `CastleFloorPlanner`.

- [ ] **Step 3: Write the code.**

`Runtime/Castle/PlannedRoom.cs`:

```csharp
using UnityEngine;

namespace Plunderspell.Castle
{
    public enum PlannedRoomKind { Room, StairUp, StairDown, FinalChamber }

    /// <summary>One room the floor planner decided on: where, which storey, which zone, what kind.</summary>
    public readonly struct PlannedRoom
    {
        public PlannedRoom(Vector2Int cell, int level, CastleZone zone, PlannedRoomKind kind, Vector2Int exitFacing)
        {
            Cell = cell;
            Level = level;
            Zone = zone;
            Kind = kind;
            ExitFacing = exitFacing;
        }

        public Vector2Int Cell { get; }
        public int Level { get; }
        public CastleZone Zone { get; }
        public PlannedRoomKind Kind { get; }
        /// <summary>For a stair: the direction it opens toward on its exit level. Zero otherwise.</summary>
        public Vector2Int ExitFacing { get; }
    }
}
```

`Runtime/Castle/CastleFloorPlanner.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Castle
{
    /// <summary>
    /// Decides the stacked castle's rooms (#247, docs/plans/multi-floor-castle.md section 1): a 5 × 5 ground floor
    /// (bailey ring, inner-ward ring, a down-stair at the centre), a 3 × 3 keep above, a 3 × 3 crypt below.
    /// Pure and seeded, so every peer plans the same castle. Courtyards come only from the bailey ring.
    /// Order is fixed (crypt, ground, keep; inner ring first; then x, then y) so placement indices depend on
    /// the seed alone.
    /// </summary>
    public static class CastleFloorPlanner
    {
        public const int CurtainWallRadius = 3;

        public static readonly Vector2Int[] UpStairCells = { new Vector2Int(-1, 0), new Vector2Int(1, 0) };

        public static List<PlannedRoom> Plan(System.Random rng, float fillFraction, Vector2Int gateApproach)
        {
            var plan = new List<PlannedRoom>();
            PlanCrypt(plan, rng);
            PlanGround(plan, rng, fillFraction, gateApproach);
            PlanKeep(plan);
            return plan;
        }

        private static void PlanCrypt(List<PlannedRoom> plan, System.Random rng)
        {
            List<Vector2Int> ring = Ring(1);
            Vector2Int final = ring[rng.Next(0, ring.Count)];
            plan.Add(new PlannedRoom(Vector2Int.zero, CastleLevels.Crypt, CastleZone.InnerWard, PlannedRoomKind.StairDown, Vector2Int.up));
            foreach (Vector2Int cell in ring)
            {
                PlannedRoomKind kind = cell == final ? PlannedRoomKind.FinalChamber : PlannedRoomKind.Room;
                plan.Add(new PlannedRoom(cell, CastleLevels.Crypt, CastleZone.Crypt, kind, Vector2Int.zero));
            }
        }

        private static void PlanGround(List<PlannedRoom> plan, System.Random rng, float fillFraction, Vector2Int gateApproach)
        {
            foreach (Vector2Int cell in Ring(1))
            {
                bool up = System.Array.IndexOf(UpStairCells, cell) >= 0;
                plan.Add(new PlannedRoom(cell, CastleLevels.Ground, CastleZone.InnerWard,
                    up ? PlannedRoomKind.StairUp : PlannedRoomKind.Room, up ? -cell : Vector2Int.zero));
            }
            foreach (Vector2Int cell in KeptBailey(rng, fillFraction, gateApproach))
                plan.Add(new PlannedRoom(cell, CastleLevels.Ground, CastleZone.OuterBailey, PlannedRoomKind.Room, Vector2Int.zero));
        }

        // The stair heads at (±1, 0) belong to the up-stair modules below them.
        private static void PlanKeep(List<PlannedRoom> plan)
        {
            var cells = new List<Vector2Int> { Vector2Int.zero };
            cells.AddRange(Ring(1));
            foreach (Vector2Int cell in cells)
            {
                if (System.Array.IndexOf(UpStairCells, cell) < 0)
                    plan.Add(new PlannedRoom(cell, CastleLevels.Keep, CastleZone.Keep, PlannedRoomKind.Room, Vector2Int.zero));
            }
        }

        // Carves courtyards from the bailey ring where the ground stays one 4-connected region.
        private static List<Vector2Int> KeptBailey(System.Random rng, float fillFraction, Vector2Int gateApproach)
        {
            List<Vector2Int> bailey = Ring(2);
            var ground = new HashSet<Vector2Int>(bailey) { Vector2Int.zero };
            foreach (Vector2Int cell in Ring(1))
                ground.Add(cell);
            var order = new List<Vector2Int>(bailey);
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
            int target = Mathf.RoundToInt(bailey.Count * (1f - fillFraction));
            int carved = 0;
            for (int i = 0; i < order.Count && carved < target; i++)
            {
                if (order[i] == gateApproach)
                    continue;
                ground.Remove(order[i]);
                if (IsConnected(ground))
                    carved++;
                else
                    ground.Add(order[i]);
            }
            var kept = new List<Vector2Int>();
            foreach (Vector2Int cell in bailey)
            {
                if (ground.Contains(cell))
                    kept.Add(cell);
            }
            return kept;
        }

        /// <summary>A ring's cells in a fixed order: x, then y.</summary>
        private static List<Vector2Int> Ring(int radius)
        {
            var cells = new List<Vector2Int>();
            for (int x = -radius; x <= radius; x++)
                for (int y = -radius; y <= radius; y++)
                    if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) == radius)
                        cells.Add(new Vector2Int(x, y));
            return cells;
        }

        private static bool IsConnected(HashSet<Vector2Int> cells)
        {
            var seen = new HashSet<Vector2Int> { Vector2Int.zero };
            var frontier = new Queue<Vector2Int>(seen);
            Vector2Int[] dirs = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
            while (frontier.Count > 0)
            {
                Vector2Int current = frontier.Dequeue();
                foreach (Vector2Int dir in dirs)
                {
                    if (cells.Contains(current + dir) && seen.Add(current + dir))
                        frontier.Enqueue(current + dir);
                }
            }
            return seen.Count == cells.Count;
        }
    }
}
```

The down-stair is planned once, at its lowest level (the crypt). Its zone is InnerWard because its lobby is a ward room on the ground.

- [ ] **Step 4: Run the tests.** Run `bash Tools/Unity/recompile.sh && bash Tools/Unity/run_tests.sh CastleFloorPlannerTests EditMode`. Expected: `total 4  passed 4`.

- [ ] **Step 5: Commit.** `git add` the three files, then commit `feat: a floor planner for the stacked castle: keep above, crypt below, stairs between (#247)`.

---

### Task 3: The generator places the planned floors

**Files:**
- Modify: `Runtime/Castle/ProceduralCastleGenerator.cs`:
  - replace `BuildInterior`, `InteriorCells`, `ZoneForRing` and `IsSingleConnectedRegion` (lines 127-241);
  - modify `PlaceModule`, `InstantiateModule`, `SealOpenArchways`, `IsArchwayConnected`, `InstantiateDoorPlug` and the occupancy dictionary;
  - remove the `m_curtainWallRadius` field.
- Modify: `Tests/Runtime/CastleGeneratorTests.cs` (`Test_InteriorRoomCountIsPlayable` and `Test_AdjacentRoomsAreDoorConnected`)

**Interfaces:**
- Consumes: `CastleFloorPlanner.Plan`, `PlannedRoom`, `CastleLevels.RootY`, `CastleStairRule.Opens`.
- Produces: `CurtainWallRadius => CastleFloorPlanner.CurtainWallRadius`. Placed modules carry their `Level`, `Storeys`, `ExitLevel` and `ExitFacing`. The stair ids are the constants `ProceduralCastleGenerator.StairUpId = "StairUp"` and `StairDownId = "StairDown"`.

- [ ] **Step 1: Update the tests to the new layout (they will fail).** In `Tests/Runtime/CastleGeneratorTests.cs`:

Replace the body of `Test_InteriorRoomCountIsPlayable`'s assertion range and add per-level checks:

```csharp
                Assert.That(rooms, Is.InRange(36, 40),
                    $"seed {seed}: {rooms} interior modules (ground 25 less courtyards, keep 7, crypt 8, stairs counted once)");
                Assert.AreEqual(7, data.PlacedModules.Count(m => m.Level == CastleLevels.Keep), $"seed {seed}: keep rooms");
                Assert.AreEqual(9, data.PlacedModules.Count(m => m.Level == CastleLevels.Crypt), $"seed {seed}: crypt modules incl. the down-stair");
                Assert.AreEqual(3, data.PlacedModules.Count(m => m.Storeys == 2), $"seed {seed}: three stairs");
```

(Add `using System.Linq;` at the top.)

In `Test_AdjacentRoomsAreDoorConnected`, key the lookup by level:

```csharp
                var byCell = new Dictionary<(Vector2Int, int), CastleZone>();
                foreach (var pm in data.PlacedModules)
                    for (int level = pm.Level; level <= pm.TopLevel; level++)
                        byCell[(pm.GridPosition, level)] = pm.Zone;
```

and look neighbours up with `byCell.TryGetValue((pm.GridPosition + dir, pm.Level), out CastleZone neighbourZone)`.

- [ ] **Step 2: Run them to see them fail.** Run `bash Tools/Unity/recompile.sh && bash Tools/Unity/run_tests.sh CastleGeneratorTests PlayMode`. Expected: `Test_InteriorRoomCountIsPlayable` FAILS (about 44 rooms, no keep level).

- [ ] **Step 3: Change the generator.**

1. Delete the `[Range(3, 7)] [SerializeField] private int m_curtainWallRadius = 4;` field and its tooltip. Then:
   - replace every `m_curtainWallRadius` with `CurtainWallRadius`;
   - change the property to `public int CurtainWallRadius => CastleFloorPlanner.CurtainWallRadius;`.

   The scenes' stale serialized value 4 is then ignored.
2. Add `public const string StairUpId = "StairUp"; public const string StairDownId = "StairDown";` next to the other ids.
3. Occupancy: change `Dictionary<Vector2Int, int> occupied` to `Dictionary<Vector3Int, int>` (x, y, level) in `Generate`, `BuildCurtainWall`, `PlaceModule`, `SealOpenArchways` and `IsArchwayConnected`. The curtain wall uses level 0: `new Vector3Int(cell.x, cell.y, CastleLevels.Ground)`.
4. Replace `BuildInterior` (and delete `InteriorCells`, `ZoneForRing`, `IsSingleConnectedRegion`) with:

```csharp
        /// <summary>
        /// Places the floor planner's rooms (#247): crypt, ground, keep, in its order, so placement indices are
        /// a function of the seed alone. A stair is placed once, at its lowest level, and spans two.
        /// </summary>
        private void BuildFloors(ProceduralCastleData data, Dictionary<Vector3Int, int> occupied, System.Random rng)
        {
            Vector2Int gateApproach = k_GateOutward * (CurtainWallRadius - 1);
            foreach (PlannedRoom room in CastleFloorPlanner.Plan(rng, m_interiorFillFraction, gateApproach))
            {
                string roomId = room.Kind switch
                {
                    PlannedRoomKind.StairUp => ResolveRoomId(StairUpId),
                    PlannedRoomKind.StairDown => ResolveRoomId(StairDownId),
                    PlannedRoomKind.FinalChamber => ResolveRoomId(k_CryptFinalId),
                    _ => PickWeighted(registry != null ? registry.GetModulesForZone(room.Zone) : null, rng, room.Zone),
                };
                bool stair = room.Kind == PlannedRoomKind.StairUp || room.Kind == PlannedRoomKind.StairDown;
                // A stair's prefab is authored with its exit facing local north; a room faces inward as before.
                Quaternion rotation = stair ? RotationForFacing(room.ExitFacing) : RotationForFacing(InwardStep(room.Cell));
                var shape = new ModuleShape(room.Level, stair ? 2 : 1,
                    room.Kind == PlannedRoomKind.StairUp ? CastleLevels.Keep : room.Level, room.ExitFacing);
                int index = PlaceModule(data, occupied, roomId, room.Zone, room.Cell, rotation,
                    room.Kind == PlannedRoomKind.FinalChamber, shape);
                if (room.Kind == PlannedRoomKind.FinalChamber)
                    data.CryptStartIndex = index;
            }
        }

        /// <summary>Where a module stands in height and, for a stair, how it opens. Ground, one storey, by default.</summary>
        private readonly struct ModuleShape
        {
            public ModuleShape(int level, int storeys, int exitLevel, Vector2Int exitFacing)
            {
                Level = level; Storeys = storeys; ExitLevel = exitLevel; ExitFacing = exitFacing;
            }
            public static ModuleShape Ground => new ModuleShape(CastleLevels.Ground, 1, CastleLevels.Ground, Vector2Int.zero);
            public int Level { get; }
            public int Storeys { get; }
            public int ExitLevel { get; }
            public Vector2Int ExitFacing { get; }
        }
```

   In `Generate`, call `BuildFloors(data, occupied, rng)` where `BuildInterior` was called.

   A down-stair's exit level is its own level (the crypt foot); an up-stair's is the keep. That's the `ExitLevel` argument above.
5. `PlaceModule` gains `ModuleShape shape` as its last parameter. Every curtain call passes `ModuleShape.Ground`. Its body becomes:

```csharp
            var worldPos = new Vector3(cell.x * cellSize, CastleLevels.RootY(shape.Level), cell.y * cellSize);
            var placed = new ProceduralCastleData.PlacedModule(roomId, worldPos, rotation, zone, cell)
            {
                IsCryptEntry = isCryptEntry,
                Level = shape.Level,
                Storeys = shape.Storeys,
                ExitLevel = shape.ExitLevel,
                ExitFacing = shape.ExitFacing,
            };
            int index = data.PlacedModules.Count;
            data.PlacedModules.Add(placed);
            for (int level = placed.Level; level <= placed.TopLevel; level++)
                occupied[new Vector3Int(cell.x, cell.y, level)] = index;
            InstantiateModule(roomId, zone, cell, worldPos, rotation, isCryptEntry);
            return index;
```

6. `SealOpenArchways`: seal per level, skipping a stair's exit level (its prefab walls are already closed):

```csharp
            for (int i = 0; i < data.PlacedModules.Count; i++)
            {
                ProceduralCastleData.PlacedModule module = data.PlacedModules[i];
                if (!IsEnclosedRoom(module.Zone))
                    continue;
                for (int level = module.Level; level <= module.TopLevel; level++)
                {
                    if (module.Storeys > 1 && level == module.ExitLevel)
                        continue;
                    // A stair's lobby is a ward room; any other module seals with its own zone's plug.
                    GameObject plug = registry.GetDoorPlugForZone(module.Zone);
                    if (plug == null)
                        continue;
                    foreach (Vector2Int dir in FourDirs)
                    {
                        if (IsArchwayConnected(data, occupied, module.GridPosition + dir, level, -dir))
                            continue;
                        InstantiateDoorPlug(plug, module, dir, level);
                    }
                }
            }
```

7. `IsArchwayConnected(data, occupied, Vector2Int neighbour, int level, Vector2Int towardMe)`: the gate and entrance rules apply on the ground only. A stair neighbour connects only where it opens toward this room:

```csharp
            if (level == CastleLevels.Ground && neighbour == k_GateOutward * CurtainWallRadius)
                return true;
            if (level == CastleLevels.Ground && data.EntranceCells.Contains(neighbour))
                return true;
            if (!occupied.TryGetValue(new Vector3Int(neighbour.x, neighbour.y, level), out int index))
                return false;
            ProceduralCastleData.PlacedModule other = data.PlacedModules[index];
            return IsEnclosedRoom(other.Zone) && CastleStairRule.Opens(other, level, towardMe);
```

8. `InstantiateDoorPlug(plug, module, dir, int level)`: the position's height is the level's floor top, not the module's root:

```csharp
            Vector3 position = new Vector3(module.Position.x, CastleLevels.RootY(level), module.Position.z)
                               + new Vector3(dir.x, 0f, dir.y) * k_ArchwayInset
                               + Vector3.up * k_FloorThickness;
```

   and add `_L{level}` to the plug's name.
9. Update the class summary: the castle is now a radius-3 curtain wall round a stacked 5 × 5 / 3 × 3 / 3 × 3 block (keep the "only the seed is replicated" paragraph).

- [ ] **Step 4: Run the generator tests.** Run `bash Tools/Unity/recompile.sh && bash Tools/Unity/run_tests.sh CastleGeneratorTests PlayMode`. Expected: all pass except `Test_PathValidatorFindsPath` if it now fails. The validator is fixed in Task 6; note the result.

- [ ] **Step 5: Commit.** Commit `ProceduralCastleGenerator.cs` and `CastleGeneratorTests.cs` as `feat: the generator stacks the castle: crypt, ground and keep floors at their heights (#247)`.

---

### Task 4: Placeholder stair prefabs, registered and baked

**Files:**
- Modify: `Runtime/Castle/CastleRoomRegistry.cs`
- Create: `Editor/CastleStairPlaceholderForge.cs`
- Modify: `Editor/CastleNavTileBaker.cs`
- Test: `Tests/Editor/CastleStairPlaceholderTests.cs`

**Interfaces:**
- Produces:
  - `CastleRoomRegistry.Stairs` (a `List<CastleRoomModuleData>`); `GetById` also searches `Stairs`;
  - `CastleRoomModuleData.UpperFloorHeight` (a float, 0 = one storey);
  - prefabs `Assets/_Project/Prefabs/Castle/StairUpPlaceholder.prefab` and `StairDownPlaceholder.prefab`, with RoomIds `StairUp` and `StairDown`;
  - the menu item `Tools/Plunderspell/Forge Stair Placeholders`.

- [ ] **Step 1: Write the failing test** (`Tests/Editor/CastleStairPlaceholderTests.cs`)

```csharp
using NUnit.Framework;
using Plunderspell.Castle;
using UnityEditor;

namespace Plunderspell.Tests.Editor
{
    /// <summary>Each Age's registry knows the two placeholder stairs, baked over both storeys (#247).</summary>
    public class CastleStairPlaceholderTests
    {
        [TestCase("CastleRoomRegistry")]
        [TestCase("CastleRoomRegistry_BronzeAge")]
        [TestCase("CastleRoomRegistry_LateMedieval")]
        public void RegistryHasBakedStairs(string name)
        {
            var registry = AssetDatabase.LoadAssetAtPath<CastleRoomRegistry>($"Assets/_Project/Data/Castle/{name}.asset");
            foreach (string id in new[] { ProceduralCastleGenerator.StairUpId, ProceduralCastleGenerator.StairDownId })
            {
                CastleRoomModuleData stair = registry.GetById(id);
                Assert.IsNotNull(stair, $"{name}: no {id}");
                Assert.IsNotNull(stair.Prefab, $"{name}: {id} has no prefab");
                Assert.IsTrue(stair.NavTile.IsBaked, $"{name}: {id} is not baked");
                Assert.IsTrue(HasCellNear(stair.NavTile, 0.30f), $"{name}: {id} has no walkable cell on its lower floor");
                Assert.IsTrue(HasCellNear(stair.NavTile, stair.UpperFloorHeight), $"{name}: {id} has no walkable cell on its upper floor");
                Assert.IsEmpty(registry.GetModulesForZone(CastleZone.InnerWard).FindAll(m => m.RoomId == id),
                    $"{name}: {id} must not be in the random room pool");
            }
        }

        private static bool HasCellNear(CastleNavTile tile, float height)
        {
            for (int i = 0; i < tile.Walkable.Length; i++)
                if (tile.Walkable[i] != 0 && System.Math.Abs(tile.HeightCm[i] / CastleNavTile.HeightScale - height) < 0.05f)
                    return true;
            return false;
        }
    }
}
```

- [ ] **Step 2: Run it to see it fail.** Run `bash Tools/Unity/recompile.sh`. Expected: compile errors naming `UpperFloorHeight`.

- [ ] **Step 3: Write the registry change.** In `CastleRoomModuleData`, after `Weight`:

```csharp
        [Tooltip("For a module spanning two storeys (a stair): the local height of its upper floor's top. " +
                 "0 for a one-storey room. The nav tile baker seeds archways on this floor too.")]
        public float UpperFloorHeight;
```

In `CastleRoomRegistry`, after `DoorPlugs`:

```csharp
        [Tooltip("Stair modules (#247): placed by the floor planner at fixed cells, never picked at random, so " +
                 "kept out of Modules. Written by Tools/Plunderspell/Forge Stair Placeholders.")]
        public List<CastleRoomModuleData> Stairs = new List<CastleRoomModuleData>();
```

and make `GetById` search `Stairs` after `Modules`, with the same loop.

- [ ] **Step 4: Write the forge** (`Editor/CastleStairPlaceholderForge.cs`). It builds the two prefabs from cube primitives (cubes keep their `BoxCollider`), adds `CastleRoomModule`, saves them under `Assets/_Project/Prefabs/Castle/`, and puts or replaces an entry in each registry's `Stairs`. Geometry is in module-local metres; the root sits at the bottom of the lowest slab, and the exit faces local +Z.

```csharp
using System.Collections.Generic;
using Plunderspell.Castle;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Builds the placeholder stairs for the stacked castle (#247): a two-storey box with a ramp, until the period
    /// stairs are modelled (#249). Up: a ward room (archways all round) with a ramp from 0.30 to the keep floor at
    /// 4.60, and a head floor that opens only north. Down: a crypt room that opens only north, with a ramp up to a
    /// ward room at 3.60 (archways all round). Numbers: docs/plans/multi-floor-castle-step1-plan.md, Task 4.
    /// </summary>
    public static class CastleStairPlaceholderForge
    {
        private const string PrefabDir = "Assets/_Project/Prefabs/Castle";
        private static readonly string[] Registries = { "CastleRoomRegistry", "CastleRoomRegistry_BronzeAge", "CastleRoomRegistry_LateMedieval" };
        private const float Half = 6f, Wall = 0.5f, In = Half - Wall, Slab = 0.3f, ArchHalf = 1.3f;

        [MenuItem("Tools/Plunderspell/Forge Stair Placeholders")]
        public static void Forge()
        {
            GameObject up = BuildUp();
            GameObject down = BuildDown();
            foreach (string name in Registries)
            {
                var registry = AssetDatabase.LoadAssetAtPath<CastleRoomRegistry>($"Assets/_Project/Data/Castle/{name}.asset");
                Register(registry, ProceduralCastleGenerator.StairUpId, up, 4.6f);
                Register(registry, ProceduralCastleGenerator.StairDownId, down, 3.6f);
                EditorUtility.SetDirty(registry);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[StairForge] Built StairUp and StairDown placeholders and registered them in 3 registries. Bake the nav tiles next.");
        }

        private static GameObject BuildUp()
        {
            var root = new GameObject("StairUpPlaceholder");
            Box(root, "LobbySlab", -Half, 0f, -Half, Half, Slab, Half);
            Walls(root, "Lobby", Slab, Slab + 4.0f, 2.88f, allSides: true);                 // ward room, 4.00 clear
            // Head floor at 4.30-4.60 with a well over the ramp's upper part (x -1.5..4.4, z 1.4..3.8): from x -1.5 a guard on the ramp has 2.40 m under the slab edge (needs 2.30).
            Box(root, "HeadSlabW", -Half, 4.3f, -Half, -1.5f, 4.6f, Half);
            Box(root, "HeadSlabE", 4.4f, 4.3f, -Half, Half, 4.6f, Half);
            Box(root, "HeadSlabS", -1.5f, 4.3f, -Half, 4.4f, 4.6f, 1.4f);
            Box(root, "HeadSlabN", -1.5f, 4.3f, 3.8f, 4.4f, 4.6f, Half);
            Walls(root, "Head", 4.6f, 4.6f + 4.6f, 3.31f, allSides: false);                  // keep storey, opens north only
            Ramp(root, "Ramp", -5.0f, Slab, 4.4f, 4.6f, 1.6f, 3.6f);                         // along +X, z 1.6..3.6
            return Save(root, ProceduralCastleGenerator.StairUpId);
        }

        private static GameObject BuildDown()
        {
            var root = new GameObject("StairDownPlaceholder");
            Box(root, "FootSlab", -Half, 0f, -Half, Half, Slab, Half);
            Walls(root, "Foot", Slab, 3.3f, 2.16f, allSides: false);                        // crypt storey, opens north only
            // Lobby floor at 3.30-3.60 with a well over the ramp (x -2.0..3.9, z -3.8..-1.4): 2.40 m headroom at the slab edge (needs 2.30).
            Box(root, "LobbySlabW", -Half, 3.3f, -Half, -2.0f, 3.6f, Half);
            Box(root, "LobbySlabE", 3.9f, 3.3f, -Half, Half, 3.6f, Half);
            Box(root, "LobbySlabS", -2.0f, 3.3f, -Half, 3.9f, 3.6f, -3.8f);
            Box(root, "LobbySlabN", -2.0f, 3.3f, -1.4f, 3.9f, 3.6f, Half);
            Walls(root, "Lobby", 3.6f, 3.6f + 4.0f, 2.88f, allSides: true);                  // ward room, 4.00 clear
            Ramp(root, "Ramp", -3.3f, Slab, 3.9f, 3.6f, -3.6f, -1.6f);                       // along +X, z -3.6..-1.6
            return Save(root, ProceduralCastleGenerator.StairDownId);
        }

        // Four walls between y0 and y1; each has a centred archway of the given height, except that with
        // allSides false only the north (+Z) wall has one.
        private static void Walls(GameObject root, string name, float y0, float y1, float archHeight, bool allSides)
        {
            for (int side = 0; side < 4; side++)
            {
                bool open = allSides || side == 0;
                // side 0 north (+Z), 1 east (+X), 2 south (-Z), 3 west (-X)
                float sign = side < 2 ? 1f : -1f;
                bool alongX = side % 2 == 0;
                if (!open)
                {
                    WallBox(root, $"{name}Wall{side}", alongX, sign, -Half, Half, y0, y1);
                    continue;
                }
                WallBox(root, $"{name}Wall{side}a", alongX, sign, -Half, -ArchHalf, y0, y1);
                WallBox(root, $"{name}Wall{side}b", alongX, sign, ArchHalf, Half, y0, y1);
                WallBox(root, $"{name}Wall{side}Lintel", alongX, sign, -ArchHalf, ArchHalf, y0 + archHeight, y1);
            }
        }

        private static void WallBox(GameObject root, string name, bool alongX, float sign, float a0, float a1, float y0, float y1)
        {
            float face0 = sign > 0 ? In : -Half, face1 = sign > 0 ? Half : -In;
            if (alongX)
                Box(root, name, a0, y0, face0, a1, y1, face1);
            else
                Box(root, name, face0, y0, a0, face1, y1, a1);
        }

        private static void Box(GameObject root, string name, float x0, float y0, float z0, float x1, float y1, float z1)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(root.transform, false);
            cube.transform.localPosition = new Vector3((x0 + x1) / 2f, (y0 + y1) / 2f, (z0 + z1) / 2f);
            cube.transform.localScale = new Vector3(x1 - x0, y1 - y0, z1 - z0);
            cube.isStatic = true;
        }

        // A 0.3 m thick ramp rising along +X from (xFoot, yFoot) to (xHead, yHead), between z0 and z1.
        private static void Ramp(GameObject root, string name, float xFoot, float yFoot, float xHead, float yHead, float z0, float z1)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(root.transform, false);
            float run = xHead - xFoot, rise = yHead - yFoot, length = Mathf.Sqrt(run * run + rise * rise);
            float angle = Mathf.Atan2(rise, run) * Mathf.Rad2Deg;
            cube.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            // The top face runs from foot to head; the box hangs 0.15 m below that line.
            Vector3 mid = new Vector3((xFoot + xHead) / 2f, (yFoot + yHead) / 2f, (z0 + z1) / 2f);
            cube.transform.localPosition = mid - cube.transform.localRotation * new Vector3(0f, 0.15f, 0f);
            cube.transform.localScale = new Vector3(length, 0.3f, z1 - z0);
            cube.isStatic = true;
        }

        private static GameObject Save(GameObject root, string roomId)
        {
            var module = root.AddComponent<CastleRoomModule>();
            module.RoomId = roomId;
            module.Zone = CastleZone.InnerWard;
            string path = $"{PrefabDir}/{root.name}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void Register(CastleRoomRegistry registry, string roomId, GameObject prefab, float upperFloor)
        {
            registry.Stairs.RemoveAll(s => s != null && s.RoomId == roomId);
            registry.Stairs.Add(new CastleRoomModuleData
            {
                RoomId = roomId, Zone = CastleZone.InnerWard, Prefab = prefab, Weight = 1, UpperFloorHeight = upperFloor,
            });
        }
    }
}
```

- [ ] **Step 5: Teach the baker the stairs.** In `Editor/CastleNavTileBaker.cs`:
  1. Where it loops `registry.Modules` to bake, loop `registry.Stairs` the same way after it.
  2. Pass `entry.UpperFloorHeight` into `BakeModule` as `float upperFloor`.
  3. In the portal block, an edge column counts as a portal if its lowest surface **or** a surface within 0.1 m of `upperFloor` exists. Add the helper beside `LowestSurface`:

```csharp
        // The surface in a column whose height is within 0.1 m of a floor, or -1: a stair's upper floor
        // (#247) is an archway floor too, though it is not the column's lowest surface.
        private static int SurfaceAt(bool[] walk, float[] height, int x, int z, float floor)
        {
            if (floor <= 0f)
                return -1;
            for (int layer = 0; layer < Scratch; layer++)
            {
                int i = (layer * N + z) * N + x;
                if (walk[i] && Mathf.Abs(height[i] - floor) < 0.1f)
                    return i;
            }
            return -1;
        }
```

     Use it in the four portal conditions, e.g. `if (LowestSurface(walk, height, i, N - 1) >= 0 || SurfaceAt(walk, height, i, N - 1, upperFloor) >= 0) north.Add(...)`.
  4. When seeding `reached`, also push `SurfaceAt(..., upperFloor)` for each portal cell when it is `>= 0`.

- [ ] **Step 6: Run the forge and the bake, then the test.**

```bash
bash Tools/Unity/recompile.sh
bash Tools/Unity/eval.sh 'Plunderspell.EditorTools.CastleStairPlaceholderForge.Forge(); return "forged";'
bash Tools/Unity/eval.sh 'UnityEditor.EditorApplication.ExecuteMenuItem("Tools/Plunderspell/Bake Castle Nav Tiles"); return "baked";'
bash Tools/Unity/run_tests.sh CastleStairPlaceholderTests EditMode
```

Expected: `forged`, then `baked`, then `total 3  passed 3`.

If the bake menu item bakes only one registry, run it per registry the way `CastleNavTileBaker` documents. Check that `docs/generated/nav-tiles-2026-10-02/` gained `StairUp`/`StairDown` pictures. Open them: the up-stair's tile shows the ramp climbing to a head floor.

- [ ] **Step 7: Commit.** Commit the registry, forge, baker and test changes, plus the two prefabs, the three registry assets and the new tile pictures, as `feat: placeholder ramp stairs for the stacked castle, registered and baked over two storeys (#247)`.

---

### Task 5: The walk map spans floors

**Files:**
- Modify: `Runtime/Castle/Navigation/CastleNavGrid.cs` (constructor, `RegisterModules`, `ModuleAtGridCell`, `CellAt`, `ConsiderColumn`)
- Modify: `Runtime/Castle/Navigation/CastleNavStitcher.cs` (`LoadTiles`/`CopyCells`, `FindLinks`, `TryJoin`)
- Modify: `Runtime/Castle/Navigation/CastleNavArchwayRule.cs` (`IsOpen` gains `int level`)
- Test: `Tests/Editor/CastleNavGraphTests.cs` (add tests)

**Interfaces:**
- Consumes: `PlacedModule.Level/TopLevel`, `CastleLevels`, `CastleStairRule.Opens`, the baked stairs (Task 4).
- Produces: `CastleNavGrid.ModuleAtGridCell(int gridX, int gridY, int level = CastleLevels.Ground)`, `CastleNavGrid.CellAt(int castleColumn, int castleRow, int layer, int level = CastleLevels.Ground)`, `CastleNavArchwayRule.IsOpen(data, a, b, int level)`.

- [ ] **Step 1: Write the failing tests.** Add to `CastleNavGraphTests` (it already loads `CastleRoomRegistry` into `_generator` in `SetUp`):

```csharp
        [Test]
        public void AStackedColumn_FindsTheFloorYouStandOn()
        {
            ProceduralCastleData data = _generator.Generate(42);
            CastleNavGraph graph = data.NavGraph;
            int keep = graph.NearestWalkableCell(new Vector3(0f, 4.7f, 0f));
            int crypt = graph.NearestWalkableCell(new Vector3(0f, 0f, 12f) + Vector3.up * -2.9f);
            Assert.That(graph.CellPosition(keep).y, Is.EqualTo(4.6f).Within(0.05f), "a point on the keep floor found another floor");
            Assert.That(graph.CellPosition(crypt).y, Is.EqualTo(-3.0f).Within(0.05f), "a point in the crypt found another floor");
        }

        [Test]
        public void TheStairsJoinTheFloors()
        {
            foreach (int seed in new[] { 1, 42, 777 })
            {
                ProceduralCastleData data = _generator.Generate(seed);
                CastleNavGraph graph = data.NavGraph;
                // A bailey room the seed kept (courtyards have no tile, so a fixed cell could be one).
                Vector3 bailey = data.PlacedModules.First(m => m.Zone == CastleZone.OuterBailey).Position + Vector3.up * 0.4f;
                Vector3 keep = new Vector3(0f, 4.7f, 12f);                        // keep, north cell
                Vector3 crypt = data.PlacedModules[data.CryptStartIndex].Position + Vector3.up * 0.4f;
                Assert.IsTrue(graph.IsReachable(bailey, keep), $"seed {seed}: no walk from the bailey up to the keep");
                Assert.IsTrue(graph.IsReachable(bailey, crypt), $"seed {seed}: no walk from the bailey down to the final chamber");
            }
        }
```

(Add `using System.Linq;` to the test file if it is not there.)

- [ ] **Step 2: Run them to see them fail.** Run `bash Tools/Unity/recompile.sh && bash Tools/Unity/run_tests.sh CastleNavGraphTests EditMode`. Expected: both new tests FAIL. The keep point resolves to the ward room's cells or none, and the stairs aren't joined.

- [ ] **Step 3: Grid lookup per level.** In `CastleNavGrid`:
  - size `_moduleAtGridCell` as `_lookupWidth * _lookupHeight * CastleLevels.Count`;
  - register each module at every level it spans;
  - keep the x/z check as it is.

```csharp
                for (int level = pm.Level; level <= pm.TopLevel; level++)
                    _moduleAtGridCell[LookupIndex(pm.GridPosition.x - _lookupOrigin.x, pm.GridPosition.y - _lookupOrigin.y, level)] = module;
```

```csharp
        private int LookupIndex(int x, int y, int level) => (CastleLevels.Index(level) * _lookupHeight + y) * _lookupWidth + x;

        /// <summary>The module placed at a grid cell on a storey, or -1.</summary>
        public int ModuleAtGridCell(int gridX, int gridY, int level = CastleLevels.Ground)
        {
            int x = gridX - _lookupOrigin.x, y = gridY - _lookupOrigin.y;
            int index = CastleLevels.Index(level);
            if (x < 0 || y < 0 || x >= _lookupWidth || y >= _lookupHeight || index < 0 || index >= CastleLevels.Count)
                return -1;
            return _moduleAtGridCell[LookupIndex(x, y, level)];
        }
```

  `CellAt` gains `int level = CastleLevels.Ground` and passes it to `ModuleAtGridCell`.

  `ConsiderColumn` scans every level. A stair seen at two levels is scored once:

```csharp
            int lastModule = -1;
            for (int level = CastleLevels.Lowest; level < CastleLevels.Lowest + CastleLevels.Count; level++)
            {
                int module = ModuleAtGridCell(gridX, gridY, level);
                if (module < 0 || module == lastModule || !_moduleHasTile[module])
                    continue;
                lastModule = module;
                // (the existing dx, dz, and per-layer scoring, unchanged, for this module)
            }
```

- [ ] **Step 4: Heights, joins and the archway rule.**
  - `CastleNavStitcher.CopyCells(grid, module, tile, turns, short offsetCm)`: write `(short)(tile.HeightCm[source] + offsetCm)`. `LoadTiles` passes `(short)Mathf.RoundToInt(placed.Position.y * CastleNavTile.HeightScale)`, because tile heights are module-local.
  - `FindLinks`: for each module, for each `level` from `placed.Level` to `placed.TopLevel`, for each side:

```csharp
                        Vector2Int at = data.PlacedModules[module].GridPosition + CastleNavGrid.SideOffset(side);
                        int other = grid.ModuleAtGridCell(at.x, at.y, level);
                        // Each pair is still looked at once, from the lower index: no two multi-storey modules
                        // sit side by side on two shared levels in this layout, so a pair meets on one level only.
                        if (other <= module || !grid.HasTile(other) ||
                            !CastleNavArchwayRule.IsOpen(data, data.PlacedModules[module], data.PlacedModules[other], level))
                            continue;
                        if (TryJoin(grid, areas, module, side, other, out NavLink link))
                            links.Add(link);
```
  - `TryJoin`: try every layer pair at each archway column, so a stair head (layer 1) meets a keep room (layer 0):

```csharp
                for (int la = 0; la < CastleNavTile.Layers; la++)
                {
                    for (int lb = 0; lb < CastleNavTile.Layers; lb++)
                    {
                        int cell = CastleNavGrid.EdgeCell(module, side, along) + la * CastleNavGrid.ColumnsPerModule;
                        int across = CastleNavGrid.EdgeCell(other, otherSide, along) + lb * CastleNavGrid.ColumnsPerModule;
                        if (!grid.IsWalkable(cell) || !grid.IsWalkable(across) || !grid.IsStep(cell, across))
                            continue;
                        // (the existing areas.Join and middle-offset bookkeeping, recording cell/across)
                    }
                }
```

    The link uses the recorded `cell` and `across` of the middle pair, not a recomputed layer-0 `EdgeCell`.
  - `CastleNavArchwayRule.IsOpen(data, a, b, int level)`: room to room is `CastleStairRule.Opens(a, level, b.GridPosition - a.GridPosition) && CastleStairRule.Opens(b, level, a.GridPosition - b.GridPosition)`. The curtain rules apply only when `level == CastleLevels.Ground`. Find every other caller with `grep -rn "CastleNavArchwayRule.IsOpen" Assets/_Project/Scripts` and pass `CastleLevels.Ground` there.

- [ ] **Step 5: Run the navigation tests.** Run `bash Tools/Unity/recompile.sh && bash Tools/Unity/run_tests.sh Nav EditMode`. Expected: every test passes, including the two new ones (18 before this task, 20 now).

- [ ] **Step 6: Commit.** Commit the three navigation files and the test as `feat: the walk map spans floors: per-level lookup, module heights, joins across layers (#247)`.

---

### Task 6: The castle check walks the floors

**Files:**
- Modify: `Runtime/Castle/CastlePathValidator.cs` (replace the raster and A* in `ValidatePath`; keep the signature)
- Test: `Tests/Runtime/CastleGeneratorTests.cs` (add `Test_PathValidatorRejectsACutStair`)

**This replaces the rasteriser on purpose** (approved with this plan): a flat raster cannot tell floors apart, so it would always pass a stacked castle.

**Interfaces:**
- Produces: `CastlePathValidator.ValidatePath(ProceduralCastleData data, out List<Vector2Int> path)`, the same signature. `path` is now the module cells from the final chamber to the gate.

- [ ] **Step 1: Write the failing test.**

```csharp
        [Test]
        public void Test_PathValidatorRejectsACutStair()
        {
            var gen = MakeGenerator();
            ProceduralCastleData data = gen.Generate(42);
            Assert.IsTrue(CastlePathValidator.ValidatePath(data, out _), "seed 42 should be walkable");
            int final = data.CryptStartIndex;
            var chamber = data.PlacedModules[final];
            chamber.Level = CastleLevels.Keep + 5;              // move the final chamber off every floor
            data.PlacedModules[final] = chamber;
            Assert.IsFalse(CastlePathValidator.ValidatePath(data, out List<Vector2Int> path), "a final chamber no floor reaches must fail");
            Assert.AreEqual(0, path.Count);
        }
```

- [ ] **Step 2: Run it to see it fail.** Run `bash Tools/Unity/run_tests.sh CastleGeneratorTests PlayMode`. Expected: `Test_PathValidatorRejectsACutStair` FAILS (the raster still finds a path).

- [ ] **Step 3: Replace the search.** The new `ValidatePath` body: a breadth-first search over (cell, level) nodes. Keep the class summary's first sentence, and replace its raster description with this method's comment.

```csharp
        /// <summary>
        /// Whether the final chamber can be walked to the gate, floor by floor (#247): a breadth-first search over
        /// (cell, level). Rooms meet through archways where both open (<see cref="CastleStairRule"/>); a stair joins
        /// its own two levels; a room meets the curtain wall only at the gate or an entrance, on the ground.
        /// </summary>
        public static bool ValidatePath(ProceduralCastleData data, out List<Vector2Int> path)
        {
            path = new List<Vector2Int>();
            if (data?.PlacedModules == null || data.PlacedModules.Count == 0 ||
                data.CryptStartIndex < 0 || data.ExtractionExitIndex < 0)
                return false;

            var at = new Dictionary<(Vector2Int, int), int>();
            for (int i = 0; i < data.PlacedModules.Count; i++)
            {
                ProceduralCastleData.PlacedModule m = data.PlacedModules[i];
                for (int level = m.Level; level <= m.TopLevel; level++)
                    at[(m.GridPosition, level)] = i;
            }

            ProceduralCastleData.PlacedModule start = data.PlacedModules[data.CryptStartIndex];
            var from = new Dictionary<(Vector2Int, int), (Vector2Int, int)>();
            var queue = new Queue<(Vector2Int, int)>();
            var first = (start.GridPosition, start.Level);
            if (!at.ContainsKey(first))
                return false;
            from[first] = first;
            queue.Enqueue(first);
            Vector2Int[] dirs = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                ProceduralCastleData.PlacedModule here = data.PlacedModules[at[node]];
                if (at[node] == data.ExtractionExitIndex)
                {
                    for (var n = node; ; n = from[n])
                    {
                        path.Insert(0, n.Item1);
                        if (n.Equals(first))
                            break;
                    }
                    return true;
                }
                foreach (var next in Neighbours(data, at, node, here, dirs))
                {
                    if (from.ContainsKey(next))
                        continue;
                    from[next] = node;
                    queue.Enqueue(next);
                }
            }
            return false;
        }

        private static IEnumerable<(Vector2Int, int)> Neighbours(ProceduralCastleData data,
            Dictionary<(Vector2Int, int), int> at, (Vector2Int, int) node, ProceduralCastleData.PlacedModule here, Vector2Int[] dirs)
        {
            (Vector2Int cell, int level) = node;
            for (int other = here.Level; other <= here.TopLevel; other++)          // a stair joins its own storeys
                if (other != level)
                    yield return (cell, other);
            foreach (Vector2Int dir in dirs)
            {
                var next = (cell + dir, level);
                if (!at.TryGetValue(next, out int index))
                    continue;
                ProceduralCastleData.PlacedModule there = data.PlacedModules[index];
                if (CastleNavArchwayRule.IsOpen(data, here, there, level))
                    yield return next;
            }
        }
```

Delete the now-unused `CellScale` and the raster and A* helpers in the file. Check with `grep -rn "CastlePathValidator.CellScale" Assets/_Project/Scripts` that nothing else uses `CellScale`. If something does, keep it and report.

The test moves the final chamber to `Keep + 5`: no module stands on that level, so nothing reaches it.

- [ ] **Step 4: Run the generator tests.** Run `bash Tools/Unity/recompile.sh && bash Tools/Unity/run_tests.sh CastleGeneratorTests PlayMode`. Expected: all pass, `Test_PathValidatorFindsPath` included.

- [ ] **Step 5: Commit.** Commit `CastlePathValidator.cs` and `CastleGeneratorTests.cs` as `fix: the castle check searches floor by floor instead of a flat raster that a stacked castle always passed (#247)`.

---

### Task 7: The ground stays the ground

**Files:**
- Modify: `Runtime/Castle/CastleEntrancePlanner.cs:31`, `Runtime/Castle/CastleDressingPlanner.cs` (`Plan`), `Runtime/Castle/CastleArrivalPlanner.cs` (`ChooseModule`), `Runtime/Raid/RaidDirector.cs:438`, `Runtime/Raid/GuardPlacementPlanner.cs:100,137`, and the loot planner's room choice (`grep -n "PlacedModules" Runtime/Loot/LootPlacementPlanner.cs`)
- Test: `Tests/Runtime/CastleArrivalTests.cs` (run; adjust only radius literals if any assert 4)

**Interfaces:**
- Consumes: `PlacedModule.Level`, `.Storeys`.

- [ ] **Step 1: Run the arrival tests first, to see what the new layout breaks.** Run `bash Tools/Unity/run_tests.sh CastleArrivalTests PlayMode`. Note each failure.

- [ ] **Step 2: Add the filters.** In each place below, skip modules with `module.Level != CastleLevels.Ground` (the entrance planner's `zoneByCell`, the dressing planner's bailey loop, `RaidDirector` at 438, the arrival planner's module choice). Also skip any module with `module.Storeys > 1` in:
  - the arrival planner: a stair's root is in the crypt;
  - guard posts: a stair is a passage, not a post;
  - loot rooms: placeholder stairs have no anchors.

  In `GuardPlacementPlanner`, apply the safe-radius check only to ground modules: `module.Level == CastleLevels.Ground && ChebyshevDistance(...) <= SafeEntranceRadius`. Each filter gets a one-line why comment, e.g. `// Arrival is on the ground floor; a stair's root stands in the crypt (#247).`

- [ ] **Step 3: Run the arrival and generator tests.** Run `bash Tools/Unity/recompile.sh && bash Tools/Unity/run_tests.sh CastleArrivalTests PlayMode && bash Tools/Unity/run_tests.sh CastleGeneratorTests PlayMode`. Expected: all pass. If a test asserts the radius-4 layout (e.g. an entrance count), change only its literal to the radius-3 value and say so in the commit body.

- [ ] **Step 4: Commit** `fix: arrival, entrances, dressing, guards and loot read the ground floor; stairs are passages (#247)`.

---

### Task 8: Check the real castle: survey, co-op, suite

**Files:**
- Modify: `Tools/Unity/eval/stair_survey.cs`
- Create: `Tools/Unity/coop_floors_check.sh`
- Modify (docs): `docs/4-systems/castle.md` (Invariants, Navigation), `docs/3-state/ProjectState.md`, `docs/5-today/Today.md`

- [ ] **Step 1: Extend the stair survey.** After the raised-area loop in `stair_survey.cs`, add a per-seed stair line. For each module with `Storeys == 2`:
  - find the nearest cell to its lower floor (`Position + up * 0.4`) and its upper floor (`Position + up * (UpperFloorHeight + 0.1)`, where `UpperFloorHeight` comes from `registry.GetById(RoomId)`);
  - record `graph.IsReachable(lowerCell, upperCell)`;
  - print `"<registry> seed <s>: <RoomId> at <cell> joins its floors: <bool>"`.

  Run it as in Task 4's survey (enter Play, eval the file inline, stop). Save the output to `docs/generated/stair-survey-2026-10-03/floors.txt`. Expected: every line ends `True`.

- [ ] **Step 2: Write the co-op check.** Copy `Tools/Unity/coop_swarm_check.sh` to `Tools/Unity/coop_floors_check.sh` and keep its raid start unchanged. Replace the measuring block with:
  1. park the host on the keep floor: `bash Tools/Unity/view.sh 0 6.3 0 0 0`;
  2. `evf provoke` (every guard chases the host);
  3. every 5 s for 60 s, record the highest guard y: `ev 'return Plunderspell.Alarm.EnemyDirector.Current.Guards.Max(g => ((UnityEngine.Component)g).transform.position.y).ToString("0.0");'`;
  4. park the host in the final chamber, at `CryptStartIndex`'s position plus 1.7 m;
  5. `evf provoke` again, and record the lowest guard y the same way for 60 s.

  The script prints `PASS` when a guard reached y > 4.0 and later y < -2.0, otherwise `FAIL` with both extremes. Outputs go to `docs/generated/floors-2026-10-03/`.

- [ ] **Step 3: Run it once.** Run `bash Tools/Unity/coop_floors_check.sh first`. Expected: `PASS`, and the client log has no new error lines (the baseline is 2). Look at the two screenshots it saves (keep floor, crypt) before reporting.

- [ ] **Step 4: Run the full PlayMode suite once.** Run `bash Tools/Unity/run_tests.sh Plunderspell.Tests PlayMode`. Expected: all pass, except the known flaky #233 if it appears (name it).

- [ ] **Step 5: Docs.** In `docs/4-systems/castle.md`:
  - **Invariants:** replace "the interior stays one 4-connected region" with the floors version. Every floor is reached through the stairs, and the castle check walks (cell, level) (`CastlePathValidator`).
  - **Navigation:** add the per-level lookup, the module height offset, and joins across layers.

  In `ProjectState.md`, record #247 as in, with the run results. Add a `Today.md` entry.

- [ ] **Step 6: Commit** the scripts, outputs and docs as `test: stacked castle checked on the real walk map and in co-op; docs (#247)`, then push.

---

## Self-review notes (written with the plan)

- **Spec coverage:**

  | Spec section | Covered by |
  |---|---|
  | 1, layout | Tasks 2 and 3 |
  | 2, stairs (as placeholders) | Task 4 |
  | 4.1, data and generation | Tasks 1, 3 and 6 |
  | 4.2, walk map | Tasks 4 and 5 |
  | 4.3, placement and the raid | Task 7 |
  | 4.4, senses | no code change: sight and hearing need none; the co-op run in Task 8 exercises the hue and cry across floors |
  | 4.6, tests | Tasks 1-8 |

  Doors (3), stair art (4.5) and fog tuning are #248, #249 and #250, not this plan.
- **Ramp geometry, modelled 2026-10-03** against the baker's rules (normal ≥ 0.70, 0.45 m step, a 0.4 × 1.85 m guard capsule lifted 0.46 m): both ramps 24.6°, 0.23 m rise per cell, headroom at the well edge 2.40 m (up and down) after widening the wells (the first numbers left 2.28 m, a gap in the walk map). Access strips beside the wells hold 2-3 rows of cells.
- **Risk to watch:** the model is not the baker. Task 4's tile pictures and Task 8's survey are where a ramp that is too steep, or a head floor too narrow for a guard, would show. If either fails, fix the forge's numbers (`Ramp`, the well boxes) and re-bake, rather than loosening the walk map's step rule.
