using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Plunderspell.Acoustics;
using Plunderspell.Alarm;
using Plunderspell.Core;
using Plunderspell.Guards;
using Plunderspell.Status;
using Plunderspell.UI;
using Plunderspell.Voice;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>
    /// Guards hearing what players say between casts: the setting, the noise, who understood, the caption.
    /// Plan: docs/plans/guards-hear-chatter.md.
    /// </summary>
    public class ChatterTests
    {
        private const int WallLayer = 8;

        private readonly List<UnityEngine.Object> _spawned = new List<UnityEngine.Object>();
        private bool _settingBefore;

        [SetUp]
        public void SetUp()
        {
            CastleGuard.EndArrivalGrace();
            _settingBefore = AudioInputSettings.GuardsHearChatter;
        }

        [TearDown]
        public void TearDown()
        {
            AudioInputSettings.GuardsHearChatter = _settingBefore;
            VoiceServiceLocator.Clear();
            TestDirector.Reset();
            foreach (UnityEngine.Object o in _spawned)
                if (o != null)
                    UnityEngine.Object.DestroyImmediate(o);
            _spawned.Clear();
        }

        private T Track<T>(T o) where T : UnityEngine.Object
        {
            _spawned.Add(o);
            return o;
        }

        private CastleGuard MakeGuard(Vector3 position, EnemyDirector alarm = null)
        {
            var go = Track(new GameObject("Guard"));
            go.transform.position = position;
            go.AddComponent<BoxCollider>();
            var guard = go.AddComponent<CastleGuard>();
            guard.Configure(alarm);
            return guard;
        }

        private PlayerChatterRelay MakeRelay()
        {
            var go = Track(new GameObject("Speaker"));
            go.transform.position = Vector3.zero;
            return go.AddComponent<PlayerChatterRelay>();
        }

        private static void StartRelay(PlayerChatterRelay relay) =>
            typeof(PlayerChatterRelay).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(relay, null);

        private sealed class FakeChatterService : IVoiceInputService, IChatterSource
        {
            public bool IsListening => false;
            public bool ChatterEnabled { get; set; }
            public event Action<VoiceRecognitionResult> OnPhraseRecognized { add { } remove { } }
            public event Action<ChatterReport> ChatterHeard;
            public void StartListening() { }
            public void StopListening() { }
            public void Say(string words, CastVolume volume) =>
                ChatterHeard?.Invoke(new ChatterReport(words, 0.2f, volume));
        }

        [Test]
        public void Test_ChatterIsOffByDefault()
        {
            PlayerPrefs.DeleteKey(AudioInputSettings.GuardsHearChatterKey);
            Assert.IsFalse(AudioInputSettings.GuardsHearChatter);
        }

        [Test]
        public void Test_TurningTheSettingOffStopsTheRelaySendingAnything()
        {
            AudioInputSettings.GuardsHearChatter = false;
            var fake = new FakeChatterService();
            VoiceServiceLocator.Register(fake);
            CastleGuard guard = MakeGuard(new Vector3(0f, 0f, 3f));
            PlayerChatterRelay relay = MakeRelay();
            StartRelay(relay);
            Physics.SyncTransforms();

            int resolved = 0;
            Action<ChatterOutcome> count = _ => resolved++;
            PlayerChatterRelay.ChatterResolved += count;
            try
            {
                Assert.IsFalse(fake.ChatterEnabled, "The source must not listen while the setting is off.");
                fake.Say("go left", CastVolume.Normal);
            }
            finally
            {
                PlayerChatterRelay.ChatterResolved -= count;
            }

            Assert.AreEqual(0, resolved, "Nothing may be resolved while the setting is off.");
            Assert.IsNull(guard.LastOverheard);
        }

        [Test]
        public void Test_SwitchingTheSettingOnStartsTheSourceAndSpeechIsResolved()
        {
            AudioInputSettings.GuardsHearChatter = false;
            var fake = new FakeChatterService();
            VoiceServiceLocator.Register(fake);
            MakeGuard(new Vector3(0f, 0f, 3f));
            PlayerChatterRelay relay = MakeRelay();
            StartRelay(relay);
            Physics.SyncTransforms();

            AudioInputSettings.GuardsHearChatter = true;
            Assert.IsTrue(fake.ChatterEnabled);

            ChatterOutcome? outcome = null;
            Action<ChatterOutcome> take = o => outcome = o;
            PlayerChatterRelay.ChatterResolved += take;
            try
            {
                fake.Say("go left", CastVolume.Normal);
            }
            finally
            {
                PlayerChatterRelay.ChatterResolved -= take;
            }

            Assert.IsTrue(outcome.HasValue, "Offline, a heard line resolves at once.");
            Assert.AreEqual(1, outcome.Value.GuardsWhoUnderstood);
            Assert.AreEqual("go left", outcome.Value.Transcript);
        }

        [Test]
        public void Test_NormalSpeechDrawsANearbyGuard()
        {
            CastleGuard guard = MakeGuard(new Vector3(0f, 0f, 4f));
            PlayerChatterRelay relay = MakeRelay();
            Physics.SyncTransforms();

            int understood = relay.Resolve("go left", CastVolume.Normal, Vector3.zero);

            Assert.AreEqual(1, understood);
            Assert.AreEqual(GuardAlertState.Investigating, guard.State);
            Assert.AreEqual("go left", guard.LastOverheard);
        }

        [Test]
        public void Test_AWhisperIsNotHeardAcrossTheRoom()
        {
            CastleGuard guard = MakeGuard(new Vector3(0f, 0f, 5f));
            PlayerChatterRelay relay = MakeRelay();
            Physics.SyncTransforms();

            int understood = relay.Resolve("be quiet", CastVolume.Whisper, Vector3.zero);

            Assert.AreEqual(0, understood);
            Assert.AreEqual(GuardAlertState.Patrolling, guard.State);
        }

        [Test]
        public void Test_AShoutCarriesThroughAWall()
        {
            CastleGuard guard = MakeGuard(new Vector3(0f, 0f, 10f));
            var wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            wall.layer = WallLayer;
            wall.transform.position = new Vector3(0f, 0f, 5f);
            wall.transform.localScale = new Vector3(6f, 4f, 0.3f);
            Physics.SyncTransforms();

            int understood = NoiseBroadcaster.BroadcastSpeech(Vector3.zero,
                PlayerChatterRelay.RadiusFor(CastVolume.Shout), PlayerChatterRelay.StrengthFor(CastVolume.Shout),
                "run", 1 << 0, 1 << WallLayer);

            Assert.AreEqual(1, understood, "One wall halves a shout; it is still audible.");
            Assert.AreEqual("run", guard.LastOverheard);
        }

        [Test]
        public void Test_ASleepingGuardDoesNotTakeInWords()
        {
            CastleGuard guard = MakeGuard(new Vector3(0f, 0f, 1f));
            guard.GetComponent<StatusEffectReceiver>().Sleep(30f);
            PlayerChatterRelay relay = MakeRelay();
            Physics.SyncTransforms();

            // A whisper is too quiet to wake the guard (wake threshold 0.5), so it stays asleep.
            int understood = relay.Resolve("psst", CastVolume.Whisper, Vector3.zero);

            Assert.IsTrue(guard.IsIncapacitated);
            Assert.AreEqual(0, understood);
            Assert.IsNull(guard.LastOverheard);
        }

        [Test]
        public void Test_TheAlarmHearsSpeechButIsNotCountedAsAGuard()
        {
            var alarmGo = Track(new GameObject("Alarm"));
            alarmGo.transform.position = new Vector3(0f, 0f, 2f);
            alarmGo.AddComponent<BoxCollider>();
            var alarm = alarmGo.AddComponent<EnemyDirector>();
            PlayerChatterRelay relay = MakeRelay();
            Physics.SyncTransforms();
            float before = alarm.AlarmLevel;

            int understood = relay.Resolve("hello", CastVolume.Shout, Vector3.zero);

            Assert.AreEqual(0, understood);
            Assert.Greater(alarm.AlarmLevel, before, "The alarm still hears the noise.");
        }

        [Test]
        public void Test_BackgroundNoiseIsNotReported()
        {
            Assert.IsFalse(ChatterFilter.IsWorthReporting("the", 0.01f));
            Assert.IsFalse(ChatterFilter.IsWorthReporting("  ", 0.3f));
            Assert.IsTrue(ChatterFilter.IsWorthReporting("go left", 0.2f));
        }

        [Test]
        public void Test_ChatterCaptionReadsRight()
        {
            string two = RaidHudView.ChatterCaptionFor(new ChatterOutcome("Go Left", CastVolume.Normal, 2), out Color amber);
            string one = RaidHudView.ChatterCaptionFor(new ChatterOutcome("go left", CastVolume.Normal, 1), out _);
            string none = RaidHudView.ChatterCaptionFor(new ChatterOutcome("go left", CastVolume.Normal, 0), out Color grey);

            StringAssert.Contains("\"go left\"", two);
            StringAssert.Contains("overheard by 2 guards", two);
            StringAssert.Contains("overheard by a guard", one);
            StringAssert.Contains("nobody heard", none);
            Assert.AreNotEqual(amber, grey);
        }

        [Test]
        public void Test_BroadcastIsUnchangedForOrdinaryNoise()
        {
            CastleGuard near = MakeGuard(new Vector3(0f, 0f, 2f));
            MakeGuard(new Vector3(0f, 0f, 30f));
            Physics.SyncTransforms();

            int heard = NoiseBroadcaster.Broadcast(Vector3.zero, 5f, 0.6f, NoiseType.GlassBreak);

            Assert.AreEqual(1, heard);
            Assert.IsNull(near.LastOverheard, "Ordinary noise carries no words.");
        }
    }
}
