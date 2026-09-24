using System.Collections.Generic;
using System.IO;
using Player;
using RogueAi.Acoustics;
using RogueAi.Alarm;
using RogueAi.Castle;
using RogueAi.Extraction;
using RogueAi.Guards;
using RogueAi.Inventory;
using RogueAi.Lair;
using RogueAi.Loot;
using RogueAi.Playtest;
using RogueAi.Raid;
using RogueAi.Spells;
using RogueAi.Status;
using RogueAi.UI;
using RogueAi.Voice;
using StateMachine;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace RogueAi.EditorTools
{
    /// <summary>
    /// Assembles a raid-scene **scaffold** from the project's authored assets: the 25 castle room
    /// prefabs, the 5 loot prefabs, the 10 enemy prefabs, extraction, HUD and the authored player.
    ///
    /// It writes `RaidScene.Scaffold.unity`, never `RaidScene.unity` — the raid scene is hand-authored
    /// now. See docs/systems/raid-scene-assembly.md, "Authored, not generated", for what that means
    /// for this tool, and for what this builds and the order it builds it in.
    /// </summary>
    public static class RaidSceneBuilder
    {
        // Tune here rather than hunting through the assembled scene.
        private const float CellSize = 12f;
        private const float EyeHeight = 1.65f;
        private const float RaidSeconds = 300f;

        private const string SceneDirectory = "Assets/_Project/Scenes";
        private const string DataDirectory = "Assets/_Project/Data/Generated";

        /// <summary>
        /// The scaffold this writes — deliberately NOT the authored scene. See
        /// docs/systems/raid-scene-assembly.md, "Authored, not generated".
        /// </summary>
        private const string ScenePath = SceneDirectory + "/RaidScene.Scaffold.unity";

        /// <summary>The hand-edited scene this tool must never write to.</summary>
        private const string AuthoredScenePath = SceneDirectory + "/RaidScene.unity";

        /// <summary>The authored player rig the scaffold instances, so both scenes share one player.</summary>
        private const string RaidPlayerPrefabPath = "Assets/_Project/Prefabs/RaidPlayer.prefab";

        // The authored assets this scene is assembled from. A missing one is a hard error, not a
        // silent fallback to primitives — that fallback is exactly how the art stopped being used.
        private const string RegistryPath = "Assets/_Project/Data/Castle/CastleRoomRegistry.asset";
        private const string LootTablePath = "Assets/_Project/Data/Loot/RaidLootTable.asset";
        private const string EnemyRosterPath = "Assets/_Project/Data/Enemies/EnemyRoster.asset";
        private const string ConjuredCoinItemPath = "Assets/_Project/Data/Loot/Loot_Conjured_Coin.asset";
        private const string ConjuredCoinPrefabPath = "Assets/_Project/Prefabs/Loot/GoldenGoblet.prefab";

        [MenuItem("Tools/Plunderspell/Build Raid Scene Scaffold")]
        public static void BuildRaidScene()
        {
            // Belt and braces. The path is a constant, but this is the one mistake in this file that
            // silently destroys a day of hand-authoring, so it is asserted rather than assumed.
            if (ScenePath == AuthoredScenePath)
            {
                Debug.LogError($"[RaidScaffold] Refusing to run: the output path is the authored " +
                               $"scene {AuthoredScenePath}.");
                return;
            }

            EnsureFolder(SceneDirectory);
            EnsureFolder(DataDirectory);

            // The catalogues are loaded AFTER the new scene, never before — see
            // docs/systems/raid-scene-assembly.md ("Traps").
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            if (!TryLoadAuthoredAssets(out CastleRoomRegistry registry, out RaidLootTable lootTable,
                    out EnemyRoster roster))
                return;

            BuildLight();
            BuildGround();

            AlarmFSMManager alarm = BuildAlarm();
            BuildLockdown(alarm);
            LairHubManager lair = BuildLair();
            ExtractionZone extraction = BuildExtractionZone();
            ProceduralCastleGenerator generator = BuildGenerator(registry);
            LootSpawner lootSpawner = BuildLootSpawner(lootTable);
            GuardSpawner guardSpawner = BuildGuardSpawner(roster);
            CastleNavMeshBaker navigation = BuildNavigation();

            GameObject player = BuildPlayer(ResolveSpawn(generator));

            RaidDirector director = BuildDirector(generator, lootSpawner, guardSpawner, extraction,
                lair, alarm, navigation, player.transform);
            BuildHud(director, extraction, alarm, lair, player.GetComponentInChildren<LootInteractor>());

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log($"Plunderspell: raid scene assembled at {ScenePath} from " +
                      $"{registry.Modules.Count} room prefab(s), {lootTable.Entries.Count} loot " +
                      $"placement(s) and {roster.Entries.Count} enemy posting(s). " +
                      "Press Play; WASD to move, E to take, Q to drop, V to cast, F5 to extract.");
        }

        /// <summary>
        /// Loads the three authored catalogues, reporting every one that is missing rather than the
        /// first. Returns false when any is absent, so the scene is never written half-wired.
        /// </summary>
        private static bool TryLoadAuthoredAssets(out CastleRoomRegistry registry,
            out RaidLootTable lootTable, out EnemyRoster roster)
        {
            registry = AssetDatabase.LoadAssetAtPath<CastleRoomRegistry>(RegistryPath);
            lootTable = AssetDatabase.LoadAssetAtPath<RaidLootTable>(LootTablePath);
            roster = AssetDatabase.LoadAssetAtPath<EnemyRoster>(EnemyRosterPath);

            var missing = new List<string>();
            if (registry == null)
                missing.Add($"castle room registry at {RegistryPath}");
            if (lootTable == null)
                missing.Add($"loot table at {LootTablePath} (run Tools/Plunderspell/Forge Raid Loot Table)");
            if (roster == null)
                missing.Add($"enemy roster at {EnemyRosterPath} (run Tools/Plunderspell/Forge Enemy Prefabs + Roster)");

            if (missing.Count == 0)
                return true;

            Debug.LogError("Plunderspell: cannot assemble the raid scene, missing " +
                           string.Join("; ", missing) + ".");
            return false;
        }

        // --- World --------------------------------------------------------------------------

        private static void BuildLight()
        {
            var go = new GameObject("DirectionalLight");
            Light light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.9f;
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void BuildGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            // The castle spans roughly 11 cells across; a plane is 10 units, hence the scale.
            ground.transform.localScale = Vector3.one * (CellSize * 14f / 10f);
            ground.GetComponent<Renderer>().sharedMaterial = MakeMaterial("GroundMaterial",
                new Color(0.18f, 0.20f, 0.18f));
        }

        // --- Systems ------------------------------------------------------------------------

        private static AlarmFSMManager BuildAlarm()
        {
            var go = new GameObject("CastleAlarm");
            // The alarm listens for noise like anything else, so it needs a collider to be found by
            // the overlap query. A large trigger means it hears the whole castle.
            var collider = go.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = Vector3.one * (CellSize * 12f);
            return go.AddComponent<AlarmFSMManager>();
        }

        private static LairHubManager BuildLair()
        {
            var go = new GameObject("Lair");
            return go.AddComponent<LairHubManager>();
        }

        private static ExtractionZone BuildExtractionZone()
        {
            var go = new GameObject("ExtractionZone");
            // Just outside the curtain wall: the carry out has to be earned.
            go.transform.position = new Vector3(CellSize * 6f, 0f, 0f);

            var collider = go.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(8f, 6f, 8f);

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "Marker";
            marker.transform.SetParent(go.transform, false);
            marker.transform.localScale = new Vector3(8f, 0.1f, 8f);
            marker.GetComponent<Renderer>().sharedMaterial = MakeMaterial("ExtractionMaterial",
                new Color(0.2f, 0.65f, 0.35f));
            Object.DestroyImmediate(marker.GetComponent<BoxCollider>());

            return go.AddComponent<ExtractionZone>();
        }

        private static ProceduralCastleGenerator BuildGenerator(CastleRoomRegistry registry)
        {
            var go = new GameObject("CastleGenerator");
            var generator = go.AddComponent<ProceduralCastleGenerator>();
            generator.Registry = registry;
            return generator;
        }

        private static LootSpawner BuildLootSpawner(RaidLootTable table)
        {
            var go = new GameObject("LootSpawner");
            var spawner = go.AddComponent<LootSpawner>();
            spawner.Table = table;

            // Aurum Voco needs somewhere for its coin to land, or the spell conjures nothing. The
            // coin borrows the goblet's mesh; LootSpawner.SpawnLoose overwrites the pickup's data,
            // so it carries the coin's worth rather than the goblet's.
            var gold = LoadOrCreateConjuredCoin();
            var body = AssetDatabase.LoadAssetAtPath<GameObject>(ConjuredCoinPrefabPath);
            if (body == null)
                Debug.LogError($"Plunderspell: no prefab at {ConjuredCoinPrefabPath}; Aurum Voco will conjure nothing.");

            go.AddComponent<ConjuredGoldSpawner>().Configure(gold, body);
            return spawner;
        }

        private static void BuildLockdown(AlarmFSMManager alarm)
        {
            var go = new GameObject("CastleLockdown");
            go.AddComponent<CastleLockdown>().Configure(alarm);
        }

        private static GuardSpawner BuildGuardSpawner(EnemyRoster roster)
        {
            var go = new GameObject("GuardSpawner");
            var spawner = go.AddComponent<GuardSpawner>();
            spawner.Roster = roster;
            return spawner;
        }

        /// <summary>
        /// The surface the garrison walks. Guards move through a <see cref="NavMeshAgent"/>, so
        /// without a baked NavMesh every enemy stands still wherever it spawned — which looks like
        /// broken AI rather than missing navigation data.
        /// </summary>
        private static CastleNavMeshBaker BuildNavigation()
        {
            var go = new GameObject("Navigation");
            var surface = go.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            // The castle rooms carry MeshColliders, not readable meshes, so collect from physics.
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            return go.AddComponent<CastleNavMeshBaker>();
        }

        private static RaidDirector BuildDirector(ProceduralCastleGenerator generator,
            LootSpawner lootSpawner, GuardSpawner guardSpawner, ExtractionZone extraction,
            LairHubManager lair, AlarmFSMManager alarm, CastleNavMeshBaker navigation,
            Transform playerRoot)
        {
            var go = new GameObject("RaidDirector");
            var director = go.AddComponent<RaidDirector>();
            director.Configure(generator, lootSpawner, extraction, lair, alarm, null, guardSpawner,
                navigation, playerRoot);

            var bootstrapper = go.AddComponent<RaidBootstrapper>();
            // The raid starts when the player sets out from the lair, not on scene load.
            bootstrapper.Configure(autoStart: false, era: HistoricalEra.HighMedieval);

            extraction.SetRaidDuration(RaidSeconds);
            return director;
        }

        // --- Player -------------------------------------------------------------------------

        /// <summary>
        /// Where the raid starts in the authored scene: standing just inside the gatehouse of the
        /// castle the default seed produces. The director re-derives this at raid start from the
        /// seed actually rolled, so this only has to be right for the scene as saved.
        ///
        /// See docs/systems/scale.md ("Spawning").
        /// </summary>
        private static Vector3 ResolveSpawn(ProceduralCastleGenerator generator)
        {
            ProceduralCastleData layout = generator.Generate(generator.defaultSeed);

            // The rooms were instantiated a moment ago; without this their colliders are still at
            // their old transforms and every overlap probe below reports clear.
            Physics.SyncTransforms();

            Vector3 spawn = CastleSpawnResolver.ResolveSpawn(layout);
            generator.ClearGenerated();
            return spawn;
        }

        /// <summary>
        /// Instances the authored raid player. See docs/systems/raid-scene-assembly.md, "Authored,
        /// not generated", for why this is an instance rather than a rig assembled here.
        /// </summary>
        private static GameObject BuildPlayer(Vector3 spawn)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RaidPlayerPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[RaidScaffold] No raid player at {RaidPlayerPrefabPath}. Run " +
                               "Tools > Plunderspell > Extract Raid Player Prefab first.");
                return BuildFallbackPlayer(spawn);
            }

            var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            root.transform.position = spawn;
            return root;
        }

        /// <summary>
        /// A body good enough to keep the scaffold openable when the authored prefab is missing. It
        /// carries the shipping controller, not <c>FreeLookPlaytestController</c>: building the
        /// harness here is what let the scaffold and the authored scene disagree about what a player
        /// even is. See docs/Decisions.md, 2026-09-18.
        /// </summary>
        private static GameObject BuildFallbackPlayer(Vector3 spawn)
        {
            var root = new GameObject("Player");
            root.tag = "Player";
            root.transform.position = spawn;

            var collider = root.AddComponent<CapsuleCollider>();
            collider.height = CastleSpawnResolver.PlayerHeight;
            collider.radius = CastleSpawnResolver.PlayerRadius;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = new Vector3(
                CastleSpawnResolver.PlayerRadius * 2f,
                CastleSpawnResolver.PlayerHeight * 0.5f,
                CastleSpawnResolver.PlayerRadius * 2f);
            Object.DestroyImmediate(visual.GetComponent<CapsuleCollider>());
            visual.GetComponent<Renderer>().sharedMaterial = MakeMaterial("PlayerMaterial",
                new Color(0.25f, 0.45f, 0.75f));

            // PlayerStateMachine pulls in Rigidbody through its [RequireComponent].
            PlayerStateMachine stateMachine = root.AddComponent<PlayerStateMachine>();

            var eye = new GameObject("Eye");
            eye.transform.SetParent(root.transform, false);
            eye.transform.localPosition = new Vector3(0f, EyeHeight - CastleSpawnResolver.PlayerHeight * 0.5f, 0f);
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
            root.AddComponent<PushToCastController>();
            SpellCastingSystem casting = root.AddComponent<SpellCastingSystem>();
            casting.SetLexicon(LoadLexicon());

            LootInteractor interactor = root.AddComponent<LootInteractor>();
            interactor.SetEye(eye.transform);

            // A runtime tag, not a build-time RegisterIntruder call: the static list a builder
            // populates at edit time is empty by the time anyone presses Play.
            root.AddComponent<IntruderTag>();
            return root;
        }

        private static void BuildHud(RaidDirector director, ExtractionZone extraction,
            AlarmFSMManager alarm, LairHubManager lair, LootInteractor interactor)
        {
            var go = new GameObject("RaidHud");
            var presenter = go.AddComponent<RaidHudPresenter>();
            presenter.Configure(director, extraction, alarm, lair, interactor);
            go.AddComponent<RaidHudView>();
        }

        /// <summary>
        /// The authored spellbook. Without it every phrase resolves to None and casting does nothing,
        /// which would make the built scene look like the voice pipeline is broken when it is only
        /// unwired.
        /// </summary>
        private static SpellLexicon LoadLexicon()
        {
            const string path = "Assets/_Project/Data/Spells/SpellLexicon.asset";
            var lexicon = AssetDatabase.LoadAssetAtPath<SpellLexicon>(path);
            if (lexicon == null)
                Debug.LogWarning($"Plunderspell: no SpellLexicon at {path}; casting will fizzle.");
            return lexicon;
        }

        // --- Assets -------------------------------------------------------------------------

        private static Material MakeMaterial(string name, Color colour)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { name = name, color = colour };
            return SaveAsset(material, $"{DataDirectory}/{name}.mat");
        }

        /// <summary>
        /// The Aurum Voco coin, created once and reused. Re-running the builder must not mint a
        /// second coin asset — that is what left "Loot_Conjured_Coin 1.asset" behind.
        /// </summary>
        private static LootItem LoadOrCreateConjuredCoin()
        {
            var existing = AssetDatabase.LoadAssetAtPath<LootItem>(ConjuredCoinItemPath);
            if (existing != null)
                return existing;

            var gold = ScriptableObject.CreateInstance<LootItem>();
            gold.DisplayName = "Conjured Coin";
            gold.Worth = 40f;
            gold.Bulk = 1f;
            gold.Fragility = 999f;   // coin does not shatter
            return SaveAsset(gold, ConjuredCoinItemPath);
        }

        /// <summary>
        /// Writes an asset to exactly <paramref name="path"/>, replacing what is there. Using
        /// GenerateUniqueAssetPath here is what produced the " 1" duplicates: every re-run made a new
        /// copy, and the scene kept pointing at the original.
        /// </summary>
        private static T SaveAsset<T>(T asset, string path) where T : Object
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
                AssetDatabase.DeleteAsset(path);

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (Directory.Exists(path))
                return;
            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }
    }
}
