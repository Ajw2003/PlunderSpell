using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.Alarm;
using Plunderspell.Guards;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>
    /// The pure rules in <see cref="GuardBrain"/> that the legacy guard's never-stand-still, follow-sound and
    /// keep-hunting behaviour (#193, #194, #195) was built on. The fresh guard (#214) does not use them: its
    /// Patrol, Investigate and Chase states replace the behaviour, and those are tested in GuardPatrolTests,
    /// GuardInvestigateTests and GuardChaseTests. They stay here, passing, until the owner approves removing
    /// the legacy guard and <see cref="GuardBrain"/>'s legacy helpers with it.
    /// </summary>
    public class GuardMovementTests
    {
        [Test]
        public void Test_StuckMeansUnderThirtyCentimetresInTheWindow()
        {
            Assert.IsTrue(GuardBrain.IsStuck(0f));
            Assert.IsTrue(GuardBrain.IsStuck(GuardBrain.StuckProgress - 0.01f));
            Assert.IsFalse(GuardBrain.IsStuck(GuardBrain.StuckProgress));
            Assert.AreEqual(1.5f, GuardBrain.StuckSeconds, 0.001f);
        }

        [Test]
        public void Test_TheSweepVisitsSpreadOutPointsWithinEightMetres()
        {
            var points = new List<Vector3>();
            for (int i = 0; i < 6; i++)
            {
                Vector3 offset = GuardBrain.SweepOffset(i);
                Assert.That(offset.magnitude, Is.InRange(3.99f, 8.01f), "Sweep point " + i);
                Assert.AreEqual(0f, offset.y, 0.0001f);
                points.Add(offset);
            }
            for (int a = 0; a < points.Count; a++)
            {
                for (int b = a + 1; b < points.Count; b++)
                {
                    Assert.Greater(Vector3.Distance(points[a], points[b]), 2f,
                        "Sweep points " + a + " and " + b + " must not sit on top of each other.");
                }
            }
        }

        [Test]
        public void Test_OnlyAHuntingGuardFollowsANoise()
        {
            const float loud = 0.6f;
            Assert.IsTrue(GuardBrain.ShouldFollowNoise(GuardAlertState.Searching, false, loud, AlarmState.Calm));
            Assert.IsTrue(GuardBrain.ShouldFollowNoise(GuardAlertState.Chasing, false, loud, AlarmState.Calm),
                "A chaser that has lost sight follows the noise.");
            Assert.IsFalse(GuardBrain.ShouldFollowNoise(GuardAlertState.Chasing, true, loud, AlarmState.Calm),
                "A chaser that can see its target does not.");
            Assert.IsFalse(GuardBrain.ShouldFollowNoise(GuardAlertState.Patrolling, false, loud, AlarmState.Calm),
                "A patrolling guard investigates instead; that path is unchanged.");
            Assert.IsFalse(GuardBrain.ShouldFollowNoise(GuardAlertState.Searching, false, 0.01f, AlarmState.Calm),
                "Background noise is not a lead.");
        }

        [Test]
        public void Test_OnlyTheHueAndCryKeepsSearchersAndInvestigatorsHunting()
        {
            Assert.IsTrue(GuardBrain.ShouldHunt(GuardAlertState.Searching, AlarmState.HueAndCry));
            Assert.IsTrue(GuardBrain.ShouldHunt(GuardAlertState.Investigating, AlarmState.HueAndCry));
            Assert.IsFalse(GuardBrain.ShouldHunt(GuardAlertState.Searching, AlarmState.Roused));
            Assert.IsFalse(GuardBrain.ShouldHunt(GuardAlertState.Chasing, AlarmState.HueAndCry));
            Assert.IsFalse(GuardBrain.ShouldHunt(GuardAlertState.Incapacitated, AlarmState.HueAndCry));
            Assert.IsFalse(GuardBrain.ShouldHunt(GuardAlertState.Patrolling, AlarmState.HueAndCry));
        }

        [Test]
        public void Test_TheHuntOffsetIsThreeToFiveMetresInAnyDirection()
        {
            for (float angle = 0f; angle <= 1f; angle += 0.125f)
            {
                for (float distance = 0f; distance <= 1f; distance += 0.25f)
                {
                    Assert.That(GuardBrain.HuntOffset(angle, distance).magnitude, Is.InRange(2.99f, 5.01f));
                }
            }
        }

        [Test]
        public void Test_TheHuntIsStaggeredPerGuardAndDueAfterAFewSeconds()
        {
            Assert.AreNotEqual(GuardBrain.HuntDelay(1), GuardBrain.HuntDelay(2));
            for (int id = -50; id < 50; id++)
            {
                Assert.That(GuardBrain.HuntDelay(id), Is.InRange(3f, 4f));
            }
            Assert.IsFalse(GuardBrain.HuntDue(1f, 7));
            Assert.IsTrue(GuardBrain.HuntDue(4f, 7));
        }
    }
}
