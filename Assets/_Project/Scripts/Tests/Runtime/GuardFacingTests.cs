using NUnit.Framework;
using Plunderspell.Alarm;
using Plunderspell.Guards;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>Walking guards face where they go (#211): turned smoothly by the navigation service, and left alone for a Combat move so the Combat state's facing wins.</summary>
    public class GuardFacingTests
    {
        private const float Step = 0.05f;

        private GuardCoreRig _rig;

        [SetUp]
        public void SetUp()
        {
            _rig = new GuardCoreRig();
            _rig.SetUp();
        }

        [TearDown]
        public void TearDown() => _rig.TearDown();

        // Only the navigation service runs: the guard's own state would otherwise replace the test's move with its own.
        private void RunService(int steps)
        {
            for (int i = 0; i < steps; i++)
                _rig.Director.Navigation.Tick(Step);
        }

        [Test]
        public void AWalkingGuardTurnsSmoothlyToFaceItsDirectionOfTravel()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            guard.transform.rotation = Quaternion.identity; // facing +z
            guard.Navigator.MoveTo(new Vector3(10f, 0f, 0f), 3f, MoveReason.Patrol);

            RunService(1);
            float firstStepTurn = Vector3.Angle(Vector3.forward, guard.transform.forward);
            Assert.That(firstStepTurn, Is.GreaterThan(0f).And.LessThan(90f), "turned a little, not snapped round");

            RunService(20);
            Assert.That(Vector3.Angle(Vector3.right, guard.transform.forward), Is.LessThan(1f), "facing east, where it walks");
        }

        [Test]
        public void ACombatMoveIsNotTurnedByTheNavigationService()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            guard.transform.rotation = Quaternion.identity;
            guard.Navigator.MoveTo(new Vector3(10f, 0f, 0f), 3f, MoveReason.Combat);

            RunService(20);

            Assert.That(Vector3.Angle(Vector3.forward, guard.transform.forward), Is.LessThan(0.01f), "Combat keeps its own facing");
        }
    }
}
