// Editor, not playing. Sets how each clip under Assets/_Project/Audio loads (#244, owner approved
// 2026-10-03). Streaming opens and closes a file stream per play, which stalled the frame in fights.
//   music, and any clip over 10 s       Streaming (unchanged)
//   voice lines (VO/)                   Compressed In Memory, Vorbis: many lines, kept small in RAM
//   everything else (short sounds)      Decompress On Load, ADPCM (audio plan section 2)
// Short sounds also preload, so the first play of a clip does not read from disk.
var counts = new System.Collections.Generic.Dictionary<string, int>();
UnityEditor.AssetDatabase.StartAssetEditing(); // batches the reimports into one pass
try {
foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/_Project/Audio" }))
{
    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
    var importer = (UnityEditor.AudioImporter)UnityEditor.AssetImporter.GetAtPath(path);
    var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AudioClip>(path);
    var settings = importer.defaultSampleSettings;
    string kind;
    if (path.Contains("/Music/") || clip.length > 10f)
    {
        kind = "streaming";
        if (settings.loadType == UnityEngine.AudioClipLoadType.Streaming) { counts.TryGetValue(kind, out int k0); counts[kind] = k0 + 1; continue; }
        settings.loadType = UnityEngine.AudioClipLoadType.Streaming;
        settings.preloadAudioData = false;
    }
    else if (path.Contains("/VO/"))
    {
        kind = "compressed in memory";
        settings.loadType = UnityEngine.AudioClipLoadType.CompressedInMemory;
        settings.compressionFormat = UnityEngine.AudioCompressionFormat.Vorbis;
        settings.preloadAudioData = true;
    }
    else
    {
        kind = "decompress on load";
        settings.loadType = UnityEngine.AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = UnityEngine.AudioCompressionFormat.ADPCM;
        settings.preloadAudioData = true;
    }
    importer.defaultSampleSettings = settings;
    importer.SaveAndReimport();
    counts.TryGetValue(kind, out int k); counts[kind] = k + 1;
}
} finally { UnityEditor.AssetDatabase.StopAssetEditing(); }
return string.Join(", ", counts.Select(kv => kv.Key + " " + kv.Value));
