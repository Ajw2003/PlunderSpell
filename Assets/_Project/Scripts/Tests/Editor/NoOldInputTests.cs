using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Plunderspell.Tests.Editor
{
    /// <summary>
    /// Keys are read only through the input actions (Player.GameInput) and the new Input System, never the old
    /// UnityEngine.Input class (#350). Old-input code came back once by being copied; see docs/6-decisions/Decisions.md,
    /// "old-input keys came back in the Lair and Market work".
    /// </summary>
    public class NoOldInputTests
    {
        // The old class used bare or as UnityEngine.Input; InputSystem, PlayerInputs and someObject.Input do not match.
        private static readonly Regex OldInput = new Regex(
            @"(?<![\w.])(UnityEngine\.)?Input\.(Get\w+|mouse\w+|any\w+|touch\w*|inputString|acceleration|compass|gyro|ResetInputAxes)\b");

        [Test]
        public void NoScriptReadsTheOldInputClass()
        {
            string[] offenders = Directory.GetFiles("Assets/_Project", "*.cs", SearchOption.AllDirectories)
                .SelectMany(path => File.ReadAllLines(path).Select((line, index) => (path, line, index)))
                .Where(hit => OldInput.IsMatch(hit.line))
                .Select(hit => $"{hit.path.Replace('\\', '/')}:{hit.index + 1}: {hit.line.Trim()}")
                .ToArray();

            Assert.IsEmpty(offenders,
                "Read keys through Player.GameInput's input actions, not UnityEngine.Input:\n" + string.Join("\n", offenders));
        }
    }
}
