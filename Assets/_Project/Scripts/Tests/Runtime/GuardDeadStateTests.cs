using System.Collections;
using NUnit.Framework;
using Plunderspell.Guards;
using UnityEngine;
using UnityEngine.TestTools;

namespace Plunderspell.Tests
{
    /// <summary>The Dead state (#213): the guard leaves the shared systems, falls over, fades and is removed.</summary>
    public class GuardDeadStateTests
    {
        private const float Step = 0.02f;

        private GuardCoreRig _rig;

        [SetUp]
        public void SetUp()
        {
            _rig = new GuardCoreRig();
            _rig.SetUp();
        }

        [TearDown]
        public void TearDown() => _rig.TearDown();

        [Test]
        public void DeathLeavesNavigationAndTheRegistryAndReleasesTheTurn()
        {
            GuardCoreRig.Victim victim = _rig.MakeVictim(new Vector3(0f, 0f, 1.5f));
            Guard guard = _rig.MakeGuard(Vector3.zero);
            guard.AttackTurn.Begin();
            guard.AttackTurn.Request(victim.transform, false);
            Assert.That(_rig.Director.AttackTurns.Holds(guard), Is.True, "setup: the guard holds a turn");
            Assert.That(_rig.Director.Navigation.MoverCount, Is.EqualTo(1), "setup: the guard has a mover");

            guard.TakeDamage(500f);

            Assert.That(_rig.Director.Navigation.MoverCount, Is.EqualTo(0));
            Assert.That(_rig.Director.Guards.Count, Is.EqualTo(0));
            Assert.That(_rig.Director.AttackTurns.Holds(guard), Is.False);
        }

        [Test]
        public void NothingMovesAGuardOutOfDead()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            guard.TakeDamage(500f);

            guard.Status.Sleep(30f);
            _rig.Director.RegisterIntruder(new GameObject("Late").transform);
            for (int i = 0; i < 100; i++)
                guard.Tick(Step);

            Assert.That(guard.State, Is.EqualTo(GuardAlertState.Dead));
            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Dead));
        }

        [Test]
        public void TheBodyLeavesTheUprightPose()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            GuardDeathPlayback playback = guard.GetComponent<GuardDeathPlayback>();

            guard.TakeDamage(500f);
            playback.Step(guard.Tuning.ToppleSeconds + Step);

            Assert.That(Vector3.Angle(guard.transform.up, Vector3.up), Is.GreaterThan(80f));
        }

        [UnityTest]
        public IEnumerator TheGuardIsRemovedOnlyAfterTheFadeTime()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            GuardDeathPlayback playback = guard.GetComponent<GuardDeathPlayback>();
            GuardTuning tuning = guard.Tuning;
            float total = tuning.ToppleSeconds + tuning.LingerSeconds + tuning.FadeSeconds;

            guard.TakeDamage(500f);
            playback.Step(total - 0.1f);
            yield return null;
            Assert.That(guard != null, Is.True, "still fading, so not yet removed");

            playback.Step(0.2f);
            yield return null;
            Assert.That(guard == null, Is.True, "the fade is over, so the server removed it");
        }

        [Test]
        public void ASecondDeathOrHitIsHarmless()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            int deaths = 0;
            _rig.Director.OnGuardDied += died => deaths++;

            guard.TakeDamage(500f);
            guard.TakeDamage(500f);
            guard.ChangeState(guard.States.Dead);
            guard.Tick(Step);

            Assert.That(deaths, Is.EqualTo(1));
            Assert.That(guard.State, Is.EqualTo(GuardAlertState.Dead));
        }
    }
}
