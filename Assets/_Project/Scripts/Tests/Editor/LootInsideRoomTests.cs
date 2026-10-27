using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.Castle;
using Plunderspell.Raid;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.Tests.Editor
{
    /// <summary>
    /// #272: the Gilded Altarpiece spawned in the floor or ceiling. Builds the real castle and checks
    /// every planned piece's renderer bounds sit between the floor below and the ceiling above it.
    /// </summary>
    public class LootInsideRoomTests
    {
        private const string HighMedievalTable = "Assets/_Project/Data/Loot/HighMedieval/RaidLootTable_HighMedieval.asset";

        [Test]
        public void EveryHighMedievalPieceSitsBetweenFloorAndCeiling()
        {
            var go = new GameObject("CastleGen");
            var table = AssetDatabase.LoadAssetAtPath<RaidLootTable>(HighMedievalTable);
            Assert.IsNotNull(table, "loot table missing");
            try
            {
                var generator = go.AddComponent<ProceduralCastleGenerator>();
                var spawner = go.AddComponent<LootSpawner>();
                generator.Registry = AssetDatabase.LoadAssetAtPath<CastleRoomRegistry>("Assets/_Project/Data/Castle/CastleRoomRegistry.asset");
                var failures = new List<string>();
                for (int seed = 1; seed <= 40; seed++)
                {
                    ProceduralCastleData castle = generator.Generate(seed);
                    Physics.SyncTransforms();
                    spawner.Table = table;
                    IReadOnlyList<LootPlacement> plan = spawner.SpawnFor(castle, seed, generator.Registry);
                    for (int i = 0; i < plan.Count; i++)
                        if (i < spawner.Spawned.Count)
                            Check(failures, castle, seed, plan[i], spawner.Spawned[i]);
                    spawner.Clear();
                }
                Assert.IsEmpty(failures, string.Join("\n", failures));
            }
            finally
            {
                go.GetComponent<ProceduralCastleGenerator>().ClearGenerated();
                Object.DestroyImmediate(go);
            }
        }

        private static void Check(List<string> failures, ProceduralCastleData castle, int seed, LootPlacement p, GameObject piece)
        {
            Renderer[] renderers = piece.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return;
            Physics.SyncTransforms();
            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers)
                b.Encapsulate(r.bounds);
            Vector3 pos = piece.transform.position;
            Vector3 at = pos + Vector3.up * 0.1f;
            float floor = Physics.Raycast(at, Vector3.down, out RaycastHit down, 3f, ~0, QueryTriggerInteraction.Ignore)
                ? down.point.y : float.NegativeInfinity;
            float ceiling = TryCeiling(at, piece, out RaycastHit up) ? up.point.y : float.PositiveInfinity;
            var m = castle.PlacedModules[p.ModuleIndex];
            string what = $"seed {seed} {p.Entry.Item.DisplayName} room {m.RoomId} level {m.Level} at {pos}: " +
                          $"bounds y {b.min.y:F2}..{b.max.y:F2}, floor {floor:F2}, ceiling {ceiling:F2}";
            if (b.min.y < floor - 0.2f || b.max.y > ceiling + 0.01f)
                failures.Add(what);
        }

        private static bool TryCeiling(Vector3 at, GameObject piece, out RaycastHit best)
        {
            best = default;
            bool found = false;
            foreach (RaycastHit h in Physics.RaycastAll(at, Vector3.up, 8f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.transform.IsChildOf(piece.transform) || (found && h.distance >= best.distance))
                    continue;
                best = h;
                found = true;
            }
            return found;
        }
    }
}
