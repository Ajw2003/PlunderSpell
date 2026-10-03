using NUnit.Framework;
using Plunderspell.Acoustics;
using Plunderspell.Guards;
using Plunderspell.Loot;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>What makes noise for the guards (#238): a player's pace and landing, and loot dropped or dragged. Heavier and faster is further and louder.</summary>
    public class NoiseSourceTests
    {
        private GuardCoreRig _rig;
        private GameObject _player;

        [SetUp]
        public void SetUp()
        {
            _rig = new GuardCoreRig();
            _rig.SetUp();
        }

        [TearDown]
        public void TearDown()
        {
            if (_player != null)
                Object.DestroyImmediate(_player);
            _rig.TearDown();
        }

        private Guard MakeListeningGuard(Vector3 position)
        {
            Guard guard = _rig.MakeGuard(position);
            guard.gameObject.AddComponent<CapsuleCollider>();
            Physics.SyncTransforms();
            return guard;
        }

        private FootstepNoiseEmitter MakePlayerAt(Vector3 position)
        {
            _player = new GameObject("NoisyPlayer");
            _player.transform.position = position;
            return _player.AddComponent<FootstepNoiseEmitter>();
        }

        // ---------------------------------------------------------------- Pace

        [TestCase(0.5f, MoveStance.Crouch)]
        [TestCase(3f, MoveStance.Walk)]
        [TestCase(5f, MoveStance.Run)]
        public void PaceDecidesTheStance(float speed, MoveStance expected)
        {
            Assert.That(FootstepNoiseEmitter.StanceForSpeed(speed), Is.EqualTo(expected));
        }

        [Test]
        public void ASlowStepIsHeardOnlyUpCloseAFastOneFromFar()
        {
            Guard guard = MakeListeningGuard(Vector3.zero);
            FootstepNoiseEmitter player = MakePlayerAt(new Vector3(0f, 0f, -5f));

            player.OnFootstep(FootstepNoiseEmitter.StanceForSpeed(1f));
            Assert.That(guard.Hearing.NoticedCount, Is.EqualTo(0), "a creeping step carries 1.5 m, not 5");

            player.OnFootstep(FootstepNoiseEmitter.StanceForSpeed(5f));
            Assert.That(guard.Hearing.NoticedCount, Is.EqualTo(1), "a full-pace step carries 8 m");
        }

        // ---------------------------------------------------------------- Landing

        [Test]
        public void AJumpLandingIsHeardBehindAGuardAndASmallStepDownIsNot()
        {
            Guard guard = MakeListeningGuard(Vector3.zero);
            FootstepNoiseEmitter player = MakePlayerAt(new Vector3(0f, 0f, -5f));

            player.OnLanding(1.5f);
            Assert.That(guard.Hearing.NoticedCount, Is.EqualTo(0), "stepping off a stair is silent");

            player.OnLanding(4.6f);
            Assert.That(guard.Hearing.NoticedCount, Is.EqualTo(1), "a jump's landing carries 6 m");
        }

        [Test]
        public void ACrashLandingCarriesFurtherThanAJumpLanding()
        {
            Guard guard = MakeListeningGuard(Vector3.zero);
            FootstepNoiseEmitter player = MakePlayerAt(new Vector3(0f, 0f, -10f));

            player.OnLanding(4.6f);
            Assert.That(guard.Hearing.NoticedCount, Is.EqualTo(0), "10 m is past a jump's 6 m");

            player.OnLanding(11f);
            Assert.That(guard.Hearing.NoticedCount, Is.EqualTo(1), "a long drop carries 12 m");
        }

        // ---------------------------------------------------------------- Loot

        [Test]
        public void HeavyLootIsHeardFurtherThanLightLoot()
        {
            Assert.That(LootNoise.ImpactRadius(16f, 4f), Is.GreaterThan(LootNoise.ImpactRadius(1.5f, 4f) * 2.5f));
            Assert.That(LootNoise.ImpactRadius(16f, 8f), Is.GreaterThan(LootNoise.ImpactRadius(16f, 4f)), "harder is further");
            Assert.That(LootNoise.ImpactRadius(1000f, 20f), Is.EqualTo(LootNoise.MaxRadius), "capped");
            Assert.That(LootNoise.ImpactRadius(16f, 1f), Is.EqualTo(0f), "set down gently makes no noise");
        }

        [Test]
        public void ADroppedHeavyChestIsHeardAcrossTheRoomButALedgerIsNot()
        {
            Guard guard = MakeListeningGuard(Vector3.zero);
            var origin = new Vector3(0f, 0f, -6f);

            LootNoise.BroadcastImpact(origin, 1.5f, 4f);
            Assert.That(guard.Hearing.NoticedCount, Is.EqualTo(0), "a 1.5 kg ledger carries about 2.4 m");

            LootNoise.BroadcastImpact(origin, 16f, 4f);
            Assert.That(guard.Hearing.NoticedCount, Is.EqualTo(1), "a 16 kg chest carries 8 m");
        }

        [Test]
        public void ADraggedHeavyPieceScrapesFurtherTheHeavierItIs()
        {
            Guard guard = MakeListeningGuard(Vector3.zero);
            var origin = new Vector3(0f, 0f, -6f);

            LootNoise.BroadcastDrag(origin, 4f);
            Assert.That(guard.Hearing.NoticedCount, Is.EqualTo(0), "a 4 kg piece scrapes 4 m");

            LootNoise.BroadcastDrag(origin, 15f);
            Assert.That(guard.Hearing.NoticedCount, Is.EqualTo(1), "a 15 kg piece scrapes 7.7 m");
        }
    }
}
