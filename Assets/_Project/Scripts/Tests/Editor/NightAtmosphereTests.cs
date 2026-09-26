using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RogueAi.Alarm;
using RogueAi.Atmosphere;
using RogueAi.Castle;
using RogueAi.EditorTools;
using UnityEditor;
using UnityEngine;

namespace RogueAi.Tests.Editor
{
    /// <summary>
    /// The pure rules behind the castle's night (docs/plans/night-atmosphere.md): which fires burn in
    /// which alarm state, which of them the machine can afford to light, what each graphics level
    /// spends, and that the fire anchors the pipeline writes reach the room registries.
    /// </summary>
    public class NightAtmosphereTests
    {
        [Test]
        public void Test_HearthsAlwaysBurnAndBeaconsWaitForTheAlarm()
        {
            foreach (AlarmState state in System.Enum.GetValues(typeof(AlarmState)))
                Assert.IsTrue(FireRules.IsLit(FireKind.Hearth, 3, state), $"A hearth is out in {state}.");

            Assert.IsFalse(FireRules.IsLit(FireKind.Beacon, 2, AlarmState.Calm));
            Assert.IsFalse(FireRules.IsLit(FireKind.Beacon, 2, AlarmState.Stirred));
            Assert.IsTrue(FireRules.IsLit(FireKind.Beacon, 2, AlarmState.Roused));
            Assert.IsTrue(FireRules.IsLit(FireKind.Beacon, 2, AlarmState.HueAndCry));
        }

        [Test]
        public void Test_SomeSconcesAreLitAtCalmAndAllOnceStirred()
        {
            Assert.IsTrue(FireRules.IsLit(FireKind.Sconce, 0, AlarmState.Calm));
            Assert.IsFalse(FireRules.IsLit(FireKind.Sconce, 1, AlarmState.Calm), "The second sconce waits for the castle to stir.");
            Assert.IsTrue(FireRules.IsLit(FireKind.Sconce, 1, AlarmState.Stirred));
        }

        [Test]
        public void Test_BraziersFlareWhenRousedAndEverythingAtTheHueAndCry()
        {
            Assert.AreEqual(1f, FireRules.Flare(FireKind.Brazier, AlarmState.Stirred));
            Assert.AreEqual(FireRules.FullAlertFlare, FireRules.Flare(FireKind.Brazier, AlarmState.Roused));
            Assert.AreEqual(1f, FireRules.Flare(FireKind.Sconce, AlarmState.Roused));
            Assert.AreEqual(FireRules.FullAlertFlare, FireRules.Flare(FireKind.Sconce, AlarmState.HueAndCry));
            Assert.AreEqual(1.5f, FireRules.FullAlertFlare, 0.001f, "The spec: a fire grows about 1.5x at full alert.");
        }

        [Test]
        public void Test_TheNearestLitFiresGetShadowsThenLightThenNothing()
        {
            var distances = new List<float> { 25f, 1f, 9f, 4f, 16f, 0.5f };
            var lit = new List<bool> { true, true, true, true, true, false };
            var grants = new List<FireRules.LightGrant>();

            FireRules.ShareLights(distances, lit, 2, 2, grants, new List<int>());

            Assert.AreEqual(FireRules.LightGrant.Shadowed, grants[1], "Nearest lit fire casts shadows.");
            Assert.AreEqual(FireRules.LightGrant.Shadowed, grants[3], "Second nearest lit fire casts shadows.");
            Assert.AreEqual(FireRules.LightGrant.Unshadowed, grants[2]);
            Assert.AreEqual(FireRules.LightGrant.Unshadowed, grants[4]);
            Assert.AreEqual(FireRules.LightGrant.None, grants[0], "Past the budget a fire is flame and glow only.");
            Assert.AreEqual(FireRules.LightGrant.None, grants[5], "An unlit fire gets no light, however close.");
        }

        [Test]
        public void Test_EqualDistancesShareLightsTheSameWayEveryTime()
        {
            var distances = new List<float> { 4f, 4f, 4f };
            var lit = new List<bool> { true, true, true };
            var grants = new List<FireRules.LightGrant>();
            FireRules.ShareLights(distances, lit, 1, 1, grants, new List<int>());
            CollectionAssert.AreEqual(
                new[] { FireRules.LightGrant.Shadowed, FireRules.LightGrant.Unshadowed, FireRules.LightGrant.None }, grants);
        }

        [Test]
        public void Test_EachGraphicsLevelSpendsWhatTheSpecSays()
        {
            TierBudget low = AtmosphereQuality.BudgetFor(QualityTier.Low);
            TierBudget medium = AtmosphereQuality.BudgetFor(QualityTier.Medium);
            TierBudget high = AtmosphereQuality.BudgetFor(QualityTier.High);

            Assert.AreEqual(2, low.ShadowedFires);
            Assert.AreEqual(4, medium.ShadowedFires);
            Assert.AreEqual(8, high.ShadowedFires);
            Assert.AreEqual(16, low.UnshadowedFires);
            Assert.AreEqual(32, medium.UnshadowedFires);
            Assert.AreEqual(int.MaxValue, high.UnshadowedFires, "High lights every fire.");
            Assert.IsFalse(low.TriplanarDetail);
            Assert.IsTrue(medium.TriplanarDetail);
            Assert.IsTrue(high.VolumetricFog && !medium.VolumetricFog && !low.VolumetricFog);
            Assert.IsTrue(high.FilmGrain && !medium.FilmGrain);
        }

