using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Castle
{
    /// <summary>
    /// Deterministic, seed-driven procedural castle builder.
    ///
    /// The castle is a radius-3 curtain wall (<see cref="CurtainWallRadius"/>) round a stacked
    /// 5 x 5 / 3 x 3 / 3 x 3 block: the ground floor, a keep above and a crypt below, laid out by
    /// <see cref="CastleFloorPlanner"/> and joined by three stairs. The outer Chebyshev ring is
    /// filled completely — corners, bastions, one gatehouse and straight runs.
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
        public const string StairUpId = "StairUp";
        public const string StairDownId = "StairDown";

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
        public int CurtainWallRadius => CastleFloorPlanner.CurtainWallRadius;

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
            var occupied = new Dictionary<Vector3Int, int>();

            BuildFloors(data, occupied, rng);
            int gatehouseIndex = BuildCurtainWall(data, occupied);
            AssignExtractionExit(data, gatehouseIndex);
            data.EntranceCells = CastleEntrancePlanner.Plan(data, CurtainWallRadius,
                new HashSet<string> { k_WallStraightId, ResolveRoomId(k_WallStraightId) });

            // Every enclosed room is authored with an archway on all four sides, so any side with
            // no neighbour is currently a hole in the outer face. Fill those.
            SealOpenArchways(data, occupied);

            // Placed with the rooms, before the nav graph is built or anyone probes for a standing point,
            // so a cart or a woodpile is walked round and never stood in.
            List<GameObject> dressing = DressCastle(data, seed);

            // Stitched here because the archways just left open are exactly the nav graph's joins.
            data.NavGraph = CastleNavGraph.Build(data, registry, dressing);

            LastGenerated = data;
            return data;
        }

        // --- Interior ------------------------------------------------------------

        /// <summary>
        /// Places the floor planner's rooms (#247): crypt, ground, keep, in its order, so placement indices are
        /// a function of the seed alone. A stair is placed once, at its lowest level, and spans two.
        /// </summary>
        private void BuildFloors(ProceduralCastleData data, Dictionary<Vector3Int, int> occupied, System.Random rng)
        {
            Vector2Int gateApproach = k_GateOutward * (CurtainWallRadius - 1);
            foreach (PlannedRoom room in CastleFloorPlanner.Plan(rng, m_interiorFillFraction, gateApproach))
            {
                string roomId = room.Kind switch
                {
                    PlannedRoomKind.StairUp => ResolveRoomId(StairUpId),
                    PlannedRoomKind.StairDown => ResolveRoomId(StairDownId),
                    PlannedRoomKind.FinalChamber => ResolveRoomId(k_CryptFinalId),
                    _ => PickWeighted(registry != null ? registry.GetModulesForZone(room.Zone) : null, rng, room.Zone),
                };
                bool stair = room.Kind == PlannedRoomKind.StairUp || room.Kind == PlannedRoomKind.StairDown;
                // A stair's prefab is authored with its exit facing local north; a room faces inward as before.
                Quaternion rotation = stair ? RotationForFacing(room.ExitFacing) : RotationForFacing(InwardStep(room.Cell));
                var shape = new ModuleShape(room.Level, stair ? 2 : 1,
                    room.Kind == PlannedRoomKind.StairUp ? CastleLevels.Keep : room.Level, room.ExitFacing);
                int index = PlaceModule(data, occupied, roomId, room.Zone, room.Cell, rotation,
                    room.Kind == PlannedRoomKind.FinalChamber, shape);
                if (room.Kind == PlannedRoomKind.FinalChamber)
                    data.CryptStartIndex = index;
            }
        }

        /// <summary>Where a module stands in height and, for a stair, how it opens. Ground, one storey, by default.</summary>
        private readonly struct ModuleShape
        {
            public ModuleShape(int level, int storeys, int exitLevel, Vector2Int exitFacing)
            {
                Level = level; Storeys = storeys; ExitLevel = exitLevel; ExitFacing = exitFacing;
            }
            public static ModuleShape Ground => new ModuleShape(CastleLevels.Ground, 1, CastleLevels.Ground, Vector2Int.zero);
            public int Level { get; }
            public int Storeys { get; }
            public int ExitLevel { get; }
            public Vector2Int ExitFacing { get; }
        }

        // --- Curtain wall --------------------------------------------------------

        /// <summary>
        /// Fills the whole outer ring so the wall is an unbroken closed loop: a corner tower at each
        /// of the four turns, bastions at a fixed spacing along the runs, exactly one gatehouse on
        /// an axis cell with its drawbridge in the cell immediately outside, and straight wall
        /// everywhere else. Returns the index of the gatehouse module.
        /// </summary>
        private int BuildCurtainWall(ProceduralCastleData data, Dictionary<Vector3Int, int> occupied)
        {
            List<Vector2Int> perimeter = PerimeterCells(CurtainWallRadius);
            Vector2Int gateCell = k_GateOutward * CurtainWallRadius;
            int gatehouseIndex = -1;

            for (int i = 0; i < perimeter.Count; i++)
            {
                Vector2Int cell = perimeter[i];
                bool isCorner = Mathf.Abs(cell.x) == CurtainWallRadius
                                && Mathf.Abs(cell.y) == CurtainWallRadius;

                if (isCorner)
                {
                    PlaceModule(data, occupied, ResolveRoomId(k_WallCornerId), CastleZone.CurtainWall,
                        cell, RotationForCorner(cell), false, ModuleShape.Ground);
                    continue;
                }

                Quaternion rotation = RotationForOutwardWall(OutwardFacing(cell));

                if (cell == gateCell)
                {
                    gatehouseIndex = PlaceModule(data, occupied, ResolveRoomId(k_GatehouseId),
                        CastleZone.CurtainWall, cell, rotation, false, ModuleShape.Ground);
                    continue;
                }

                string roomId = i % m_bastionSpacing == 0
                    ? ResolveRoomId(k_BastionId)
                    : ResolveRoomId(k_WallStraightId);

                PlaceModule(data, occupied, roomId, CastleZone.CurtainWall, cell, rotation,
                    false, ModuleShape.Ground);
            }

            // The deck runs inward from its own wall face, so the drawbridge shares the gatehouse's
            // orientation and lands pointing back at the gate.
            PlaceModule(data, occupied, ResolveRoomId(k_DrawbridgeId), CastleZone.CurtainWall,
                gateCell + k_GateOutward, RotationForOutwardWall(k_GateOutward), false, ModuleShape.Ground);

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
            if (cell.x == CurtainWallRadius)
                return new Vector2Int(1, 0);
            if (cell.x == -CurtainWallRadius)
                return new Vector2Int(-1, 0);
            if (cell.y == CurtainWallRadius)
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
        private int PlaceModule(ProceduralCastleData data, Dictionary<Vector3Int, int> occupied,
            string roomId, CastleZone zone, Vector2Int cell, Quaternion rotation, bool isCryptEntry,
            ModuleShape shape)
        {
            var worldPos = new Vector3(cell.x * cellSize, CastleLevels.RootY(shape.Level), cell.y * cellSize);

            var placed = new ProceduralCastleData.PlacedModule(roomId, worldPos, rotation, zone, cell)
            {
                IsCryptEntry = isCryptEntry,
                Level = shape.Level,
                Storeys = shape.Storeys,
                ExitLevel = shape.ExitLevel,
                ExitFacing = shape.ExitFacing,
            };

            int index = data.PlacedModules.Count;
            data.PlacedModules.Add(placed);
            for (int level = placed.Level; level <= placed.TopLevel; level++)
                occupied[new Vector3Int(cell.x, cell.y, level)] = index;

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
        /// Returns the pieces placed, for the nav graph to walk round.
        /// </summary>
        private List<GameObject> DressCastle(ProceduralCastleData data, int seed)
        {
            var placed = new List<GameObject>();
            if (m_dressing == null)
                return placed;

            var straightIds = new HashSet<string> { k_WallStraightId, ResolveRoomId(k_WallStraightId) };
            data.Dressings = CastleDressingPlanner.Plan(data, seed, m_dressing, straightIds, CurtainWallRadius, cellSize);

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
                placed.Add(go);
            }
            return placed;
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
        /// See docs/4-systems/scale.md ("Archways") for the sizes this relies on.
        /// </summary>
        private void SealOpenArchways(ProceduralCastleData data, Dictionary<Vector3Int, int> occupied)
        {
            if (registry == null)
                return;

            for (int i = 0; i < data.PlacedModules.Count; i++)
            {
                ProceduralCastleData.PlacedModule module = data.PlacedModules[i];
                if (!IsEnclosedRoom(module.Zone))
                    continue;

                for (int level = module.Level; level <= module.TopLevel; level++)
                {
                    // A stair's exit level has its walls closed in the prefab already.
                    if (module.Storeys > 1 && level == module.ExitLevel)
                        continue;

                    GameObject plug = registry.GetDoorPlugForZone(module.Zone);
                    if (plug == null)
                        continue;

                    foreach (Vector2Int dir in FourDirs)
                    {
                        if (IsArchwayConnected(data, occupied, module.GridPosition + dir, level, -dir))
                            continue;
                        InstantiateDoorPlug(plug, module, dir, level);
                    }
                }
            }
        }

        /// <summary>
        /// Whether an archway onto <paramref name="neighbour"/> leads somewhere, and so must be
        /// left open. A curtain-wall cell is a wall, not a room, so an archway onto one is as open
        /// as an archway onto nothing — except at the gatehouse, and at an entrance from the
        /// strip (<see cref="CastleEntrancePlanner"/>).
        /// </summary>
        private bool IsArchwayConnected(ProceduralCastleData data,
            Dictionary<Vector3Int, int> occupied, Vector2Int neighbour, int level, Vector2Int towardMe)
        {
            // The gate and the strip entrances are ground-level only.
            if (level == CastleLevels.Ground && neighbour == k_GateOutward * CurtainWallRadius)
                return true;
            if (level == CastleLevels.Ground && data.EntranceCells.Contains(neighbour))
                return true;
            if (!occupied.TryGetValue(new Vector3Int(neighbour.x, neighbour.y, level), out int index))
                return false;
            ProceduralCastleData.PlacedModule other = data.PlacedModules[index];
            return IsEnclosedRoom(other.Zone) && CastleStairRule.Opens(other, level, towardMe);
        }

        /// <summary>Places one door plug in the archway of <paramref name="module"/> facing <paramref name="dir"/>.</summary>
        private void InstantiateDoorPlug(GameObject plug, ProceduralCastleData.PlacedModule module,
            Vector2Int dir, int level)
        {
            EnsureContainer();

            Vector3 position = new Vector3(module.Position.x, CastleLevels.RootY(level), module.Position.z)
                               + new Vector3(dir.x, 0f, dir.y) * k_ArchwayInset
                               + Vector3.up * k_FloorThickness;

            // The plug slab is authored spanning its own local X, which lands on world X once the
            // prefab's Blender-to-Unity root rotation is composed in. A north/south archway needs
            // that span across Z instead, hence the quarter turn on the east/west pair.
            float yaw = dir.x != 0 ? 90f : 0f;
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f) * plug.transform.rotation;

            GameObject go = Instantiate(plug, position, rotation, roomContainer);
            go.name = $"DoorPlug_{module.Zone}_{module.GridPosition.x}_{module.GridPosition.y}_{dir.x}_{dir.y}_L{level}";
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
        /// docs/4-systems/castle.md ("Doorways and door plugs").
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
                    // and its nav graph built in this same frame: without this the old walls still
                    // answered physics queries and sealed the new doorways (this was found when the
                    // NavMesh was baked here, and the guards' capsule sweep has the same exposure).
                    // Inactive objects leave physics now.
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
