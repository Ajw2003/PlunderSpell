using NUnit.Framework;
using Plunderspell.Extraction;
using Plunderspell.Guards;

namespace Plunderspell.Tests.Editor
{
    /// <summary>The arrival portal closes after the guards' grace; the escape portal opens with the hunt window.</summary>
    public class PortalScheduleTests
    {
        private const float Raid = 300f;
        private const float Hunt = 60f;

        private static PortalState At(float elapsed) =>
            PortalSchedule.At(elapsed, Raid - elapsed, PortalSchedule.ArrivalSeconds, Hunt);

        [Test] public void OpenAtArrival() => Assert.AreEqual(PortalState.Arrival, At(0f));
        [Test] public void StillOpenJustBeforeGraceEnds() => Assert.AreEqual(PortalState.Arrival, At(19.9f));
        [Test] public void ClosedWhenGraceEnds() => Assert.AreEqual(PortalState.Closed, At(20f));
        [Test] public void ClosedJustBeforeHuntWindow() => Assert.AreEqual(PortalState.Closed, At(Raid - Hunt - 0.1f));
        [Test] public void EscapeWhenHuntWindowOpens() => Assert.AreEqual(PortalState.Escape, At(Raid - Hunt));
        [Test] public void EscapeAtTheEnd() => Assert.AreEqual(PortalState.Escape, At(Raid));

        [Test]
        public void EscapeWinsWhenTheRaidIsShorterThanTheGrace() =>
            Assert.AreEqual(PortalState.Escape, PortalSchedule.At(5f, 50f, 20f, Hunt));

        [Test]
        public void ArrivalSecondsMatchTheGuardGrace() =>
            Assert.AreEqual(GuardArrivalGrace.Seconds, PortalSchedule.ArrivalSeconds);
    }
}
