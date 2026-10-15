using Interfaces;
using NUnit.Framework;
using Plunderspell.Acoustics;
using Plunderspell.Guards;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>The Lantern Warden's turn into the Wick (#417): what turns it, what does not, that it never turns back, that a catch kills, and that the human form is stunned by a hard blow to the head and never killed.</summary>
    public class GuardTrueFormTests
    {
        private const float Step = 0.05f;
        private static readonly Vector3 Chest = new Vector3(0f, 1.2f, 0f);
        private static readonly Vector3 Head = new Vector3(0f, 1.75f, 0f);

        private GuardCoreRig _rig;

        [SetUp]
        public void SetUp()
        {
            _rig = new GuardCoreRig();
            _rig.SetUp();
        }

        [TearDown]
        public void TearDown() => _rig.TearDown();

        private Guard MakeWarden()
        {
            Guard warden = _rig.MakeGuard(Vector3.zero);
            warden.Tuning.TrueForm.CanTurn = true;
            return warden;
        }

        // The status receiver counts its own timers in Update; a test with no frames ticks it by hand.
        private void Advance(Guard guard, float seconds)
        {
            for (int i = 0; i < Mathf.CeilToInt(seconds / Step); i++)
            {
                guard.Status.Tick(Step);
                _rig.StepTogether(1, Step, guard);
            }
        }

        private static float Hit(Guard guard, float damage, Vector3 point) =>
            Damage.Apply(guard, damage, null, null, guard.transform.position + point, DamageKind.Melee);

        [Test]
        public void ConfirmingSightOfAPlayerTurnsIt()
        {
            Guard warden = MakeWarden();
            _rig.MakeIntruder(new Vector3(0f, 0f, 6f));

            Advance(warden, 6f);

            Assert.That(warden.IsTurned, Is.True);
            Assert.That(warden.Tuning.ChaseSpeed, Is.EqualTo(4.2f));
        }

        [Test]
        public void HearingOnlyInvestigatesAndNeverTurns()
        {
            Guard warden = MakeWarden();

            warden.OnNoiseHeard(new NoiseEvent(new Vector3(0f, 0f, 8f), 0.9f, NoiseType.Footstep));
            Advance(warden, 1f);

            Assert.That(warden.IsTurned, Is.False);
            Assert.That(warden.CurrentState, Is.SameAs(warden.States.Investigate));
        }

        [Test]
        public void BeingHitTurnsIt()
        {
            Guard warden = MakeWarden();

            Hit(warden, 5f, Chest);

            Assert.That(warden.CurrentState, Is.SameAs(warden.States.Turning));
            Assert.That(warden.IsTurned, Is.True);
        }

        [Test]
        public void ASomnusThatWakesItWithTheCastTurnsItButAQuietNoiseDoesNot()
        {
            Guard warden = MakeWarden();
            warden.Status.Sleep(30f);

            warden.OnNoiseHeard(new NoiseEvent(Vector3.forward * 8f, 0.3f, NoiseType.VoiceCast));
            Assert.That(warden.IsTurned, Is.False, "too quiet to wake it");

            warden.OnNoiseHeard(new NoiseEvent(Vector3.forward * 8f, 0.6f, NoiseType.VoiceCast));
            Assert.That(warden.IsTurned, Is.True);
        }

        [Test]
        public void ItNeverTurnsBack()
        {
            Guard warden = MakeWarden();
            Hit(warden, 5f, Chest);
            Advance(warden, 3f);

            Hit(warden, 5f, Chest);
            warden.Status.Stun(1f);
            Advance(warden, 3f);

            Assert.That(warden.IsTurned, Is.True);
            Assert.That(warden.Tuning.PatrolSpeed, Is.EqualTo(1.4f), "still the Wick's stalk");
            Assert.That(warden.CurrentState, Is.Not.SameAs(warden.States.Turning), "it does not turn twice");
        }

        [Test]
        public void ACatchKillsThePlayer()
        {
            Guard warden = MakeWarden();
            GuardCoreRig.Victim victim = _rig.MakeVictim(new Vector3(0f, 0f, 1f));
            Advance(warden, 5f); // sees the player, turns, hunts, catches

            Assert.That(warden.IsTurned, Is.True);
            Assert.That(victim.DamageTaken, Is.GreaterThanOrEqualTo(victim.MaxHealth));
        }

        [Test]
        public void ABlowToTheHeadStunsTheHumanFormAndNothingKillsIt()
        {
            Guard warden = MakeWarden();

            Hit(warden, 25f, Head);
            Assert.That(warden.Status.IsStunned, Is.True);
            Assert.That(warden.IsTurned, Is.False, "knocked down, not yet turned");

            for (int blow = 0; blow < 10; blow++)
                Hit(warden, 1000f, Head);
            Assert.That(warden.IsDead, Is.False);
            Assert.That(warden.CurrentHealth, Is.GreaterThanOrEqualTo(1f));
        }

        [Test]
        public void AGuardThatCannotTurnKeepsItsDeathAndDoesNotTurn()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);

            Hit(guard, 5f, Chest);
            Assert.That(guard.IsTurned, Is.False);

            Hit(guard, 1000f, Chest);
            Assert.That(guard.IsDead, Is.True);
        }
    }
}
