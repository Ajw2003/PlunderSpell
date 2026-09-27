using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>Produces a standalone Windows player build. See docs/plans/GitIssues/Issue_53_Plan.md.</summary>
    public static class PlayerBuilder
    {
        private const string k_outputDirectory = "Build/Windows";
        private const string k_executableName = "Plunderspell.exe";
        private const string k_steamAppIdFile = "steam_appid.txt";

        [MenuItem("Tools/Plunderspell/Build Standalone Player")]
        public static void BuildFromMenu()
        {
            string path = Build();
            EditorUtility.RevealInFinder(path);
        }

        /// <summary>Builds the player and returns the path to the produced executable. Public so it
        /// can also be invoked headlessly via <c>-executeMethod</c>.</summary>
        public static string Build()
        {
            string outputPath = Path.Combine(k_outputDirectory, k_executableName);
            Directory.CreateDirectory(k_outputDirectory);

            var options = new BuildPlayerOptions
            {
                // The whole game lives in one scene, driven by Plunderspell.Core.GameState
                // (MainMenu/Lair/Playing/...) rather than a scene per screen — see
                // docs/6-decisions/Decisions.md, "The standalone build is one scene, not one per screen".
                scenes = new[] { "Assets/_Project/Scenes/RaidScene.unity" },
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"Build failed: {report.summary.result}, " +
                    $"{report.summary.totalErrors} error(s). See the Editor log for details.");
            }

            // Launched outside Steam (double-clicking the exe), Steamworks reads the App ID from this
            // file next to the executable; without it SteamAPI.Init fails and co-op is off.
            File.Copy(k_steamAppIdFile, Path.Combine(k_outputDirectory, k_steamAppIdFile), overwrite: true);

            Debug.Log($"Build succeeded: {outputPath} ({report.summary.totalSize} bytes)");
            return outputPath;
        }
    }
}
