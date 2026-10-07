using System.Collections;
using System.Collections.Generic;
using Code.Scripts.EventSystems;
using NUnit.Framework;
using Plunderspell.Alarm;
using Plunderspell.Extraction;
using Plunderspell.Inventory;
using Plunderspell.Lair;
using StateMachine;
using UnityEngine;
using UnityEngine.TestTools;

namespace Plunderspell.Tests
{
    /// <summary>
    /// The change events added at their owners (#302): each is published when the value changes and not otherwise.
    /// </summary>
    public class ChangeEventTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            EventManager.Instance?.UnsubscribeFromAllEvents(this);
            foreach (GameObject go in _created)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }
            _created.Clear();
            TestDirector.Reset();
        }

        private T Make<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            _created.Add(go);
            return go.AddComponent<T>();
        }

        [Test]
        public void Test_TheLedgerPublishesDebtAndGoldOnlyWhenTheyChange()
        {
            LairHubManager lair = Make<LairHubManager>("Lair");
            var debts = new List<float>();
            var golds = new List<float>();
            EventManager.Instance.Subscribe(this, (DebtChanged e) => debts.Add(e.Debt));
            EventManager.Instance.Subscribe(this, (BankedGoldChanged e) => golds.Add(e.Gold));

            lair.ShowHostCampaign(100f, 40f, 0f);
            lair.ShowHostCampaign(100f, 40f, 0f);
            lair.ShowHostCampaign(100f, 55f, 0f);

            CollectionAssert.AreEqual(new[] { 100f }, debts, "The debt changed once; the repeat and the gold-only change are silent.");
            CollectionAssert.AreEqual(new[] { 40f, 55f }, golds);
        }

        [Test]
        public void Test_ChoosingAnAgePublishesItOnlyWhenItDiffers()
        {
            LairHubManager lair = Make<LairHubManager>("Lair");
            HistoricalEra original = lair.GetLairState().SelectedEra;
            HistoricalEra other = original == HistoricalEra.BronzeAge ? HistoricalEra.AgeOfPowder : HistoricalEra.BronzeAge;
            var chosen = new List<HistoricalEra>();
            EventManager.Instance.Subscribe(this, (AgeChosen e) => chosen.Add(e.Era));
            try
            {
                lair.SelectEra(original);
                lair.SelectEra(other);
            }
            finally
            {
                lair.SelectEra(original); // the choice is saved; put the player's own back
            }

            CollectionAssert.AreEqual(new[] { other, original }, chosen, "Re-choosing the same Age says nothing.");
        }

        [UnityTest]
        public IEnumerator Test_TheAlarmLevelIsPublishedWhenItMoves()
        {
            EnemyDirector director = TestDirector.Ensure();
            var levels = new List<float>();
            EventManager.Instance.Subscribe(this, (AlarmLevelChanged e) => levels.Add(e.Level));

            director.SetAlarmLevel(40f);
            yield return null;

            Assert.IsNotEmpty(levels, "A new level must be announced.");
            Assert.AreEqual(director.AlarmLevel, levels[levels.Count - 1], 0.001f);
        }

        [UnityTest]
        public IEnumerator Test_TheRaidClockIsPublishedAsItCountsDown()
        {
            var zoneGo = new GameObject("ExtractionZone");
            _created.Add(zoneGo);
            zoneGo.AddComponent<BoxCollider>().isTrigger = true;
            ExtractionZone zone = zoneGo.AddComponent<ExtractionZone>();
            var seen = new List<float>();
            EventManager.Instance.Subscribe(this, (ExtractionTimerChanged e) => seen.Add(e.SecondsRemaining));

            yield return null;
            yield return null;

            Assert.GreaterOrEqual(seen.Count, 2, "The clock changes every frame, so it is published every frame.");
            Assert.Less(seen[seen.Count - 1], seen[0], "It counts down.");
            Assert.AreEqual(zone.TimeRemaining, seen[seen.Count - 1], 0.05f);
        }

        [Test]
        public void Test_ClaimingAndReleasingTheLocalPlayerIsAnnounced()
        {
            PlayerStateMachine player = null;
            var seen = new List<PlayerStateMachine>();
            EventManager.Instance.Subscribe(this, (LocalPlayerChanged e) => seen.Add(e.Player));
            try
            {
                player = Make<PlayerStateMachine>("Player");
                player.ClaimLocal();
                player.ClaimLocal(); // already the local player: no second announcement
                player.ReleaseLocal();
            }
            finally
            {
                player?.ReleaseLocal();
            }

            CollectionAssert.AreEqual(new[] { player, null }, seen);
        }
    }
}
