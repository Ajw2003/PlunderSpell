using System.Collections.Generic;
using System.IO;
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
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RogueAi.EditorTools
{
    /// <summary>
    /// Builds a complete, playable raid scene from code: castle, loot, garrison, extraction, HUD and
    /// a player who can walk, grab and cast.
    ///
    /// It exists because everything Plunderspell needs to be playable is a script, and none of it was
    /// wired into a scene — the systems worked and there was nothing to press play on. Building the
    /// scene in code rather than authoring it by hand keeps that wiring reviewable in a diff, and
    /// makes it reproducible: re-running always starts from an empty scene, so it can never leave a
    /// second castle behind.
    ///
    /// Everything it generates is placeholder: primitives, flat colours, one room shape. It is a
    /// harness for playing the game, not the art pass.
    /// </summary>
    public static class RaidSceneBuilder
    {
        // Tune here rather than hunting through the generated scene.
        private const float CellSize = 12f;
        private const float RoomSize = 11.5f;      // slightly under the cell so rooms read as separate
        private const float WallHeight = 4f;
        private const float WallThickness = 0.4f;
        private const float DoorwayWidth = 3.5f;
        private const float PlayerHeight = 2f;
        private const float PlayerRadius = 0.4f;
        private const float EyeHeight = 1.6f;
        private const float RaidSeconds = 300f;

        private const string SceneDirectory = "Assets/_Project/Scenes";
        private const string DataDirectory = "Assets/_Project/Data/Generated";
        private const string PrefabDirectory = "Assets/_Project/Prefabs/Generated";
        private const string ScenePath = SceneDirectory + "/RaidScene.unity";

        [MenuItem("Tools/Plunderspell/Build Playable Raid Scene")]
        public static void BuildRaidScene()
        {
            EnsureFolder(SceneDirectory);
            EnsureFolder(DataDirectory);
            EnsureFolder(PrefabDirectory);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildLight();
            BuildGround();

            GameObject roomPrefab = BuildRoomPrefab();
            CastleRoomRegistry registry = BuildRegistry(roomPrefab);
            RaidLootTable lootTable = BuildLootTable();
            GameObject guardPrefab = BuildGuardPrefab();

            AlarmFSMManager alarm = BuildAlarm();
            BuildLockdown(alarm);
            LairHubManager lair = BuildLair();
            ExtractionZone extraction = BuildExtractionZone();
            ProceduralCastleGenerator generator = BuildGenerator(registry);
            LootSpawner lootSpawner = BuildLootSpawner(lootTable);
            GuardSpawner guardSpawner = BuildGuardSpawner(guardPrefab);

            RaidDirector director = BuildDirector(generator, lootSpawner, guardSpawner, extraction,
                lair, alarm);

            GameObject player = BuildPlayer();
            BuildHud(director, extraction, alarm, lair, player.GetComponentInChildren<LootInteractor>());

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log($"Plunderspell: playable raid scene built at {ScenePath}. " +
                      "Press Play; WASD to move, E to take, Q to drop, V to cast, F5 to extract.");
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

        // --- Castle -------------------------------------------------------------------------

        /// <summary>
        /// One generic room: a floor and four walls, each wall split either side of a doorway gap so
        /// adjacent rooms connect. Every zone uses it — the generator's job is layout, and a single
        /// shape is enough to walk the layout and find out whether it plays.
        /// </summary>
        private static GameObject BuildRoomPrefab()
        {
            var root = new GameObject("CastleRoom_Generic");
            var module = root.AddComponent<CastleRoomModule>();
            module.RoomId = "GenericRoom";

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(root.transform, false);
            floor.transform.localScale = new Vector3(RoomSize, 0.2f, RoomSize);
            floor.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            floor.GetComponent<Renderer>().sharedMaterial = MakeMaterial("FloorMaterial",
                new Color(0.32f, 0.30f, 0.27f));

            Material wallMaterial = MakeMaterial("WallMaterial", new Color(0.42f, 0.41f, 0.38f));
            BuildWallWithDoorway(root.transform, new Vector3(0f, 0f, RoomSize * 0.5f), 0f, wallMaterial);
            BuildWallWithDoorway(root.transform, new Vector3(0f, 0f, -RoomSize * 0.5f), 180f, wallMaterial);
            BuildWallWithDoorway(root.transform, new Vector3(RoomSize * 0.5f, 0f, 0f), 90f, wallMaterial);
            BuildWallWithDoorway(root.transform, new Vector3(-RoomSize * 0.5f, 0f, 0f), 270f, wallMaterial);

            // A socket per side, so the room is legible to the socket system even though the
            // generator currently places by grid cell rather than by socket matching.
            AddSocket(root.transform, new Vector3(0f, 0f, RoomSize * 0.5f), Direction.North);
            AddSocket(root.transform, new Vector3(0f, 0f, -RoomSize * 0.5f), Direction.South);
            AddSocket(root.transform, new Vector3(RoomSize * 0.5f, 0f, 0f), Direction.East);
            AddSocket(root.transform, new Vector3(-RoomSize * 0.5f, 0f, 0f), Direction.West);

            module.PopulateSockets();

            string path = $"{PrefabDirectory}/CastleRoom_Generic.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>A wall in two halves with a gap between them, so players can walk through.</summary>
        private static void BuildWallWithDoorway(Transform parent, Vector3 position, float yaw,
            Material material)
        {
            float segmentWidth = (RoomSize - DoorwayWidth) * 0.5f;
            float offset = (DoorwayWidth + segmentWidth) * 0.5f;

            var wall = new GameObject("Wall");
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = position;
            wall.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            for (int side = -1; side <= 1; side += 2)
            {
                GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                segment.name = side < 0 ? "SegmentLeft" : "SegmentRight";
                segment.transform.SetParent(wall.transform, false);
                segment.transform.localPosition = new Vector3(offset * side, WallHeight * 0.5f, 0f);
                segment.transform.localScale = new Vector3(segmentWidth, WallHeight, WallThickness);
                segment.GetComponent<Renderer>().sharedMaterial = material;
            }
        }

        private static void AddSocket(Transform parent, Vector3 position, Direction facing)
        {
            var go = new GameObject($"Socket_{facing}");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            SocketPoint socket = go.AddComponent<SocketPoint>();
            socket.Type = SocketType.Door;
            socket.Facing = facing;
        }

        private static CastleRoomRegistry BuildRegistry(GameObject roomPrefab)
        {
            var registry = ScriptableObject.CreateInstance<CastleRoomRegistry>();

            // Every zone draws from the same shape for now; the crypt entry keeps the id the
            // generator looks for by name.
            registry.Modules.Add(new CastleRoomModuleData
            {
                RoomId = "CryptChamberFinal",
                Zone = CastleZone.Crypt,
                Prefab = roomPrefab,
                Weight = 1
            });

            foreach (CastleZone zone in System.Enum.GetValues(typeof(CastleZone)))
            {
                registry.Modules.Add(new CastleRoomModuleData
                {
                    RoomId = $"{zone}_Room",
                    Zone = zone,
                    Prefab = roomPrefab,
                    Weight = 10
                });
            }

            return SaveAsset(registry, $"{DataDirectory}/GeneratedRoomRegistry.asset");
        }

        // --- Loot ---------------------------------------------------------------------------

        /// <summary>
        /// A placeholder haul that still teaches the game's two lessons: fragile things break, and
        /// heavy things need a second pair of hands.
        /// </summary>
        private static RaidLootTable BuildLootTable()
        {
            var table = ScriptableObject.CreateInstance<RaidLootTable>();
            GameObject lootPrefab = BuildLootPrefab();

            Add(table, lootPrefab, "Tin Cup", CastleZone.CurtainWall, worth: 15f, bulk: 1f, fragility: 30f);
            Add(table, lootPrefab, "Silver Plate", CastleZone.OuterBailey, worth: 45f, bulk: 2f, fragility: 14f);
            Add(table, lootPrefab, "Glass Reliquary", CastleZone.InnerWard, worth: 120f, bulk: 3f, fragility: 4f);
            Add(table, lootPrefab, "Gilded Chest", CastleZone.Keep, worth: 260f, bulk: 14f, fragility: 20f);
            Add(table, lootPrefab, "Crown of the Founder", CastleZone.Crypt, worth: 650f, bulk: 12f, fragility: 6f);

            return SaveAsset(table, $"{DataDirectory}/GeneratedLootTable.asset");
        }

        private static void Add(RaidLootTable table, GameObject prefab, string name, CastleZone zone,
            float worth, float bulk, float fragility)
        {
            var item = ScriptableObject.CreateInstance<LootItem>();
            item.DisplayName = name;
            item.Worth = worth;
            item.Bulk = bulk;
            item.Fragility = fragility;
            item.IsArtifact = worth >= 500f;
            SaveAsset(item, $"{DataDirectory}/Loot_{name.Replace(' ', '_')}.asset");

            table.Entries.Add(new RaidLootTable.Entry
            {
                Item = item,
                Zone = zone,
                Weight = 10,
                Prefab = prefab
            });
        }

        private static GameObject BuildLootPrefab()
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = "LootPickup";
            root.transform.localScale = Vector3.one * 0.6f;
            root.GetComponent<Renderer>().sharedMaterial = MakeMaterial("LootMaterial",
                new Color(0.85f, 0.72f, 0.25f));

            Rigidbody body = root.AddComponent<Rigidbody>();
            body.mass = 5f;
            root.AddComponent<LootPickup>();
            root.AddComponent<AcousticEmitter>();

            string path = $"{PrefabDirectory}/LootPickup.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // --- Guards -------------------------------------------------------------------------

        private static GameObject BuildGuardPrefab()
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            root.name = "CastleGuard";
            root.GetComponent<Renderer>().sharedMaterial = MakeMaterial("GuardMaterial",
                new Color(0.55f, 0.18f, 0.18f));

            root.AddComponent<StatusEffectReceiver>();
            root.AddComponent<CastleGuard>();

            string path = $"{PrefabDirectory}/CastleGuard.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
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

            // Aurum Voco needs somewhere for its coin to land, or the spell conjures nothing.
            var gold = ScriptableObject.CreateInstance<LootItem>();
            gold.DisplayName = "Conjured Coin";
            gold.Worth = 40f;
            gold.Bulk = 1f;
            gold.Fragility = 999f;   // coin does not shatter
            SaveAsset(gold, $"{DataDirectory}/Loot_Conjured_Coin.asset");

            go.AddComponent<ConjuredGoldSpawner>().Configure(gold, BuildLootPrefab());
            return spawner;
        }

        private static void BuildLockdown(AlarmFSMManager alarm)
        {
            var go = new GameObject("CastleLockdown");
            go.AddComponent<CastleLockdown>().Configure(alarm);
        }

        private static GuardSpawner BuildGuardSpawner(GameObject guardPrefab)
        {
            var go = new GameObject("GuardSpawner");
            var spawner = go.AddComponent<GuardSpawner>();
            spawner.GuardPrefab = guardPrefab;
            return spawner;
        }

        private static RaidDirector BuildDirector(ProceduralCastleGenerator generator,
            LootSpawner lootSpawner, GuardSpawner guardSpawner, ExtractionZone extraction,
            LairHubManager lair, AlarmFSMManager alarm)
        {
            var go = new GameObject("RaidDirector");
            var director = go.AddComponent<RaidDirector>();
            director.Configure(generator, lootSpawner, extraction, lair, alarm, null, guardSpawner);

            var bootstrapper = go.AddComponent<RaidBootstrapper>();
            bootstrapper.Configure(autoStart: true, era: HistoricalEra.HighMedieval);

            extraction.SetRaidDuration(RaidSeconds);
            return director;
        }

        // --- Player -------------------------------------------------------------------------

        private static GameObject BuildPlayer()
        {
            var root = new GameObject("Player");
            root.transform.position = new Vector3(CellSize * 5f, PlayerHeight * 0.5f, 0f);

            var body = root.AddComponent<Rigidbody>();
            body.freezeRotation = true;
            body.mass = 70f;

            var collider = root.AddComponent<CapsuleCollider>();
            collider.height = PlayerHeight;
            collider.radius = PlayerRadius;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);
            Object.DestroyImmediate(visual.GetComponent<CapsuleCollider>());
            visual.GetComponent<Renderer>().sharedMaterial = MakeMaterial("PlayerMaterial",
                new Color(0.25f, 0.45f, 0.75f));

            var eye = new GameObject("Eye");
            eye.transform.SetParent(root.transform, false);
            eye.transform.localPosition = new Vector3(0f, EyeHeight, 0f);
            Camera camera = eye.AddComponent<Camera>();
            camera.tag = "MainCamera";
            eye.AddComponent<AudioListener>();

            var hand = new GameObject("HandSocket");
            hand.transform.SetParent(root.transform, false);
            hand.transform.localPosition = new Vector3(0.4f, 1.2f, 0.6f);

            root.AddComponent<StatusEffectReceiver>();
            root.AddComponent<AcousticEmitter>();
            root.AddComponent<FootstepNoiseEmitter>();
            root.AddComponent<PushToCastController>();
            SpellCastingSystem casting = root.AddComponent<SpellCastingSystem>();
            casting.SetLexicon(LoadLexicon());
            root.AddComponent<FreeLookPlaytestController>();

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

        private static T SaveAsset<T>(T asset, string path) where T : Object
        {
            AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(path));
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
