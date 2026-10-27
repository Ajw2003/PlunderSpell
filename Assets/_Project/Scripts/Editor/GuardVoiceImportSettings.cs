using UnityEditor;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Guard speech clips are re-voiced at run time from their samples, which only works for clips that
    /// are decompressed on load; this sets that on every clip under Resources/GuardVoice.
    /// </summary>
    public sealed class GuardVoiceImportSettings : AssetPostprocessor
    {
        private const string Folder = "Assets/_Project/Resources/GuardVoice/";

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Folder, System.StringComparison.Ordinal))
                return;

            var importer = (AudioImporter)assetImporter;
            importer.forceToMono = true;
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            importer.defaultSampleSettings = settings;
        }
    }
}
