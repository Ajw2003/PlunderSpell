using NUnit.Framework;
using Plunderspell.Lair;
using UnityEngine;

namespace Plunderspell.Tests.Editor
{
    /// <summary>The haul pile save keeps each piece's pose, and still reads the older names-only saves.</summary>
    public class HaulPileSaveTests
    {
        private const int TestSlot = 91;

        [TearDown]
        public void TearDown() => HaulPileSave.Clear(TestSlot);

        [Test]
        public void Test_NamesWithPosesRoundTrip()
        {
            var pose = Quaternion.Euler(10f, 200f, 30f);
            HaulPileSave.SaveEntries(TestSlot, new[] { new HaulPileEntry("Goblet", new Vector3(1000.25f, 0.5f, -3.125f), pose) });

            HaulPileEntry[] loaded = HaulPileSave.LoadEntries(TestSlot);

            Assert.AreEqual(1, loaded.Length);
            Assert.AreEqual("Goblet", loaded[0].Name);
            Assert.IsTrue(loaded[0].HasPose);
            Assert.AreEqual(new Vector3(1000.25f, 0.5f, -3.125f), loaded[0].Position);
            Assert.AreEqual(pose, loaded[0].Rotation);
        }

        [Test]
        public void Test_OldNamesOnlySaveLoadsWithNoPose()
        {
            PlayerPrefs.SetString(SaveSlots.Key("HaulPile", TestSlot), "Goblet|Crown");

            HaulPileEntry[] loaded = HaulPileSave.LoadEntries(TestSlot);

            Assert.AreEqual(2, loaded.Length);
            Assert.AreEqual("Crown", loaded[1].Name);
            Assert.IsFalse(loaded[0].HasPose);
            Assert.IsFalse(loaded[1].HasPose);
        }

        [Test]
        public void Test_EmptySaveLoadsNothing()
        {
            Assert.AreEqual(0, HaulPileSave.LoadEntries(TestSlot).Length);
            HaulPileSave.SaveEntries(TestSlot, new HaulPileEntry[0]);
            Assert.AreEqual(0, HaulPileSave.LoadEntries(TestSlot).Length);
        }

        [Test]
        public void Test_MixedEntriesKeepTheirOrderAndPoses()
        {
            HaulPileSave.SaveEntries(TestSlot, new[]
            {
                new HaulPileEntry("Goblet"),
                new HaulPileEntry("Crown", new Vector3(1f, 2f, 3f), Quaternion.identity),
                new HaulPileEntry("Ring"),
            });

            HaulPileEntry[] loaded = HaulPileSave.LoadEntries(TestSlot);

            Assert.AreEqual(3, loaded.Length);
            Assert.IsFalse(loaded[0].HasPose);
            Assert.IsTrue(loaded[1].HasPose);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), loaded[1].Position);
            Assert.AreEqual("Ring", loaded[2].Name);
            Assert.IsFalse(loaded[2].HasPose);
        }
    }
}
