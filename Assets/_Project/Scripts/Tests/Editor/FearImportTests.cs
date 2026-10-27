using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Plunderspell.EditorTools;
using UnityEngine;

namespace Plunderspell.Tests.Editor
{
    /// <summary>
    /// The fear pass's import rules (#413, under #386): the Wick and the Ophan of Hours, their clip
    /// files, and the constants the importer keeps in step with the ArtForge manifests.
    /// </summary>
    public class FearImportTests
    {
        private const string WickPath = ArtBibleModelImporter.FearRoot + "TheWick/TheWick.fbx";
        private const string OphanPath = ArtBibleModelImporter.FearRoot + "TheOphanOfHours/TheOphanOfHours.fbx";
        private const string FearData = "docs/art/data/fear.json";

        private static readonly string[] ClipManifests =
        {
            "Assets/Models/ArtBible/Animations/fear_anim_manifest.json",
            "Assets/Models/ArtBible/Animations/ophan_anim_manifest.json",
        };

        private static string FullPath(string projectPath) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", projectPath));

        private static object ReadJson(string projectPath)
        {
            string path = FullPath(projectPath);
            Assert.IsTrue(File.Exists(path), $"{projectPath} missing: rebuild it with Tools/ArtForge");
            return ArtBibleJson.Parse(File.ReadAllText(path));
        }

        /// <summary>Every clip row in both fear clip manifests.</summary>
        private static List<object> ManifestClips() =>
            ClipManifests.SelectMany(m => ArtBibleJson.GetList(ReadJson(m), "fbx"))
                .SelectMany(fbx => ArtBibleJson.GetList(fbx, "clips"))
                .ToList();

        [Test]
        public void ImporterClassifiesTheFearModelsAndClips()
        {
            Assert.AreEqual(ArtBibleModelImporter.Kind.HumanoidEnemy, ArtBibleModelImporter.KindOf(WickPath));
            Assert.AreEqual(ArtBibleModelImporter.Kind.GenericEnemy, ArtBibleModelImporter.KindOf(OphanPath),
                "the Ophan is wheels and wings, not a human: it must import Generic");
            Assert.AreEqual(ArtBibleModelImporter.Kind.HumanoidClips,
                ArtBibleModelImporter.KindOf(ArtBibleModelImporter.FearClipsPath));
            Assert.AreEqual(ArtBibleModelImporter.Kind.GenericClips,
                ArtBibleModelImporter.KindOf(ArtBibleModelImporter.OphanClipsPath));
        }

        [Test]
        public void TheAgesRulesAreUnchanged()
        {
            Assert.AreEqual(ArtBibleModelImporter.Kind.GenericEnemy,
                ArtBibleModelImporter.KindOf("Assets/Models/ArtBible/Enemies/High/AlauntWarHound/AlauntWarHound.fbx"));
            Assert.AreEqual(ArtBibleModelImporter.Kind.HumanoidEnemy,
                ArtBibleModelImporter.KindOf("Assets/Models/ArtBible/Enemies/High/LanternWarden/LanternWarden.fbx"));
            Assert.AreEqual(ArtBibleModelImporter.Kind.HumanoidClips,
                ArtBibleModelImporter.KindOf(ArtBibleModelImporter.PlayerClipsPath));
            Assert.IsNull(ArtBibleModelImporter.KindOf("Assets/Models/ArtBible/Animations/Humanoid_Base.fbx"));
        }

        [Test]
        public void TheFireMaskImportsLinear()
        {
            Assert.IsTrue(ArtBibleModelImporter.IsLinearTexture(
                ArtBibleModelImporter.FearRoot + "TheWick/Textures/TheWick_FireMask.png"));
            Assert.IsFalse(ArtBibleModelImporter.IsLinearTexture(
                ArtBibleModelImporter.FearRoot + "TheWick/Textures/TheWick_BaseMap.png"));
        }

