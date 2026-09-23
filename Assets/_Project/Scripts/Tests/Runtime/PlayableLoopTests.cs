using System.Collections;
using System.Collections.Generic;
using Interfaces;
using NUnit.Framework;
using Plunderspell.Core;
using RogueAi.Castle;
using RogueAi.Extraction;
using RogueAi.Loot;
using RogueAi.Raid;
using UnityEngine;
using UnityEngine.TestTools;

namespace RogueAi.Tests
{
    /// <summary>
    /// Regression tests for the 2026-09-23 Phase 0 play session (docs/plans/phase0-playable-loop.md):
    /// one damage pathway with blame, leaving by standing on the pad, weight you can feel, and a
    /// guard-free entrance. Each was found broken by playing, not by a test.
    /// </summary>
    public class PlayableLoopTests
    {
        private readonly List<Object> _spawned = new List<Object>();
        private GameState _stateBefore;

        [SetUp]
        public void SetUp()
        {
            GameServices.Initialize();
            _stateBefore = GameServices.GameState.CurrentState;
        }

        [TearDown]
        public void TearDown()
        {
            GameServices.Extraction.CancelExtraction();
            GameServices.GameState.ChangeState(_stateBefore);
            foreach (Object o in _spawned)
                if (o != null)
                    Object.Destroy(o);
            _spawned.Clear();
        }

        private T Track<T>(T o) where T : Object
        {
            _spawned.Add(o);
            return o;
        }

        /// <summary>A minimal thing with health.</summary>
        private class Dummy : MonoBehaviour, IHealth
        {
            public float Health = 50f;
            public float CurrentHealth => Health;
            public float MaxHealth => 50f;
            public void TakeDamage(float damage) => Health = Mathf.Max(0f, Health - damage);

            public void TakeDamage(float damage, float impactVelocity)
            {
                if (impactVelocity >= 5f)
                    TakeDamage(damage);
            }
        }

        /// <summary>A minimal player body, for the extraction zone's "is this a player" check.</summary>
        private class Body : MonoBehaviour, IPlayerBody
        {
            public bool IsAlive => true;
        }

        // --- One damage pathway ----------------------------------------------------------------

        [Test]
        public void Test_ApplyReportsTheHitWithWhoIsToBlame()
        {
            var target = Track(new GameObject("Target")).AddComponent<Dummy>();
            var thrower = Track(new GameObject("Thrower"));
            var goblet = Track(new GameObject("Goblet"));

            var reports = new List<DamageReport>();
            void Record(DamageReport r) => reports.Add(r);
            Damage.Dealt += Record;
            try
            {
                float lost = Damage.Apply(target, 20f, goblet, thrower, Vector3.one, DamageKind.Impact);

                Assert.AreEqual(20f, lost);
                Assert.AreEqual(1, reports.Count, "Every hit that costs health must be announced.");
                Assert.AreSame(goblet, reports[0].Source, "The source is what physically hit.");
                Assert.AreSame(thrower, reports[0].Instigator, "The instigator is who gets the blame.");
                Assert.AreEqual(30f, reports[0].HealthAfter);
            }
            finally
            {
                Damage.Dealt -= Record;
            }
        }

        [Test]
        public void Test_AHitThatCostsNothingIsNotReported()
        {
            var target = Track(new GameObject("Target")).AddComponent<Dummy>();
            int reports = 0;
            void Count(DamageReport _) => reports++;
            Damage.Dealt += Count;
            try
            {
                // Below the dummy's impact threshold: it shrugs it off, so no number should appear.
                Damage.Apply(target, 20f, null, null, Vector3.zero, DamageKind.Impact, impactVelocity: 1f);
                Assert.AreEqual(0, reports);

                target.Health = 0f;
                Damage.Apply(target, 20f, null, null, Vector3.zero, DamageKind.Melee);
                Assert.AreEqual(0, reports, "Hitting a corpse is not a hit.");
            }
            finally
            {
                Damage.Dealt -= Count;
            }
        }

        [Test]
        public void Test_AKillingBlowAndSelfHarmAreRecognised()
        {
            var player = Track(new GameObject("Player"));
            var target = player.AddComponent<Dummy>();
            DamageReport last = default;
            void Keep(DamageReport r) => last = r;
            Damage.Dealt += Keep;
            try
            {
                Damage.Apply(target, 999f, player, player, Vector3.zero, DamageKind.Burn);
                Assert.IsTrue(last.Killed);
                Assert.IsTrue(last.SelfInflicted, "Your own fire is your own fault.");
            }
            finally
            {
                Damage.Dealt -= Keep;
            }
        }

        // --- Leaving by standing on the pad ----------------------------------------------------

