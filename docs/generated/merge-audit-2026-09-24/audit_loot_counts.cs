// Run in a live Editor (edit mode) with:  unity command eval_file --file <this file>
// For each era and 5 seeds: builds the castle with RaidScene's generator settings in a throwaway
// additive scene, then counts rooms, loot anchors and the loot LootPlacementPlanner actually plans.
// The throwaway scene is closed without saving; RaidScene itself is not modified.
var sb = new System.Text.StringBuilder();
var raidScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var sceneGen = UnityEngine.Object.FindFirstObjectByType<RogueAi.Castle.ProceduralCastleGenerator>();
if (sceneGen == null) return "no ProceduralCastleGenerator in the open scene (" + raidScene.path + ")";
var cat = UnityEditor.AssetDatabase.LoadAssetAtPath<RogueAi.Raid.EraContentCatalogue>("Assets/_Project/Data/Eras/EraContentCatalogue.asset");
var temp = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Additive);
UnityEngine.SceneManagement.SceneManager.SetActiveScene(temp);
try
{
    var go = new GameObject("AuditGenerator");
    var gen = go.AddComponent<RogueAi.Castle.ProceduralCastleGenerator>();
    UnityEditor.EditorJsonUtility.FromJsonOverwrite(UnityEditor.EditorJsonUtility.ToJson(sceneGen), gen);
    var defaultRooms = sceneGen.Registry;
    var defaultLoot = UnityEditor.AssetDatabase.LoadAssetAtPath<RogueAi.Raid.RaidLootTable>("Assets/_Project/Data/Loot/RaidLootTable.asset");
    foreach (RogueAi.Inventory.HistoricalEra era in System.Enum.GetValues(typeof(RogueAi.Inventory.HistoricalEra)))
    {
        var entry = cat.For(era);
        var rooms = entry != null && entry.Rooms != null ? entry.Rooms : defaultRooms;
        var loot = entry != null && entry.Loot != null ? entry.Loot : defaultLoot;
        gen.Registry = rooms;
        int roomTotal = 0, anchorTotal = 0, placedTotal = 0, runs = 5;
        var perZone = new System.Collections.Generic.Dictionary<RogueAi.Castle.CastleZone, int[]>();
        for (int seed = 1; seed <= runs; seed++)
        {
            var data = gen.Generate(seed * 7919);
            var plan = RogueAi.Raid.LootPlacementPlanner.Plan(data, loot, seed * 7919, rooms);
            for (int i = 0; i < data.PlacedModules.Count; i++)
            {
                var m = data.PlacedModules[i];
                if (m.Zone == RogueAi.Castle.CastleZone.CurtainWall) continue;
                roomTotal++;
                var a = rooms.GetById(m.RoomId);
                int anchors = a != null && a.LootAnchors != null ? a.LootAnchors.Length : 0;
                anchorTotal += anchors;
                if (!perZone.ContainsKey(m.Zone)) perZone[m.Zone] = new int[3];
                perZone[m.Zone][0]++; perZone[m.Zone][1] += anchors;
            }
            foreach (var p in plan) { placedTotal++; perZone[p.Zone][2]++; }
            gen.ClearGenerated();
        }
        sb.AppendLine($"{era} ({rooms.name}, {loot.name}): per raid avg rooms={roomTotal / (float)runs:F1} anchors={anchorTotal / (float)runs:F1} loot placed={placedTotal / (float)runs:F1}");
        foreach (var kv in perZone)
            sb.AppendLine($"    {kv.Key}: rooms={kv.Value[0] / (float)runs:F1} anchors={kv.Value[1] / (float)runs:F1} placed={kv.Value[2] / (float)runs:F1}");
    }
}
finally
{
    UnityEngine.SceneManagement.SceneManager.SetActiveScene(raidScene);
    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(temp, true);
}
return sb.ToString();
