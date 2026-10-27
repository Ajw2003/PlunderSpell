using NUnit.Framework;
using Plunderspell.EditorTools;
using Plunderspell.Playtest;
using Plunderspell.Raid;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Plunderspell.Tests.Editor
{
    /// <summary>
    /// The Ophan Gym (#452) is the main level plus the gym, and the main level never carries the gym. See
    /// docs/4-systems/fear.md, "The Ophan Gym".
    /// </summary>
    public class OphanGymSceneTests
    {
        private static T FindIn<T>(Scene scene) where T : Object
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                var found = root.GetComponentInChildren<T>(true);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static void WithScene(string path, System.Action<Scene> check)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                check(scene);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void Test_TheGymCarriesItsRulesKeysAndPanelBesideTheOphan()
        {
            WithScene(OphanGymBuilder.GymScenePath, scene =>
            {
                var gym = FindIn<OphanGym>(scene);
                Assert.IsNotNull(gym, "OphanGym.unity has no OphanGym. Run Tools > Plunderspell > Build Ophan Gym.");
                Assert.IsNotNull(gym.GetComponent<OphanOfHours>(), "The gym must sit on the Ophan's own Network object.");
                Assert.IsNotNull(gym.GetComponent<OphanGymKeys>(), "The gym has no test keys.");
                Assert.IsNotNull(gym.GetComponent<OphanGymPanel>(), "The gym has no panel.");
            });
        }

        [Test]
        public void Test_TheMainLevelHasNoGym()
        {
            WithScene(OphanGymBuilder.RaidScenePath, scene =>
                Assert.IsNull(FindIn<OphanGym>(scene), "RaidScene.unity carries the Ophan Gym; only OphanGym.unity may."));
        }
    }
}