        [Test]
        public void TheOphanGlowsAndTheWickDoesNot()
        {
            Assert.IsTrue(ArtBibleEnemyCatalog.IsEmissiveModel("TheOphanOfHours"));
            Assert.IsFalse(ArtBibleEnemyCatalog.IsEmissiveModel("TheWick"));
            Assert.IsTrue(ArtBibleEnemyCatalog.IsEmissiveModel("LanternWarden"), "the Ages' list still counts");
        }

        [Test]
        public void FearRigAndGlowListsMatchTheArtBible()
        {
            var names = new Dictionary<string, object>();
            foreach (object asset in ArtBibleJson.GetList(ReadJson(ArtBibleEnemyCatalog.ManifestPath), "assets"))
            {
                string key = ArtBibleJson.GetString(asset, "key");
                if (key != null && key.StartsWith("enemies/fear/"))
                    names[key.Substring("enemies/fear/".Length)] = asset;
            }

            var generic = new List<string>();
            var emissive = new List<string>();
            foreach (object enemy in ArtBibleJson.GetList(ReadJson(FearData), "enemies"))
            {
                string slug = ArtBibleJson.GetString(enemy, "slug");
                Assert.IsTrue(names.TryGetValue(slug, out object asset), $"fear/{slug} is not in the ArtForge manifest");
                string name = ArtBibleJson.GetString(asset, "name");
                string skeleton = ArtBibleJson.GetString(ArtBibleJson.Get(enemy, "rig"), "skeleton") ?? string.Empty;
                if (skeleton.StartsWith("Generic"))
                    generic.Add(name);
                if (ArtBibleEnemyCatalog.HasEmissiveFamily(asset))
                    emissive.Add(name);
            }

            CollectionAssert.AreEqual(ArtBibleEnemyCatalog.FearGenericRigModels.OrderBy(n => n).ToArray(),
                generic.OrderBy(n => n).ToArray(), "FearGenericRigModels must list fear.json's Generic rigs");
            CollectionAssert.AreEqual(ArtBibleEnemyCatalog.FearEmissiveModels.OrderBy(n => n).ToArray(),
                emissive.OrderBy(n => n).ToArray(), "FearEmissiveModels must list the fear models with an emit family");
        }

        [Test]
        public void LoopFlagsMatchTheClipManifests()
        {
            string[] looping = ManifestClips()
                .Where(c => ArtBibleJson.Get(c, "loop") is bool loop && loop)
                .Select(c => ArtBibleJson.GetString(c, "clip"))
                .OrderBy(id => id)
                .ToArray();
            CollectionAssert.AreEqual(ArtBibleModelImporter.LoopingFearClips.OrderBy(id => id).ToArray(), looping,
                "ArtBibleModelImporter.LoopingFearClips must list exactly the looping clips in the fear manifests");
        }

        [Test]
        public void EventsMatchTheClipManifests()
        {
            var expected = new Dictionary<string, string[]>();
            foreach (object clip in ManifestClips())
            {
                string[] events = ArtBibleJson.GetList(clip, "events")
                    .Select(e => Describe(ArtBibleJson.GetString(e, "type"), ArtBibleJson.GetFloat(e, "t") ?? -1f,
                        FirstParameter(ArtBibleJson.Get(e, "param"))))
                    .ToArray();
                if (events.Length > 0)
                    expected[ArtBibleJson.GetString(clip, "clip")] = events;
            }

            CollectionAssert.AreEquivalent(expected.Keys, ArtBibleModelImporter.FearClipEvents.Keys,
                "FearClipEvents must name exactly the fear clips that have events");
            foreach (KeyValuePair<string, string[]> pair in expected)
            {
                string[] actual = ArtBibleModelImporter.FearClipEvents[pair.Key]
                    .Select(e => Describe(e.Function, e.NormalizedTime, e.Parameter))
                    .ToArray();
                CollectionAssert.AreEqual(pair.Value, actual, $"{pair.Key}'s events differ from its manifest");
            }
        }

        private static string FirstParameter(object param) =>
            param is Dictionary<string, object> map && map.Count > 0 ? map.Values.First() as string ?? string.Empty
                : string.Empty;

        private static string Describe(string function, float normalizedTime, string parameter) =>
            $"{function}@{normalizedTime:0.0000}({parameter})";
    }
}
