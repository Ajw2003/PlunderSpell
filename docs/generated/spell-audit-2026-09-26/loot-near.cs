// Counts loot within 4 m of the local player, and what it is worth: Aurum Voco's conjured coin.
int n = 0; float worth = 0f;
var here = StateMachine.PlayerStateMachine.Local.transform.position;
foreach (var l in UnityEngine.Object.FindObjectsByType<Plunderspell.Loot.LootValue>(FindObjectsSortMode.None))
    if (Vector3.Distance(l.transform.position, here) < 4f) { n++; worth += l.Worth; }
return "loot within 4 m: " + n + ", worth " + worth;
