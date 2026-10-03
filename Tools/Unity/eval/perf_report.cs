// Editor, not playing. Loads one binary Profiler capture (.raw, from perf_capture.sh) and summarises the
// main thread: frame-time percentiles, the frames over 2x the median ("spikes"), the markers with the
// most self time across all frames and across the spike frames only, and GC allocation per frame.
// perf_report.sh fills in the path below.
string path = @"__PATH__";
if (!UnityEditorInternal.ProfilerDriver.LoadProfile(path, false)) return "could not load " + path;
int first = UnityEditorInternal.ProfilerDriver.firstFrameIndex, last = UnityEditorInternal.ProfilerDriver.lastFrameIndex;
var times = new System.Collections.Generic.List<float>();
var self = new System.Collections.Generic.Dictionary<string, double>();
var spikeSelf = new System.Collections.Generic.Dictionary<string, double>();
var frameSelf = new System.Collections.Generic.Dictionary<string, float>();
var children = new System.Collections.Generic.List<int>();
var stack = new System.Collections.Generic.Stack<int>();
double gcBytes = 0;
var parentOf = new System.Collections.Generic.Dictionary<int, string>();
var gcBy = new System.Collections.Generic.Dictionary<string, double>();
var slow = new System.Collections.Generic.Dictionary<string, double>();
var perFrame = new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, float>>();
for (int f = first; f <= last; f++)
{
    using (var view = UnityEditorInternal.ProfilerDriver.GetHierarchyFrameDataView(f, 0,
        UnityEditor.Profiling.HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,
        UnityEditor.Profiling.HierarchyFrameDataView.columnSelfTime, false))
    {
        if (view == null || !view.valid) continue;
        times.Add(view.frameTimeMs);
        var mine = new System.Collections.Generic.Dictionary<string, float>();
        stack.Clear(); stack.Push(view.GetRootItemID());
        parentOf.Clear();
        while (stack.Count > 0)
        {
            int id = stack.Pop();
            view.GetItemChildren(id, children);
            string name = id == view.GetRootItemID() ? "" : view.GetItemName(id);
            foreach (var c in children) { stack.Push(c); parentOf[c] = name; }
            if (id == view.GetRootItemID()) continue;
            float ms = view.GetItemColumnDataAsFloat(id, UnityEditor.Profiling.HierarchyFrameDataView.columnSelfTime);
            mine.TryGetValue(name, out var had); mine[name] = had + ms;
            // A GC.Alloc sample sits under the code that allocated; charge its bytes to that parent.
            if (name == "GC.Alloc")
            {
                float bytes = view.GetItemColumnDataAsFloat(id, UnityEditor.Profiling.HierarchyFrameDataView.columnGcMemory);
                gcBytes += bytes;
                string owner = parentOf[id];
                gcBy.TryGetValue(owner, out var g); gcBy[owner] = g + bytes;
            }
            // Where slow frames spend their time: every marker's self time, keyed by "parent > marker".
            if (ms > 1f) { string key = parentOf[id] + " > " + name; slow.TryGetValue(key, out var s0); slow[key] = s0 + ms; }
        }
        perFrame.Add(mine);
    }
}
int n = times.Count;
if (n == 0) return "no frames";
var sorted = new System.Collections.Generic.List<float>(times); sorted.Sort();
float P(float q) => sorted[System.Math.Min(n - 1, (int)(q * n))];
float median = P(0.5f), total = 0; foreach (var t in times) total += t;
int spikes = 0;
for (int i = 0; i < n; i++)
{
    bool spike = times[i] > 2 * median;
    if (spike) spikes++;
    foreach (var kv in perFrame[i])
    {
        self.TryGetValue(kv.Key, out var a); self[kv.Key] = a + kv.Value;
        if (spike) { spikeSelf.TryGetValue(kv.Key, out var b); spikeSelf[kv.Key] = b + kv.Value; }
    }
}
var sb = new System.Text.StringBuilder();
sb.AppendLine(System.IO.Path.GetFileName(path) + ": " + n + " frames, avg " + (1000 * n / total).ToString("0") + " fps, frame ms p50 " + median.ToString("0.00")
    + " p95 " + P(0.95f).ToString("0.00") + " p99 " + P(0.99f).ToString("0.00") + " max " + sorted[n - 1].ToString("0.00")
    + "; spikes (>2x p50) " + spikes + "; GC alloc " + (gcBytes / n / 1024).ToString("0.0") + " KB/frame");
sb.AppendLine("top self time, ms per frame:");
foreach (var kv in self.OrderByDescending(k => k.Value).Take(30)) sb.AppendLine("  " + (kv.Value / n).ToString("0.000") + "  " + kv.Key);
if (spikes > 0)
{
    sb.AppendLine("top self time in spike frames, ms per spike frame:");
    foreach (var kv in spikeSelf.OrderByDescending(k => k.Value).Take(15)) sb.AppendLine("  " + (kv.Value / spikes).ToString("0.000") + "  " + kv.Key);
}
sb.AppendLine("GC allocation by allocating marker, KB per frame:");
foreach (var kv in gcBy.OrderByDescending(k => k.Value).Take(12)) sb.AppendLine("  " + (kv.Value / n / 1024).ToString("0.00") + "  " + kv.Key);
sb.AppendLine("samples over 1 ms self time, total ms over the capture (parent > marker):");
foreach (var kv in slow.OrderByDescending(k => k.Value).Take(12)) sb.AppendLine("  " + kv.Value.ToString("0.0") + "  " + kv.Key);
sb.AppendLine("worst frames: " + string.Join(", ", times.Select((t, i) => (t, i)).OrderByDescending(x => x.t).Take(8).Select(x => x.t.ToString("0.0") + "ms@" + x.i)));
return sb.ToString();
