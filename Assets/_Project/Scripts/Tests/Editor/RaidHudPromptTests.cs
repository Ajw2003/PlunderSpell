using NUnit.Framework;
using Plunderspell.UI;

namespace Plunderspell.Tests.Editor
{
    /// <summary>The interact prompt is split so its key can be drawn in a box; see RaidHudView.SplitPrompt.</summary>
    public class RaidHudPromptTests
    {
        [Test]
        public void Test_PickUpSplitsIntoKeyAndAction()
        {
            Assert.IsTrue(RaidHudView.SplitPrompt("Press [E] to pick up Gold Death Mask", out string key, out string rest));
            Assert.AreEqual("E", key);
            Assert.AreEqual("pick up Gold Death Mask", rest);
        }

        [Test]
        public void Test_ATwoPersonLiftKeepsItsWarning()
        {
            Assert.IsTrue(RaidHudView.SplitPrompt("Press [E] to lift Rolled Tapestry — needs two", out string key, out string rest));
            Assert.AreEqual("E", key);
            Assert.AreEqual("lift Rolled Tapestry — needs two", rest);
        }

        [Test]
        public void Test_OpeningADoorAndDropping()
        {
            Assert.IsTrue(RaidHudView.SplitPrompt("Press [E] to open the door", out string key, out string rest));
            Assert.AreEqual("E", key);
            Assert.AreEqual("open the door", rest);

            Assert.IsTrue(RaidHudView.SplitPrompt("Press [Q] to drop", out key, out rest));
            Assert.AreEqual("Q", key);
            Assert.AreEqual("drop", rest);
        }

        [Test]
        public void Test_ATextThatIsNotAPromptComesBackWhole()
        {
            const string text = "Haul: bring loot to the portal";
            Assert.IsFalse(RaidHudView.SplitPrompt(text, out string key, out string rest));
            Assert.AreEqual(string.Empty, key);
            Assert.AreEqual(text, rest);
        }

        [Test]
        public void Test_AnEmptyKeyOrNoTextIsNotSplit()
        {
            Assert.IsFalse(RaidHudView.SplitPrompt("Press [] to nothing", out _, out string rest));
            Assert.AreEqual("Press [] to nothing", rest);

            Assert.IsFalse(RaidHudView.SplitPrompt(string.Empty, out _, out rest));
            Assert.AreEqual(string.Empty, rest);
        }
    }
}
