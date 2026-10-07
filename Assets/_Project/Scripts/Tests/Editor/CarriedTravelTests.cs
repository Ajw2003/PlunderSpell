using NUnit.Framework;
using Plunderspell.Raid;
using UnityEngine;

namespace Plunderspell.Tests.Editor
{
    /// <summary>The offset a carried piece keeps from its holder across a room change (#333).</summary>
    public class CarriedTravelTests
    {
        [Test]
        public void PieceKeepsItsOffsetWhenTheHolderOnlyMoves()
        {
            var piece = new Pose(new Vector3(1001f, 1.5f, 2f), Quaternion.identity);
            Pose moved = CarriedTravel.Moved(piece, new Vector3(1000f, 1f, 0f), new Vector3(1100f, 1f, 5f), 0f);
            Assert.That((moved.position - new Vector3(1101f, 1.5f, 7f)).magnitude, Is.LessThan(1e-4f));
        }

        [Test]
        public void PieceSwingsRoundTheHolderWhenTheyTurn()
        {
            var piece = new Pose(new Vector3(0f, 1f, 2f), Quaternion.identity);
            Pose moved = CarriedTravel.Moved(piece, Vector3.zero, new Vector3(100f, 0f, 0f), 90f);
            Assert.That((moved.position - new Vector3(102f, 1f, 0f)).magnitude, Is.LessThan(1e-4f));
            Assert.That(Quaternion.Angle(moved.rotation, Quaternion.Euler(0f, 90f, 0f)), Is.LessThan(1e-3f));
        }
    }
}
