using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Solarpunk.EditorTools
{
    /// <summary>
    /// Builds a windowed Windows player, used both for playtesting and for the
    /// -autoshot screenshot harness.
    /// </summary>
    public static class PlayerBuilder
    {
        private const string OutputPath = "Builds/Solarpunk/Solarpunk.exe";

        [MenuItem("Solarpunk/Build Windows Player")]
        public static void BuildWindowsPlayer()
        {
            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/_Game/Scenes/Game.unity" },
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[Build] Failed: {summary.result}, {summary.totalErrors} error(s).");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"[Build] Player written to {OutputPath} ({summary.totalSize / 1048576} MB).");
        }
    }
}