        [UnityTest]
        public IEnumerator Test_StandingOnThePadLeavesWithTheLootOnIt()
        {
            GameServices.GameState.ChangeState(GameState.Playing);

            var zoneGo = Track(new GameObject("Pad"));
            zoneGo.transform.position = new Vector3(500f, 0f, 500f);
            zoneGo.AddComponent<BoxCollider>().size = new Vector3(4f, 4f, 4f);
            zoneGo.GetComponent<BoxCollider>().isTrigger = true;
            var zone = zoneGo.AddComponent<ExtractionZone>();
            zone.SetRaidDuration(600f);

            // Past the "just spawned beside the pad" grace period.
            typeof(ExtractionZone).GetField("_minimumRaidSecondsBeforeLeaving",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(zone, 0f);

            var loot = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            loot.transform.position = zoneGo.transform.position + Vector3.right;
            loot.AddComponent<Rigidbody>().isKinematic = true; // resting loot is kinematic
            var value = loot.AddComponent<LootValue>();
            typeof(LootValue).GetField("m_worth",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(value, 250f);

            var player = Track(GameObject.CreatePrimitive(PrimitiveType.Capsule));
            player.transform.position = zoneGo.transform.position;
            player.AddComponent<Rigidbody>().isKinematic = true;
            player.AddComponent<Body>();

            float worth = -1f;
            int saved = -1;
            zone.ExtractionResolved += (w, s) => { worth = w; saved = s; };

            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(1, zone.PiecesInZone,
                "Kinematic loot resting on the pad sends no trigger event; the poll must still see it.");
            Assert.IsTrue(zone.IsPlayerExtracting, "Standing on the pad must start the countdown.");

            float deadline = Time.time + GameServices.Extraction.DurationSeconds + 2f;
            while (worth < 0f && Time.time < deadline)
                yield return null;

            Assert.AreEqual(250f, worth, "The loot on the pad is what gets banked.");
            Assert.AreEqual(1, saved);
        }

        [UnityTest]
        public IEnumerator Test_SteppingOffThePadCancelsLeaving()
        {
            GameServices.GameState.ChangeState(GameState.Playing);

            var zoneGo = Track(new GameObject("Pad"));
            zoneGo.transform.position = new Vector3(-500f, 0f, 500f);
            zoneGo.AddComponent<BoxCollider>().size = new Vector3(4f, 4f, 4f);
            zoneGo.GetComponent<BoxCollider>().isTrigger = true;
            var zone = zoneGo.AddComponent<ExtractionZone>();
            typeof(ExtractionZone).GetField("_minimumRaidSecondsBeforeLeaving",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(zone, 0f);

            var player = Track(GameObject.CreatePrimitive(PrimitiveType.Capsule));
            player.transform.position = zoneGo.transform.position;
            player.AddComponent<Rigidbody>().isKinematic = true;
            player.AddComponent<Body>();

            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(zone.IsPlayerExtracting);

            player.transform.position += Vector3.right * 20f;
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(zone.IsPlayerExtracting, "Leaving the pad must stop the countdown.");
            Assert.IsFalse(zone.ExtractionComplete);
        }

        // --- Weight ----------------------------------------------------------------------------

        /// <summary>Seconds a held item of <paramref name="mass"/> takes to follow a 2 m jump of the hand.</summary>
        private IEnumerator MeasureCatchUp(float mass, float[] result, Vector3 origin)
        {
            var go = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            go.transform.position = origin;
            go.transform.localScale = Vector3.one * 0.3f;
            var body = go.AddComponent<Rigidbody>();
            body.mass = mass;
            var item = go.AddComponent<Item>();

            item.StartDragging();
            item.UpdateTargetPosition(origin);
            yield return new WaitForSeconds(0.3f);

            Vector3 target = origin + Vector3.right * 2f;
            item.UpdateTargetPosition(target);
            float start = Time.time;
            while (Vector3.Distance(go.transform.position, target) > 0.2f && Time.time - start < 5f)
                yield return new WaitForFixedUpdate();
            result[0] = Time.time - start;
            item.StopDragging();
        }

        [UnityTest]
        public IEnumerator Test_AHeavierThingLagsBehindTheHand()
        {
            var light = new float[1];
            var heavy = new float[1];
            yield return MeasureCatchUp(1f, light, new Vector3(0f, 200f, 800f));
            yield return MeasureCatchUp(15f, heavy, new Vector3(10f, 200f, 800f));

            Assert.Less(light[0], 0.6f, $"A 1 kg pot should keep up with the hand ({light[0]:0.00}s).");
            Assert.Greater(heavy[0], light[0] * 2f,
                $"A 15 kg chest must lag well behind a pot ({heavy[0]:0.00}s vs {light[0]:0.00}s): weight is felt.");
        }

        [Test]
        public void Test_AHeavyLoadSlowsTheCarrier()
        {
            var go = Track(new GameObject("Chest"));
            go.AddComponent<Rigidbody>().mass = 15f;
            var chest = go.AddComponent<Item>();
            var go2 = Track(new GameObject("Cup"));
            go2.AddComponent<Rigidbody>().mass = 1f;
            var cup = go2.AddComponent<Item>();

            Assert.AreEqual(1f, cup.CarrySpeedMultiplier);
            Assert.AreEqual(0.5f, chest.CarrySpeedMultiplier, 0.01f);
        }

        // --- A guard-free entrance -------------------------------------------------------------

        [Test]
        public void Test_NoGuardIsPostedNextToTheEntrance()
        {
            var generator = Track(new GameObject("CastleGen")).AddComponent<ProceduralCastleGenerator>();

            for (int seed = 1; seed <= 25; seed++)
            {
                ProceduralCastleData castle = generator.Generate(seed);
                Vector2Int entrance = castle.PlacedModules[castle.ExtractionExitIndex].GridPosition;

                foreach (GuardPlacement guard in GuardPlacementPlanner.Plan(castle, seed))
                {
                    Vector2Int at = castle.PlacedModules[guard.ModuleIndex].GridPosition;
                    int distance = Mathf.Max(Mathf.Abs(at.x - entrance.x), Mathf.Abs(at.y - entrance.y));
                    Assert.Greater(distance, GuardPlacementPlanner.SafeEntranceRadius,
                        $"Seed {seed}: a guard next to the spawn kills a player still reading the HUD.");
                }
            }
        }
    }
}