        [Test]
        public void Test_ASteamDeckStartsOnLowAndEverythingElseOnMedium()
        {
            Assert.AreEqual(QualityTier.Low, AtmosphereQuality.DefaultTierFor("Valve Jupiter", "AMD Custom GPU 0405"));
            Assert.AreEqual(QualityTier.Low, AtmosphereQuality.DefaultTierFor("Valve Galileo", "AMD Custom GPU 0932"));
            Assert.AreEqual(QualityTier.Low, AtmosphereQuality.DefaultTierFor("", "AMD Custom GPU 0405"));
            Assert.AreEqual(QualityTier.Medium, AtmosphereQuality.DefaultTierFor("System Product Name", "NVIDIA GeForce RTX 3070"));
            Assert.AreEqual(QualityTier.Medium, AtmosphereQuality.DefaultTierFor(null, null));
        }

        [Test]
        public void Test_QualityLevelNamesMapToTiers()
        {
            Assert.AreEqual(QualityTier.Low, AtmosphereQuality.TierForLevelName("Low"));
            Assert.AreEqual(QualityTier.High, AtmosphereQuality.TierForLevelName("high"));
            Assert.AreEqual(QualityTier.Medium, AtmosphereQuality.TierForLevelName("PC"), "Unknown names fall back to Medium.");
            CollectionAssert.AreEqual(AtmosphereQuality.LevelNames, QualitySettings.names,
                "ProjectSettings' quality levels are Low, Medium, High, in that order.");
        }

        [Test]
        public void Test_AFireAnchorComesThroughInModuleSpaceFacingTheRightWay()
        {
            const string json = "{\"rooms\": {\"TestRoom\": [" +
                                "{\"kind\": \"Sconce\", \"p\": [1.5, 5.5, 2.6], \"facing\": [0.0, -1.0], \"lit\": 1, \"holder\": true}," +
                                "{\"kind\": \"Hearth\", \"p\": [-4.3, 4.3, 1.35], \"facing\": [1.0, 0.0], \"lit\": 0, \"holder\": false}]}}";

            Dictionary<string, CastleFireAnchor[]> rooms = CastleFireAnchorImporter.Parse(json);

            CastleFireAnchor sconce = rooms["TestRoom"][0];
            Assert.AreEqual(new Vector3(1.5f, 2.6f, 5.5f), sconce.Position, "Blender (x, y, z) is module (x, z, y), as for loot.");
            Assert.AreEqual(180f, Mathf.Abs(sconce.Yaw), 0.01f, "Facing -Y in Blender is facing -Z: yaw 180.");
            Assert.AreEqual(FireKind.Sconce, sconce.Kind);
            Assert.AreEqual(1, sconce.LitFrom);
            Assert.IsTrue(sconce.BringsHolder);

            CastleFireAnchor hearth = rooms["TestRoom"][1];
            Assert.AreEqual(90f, hearth.Yaw, 0.01f, "Facing +X: yaw 90.");
            Assert.IsFalse(hearth.BringsHolder);
        }

        [Test]
        public void Test_EveryHighMedievalRoomHasFireAndTheWallHasTorches()
        {
            var registry = AssetDatabase.LoadAssetAtPath<CastleRoomRegistry>("Assets/_Project/Data/Castle/CastleRoomRegistry.asset");
            Assert.IsNotNull(registry);
            Assert.IsTrue(File.Exists("Assets/_Project/Data/Castle/CastleFireAnchors.json"),
                "The asset pipeline writes the fire anchors; run build_assets.py.");

            foreach (CastleRoomModuleData module in registry.Modules)
            {
                if (module.Zone == CastleZone.CurtainWall && module.RoomId == "Drawbridge")
                    continue;
                Assert.IsNotEmpty(module.FireAnchors,
                    $"{module.RoomId} has no fire; run Tools/Plunderspell/Import Castle Fire Anchors.");
                foreach (CastleFireAnchor anchor in module.FireAnchors)
                {
                    Assert.LessOrEqual(Mathf.Abs(anchor.Position.x), 6f, $"{module.RoomId}: a fire outside its 12 m cell.");
                    Assert.LessOrEqual(Mathf.Abs(anchor.Position.z), 6f, $"{module.RoomId}: a fire outside its 12 m cell.");
                    Assert.GreaterOrEqual(anchor.Position.y, 0f, $"{module.RoomId}: a fire below the floor.");
                }
            }
        }
    }
}
