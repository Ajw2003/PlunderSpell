// Play mode, solo, in the Lair room (#313). Moves the one loose loot piece (spawn it first with market_spawn_piece.cs) onto
// VENDOR's counter, then reports the piece's category and each counter's open haggle (limit and opening offer).
// Run it once with VENDOR = "Goldsmith", wait two seconds, read; edit VENDOR to "Pardoner" and run again. Read-only on the second
// call: set MOVE to false to only report.
const string VENDOR = "Pardoner";
const bool MOVE = true;
var piece = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Loot.LootValue>();
var counters = UnityEngine.Object.FindObjectsByType<Plunderspell.Raid.SellCounter>(UnityEngine.FindObjectsSortMode.None);
if (MOVE)
    foreach (var c in counters)
    {
        if (c.Vendor.ToString() != VENDOR) continue;
        var top = c.GetComponentInChildren<UnityEngine.BoxCollider>();
        piece.transform.position = top.bounds.center + UnityEngine.Vector3.up * 0.1f;
        var body = piece.GetComponent<UnityEngine.Rigidbody>();
        if (body != null) body.linearVelocity = UnityEngine.Vector3.zero;
    }
string s = piece.name + " category " + piece.Item.Category + " worth " + piece.Worth + "\n";
foreach (var c in counters)
    s += c.Vendor + ": " + (c.Open == null ? "no haggle" : "limit " + c.Open.Limit.ToString("F1") + " opening " + c.Open.Offer.ToString("F1")) + "\n";
return s;
