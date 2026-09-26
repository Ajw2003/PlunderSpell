using System.Collections;
using System.Collections.Generic;
using Interfaces;
using NUnit.Framework;
using RogueAi.Guards;
using RogueAi.Status;
using UnityEngine;
using UnityEngine.TestTools;

namespace RogueAi.Tests
{
    /// <summary>
    /// A guard that chases but never lands a blow is scenery. See docs/systems/raid.md,
    /// "Guards that can actually hurt you".
    /// </summary>
    public class GuardAttackTests
    {
        private readonly List<Object> m_tracked = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            CastleGuard.ClearIntruders();
            CastleGuard.EndArrivalGrace();
        }

        [TearDown]
        public void TearDown()
        {
            CastleGuard.ClearIntruders();
            foreach (Object o in m_tracked)
            {
                if (o != null)
                {
                    Object.DestroyImmediate(o);
                }
            }

            m_tracked.Clear();
        }

        private T Track<T>(T o) where T : Object
        {
            m_tracked.Add(o);
            return o;
        }

        /// <summary>A body guards can see, that records what it has been hit for.</summary>
        private sealed class Victim : MonoBehaviour, IHealth
        {
            public float Taken;
            public float CurrentHealth => 100f - Taken;
            public float MaxHealth => 100f;
            public void TakeDamage(float damage) => Taken += damage;
            public void TakeDamage(float damage, float impactVelocity) => TakeDamage(damage);
        }

        private CastleGuard MakeGuard(Vector3 position)
        {
            var go = Track(new GameObject("Guard"));
            go.transform.position = position;
            go.AddComponent<StatusEffectReceiver>();
            var guard = go.AddComponent<CastleGuard>();
            // Disabled so the guard's own Update cannot tick it during the test's yield frame: with
            // the victim in reach it would swing there, and the test would be counting that blow
            // instead of the one its explicit Tick lands. Awake has already run.
            guard.enabled = false;
            return guard;
        }

        private Victim MakeVictim(Vector3 position)
        {
            var go = Track(new GameObject("Victim"));
            go.transform.position = position;
            go.AddComponent<BoxCollider>();
            var victim = go.AddComponent<Victim>();
            CastleGuard.RegisterIntruder(go.transform);
            return victim;
        }

        [UnityTest]
        public IEnumerator Test_AGuardInReachActuallyDamagesTheIntruder()
        {
            CastleGuard guard = MakeGuard(Vector3.zero);
            Victim victim = MakeVictim(new Vector3(0f, 0f, 1.2f));
            guard.transform.LookAt(victim.transform);

            yield return null;

            guard.Tick(0.1f);

            Assert.Greater(victim.Taken, 0f,
                "A guard standing on top of an intruder must be able to hurt them.");
        }

        [UnityTest]
        public IEnumerator Test_AGuardOutOfReachDoesNotDamageTheIntruder()
        {
            CastleGuard guard = MakeGuard(Vector3.zero);
            Victim victim = MakeVictim(new Vector3(0f, 0f, 8f));
            guard.transform.LookAt(victim.transform);

            yield return null;

            guard.Tick(0.1f);

            Assert.AreEqual(0f, victim.Taken,
                "A melee guard must close the distance before it can hurt anyone.");
        }

        /// <summary>
        /// The cooldown is what keeps a chase survivable: without it a guard in contact damages the
        /// player every single frame, which reads as dying instantly for no visible reason.
        /// </summary>
        [UnityTest]
        public IEnumerator Test_AGuardCannotAttackEveryFrame()
        {
            CastleGuard guard = MakeGuard(Vector3.zero);
            Victim victim = MakeVictim(new Vector3(0f, 0f, 1.2f));
            guard.transform.LookAt(victim.transform);

            yield return null;

            for (int i = 0; i < 10; i++)
            {
                guard.Tick(0.01f);
            }

            float afterBurst = victim.Taken;
            Assert.Greater(afterBurst, 0f, "Sanity: it should have landed the first blow.");

            for (int i = 0; i < 10; i++)
            {
                guard.Tick(0.01f);
            }

            Assert.AreEqual(afterBurst, victim.Taken,
                "Twenty ticks inside one cooldown must still be a single hit.");
        }

        [UnityTest]
        public IEnumerator Test_AnIncapacitatedGuardDoesNotAttack()
        {
            CastleGuard guard = MakeGuard(Vector3.zero);
            Victim victim = MakeVictim(new Vector3(0f, 0f, 1.2f));
            guard.transform.LookAt(victim.transform);
            guard.GetComponent<StatusEffectReceiver>().Sleep(5f);

            yield return null;

            guard.Tick(0.1f);

            Assert.AreEqual(0f, victim.Taken,
                "Somnus has to actually stop a guard, or the spell is decorative.");
        }

        // --- The replicated attack signal (docs/plans/artbible-enemies-in-engine.md, E4) ----------

        [UnityTest]
        public IEnumerator Test_EveryAttackBumpsTheReplicatedSignal()
        {
            CastleGuard guard = MakeGuard(Vector3.zero);
            Victim victim = MakeVictim(new Vector3(0f, 0f, 1.2f));
            guard.transform.LookAt(victim.transform);
            var heard = new List<GuardAttackKind>();
            guard.Attacked += heard.Add;

            yield return null;

            Assert.AreEqual(0, guard.AttackCount, "No attack yet.");
            guard.Tick(0.1f);

            Assert.Greater(victim.Taken, 0f, "Sanity: the blow landed.");
            Assert.AreEqual(1, guard.AttackCount, "One blow, one count: this is what every client sees.");
            Assert.AreEqual(GuardAttackKind.Melee, guard.LastAttackKind);
            CollectionAssert.AreEqual(new[] { GuardAttackKind.Melee }, heard,
                "The server raises Attacked as the blow lands.");
        }

        [UnityTest]
        public IEnumerator Test_AnAttackOnCooldownSignalsNothing()
        {
            CastleGuard guard = MakeGuard(Vector3.zero);
            Victim victim = MakeVictim(new Vector3(0f, 0f, 1.2f));
            guard.transform.LookAt(victim.transform);

            yield return null;

            for (int i = 0; i < 10; i++)
            {
                guard.Tick(0.01f);
            }

            Assert.AreEqual(1, guard.AttackCount,
                "Ten ticks inside one cooldown are one attack; a swing per tick would be an animation per frame.");
        }

        [UnityTest]
        public IEnumerator Test_AGuardOutOfReachSignalsNoAttack()
        {
            CastleGuard guard = MakeGuard(Vector3.zero);
            Victim victim = MakeVictim(new Vector3(0f, 0f, 8f));
            guard.transform.LookAt(victim.transform);

            yield return null;

            guard.Tick(0.1f);

            Assert.AreEqual(0, guard.AttackCount, "No swing at thin air.");
        }
    }
}
