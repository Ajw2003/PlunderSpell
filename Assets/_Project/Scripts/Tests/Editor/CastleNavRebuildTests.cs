using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.Castle;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Plunderspell.Tests.Editor
{
    /// <summary>
    /// The nav graph after the Ophan's beam breaks the castle (#478): a cleared cell leaves the map, a link added between
    /// two cut-off areas joins them, and a door's state survives the rebuild. The physics half
    /// (<see cref="CastleNavGraph.RefreshAfterBreak"/>) needs a live scene and is checked in play; these drive the same
    /// <see cref="CastleNavGraph.Rebuild"/> it ends with.
    /// </summary>
    public class CastleNavRebuildTests
    {
        private const string RegistryPath = "Assets/_Project/Data/Castle/CastleRoomRegistry.asset";

        private GameObject _generatorGo;
        private ProceduralCastleGenerator _generator;
        private CastleNavGraph _graph;
        private ProceduralCastleData _data;

        [SetUp]
        public void SetUp()
        {
            var registry = AssetDatabase.LoadAssetAtPath<CastleRoomRegistry>(RegistryPath);
            Assert.That(registry, Is.Not.Null);
            _generatorGo = new GameObject("NavRebuildTestGenerator");
            _generator = _generatorGo.AddComponent<ProceduralCastleGenerator>();
            _generator.Registry = registry;
            _data = _generator.Generate(12345);
            _graph = _data.NavGraph;
        }

        [TearDown]
        public void TearDown()
        {
            _generator.ClearGenerated();
            Object.DestroyImmediate(_generatorGo);
        }

        [Test]
        public void AClearedCellLeavesTheMapAfterARebuild()
        {
            int link = AnyLink(out int cellA, out int cellB);
            Vector3 at = _graph.CellPosition(cellA);
            int cells = _graph.WalkableCellCount;
            Assert.That(_graph.NearestWalkableCell(at, 0.2f), Is.EqualTo(cellA));

            _graph.ClearCell(cellA);
            _graph.Rebuild();

            Assert.That(_graph.WalkableCellCount, Is.EqualTo(cells - 1));
            Assert.That(_graph.NearestWalkableCell(at, 0.2f), Is.EqualTo(CastleNavGrid.NoCell), "a cleared cell was still found");
            var path = new List<Vector3>();
            Vector3 far = _graph.CellPosition(_graph.NearestWalkableCell(_data.PlacedModules[_data.CryptStartIndex].Position, 6f));
            if (_graph.FindPath(_graph.CellPosition(cellB), far, path))
                Assert.That(path, Has.None.EqualTo(at), "a path still crosses the cleared cell");
            Assert.That(link, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void ALinkAddedBetweenTwoCutOffRoomsMakesAPath()
        {
            AnyLink(out int cellA, out int cellB);
            int module = CastleNavGrid.ModuleOf(cellA);
            int interior = InteriorCell(module);
            Assert.That(interior, Is.GreaterThanOrEqualTo(0), "the room has no walkable inside");

            // Cut the room off on all four sides.
            for (int side = 0; side < 4; side++)
            {
                for (int along = 0; along < CastleNavGrid.TileSize; along++)
                {
                    for (int layer = 0; layer < CastleNavTile.Layers; layer++)
                        _graph.ClearCell(CastleNavGrid.EdgeCell(module, side, along) + layer * CastleNavGrid.ColumnsPerModule);
                }
            }
            _graph.Rebuild();
            Assert.That(_graph.IsWalkable(interior) && _graph.IsWalkable(cellB), Is.True);
            Assert.That(_graph.IsReachable(interior, cellB), Is.False, "the room is still joined after its edges were cleared");
            var path = new List<Vector3>();
            Assert.That(_graph.FindPath(_graph.CellPosition(interior), _graph.CellPosition(cellB), path), Is.False);

            int links = _graph.LinkCount;
            _graph.AddLink(interior, cellB);
            _graph.Rebuild();

            Assert.That(_graph.LinkCount, Is.EqualTo(links + 1));
            Assert.That(_graph.IsReachable(interior, cellB), Is.True, "the added link did not join the two areas");
            Assert.That(_graph.FindPath(_graph.CellPosition(interior), _graph.CellPosition(cellB), path), Is.True);
        }

        [Test]
        public void ADoorKeepsItsStateThroughARebuild()
        {
            int link = AnyLink(out int cellA, out _);
            Vector3 at = _graph.CellPosition(cellA);
            _graph.SetDoorCost(link, CastleNavGraph.ClosedDoor);
            int version = _graph.DoorVersion;

            _graph.Rebuild();

            Assert.That(_graph.DoorVersion, Is.GreaterThan(version), "routes planned before the rebuild must read as stale");
            int now = _graph.FindLinkNear(at, 1f);
            Assert.That(now, Is.GreaterThanOrEqualTo(0));
            Assert.That(_graph.DoorCost(now), Is.EqualTo(CastleNavGraph.ClosedDoor), "the closed door opened itself");
        }

        // A link between two rooms that each have cells on both sides, so clearing or isolating it means something.
        private int AnyLink(out int cellA, out int cellB)
        {
            foreach (ProceduralCastleData.PlacedModule placed in _data.PlacedModules)
            {
                int link = _graph.FindLinkNear(placed.Position, 8f);
                if (link >= 0 && _graph.TryGetLinkCells(link, out cellA, out cellB))
                    return link;
            }
            Assert.Fail("the castle has no links");
            cellA = cellB = CastleNavGrid.NoCell;
            return -1;
        }

        // A walkable cell well inside a room, away from the edge cells the tests clear.
        private int InteriorCell(int module)
        {
            for (int layer = 0; layer < CastleNavTile.Layers; layer++)
            {
                for (int row = 2; row < CastleNavGrid.TileSize - 2; row++)
                {
                    for (int column = 2; column < CastleNavGrid.TileSize - 2; column++)
                    {
                        int cell = module * CastleNavGrid.CellsPerModule + CastleNavTile.Index(layer, column, row);
                        if (_graph.IsWalkable(cell))
                            return cell;
                    }
                }
            }
            return CastleNavGrid.NoCell;
        }
    }
}
