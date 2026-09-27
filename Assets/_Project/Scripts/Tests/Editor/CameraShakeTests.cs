using Interfaces;
using NUnit.Framework;
using Plunderspell.UI;
using Plunderspell.Voice;
using UnityEngine;

namespace Plunderspell.Tests.Editor
{
    /// <summary>
    /// Issue #51: the view shakes for casts, hits and hurts, sized by the event, and settles back.
    /// See docs/4-systems/damage.md, "Camera shake".
    /// </summary>
    public class CameraShakeTests
    {
        [Test]
        public void Test_NoTraumaMeansNoShake()
        {
            var trauma = new ShakeTrauma();
            Assert.AreEqual(Quaternion.identity, trauma.Step(0.016f));
        }

        [Test]
        public void Test_TraumaTurnsTheViewThenSettles()
        {
            var trauma = new ShakeTrauma();
            trauma.Add(1f);

            float biggest = 0f;
            for (int i = 0; i < 10; i++)
                biggest = Mathf.Max(biggest, Quaternion.Angle(Quaternion.identity, trauma.Step(0.016f)));
            Assert.Greater(biggest, 0.5f, "Full trauma must visibly turn the view.");
            Assert.LessOrEqual(biggest, ShakeTrauma.MaxTurnDegrees * 2f + ShakeTrauma.MaxRollDegrees,
                "The shake must stay small enough not to throw the aim.");

            for (int i = 0; i < 120; i++)
                trauma.Step(0.016f);
            Assert.AreEqual(0f, trauma.Trauma);
            Assert.AreEqual(Quaternion.identity, trauma.Step(0.016f), "Once drained, the view is exactly the look again.");
        }

        [Test]
        public void Test_TraumaCapsAtOne()
        {
            var trauma = new ShakeTrauma();
            trauma.Add(0.8f);
            trauma.Add(0.8f);
            Assert.AreEqual(1f, trauma.Trauma);
        }

        [Test]
        public void Test_AShoutShakesMoreThanAWhisper()
        {
            Assert.Greater(CameraShakeDirector.ForCast(CastVolume.Shout, 2f), CameraShakeDirector.ForCast(CastVolume.Normal, 2f));
            Assert.Greater(CameraShakeDirector.ForCast(CastVolume.Normal, 2f), CameraShakeDirector.ForCast(CastVolume.Whisper, 2f));
        }

        [Test]
        public void Test_AFarCastDoesNotShake()
        {
            Assert.Greater(CameraShakeDirector.ForCast(CastVolume.Shout, 1f), CameraShakeDirector.ForCast(CastVolume.Shout, 10f));
            Assert.AreEqual(0f, CameraShakeDirector.ForCast(CastVolume.Shout, CameraShakeDirector.CastReachMetres + 1f));
        }

        [Test]
        public void Test_ABigHitShakesMoreThanASmallOne()
        {
            Assert.Greater(CameraShakeDirector.ForHitTaken(40f, 100f, DamageKind.Melee),
                CameraShakeDirector.ForHitTaken(5f, 100f, DamageKind.Melee));
            Assert.Less(CameraShakeDirector.ForHitTaken(3f, 100f, DamageKind.Burn),
                CameraShakeDirector.ForHitTaken(3f, 100f, DamageKind.Melee), "A fire tick barely registers.");
        }

        [Test]
        public void Test_AKillShakesMoreThanAHit()
        {
            Assert.Greater(CameraShakeDirector.ForHitDealt(true), CameraShakeDirector.ForHitDealt(false));
        }

        [Test]
        public void Test_AFasterSlamShakesMore()
        {
            Assert.Greater(CameraShakeDirector.ForSlam(25f), CameraShakeDirector.ForSlam(5f));
        }
    }
}
