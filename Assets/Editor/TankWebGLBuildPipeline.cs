using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TankWebGLBuildPipeline
{
    public const string BuildOutputDirectory = "Builds/WebGL";
    public const string BuildManifestPath = "Builds/WebGL_BUILD_MANIFEST.json";
    public const string DeploymentRoot = "pythonanywhere_flask";
    public const string DeploymentWebGLDirectory = "pythonanywhere_flask/webgl";
    public const string DeploymentManifestPath = "pythonanywhere_flask/WEBGL_BUILD_MANIFEST.json";
    public const string ZipOutputPath = "tank_game_pythonanywhere.zip";

    private static readonly string[] RequiredBuildPatterns =
    {
        "index.html",
        "Build/*.loader.js",
        "Build/*.framework.js",
        "Build/*.wasm",
        "Build/*.data"
    };

    [MenuItem("Silly Tank/Configure WebGL")]
    public static void ConfigureWebGL()
    {
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;
        PlayerSettings.WebGL.threadsSupport = false;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.stripEngineCode = false;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, true);

        foreach (Camera camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
        {
            camera.allowHDR = false;
            camera.allowMSAA = false;
            EditorUtility.SetDirty(camera);
        }

        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("Configured Silly Tank WebGL settings for direct Chrome/Safari/PythonAnywhere hosting.");
    }

    [MenuItem("Silly Tank/Build WebGL And PythonAnywhere Zip")]
    public static void BuildWebGLAndPackage()
    {
        ConfigureWebGL();
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled && !string.IsNullOrWhiteSpace(scene.path))
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new InvalidOperationException("No enabled scenes are configured for the WebGL build.");
        }

        Directory.CreateDirectory(BuildOutputDirectory);
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = BuildOutputDirectory,
            target = BuildTarget.WebGL,
            options = BuildOptions.CleanBuildCache
        });

        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException("WebGL build failed: " + report.summary.result);
        }

        ValidateRequiredBuildFiles(BuildOutputDirectory);
        WriteManifest(BuildOutputDirectory, BuildManifestPath, report.summary.totalWarnings);
        RefreshDeploymentCopy();
        WriteManifest(DeploymentWebGLDirectory, DeploymentManifestPath, report.summary.totalWarnings);
        CreateDeploymentZip();

        Debug.Log($"Silly Tank WebGL build succeeded. Output={Path.GetFullPath(BuildOutputDirectory)} Size={report.summary.totalSize} Warnings={report.summary.totalWarnings}");
    }

    public static void BuildFromCommandLine()
    {
        BuildWebGLAndPackage();
    }

    public static void ValidateRequiredBuildFiles(string root)
    {
        foreach (string pattern in RequiredBuildPatterns)
        {
            string directory = Path.Combine(root, Path.GetDirectoryName(pattern) ?? string.Empty);
            string filePattern = Path.GetFileName(pattern);
            if (!Directory.Exists(directory) || Directory.GetFiles(directory, filePattern, SearchOption.TopDirectoryOnly).Length == 0)
            {
                throw new FileNotFoundException($"Required WebGL build output is missing: {pattern}");
            }
        }
    }

    private static void RefreshDeploymentCopy()
    {
        if (!File.Exists(Path.Combine(DeploymentRoot, "flask_app.py")))
        {
            throw new FileNotFoundException("PythonAnywhere Flask app is missing.", Path.Combine(DeploymentRoot, "flask_app.py"));
        }

        if (Directory.Exists(DeploymentWebGLDirectory))
        {
            Directory.Delete(DeploymentWebGLDirectory, true);
        }

        CopyDirectory(BuildOutputDirectory, DeploymentWebGLDirectory);
    }

    private static void CreateDeploymentZip()
    {
        if (File.Exists(ZipOutputPath))
        {
            File.Delete(ZipOutputPath);
        }

        using (ZipArchive archive = ZipFile.Open(ZipOutputPath, ZipArchiveMode.Create))
        {
            foreach (string file in Directory.GetFiles(DeploymentRoot, "*", SearchOption.AllDirectories)
                         .Where(ShouldIncludeDeploymentFile))
            {
                string entryName = Path.GetRelativePath(DeploymentRoot, file).Replace('\\', '/');
                archive.CreateEntryFromFile(file, entryName, System.IO.Compression.CompressionLevel.Optimal);
            }
        }
        Debug.Log($"Created PythonAnywhere ZIP: {Path.GetFullPath(ZipOutputPath)} ({new FileInfo(ZipOutputPath).Length} bytes)");
    }

    public static bool ShouldIncludeDeploymentFile(string path)
    {
        string normalized = path.Replace('\\', '/');
        return !normalized.Contains("/__pycache__/", StringComparison.OrdinalIgnoreCase) &&
               !normalized.EndsWith(".pyc", StringComparison.OrdinalIgnoreCase);
    }

    private static void CopyDirectory(string source, string destination)
    {
        foreach (string directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(source, destination));
        }

        Directory.CreateDirectory(destination);
        foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            string destinationFile = file.Replace(source, destination);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile) ?? destination);
            File.Copy(file, destinationFile, true);
        }
    }

    private static void WriteManifest(string root, string outputPath, int warningCount)
    {
        List<BuildFileEntry> files = Directory.GetFiles(root, "*", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => new BuildFileEntry
            {
                path = Path.GetRelativePath(root, path).Replace('\\', '/'),
                sizeBytes = new FileInfo(path).Length,
                sha256 = ComputeSha256(path)
            })
            .ToList();

        BuildManifest manifest = new BuildManifest
        {
            generatedUtc = DateTime.UtcNow.ToString("O"),
            unityVersion = Application.unityVersion,
            warningCount = warningCount,
            fileCount = files.Count,
            totalSizeBytes = files.Sum(file => file.sizeBytes),
            files = files.ToArray()
        };

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
        File.WriteAllText(outputPath, JsonUtility.ToJson(manifest, true));
        Debug.Log($"Wrote manifest {outputPath} with {manifest.fileCount} files.");
        foreach (BuildFileEntry file in files)
        {
            Debug.Log($"WebGL output: {file.path} ({file.sizeBytes} bytes)");
        }
    }

    private static string ComputeSha256(string path)
    {
        using SHA256 sha = SHA256.Create();
        using FileStream stream = File.OpenRead(path);
        return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
    }

    [Serializable]
    private class BuildManifest
    {
        public string generatedUtc;
        public string unityVersion;
        public int warningCount;
        public int fileCount;
        public long totalSizeBytes;
        public BuildFileEntry[] files;
    }

    [Serializable]
    private class BuildFileEntry
    {
        public string path;
        public long sizeBytes;
        public string sha256;
    }
}
