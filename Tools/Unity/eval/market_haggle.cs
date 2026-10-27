// Play mode, solo, in the Lair room. Reports the ledger, the pile and the four counters (#312).
// Run: bash Tools/Unity/eval.sh --file Tools/Unity/eval/market_haggle.cs
var hl = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.HaulLanding>();
var lair = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Lair.LairHubManager>();
string s = "state " + Plunderspell.Core.GameServices.GameState.CurrentState + " gold " + lair.AccumulatedGold + " debt " + lair.TotalDebt
    + " landing " + (hl != null ? hl.transform.position.ToString() : "none") + "\n";
foreach (var v in UnityEngine.Object.FindObjectsByType<Plunderspell.Loot.LootValue>(UnityEngine.FindObjectsSortMode.None))
    s += v.name + " worth " + v.Worth + " at " + v.transform.position + "\n";
foreach (var c in UnityEngine.Object.FindObjectsByType<Plunderspell.Raid.SellCounter>(UnityEngine.FindObjectsSortMode.None))
    s += c.Vendor + " counter " + c.transform.position + "\n";
s += "camera " + UnityEngine.Camera.main.transform.position;
return s;
