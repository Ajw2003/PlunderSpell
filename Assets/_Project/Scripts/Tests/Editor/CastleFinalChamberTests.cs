using System.Linq;
using NUnit.Framework;
using Plunderspell.Castle;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.Tests.Editor
{
    /// <summary>
    /// The final chamber sits in the Crypt pool, so a plain-room draw could place a second one.
    /// Runs on the real registry (the data-only generator never reaches that pool).
    /// </summary>
    public class CastleFinalChamberTests
    {
        private const string RegistryPath = "Assets/_Project/Data/Castle/CastleRoomRegistry.asset";
        private const string FinalChamberId = "CryptChamberFinal";

        [Test]
        public void ExactlyOneFinalChamberAtTheCryptLevelOnTheRealRegistry()
        {
            var go = new GameObject("CastleGen");
            try
            {
                var generator = go.AddComponent<ProceduralCastleGenerator>();
                generator.Registry = AssetDatabase.LoadAssetAtPath<CastleRoomRegistry>(RegistryPath);
                Assert.IsNotNull(generator.Registry, "registry asset missing");

                for (int seed = 1; seed <= 40; seed++)
                {
                    var finals = generator.Generate(seed).PlacedModules.Where(m => m.RoomId == FinalChamberId).ToList();
                    Assert.AreEqual(1, finals.Count, $"seed {seed}: final chambers");
                    Assert.AreEqual(CastleLevels.Crypt, finals[0].Level, $"seed {seed}: final chamber level");
                }
            }
            finally
            {
                go.GetComponent<ProceduralCastleGenerator>().ClearGenerated();
                Object.DestroyImmediate(go);
            }
        }
    }
}
