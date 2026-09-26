using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Castle
{
    /// <summary>
    /// Deterministic, seed-driven procedural castle builder.
    ///
    /// The castle is a closed curtain wall enclosing a dense block of concentric wards. The outer
    /// Chebyshev ring at <see cref="CurtainWallRadius"/> is filled completely — corners, bastions,
    /// one gatehouse and straight runs — so the wall reads as an unbroken loop. Everything inside
    /// that ring is enclosed rooms: Crypt at the origin, then Keep, InnerWard and OuterBailey
    /// outward, filled to <c>m_interiorFillFraction</c> with the remainder left as courtyards.
    ///
    /// Courtyards are only ever carved where the interior stays a single 4-connected region, which
    /// is what lets the A* validator always find (or correctly reject) a crypt→extraction path.
    ///
    /// Because the entire layout is a pure function of the seed, only the seed is replicated over
    /// the network (see <see cref="CastleNetworkManager"/>); no mesh or transform data is sent.
    /// </summary>
    public class ProceduralCastleGenerator : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private CastleRoomRegistry registry;

        [Tooltip("The bailey's furniture and the courtyards' yards. Optional: without it the curtain " +
                 "strip and the carved cells stay bare.")]
        [SerializeField] private CastleDressingSet m_dressing;
        [SerializeField] public int defaultSeed = 12345;

        [Header("Layout")]
        [Tooltip("World units between adjacent grid cells.")]
        [SerializeField] private float cellSize = 12f;

        [Tooltip("Parent for instantiated rooms. Auto-created if left null.")]
        [SerializeField] private Transform roomContainer;

        [Tooltip("Chebyshev ring the closed curtain wall sits on. Everything inside it is rooms, " +
                 "so the castle is (2 x radius + 1) cells across. 4 gives a 9x9 interior.")]
        [Range(3, 7)]
        [SerializeField] private int m_curtainWallRadius = 4;

        [Tooltip("Fraction of the interior cells that become rooms. The remainder are left open " +
                 "as courtyards, but only where the interior stays one connected region.")]
        [Range(0.5f, 1f)]
        [SerializeField] private float m_interiorFillFraction = 0.9f;

        [Tooltip("Cells between bastions along the straight runs of the curtain wall.")]
        [Range(2, 6)]
        [SerializeField] private int m_bastionSpacing = 3;

        // Module geometry the door-plug placement has to agree with, authored in
        // Tools/AssetPipeline/room_kit.py. Duplicated here rather than measured off the mesh
        // because the layout is computed for data-only (prefab-free) castles too.
        private const float k_ModuleFootprint = 12f;
        private const float k_WallThickness = 0.5f;
        private const float k_FloorThickness = 0.3f;

        /// <summary>Distance from a module's centre to the middle of one of its four walls.</summary>
        private const float k_ArchwayInset = k_ModuleFootprint / 2f - k_WallThickness / 2f;

        // Curtain-wall piece ids, matching the prefabs in the registry.
        private const string k_WallStraightId = "WallStraight";
        private const string k_WallCornerId = "WallCorner";
        private const string k_BastionId = "Bastion";
        private const string k_GatehouseId = "GatehouseModule";
        private const string k_DrawbridgeId = "Drawbridge";
        private const string k_CryptFinalId = "CryptChamberFinal";

        /// <summary>
        /// The side the one gatehouse sits on. Fixed rather than rolled so the drawbridge approach,
        /// the authored extraction zone and the player's spawn all agree for every seed.
        /// </summary>
        private static readonly Vector2Int k_GateOutward = new Vector2Int(1, 0);

        /// <summary>The most recent layout produced by <see cref="Generate"/>.</summary>
        public ProceduralCastleData LastGenerated { get; private set; }

        public CastleRoomRegistry Registry { get => registry; set => registry = value; }

        /// <summary>The dressing placed over the layout. Shared by every Age for now.</summary>
        public CastleDressingSet Dressing { get => m_dressing; set => m_dressing = value; }

        /// <summary>Chebyshev ring the closed curtain wall occupies.</summary>
        public int CurtainWallRadius => m_curtainWallRadius;

        private readonly List<GameObject> _instantiated = new List<GameObject>();

        /// <summary>
        /// Builds a castle layout deterministically from <paramref name="seed"/>. If room prefabs are
        /// assigned in the registry they are instantiated; otherwise the returned data is a pure
        /// metadata layout (used by tests and by the network path before art is wired in).
        /// </summary>
        public ProceduralCastleData Generate(int seed)
        {
            ClearGenerated();

            var rng = new System.Random(seed);
            var data = new ProceduralCastleData(seed);

            // Maps an occupied grid cell -> index into data.PlacedModules.
            var occupied = new Dictionary<Vector2Int, int>();

            BuildInterior(data, occupied, rng);
            int gatehouseIndex = BuildCurtainWall(data, occupied);
            AssignExtractionExit(data, gatehouseIndex);

            // Every enclosed room is authored with an archway on all four sides, so any side with
            // no neighbour is currently a hole in the outer face. Fill those.
            SealOpenArchways(data, occupied);

            // Placed with the rooms, before anyone bakes the NavMesh or probes for a standing point,
            // so a cart or a woodpile is walked round and never stood in.
            DressCastle(data, seed);

            LastGenerated = data;
            return data;
        }

        // --- Interior ------------------------------------------------------------

        /// <summary>
        /// Fills the cells inside the curtain wall with enclosed rooms, zoned by ring, leaving a
        /// deterministic handful open as courtyards. A cell is only left open when the remaining
        /// interior is still one 4-connected region, so the crypt can always be walked out of.
        /// </summary>
        private void BuildInterior(ProceduralCastleData data, Dictionary<Vector2Int, int> occupied,
            System.Random rng)
        {
            List<Vector2Int> interior = InteriorCells();
            var kept = new HashSet<Vector2Int>(interior);

            var courtyardOrder = new List<Vector2Int>(interior);
            Shuffle(courtyardOrder, rng);

            int courtyardTarget = Mathf.RoundToInt(interior.Count * (1f - m_interiorFillFraction));
            Vector2Int gateApproach = k_GateOutward * (m_curtainWallRadius - 1);
            int carved = 0;

            for (int i = 0; i < courtyardOrder.Count && carved < courtyardTarget; i++)
            {
                Vector2Int cell = courtyardOrder[i];

                // The crypt is the path's start and the gate approach is its end; neither can be a
                // hole without making some seeds unplayable.
                if (cell == Vector2Int.zero || cell == gateApproach)
                    continue;

                kept.Remove(cell);
                if (IsSingleConnectedRegion(kept))
                {
                    carved++;
                }
                else
                {
                    kept.Add(cell);
                }
            }

            // Place innermost-first in a fixed cell order, so placement indices (and therefore
            // CryptStartIndex) are a function of the seed alone and never of hash iteration order.
            var ordered = new List<Vector2Int>(kept);
            ordered.Sort(CompareByRingThenCell);

            for (int i = 0; i < ordered.Count; i++)
            {
                Vector2Int cell = ordered[i];
                CastleZone zone = ZoneForRing(Chebyshev(cell));
                bool isCryptCentre = cell == Vector2Int.zero;

                string roomId = isCryptCentre
                    ? ResolveRoomId(k_CryptFinalId)
                    : PickWeighted(registry != null ? registry.GetModulesForZone(zone) : null, rng, zone);

                int index = PlaceModule(data, occupied, roomId, zone, cell,
                    RotationForFacing(InwardStep(cell)), isCryptEntry: isCryptCentre);

                if (isCryptCentre)
                    data.CryptStartIndex = index;
            }
        }

        /// <summary>Every cell strictly inside the curtain wall ring.</summary>
        private List<Vector2Int> InteriorCells()
        {
            int limit = m_curtainWallRadius - 1;
            var cells = new List<Vector2Int>();
            for (int x = -limit; x <= limit; x++)
            {
                for (int y = -limit; y <= limit; y++)
                {
                    cells.Add(new Vector2Int(x, y));
                }
            }
            return cells;
        }

        /// <summary>
        /// Which ward a given Chebyshev ring belongs to. The origin is the crypt and the outermost
        /// interior ring is the bailey; widening the castle widens the Keep band between them.
        /// </summary>
        private CastleZone ZoneForRing(int ring)
        {
            if (ring == 0)
                return CastleZone.Crypt;
            if (ring >= m_curtainWallRadius - 1)
                return CastleZone.OuterBailey;
            if (ring == m_curtainWallRadius - 2)
                return CastleZone.InnerWard;
            return CastleZone.Keep;
        }

        /// <summary>Whether every cell in <paramref name="cells"/> is reachable from the origin.</summary>
        private static bool IsSingleConnectedRegion(HashSet<Vector2Int> cells)
        {
            if (cells.Count == 0 || !cells.Contains(Vector2Int.zero))
                return false;

            var seen = new HashSet<Vector2Int> { Vector2Int.zero };
            var frontier = new Queue<Vector2Int>();
            frontier.Enqueue(Vector2Int.zero);

            while (frontier.Count > 0)
            {
                Vector2Int current = frontier.Dequeue();
                foreach (Vector2Int dir in FourDirs)
                {
                    Vector2Int next = current + dir;
                    if (!cells.Contains(next) || !seen.Add(next))
                        continue;
                    frontier.Enqueue(next);
                }
            }

            return seen.Count == cells.Count;
        }

        // --- Curtain wall --------------------------------------------------------

        /// <summary>
        /// Fills the whole outer ring so the wall is an unbroken closed loop: a corner tower at each
        /// of the four turns, bastions at a fixed spacing along the runs, exactly one gatehouse on
        /// an axis cell with its drawbridge in the cell immediately outside, and straight wall
        /// everywhere else. Returns the index of the gatehouse module.
        /// </summary>
        private int BuildCurtainWall(ProceduralCastleData data, Dictionary<Vector2Int, int> occupied)
        {
            List<Vector2Int> perimeter = PerimeterCells(m_curtainWallRadius);
            Vector2Int gateCell = k_GateOutward * m_curtainWallRadius;
            int gatehouseIndex = -1;

            for (int i = 0; i < perimeter.Count; i++)
            {
                Vector2Int cell = perimeter[i];
                bool isCorner = Mathf.Abs(cell.x) == m_curtainWallRadius
                                && Mathf.Abs(cell.y) == m_curtainWallRadius;

                if (isCorner)
                {
                    PlaceModule(data, occupied, ResolveRoomId(k_WallCornerId), CastleZone.CurtainWall,
                        cell, RotationForCorner(cell), isCryptEntry: false);
                    continue;
                }

                Quaternion rotation = RotationForOutwardWall(OutwardFacing(cell));

                if (cell == gateCell)
                {
                    gatehouseIndex = PlaceModule(data, occupied, ResolveRoomId(k_GatehouseId),
                        CastleZone.CurtainWall, cell, rotation, isCryptEntry: false);
                    continue;
                }

                string roomId = i % m_bastionSpacing == 0
                    ? ResolveRoomId(k_BastionId)
                    : ResolveRoomId(k_WallStraightId);

                PlaceModule(data, occupied, roomId, CastleZone.CurtainWall, cell, rotation,
                    isCryptEntry: false);
            }

            // The deck runs inward from its own wall face, so the drawbridge shares the gatehouse's
            // orientation and lands pointing back at the gate.
            PlaceModule(data, occupied, ResolveRoomId(k_DrawbridgeId), CastleZone.CurtainWall,
                gateCell + k_GateOutward, RotationForOutwardWall(k_GateOutward), isCryptEntry: false);

            return gatehouseIndex;
        }

        /// <summary>The ring's cells walked once round the boundary, so "every Nth" reads as spacing.</summary>
        private static List<Vector2Int> PerimeterCells(int radius)
        {
            var cells = new List<Vector2Int>();
            for (int x = -radius; x <= radius; x++)
            {
                cells.Add(new Vector2Int(x, -radius));
            }
            for (int y = -radius + 1; y <= radius; y++)
            {
                cells.Add(new Vector2Int(radius, y));
            }
            for (int x = radius - 1; x >= -radius; x--)
            {
                cells.Add(new Vector2Int(x, radius));
            }
            for (int y = radius - 1; y > -radius; y--)
            {
                cells.Add(new Vector2Int(-radius, y));
            }
            return cells;
        }

        /// <summary>Which way a non-corner perimeter cell faces out of the castle.</summary>
        private Vector2Int OutwardFacing(Vector2Int cell)
        {
            if (cell.x == m_curtainWallRadius)
                return new Vector2Int(1, 0);
            if (cell.x == -m_curtainWallRadius)
                return new Vector2Int(-1, 0);
            if (cell.y == m_curtainWallRadius)
                return new Vector2Int(0, 1);
            return new Vector2Int(0, -1);
        }

        /// <summary>
        /// Yaw that turns a curtain-wall piece's wall face outward.
        ///
        /// <c>build_wall_straight</c> (Tools/AssetPipeline/castle_builders.py) raises its wall on the
        /// module's SOUTH side, which lands on local -Z once the Blender Z-up correction is applied.
        /// So the module has to be yawed until local -Z points away from the castle — the opposite of
        /// what <see cref="RotationForFacing"/> does, which is why the wall ring has its own rule.
        /// </summary>
        private static Quaternion RotationForOutwardWall(Vector2Int outward)
        {
            float yaw;
            if (outward.x > 0)
            {
                yaw = 270f;
            }
            else if (outward.x < 0)
            {
                yaw = 90f;
            }
            else if (outward.y > 0)
            {
                yaw = 180f;
            }
            else
            {
                yaw = 0f;
            }
            return Quaternion.Euler(0f, yaw, 0f);
        }

        /// <summary>
        /// Yaw for a corner tower. <c>build_wall_corner</c> raises SOUTH and WEST, so unrotated its
        /// two faces cover the south-west outward pair; yawing by that piece's south face is enough
        /// to carry the west face onto the other outward side of any corner.
        /// </summary>
        private static Quaternion RotationForCorner(Vector2Int cell)
        {
            int signX = cell.x > 0 ? 1 : -1;
            int signY = cell.y > 0 ? 1 : -1;

            // South-west and north-east are reached by turning the south face onto the vertical
            // outward side; the mixed corners by turning it onto the horizontal one.
            Vector2Int primary = signX == signY
                ? new Vector2Int(0, signY)
                : new Vector2Int(signX, 0);

            return RotationForOutwardWall(primary);
        }

        // --- Placement -----------------------------------------------------------

        /// <summary>Records (and optionally instantiates) a single module, returning its index.</summary>
        private int PlaceModule(ProceduralCastleData data, Dictionary<Vector2Int, int> occupied,
            string roomId, CastleZone zone, Vector2Int cell, Quaternion rotation, bool isCryptEntry)
        {
            var worldPos = new Vector3(cell.x * cellSize, 0f, cell.y * cellSize);

            var placed = new ProceduralCastleData.PlacedModule(roomId, worldPos, rotation, zone, cell)
            {
                IsCryptEntry = isCryptEntry
            };

            int index = data.PlacedModules.Count;
            data.PlacedModules.Add(placed);
            occupied[cell] = index;

            InstantiateModule(roomId, zone, cell, worldPos, rotation, isCryptEntry);
            return index;
        }

        /// <summary>Instantiates the prefab for a module if the registry provides one.</summary>
        private void InstantiateModule(string roomId, CastleZone zone, Vector2Int cell,
            Vector3 worldPos, Quaternion rot, bool isCryptEntry)
        {
            if (registry == null)
                return;

            CastleRoomModuleData entry = registry.GetById(roomId);
            if (entry == null || entry.Prefab == null)
                return; // data-only layout; designer prefab not yet assigned.

            EnsureContainer();
            // Compose the facing with the prefab's own rotation rather than replacing it. The room
            // prefabs carry the Blender Z-up -> Unity Y-up correction on their root, and passing a
            // rotation to Instantiate overwrites it, which lays every room on its edge.
            GameObject go = Instantiate(entry.Prefab, worldPos, rot * entry.Prefab.transform.rotation,
                roomContainer);
            _instantiated.Add(go);

            var module = go.GetComponent<CastleRoomModule>();
            if (module == null)
                module = go.AddComponent<CastleRoomModule>();

            module.Zone = zone;
            module.RoomId = roomId;
            module.GridPosition = cell;
            module.IsCryptEntry = isCryptEntry;
            module.PopulateSockets();
        }

        /// <summary>
        /// Plans the bailey's dressing on its own seed stream (<see cref="CastleDressingPlanner"/>)
        /// and instantiates it into the castle, so it is cleared, baked and probed with the rooms.
        /// </summary>
        private void DressCastle(ProceduralCastleData data, int seed)
        {
            if (m_dressing == null)
                return;

            var straightIds = new HashSet<string> { k_WallStraightId, ResolveRoomId(k_WallStraightId) };
            data.Dressings = CastleDressingPlanner.Plan(data, seed, m_dressing, straightIds, m_curtainWallRadius, cellSize);

            foreach (PlacedDressing dressing in data.Dressings)
            {
                CastleDressingSet.Entry entry = m_dressing.GetById(dressing.Id);
                if (entry?.Prefab == null)
                    continue;
                EnsureContainer();
                GameObject go = Instantiate(entry.Prefab, dressing.Position,
                    dressing.Rotation * entry.Prefab.transform.rotation, roomContainer);
                go.name = $"{dressing.Id}_{dressing.Cell.x}_{dressing.Cell.y}";
                _instantiated.Add(go);
            }
        }

        /// <summary>
        /// Marks the gatehouse as the extraction exit: the castle has exactly one gate, so leaving
        /// through it is the only way out, and it is where the raid starts too.
        /// </summary>
        private void AssignExtractionExit(ProceduralCastleData data, int gatehouseIndex)
        {
            if (gatehouseIndex < 0 || gatehouseIndex >= data.PlacedModules.Count)
            {
                Debug.LogWarning("[CastleGen] No gatehouse placed — extraction exit unassigned.");
                return;
            }

            ProceduralCastleData.PlacedModule module = data.PlacedModules[gatehouseIndex];
            module.IsExtractionExit = true;
            data.PlacedModules[gatehouseIndex] = module;
            data.ExtractionExitIndex = gatehouseIndex;

            foreach (GameObject go in _instantiated)
            {
                if (go == null)
                    continue;
                var m = go.GetComponent<CastleRoomModule>();
                if (m != null && m.GridPosition == module.GridPosition)
                {
                    m.IsExtractionExit = true;
                    break;
                }
            }
        }

        /// <summary>
        /// Fills every archway that faces an empty cell with its zone's door plug, so an opening
        /// either leads into the neighbouring room or is walled off — never out into nothing.
        /// See docs/systems/scale.md ("Archways") for the sizes this relies on.
        /// </summary>
        private void SealOpenArchways(ProceduralCastleData data, Dictionary<Vector2Int, int> occupied)
        {
            if (registry == null)
                return;

            for (int i = 0; i < data.PlacedModules.Count; i++)
            {
                ProceduralCastleData.PlacedModule module = data.PlacedModules[i];
                if (!IsEnclosedRoom(module.Zone))
                    continue;

                GameObject plug = registry.GetDoorPlugForZone(module.Zone);
                if (plug == null)
                    continue;

                foreach (Vector2Int dir in FourDirs)
                {
                    if (IsArchwayConnected(data, occupied, module.GridPosition + dir))
                        continue;
                    InstantiateDoorPlug(plug, module, dir);
                }
            }
        }

        /// <summary>
        /// Whether an archway onto <paramref name="neighbour"/> leads somewhere, and so must be
        /// left open. A curtain-wall cell is a wall, not a room, so an archway onto one is as open
        /// as an archway onto nothing — except at the gatehouse, which is the way out.
        /// </summary>
        private bool IsArchwayConnected(ProceduralCastleData data,
            Dictionary<Vector2Int, int> occupied, Vector2Int neighbour)
        {
            if (neighbour == k_GateOutward * m_curtainWallRadius)
                return true;
            if (!occupied.TryGetValue(neighbour, out int index))
                return false;
            return IsEnclosedRoom(data.PlacedModules[index].Zone);
        }

        /// <summary>Places one door plug in the archway of <paramref name="module"/> facing <paramref name="dir"/>.</summary>
        private void InstantiateDoorPlug(GameObject plug, ProceduralCastleData.PlacedModule module,
            Vector2Int dir)
        {
            EnsureContainer();

            Vector3 position = module.Position
                               + new Vector3(dir.x, 0f, dir.y) * k_ArchwayInset
                               + Vector3.up * k_FloorThickness;

            // The plug slab is authored spanning its own local X, which lands on world X once the
            // prefab's Blender-to-Unity root rotation is composed in. A north/south archway needs
            // that span across Z instead, hence the quarter turn on the east/west pair.
            float yaw = dir.x != 0 ? 90f : 0f;
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f) * plug.transform.rotation;

            GameObject go = Instantiate(plug, position, rotation, roomContainer);
            go.name = $"DoorPlug_{module.Zone}_{module.GridPosition.x}_{module.GridPosition.y}_{dir.x}_{dir.y}";
            _instantiated.Add(go);
        }

        /// <summary>
        /// Whether modules of <paramref name="zone"/> are enclosed chambers (floor plus four
        /// walls) rather than stretches of the curtain wall itself.
        /// </summary>
        public static bool IsEnclosedRoom(CastleZone zone) => zone != CastleZone.CurtainWall;

        /// <summary>
        /// Whether a module of <paramref name="zone"/> is authored with an archway facing
        /// <paramref name="direction"/>. Enclosed rooms open on all four sides — see
        /// docs/systems/castle.md ("Doorways and door plugs").
        /// </summary>
        public static bool HasArchwayFacing(CastleZone zone, Vector2Int direction)
        {
            if (!IsEnclosedRoom(zone))
                return false;

            for (int i = 0; i < FourDirs.Length; i++)
            {
                if (FourDirs[i] == direction)
                    return true;
            }
            return false;
        }

        /// <summary>Destroys every room GameObject instantiated by the last generation pass.</summary>
        public void ClearGenerated()
        {
            for (int i = 0; i < _instantiated.Count; i++)
            {
                GameObject go = _instantiated[i];
                if (go == null)
                    continue;
                if (Application.isPlaying)
                {
                    // Destroy only lands at the end of the frame, and the next castle is generated
                    // and its NavMesh baked in this same frame: without this the bake saw both
                    // castles at once and the old walls sealed the new doorways, so every raid after
                    // the first was largely unwalkable. Inactive objects leave physics and the bake now.
                    go.SetActive(false);
                    Destroy(go);
                }
                else
                    DestroyImmediate(go);
            }
            _instantiated.Clear();
        }

        // --- Selection helpers ---------------------------------------------------

        /// <summary>The registry's id for a known piece, falling back to the literal for data-only runs.</summary>
        private string ResolveRoomId(string roomId)
        {
            if (registry == null)
                return roomId;
            CastleRoomModuleData entry = registry.GetById(roomId);
            return entry != null ? entry.RoomId : roomId;
        }

        /// <summary>Weighted random RoomId from a zone pool; falls back to a synthetic id.</summary>
        private static string PickWeighted(List<CastleRoomModuleData> pool, System.Random rng,
            CastleZone zone)
        {
            if (pool == null || pool.Count == 0)
                return zone + "_Room";

            int total = 0;
            for (int i = 0; i < pool.Count; i++)
                total += Mathf.Max(1, pool[i].Weight);

            int roll = rng.Next(0, total);
            for (int i = 0; i < pool.Count; i++)
            {
                roll -= Mathf.Max(1, pool[i].Weight);
                if (roll < 0)
                    return pool[i].RoomId;
            }
            return pool[pool.Count - 1].RoomId;
        }

        // --- Grid helpers --------------------------------------------------------

        private static int Chebyshev(Vector2Int c) => Mathf.Max(Mathf.Abs(c.x), Mathf.Abs(c.y));

        private static readonly Vector2Int[] FourDirs =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 1), new Vector2Int(0, -1)
        };

        /// <summary>Innermost ring first, then a stable cell order within the ring.</summary>
        private static int CompareByRingThenCell(Vector2Int a, Vector2Int b)
        {
            int ring = Chebyshev(a).CompareTo(Chebyshev(b));
            if (ring != 0)
                return ring;
            int x = a.x.CompareTo(b.x);
            return x != 0 ? x : a.y.CompareTo(b.y);
        }

        /// <summary>The single step from <paramref name="cell"/> toward the origin.</summary>
        private static Vector2Int InwardStep(Vector2Int cell)
        {
            int ring = Chebyshev(cell);
            if (ring == 0)
                return Vector2Int.zero;
            if (Mathf.Abs(cell.x) == ring)
                return new Vector2Int(cell.x > 0 ? -1 : 1, 0);
            return new Vector2Int(0, cell.y > 0 ? -1 : 1);
        }

        /// <summary>Deterministic Fisher–Yates shuffle driven by the seeded RNG.</summary>
        private static void Shuffle<T>(IList<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>Yaw so the module's forward (+Z, local North) points along a grid facing.</summary>
        private static Quaternion RotationForFacing(Vector2Int facing)
        {
            if (facing == Vector2Int.zero)
                return Quaternion.identity;
            var dir = new Vector3(facing.x, 0f, facing.y);
            return Quaternion.LookRotation(dir, Vector3.up);
        }

        private void EnsureContainer()
        {
            if (roomContainer != null)
                return;
            var container = new GameObject("GeneratedCastle");
            container.transform.SetParent(transform, false);
            roomContainer = container.transform;
        }
    }
}
