using NUnit.Framework;
using Plunderspell.Audio;

namespace Plunderspell.Tests.Editor
{
    /// <summary>The reduced playtest sound set: footsteps, guards and UI only.</summary>
    public class SoundFocusTests
    {
        private bool _wasEnabled;

        [SetUp]
        public void SetUp() => _wasEnabled = SoundFocus.Enabled;

        [TearDown]
        public void TearDown() => SoundFocus.Enabled = _wasEnabled;

        [TestCase("foley_step_stone_walk")]
        [TestCase("vo_bronze_alert")]
        [TestCase("vo_hound_howl")]
        [TestCase("sfx_wpn_blade_swing")]
        [TestCase("ui_button_click")]
        public void Test_KeptSoundsPlay(string name)
        {
            SoundFocus.Enabled = true;
            Assert.IsTrue(SoundFocus.Allows(name));
        }

        [TestCase("sfx_spell_ignis_cast")]
        [TestCase("mus_title_loop")]
        [TestCase("amb_fire_crackle")]
        [TestCase("sting_alarm_roused_late")]
        [TestCase("sfx_door_open")]
        [TestCase("sfx_player_hurt")]
        [TestCase("phys_break_pottery")]
        [TestCase("phys_impact_wood")]
        [TestCase("phys_scrape_stone_loop")]
        public void Test_EverythingElseIsMuted(string name)
        {
            SoundFocus.Enabled = true;
            Assert.IsFalse(SoundFocus.Allows(name));
        }

        [Test]
        public void Test_TurningItOffPlaysEverything()
        {
            SoundFocus.Enabled = false;
            Assert.IsTrue(SoundFocus.Allows("sfx_spell_ignis_cast"));
            Assert.IsTrue(SoundFocus.Allows("mus_title_loop"));
        }
    }
}
