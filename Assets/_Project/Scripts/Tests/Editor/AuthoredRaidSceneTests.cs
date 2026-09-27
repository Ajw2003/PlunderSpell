using NUnit.Framework;
using Player;
using Plunderspell.Guards;
using Plunderspell.Loot;
using Plunderspell.Raid;
using Plunderspell.UI;
using StateMachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Plunderspell.Tests.Editor
{
    /// <summary>
    /// Guards the wiring of the hand-authored raid scene. See
    /// docs/4-systems/raid-scene-assembly.md, "Authored, not generated", for why this exists.
    /// </summary>
    public class AuthoredRaidSceneTests
    {
        private const string k_ScenePath = "Assets/_Project/Scenes/RaidScene.unity";
        private const string k_PlayerPrefabPath = "Assets/_Project/Prefabs/RaidPlayer.prefab";

        private Scene m_scene;

        [SetUp]
        public void SetUp()
        {
            m_scene = EditorSceneManager.OpenScene(k_ScenePath, OpenSceneMode.Additive);
        }

        [TearDown]
        public void TearDown()
        {
            if (m_scene.IsValid())
            {
                EditorSceneManager.CloseScene(m_scene, true);
            }
        }

        private T Find<T>() where T : Object
        {
            foreach (GameObject root in m_scene.GetRootGameObjects())
            {
                var found = root.GetComponentInChildren<T>(true);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private GameObject PlayerPrefab() => AssetDatabase.LoadAssetAtPath<GameObject>(k_PlayerPrefabPath);

        /// <summary>
        /// The player is not placed in the scene: the network spawns one per connection, solo
        /// included, from the prefab the spawner names. See docs/4-systems/net.md.
        /// </summary>
        [Test]
        public void Test_ThePlayerIsSpawnedFromTheAuthoredPrefab()
        {
            Assert.IsNull(Find<PlayerStateMachine>(),
                "A player placed in the scene shares one network ID across every machine, so a " +
                "friend's actions would land on the host's body. Players come from the spawner.");

            var spawner = Find<PurrNet.PlayerSpawner>();
            Assert.IsNotNull(spawner, $"{k_ScenePath} has no PlayerSpawner, so nobody would have a body.");
            var prefab = new SerializedObject(spawner).FindProperty("_playerPrefab").objectReferenceValue;
            Assert.AreEqual(k_PlayerPrefabPath, AssetDatabase.GetAssetPath(prefab));

            var session = Find<Plunderspell.Net.CoopSession>();
            Assert.IsNotNull(session, $"{k_ScenePath} has no CoopSession, so no session can start.");
            var wiring = new SerializedObject(session);
            foreach (string field in new[] { "_manager", "_localTransport", "_udpTransport", "_steamTransport" })
                Assert.IsNotNull(wiring.FindProperty(field).objectReferenceValue, $"CoopSession.{field} is unassigned.");
        }

        /// <summary>
        /// The regression that started all this: the player carries the shipping controller, and the
        /// playtest harness is not in the raid.
        /// </summary>
        [Test]
        public void Test_ThePlayerUsesTheShippingControllerNotThePlaytestHarness()
        {
            Assert.IsNotNull(PlayerPrefab().GetComponent<PlayerInputController>(),
                "The raid player needs PlayerInputController; without it nothing drives the " +
                "state machine and the player reads as unresponsive.");
            Assert.IsNull(Find<Plunderspell.Playtest.FreeLookPlaytestController>(),
                "FreeLookPlaytestController is the ItemGym harness. In the raid it silently " +
                "replaces the real player and undoes the issue 9 input gate.");
        }

        /// <summary>
        /// The CastleBench player is the reference: it is the one voice casting was confirmed on. The
        /// raid prefab once differed (push-to-cast on F19, the eye 1.65 m above the capsule's centre),
        /// so casting did nothing and the view sat in door lintels.
        /// </summary>
        [Test]
        public void Test_ThePlayerMatchesTheCastleBenchSetup()
        {
            GameObject player = PlayerPrefab();
            var pushToCast = new SerializedObject(player.GetComponent<Plunderspell.Voice.PushToCastController>());
            SerializedProperty key = pushToCast.FindProperty("_pushToCastKey");
            Assert.AreEqual("V", key.enumNames[key.enumValueIndex], "The HUD says Hold V to cast.");
            Assert.AreEqual(0.75f, player.transform.Find("Eye").localPosition.y, 0.001f,
                "The eye sits 0.75 m above the capsule's centre, as on the CastleBench player.");
        }

        [Test]
        public void Test_ThePlayerCarriesWhatTheRaidExpectsOfIt()
        {
            GameObject player = PlayerPrefab();
            Assert.IsNotNull(player.GetComponentInChildren<IntruderTag>(true), "Guards find intruders through IntruderTag.");
            // Picking things up is ItemManager's mouse drag, as on the CastleBench player this prefab
            // now copies; the E/Q LootInteractor was removed with the rest of the old setup.
            Assert.IsNotNull(player.GetComponent<Plunderspell.Voice.PushToCastController>(), "Nothing can be cast without push-to-cast.");
            Assert.IsNotNull(player.GetComponentInChildren<Camera>(true), "The player has no camera to see out of.");
            Assert.IsNotNull(player.GetComponent<PurrNet.NetworkTransform>(), "Nobody else would see this player move.");
            Assert.IsNotNull(player.GetComponent<PlayerNetworkOwnership>(),
                "Without ownership gating every machine drives every body and runs every camera.");
        }

        /// <summary>
        /// A generated scene had its references wired by the builder every run. An authored one keeps
        /// whatever was last saved, so a reference dropped by hand stays dropped until someone plays
        /// it — this is what replaces the builder as the check.
        /// </summary>
        [Test]
        public void Test_TheDirectorAndHudAreStillWiredToTheScene()
        {
            var director = Find<RaidDirector>();
            Assert.IsNotNull(director, $"{k_ScenePath} has no RaidDirector.");

            var serialized = new SerializedObject(director);
            foreach (string field in new[]
                     {
                         "_generator", "_lootSpawner", "_guardSpawner", "_extractionZone",
                         "_lair", "_alarm", "_navigation",
                     })
            {
                SerializedProperty property = serialized.FindProperty(field);
                Assert.IsNotNull(property, $"RaidDirector has no field {field}.");
                Assert.IsNotNull(property.objectReferenceValue,
                    $"RaidDirector.{field} is unassigned in the authored scene.");
            }

            var presenter = Find<RaidHudPresenter>();
            Assert.IsNotNull(presenter, $"{k_ScenePath} has no RaidHudPresenter.");

            var hud = new SerializedObject(presenter);
            foreach (string field in new[] { "_director", "_extractionZone", "_alarm", "_lair" })
            {
                SerializedProperty property = hud.FindProperty(field);
                Assert.IsNotNull(property, $"RaidHudPresenter has no field {field}.");
                Assert.IsNotNull(property.objectReferenceValue,
                    $"RaidHudPresenter.{field} is unassigned in the authored scene.");
            }
        }
    }
}
