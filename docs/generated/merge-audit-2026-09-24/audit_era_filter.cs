// Asks each era's forged roster for a guard the way a raid does (dreamy's era-filtered PickForZone),
// and reports whether the pick came from that era's own pool or from the any-Age fallback.
var sb = new System.Text.StringBuilder();
var cat = UnityEditor.AssetDatabase.LoadAssetAtPath<RogueAi.Raid.EraContentCatalogue>("Assets/_Project/Data/Eras/EraContentCatalogue.asset");
var warnings = new System.Collections.Generic.List<string>();
Application.LogCallback cb = (msg, st, type) => { if (type == LogType.Warning && msg.StartsWith("[Roster]")) warnings.Add(msg); };
Application.logMessageReceived += cb;
try
{
    foreach (RogueAi.Inventory.HistoricalEra era in System.Enum.GetValues(typeof(RogueAi.Inventory.HistoricalEra)))
    {
        var roster = cat.For(era)?.Enemies;
        if (roster == null) { sb.AppendLine(era + ": no roster"); continue; }
        roster.ResetWarnings();
        int before = warnings.Count;
        var rng = new System.Random(1);
        var own = 0; var total = 0;
        foreach (RogueAi.Castle.CastleZone zone in System.Enum.GetValues(typeof(RogueAi.Castle.CastleZone)))
        {
            if (roster.EntriesFor(zone).Count == 0) continue;
            total++;
            if (roster.EntriesFor(zone, era).Count > 0) own++;
            roster.PickForZone(zone, era, rng);
        }
        sb.AppendLine(era + ": zones with guards=" + total + ", zones served by own-era entries=" + own + ", fallback warnings=" + (warnings.Count - before));
    }
}
finally { Application.logMessageReceived -= cb; }
if (warnings.Count > 0) sb.AppendLine("first warning: " + warnings[0]);
return sb.ToString();
