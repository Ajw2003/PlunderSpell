using System.Collections.Generic;
using System.Reflection;
using Code.Scripts.EventSystems;
using NUnit.Framework;
using Plunderspell.Alarm;
using Plunderspell.Extraction;
using Plunderspell.Lair;
using Plunderspell.UI;
using Plunderspell.UI.Screens;
using UnityEngine;
using UnityEngine.UI;

namespace Plunderspell.Tests
{
    /// <summary>
    /// The raid HUD's model is built from events (#303): the presenter updates on each, polls nothing, and leaves
    /// the bus when disabled; the Settings screen's mic meter follows the level while it is open and only then.
    /// </summary>
    public class RaidHudEventTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _created)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }
            _created.Clear();
        }

        private T Make<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            _created.Add(go);
            return go.AddComponent<T>();
        }

        [Test]
        public void Test_ThePresenterUpdatesItsModelFromEventsAlone()
        {
            RaidHudPresenter presenter = Make<RaidHudPresenter>("Hud");

            EventManager.Instance.Publish(new DebtChanged(250f));
            EventManager.Instance.Publish(new BankedGoldChanged(75f));
            EventManager.Instance.Publish(new ExtractionTimerChanged(61f));
            EventManager.Instance.Publish(new AlarmLevelChanged(40f));
            EventManager.Instance.Publish(new HaulInZoneChanged(120f, 3));

            RaidHudModel model = presenter.Model;
            Assert.AreEqual(250f, model.Debt);
            Assert.AreEqual(75f, model.BankedGold);
            Assert.AreEqual(61f, model.TimeRemaining);
            Assert.AreEqual(40f, model.AlarmLevel);
            Assert.AreEqual(120f, model.HaulWorth);
            Assert.AreEqual(3, model.HaulPieces);
        }

        [Test]
        public void Test_ThePresenterHasNoPerFrameUpdate()
        {
            MethodInfo update = typeof(RaidHudPresenter).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNull(update, "The presenter must not rebuild its model every frame.");
        }

        [Test]
        public void Test_ThePresenterLeavesTheBusWhenDisabled()
        {
            RaidHudPresenter presenter = Make<RaidHudPresenter>("Hud");
            Assert.Greater(EventManager.Instance.SubscriptionCount(presenter), 0, "An enabled presenter is listening.");

            presenter.enabled = false;

            Assert.AreEqual(0, EventManager.Instance.SubscriptionCount(presenter), "A disabled presenter must leave nothing behind.");

            EventManager.Instance.Publish(new DebtChanged(999f));
            Assert.AreNotEqual(999f, presenter.Model.Debt, "…and must not hear anything.");
        }

        [Test]
        public void Test_TheSettingsMicMeterFollowsTheLevelOnlyWhileTheScreenIsOpen()
        {
            SettingsScreen screen = Make<SettingsScreen>("Settings");
            screen.Build();
            FieldInfo fillField = typeof(SettingsScreen).GetField("_micLevelFill", BindingFlags.Instance | BindingFlags.NonPublic);
            var fill = (Image)fillField.GetValue(screen);

            screen.SetVisible(true);
            EventManager.Instance.Publish(new Plunderspell.Voice.MicLevelChanged(0.3f));
            Assert.AreEqual(0.5f, fill.rectTransform.anchorMax.x, 0.001f, "0.3 of a 0.6 scale is half the bar.");

            screen.SetVisible(false);
            Assert.AreEqual(0, EventManager.Instance.SubscriptionCount(screen), "A closed screen leaves the bus.");
            EventManager.Instance.Publish(new Plunderspell.Voice.MicLevelChanged(0.6f));
            Assert.AreEqual(0f, fill.rectTransform.anchorMax.x, 0.001f, "…and the meter is back to empty.");
        }
    }
}
