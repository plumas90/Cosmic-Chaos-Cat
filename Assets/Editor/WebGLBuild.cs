using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CosmicChaosCat.Editor
{
    public static class WebGLBuild
    {
        private const string OutputDirectory = "docs";

        [MenuItem("Build/Cosmic Chaos Cat/WebGL")]
        public static void BuildFromMenu()
        {
            Build();
        }

        // CI entry point: -executeMethod CosmicChaosCat.Editor.WebGLBuild.Build
        public static void Build()
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                throw new InvalidOperationException("No enabled scenes were found in Build Settings.");
            }

            Directory.CreateDirectory(OutputDirectory);

            // GitHub Pages cannot provide Unity-specific Content-Encoding headers.
            // Keep the build uncompressed so the browser does not need Unity's
            // JavaScript decompressor for the large data file. Git LFS handles
            // the repository's individual file-size limit.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.dataCaching = true;

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputDirectory,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception(
                    $"WebGL build failed: {report.summary.result} " +
                    $"({report.summary.totalErrors} errors)");
            }

            File.WriteAllText(Path.Combine(OutputDirectory, ".nojekyll"), string.Empty);
            Debug.Log($"WebGL build completed: {Path.GetFullPath(OutputDirectory)}");
        }
    }
}
