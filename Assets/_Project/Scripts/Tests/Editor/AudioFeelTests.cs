using System;
using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.Audio;
using Plunderspell.EditorTools;
using Plunderspell.Guards;
using Plunderspell.Loot;
using Plunderspell.Raid;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.Tests.Editor
{
    /// <summary>Footsteps, physics impacts and guard voices: the lookups, and that every name they can produce is in the bank.</summary>
    public class AudioFeelTests
    {
        private SoundBank LoadBank()
        {
            var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(SoundBankBuilder.BankPath);
            Assert.IsNotNull(bank, "SoundBank.asset is missing; run Plunderspell > Audio > Rebuild SoundBank.");
            return bank;
        }

        private static void AssertAllInBank(SoundBank bank, IEnumerable<string> names)
        {
            var missing = new List<string>();
            foreach (string name in names)
            {
                if (name != null && !bank.TryGet(name, out _))
                    missing.Add(name);
            }
            Assert.IsEmpty(missing, "Not in the SoundBank: " + string.Join(", ", missing));
        }

        [Test]
        public void SurfaceComesFromTheRoomNameAndTheGroundMaterial()
        {
            Assert.AreEqual(Surface.Wood, SurfaceLookup.FromNames("BronzeLevyBarracks(Clone)", "BronzeLevyBarracks_oak"));
            Assert.AreEqual(Surface.Earth, SurfaceLookup.FromNames("Ground", "BaileyEarth"));
            Assert.AreEqual(Surface.Earth, SurfaceLookup.FromNames("BronzeLevyBarracks(Clone)", "BaileyEarth"), "Ground material beats the room name.");
            Assert.AreEqual(Surface.Tile, SurfaceLookup.FromNames("BronzePalaceKitchen(Clone)", null));
            Assert.AreEqual(Surface.Water, SurfaceLookup.FromNames("BronzeCistern(Clone)", null));
            Assert.AreEqual(Surface.Metal, SurfaceLookup.FromNames("BronzeFoundry(Clone)", null));
            Assert.AreEqual(Surface.Earth, SurfaceLookup.FromNames("DressingGateYard_4_1", "DressingGateYard_ash"));
            Assert.AreEqual(Surface.Stone, SurfaceLookup.FromNames("SomethingElse", "SomethingElse_ash"));
            Assert.AreEqual(Surface.Stone, SurfaceLookup.FromNames(null, null));
        }

        [Test]
        public void EverySurfaceHasAStepSoundInTheBank()
        {
            var names = new List<string>();
            foreach (Surface surface in Enum.GetValues(typeof(Surface)))
                names.Add(SurfaceLookup.StepSound(surface));
            names.Add("foley_step_hound");
            names.Add(SoundNames.Jump);
            names.Add(SoundNames.Dodge);
            names.Add(StepMath.Land(StepMath.MinLandFallSpeed));
            names.Add(StepMath.Land(StepMath.HeavyLandFallSpeed));
            AssertAllInBank(LoadBank(), names);
        }

        [Test]
        public void StrideGrowsWithSpeedAndLoudnessFollowsTheStance()
        {
            Assert.Less(StepMath.Stride(1f), StepMath.Stride(3f));
            Assert.Less(StepMath.Stride(3f), StepMath.Stride(6f));
            Assert.LessOrEqual(StepMath.Stride(50f), 2f);
            Assert.Less(StepMath.Loudness(1f), StepMath.Loudness(3f));
            Assert.Less(StepMath.Loudness(3f), StepMath.Loudness(6f));
            Assert.IsNull(StepMath.Land(1f));
            Assert.AreEqual("foley_player_land", StepMath.Land(4f));
            Assert.AreEqual("foley_player_land", StepMath.Land(8f), "An ordinary jump (about 8 m/s down) is a light landing.");
            Assert.AreEqual("foley_player_land_heavy", StepMath.Land(12f));
        }

        [Test]
        public void EveryLootPieceInTheProjectHasAKnownMaterial()
        {
            var unknown = new List<string>();
            int count = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:LootItem"))
            {
                var item = AssetDatabase.LoadAssetAtPath<LootItem>(AssetDatabase.GUIDToAssetPath(guid));
                count++;
                LootMaterials.FromName(item.name, out bool known);
                if (!known)
                    unknown.Add(item.name);
            }

            Assert.Greater(count, 20, "Expected the project's loot pieces to be found.");
            Assert.IsEmpty(unknown, "No keyword matches: " + string.Join(", ", unknown));
        }

        [Test]
        public void LootMaterialsFollowTheNameAndIgnoreSpacingAndCase()
        {
            Assert.AreEqual(LootMaterial.Gold, LootMaterials.FromName("Golden Goblet"));
            Assert.AreEqual(LootMaterial.Gold, LootMaterials.FromName("GoldenGoblet"));
            Assert.AreEqual(LootMaterial.Gold, LootMaterials.FromName("golden_goblet"));
            Assert.AreEqual(LootMaterial.Book, LootMaterials.FromName("IlluminatedPsalter"));
            Assert.AreEqual(LootMaterial.Coins, LootMaterials.FromName("CoinCoffer"));
            Assert.AreEqual(LootMaterial.Glass, LootMaterials.FromName("Loot_Glass_Reliquary"));
            Assert.AreEqual(LootMaterial.Cloth, LootMaterials.FromName("RolledTapestry"));
            Assert.AreEqual(LootMaterial.Bronze, LootMaterials.FromName("TripodCauldron"));
            Assert.AreEqual(LootMaterial.Metal, LootMaterials.FromName("Weapon_Longsword"));
            Assert.AreEqual(LootMaterial.Stone, LootMaterials.FromName("Unnamed"));
            Assert.AreEqual(LootMaterial.Stone, LootMaterials.FromName(null));
        }

        [Test]
        public void EveryMaterialsImpactBreakAndScrapeSoundIsInTheBank()
        {
            var names = new List<string>();
            foreach (LootMaterial material in Enum.GetValues(typeof(LootMaterial)))
            {
                names.Add(LootMaterials.Impact(material, false));
                names.Add(LootMaterials.Impact(material, true));
                names.Add(LootMaterials.Break(material, "Piece"));
                names.Add(LootMaterials.Scrape(material));
            }
            names.Add(LootMaterials.Break(LootMaterial.Glass, "VenetianMirror"));
            names.Add(LootMaterials.Break(LootMaterial.Ceramic, "SealedAmphora"));
            names.Add(LootMaterials.Roll);
            names.Add(LootMaterials.Body);
            AssertAllInBank(LoadBank(), names);
        }

        [Test]
        public void EveryRosterEnemyOfTheFourAgesHasAVoiceAndEveryLineIsInTheBank()
        {
            string[] rosters =
            {
                "EnemyRoster_BronzeAge", "EnemyRoster_HighMedieval", "EnemyRoster_LateMedieval", "EnemyRoster_AgeOfPowder"
            };

            var names = new List<string>();
            var seen = new HashSet<string>();
            foreach (string rosterName in rosters)
            {
                var roster = AssetDatabase.LoadAssetAtPath<EnemyRoster>("Assets/_Project/Data/Enemies/" + rosterName + ".asset");
                Assert.IsNotNull(roster, rosterName);
                foreach (EnemyRoster.Entry entry in roster.Entries)
                {
                    GuardVoiceProfile profile = GuardVoices.Resolve(entry.Prefab.name);
                    Assert.IsTrue(profile.Valid, entry.Prefab.name + " has no guard voice.");
                    seen.Add(entry.Prefab.name);
                    foreach (GuardLine line in Enum.GetValues(typeof(GuardLine)))
                        names.Add(GuardVoices.LineName(profile, line));
                    names.Add(GuardVoices.GearSound(profile));
                }
            }

            Assert.AreEqual(16, seen.Count, "Four Ages of four enemies.");
            AssertAllInBank(LoadBank(), names);
        }

        [Test]
        public void PrefabNamesWithCloneAndPrototypesAreHandled()
        {
            Assert.IsTrue(GuardVoices.Resolve("PalaceLevy(Clone)").Valid);
            Assert.AreEqual("levy", GuardVoices.Resolve("PalaceLevy(Clone)").Voice);
            Assert.IsTrue(GuardVoices.Resolve("AlauntWarHound").Hound);
            Assert.IsFalse(GuardVoices.Resolve("ArcRevenant(Clone)").Valid);
            Assert.IsFalse(GuardVoices.Resolve(null).Valid);
            Assert.IsNull(GuardVoices.LineName(GuardVoices.Resolve("HexTurret"), GuardLine.Alert));
            Assert.AreEqual("vo_bronze_levy_alert", GuardVoices.LineName(GuardVoices.Resolve("PalaceLevy"), GuardLine.Alert));
            Assert.AreEqual("vo_hound_bark", GuardVoices.LineName(GuardVoices.Resolve("AlauntWarHound"), GuardLine.Chase));
            Assert.IsNull(GuardVoices.LineName(GuardVoices.Resolve("AlauntWarHound"), GuardLine.Murmur));
        }

        [Test]
        public void ALineIsChosenForEachChangeOfAlertState()
        {
            Assert.AreEqual(GuardLine.Alert, GuardVoices.LineForStateChange(GuardAlertState.Patrolling, GuardAlertState.Investigating));
            Assert.AreEqual(GuardLine.Chase, GuardVoices.LineForStateChange(GuardAlertState.Investigating, GuardAlertState.Chasing));
            Assert.AreEqual(GuardLine.Search, GuardVoices.LineForStateChange(GuardAlertState.Chasing, GuardAlertState.Searching));
            Assert.AreEqual(GuardLine.Lost, GuardVoices.LineForStateChange(GuardAlertState.Searching, GuardAlertState.Patrolling));
            Assert.AreEqual(GuardLine.Asleep, GuardVoices.LineForStateChange(GuardAlertState.Patrolling, GuardAlertState.Incapacitated));
            Assert.IsNull(GuardVoices.LineForStateChange(GuardAlertState.Incapacitated, GuardAlertState.Patrolling), "Waking is not 'lost'.");
        }

        [Test]
        public void LoopBusGivesOneSlotPerKeyAndNeverMoreThanItsSize()
        {
            SoundBank bank = LoadBank();
            Assert.IsTrue(bank.TryGet(LootMaterials.Roll, out SoundEntry entry));
            var root = new GameObject("LoopTest");
            try
            {
                var bus = new LoopBus(root.transform, 3);
                for (int frame = 0; frame < 5; frame++)
                {
                    for (int key = 1; key <= 10; key++)
                        bus.Drive(key, entry, 1f, Vector3.zero);
                }
                Assert.LessOrEqual(bus.Active, 3);
                Assert.AreEqual(3, root.GetComponentsInChildren<AudioSource>().Length, "No source was created after the bus was made.");
                Assert.IsNotNull(bus.SourceFor(1));
                Assert.IsNull(bus.SourceFor(10), "The bus was full, so a late key waits.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
