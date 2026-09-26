using Code.Scripts.EventSystems;
using Player;
using Plunderspell.Acoustics;
using Plunderspell.Alarm;
using Plunderspell.Guards;
using Plunderspell.Playtest;
using Plunderspell.Raid;
using Plunderspell.Spells;
using Plunderspell.Spells.Vfx;
using Plunderspell.Status;
using Plunderspell.UI;
using Plunderspell.Voice;
using StateMachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Builds the combat bench scene: a walled arena, the raid's own player rig, and a spawn panel.
    /// Rebuilt from scratch every run and saved over the same path, so it cannot accumulate cruft.
    ///
    /// See docs/4-systems/combat-bench.md.
    /// </summary>
    public static class CombatBenchSceneBuilder
    {
        private const string k_ScenePath = "Assets/_Project/Scenes/CombatBench.unity";
        private const string k_RosterPath = "Assets/_Project/Data/Enemies/EnemyRoster.asset";
        private const string k_SwordPath = "Assets/_Project/Prefabs/Weapons/ArmingSword.prefab";
        private const string k_LexiconPath = "Assets/_Project/Data/Spells/SpellLexicon.asset";
        private const string k_BoltPath = "Assets/_Project/Prefabs/Projectiles/Bolt.prefab";

        private const float k_ArenaSize = 40f;
        private const float k_WallHeight = 5f;
        private const float k_WallThickness = 1f;
        private const float k_GroundThickness = 1f;

        private const float k_PlayerHeight = 1.8f;
        private const float k_PlayerRadius = 0.4f;
        private const float k_EyeHeight = 1.65f;

        [MenuItem("Tools/Plunderspell/Build Combat Bench Scene")]
        public static void BuildCombatBenchScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildSunlight();
            BuildManagers();
            BuildArena();
            BuildAlarm();
            BuildSpellVfx();

            GameObject player = BuildPlayer();
            BuildBench(player.transform);
            PlaceStarterWeapon(player.transform);

            EditorSceneManager.SaveScene(scene, k_ScenePath);
            Debug.Log($"[CombatBench] Built {k_ScenePath}.");
        }

        private static void BuildSunlight()
        {
            var go = new GameObject("Sunlight");
            Light light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        /// <summary>Scene-owned singletons the swing path needs. See docs/4-systems/combat-bench.md,
        /// "Why the raid's rig, not the playtest harness".</summary>
        private static void BuildManagers()
        {
            new GameObject("EventManager").AddComponent<EventManager>();
            new GameObject("ItemManager").AddComponent<ItemManager>();
        }

        private static void BuildArena()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0f, -k_GroundThickness * 0.5f, 0f);
            ground.transform.localScale = new Vector3(k_ArenaSize, k_GroundThickness, k_ArenaSize);
            ground.GetComponent<Renderer>().sharedMaterial =
                MakeMaterial("BenchGround", new Color(0.20f, 0.22f, 0.20f));

            float offset = (k_ArenaSize + k_WallThickness) * 0.5f;
            BuildWall("WallNorth", new Vector3(0f, k_WallHeight * 0.5f, offset),
                new Vector3(k_ArenaSize + k_WallThickness * 2f, k_WallHeight, k_WallThickness));
            BuildWall("WallSouth", new Vector3(0f, k_WallHeight * 0.5f, -offset),
                new Vector3(k_ArenaSize + k_WallThickness * 2f, k_WallHeight, k_WallThickness));
            BuildWall("WallEast", new Vector3(offset, k_WallHeight * 0.5f, 0f),
                new Vector3(k_WallThickness, k_WallHeight, k_ArenaSize));
            BuildWall("WallWest", new Vector3(-offset, k_WallHeight * 0.5f, 0f),
                new Vector3(k_WallThickness, k_WallHeight, k_ArenaSize));
        }

        private static void BuildWall(string name, Vector3 position, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.position = position;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().sharedMaterial =
                MakeMaterial("BenchWall", new Color(0.32f, 0.33f, 0.36f));
        }

        /// <summary>Guards find their alarm by searching the scene; one here means a shout
        /// escalates as it does in a raid.</summary>
        private static void BuildAlarm()
        {
            var go = new GameObject("BenchAlarm");
            var collider = go.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = Vector3.one * (k_ArenaSize * 2f);
            go.AddComponent<AlarmFSMManager>();
        }

        /// <summary>See docs/4-systems/combat-bench.md, "The bench carries its own spell VFX".</summary>
        private static void BuildSpellVfx()
        {
            var bolt = AssetDatabase.LoadAssetAtPath<GameObject>(k_BoltPath);
            if (bolt == null)
            {
                Debug.LogWarning($"[CombatBench] No projectile at {k_BoltPath}; bolt spells will fall back to a burst.");
                return;
            }

            var go = new GameObject("SpellVfx");
            var director = go.AddComponent<SpellVfxDirector>();

            var serialized = new SerializedObject(director);
            serialized.FindProperty("m_boltPrefab").objectReferenceValue = bolt;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>The raid's rig, not the playtest harness. See docs/4-systems/combat-bench.md,
        /// "Why the raid's rig, not the playtest harness".</summary>
        private static GameObject BuildPlayer()
        {
            var root = new GameObject("Player");
            root.tag = "Player";
            root.transform.position = new Vector3(0f, k_PlayerHeight * 0.5f, 0f);

            var collider = root.AddComponent<CapsuleCollider>();
            collider.height = k_PlayerHeight;
            collider.radius = k_PlayerRadius;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = new Vector3(k_PlayerRadius * 2f, k_PlayerHeight * 0.5f,
                k_PlayerRadius * 2f);
            Object.DestroyImmediate(visual.GetComponent<CapsuleCollider>());
            visual.GetComponent<Renderer>().sharedMaterial =
                MakeMaterial("BenchPlayer", new Color(0.25f, 0.45f, 0.75f));

            // PlayerStateMachine pulls in Rigidbody through its [RequireComponent].
            PlayerStateMachine stateMachine = root.AddComponent<PlayerStateMachine>();
            SetGroundCheckFields(stateMachine);

            var eye = new GameObject("Eye");
            eye.transform.SetParent(root.transform, false);
            eye.transform.localPosition = new Vector3(0f, k_EyeHeight - k_PlayerHeight * 0.5f, 0f);
            Camera camera = eye.AddComponent<Camera>();
            camera.tag = "MainCamera";
            eye.AddComponent<AudioListener>();
            stateMachine.CameraTransform = eye.transform;

            var hand = new GameObject("HandSocket");
            hand.transform.SetParent(eye.transform, false);
            hand.transform.localPosition = new Vector3(0.4f, -0.3f, 0.6f);

            root.AddComponent<PlayerInputController>();
            root.AddComponent<StatusEffectReceiver>();
            root.AddComponent<AcousticEmitter>();
            root.AddComponent<FootstepNoiseEmitter>();
            root.AddComponent<IntruderTag>();
            WireCasting(root);
            return root;
        }

        /// <summary>See docs/4-systems/combat-bench.md, "The bench carries its own spell VFX".</summary>
        private static void WireCasting(GameObject player)
        {
            player.AddComponent<PushToCastController>();
            var casting = player.AddComponent<SpellCastingSystem>();

            var lexicon = AssetDatabase.LoadAssetAtPath<SpellLexicon>(k_LexiconPath);
            if (lexicon == null)
            {
                Debug.LogWarning($"[CombatBench] No SpellLexicon at {k_LexiconPath}; every phrase will fizzle.");
                return;
            }

            casting.SetLexicon(lexicon);
        }

        // The ground-check fields are private [SerializeField], so they need SerializedObject
        // rather than a direct write.
        private static void SetGroundCheckFields(PlayerStateMachine stateMachine)
        {
            var serialized = new SerializedObject(stateMachine);
            serialized.FindProperty("_groundCheckRadius").floatValue = 0.3f;
            serialized.FindProperty("_groundCheckDistance").floatValue = 1.6f;
            serialized.FindProperty("_groundedHeight").floatValue = k_PlayerHeight * 0.5f;
            serialized.FindProperty("_groundLayer").intValue = LayerMask.GetMask("Default");
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildBench(Transform player)
        {
            var go = new GameObject("CombatBench");
            CombatBench bench = go.AddComponent<CombatBench>();
            go.AddComponent<CombatBenchHud>();
            go.AddComponent<CrosshairView>();

            var serialized = new SerializedObject(bench);
            serialized.FindProperty("m_roster").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<EnemyRoster>(k_RosterPath);
            serialized.FindProperty("m_spawnOrigin").objectReferenceValue = player;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Something to swing, dropped at the player's feet rather than put in their hand,
        /// so picking it up is part of what the bench exercises.</summary>
        private static void PlaceStarterWeapon(Transform player)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_SwordPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[CombatBench] No weapon prefab at {k_SwordPath}; bench has nothing to swing.");
                return;
            }

            GameObject sword = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            sword.transform.position = player.position + player.forward * 2f + Vector3.up * 0.5f;
        }

        private static Material MakeMaterial(string name, Color colour)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { name = name };
            material.color = colour;
            return material;
        }
    }
}
