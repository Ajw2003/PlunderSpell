// Play mode (any scene; nothing networked is touched). Stair survey for #197: generates castles for a
// few seeds and each Age's room registry, and lists every raised walkable area in the walk map (cells
// 1.2 m or more above their room's floor: galleries, daises, landings) with its size and whether a player
// on the ground can walk up to it. A raised area nobody can reach is a stair that leads nowhere (or a
// gallery with no stair); a reachable one of a few cells is a stair that stops at a ledge.
// Output is grouped by room type, since stairs are authored per room.
var seeds = new[] { 3508293, 43, 64, 88, 1234 };
var registries = new[] { "CastleRoomRegistry", "CastleRoomRegistry_BronzeAge", "CastleRoomRegistry_LateMedieval" };
const int RaisedCm = 120;
var All = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
var dressing = UnityEditor.AssetDatabase.LoadAssetAtPath<Plunderspell.Castle.CastleDressingSet>("Assets/_Project/Data/Castle/CastleDressingSet.asset");
var host = new UnityEngine.GameObject("StairSurvey");
host.transform.position = new UnityEngine.Vector3(0f, -500f, 0f); // out of the way of anything in the scene
var gen = host.AddComponent<Plunderspell.Castle.ProceduralCastleGenerator>();
// room type -> "regions" summary lines (deduplicated) and counters
var byRoom = new System.Collections.Generic.SortedDictionary<string, System.Collections.Generic.SortedDictionary<string, int>>();
var sb = new System.Text.StringBuilder();
try
{
    foreach (var regName in registries)
    {
        var registry = UnityEditor.AssetDatabase.LoadAssetAtPath<Plunderspell.Castle.CastleRoomRegistry>("Assets/_Project/Data/Castle/" + regName + ".asset");
        if (registry == null) { sb.AppendLine("missing " + regName); continue; }
        gen.Registry = registry;
        gen.Dressing = dressing;
        foreach (int seed in seeds)
        {
            var data = gen.Generate(seed);
            var graph = data.NavGraph;
            var grid = (Plunderspell.Castle.CastleNavGrid)typeof(Plunderspell.Castle.CastleNavGraph).GetField("_grid", All).GetValue(graph);
            if (grid == null) { sb.AppendLine(regName + " seed " + seed + ": no walk map"); continue; }
            int per = Plunderspell.Castle.CastleNavGrid.CellsPerModule, cols = Plunderspell.Castle.CastleNavGrid.ColumnsPerModule;
            // The reference "ground": the walkable cell nearest the crypt start module's centre.
            var reference = graph.NearestWalkableCell(data.PlacedModules[System.Math.Max(0, data.CryptStartIndex)].Position, 12f);
            for (int m = 0; m < grid.ModuleCount; m++)
            {
                if (!grid.HasTile(m)) continue;
                // The room's floor: the most common layer-0 height.
                var heights = new System.Collections.Generic.Dictionary<int, int>();
                for (int c = 0; c < cols; c++)
                {
                    int cell = m * per + c;
                    if (!grid.IsWalkable(cell)) continue;
                    heights.TryGetValue(grid.HeightCm(cell), out int k); heights[grid.HeightCm(cell)] = k + 1;
                }
                if (heights.Count == 0) continue;
                int floor = heights.OrderByDescending(h => h.Value).First().Key;
                var seen = new System.Collections.Generic.HashSet<int>();
                string room = data.PlacedModules[m].RoomId;
                for (int local = 0; local < per; local++)
                {
                    int cell = m * per + local;
                    if (!grid.IsWalkable(cell) || grid.HeightCm(cell) - floor < RaisedCm || seen.Contains(local)) continue;
                    // Flood the raised area within this room.
                    var queue = new System.Collections.Generic.Queue<int>(); queue.Enqueue(local); seen.Add(local);
                    int size = 0, top = 0;
                    while (queue.Count > 0)
                    {
                        int at = queue.Dequeue(); size++;
                        top = System.Math.Max(top, grid.HeightCm(m * per + at) - floor);
                        for (int dir = 0; dir < 4; dir++)
                            for (int layer = 0; layer < Plunderspell.Castle.CastleNavTile.Layers; layer++)
                                if (grid.TryStep(m, at, dir, layer, out int next) && !seen.Contains(next)
                                    && grid.HeightCm(m * per + next) - floor >= RaisedCm)
                                { seen.Add(next); queue.Enqueue(next); }
                    }
                    bool reachable = reference >= 0 && graph.IsReachable(cell, reference);
                    string verdict = !reachable ? "UNREACHABLE" : size < 12 ? "TINY" : "ok";
                    string key = verdict + " raised area " + size + " cells, top +" + (top / 100f).ToString("0.0") + " m";
                    if (!byRoom.TryGetValue(regName + " / " + room, out var lines)) byRoom[regName + " / " + room] = lines = new System.Collections.Generic.SortedDictionary<string, int>();
                    lines.TryGetValue(key, out int n); lines[key] = n + 1;
                }
            }
        }
    }
}
finally
{
    gen.GetType().GetMethod("ClearGenerated", All)?.Invoke(gen, null);
    UnityEngine.Object.Destroy(host);
}
foreach (var kv in byRoom)
{
    sb.AppendLine(kv.Key);
    foreach (var line in kv.Value) sb.AppendLine("  " + line.Key + "  (x" + line.Value + ")");
}
return sb.Length == 0 ? "no raised areas found" : sb.ToString();
