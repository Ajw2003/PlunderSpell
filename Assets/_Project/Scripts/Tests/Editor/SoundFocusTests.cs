using NUnit.Framework;
using Plunderspell.Audio;
using UnityEngine;

namespace Plunderspell.Tests.Editor
{
    /// <summary>
    /// The playtest sound filter: its rules on a settings object built here, and the shipped asset
    /// loading. What the shipped asset lets through is the owner's to change, so it is not asserted.
    /// </summary>
    public class SoundFocusTests
    {
        private SoundFocusSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<SoundFocusSettings>();
            _settings.Groups.Clear();
            _settings.Overrides.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_settings);
            SoundFocus.ClearOverride();
        }

        [Test]
        public void Test_TheGroupDecides()
        {
            _settings.Groups.Add(new SoundFocusSettings.Group("Spells", true, "sfx_spell_"));
            _settings.Groups.Add(new SoundFocusSettings.Group("Physics", false, "phys_"));
            Assert.IsTrue(_settings.Allows("sfx_spell_frango_cast"));
            Assert.IsFalse(_settings.Allows("phys_impact_wood"));
        }

        [Test]
        public void Test_TheLongestPrefixWins()
        {
            _settings.Groups.Add(new SoundFocusSettings.Group("All effects", false, "sfx_"));
            _settings.Groups.Add(new SoundFocusSettings.Group("Spells", true, "sfx_spell_"));
            Assert.IsTrue(_settings.Allows("sfx_spell_frango_cast"));
            Assert.IsFalse(_settings.Allows("sfx_door_open"));
        }

        [Test]
        public void Test_AnOverrideBeatsItsGroup()
        {
            _settings.Groups.Add(new SoundFocusSettings.Group("Spells", true, "sfx_spell_"));
            _settings.Overrides.Add(new SoundFocusSettings.Override("sfx_spell_ignis_impact", false));
            Assert.IsFalse(_settings.Allows("sfx_spell_ignis_impact"));
            Assert.IsTrue(_settings.Allows("sfx_spell_ignis_misfire"));
        }

        [Test]
        public void Test_EverythingElseFollowsItsSwitch()
        {
            _settings.PlayEverythingElse = false;
            Assert.IsFalse(_settings.Allows("mus_title_loop"));
            _settings.PlayEverythingElse = true;
            Assert.IsTrue(_settings.Allows("mus_title_loop"));
            Assert.IsFalse(_settings.Allows(null));
        }

        [Test]
        public void Test_TheShippedAssetLoadsAndCanBeSwitchedOff()
        {
            Assert.IsNotNull(SoundFocus.Settings, "Assets/_Project/Resources/SoundFocusSettings.asset is missing.");
            SoundFocus.Enabled = false;
            Assert.IsTrue(SoundFocus.Allows("sfx_spell_ignis_cast"));
            Assert.IsTrue(SoundFocus.Allows("mus_title_loop"));
        }
    }
}
