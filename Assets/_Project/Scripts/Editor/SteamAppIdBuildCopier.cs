using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Copies <c>steam_appid.txt</c> next to the executable after every standalone build, whichever
    /// way the build was started (File > Build, Build And Run, or PlayerBuilder). Launched outside
    /// Steam, Steamworks reads the App ID from the working directory; Build And Run starts the player
    /// from the project root, where the file lives, but double-clicking the exe does not.
    /// </summary>
    public sealed class SteamAppIdBuildCopier : IPostprocessBuildWithReport
    {
        private const string k_steamAppIdFile = "steam_appid.txt";

        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            BuildTarget target = report.summary.platform;
            if (target != BuildTarget.StandaloneWindows64 && target != BuildTarget.StandaloneWindows &&
                target != BuildTarget.StandaloneLinux64 && target != BuildTarget.StandaloneOSX)
                return;

            if (!File.Exists(k_steamAppIdFile))
            {
                Debug.LogError($"[Steam] {k_steamAppIdFile} is missing from the project root; " +
                               "the build will not start Steam when launched from its exe.");
                return;
            }

            string buildDirectory = Path.GetDirectoryName(report.summary.outputPath);
            string destination = Path.Combine(buildDirectory, k_steamAppIdFile);
            File.Copy(k_steamAppIdFile, destination, overwrite: true);
            Debug.Log($"[Steam] Copied {k_steamAppIdFile} to {destination}.");
        }
    }
}
