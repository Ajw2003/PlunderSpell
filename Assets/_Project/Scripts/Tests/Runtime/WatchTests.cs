using System.Collections;
using System.Collections.Generic;
using Code.Scripts.EventSystems;
using NUnit.Framework;
using Plunderspell.Core;
using Plunderspell.Extraction;
using Plunderspell.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Plunderspell.Tests
{
    /// <summary>
    /// The pocket watch (#324): holding T raises it, the HUD model learns that by event, and the face shows what share of
    /// the raid is left. The key is pressed for real, as in <see cref="CastingInputTests"/>.
    /// </summary>
    public class WatchTests : InputTestFixture
    {
        private Keyboard _keyboard;
        private GameState _stateBefore;
        private readonly List<GameObject> _created = new List<GameObject>();

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            GameServices.Initialize();
            _stateBefore = GameServices.GameState.CurrentState;
            GameServices.GameState.ChangeState(GameState.Playing);
        }

        public override void TearDown()
        {
            EventManager.Instance?.UnsubscribeFromAllEvents(this);
            foreach (GameObject go in _created)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }
            _created.Clear();
            GameServices.GameState.ChangeState(_stateBefore);
            base.TearDown();
        }

        private T Make<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            _created.Add(go);
            return go.AddComponent<T>();
        }

        [Test]
        public void Test_TheRingEmptiesAsTheRaidRunsDown()
        {
            Assert.AreEqual(60, WatchView.LitTicks(600f, 600f), "A full raid lights the whole ring.");
            Assert.AreEqual(30, WatchView.LitTicks(300f, 600f), "Half a raid left lights half.");
            Assert.AreEqual(1, WatchView.LitTicks(1f, 600f), "A started tick still shows until it is gone.");
            Assert.AreEqual(0, WatchView.LitTicks(0f, 600f), "Out of time lights nothing.");
            Assert.AreEqual(0, WatchView.LitTicks(100f, 0f), "With no raid length known there is nothing to show.");
        }

        [Test]
        public void Test_TheCaptionSaysMinutesThenUnderAMinute()
        {
            var view = Make<WatchView>("Watch");
            Assert.AreEqual("5 min", view.CaptionFor(300f));
            Assert.AreEqual("5 min", view.CaptionFor(241f), "A started minute counts.");
            Assert.AreEqual("under a minute", view.CaptionFor(59f));
            Assert.AreEqual("the way is shut", view.CaptionFor(0f));
        }

        [UnityTest]
        public IEnumerator Test_HoldingTRaisesTheWatchAndReleasingLowersIt()
        {
            var presenter = Make<RaidHudPresenter>("Hud");
            Make<HudHoldKeys>("Keys");
            var raised = new List<bool>();
            EventManager.Instance.Subscribe(this, (WatchRaised e) => raised.Add(e.Up));

            Press(_keyboard.tKey);
            yield return null;
            Assert.IsTrue(presenter.Model.WatchUp, "Holding T puts the watch up in the HUD model.");

            Release(_keyboard.tKey);
            yield return null;
            Assert.IsFalse(presenter.Model.WatchUp, "Letting go puts it away.");
            CollectionAssert.AreEqual(new[] { true, false }, raised, "One event per change, not one per frame.");
        }

        [UnityTest]
        public IEnumerator Test_TheWatchStaysDownOutsideARaidAndAMenuPutsItAway()
        {
            var presenter = Make<RaidHudPresenter>("Hud");
            Make<HudHoldKeys>("Keys");

            GameServices.GameState.ChangeState(GameState.Paused);
            Press(_keyboard.tKey);
            yield return null;
            Assert.IsFalse(presenter.Model.WatchUp, "A paused game does not raise the watch.");

            Release(_keyboard.tKey);
            GameServices.GameState.ChangeState(GameState.Playing);
            Press(_keyboard.tKey);
            yield return null;
            Assert.IsTrue(presenter.Model.WatchUp);

            GameServices.GameState.ChangeState(GameState.Paused);
            yield return null;
            Assert.IsFalse(presenter.Model.WatchUp, "Opening the menu with T held puts the watch away.");
        }

        [Test]
        public void Test_ThePresenterCarriesTheRaidLengthForTheFace()
        {
            var zoneGo = new GameObject("Zone");
            _created.Add(zoneGo);
            zoneGo.AddComponent<BoxCollider>().isTrigger = true;
            ExtractionZone zone = zoneGo.AddComponent<ExtractionZone>();
            var presenter = Make<RaidHudPresenter>("Hud");
            presenter.Configure(null, zone, null, null, null);

            RaidHudModel model = presenter.Build();

            Assert.AreEqual(zone.RaidLength, model.TimeTotal, "The face needs the whole raid to know what share is left.");
            Assert.Greater(model.TimeTotal, 0f);
        }
    }
}
