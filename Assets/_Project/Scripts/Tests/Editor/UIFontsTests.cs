using NUnit.Framework;
using Plunderspell.UI;
using UnityEngine;

namespace Plunderspell.Tests.Editor
{
    /// <summary>
    /// The redesign's three typefaces must actually load from Resources/UI/Fonts. A missing or
    /// misnamed file falls back to the built-in font with a warning, which looks like working text
    /// until someone notices every label is Arial-ish, so this asserts the fallback did not happen.
    /// </summary>
    public class UIFontsTests
    {
        private static void AssertLoaded(string role, Font font)
        {
            Assert.IsNotNull(font, $"{role} font is null.");
            Assert.AreNotEqual("LegacyRuntime", font.name,
                $"{role} fell back to the built-in font: its file under Resources/UI/Fonts did not load.");
        }

        [Test]
        public void Test_DisplayFontsLoad()
        {
            AssertLoaded("Display", UIFonts.Display);
            AssertLoaded("DisplayHeavy", UIFonts.DisplayHeavy);
        }

        [Test]
        public void Test_BodyFontsLoad()
        {
            AssertLoaded("Body", UIFonts.Body);
            AssertLoaded("BodyLight", UIFonts.BodyLight);
            AssertLoaded("BodyItalic", UIFonts.BodyItalic);
        }

        [Test]
        public void Test_MonoFontsLoad()
        {
            AssertLoaded("Mono", UIFonts.Mono);
            AssertLoaded("MonoBold", UIFonts.MonoBold);
        }

        [Test]
        public void Test_AFontIsLoadedOnceAndReused()
        {
            Assert.AreSame(UIFonts.Display, UIFonts.Display);
        }
    }
}
