using System.Reflection;
using NUnit.Framework;
using Plunderspell.Core;
using UnityEngine;

namespace Plunderspell.Tests.Editor
{
    /// <summary>
    /// The shipped Ophan demand tuning (#433): it loads, holds the agreed defaults, and every number sits
    /// inside its Inspector range. If the owner retunes the asset, update the defaults here with it.
    /// </summary>
    public class OphanDemandTuningTests
    {
        private static OphanDemandTuning LoadShipped()
        {
            var tuning = Resources.Load<OphanDemandTuning>(OphanDemandTuning.ResourcePath);
            Assert.IsNotNull(tuning, $"Resources/{OphanDemandTuning.ResourcePath}.asset is missing.");
            return tuning;
        }

        [Test]
        public void Test_TheShippedAssetHoldsTheAgreedDefaults()
        {
            OphanDemandTuning t = LoadShipped();
            Assert.AreEqual(300f, t.RaidSeconds, "Raids last 5 minutes (#433).");
            Assert.AreEqual(60f, t.HuntWindowSeconds);
            Assert.AreEqual(1, t.FirstDemandDueAfterRaids);
            Assert.AreEqual(3, t.RaidsBetweenDemandsMin);
            Assert.AreEqual(5, t.RaidsBetweenDemandsMax);
            Assert.IsTrue(t.HuntEveryRaidWhileOwed);
            Assert.AreEqual(1f, t.SpoilsTakenWhileOwed);
            Assert.AreEqual(0, t.ExtraPiecesPerMissedDemand);
            Assert.AreEqual(3, t.MaxPiecesPerDemand);
            Assert.AreEqual(0f, t.UnscheduledVisitChance);
            Assert.AreEqual(0f, t.HuntSecondsAfterPaid);
            Assert.AreEqual(6f, t.PieceMarkRadius);
            Assert.AreEqual(3, t.PieceMarkLastsForDemands);
            Assert.AreEqual(6, t.FirstPieceMinRoomsFromPortal);
        }

        [Test]
        public void Test_EveryNumberHasARangeAndSitsInsideIt()
        {
            OphanDemandTuning t = LoadShipped();
            int checkedFields = 0;
            foreach (FieldInfo field in typeof(OphanDemandTuning).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.FieldType == typeof(bool))
                    continue; // a toggle has no range
                var range = field.GetCustomAttribute<UnityEngine.RangeAttribute>();
                Assert.IsNotNull(range, $"{field.Name} has no [Range]: the Inspector would accept any number.");
                float value = System.Convert.ToSingle(field.GetValue(t));
                Assert.That(value, Is.InRange(range.min, range.max), $"{field.Name} = {value} is outside {range.min}..{range.max}.");
                checkedFields++;
            }
            Assert.AreEqual(13, checkedFields, "Every number in #433's table, the bool aside.");
        }

        [Test]
        public void Test_TheNumbersAgreeWithEachOther()
        {
            OphanDemandTuning t = LoadShipped();
            Assert.LessOrEqual(t.RaidsBetweenDemandsMin, t.RaidsBetweenDemandsMax);
            Assert.Less(t.HuntWindowSeconds, t.RaidSeconds, "The hunt window is the end of a raid, not all of it.");
        }
    }
}
