#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.Voice.EditorTools
{
    /// <summary>
    /// Editor utility that fetches the small English Vosk model and unpacks it into
    /// StreamingAssets so <see cref="VoskVoiceInputService"/> can load it at runtime.
    ///
    /// The model (~40MB) is intentionally NOT committed to the repo. Run
    /// <c>Plunderspell/Voice/Download Vosk Small Model</c> once per clone.
    /// </summary>
    public static class VoskModelDownloader
    {
        private const string ModelUrl =
            "https://alphacephei.com/vosk/models/vosk-model-small-en-us-0.15.zip";

        // Where the model must end up for VoskVoiceInputService.ModelRelativePath.
        private const string TargetDir = "Assets/StreamingAssets/VoskModels/small-en-us";

        private const string ZipName = "vosk-model-small-en-us-0.15.zip";

        [MenuItem("Plunderspell/Voice/Download Vosk Small Model")]
        public static void DownloadModel()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string targetFull = Path.Combine(projectRoot, TargetDir);
            string zipPath = Path.Combine(Path.GetTempPath(), ZipName);
            string extractRoot = Path.Combine(Path.GetTempPath(), "vosk_extract_" + Guid.NewGuid().ToString("N"));

            if (Directory.Exists(targetFull) &&
                Directory.GetFiles(targetFull, "*", SearchOption.AllDirectories).Length > 0)
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Vosk Model",
                    $"A model already exists at:\n{TargetDir}\n\nDownload and overwrite it?",
                    "Overwrite", "Cancel");
                if (!overwrite)
                    return;
            }

            try
            {
                EditorUtility.DisplayProgressBar("Vosk Model", "Contacting alphacephei.com...", 0.02f);

                // --- Download with progress ---
                using (var client = new WebClient())
                {
                    bool done = false;
                    Exception error = null;
                    int lastPercent = -1;

                    client.DownloadProgressChanged += (s, e) =>
                    {
                        if (e.ProgressPercentage != lastPercent)
                        {
                            lastPercent = e.ProgressPercentage;
                        }
                    };
                    client.DownloadFileCompleted += (s, e) =>
                    {
                        error = e.Error;
                        done = true;
                    };

                    client.DownloadFileAsync(new Uri(ModelUrl), zipPath);

                    while (!done)
                    {
                        float p = Mathf.Clamp01(lastPercent / 100f);
                        bool cancelled = EditorUtility.DisplayCancelableProgressBar(
                            "Vosk Model",
                            $"Downloading {ZipName} ({lastPercent}%)...",
                            0.05f + p * 0.7f);
                        if (cancelled)
                        {
                            client.CancelAsync();
                            throw new OperationCanceledException("Download cancelled by user.");
                        }
                        System.Threading.Thread.Sleep(100);
                    }

                    if (error != null)
                        throw error;
                }

                // --- Extract ---
                EditorUtility.DisplayProgressBar("Vosk Model", "Extracting archive...", 0.8f);
                if (Directory.Exists(extractRoot))
                    Directory.Delete(extractRoot, true);
                Directory.CreateDirectory(extractRoot);
                ZipFile.ExtractToDirectory(zipPath, extractRoot);

                // The zip contains a single top-level folder (vosk-model-small-en-us-0.15);
                // move its *contents* into the target dir so the model files sit directly there.
                string modelSource = extractRoot;
                var topDirs = Directory.GetDirectories(extractRoot);
                var topFiles = Directory.GetFiles(extractRoot);
                if (topDirs.Length == 1 && topFiles.Length == 0)
                    modelSource = topDirs[0];

                EditorUtility.DisplayProgressBar("Vosk Model", "Installing into StreamingAssets...", 0.9f);
                if (Directory.Exists(targetFull))
                    Directory.Delete(targetFull, true);
                Directory.CreateDirectory(targetFull);
                CopyDirectory(modelSource, targetFull);

                AssetDatabase.Refresh();
                Debug.Log($"[VoskModelDownloader] Model installed at {TargetDir}");
                EditorUtility.DisplayDialog("Vosk Model",
                    $"Vosk small English model installed at:\n{TargetDir}", "OK");
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[VoskModelDownloader] Download cancelled.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[VoskModelDownloader] Failed: {e}");
                EditorUtility.DisplayDialog("Vosk Model",
                    $"Download/extract failed:\n{e.Message}\n\nYou can manually download:\n{ModelUrl}\nand extract it into {TargetDir}",
                    "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                try { if (File.Exists(zipPath)) File.Delete(zipPath); } catch { /* ignore */ }
                try { if (Directory.Exists(extractRoot)) Directory.Delete(extractRoot, true); } catch { /* ignore */ }
            }
        }

        private static void CopyDirectory(string source, string dest)
        {
            Directory.CreateDirectory(dest);
            foreach (var file in Directory.GetFiles(source))
                File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), true);
            foreach (var dir in Directory.GetDirectories(source))
                CopyDirectory(dir, Path.Combine(dest, Path.GetFileName(dir)));
        }
    }
}
#endif
