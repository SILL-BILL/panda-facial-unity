using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SillBill.PandaFacial.Build
{
    public static class PandaFacialPackageBuilder
    {
        private const string PackageName = "com.sillbill.panda-facial";
        private const string StagingRoot = "Assets/PandaFacial";

        [Serializable]
        private sealed class PackageManifest
        {
            public string name;
            public string version;
        }

        [MenuItem("Tools/Panda Facial/Build UnityPackage")]
        public static void Build()
        {
            string repositoryRoot = FindRepositoryRoot();
            PackageManifest manifest = ReadManifest(repositoryRoot);
            string outputDirectory = Path.Combine(repositoryRoot, "dist");
            string outputPath = Path.Combine(
                outputDirectory,
                "PandaFacial-" + manifest.version + ".unitypackage");

            Directory.CreateDirectory(outputDirectory);
            EnsureStagingDoesNotExist();
            bool stagingCreated = false;

            try
            {
                Directory.CreateDirectory(Path.GetFullPath(StagingRoot));
                stagingCreated = true;
                StageDirectory(repositoryRoot, "Runtime");
                StageDirectory(repositoryRoot, "Editor");
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                ValidateStaging();

                if (File.Exists(outputPath))
                    File.Delete(outputPath);

                AssetDatabase.ExportPackage(
                    StagingRoot,
                    outputPath,
                    ExportPackageOptions.Recurse);

                ValidateOutput(outputPath, manifest.version);
                Debug.Log("Panda Facial UnityPackage built: " + outputPath);
            }
            finally
            {
                if (stagingCreated)
                    DeleteStagingDirectory();
            }
        }

        private static string FindRepositoryRoot()
        {
            UnityEditor.PackageManager.PackageInfo buildPackage =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(PandaFacialPackageBuilder).Assembly);
            if (buildPackage == null)
                throw new InvalidOperationException("Cannot locate the Panda Facial build tool package.");

            DirectoryInfo directory = new DirectoryInfo(buildPackage.resolvedPath);
            while (directory != null)
            {
                string manifestPath = Path.Combine(directory.FullName, "package.json");
                if (File.Exists(manifestPath))
                {
                    PackageManifest manifest = JsonUtility.FromJson<PackageManifest>(File.ReadAllText(manifestPath));
                    if (manifest != null && manifest.name == PackageName)
                        return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException("Cannot locate the Panda Facial repository root.");
        }

        private static PackageManifest ReadManifest(string repositoryRoot)
        {
            string path = Path.Combine(repositoryRoot, "package.json");
            PackageManifest manifest = JsonUtility.FromJson<PackageManifest>(File.ReadAllText(path));
            if (manifest == null || manifest.name != PackageName || string.IsNullOrWhiteSpace(manifest.version))
                throw new InvalidDataException("The Panda Facial package.json name or version is invalid.");
            if (manifest.version.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new InvalidDataException("The package version cannot be used in an artifact filename.");
            return manifest;
        }

        private static void StageDirectory(string repositoryRoot, string directoryName)
        {
            string source = Path.Combine(repositoryRoot, directoryName);
            if (!Directory.Exists(source))
                throw new DirectoryNotFoundException("Required package directory was not found: " + source);

            FileUtil.CopyFileOrDirectory(source, StagingRoot + "/" + directoryName);
        }

        private static void ValidateStaging()
        {
            string absoluteStagingRoot = Path.GetFullPath(StagingRoot);
            string[] files = Directory.GetFiles(absoluteStagingRoot, "*", SearchOption.AllDirectories);
            if (!files.Any(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException("The staging directory contains no C# source files.");

            string[] forbiddenNames =
            {
                "Development~", "Tests", "LocalOnly", "Miyuki", "dist", ".git", "AGENTS.md"
            };
            foreach (string file in files)
            {
                string relativePath = file.Substring(absoluteStagingRoot.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (forbiddenNames.Any(name =>
                    relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        .Any(segment => string.Equals(segment, name, StringComparison.OrdinalIgnoreCase))))
                {
                    throw new InvalidDataException("Forbidden content entered the staging directory: " + relativePath);
                }
            }
        }

        private static void ValidateOutput(string outputPath, string version)
        {
            if (!File.Exists(outputPath))
                throw new IOException("UnityPackage output was not created: " + outputPath);
            if (new FileInfo(outputPath).Length <= 0)
                throw new IOException("UnityPackage output is empty: " + outputPath);

            string expectedFilename = "PandaFacial-" + version + ".unitypackage";
            if (!string.Equals(Path.GetFileName(outputPath), expectedFilename, StringComparison.Ordinal))
                throw new InvalidDataException("UnityPackage filename does not match package.json version.");
        }

        private static void DeleteStagingDirectory()
        {
            if (AssetDatabase.IsValidFolder(StagingRoot))
                AssetDatabase.DeleteAsset(StagingRoot);

            string absolutePath = Path.GetFullPath(StagingRoot);
            if (Directory.Exists(absolutePath))
                FileUtil.DeleteFileOrDirectory(absolutePath);
            if (File.Exists(absolutePath + ".meta"))
                FileUtil.DeleteFileOrDirectory(absolutePath + ".meta");

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void EnsureStagingDoesNotExist()
        {
            string absolutePath = Path.GetFullPath(StagingRoot);
            if (AssetDatabase.IsValidFolder(StagingRoot) || Directory.Exists(absolutePath) ||
                File.Exists(absolutePath + ".meta"))
            {
                throw new IOException(
                    "Cannot build because the staging destination already exists: " + absolutePath);
            }
        }
    }
}
