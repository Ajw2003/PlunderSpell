// Play mode, solo, in the Lair room. Spawns one pile piece (the first entry of the raid's loot table) above the named
// counter's top (entry ENTRY), through the haul pile's own spawner so the pile save counts it (#312). Edit VENDOR first.
// Run: bash Tools/Unity/eval.sh --file Tools/Unity/eval/market_spawn_piece.cs
const string VENDOR = "Goldsmith";
const int ENTRY = 4; // GoldenGoblet, worth 150, small enough to stay on a 0.6 m deep counter (the copper pot slides off).
var hl = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.HaulLanding>();
var pile = (Plunderspell.Raid.LootSpawner)typeof(Plunderspell.Raid.HaulLanding)
    .GetField("_pile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(hl);
Plunderspell.Raid.RaidLootTable table = null;
foreach (var sp in UnityEngine.Object.FindObjectsByType<Plunderspell.Raid.LootSpawner>(UnityEngine.FindObjectsSortMode.None))
    if (sp != pile && sp.Table != null) table = sp.Table;
var entry = table.Entries[ENTRY];
foreach (var c in UnityEngine.Object.FindObjectsByType<Plunderspell.Raid.SellCounter>(UnityEngine.FindObjectsSortMode.None))
{
    if (c.Vendor.ToString() != VENDOR) continue;
    var top = c.GetComponentInChildren<UnityEngine.BoxCollider>();
    var go = pile.SpawnLoose(entry.Item, entry.Prefab, top.bounds.center + UnityEngine.Vector3.up * 0.1f);
    return "spawned " + go.name + " worth " + go.GetComponent<Plunderspell.Loot.LootValue>().Worth + " at " + go.transform.position
        + "; saved pile " + string.Join(",", Plunderspell.Lair.HaulPileSave.Load(Plunderspell.Lair.SaveSlots.Active));
}
return "no counter for " + VENDOR;
