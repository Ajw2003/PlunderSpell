using System.IO;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Builds the Lair room prefab: the cellar and every prop, placed as in the Blender reference
    /// (<c>Tools/AssetPipeline/render_lair_scene.py</c>, PLACEMENTS), plus the hearth, candle and portal
    /// lights, colliders and one spawn point per player. Rebuilt from scratch every run.
    /// Placements are in Blender's coordinates; <see cref="BlenderPlacement"/> converts them.
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
                spawn.transform.localPosition = BlenderPlacement.ToUnity(new Vector3(-5.2f, -1.5f + i * 1.0f, Floor));
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

            // The book on the table is its own object, so looking at it opens the ledger too.
            root.transform.Find("LairLedgerTable").gameObject.AddComponent<Plunderspell.Raid.LairLedgerHandle>();
            root.transform.Find("LairLedger").gameObject.AddComponent<Plunderspell.Raid.LairLedgerHandle>();
        }

        private static void Place(GameObject root, string model, Vector3 blenderPosition, Vector3 blenderDegrees,
            bool withCollider = true) =>
            BlenderPlacement.PlaceModel(root.transform, $"{ModelDirectory}/{model}.fbx", blenderPosition, blenderDegrees,
                withCollider);

        private static void AddLight(GameObject root, string name, Vector3 blenderPosition, Color color,
            float intensity, float range) =>
            BlenderPlacement.AddPointLight(root.transform, name, blenderPosition, color, intensity, range);
    }
}
