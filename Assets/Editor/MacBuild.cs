using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace OnLinces.EditorTools
{
    /// Build de escritorio para macOS en Builds/macOS.
    /// Terminal: Unity -batchmode -quit -projectPath . -executeMethod OnLinces.EditorTools.MacBuild.Build
    public static class MacBuild
    {
        [MenuItem("OnLinces/Build macOS")]
        public static void Build()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "Builds/macOS/OnLinces-Dungeon.app",
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None,
            });
            Debug.Log($"MacBuild: {report.summary.result} {report.summary.totalSize / (1024 * 1024)} MB, {report.summary.totalErrors} errores");
            if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}
