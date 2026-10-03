using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Plunderspell.Alarm;
using Plunderspell.UI;
using UnityEngine;

namespace Plunderspell.Tests.Editor
{
    /// <summary>The UI palette is the design system's; these keep it from drifting from tokens.json.</summary>
    public class UIThemeTests
    {
        private const string TokensPath = "docs/generated/design-system/project/tokens.json";

        private static void AssertHex(string name, Color actual, string hex)
        {
            Assert.AreEqual(hex.Substring(1).ToUpperInvariant(), ToHex(actual), $"{name} is not {hex}");
        }

        private static string ToHex(Color c) =>
            $"{Mathf.RoundToInt(c.r * 255f):X2}{Mathf.RoundToInt(c.g * 255f):X2}{Mathf.RoundToInt(c.b * 255f):X2}";

        /// <summary>Reads each token's dark-theme value out of tokens.json.</summary>
        private static string TokenHex(string tokenName)
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", TokensPath));
            if (!File.Exists(path))
                Assert.Inconclusive($"tokens.json not found at {path}, so the palette was not compared with it.");

            string json = File.ReadAllText(path);
            Match match = Regex.Match(json,
                "\"name\":\\s*\"" + Regex.Escape(tokenName) + "\",\\s*\"value\":\\s*\\{\\s*\"dark\":\\s*\"(#[0-9A-Fa-f]{6})\"");
            Assert.IsTrue(match.Success, $"tokens.json has no colour named {tokenName}.");
            return match.Groups[1].Value;
        }

        [Test]
        public void Test_RolesMatchTheDesignTokens()
        {
            AssertHex("Ground", UITheme.Ground, TokenHex("bone-black"));
            AssertHex("Surface", UITheme.Surface, TokenHex("ash"));
            AssertHex("SurfaceHi", UITheme.SurfaceHi, TokenHex("ash-hi"));
            AssertHex("Text", UITheme.Text, TokenHex("vellum"));
            AssertHex("TextDim", UITheme.TextDim, TokenHex("vellum-dim"));
            AssertHex("TextFaint", UITheme.TextFaint, TokenHex("vellum-faint"));
            AssertHex("Interactive", UITheme.Interactive, TokenHex("verdigris"));
            AssertHex("InteractiveLo", UITheme.InteractiveLo, TokenHex("verdigris-lo"));
            AssertHex("Value", UITheme.Value, TokenHex("orpiment"));
            AssertHex("Danger", UITheme.Danger, TokenHex("madder"));
            AssertHex("Voice", UITheme.Voice, TokenHex("lapis"));
            AssertHex("Line", UITheme.Line, TokenHex("line"));
            AssertHex("LineSoft", UITheme.LineSoft, TokenHex("line-soft"));
            AssertHex("Flint", UITheme.Flint, TokenHex("flint"));
            AssertHex("Umber", UITheme.Umber, TokenHex("umber"));
        }

        [Test]
        public void Test_TheDarkVariantsAreTheMockupsValues()
        {
            // Not in tokens.json: the two low-emphasis variants the mockup adds for madder and lapis.
            AssertHex("DangerLo", UITheme.DangerLo, "#5E2A18");
            AssertHex("VoiceLo", UITheme.VoiceLo, "#3A3350");
        }

        [Test]
        public void Test_TheAlarmClimbsFromQuietToMadder()
        {
            Assert.AreEqual(UITheme.TextFaint, UITheme.AlarmColour(AlarmState.Calm));
            Assert.AreEqual(UITheme.DangerLo, UITheme.AlarmColour(AlarmState.Stirred));
            Assert.AreEqual(UITheme.Danger, UITheme.AlarmColour(AlarmState.Roused));
            Assert.AreEqual(UITheme.Danger, UITheme.AlarmColour(AlarmState.HueAndCry));
        }

        [Test]
        public void Test_TrackedUpperCasesAndSpacesWithAShrunkenSpace()
        {
            // 15 px at 0.18 em over Overpass Mono's 0.62 em space is a 4 px spacer.
            Assert.AreEqual("A<size=4> </size>B", UITheme.Tracked("ab", 15));
            Assert.AreEqual("A", UITheme.Tracked("a", 15));
            Assert.AreEqual(string.Empty, UITheme.Tracked(string.Empty, 15));
            Assert.AreEqual(string.Empty, UITheme.Tracked(null, 15));
        }

        [Test]
        public void Test_TrackedSpacerGrowsWithTheFontSize()
        {
            Assert.AreEqual("A<size=8> </size>B", UITheme.Tracked("ab", 26));
            Assert.AreEqual("A<size=1> </size>B", UITheme.Tracked("ab", 1));
        }
    }
}
