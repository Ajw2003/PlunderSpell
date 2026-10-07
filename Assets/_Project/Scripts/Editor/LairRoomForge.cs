using System.IO;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Builds the Lair room prefab: the cellar and every prop, placed as in the Blender reference
    /// (<c>Tools/AssetPipeline/render_lair_scene.py</c>, PLACEMENTS), plus the hearth, candle and portal
    /// lights, colliders and one spawn point per player. Rebuilt from scratch every run.
    ///
    /// Placements are written in Blender's Z-up coordinates and converted with
    /// Unity(X, Y, Z) = Blender(-X, Z, -Y): measured on the imported cellar, whose hearth (Blender x +3.5)
    /// lands at Unity x -3.5. Tools/AssetPipeline/README.md's "X passes straight through" does not hold here.
    /// </summary>
    public static class LairRoomForge
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/Lair/LairRoom.prefab";
        private const string ModelDirectory = "Assets/_Project/Art/Models/Lair";

        /// <summary>Walkable floor height above the cellar's origin (the 0.30 m slab).</summary>
        private const float Floor = 0.30f;
        private static readonly Vector2 Table = new Vector2(-0.8f, -1.0f);
        private static readonly Vector2 Dial = new Vector2(2.2f, -0.2f);

        [MenuItem("Tools/Plunderspell/Build Lair Room")]
        public static void BuildLairRoom()
        {
            var root = new GameObject("LairRoom");

            // (model, Blender location, Blender rotation in degrees), as in render_lair_scene.py.
            Place(root, "LairCellar", new Vector3(0f, 0f, 0f), Vector3.zero);
            Place(root, "LairPortalArch", new Vector3(-7.2f, 0f, Floor), new Vector3(0f, 0f, 90f));
            Place(root, "LairLedgerTable", new Vector3(Table.x, Table.y, Floor), Vector3.zero);
            Place(root, "LairLedger", new Vector3(Table.x, Table.y, Floor + 0.80f), Vector3.zero);
            Place(root, "LairCandle", new Vector3(Table.x + 1.0f, Table.y + 0.25f, Floor + 0.80f), Vector3.zero);
            Place(root, "LairCenturyDialStand", new Vector3(Dial.x, Dial.y, Floor), Vector3.zero);
            Place(root, "LairWeaponRack", new Vector3(-4.2f, 4.7f, Floor), Vector3.zero);
            foreach (float dx in new[] { -1.05f, -0.35f, 0.35f, 1.05f })
                Place(root, "LairStrongbox", new Vector3(Table.x + dx, Table.y - 0.62f, Floor), Vector3.zero);

            // The dial's rings turn, so they get no collider and sit inside the stand's own reach.
            var ringTilts = new[]
            {
                new Vector3(15f, 0f, 0f), new Vector3(-30f, 0f, 20f),
                new Vector3(0f, 35f, 60f), new Vector3(25f, -20f, 110f),
            };
            for (int n = 0; n < ringTilts.Length; n++)
                Place(root, $"LairCenturyDialRing{n + 1}", new Vector3(Dial.x, Dial.y, Floor + 1.18f), ringTilts[n],
                    withCollider: false);

            // One fire, one candle, a breath of portal light. Intensities are tuned by eye in Unity,
            // not converted from the Blender render's watts.
            AddLight(root, "HearthLight", new Vector3(3.5f, 4.35f, Floor + 0.8f), new Color(1.0f, 0.45f, 0.15f), 6f, 12f);
            AddLight(root, "CandleLight", new Vector3(Table.x + 1.0f, Table.y + 0.25f, Floor + 1.05f),
                new Color(1.0f, 0.75f, 0.4f), 1.2f, 4f);
            AddLight(root, "PortalLight", new Vector3(-6.6f, 0f, Floor + 1.6f), new Color(0.48f, 0.42f, 0.63f), 1.5f, 6f);

            // Four players stand in a row in front of the portal arch, facing into the room (Unity -X).
            var spawns = new GameObject("PlayerSpawns");
            spawns.transform.SetParent(root.transform, false);
            for (int i = 0; i < 4; i++)
            {
                var spawn = new GameObject($"Spawn{i + 1}");
                spawn.transform.SetParent(spawns.transform, false);
                spawn.transform.localPosition = ToUnity(new Vector3(-5.2f, -1.5f + i * 1.0f, Floor));
                spawn.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            }

            AddBehaviours(root);

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[Lair] Built {PrefabPath}.");
        }

        /// <summary>The room's behaviour: the spawner, a portal trigger in front of the arch, the ledger handle.</summary>
        private static void AddBehaviours(GameObject root)
        {
            root.AddComponent<Plunderspell.Raid.LairRoomSpawner>();

            // Unity x 6.0..7.2 in front of the arch stones (x 7.2); the spawns at x 5.2 stand clear of it.
            var portal = new GameObject("LairPortalTrigger");
            portal.transform.SetParent(root.transform, false);
            portal.transform.localPosition = new Vector3(6.6f, Floor + 1.2f, 0f);
            var box = portal.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(1.2f, 2.4f, 2.4f);
            portal.AddComponent<Plunderspell.Raid.LairPortalTrigger>();

            root.transform.Find("LairLedgerTable").gameObject.AddComponent<Plunderspell.Raid.LairLedgerHandle>();
        }

        /// <summary>RaidScene is authored, not built, so the room is placed into it here. 1000 m east of the origin:
        /// the castle's curtain wall reaches 3 cells of 12 m (about 45 m) round the origin.</summary>
        [MenuItem("Tools/Plunderspell/Place Lair Room In Raid Scene")]
        public static void PlaceInRaidScene()
        {
            const string scenePath = "Assets/_Project/Scenes/RaidScene.unity";
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
            foreach (GameObject existing in scene.GetRootGameObjects())
                if (existing.name == "LairRoom")
                    Object.DestroyImmediate(existing);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.transform.position = new Vector3(1000f, 0f, 0f);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Lair] Placed LairRoom at {instance.transform.position} in {scenePath}.");
        }

        private static void Place(GameObject root, string model, Vector3 blenderPosition, Vector3 blenderDegrees,
            bool withCollider = true)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelDirectory}/{model}.fbx");
            if (asset == null)
            {
                Debug.LogError($"[Lair] No model at {ModelDirectory}/{model}.fbx; the Lair room is missing it.");
                return;
            }

            // A parent carries the placement, so the model keeps whatever root rotation its import gave it.
            var slot = new GameObject(model);
            slot.transform.SetParent(root.transform, false);
            slot.transform.localPosition = ToUnity(blenderPosition);
            slot.transform.localRotation = ToUnityRotation(blenderDegrees);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, slot.transform);
            if (!withCollider)
                return;
            foreach (MeshFilter filter in instance.GetComponentsInChildren<MeshFilter>())
                filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
        }

        private static void AddLight(GameObject root, string name, Vector3 blenderPosition, Color color,
            float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = ToUnity(blenderPosition);
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.Soft;
        }

        public static Vector3 ToUnity(Vector3 blender) => new Vector3(-blender.x, blender.z, -blender.y);

        /// <summary>
        /// Blender XYZ Euler (applied X, then Y, then Z) to a Unity rotation. The axis map is a mirror,
        /// so each turn keeps its angle about the mapped axis but reverses sense: X stays X,
        /// Y becomes Unity Z, and a turn about Blender Z is the opposite turn about Unity Y.
        /// </summary>
        public static Quaternion ToUnityRotation(Vector3 blenderDegrees) =>
            Quaternion.AngleAxis(-blenderDegrees.z, Vector3.up)
            * Quaternion.AngleAxis(blenderDegrees.y, Vector3.forward)
            * Quaternion.AngleAxis(blenderDegrees.x, Vector3.right);
    }
}
