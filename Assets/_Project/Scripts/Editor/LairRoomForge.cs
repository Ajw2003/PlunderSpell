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
            var boxOffsets = new[] { -1.05f, -0.35f, 0.35f, 1.05f };
            for (int seat = 0; seat < boxOffsets.Length; seat++)
                AddStrongboxLid(Place(root, "LairStrongbox", new Vector3(Table.x + boxOffsets[seat], Table.y - 0.62f, Floor), Vector3.zero), seat);

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

            // Where the last raid's haul lands: on the floor 1.2 m in front of the arriving players (#310).
            var landing = new GameObject("HaulLanding");
            landing.transform.SetParent(root.transform, false);
            landing.transform.localPosition = new Vector3(4.0f, Floor, 0f);
            var pile = landing.AddComponent<Plunderspell.Raid.LootSpawner>();
            var haul = landing.AddComponent<Plunderspell.Raid.HaulLanding>();
            var haulObject = new SerializedObject(haul);
            haulObject.FindProperty("_pile").objectReferenceValue = pile;
            haulObject.ApplyModifiedPropertiesWithoutUndo();

            // The book on the table is its own object, so looking at it opens the ledger too.
            root.transform.Find("LairLedgerTable").gameObject.AddComponent<Plunderspell.Raid.LairLedgerHandle>();
            root.transform.Find("LairLedger").gameObject.AddComponent<Plunderspell.Raid.LairLedgerHandle>();

            AddMarketDoor(root);
            AddLedgerPages(root);
            AddCenturyDial(root);
        }

        private const string PlaqueMaterialPath = "Assets/_Project/Prefabs/Lair/LairPlaqueBrass.mat";
        private static readonly Vector3 PlaqueSize = new Vector3(0.03f, 0.40f, 0.62f);

        /// <summary>
        /// The dial's behaviour (<see cref="Plunderspell.Raid.LairCenturyDial"/>, #358) on the stand, and a brass plaque on the
        /// stand's face toward the portal (+X, where the players arrive) carrying the chosen Age's name, date and blurb in
        /// the ledger's Spectral TMP font. The text is scaled 0.01 like the ledger pages: its rect is in centimetres.
        /// </summary>
        private static void AddCenturyDial(GameObject root)
        {
            Transform stand = root.transform.Find("LairCenturyDialStand");
            Bounds bounds = stand.GetComponentInChildren<Renderer>().bounds;
            foreach (Renderer part in stand.GetComponentsInChildren<Renderer>())
                bounds.Encapsulate(part.bounds);

            var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "DialPlaque";
            Object.DestroyImmediate(plate.GetComponent<BoxCollider>()); // text to read, not a thing to bump into
            plate.transform.SetParent(root.transform, false);
            plate.transform.position = new Vector3(bounds.max.x + PlaqueSize.x * 0.5f, Floor + 0.95f, stand.position.z);
            plate.transform.localScale = PlaqueSize;
            plate.GetComponent<Renderer>().sharedMaterial = BrassMaterial();

            var words = new GameObject("PlaqueText");
            words.transform.SetParent(root.transform, false); // not under the squashed plate, which would shear it
            words.transform.position = plate.transform.position + Vector3.right * (PlaqueSize.x * 0.5f + 0.002f);
            words.transform.rotation = Quaternion.LookRotation(Vector3.left);
            words.transform.localScale = Vector3.one * 0.01f;
            var text = words.AddComponent<TMPro.TextMeshPro>();
            text.font = LedgerFont();
            text.fontSize = 32f;
            text.color = new Color(0.10f, 0.06f, 0.02f);
            text.alignment = TMPro.TextAlignmentOptions.Top;
            text.textWrappingMode = TMPro.TextWrappingModes.Normal;
            text.richText = true;
            text.margin = new Vector4(2f, 3f, 2f, 3f);
            text.rectTransform.sizeDelta = new Vector2(PlaqueSize.z, PlaqueSize.y) * 100f;

            var rings = new Transform[4];
            for (int n = 0; n < rings.Length; n++)
                rings[n] = root.transform.Find($"LairCenturyDialRing{n + 1}");
            var dial = stand.gameObject.AddComponent<Plunderspell.Raid.LairCenturyDial>();
            var dialObject = new SerializedObject(dial);
            dialObject.FindProperty("_plaque").objectReferenceValue = text;
            SerializedProperty ringList = dialObject.FindProperty("_rings");
            ringList.arraySize = rings.Length;
            for (int n = 0; n < rings.Length; n++)
                ringList.GetArrayElementAtIndex(n).objectReferenceValue = rings[n];
            dialObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material BrassMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(PlaqueMaterialPath);
            if (existing != null)
                return existing;
            var brass = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "LairPlaqueBrass" };
            brass.SetColor("_BaseColor", new Color(0.78f, 0.58f, 0.24f));
            brass.SetFloat("_Metallic", 0.85f);
            brass.SetFloat("_Smoothness", 0.55f);
            AssetDatabase.CreateAsset(brass, PlaqueMaterialPath);
            return brass;
        }

        private const string LedgerFontPath = "Assets/_Project/Resources/UI/Fonts/Spectral-Regular SDF.asset";

        // Blender's ledger (lair_builders.py build_lair_ledger): two vellum pages 0.33 x 0.47 m, centred 0.17 m either side of
        // the book's middle, whose tops lie in the book's bounds. The model is turned 180 degrees about the vertical against
        // Blender, so the spine runs across the reader standing on the strongbox side (+Z), and the pages' text turns to face them.
        private const float PageOffset = 0.17f;
        private static readonly Vector2 PageSize = new Vector2(0.30f, 0.43f);
        private const float PageInk = 0.003f;

        /// <summary>The open book's two pages as 3D text (<see cref="Plunderspell.Raid.LairLedgerBook"/>), in dark ink on the vellum (#357).</summary>
        private static void AddLedgerPages(GameObject root)
        {
            Transform book = root.transform.Find("LairLedger");
            float top = book.GetComponentInChildren<Renderer>().bounds.max.y + PageInk;
            TMPro.TMP_FontAsset font = LedgerFont();

            var pages = new GameObject("LedgerPages");
            pages.transform.SetParent(root.transform, false);
            var ledgerBook = pages.AddComponent<Plunderspell.Raid.LairLedgerBook>();
            var bookObject = new SerializedObject(ledgerBook);
            bookObject.FindProperty("_leftPage").objectReferenceValue = AddPage(pages, "LeftPage", font, new Vector3(book.position.x + PageOffset, top, book.position.z));
            bookObject.FindProperty("_rightPage").objectReferenceValue = AddPage(pages, "RightPage", font, new Vector3(book.position.x - PageOffset, top, book.position.z));
            bookObject.ApplyModifiedPropertiesWithoutUndo();
        }

        // Scaled 0.01 so the rect is in centimetres and the font size reads in about centimetres of em.
        private static TMPro.TextMeshPro AddPage(GameObject pages, string name, TMPro.TMP_FontAsset font, Vector3 at)
        {
            var go = new GameObject(name);
            go.transform.SetParent(pages.transform, false);
            go.transform.position = at;
            go.transform.rotation = Quaternion.Euler(90f, 180f, 0f);
            go.transform.localScale = Vector3.one * 0.01f;
            var text = go.AddComponent<TMPro.TextMeshPro>();
            if (font != null)
                text.font = font;
            text.fontSize = 24f;
            text.color = new Color(0.08f, 0.05f, 0.03f);
            text.margin = new Vector4(1f, 5f, 1f, 0f); // below the book's printed heading bars
            text.alignment = TMPro.TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TMPro.TextWrappingModes.Normal;
            text.richText = true;
            text.rectTransform.sizeDelta = PageSize * 100f;
            return text;
        }

        /// <summary>A serif TMP font (Spectral, already the UI's body face), made once from the TTF and kept as an asset.</summary>
        private static TMPro.TMP_FontAsset LedgerFont()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(LedgerFontPath);
            if (existing != null)
                return existing;

            var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Project/Resources/UI/Fonts/Spectral-Regular.ttf");
            TMPro.TMP_FontAsset made = TMPro.TMP_FontAsset.CreateFontAsset(source, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024,
                TMPro.AtlasPopulationMode.Dynamic);
            AssetDatabase.CreateAsset(made, LedgerFontPath);
            made.material.name = "Spectral-Regular SDF Material";
            AssetDatabase.AddObjectToAsset(made.material, made);
            made.atlasTexture.name = "Spectral-Regular SDF Atlas";
            AssetDatabase.AddObjectToAsset(made.atlasTexture, made);

            // Every glyph the pages use, so playing never has to add one and dirty the asset.
            var glyphs = new System.Text.StringBuilder("·");
            for (char c = ' '; c <= '~'; c++)
                glyphs.Append(c);
            made.TryAddCharacters(glyphs.ToString());
            EditorUtility.SetDirty(made);
            AssetDatabase.SaveAssets();
            return made;
        }

        public const string MarketDoorName = "MarketDoor";
        public const string MarketDoorArrivalsName = "MarketDoorArrivals";

        /// <summary>
        /// The Market door in the back wall (Blender x -0.7, 2.6 x 2.16 m; lair_builders.py DOOR_CX): look at
        /// it and press E to go to the Market. Its leaf is part of the cellar mesh, so a thin collider of its
        /// own sits just in front of it for the look to hit. RaidSceneRooms points it at the Market's spawns.
        /// Players coming back stand at MarketDoorArrivals, just inside the door, facing into the room.
        /// </summary>
        private static void AddMarketDoor(GameObject root)
        {
            var door = new GameObject(MarketDoorName);
            door.transform.SetParent(root.transform, false);
            door.transform.localPosition = BlenderPlacement.ToUnity(new Vector3(-0.7f, 4.95f, Floor + 1.08f));
            door.AddComponent<BoxCollider>().size = new Vector3(2.6f, 2.16f, 0.1f);
            door.AddComponent<Plunderspell.Raid.RoomTravel>();

            var arrivals = new GameObject(MarketDoorArrivalsName);
            arrivals.transform.SetParent(root.transform, false);
            for (int i = 0; i < 4; i++)
            {
                var spawn = new GameObject($"Spawn{i + 1}");
                spawn.transform.SetParent(arrivals.transform, false);
                spawn.transform.localPosition = BlenderPlacement.ToUnity(new Vector3(-2.2f + i * 1.0f, 3.8f, Floor));
                spawn.transform.localRotation = Quaternion.LookRotation(BlenderPlacement.ToUnity(Vector3.down));
            }
        }

        /// <summary>
        /// The box's lid is a trigger slab over its top (a hand's breadth into the box to a third of a metre above it): a
        /// coin pouch let go in it is banked into this seat's purse (<see cref="Plunderspell.Raid.LairStrongbox"/>).
        /// The slot is unturned under an unturned root, so the model's world bounds are its bounds in the slot's axes.
        /// </summary>
        private static void AddStrongboxLid(GameObject slot, int seat)
        {
            Bounds bounds = slot.GetComponentInChildren<Renderer>().bounds;
            foreach (Renderer part in slot.GetComponentsInChildren<Renderer>())
                bounds.Encapsulate(part.bounds);

            var lid = new GameObject($"StrongboxLid{seat + 1}");
            lid.transform.SetParent(slot.transform, false);
            lid.transform.position = new Vector3(bounds.center.x, bounds.max.y + 0.1f, bounds.center.z);
            var trigger = lid.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(bounds.size.x, 0.5f, bounds.size.z);
            lid.AddComponent<Plunderspell.Raid.LairStrongbox>().SetSeat(seat);
        }

        private static GameObject Place(GameObject root, string model, Vector3 blenderPosition, Vector3 blenderDegrees,
            bool withCollider = true) =>
            BlenderPlacement.PlaceModel(root.transform, $"{ModelDirectory}/{model}.fbx", blenderPosition, blenderDegrees,
                withCollider);

        private static void AddLight(GameObject root, string name, Vector3 blenderPosition, Color color,
            float intensity, float range) =>
            BlenderPlacement.AddPointLight(root.transform, name, blenderPosition, color, intensity, range);
    }
}
