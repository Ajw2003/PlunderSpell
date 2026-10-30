using NUnit.Framework;
using Plunderspell.Castle;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Plunderspell.Tests.Editor
{
    /// <summary>The hidden escape portal's room (#553): deterministic, far from the arrival, never a dead entry.</summary>
    public class CastleEscapePlannerTests
    {
        private const string RegistryPath = "Assets/_Project/Data/Castle/CastleRoomRegistry.asset";

        // Nine entries: indices 1 and 4 are not candidates; 7 candidates leave a farthest third of 3.
        private static readonly float[] s_distances = { 10f, float.NaN, 70f, 30f, float.PositiveInfinity, 50f, 90f, 20f, 80f };

        [Test]
        public void SameSeedGivesTheSamePick()
        {
            for (int seed = 0; seed < 50; seed++)
                Assert.AreEqual(CastleEscapePlanner.ChooseFromDistances(s_distances, seed),
                    CastleEscapePlanner.ChooseFromDistances(s_distances, seed));
        }

        [Test]
        public void NeverPicksANonCandidate_AndAlwaysTheFarthestThird()
        {
            // Candidates by distance: 6 (90), 8 (80), 2 (70) are the farthest third.
            var allowed = new System.Collections.Generic.HashSet<int> { 6, 8, 2 };
            for (int seed = -200; seed < 800; seed++)
                Assert.That(allowed, Does.Contain(CastleEscapePlanner.ChooseFromDistances(s_distances, seed)), $"seed {seed}");
        }

        [Test]
        public void NoCandidatesGivesNoEscape()
        {
            var none = new[] { float.NaN, float.NaN, -1f, float.PositiveInfinity };
            Assert.AreEqual(CastleEscapePlanner.NoEscape, CastleEscapePlanner.ChooseFromDistances(none, 1));
            Assert.AreEqual(CastleEscapePlanner.NoEscape, CastleEscapePlanner.ChooseFromDistances(new float[0], 1));
        }

        [Test]
        public void ARealCastleGivesARoomOtherThanTheArrival()
        {
            var registry = AssetDatabase.LoadAssetAtPath<CastleRoomRegistry>(RegistryPath);
            Assert.That(registry, Is.Not.Null);
            var go = new GameObject("EscapePlannerTestGenerator");
            var generator = go.AddComponent<ProceduralCastleGenerator>();
            generator.Registry = registry;
            try
            {
                foreach (int seed in new[] { 12345, 777 })
                {
                    ProceduralCastleData data = generator.Generate(seed);
                    Vector3 arrival = CastleSpawnResolver.ResolveArrival(data, seed, out int arrivalModule);
                    int escape = CastleEscapePlanner.ChooseModule(data, data.NavGraph, arrivalModule, arrival, seed);
                    Assert.AreNotEqual(CastleEscapePlanner.NoEscape, escape, $"seed {seed}: no escape room");
                    Assert.AreNotEqual(arrivalModule, escape, $"seed {seed}: escape is the arrival room");
                    Assert.AreNotEqual(data.ExtractionExitIndex, escape, $"seed {seed}: escape is the exit");
                }
            }
            finally
            {
                generator.ClearGenerated();
                Object.DestroyImmediate(go);
            }
        }
    }
}
