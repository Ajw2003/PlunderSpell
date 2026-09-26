using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.Acoustics;
using Plunderspell.Alarm;
using Plunderspell.Castle;
using Plunderspell.Extraction;
using Plunderspell.Guards;
using Plunderspell.Inventory;
using Plunderspell.Lair;
using Plunderspell.Loot;
using Plunderspell.Raid;
using Plunderspell.Spells;
using Plunderspell.Status;
using Plunderspell.Voice;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>
    /// End-to-end tests of the core concept: speak a word, wake the castle, carry the treasure out,
    /// pay the debt. These are the tests that would catch the whole thing being wired up wrong even
    /// while every individual system passes.
    /// </summary>
    public class FullRaidIntegrationTests
    {
        private readonly List<Object> _spawned = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteAll();
            SpellEffectRegistry.Reset();
            CastleGuard.ClearIntruders();
        }

        [TearDown]
        public void TearDown()
        {
            SpellEffectRegistry.Reset();
            CastleGuard.ClearIntruders();
            VoiceServiceLocator.Clear();
            foreach (Object o in _spawned)
                if (o != null)
                    Object.DestroyImmediate(o);
            _spawned.Clear();
        }

        private T Track<T>(T o) where T : Object
        {
            _spawned.Add(o);
            return o;
        }

        // --- Voice → misfire → consequence ---------------------------------------------------

        [Test]
        public void Test_SayingTheWordBadlyHurtsYouInsteadOfTheGuard()
        {
            SpellLexicon lexicon = BuildLexicon();

            // Spoken correctly.
            Assert.AreEqual(SpellId.Ignis, MisfireEngine.Resolve("IGNIS", lexicon));

            // Spoken badly — one letter out.
            SpellId misfired = MisfireEngine.Resolve("AGNIS", lexicon);
            Assert.AreEqual(SpellId.MisfireIgnis, misfired,
                "A near-miss must misfire, not cast the spell you meant.");

            var casterGo = Track(new GameObject("Caster"));
            casterGo.AddComponent<BoxCollider>();
            casterGo.AddComponent<PurrNet.NetworkIdentity>();
            StatusEffectReceiver caster = casterGo.AddComponent<StatusEffectReceiver>();

            var guardGo = Track(new GameObject("Guard"));
            guardGo.transform.position = new Vector3(0f, 0f, 4f);
            guardGo.AddComponent<BoxCollider>();
            StatusEffectReceiver guard = guardGo.AddComponent<StatusEffectReceiver>();

            SpellEffectRegistry.Execute(new SpellEffectContext(
                misfired, CastVolume.Normal, Vector3.zero, Vector3.forward,
                casterGo.GetComponent<PurrNet.NetworkIdentity>()));

            Assert.IsTrue(caster.IsBurning, "The misfire must land on the caster.");
            Assert.IsFalse(guard.IsBurning, "…and not on the guard they aimed at.");
        }

        [Test]
        public void Test_ShoutingASpellWakesTheCastleAndBringsAGuard()
        {
            var alarmGo = Track(new GameObject("Alarm"));
            alarmGo.AddComponent<BoxCollider>();
            AlarmFSMManager alarm = alarmGo.AddComponent<AlarmFSMManager>();

            var guardGo = Track(new GameObject("Guard"));
            guardGo.transform.position = new Vector3(0f, 0f, 6f);
            guardGo.AddComponent<BoxCollider>();
            guardGo.AddComponent<StatusEffectReceiver>();
            CastleGuard guard = guardGo.AddComponent<CastleGuard>();
            guard.Configure(alarm);

            SpellEffectRegistry.Execute(new SpellEffectContext(
                SpellId.Tonitrus, CastVolume.Shout, Vector3.zero, Vector3.forward));

            Assert.GreaterOrEqual((int)alarm.State, (int)AlarmState.Stirred,
                "A shouted thunderclap must wake the castle.");
            Assert.AreEqual(GuardAlertState.Investigating, guard.State,
                "…and bring a guard to look.");
        }

        // --- The alarm closing the castle ------------------------------------------------------

        [Test]
        public void Test_ARousedCastleLocksItsDoors()
        {
            var alarmGo = Track(new GameObject("Alarm"));
            AlarmFSMManager alarm = alarmGo.AddComponent<AlarmFSMManager>();

            var doorGo = Track(new GameObject("Door"));
            CastleDoor door = doorGo.AddComponent<CastleDoor>();
            Assert.IsTrue(door.TryOpenByHand(), "Before the alarm, doors open by hand.");
            door.Close();

            var lockdownGo = Track(new GameObject("Lockdown"));
            CastleLockdown lockdown = lockdownGo.AddComponent<CastleLockdown>();
            lockdown.Configure(alarm);

            alarm.SetAlarmLevel(55f);
            Assert.AreEqual(AlarmState.Roused, alarm.State);
            Assert.IsTrue(lockdown.IsLockedDown);

            Assert.IsFalse(door.TryOpenByHand(),
                "Once roused, the way out costs a word or a very loud shoulder.");
            Assert.IsTrue(door.ForceOpen(), "Forcing always works — that is the trade.");
        }

        [Test]
        public void Test_PortaStillOpensALockedDownCastle()
        {
            var doorGo = Track(new GameObject("Door"));
            doorGo.transform.position = new Vector3(0f, 0f, 3f);
            doorGo.AddComponent<BoxCollider>();
            CastleDoor door = doorGo.AddComponent<CastleDoor>();
            door.Lock();
            door.Bar();

            int opened = SpellEffectRegistry.Execute(new SpellEffectContext(
                SpellId.Porta, CastVolume.Whisper, Vector3.zero, Vector3.forward));

            Assert.AreEqual(1, opened);
            Assert.IsTrue(door.IsOpen, "A whispered Porta is the silent way out of a locked castle.");
        }

        // --- Aurum Voco ------------------------------------------------------------------------

        [Test]
        public void Test_AurumVocoPutsCoinOnTheFloor()
        {
            var go = Track(new GameObject("LootSpawner"));
            LootSpawner spawner = go.AddComponent<LootSpawner>();
            ConjuredGoldSpawner gold = go.AddComponent<ConjuredGoldSpawner>();

            var template = Track(ScriptableObject.CreateInstance<LootItem>());
            template.DisplayName = "Conjured Coin";
            template.Fragility = 999f;
            gold.Configure(template, null);

            SpellEffectRegistry.Execute(new SpellEffectContext(
                SpellId.AurumVoco, CastVolume.Normal, Vector3.zero, Vector3.forward));

            Assert.Greater(gold.TotalConjured, 0f, "Aurum Voco must actually create value.");
            Assert.AreEqual(1, spawner.Spawned.Count, "…as an object you can pick up and carry out.");
        }

        [Test]
        public void Test_AMisfiredAurumVocoScattersTheSameGold()
        {
            var go = Track(new GameObject("LootSpawner"));
            LootSpawner spawner = go.AddComponent<LootSpawner>();
            ConjuredGoldSpawner gold = go.AddComponent<ConjuredGoldSpawner>();

            var template = Track(ScriptableObject.CreateInstance<LootItem>());
            template.DisplayName = "Conjured Coin";
            gold.Configure(template, null);

            SpellEffectRegistry.Execute(new SpellEffectContext(
                SpellId.MisfireAurumVoco, CastVolume.Normal, Vector3.zero, Vector3.forward));

            Assert.AreEqual(MisfireAurumVocoEffect.ScatterPieces, spawner.Spawned.Count,
                "The gold is not lost — it is now spread across the floor.");
            Assert.AreEqual(SpellTuning.AurumVocoWorth, gold.TotalConjured, 0.01f,
                "A scattered pile is worth the same in total as a neat one.");
        }

        // --- A whole raid ----------------------------------------------------------------------

        [Test]
        public void Test_AWholeRaidFromTheLairAndBack()
        {
            // Lair.
            var lairGo = Track(new GameObject("Lair"));
            LairHubManager lair = lairGo.AddComponent<LairHubManager>();
            float debtBefore = lair.TotalDebt;

            // Castle, haul, garrison, exit.
            var generatorGo = Track(new GameObject("Generator"));
            ProceduralCastleGenerator generator = generatorGo.AddComponent<ProceduralCastleGenerator>();

            var spawnerGo = Track(new GameObject("LootSpawner"));
            LootSpawner lootSpawner = spawnerGo.AddComponent<LootSpawner>();
            lootSpawner.Table = BuildLootTable();

            var zoneGo = Track(new GameObject("Zone"));
            zoneGo.transform.position = new Vector3(100f, 0f, 0f);
            zoneGo.AddComponent<BoxCollider>().isTrigger = true;
            ExtractionZone zone = zoneGo.AddComponent<ExtractionZone>();

            var directorGo = Track(new GameObject("Director"));
            RaidDirector director = directorGo.AddComponent<RaidDirector>();
            director.Configure(generator, lootSpawner, zone, lair);
            director.SetFixedSeed(20260914);

            // Set out.
            director.StartRaid(HistoricalEra.LateMedieval);
            Assert.AreEqual(RaidPhase.Raiding, director.Phase);
            Assert.IsTrue(CastlePathValidator.ValidatePath(director.Castle, out _),
                "The castle must be completable.");
            Assert.Greater(lootSpawner.Spawned.Count, 0, "There must be something in it to steal.");
            Assert.Greater(lair.TotalDebt, debtBefore, "Setting out raises the debt.");

            // Carry two pieces to the exit; smash one on the way, and leave one behind.
            var carried = new List<LootPickup>();
            float carriedWorth = 0f;
            for (int i = 0; i < lootSpawner.Spawned.Count && carried.Count < 2; i++)
            {
                var pickup = lootSpawner.Spawned[i].GetComponent<LootPickup>();
                if (pickup == null || pickup.Data == null)
                    continue;
                carried.Add(pickup);
                carriedWorth += pickup.Data.Worth;
                zone.TrackLoot(pickup);
            }
            Assert.AreEqual(2, carried.Count, "Sanity: the raid produced at least two pieces of loot.");

            carried[1].Break();
            carriedWorth -= carried[1].Data.Worth;

            // Two players walk out with it.
            zone.TrackPlayer(directorGo.AddComponent<PurrNet.NetworkIdentity>());

            float debtDuringRaid = lair.TotalDebt;
            director.CallExtraction();

            Assert.AreEqual(RaidPhase.Resolved, director.Phase);
            Assert.AreEqual(carriedWorth, director.LastWorthExtracted, 0.01f,
                "Only intact loot inside the zone pays out.");
            Assert.AreEqual(1, director.LastPlayersSaved);
            Assert.Less(lair.TotalDebt, debtDuringRaid, "The takings pay down the debt.");
            Assert.AreEqual(0, lootSpawner.Spawned.Count, "The castle is cleared away after the raid.");

            // And back out again.
            director.ReturnToLair();
            Assert.AreEqual(RaidPhase.InLair, director.Phase);

            director.SetFixedSeed(0);
            director.StartRaid(HistoricalEra.BronzeAge);
            Assert.AreEqual(RaidPhase.Raiding, director.Phase, "You can always go again.");
        }

        // --- Fixtures ---------------------------------------------------------------------------

        private SpellLexicon BuildLexicon()
        {
            var lexicon = Track(ScriptableObject.CreateInstance<SpellLexicon>());
            lexicon.MaxNearMatchDistance = 2;

            var ignis = Track(ScriptableObject.CreateInstance<SpellWord>());
            ignis.Word = "IGNIS";
            ignis.spellId = SpellId.Ignis;
            ignis.misfireId = SpellId.MisfireIgnis;
            ignis.AltPronunciations = new[] { "AGNIS", "IGNISH" };

            lexicon.Spells = new List<SpellWord> { ignis };
            return lexicon;
        }

        private RaidLootTable BuildLootTable()
        {
            var table = Track(ScriptableObject.CreateInstance<RaidLootTable>());
            foreach (CastleZone zone in System.Enum.GetValues(typeof(CastleZone)))
            {
                var item = Track(ScriptableObject.CreateInstance<LootItem>());
                item.DisplayName = $"{zone} Treasure";
                item.Worth = 50f + (int)zone * 25f;
                item.WeightKg = 3f;
                item.Fragility = 8f;

                table.Entries.Add(new RaidLootTable.Entry { Item = item, Zone = zone, Weight = 10 });
            }
            return table;
        }
    }
}
