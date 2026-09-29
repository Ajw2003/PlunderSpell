using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Interfaces;
using NUnit.Framework;
using Plunderspell.Alarm;
using Plunderspell.Audio;
using Plunderspell.Core;
using Plunderspell.EditorTools;
using Plunderspell.Guards;
using Plunderspell.Inventory;
using Plunderspell.Spells;
using Plunderspell.Voice;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Plunderspell.Tests.Editor
{
    public class AudioLayerTests
    {
        private SoundBank LoadBank()
        {
            var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(SoundBankBuilder.BankPath);
            Assert.IsNotNull(bank, "SoundBank.asset is missing; run Plunderspell > Audio > Rebuild SoundBank.");
            return bank;
        }

        [Test]
        public void EveryManifestNameHasAnEntryWithAClip()
        {
            SoundBank bank = LoadBank();
            List<SoundBankBuilder.ManifestRow> rows = SoundBankBuilder.ReadManifest(File.ReadAllText(SoundBankBuilder.ManifestPath));
            Assert.AreEqual(483, rows.Count, "The manifest is expected to list 483 sounds.");

            var problems = new List<string>();
            foreach (SoundBankBuilder.ManifestRow row in rows)
            {
                if (!bank.TryGet(row.Name, out SoundEntry entry))
                    problems.Add(row.Name + ": no entry");
                else if (entry.Clips.Length == 0)
                    problems.Add(row.Name + ": no clips");
                else if (entry.Group == null)
                    problems.Add(row.Name + ": no mixer group");
                else if (entry.Clips.Length != row.Variants)
                    problems.Add(row.Name + ": " + entry.Clips.Length + " clips, manifest says " + row.Variants);
            }

            Assert.IsEmpty(problems, string.Join("\n", problems));
            Assert.AreEqual(rows.Count, bank.Entries.Count);
        }

        [Test]
        public void EveryGroupInTheBankBelongsToTheMixer()
        {
            SoundBank bank = LoadBank();
            Assert.IsNotNull(bank.Mixer);
            foreach (SoundEntry entry in bank.Entries)
                Assert.AreSame(bank.Mixer, entry.Group.audioMixer, entry.Name);
        }

        [Test]
        public void DecibelMappingIsSilentAtZeroUnityAtOneAndSixDownAtHalf()
        {
            Assert.AreEqual(-80f, AudioLevels.LinearToDb(0f));
            Assert.AreEqual(0f, AudioLevels.LinearToDb(1f), 0.0001f);
            Assert.AreEqual(-6f, AudioLevels.LinearToDb(0.5f), 0.1f);
            Assert.AreEqual(-80f, AudioLevels.LinearToDb(-1f));
            Assert.AreEqual(0f, AudioLevels.LinearToDb(2f), 0.0001f);
        }

        [Test]
        public void MixerExposesTheVolumeParametersAndTheCastingSnapshot()
        {
            SoundBank bank = LoadBank();
            foreach (string parameter in new[]
                { AudioLevels.MasterParameter, AudioLevels.MusicParameter, AudioLevels.SfxParameter, AudioLevels.UiParameter })
                Assert.IsTrue(bank.Mixer.GetFloat(parameter, out _), parameter + " is not exposed");
            Assert.IsNotNull(bank.Mixer.FindSnapshot("Default"));
            Assert.IsNotNull(bank.Mixer.FindSnapshot("Casting"));
        }

        [Test]
        public void PoolReusesItsSourcesAndNeverGrows()
        {
            var root = new GameObject("PoolTest");
            try
            {
                var pool = new AudioSourcePool(root.transform, 8);
                var seen = new HashSet<AudioSource>();
                for (int i = 0; i < 200; i++)
                    seen.Add(pool.Acquire());

                Assert.AreEqual(8, pool.Size);
                Assert.AreEqual(8, seen.Count, "200 acquisitions used exactly the 8 pooled sources.");
                Assert.AreEqual(8, root.GetComponentsInChildren<AudioSource>().Length, "No source was created after the pool was made.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void UnknownNameLogsOnceAndDoesNotThrow()
        {
            var root = new GameObject("DirectorTest");
            try
            {
                var director = root.AddComponent<AudioDirector>();
                director.Initialize(LoadBank(), withMusic: false);

                LogAssert.Expect(LogType.Warning, new Regex("No sound named 'no_such_sound'"));
                Assert.IsNull(director.Play("no_such_sound", Vector3.zero));
                Assert.IsNull(director.Play("no_such_sound", Vector3.zero));
                Assert.IsNull(director.Play(null, Vector3.zero));
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EveryNameAnEventCanPlayIsInTheBank()
        {
            SoundBank bank = LoadBank();
            var names = new List<string>
            {
                SoundNames.DoorOpen, SoundNames.DoorClose, SoundNames.LootBreak, SoundNames.Fizzle, SoundNames.NoMana,
                SoundNames.MisfireSting, SoundNames.PlayerHurt, SoundNames.PlayerHurtHeavy, SoundNames.PlayerDeath,
                SoundNames.UiHover, SoundNames.UiClick, SoundNames.UiBack, SoundNames.ItemCrossed,
                SoundNames.ExtractSuccess, SoundNames.PlayerDownSting, SoundNames.PortalOpened, SoundNames.PortalWarning,
                "mus_title_loop", "mus_lair_loop", "mus_results_success_loop", "mus_results_failure_loop"
            };

            foreach (SpellId id in System.Enum.GetValues(typeof(SpellId)))
            {
                foreach (CastVolume volume in System.Enum.GetValues(typeof(CastVolume)))
                    names.Add(SoundNames.SpellCast(id, volume));
                names.Add(SoundNames.SpellMisfire(id));
            }

            foreach (DamageKind kind in System.Enum.GetValues(typeof(DamageKind)))
                names.Add(SoundNames.Hit(kind));

            foreach (HistoricalEra era in System.Enum.GetValues(typeof(HistoricalEra)))
            {
                for (int i = 0; i < 4; i++)
                    names.Add(SoundNames.RaidStem(era, i));
                foreach (AlarmState state in System.Enum.GetValues(typeof(AlarmState)))
                    names.Add(SoundNames.AlarmSting(state, era));
                foreach (GuardAttackKind kind in System.Enum.GetValues(typeof(GuardAttackKind)))
                    names.Add(SoundNames.GuardAttack(kind, era));
            }

            var missing = new List<string>();
            foreach (string name in names)
            {
                if (name != null && !bank.TryGet(name, out _))
                    missing.Add(name);
            }

            Assert.IsEmpty(missing, "Not in the SoundBank: " + string.Join(", ", missing));
        }

        [Test]
        public void MusicPlanFollowsTheGameState()
        {
            Assert.AreEqual("mus_title_loop", MusicDirector.Choose(GameState.MainMenu, false).Bed);
            Assert.AreEqual("mus_lair_loop", MusicDirector.Choose(GameState.Lair, false).Bed);
            Assert.AreEqual("mus_results_failure_loop", MusicDirector.Choose(GameState.GameOver, false).Bed);
            Assert.AreEqual("mus_results_success_loop", MusicDirector.Choose(GameState.Victory, false).Bed);
            Assert.IsTrue(MusicDirector.Choose(GameState.Playing, true).RaidStems);
            Assert.IsTrue(MusicDirector.Choose(GameState.Playing, false).Hold);
            Assert.IsTrue(MusicDirector.Choose(GameState.Paused, true).Hold);
            Assert.IsTrue(MusicDirector.Choose(GameState.Settings, false).Hold);
            Assert.AreEqual(3, MusicDirector.StemIndex(AlarmState.HueAndCry));
            Assert.AreEqual(0, MusicDirector.StemIndex(AlarmState.Calm));
        }
    }
}
