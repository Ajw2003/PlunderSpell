using NUnit.Framework;
using Plunderspell.Guards;

namespace Plunderspell.Tests.Editor
{
    /// <summary>
    /// The replicated attack signal (docs/plans/artbible-enemies-in-engine.md, E4): one integer
    /// carrying how many attacks and the kind of the latest, so a client never pairs a new count with
    /// a stale kind. The behaviour on a real guard is in <c>GuardAttackTests</c>.
    /// </summary>
    public class GuardAttackSignalTests
    {
        [Test]
        public void Test_ANewGuardHasNotAttacked()
        {
            Assert.AreEqual(0, GuardAttackSignal.Count(GuardAttackSignal.None));
        }

        [Test]
        public void Test_EachAttackCountsOnceAndCarriesItsKind()
        {
            int signal = GuardAttackSignal.None;

            signal = GuardAttackSignal.Next(signal, GuardAttackKind.Melee);
            Assert.AreEqual(1, GuardAttackSignal.Count(signal));
            Assert.AreEqual(GuardAttackKind.Melee, GuardAttackSignal.Kind(signal));

            signal = GuardAttackSignal.Next(signal, GuardAttackKind.Projectile);
            Assert.AreEqual(2, GuardAttackSignal.Count(signal));
            Assert.AreEqual(GuardAttackKind.Projectile, GuardAttackSignal.Kind(signal));

            signal = GuardAttackSignal.Next(signal, GuardAttackKind.Melee);
            Assert.AreEqual(3, GuardAttackSignal.Count(signal));
            Assert.AreEqual(GuardAttackKind.Melee, GuardAttackSignal.Kind(signal));
        }

        /// <summary>
        /// A SyncVar only replicates a change. Two melee swings in a row must still be two different
        /// values, or a client would see the first and miss the second.
        /// </summary>
        [Test]
        public void Test_TwoIdenticalAttacksAreStillTwoChanges()
        {
            int first = GuardAttackSignal.Next(GuardAttackSignal.None, GuardAttackKind.Melee);
            int second = GuardAttackSignal.Next(first, GuardAttackKind.Melee);
            Assert.AreNotEqual(first, second);
        }

        [Test]
        public void Test_TheCountWrapsWithoutGoingNegativeOrThrowing()
        {
            int nearTheTop = (int.MaxValue >> GuardAttackSignal.KindBits) << GuardAttackSignal.KindBits;
            int wrapped = GuardAttackSignal.Next(nearTheTop, GuardAttackKind.Projectile);

            Assert.AreNotEqual(nearTheTop, wrapped, "A wrap must still read as a change.");
            Assert.GreaterOrEqual(GuardAttackSignal.Count(wrapped), 0);
            Assert.AreEqual(GuardAttackKind.Projectile, GuardAttackSignal.Kind(wrapped));
        }
    }
}
