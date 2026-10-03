using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.Guards;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>
    /// A guard that chases but never lands a blow is scenery. See docs/4-systems/raid.md,
    /// "Guards that can actually hurt you". Ported to the fresh guard (#214): the legacy tests ticked a
    /// guard standing beside its victim; the fresh guard first sees, closes, then waits for an attack turn,
    /// so these step it for a second the way the game does (GuardCoreRig.StepTogether).
    /// </summary>
    public class GuardAttackTests
    {
        private const float StepSeconds = 0.05f;
        private const int OneSecondOfSteps = 20;

        private readonly GuardCoreRig _rig = new GuardCoreRig();

        [SetUp]
        public void SetUp() => _rig.SetUp();

        [TearDown]
        public void TearDown() => _rig.TearDown();

        [Test]
        public void Test_AGuardInReachActuallyDamagesTheIntruder()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            GuardCoreRig.Victim victim = _rig.MakeVictim(new Vector3(0f, 0f, 1.2f));

            _rig.StepTogether(3 * OneSecondOfSteps, StepSeconds, guard);

            Assert.Greater(victim.DamageTaken, 0f,
                "A guard standing beside an intruder must be able to hurt them.");
        }

        [Test]
        public void Test_AGuardOutOfReachDoesNotDamageTheIntruder()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            GuardCoreRig.Victim victim = _rig.MakeVictim(new Vector3(0f, 0f, 8f));

            // A fifth of a second is not enough to cross eight metres at chase speed.
            _rig.StepTogether(4, StepSeconds, guard);

            Assert.AreEqual(0f, victim.DamageTaken,
                "A melee guard must close the distance before it can hurt anyone.");
        }

        /// <summary>
        /// The cooldown is what keeps a chase survivable: without it a guard in contact damages the
        /// player every single frame, which reads as dying instantly for no visible reason.
        /// </summary>
        [Test]
        public void Test_AGuardCannotAttackEveryFrame()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            GuardCoreRig.Victim victim = _rig.MakeVictim(new Vector3(0f, 0f, 1.2f));
            float oneHit = guard.Tuning.AttackDamage;

            _rig.StepTogether(OneSecondOfSteps, StepSeconds, guard);

            Assert.Greater(victim.DamageTaken, 0f, "Sanity: it should have landed the first blow.");
            Assert.LessOrEqual(victim.DamageTaken, oneHit + 0.001f,
                "A second of contact inside one cooldown must still be a single hit.");
        }

        [Test]
        public void Test_AnIncapacitatedGuardDoesNotAttack()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            GuardCoreRig.Victim victim = _rig.MakeVictim(new Vector3(0f, 0f, 1.2f));
            guard.Status.Sleep(5f);

            _rig.StepTogether(OneSecondOfSteps, StepSeconds, guard);

            Assert.AreEqual(0f, victim.DamageTaken,
                "Somnus has to actually stop a guard, or the spell is decorative.");
        }

        // --- The replicated attack signal (docs/plans/artbible-enemies-in-engine.md, E4) ----------

        [Test]
        public void Test_EveryAttackBumpsTheReplicatedSignal()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            GuardCoreRig.Victim victim = _rig.MakeVictim(new Vector3(0f, 0f, 1.2f));
            var heard = new List<GuardAttackKind>();
            guard.AttackSignal.Attacked += heard.Add;
            Assert.AreEqual(0, guard.AttackSignal.Count, "No attack yet.");

            _rig.StepTogether(OneSecondOfSteps, StepSeconds, guard);

            Assert.Greater(victim.DamageTaken, 0f, "Sanity: the blow landed.");
            Assert.AreEqual(1, guard.AttackSignal.Count, "One blow, one count: this is what every client sees.");
            Assert.AreEqual(GuardAttackKind.Melee, guard.AttackSignal.LastKind);
            CollectionAssert.AreEqual(new[] { GuardAttackKind.Melee }, heard,
                "The server raises Attacked as the blow lands.");
        }

        [Test]
        public void Test_AGuardOutOfReachSignalsNoAttack()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            _rig.MakeVictim(new Vector3(0f, 0f, 8f));

            _rig.StepTogether(4, StepSeconds, guard);

            Assert.AreEqual(0, guard.AttackSignal.Count, "No swing at thin air.");
        }
    }
}
