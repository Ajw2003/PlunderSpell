using System.Collections;
using System.Collections.Generic;
using Code.Scripts.EventSystems;
using NUnit.Framework;
using Plunderspell.Core;
using Plunderspell.Spells;
using Plunderspell.UI;
using Plunderspell.Voice;
using StateMachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Plunderspell.Tests
{
    /// <summary>
    /// The grimoire (#325): holding Tab opens it, the HUD model and the player's body learn that by event (reading slows
    /// the walk), it opens by itself once for a first raid, and its margin keeps the last casts. The key is pressed for real.
    /// </summary>
    public class GrimoireTests : InputTestFixture
    {
        private const string SeenKey = "Hud.GrimoireSeen";

        private Keyboard _keyboard;
        private GameState _stateBefore;
        private int _seenBefore;
        private readonly List<GameObject> _created = new List<GameObject>();

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _seenBefore = PlayerPrefs.GetInt(SeenKey, 0);
            PlayerPrefs.SetInt(SeenKey, 1); // not a first raid unless a test says so
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
            PlayerPrefs.SetInt(SeenKey, _seenBefore);
            base.TearDown();
        }

        private T Make<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            _created.Add(go);
            return go.AddComponent<T>();
        }

        [Test]
        public void Test_TurningThePageStaysInsideTheBook()
        {
            Assert.AreEqual(1, GrimoireView.Turn(0, 1));
            Assert.AreEqual(0, GrimoireView.Turn(0, -1), "The keys page is the first.");
            Assert.AreEqual(GrimoireView.PageCount - 1, GrimoireView.Turn(GrimoireView.PageCount - 1, 1), "The last spell is the last page.");
            Assert.AreEqual(GrimoireView.PageCount - 1, GrimoireView.Words.Length, "One page per spell, after the keys.");
        }

        [UnityTest]
        public IEnumerator Test_HoldingTabOpensTheBookAndLettingGoClosesIt()
        {
            var presenter = Make<RaidHudPresenter>("Hud");
            Make<HudHoldKeys>("Keys");
            var opened = new List<bool>();
            EventManager.Instance.Subscribe(this, (GrimoireOpened e) => opened.Add(e.Open));

            Press(_keyboard.tabKey);
            yield return null;
            Assert.IsTrue(presenter.Model.GrimoireOpen, "Holding Tab opens the book in the HUD model.");

            Release(_keyboard.tabKey);
            yield return null;
            Assert.IsFalse(presenter.Model.GrimoireOpen);
            CollectionAssert.AreEqual(new[] { true, false }, opened, "One event per change, not one per frame.");
        }

        [UnityTest]
        public IEnumerator Test_ReadingSlowsOnlyTheLocalPlayersBody()
        {
            Make<HudHoldKeys>("Keys");
            var local = Make<PlayerStateMachine>("Local");
            var other = Make<PlayerStateMachine>("Other");
            local.ClaimLocal();
            yield return null; // Start runs and subscribes

            Press(_keyboard.tabKey);
            yield return null;
            Assert.IsTrue(local.ReadingGrimoire, "The local body reads.");
            Assert.IsFalse(other.ReadingGrimoire, "A teammate's body on this machine does not slow because we read.");

            Release(_keyboard.tabKey);
            yield return null;
            Assert.IsFalse(local.ReadingGrimoire);
            local.ReleaseLocal();
            Assert.AreEqual(0.6f, PlayerStateMachine.ReadingPace, "Reading is 60% pace, as the owner approved.");
        }

        [UnityTest]
        public IEnumerator Test_TheFirstRaidOpensTheBookByItselfOnce()
        {
            PlayerPrefs.SetInt(SeenKey, 0);
            var presenter = Make<RaidHudPresenter>("Hud");
            Make<HudHoldKeys>("Keys");
            GameServices.GameState.ChangeState(GameState.LairRoom);
            GameServices.GameState.ChangeState(GameState.Playing);
            yield return null;
            Assert.IsTrue(presenter.Model.GrimoireOpen, "A first raid opens the book so the keys are learnt.");
            Assert.AreEqual(1, PlayerPrefs.GetInt(SeenKey), "…and it is only ever the first.");

            GameServices.GameState.ChangeState(GameState.LairRoom);
            yield return null;
            GameServices.GameState.ChangeState(GameState.Playing);
            yield return null;
            Assert.IsFalse(presenter.Model.GrimoireOpen, "A later raid starts with the book shut.");
        }

        [Test]
        public void Test_TheMarginKeepsTheLastThreeCastsNewestFirst()
        {
            var presenter = Make<RaidHudPresenter>("Hud");
            foreach (SpellId spell in new[] { SpellId.Ignis, SpellId.Frango, SpellId.Levo, SpellId.Somnus })
                SpellCastingSystem.AnnounceForTesting(new SpellCastingSystem.CastReport(spell, CastVolume.Normal, 1, "tester"));

            string[] recent = presenter.Model.RecentCasts;

            Assert.AreEqual(3, recent.Length, "Only the last three are kept.");
            StringAssert.Contains("Somnus", recent[0]);
            StringAssert.Contains("Levo", recent[1]);
            StringAssert.Contains("Frango", recent[2]);
        }
    }
}
