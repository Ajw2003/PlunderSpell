using System.Collections.Generic;
using System.Reflection;
using Code.Scripts.EventSystems;
using Interfaces;
using NUnit.Framework;
using Plunderspell.Audio;
using Plunderspell.Core;
using Plunderspell.UI;
using StateMachine;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>
    /// Damage feedback, camera shake and music learn what they need from events (#304): the health a hit leaves
    /// travels in the damage event, the local player arrives as an event, and the music follows the game state.
    /// </summary>
    public class FeedbackEventTests
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
        }

        private T Make<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            _created.Add(go);
            return go.AddComponent<T>();
        }

        private static T Field<T>(object owner, string name) =>
            (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);

        [Test]
        public void Test_TheShakeFollowsTheLocalPlayerByEventAndStopsWhenItLeaves()
        {
            CameraShakeDirector shake = CameraShakeDirector.Instance;
            Assert.IsNotNull(shake, "The shake director creates itself.");
            PlayerStateMachine player = Make<PlayerStateMachine>("Player");
            var hit = new DamageReport(player, null, null, 20f, Vector3.zero, DamageKind.Impact, 80f, 100f);

            player.ClaimLocal();
            EventManager.Instance.Publish(new DamageDealt(hit));
            Assert.Greater(shake.Trauma, 0f, "A hit on the local player shakes the view.");

            player.ReleaseLocal();
            Assert.AreEqual(0f, shake.Trauma, "With no local player the shake is cleared…");
            EventManager.Instance.Publish(new DamageDealt(hit));
            Assert.AreEqual(0f, shake.Trauma, "…and a hit on nobody in particular adds nothing.");
        }

        [Test]
        public void Test_AHurtTargetsHealthBarComesFromTheEvent()
        {
            var view = Make<DamageFeedbackView>("Feedback");
            var target = Make<BoxCollider>("Guard");

            EventManager.Instance.Publish(new DamageDealt(new DamageReport(target, null, null, 70f, Vector3.zero, DamageKind.Melee, 30f, 100f)));

            var fractions = Field<Dictionary<Component, float>>(view, "_hurtFraction");
            Assert.AreEqual(0.3f, fractions[target], 0.001f, "The bar is as full as the event said the health was left.");
        }

        [Test]
        public void Test_TheLowHealthVignetteFollowsThePlayerStatsEvent()
        {
            var view = Make<DamageFeedbackView>("Feedback");
            GameServices.Initialize();
            PlayerStats stats = GameServices.PlayerStats;
            int original = stats.Health;
            try
            {
                stats.SetHealth(15); // publishes PlayerStatsChanged
                Assert.AreEqual(0.15f, Field<float>(view, "_youFraction"), 0.001f);
            }
            finally
            {
                stats.SetHealth(original);
            }
        }

        [Test]
        public void Test_TheMusicChoosesItsBedWhenTheGameStateChanges()
        {
            AudioDirector audio = AudioDirector.Instance;
            MusicDirector music = audio != null ? audio.Music : null;
            if (music == null)
                Assert.Ignore("No audio director with scene layers in this run, so there is no music to follow.");

            GameState before = GameServices.GameState.CurrentState;
            try
            {
                EventManager.Instance.Publish(new GameStateChanged(GameState.MainMenu, GameState.Lair));
                Assert.AreEqual("mus_lair_loop", music.CurrentBed, "Entering the Lair starts the Lair bed.");

                EventManager.Instance.Publish(new GameStateChanged(GameState.Lair, GameState.GameOver));
                Assert.AreEqual("mus_results_failure_loop", music.CurrentBed, "A lost raid starts the failure bed.");
            }
            finally
            {
                EventManager.Instance.Publish(new GameStateChanged(GameState.GameOver, before));
            }
        }

        [Test]
        public void Test_AllThreeLeaveTheBusWhenDisabled()
        {
            var shake = Make<CameraShakeDirector>("Shake");
            var view = Make<DamageFeedbackView>("Feedback");
            var backdrop = Make<BackdropCamera>("Backdrop");

            shake.enabled = false;
            view.enabled = false;
            backdrop.enabled = false;

            Assert.AreEqual(0, EventManager.Instance.SubscriptionCount(shake));
            Assert.AreEqual(0, EventManager.Instance.SubscriptionCount(view));
            Assert.AreEqual(0, EventManager.Instance.SubscriptionCount(backdrop));
        }
    }
}
