using NUnit.Framework;
using Plunderspell.Inventory;
using Plunderspell.Lair;
using UnityEngine;

namespace Plunderspell.Tests.Editor
{
    /// <summary>
    /// Save slots keep separate campaigns. These tests write real PlayerPrefs, so each one saves the
    /// keys it touches first and puts them back afterwards.
    /// </summary>
    public class SaveSlotTests
    {
        private static readonly string[] s_keys =
        {
            "Save.ActiveSlot",
            "TotalDebt", "AccumulatedGold", "SelectedEra",
            "Slot2.TotalDebt", "Slot2.AccumulatedGold", "Slot2.SelectedEra",
            "Slot3.TotalDebt", "Slot3.AccumulatedGold", "Slot3.SelectedEra",
            "Slot2.Purse0", "Slot2.Purse3", "Slot3.Purse0", "Slot3.Purse3",
        };

        [Test]
        public void Test_HaulPileIsSavedPerSlotAndWipedWithIt()
        {
            string[] none = new string[0];
            HaulPileSave.Save(2, new[] { "Goblet", "Crown" });
            Assert.AreEqual(new[] { "Goblet", "Crown" }, HaulPileSave.Load(2));
            Assert.AreEqual(none, HaulPileSave.Load(3), "Another slot has its own pile.");

            LairHubManager.ResetSlot(2);
            Assert.AreEqual(none, HaulPileSave.Load(2), "A reset slot is a new campaign with an empty floor.");
            HaulPileSave.Clear(3);
        }

        private readonly string[] _saved = new string[s_keys.Length];
        private GameObject _host;

        [SetUp]
        public void SetUp()
        {
            for (int i = 0; i < s_keys.Length; i++)
            {
                string key = s_keys[i];
                _saved[i] = !PlayerPrefs.HasKey(key) ? null
                    : key.EndsWith("Era") || key.Contains("Purse") || key == "Save.ActiveSlot" ? "i" + PlayerPrefs.GetInt(key)
                    : "f" + PlayerPrefs.GetFloat(key).ToString("R");
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (_host != null)
                Object.DestroyImmediate(_host);
            for (int i = 0; i < s_keys.Length; i++)
            {
                string key = s_keys[i];
                string value = _saved[i];
                if (value == null)
                    PlayerPrefs.DeleteKey(key);
                else if (value[0] == 'i')
                    PlayerPrefs.SetInt(key, int.Parse(value.Substring(1)));
                else
                    PlayerPrefs.SetFloat(key, float.Parse(value.Substring(1)));
            }
            PlayerPrefs.Save();
        }

        [Test]
        public void Test_TestSlotIsKeptButNeverOneOfTheMenuSlots()
        {
            SaveSlots.Active = SaveSlots.TestSlot;
            Assert.AreEqual(SaveSlots.TestSlot, SaveSlots.Active);
            Assert.Greater(SaveSlots.TestSlot, SaveSlots.Count, "The menu steps through 1 to Count only.");
            SaveSlots.Active = 7;
            Assert.AreEqual(SaveSlots.Count, SaveSlots.Active, "Any other out-of-range slot still clamps.");
        }

        [Test]
        public void Test_SlotsKeepSeparateCampaigns()
        {
            LairHubManager.ResetSlot(2);
            LairHubManager.ResetSlot(3);
            _host = new GameObject("Lair");
            var lair = _host.AddComponent<LairHubManager>();

            lair.LoadSlot(2);
            lair.BankSale(120f);
            lair.SelectEra(HistoricalEra.LateMedieval);
            Assert.IsTrue(LairHubManager.HasSave(2));
            Assert.AreEqual(380f, LairHubManager.Peek(2).TotalDebt, 0.01f, "120 banked pays 120 of the 500 debt.");

            lair.LoadSlot(3);
            Assert.IsFalse(LairHubManager.HasSave(3), "Slot 3 was never played.");
            Assert.AreEqual(500f, lair.TotalDebt, 0.01f, "A new slot starts at the default debt.");
            Assert.AreEqual(HistoricalEra.BronzeAge, lair.GetLairState().SelectedEra);

            lair.LoadSlot(2);
            Assert.AreEqual(380f, lair.TotalDebt, 0.01f, "Switching back finds slot 2 as it was left.");
            Assert.AreEqual(HistoricalEra.LateMedieval, lair.GetLairState().SelectedEra);
            Assert.AreEqual(-1f, lair.LastRaidWorth, "The last-raid line belongs to the slot that was left.");

            LairHubManager.ResetSlot(2);
            Assert.IsFalse(LairHubManager.HasSave(2));
            Assert.AreEqual(500f, LairHubManager.Peek(2).TotalDebt, 0.01f, "A reset slot is a new campaign.");
        }

        [Test]
        public void Test_PursesAreSavedPerSlot()
        {
            LairHubManager.ResetSlot(2);
            LairHubManager.ResetSlot(3);
            _host = new GameObject("Lair");
            var lair = _host.AddComponent<LairHubManager>();

            lair.LoadSlot(2);
            lair.BankPouch(0, 40);
            lair.BankPouch(3, 25);
            lair.BankPouch(3, 5);
            Assert.AreEqual(40, LairHubManager.PeekPurse(2, 0));
            Assert.AreEqual(30, LairHubManager.PeekPurse(2, 3));
            Assert.AreEqual(0, LairHubManager.PeekPurse(3, 0), "Another slot has its own purses.");

            lair.LoadSlot(3);
            Assert.AreEqual(0, lair.Purse(3), "Loading another slot shows its purses.");
            lair.LoadSlot(2);
            Assert.AreEqual(40, lair.Purse(0));
            Assert.AreEqual(30, lair.Purse(3));

            LairHubManager.ResetSlot(2);
            Assert.AreEqual(0, LairHubManager.PeekPurse(2, 3), "A reset slot is a new campaign with empty purses.");
        }

        [Test]
        public void Test_SlotOneUsesThePreSlotKeys()
        {
            Assert.AreEqual("TotalDebt", SaveSlots.Key("TotalDebt", 1), "An existing save must load as slot 1.");
            Assert.AreEqual("Slot2.TotalDebt", SaveSlots.Key("TotalDebt", 2));
        }
    }
}
