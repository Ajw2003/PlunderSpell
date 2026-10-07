using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.Audio;
using Plunderspell.Audio.VoiceBank;
using UnityEngine;

namespace Plunderspell.Tests.Editor
{
    /// <summary>
    /// Recorded guard speech: the clip bank and its fallback order, the per-archetype voice, the cached
    /// re-voicing, and the sound filter settings that switch the old guard sounds off.
    /// </summary>
    public class GuardSpeechTests
    {
        private const int SampleRate = 16000;

        private readonly List<AudioClip> _made = new List<AudioClip>();

        [TearDown]
        public void TearDown()
        {
            foreach (AudioClip clip in _made)
                Object.DestroyImmediate(clip);
            _made.Clear();
            SoundFocus.ClearOverride();
        }

        private AudioClip Clip(string name, float seconds = 0.5f)
        {
            int length = (int)(seconds * SampleRate);
            var samples = new float[length];
            for (int i = 0; i < length; i++)
                samples[i] = 0.4f * Mathf.Sin(2f * Mathf.PI * 180f * i / SampleRate);
            AudioClip clip = AudioClip.Create(name, length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            _made.Add(clip);
            return clip;
        }

        // --- Bank ---------------------------------------------------------------------------------------

        [Test]
        public void Test_ABankPicksOnlyClipsOfThatAgeAndLine()
        {
            var bank = new GuardSpeechBank(new[]
            {
                Clip("vo_high_base_chase_01"), Clip("vo_high_base_chase_02"),
                Clip("vo_high_base_hurt_01"), Clip("vo_late_base_chase_01")
            });
            var rng = new System.Random(1);
            for (int i = 0; i < 20; i++)
            {
                Assert.IsTrue(bank.TryPick("high", GuardLine.Chase, rng, out AudioClip clip));
                StringAssert.StartsWith("vo_high_base_chase_", clip.name);
            }
            Assert.IsTrue(bank.TryPick("high", GuardLine.Hurt, rng, out AudioClip hurt));
            Assert.AreEqual("vo_high_base_hurt_01", hurt.name);
            Assert.AreEqual(4, bank.ClipCount);
        }

        [Test]
        public void Test_ABankIgnoresClipsWithOtherNames()
        {
            var bank = new GuardSpeechBank(new[] { Clip("vo_high_levy_chase"), Clip("ambience"), Clip("vo_high_base_chase_01") });
            Assert.AreEqual(1, bank.ClipCount);
        }

        [Test]
        public void Test_AnAgeWithoutTheLineFallsBackPowderHighLateBronze()
        {
            var rng = new System.Random(2);
            var bronzeOnly = new GuardSpeechBank(new[] { Clip("vo_bronze_base_lost_01") });
            Assert.IsTrue(bronzeOnly.TryPick("high", GuardLine.Lost, rng, out AudioClip clip));
            Assert.AreEqual("vo_bronze_base_lost_01", clip.name);

            var lateAndBronze = new GuardSpeechBank(new[] { Clip("vo_bronze_base_lost_01"), Clip("vo_late_base_lost_01") });
            Assert.IsTrue(lateAndBronze.TryPick("high", GuardLine.Lost, rng, out clip));
            Assert.AreEqual("vo_late_base_lost_01", clip.name);

            var powderAndLate = new GuardSpeechBank(new[] { Clip("vo_late_base_lost_01"), Clip("vo_powder_base_lost_01") });
            Assert.IsTrue(powderAndLate.TryPick("bronze", GuardLine.Lost, rng, out clip));
            Assert.AreEqual("vo_powder_base_lost_01", clip.name);

            var highAndLate = new GuardSpeechBank(new[] { Clip("vo_late_base_lost_01"), Clip("vo_high_base_lost_01") });
            Assert.IsTrue(highAndLate.TryPick("bronze", GuardLine.Lost, rng, out clip));
            Assert.AreEqual("vo_high_base_lost_01", clip.name);
        }

        [Test]
        public void Test_AnEmptyBankPicksNothing()
        {
            var bank = new GuardSpeechBank(new AudioClip[0]);
            Assert.IsFalse(bank.TryPick("high", GuardLine.Chase, new System.Random(3), out AudioClip clip));
            Assert.IsNull(clip);
        }

        [Test]
        public void Test_EveryLineMapsToItsSituationName()
        {
            Assert.AreEqual("murmur", GuardSpeechBank.Situation(GuardLine.Murmur));
            Assert.AreEqual("alert", GuardSpeechBank.Situation(GuardLine.Alert));
            Assert.AreEqual("chase", GuardSpeechBank.Situation(GuardLine.Chase));
            Assert.AreEqual("search", GuardSpeechBank.Situation(GuardLine.Search));
            Assert.AreEqual("lost", GuardSpeechBank.Situation(GuardLine.Lost));
            Assert.AreEqual("attack", GuardSpeechBank.Situation(GuardLine.Attack));
            Assert.AreEqual("hurt", GuardSpeechBank.Situation(GuardLine.Hurt));
            Assert.AreEqual("death", GuardSpeechBank.Situation(GuardLine.Death));
            Assert.AreEqual("asleep", GuardSpeechBank.Situation(GuardLine.Asleep));
        }

        // --- Voice per guard ----------------------------------------------------------------------------

        [Test]
        public void Test_ArchetypePitchTable()
        {
            var expected = new Dictionary<string, float>
            {
                { "levy", 130f }, { "slinger", 150f }, { "champion", 95f }, { "keeper", 115f }, { "warden", 120f },
                { "crossbowman", 140f }, { "knight", 100f }, { "halberdier", 125f }, { "handgunner", 145f },
                { "manatarms", 98f }, { "pavisier", 135f }, { "guard", 128f }, { "musketeer", 150f },
                { "cuirassier", 92f }, { "petardier", 140f }, { "nobody", 120f }
            };
            foreach (KeyValuePair<string, float> pair in expected)
                Assert.AreEqual(pair.Value, GuardVoiceProfiles.ArchetypeHz(pair.Key), pair.Key);
        }

        [Test]
        public void Test_ASeedAlwaysGivesTheSameVoice()
        {
            DisguiseProfile a = GuardVoiceProfiles.For(1234, "levy");
            DisguiseProfile b = GuardVoiceProfiles.For(1234, "levy");
            Assert.AreEqual(a.PitchRatio, b.PitchRatio);
            Assert.AreEqual(a.Speed, b.Speed);
            Assert.AreEqual(a.BrightnessHz, b.BrightnessHz);
            Assert.AreEqual(a.Roughness, b.Roughness);
        }

        [Test]
        public void Test_TwoGuardsOfOneArchetypeMostlySoundDifferent()
        {
            int different = 0;
            for (int seed = 1; seed <= 20; seed++)
            {
                if (GuardVoiceProfiles.For(seed, "levy").PitchRatio != GuardVoiceProfiles.For(seed + 100, "levy").PitchRatio)
                    different++;
            }
            Assert.GreaterOrEqual(different, 18);
        }

        [Test]
        public void Test_ANetworkIdIsTheSeedWhateverTheNameOrPlace()
        {
            int onHost = GuardVoiceProfiles.SeedFor(42UL, "PalaceLevy", Vector3.zero);
            int onClient = GuardVoiceProfiles.SeedFor(42UL, "PalaceLevy(Clone)", new Vector3(5f, 0f, 5f));
            Assert.AreEqual(onHost, onClient);
            Assert.AreNotEqual(onHost, GuardVoiceProfiles.SeedFor(43UL, "PalaceLevy", Vector3.zero));
        }

        [Test]
        public void Test_WithoutANetworkIdTheSeedFollowsNameAndPlace()
        {
            var spot = new Vector3(3f, 0f, 8f);
            Assert.AreEqual(GuardVoiceProfiles.SeedFor(0UL, "PalaceLevy", spot), GuardVoiceProfiles.SeedFor(0UL, "PalaceLevy", spot));
            Assert.AreNotEqual(GuardVoiceProfiles.SeedFor(0UL, "PalaceLevy", spot), GuardVoiceProfiles.SeedFor(0UL, "PalaceLevy", spot + Vector3.right * 4f));
        }

        // --- Renderer -----------------------------------------------------------------------------------

        [Test]
        public void Test_ARenderedClipKeepsItsLengthUpToTheSpeedAndIsNormalised()
        {
            var renderer = new GuardSpeechRenderer();
            AudioClip source = Clip("vo_high_base_chase_01", 1f);
            DisguiseProfile profile = GuardVoiceProfiles.For(7, "knight");
            AudioClip rendered = renderer.Get(source, profile, "high/knight/7");
            _made.Add(rendered);

            float expected = source.length / profile.Speed;
            Assert.AreEqual(expected, rendered.length, expected * 0.05f);
            Assert.AreEqual(1, rendered.channels);

            var data = new float[rendered.samples];
            rendered.GetData(data, 0);
            float peak = 0f;
            foreach (float sample in data)
                peak = Mathf.Max(peak, Mathf.Abs(sample));
            Assert.AreEqual(0.8f, peak, 0.01f);
        }

        [Test]
        public void Test_ASecondRequestReturnsTheCachedClip()
        {
            var renderer = new GuardSpeechRenderer();
            AudioClip source = Clip("vo_high_base_chase_01");
            DisguiseProfile profile = GuardVoiceProfiles.For(7, "knight");
            AudioClip first = renderer.Get(source, profile, "high/knight/7");
            _made.Add(first);
            Assert.AreSame(first, renderer.Get(source, profile, "high/knight/7"));
            Assert.AreEqual(1, renderer.CachedCount);
        }

        [Test]
        public void Test_TheCacheEvictsTheOldestBeyondItsCap()
        {
            var renderer = new GuardSpeechRenderer();
            AudioClip source = Clip("vo_high_base_chase_01", 0.1f);
            DisguiseProfile profile = GuardVoiceProfiles.For(7, "knight");
            AudioClip oldest = renderer.Get(source, profile, "guard/0");
            for (int i = 1; i <= GuardSpeechRenderer.CacheSize; i++)
                _made.Add(renderer.Get(source, profile, "guard/" + i));

            Assert.AreEqual(GuardSpeechRenderer.CacheSize, renderer.CachedCount);
            Assert.IsTrue(oldest == null, "The evicted clip is destroyed.");
        }

        [Test]
        public void Test_TryGetRendersInTheBackgroundAndPumpDeliversTheClip()
        {
            var renderer = new GuardSpeechRenderer();
            AudioClip source = Clip("vo_high_base_chase_01", 1f);
            DisguiseProfile profile = GuardVoiceProfiles.For(7, "knight");

            Assert.IsFalse(renderer.TryGet(source, profile, "high/knight/7", out AudioClip early), "The first ask must not block for a render.");
            Assert.IsNull(early);
            Assert.IsFalse(renderer.TryGet(source, profile, "high/knight/7", out _), "Asking again while it renders must not start a second render.");
            Assert.AreEqual(1, renderer.RenderingCount);

            AudioClip ready = null;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            while (timer.Elapsed.TotalSeconds < 5 && ready == null)
            {
                System.Threading.Thread.Sleep(10);
                renderer.Pump();
                renderer.TryGet(source, profile, "high/knight/7", out ready);
            }

            Assert.IsNotNull(ready, "The background render never finished.");
            _made.Add(ready);
            Assert.AreEqual(0, renderer.RenderingCount);
            Assert.AreEqual(source.length / profile.Speed, ready.length, source.length / profile.Speed * 0.05f);
            Assert.AreSame(ready, renderer.Get(source, profile, "high/knight/7"), "The blocking Get must see the same cached clip.");
        }

        // --- Sound filter -------------------------------------------------------------------------------

        [Test]
        public void Test_TheDefaultGroupsSwitchTheOldGuardSoundsOffAndSpeechOn()
        {
            var settings = ScriptableObject.CreateInstance<SoundFocusSettings>();
            try
            {
                AssertGuardSoundRules(settings.Allows);
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void Test_TheShippedAssetSwitchesTheOldGuardSoundsOffAndSpeechOn()
        {
            SoundFocus.Enabled = true;
            AssertGuardSoundRules(SoundFocus.Allows);
        }

        private static void AssertGuardSoundRules(System.Func<string, bool> allows)
        {
            Assert.IsFalse(allows("vo_hound_growl"));
            Assert.IsFalse(allows("sfx_enemy_anything"));
            Assert.IsFalse(allows("mimic_word"));
            Assert.IsTrue(allows("guardspeech_chase"));
            Assert.IsFalse(allows("guard_foley"));
            Assert.IsTrue(allows("foley_step_stone"));
        }
    }
}
