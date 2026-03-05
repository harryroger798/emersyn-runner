using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System;
using System.IO;
using System.Linq;

public static class BuildAndroid
{
    private const string OutputPath = "Builds/EmersynRunner-arm64.apk";
    private const string LogPath = "Builds/logs/unity_build.log";

    [MenuItem("Build/Build Android APK")]
    public static void Build()
    {
        Debug.Log("[BuildAndroid] Starting Android APK build...");

        // Ensure output directories exist
        string buildDir = Path.GetDirectoryName(OutputPath);
        string logDir = Path.GetDirectoryName(LogPath);
        if (!Directory.Exists(buildDir)) Directory.CreateDirectory(buildDir);
        if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);

        // Gather all enabled scenes
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            // Fallback: find main scene
            scenes = new[] { "Assets/Scenes/MainScene.unity" };
            Debug.LogWarning("[BuildAndroid] No scenes in build settings, using fallback: " + scenes[0]);
        }

        Debug.Log("[BuildAndroid] Building scenes: " + string.Join(", ", scenes));

        // Configure build options
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutputPath,
            target = BuildTarget.Android,
            options = BuildOptions.CompressWithLz4HC
        };

        // Set Android-specific settings
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel33;

        // Set application identifier
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.emersynGames.emersynrunner");
        PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.Android.bundleVersionCode = 1;

        // Check for keystore environment variables
        string keystorePath = Environment.GetEnvironmentVariable("KEYSTORE_FILE");
        string keystoreAlias = Environment.GetEnvironmentVariable("KEYSTORE_ALIAS");
        string keystorePass = Environment.GetEnvironmentVariable("KEYSTORE_PASSWORD");
        string keyPass = Environment.GetEnvironmentVariable("KEY_PASSWORD");

        if (!string.IsNullOrEmpty(keystorePath) && File.Exists(keystorePath))
        {
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystorePath;
            PlayerSettings.Android.keystorePass = keystorePass;
            PlayerSettings.Android.keyaliasName = keystoreAlias;
            PlayerSettings.Android.keyaliasPass = keyPass;
            Debug.Log("[BuildAndroid] Using custom keystore for signing.");
        }
        else
        {
            PlayerSettings.Android.useCustomKeystore = false;
            Debug.Log("[BuildAndroid] Using debug keystore (no custom keystore found).");
        }

        // Execute the build
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        // Write build log
        using (StreamWriter writer = new StreamWriter(LogPath))
        {
            writer.WriteLine("=== Emersyn Runner Android Build Log ===");
            writer.WriteLine($"Build Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine($"Result: {summary.result}");
            writer.WriteLine($"Platform: {summary.platform}");
            writer.WriteLine($"Output Path: {summary.outputPath}");
            writer.WriteLine($"Total Size: {summary.totalSize} bytes");
            writer.WriteLine($"Total Time: {summary.totalTime}");
            writer.WriteLine($"Total Errors: {summary.totalErrors}");
            writer.WriteLine($"Total Warnings: {summary.totalWarnings}");
            writer.WriteLine();

            if (report.steps != null)
            {
                foreach (var step in report.steps)
                {
                    writer.WriteLine($"Step: {step.name} ({step.duration})");
                    foreach (var msg in step.messages)
                    {
                        writer.WriteLine($"  [{msg.type}] {msg.content}");
                    }
                }
            }
        }

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildAndroid] Build succeeded! APK: {summary.outputPath} ({summary.totalSize} bytes)");
        }
        else
        {
            Debug.LogError($"[BuildAndroid] Build FAILED with {summary.totalErrors} errors.");
            EditorApplication.Exit(1);
        }
    }
}
