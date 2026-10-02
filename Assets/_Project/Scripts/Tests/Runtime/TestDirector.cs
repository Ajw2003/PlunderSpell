using Plunderspell.Alarm;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>
    /// Test seam for the <see cref="EnemyDirector"/> (#205). The intruder and guard registries used to
    /// be static lists a test could clear; they now live on the director, so tests share one per test
    /// through here and tear it down with <see cref="Reset"/>.
    /// </summary>
    internal static class TestDirector
    {
        /// <summary>The director in play, creating a bare one when there is none.</summary>
        public static EnemyDirector Ensure()
        {
            if (EnemyDirector.Current != null)
                return EnemyDirector.Current;
            return new GameObject("TestDirector").AddComponent<EnemyDirector>();
        }

        /// <summary>Destroys the director in play, taking its registries with it.</summary>
        public static void Reset()
        {
            foreach (EnemyDirector director in Object.FindObjectsByType<EnemyDirector>(FindObjectsSortMode.None))
                Object.DestroyImmediate(director.gameObject);
        }

        /// <summary>Forgets every intruder on the director in play, if any.</summary>
        public static void ClearIntruders()
        {
            if (EnemyDirector.Current != null)
                EnemyDirector.Current.ClearIntruders();
        }
    }
}
