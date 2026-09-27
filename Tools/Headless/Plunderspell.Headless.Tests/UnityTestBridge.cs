using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.TestTools;

namespace Plunderspell.Tests.Headless
{
    /// <summary>
    /// Runs the project's <c>[UnityTest]</c> coroutine tests under the plain NUnit runner.
    ///
    /// NUnit cannot execute a test method that returns <see cref="IEnumerator"/>, so this bridge
    /// discovers every <c>[UnityTest]</c> in the linked Unity test assemblies, builds the fixture,
    /// runs its <c>[SetUp]</c>/<c>[TearDown]</c> around the body and drains the coroutine to
    /// completion. <c>yield return new WaitForSeconds(t)</c> advances the shim clock by <c>t</c>
    /// instead of sleeping, so time-dependent assertions still see time pass — and the suite stays
    /// fast and deterministic.
    /// </summary>
    [TestFixture]
    public class UnityTestBridge
    {
        public static IEnumerable<TestCaseData> UnityTests()
        {
            IEnumerable<Type> types = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(SafeTypes)
                .Where(t => t.IsClass && !t.IsAbstract);

            foreach (Type type in types)
            {
                foreach (MethodInfo m in type.GetMethods(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (m.GetCustomAttribute<UnityTestAttribute>() == null) continue;
                    if (!typeof(IEnumerator).IsAssignableFrom(m.ReturnType)) continue;
                    yield return new TestCaseData(type, m.Name).SetName($"{type.Name}.{m.Name}");
                }
            }
        }

        private static IEnumerable<Type> SafeTypes(Assembly a)
        {
            try { return a.GetTypes(); }
            catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null); }
        }

        [TestCaseSource(nameof(UnityTests))]
        public void RunUnityTest(Type fixtureType, string methodName)
        {
            object fixture = Activator.CreateInstance(fixtureType);
            MethodInfo method = fixtureType.GetMethod(methodName);
            Assert.That(method, Is.Not.Null, $"{fixtureType.Name}.{methodName} disappeared");

            InvokeAll(fixture, typeof(SetUpAttribute));
            try
            {
                Drain((IEnumerator)method.Invoke(fixture, Array.Empty<object>()));
            }
            catch (TargetInvocationException e) when (e.InnerException != null)
            {
                throw e.InnerException;
            }
            finally
            {
                InvokeAll(fixture, typeof(TearDownAttribute));
            }
        }

        private static void InvokeAll(object fixture, Type attribute)
        {
            foreach (MethodInfo m in fixture.GetType()
                         .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                         .Where(m => m.GetCustomAttributes(attribute, true).Length > 0))
                m.Invoke(fixture, Array.Empty<object>());
        }

        /// <summary>Runs a coroutine to completion, turning frame/second yields into clock advances.</summary>
        internal static void Drain(IEnumerator routine, int depth = 0)
        {
            if (routine == null || depth > 16) return;
            int guard = 0;
            while (routine.MoveNext())
            {
                if (guard++ > 200000)
                    throw new TimeoutException("Coroutine did not finish within 200k steps.");

                switch (routine.Current)
                {
                    case IEnumerator nested:
                        Drain(nested, depth + 1);
                        break;
                    case WaitForSeconds wait:
                        Time.Advance(WaitSeconds(wait));
                        break;
                    default:
                        Time.Advance(Time.deltaTime);
                        break;
                }
            }
        }

        /// <summary>WaitForSeconds keeps its duration private in Unity; the shim stores it in a field.</summary>
        private static float WaitSeconds(WaitForSeconds wait)
        {
            FieldInfo f = typeof(WaitForSeconds).GetField("_seconds",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return f != null ? (float)f.GetValue(wait) : 0f;
        }
    }
}
