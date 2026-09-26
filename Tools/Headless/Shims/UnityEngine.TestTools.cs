// Shim of the UnityEngine.TestTools surface the project's play-mode tests use, so the very same
// test files run under the headless NUnit runner. [UnityTest] coroutines are executed by the
// UnityTestBridge in the test project, which drains the returned IEnumerator.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace UnityEngine.TestTools
{
    /// <summary>Marks a coroutine-style test. Discovered and driven by the headless UnityTestBridge.</summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class UnityTestAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)] public class UnitySetUpAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public class UnityTearDownAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public class PrebuildSetupAttribute : Attribute
    {
        public PrebuildSetupAttribute(Type type) { }
        public PrebuildSetupAttribute(string type) { }
    }

    /// <summary>
    /// Asserts on captured log output. Backed by the shim's Debug log buffer, so expectations behave
    /// the way they do in the editor: an expected message is consumed from the buffer when matched.
    /// </summary>
    public static class LogAssert
    {
        public static bool ignoreFailingMessages { get; set; }

        public static void Expect(LogType type, string message) => ExpectMatching(message, Regex.Escape(message));
        public static void Expect(LogType type, Regex pattern) => ExpectMatching(pattern.ToString(), pattern.ToString());

        private static void ExpectMatching(string description, string pattern)
        {
            List<Debug.Entry> entries = Debug.Log_Entries.ToList();
            bool found = entries.Any(e => e.Message != null && Regex.IsMatch(e.Message, pattern));
            if (!found)
                throw new Exception($"LogAssert.Expect: no log entry matching \"{description}\". " +
                                    $"Captured: {string.Join(" | ", entries.Select(e => e.Message))}");
        }

        public static void NoUnexpectedReceived()
        {
            List<Debug.Entry> bad = Debug.Log_Entries
                .Where(e => e.Severity == Debug.Level.Error || e.Severity == Debug.Level.Assert).ToList();
            if (bad.Count > 0 && !ignoreFailingMessages)
                throw new Exception("Unexpected error log(s): " + string.Join(" | ", bad.Select(e => e.Message)));
        }
    }
}

namespace UnityEngine
{
    public enum LogType { Error, Assert, Warning, Log, Exception }
}
