using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.Raid;
using UnityEngine;

namespace Plunderspell.Tests.Editor
{
    /// <summary>
    /// The Ophan of Hours' rules (#418, approved 2026-10-27): arrival, the lock-on, the slow beam and the stone that stops
    /// it, the charged beam after hiding, the take that spares an artifact's carrier, and leaving once paid. Its target
    /// order and where it hangs in the sky.
    /// </summary>
    public class OphanHuntTests
    {
        private const float Step = 0.05f;
        private OphanTuning _tuning;
        private OphanHunt _hunt;

        [SetUp]
        public void SetUp()
        {
            _tuning = new OphanTuning();
            _hunt = new OphanHunt(_tuning);
        }

        /// <summary>Ticks for <paramref name="seconds"/> with the same facts, summing what it asks for.</summary>
        private (float drain, int charged, int takes, bool gone) Run(float seconds, bool target = true, bool cover = false,
            bool artifact = false)
        {
            float drain = 0f;
            int charged = 0, takes = 0;
            bool gone = false;
            for (float t = 0f; t < seconds - 1e-4f; t += Step)
            {
                OphanStep step = _hunt.Tick(Step, target, cover, artifact);
                drain += step.Drain;
                charged += step.ChargedFire ? 1 : 0;
                takes += step.Take ? 1 : 0;
                gone |= step.Gone;
            }
            return (drain, charged, takes, gone);
        }

        [Test]
        public void Test_ItArrivesThenWatchesThenLocksOnBeforeAnyDamage()
        {
            _hunt.Arrive();
            Assert.AreEqual(OphanState.Arriving, _hunt.State);
            // Arriving gives way to Watching on one step and Watching to Locking on the next.
            Assert.AreEqual(0f, Run(_tuning.ArriveSeconds + 2 * Step).drain, "Nothing is hurt while it unfolds.");
            Assert.AreEqual(OphanState.Locking, _hunt.State, "A target in the open is locked on to.");
            Assert.AreEqual(0f, Run(_tuning.LockSeconds - 2 * Step).drain, "The 3-second lock-on deals nothing.");
            Run(3 * Step);
            Assert.AreEqual(OphanState.Draining, _hunt.State);
        }

        [Test]
        public void Test_TheSlowBeamKillsSomeoneInTheOpenInFourSeconds()
        {
            ToDraining();
            float drained = Run(4f).drain;
            Assert.That(drained, Is.EqualTo(100f).Within(_tuning.SlowBeamDamagePerSecond * _tuning.DamageStepSeconds),
                "25 health a second takes a full 100 in about 4 seconds.");
        }

        [Test]
        public void Test_StoneStopsTheSlowBeamAndEscapesTheLockOn()
        {
            _hunt.Arrive();
            Run(_tuning.ArriveSeconds + Step);
            Run(1f);
            Run(Step, cover: true);
            Assert.AreEqual(OphanState.Watching, _hunt.State, "Getting behind stone during the lock-on escapes it.");

            ToDraining();
            Assert.AreEqual(0f, Run(2f, cover: true).drain, "Behind stone, the slow beam does nothing.");
        }

        [Test]
        public void Test_HidingTooLongBringsTheChargedBeamWhichFiresThroughStone()
        {
            ToDraining();
            Run(_tuning.CoverSecondsBeforeCharge + Step, cover: true);
            Assert.AreEqual(OphanState.Charging, _hunt.State, "3 seconds behind cover starts the charge.");
            var (_, charged, _, _) = Run(_tuning.ChargeSeconds + Step, cover: true);
            Assert.AreEqual(1, charged, "After a 3-second charge the charged beam fires, cover or not.");
            Assert.AreEqual(OphanState.Draining, _hunt.State);

            Assert.AreEqual(0, Run(_tuning.ChargeCooldownSeconds - 1f, cover: true).charged, "It waits 10 seconds to charge again.");
        }

        [Test]
        public void Test_ABeamOnAnArtifactsCarrierTakesItAndTheOphanLeaves()
        {
            ToDraining();
            var (drain, _, takes, _) = Run(Step, artifact: true);
            Assert.AreEqual(1, takes, "The beam lifts the wanted piece away.");
            Assert.AreEqual(0f, drain, "The carrier is spared.");
            Assert.AreEqual(OphanState.Leaving, _hunt.State, "Paid, it leaves at once.");
            Assert.IsTrue(Run(_tuning.LeaveSeconds + Step).gone);
            Assert.AreEqual(OphanState.Absent, _hunt.State);
            Assert.AreEqual(0f, Run(5f).drain, "Once gone it hurts no one.");
        }

        [Test]
        public void Test_TheChargedBeamTakesAnArtifactInsteadOfALife()
        {
            ToDraining();
            Run(_tuning.CoverSecondsBeforeCharge + Step, cover: true, artifact: true);
            var (_, charged, takes, _) = Run(_tuning.ChargeSeconds + Step, cover: true, artifact: true);
            Assert.AreEqual(0, charged, "No killing blow on the carrier.");
            Assert.AreEqual(1, takes);
            Assert.AreEqual(OphanState.Leaving, _hunt.State);
        }

        [Test]
        public void Test_WithNobodyLeftItWatches()
        {
            ToDraining();
            Run(Step, target: false);
            Assert.AreEqual(OphanState.Watching, _hunt.State, "After a kill it waits for the next target.");
        }

        [Test]
        public void Test_ItTargetsTheMostWorthThenDownTheList()
        {
            var players = new List<OphanCandidate<string>>
            {
                new OphanCandidate<string>("light", 40f, true),
                new OphanCandidate<string>("richest", 900f, true),
                new OphanCandidate<string>("second", 300f, true),
            };
            Assert.IsTrue(OphanTargeting.TryPick(players, out string first));
            Assert.AreEqual("richest", first);

            players[1] = new OphanCandidate<string>("richest", 900f, false);
            OphanTargeting.TryPick(players, out string next);
            Assert.AreEqual("second", next, "With the richest dead, the next down the list.");

            Assert.IsFalse(OphanTargeting.TryPick(new List<OphanCandidate<string>>(), out string _));
        }

        [Test]
        public void Test_ItHangsFarOffBeyondTheCastleFacingIt()
        {
            var castle = new Vector3(6f, 0f, 0f);
            var arrival = new Vector3(6f, 1f, -40f);
            Vector3 centre = OphanSkyPlacement.Centre(castle, arrival, _tuning);
            Assert.That(new Vector2(centre.x - castle.x, centre.z - castle.z).magnitude, Is.EqualTo(900f).Within(0.01f));
            Assert.Greater(centre.z, castle.z, "Across the castle from where the players arrive.");
            Assert.That(centre.y, Is.EqualTo(900f * Mathf.Tan(30f * Mathf.Deg2Rad)).Within(0.01f));
            Vector3 forward = OphanSkyPlacement.Facing(centre, castle) * Vector3.forward;
            Assert.Less(Vector3.Angle(forward, new Vector3(castle.x, 1.65f, castle.z) - centre), 0.01f, "Its eye faces the castle.");
        }

        private void ToDraining()
        {
            _hunt = new OphanHunt(_tuning);
            _hunt.Arrive();
            Run(_tuning.ArriveSeconds + Step);
            Run(_tuning.LockSeconds + 2 * Step);
            Assert.AreEqual(OphanState.Draining, _hunt.State);
        }
    }
}
