/*
    Electron2D
    MIT License
    Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
    SPDX-License-Identifier: MIT

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Electron2D;

internal static class WindowsPackageBuilder
{
    private const int FormatVersion = 1;

    private static readonly JsonSerializerOptions IndentedJsonOptions = new()
    {
        WriteIndented = true
    };

    public static WindowsPackageBuildResult Build(
        WindowsExportPlan plan,
        string projectRoot,
        Electron2DProjectSettings projectSettings,
        string projectAssemblyFileName,
        string rootNamespace,
        string targetFramework)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentNullException.ThrowIfNull(projectSettings);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectAssemblyFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootNamespace);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetFramework);

        var diagnostics = new List<Electron2DExportDiagnostic>();
        var packages = new List<ResourcePackageManifestEntry>();
        try
        {
            Directory.CreateDirectory(plan.OutputDirectory);
            Directory.CreateDirectory(Path.Combine(plan.OutputDirectory, "packs"));

            AddProjectPackage(plan, projectRoot, packages, diagnostics);
            AddScenePackages(plan, projectRoot, packages, diagnostics);
            AddAssetPackages(plan, projectRoot, packages, diagnostics);
            AddResourcesPackage(plan, projectRoot, packages, diagnostics);
            WriteManifest(plan, projectRoot, projectSettings, projectAssemblyFileName, rootNamespace, targetFramework, packages);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-WINDOWS-0008",
                "windows-package",
                $"Windows resource packages could not be written: {exception.Message}"));
        }

        return new WindowsPackageBuildResult(
            [NormalizePortablePath(Path.GetRelativePath(plan.OutputDirectory, plan.ResourceManifestPath)), .. packages.SelectMany(package => package.OutputFiles)],
            diagnostics);
    }

    private static void AddProjectPackage(
        WindowsExportPlan plan,
        string projectRoot,
        List<ResourcePackageManifestEntry> packages,
        List<Electron2DExportDiagnostic> diagnostics)
    {
        var packagePath = Path.Combine(plan.OutputDirectory, "packs", "project.e2dpkg");
        var projectSettingsEntry = NormalizePortablePath(Path.GetRelativePath(projectRoot, plan.ProjectSettingsPath));
        var entries = AddPackage(
            packagePath,
            projectRoot,
            [projectSettingsEntry],
            diagnostics);
        if (entries.Count > 0)
        {
            packages.Add(ResourcePackageManifestEntry.Create(
                "project",
                "project",
                NormalizePortablePath(Path.GetRelativePath(plan.OutputDirectory, packagePath)),
                autoload: true,
                scene: string.Empty,
                entries));
        }
    }

    private static void AddScenePackages(
        WindowsExportPlan plan,
        string projectRoot,
        List<ResourcePackageManifestEntry> packages,
        List<Electron2DExportDiagnostic> diagnostics)
    {
        var scenesRoot = Path.Combine(projectRoot, "scenes");
        if (!Directory.Exists(scenesRoot))
        {
            return;
        }

        foreach (var sceneFile in Directory.EnumerateFiles(scenesRoot, "*", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal))
        {
            var sceneRelative = NormalizePortablePath(Path.GetRelativePath(projectRoot, sceneFile));
            if (IsForbiddenPackageEntry(sceneRelative))
            {
                continue;
            }

            var packagePath = GetScenePackagePath(plan.OutputDirectory, sceneRelative);
            var entries = AddPackage(packagePath, projectRoot, [sceneRelative], diagnostics);
            if (entries.Count == 0)
            {
                continue;
            }

            packages.Add(ResourcePackageManifestEntry.Create(
                "scene:" + sceneRelative,
                "scene",
                NormalizePortablePath(Path.GetRelativePath(plan.OutputDirectory, packagePath)),
                autoload: string.Equals(sceneRelative, NormalizePortablePath(plan.ResourcePackEntries
                    .FirstOrDefault(entry => entry.Contains("::", StringComparison.Ordinal))?
                    .Split("::", 2)[1] ?? string.Empty), StringComparison.Ordinal),
                sceneRelative,
                entries));
        }
    }

    private static void AddAssetPackages(
        WindowsExportPlan plan,
        string projectRoot,
        List<ResourcePackageManifestEntry> packages,
        List<Electron2DExportDiagnostic> diagnostics)
    {
        var assetsRoot = Path.Combine(projectRoot, "assets");
        if (!Directory.Exists(assetsRoot))
        {
            return;
        }

        var groups = Directory.EnumerateFiles(assetsRoot, "*", SearchOption.AllDirectories)
            .Select(path => NormalizePortablePath(Path.GetRelativePath(projectRoot, path)))
            .Where(path => !IsForbiddenPackageEntry(path))
            .GroupBy(GetAssetGroupName, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal);
        foreach (var group in groups)
        {
            var packagePath = Path.Combine(plan.OutputDirectory, "packs", "assets", group.Key + ".e2dpkg");
            var entries = AddPackage(packagePath, projectRoot, group.OrderBy(path => path, StringComparer.Ordinal).ToArray(), diagnostics);
            if (entries.Count == 0)
            {
                continue;
            }

            packages.Add(ResourcePackageManifestEntry.Create(
                "assets:" + group.Key,
                "assets",
                NormalizePortablePath(Path.GetRelativePath(plan.OutputDirectory, packagePath)),
                autoload: false,
                scene: string.Empty,
                entries));
        }
    }

    private static void AddResourcesPackage(
        WindowsExportPlan plan,
        string projectRoot,
        List<ResourcePackageManifestEntry> packages,
        List<Electron2DExportDiagnostic> diagnostics)
    {
        var resourcesRoot = Path.Combine(projectRoot, "resources");
        if (!Directory.Exists(resourcesRoot))
        {
            return;
        }

        var resourceEntries = Directory.EnumerateFiles(resourcesRoot, "*", SearchOption.AllDirectories)
            .Select(path => NormalizePortablePath(Path.GetRelativePath(projectRoot, path)))
            .Where(path => !IsForbiddenPackageEntry(path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        if (resourceEntries.Length == 0)
        {
            return;
        }

        var packagePath = Path.Combine(plan.OutputDirectory, "packs", "resources.e2dpkg");
        var entries = AddPackage(packagePath, projectRoot, resourceEntries, diagnostics);
        if (entries.Count > 0)
        {
            packages.Add(ResourcePackageManifestEntry.Create(
                "resources",
                "resources",
                NormalizePortablePath(Path.GetRelativePath(plan.OutputDirectory, packagePath)),
                autoload: true,
                scene: string.Empty,
                entries));
        }
    }

    private static IReadOnlyList<string> AddPackage(
        string packagePath,
        string projectRoot,
        IReadOnlyList<string> relativeEntries,
        List<Electron2DExportDiagnostic> diagnostics)
    {
        var packageDirectory = Path.GetDirectoryName(packagePath);
        if (!string.IsNullOrWhiteSpace(packageDirectory))
        {
            Directory.CreateDirectory(packageDirectory);
        }

        if (File.Exists(packagePath))
        {
            File.Delete(packagePath);
        }

        var writtenEntries = new List<string>();
        using var archive = ZipFile.Open(packagePath, ZipArchiveMode.Create);
        foreach (var relativePath in relativeEntries)
        {
            var normalized = NormalizePortablePath(relativePath);
            if (IsForbiddenPackageEntry(normalized))
            {
                continue;
            }

            if (!TryResolveProjectFile(projectRoot, normalized, out var source))
            {
                diagnostics.Add(Error("E2D-EXPORT-WINDOWS-0009", "windows-package", $"Project file '{normalized}' must stay inside the project root."));
                continue;
            }

            if (!File.Exists(source))
            {
                diagnostics.Add(Error("E2D-EXPORT-WINDOWS-0010", "windows-package", $"Project file '{normalized}' was not found."));
                continue;
            }

            var entry = archive.CreateEntry(normalized, CompressionLevel.SmallestSize);
            using var input = File.OpenRead(source);
            using var output = entry.Open();
            input.CopyTo(output);
            writtenEntries.Add(normalized);
        }

        return writtenEntries;
    }

    private static void WriteManifest(
        WindowsExportPlan plan,
        string projectRoot,
        Electron2DProjectSettings settings,
        string projectAssemblyFileName,
        string rootNamespace,
        string targetFramework,
        IReadOnlyList<ResourcePackageManifestEntry> packages)
    {
        var manifest = new JsonObject
        {
            ["format"] = "Electron2D.ResourcePackManifest",
            ["formatVersion"] = FormatVersion,
            ["projectName"] = settings.Name,
            ["projectVersion"] = settings.ProjectVersion,
            ["engineVersion"] = settings.EngineVersion,
            ["projectFile"] = NormalizePortablePath(Path.GetRelativePath(projectRoot, plan.ProjectSettingsPath)),
            ["mainScene"] = NormalizePortablePath(settings.MainScene),
            ["projectAssembly"] = projectAssemblyFileName,
            ["rootNamespace"] = rootNamespace,
            ["targetFramework"] = targetFramework,
            ["rendererProfile"] = plan.RendererProfile.ToString(),
            ["graphicsBackend"] = plan.GraphicsBackend,
            ["runtimeIdentifier"] = plan.RuntimeIdentifier,
            ["packs"] = WritePackages(packages)
        };

        var directory = Path.GetDirectoryName(plan.ResourceManifestPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(
            plan.ResourceManifestPath,
            manifest.ToJsonString(IndentedJsonOptions).Replace("\r\n", "\n", StringComparison.Ordinal));
    }

    private static JsonArray WritePackages(IEnumerable<ResourcePackageManifestEntry> packages)
    {
        var result = new JsonArray();
        foreach (var package in packages.OrderBy(package => package.Path, StringComparer.Ordinal))
        {
            result.Add(new JsonObject
            {
                ["id"] = package.Id,
                ["kind"] = package.Kind,
                ["path"] = package.Path,
                ["autoload"] = package.Autoload,
                ["scene"] = string.IsNullOrWhiteSpace(package.Scene) ? null : package.Scene,
                ["entries"] = WriteStringArray(package.Entries)
            });
        }

        return result;
    }

    private static string GetScenePackagePath(string outputDirectory, string scenePath)
    {
        var relativeWithoutExtension = NormalizePortablePath(scenePath);
        if (relativeWithoutExtension.StartsWith("scenes/", StringComparison.OrdinalIgnoreCase))
        {
            relativeWithoutExtension = relativeWithoutExtension["scenes/".Length..];
        }

        relativeWithoutExtension = relativeWithoutExtension.EndsWith(".scene.json", StringComparison.OrdinalIgnoreCase)
            ? relativeWithoutExtension[..^".scene.json".Length]
            : Path.ChangeExtension(relativeWithoutExtension, null) ?? relativeWithoutExtension;

        return Path.Combine(
            outputDirectory,
            "packs",
            "scenes",
            (relativeWithoutExtension + ".e2dpkg").Replace('/', Path.DirectorySeparatorChar));
    }

    private static string GetAssetGroupName(string relativeAssetPath)
    {
        var withoutPrefix = relativeAssetPath.StartsWith("assets/", StringComparison.Ordinal)
            ? relativeAssetPath["assets/".Length..]
            : relativeAssetPath;
        var firstSlash = withoutPrefix.IndexOf('/');
        var name = firstSlash < 0 ? "root" : withoutPrefix[..firstSlash];
        return string.IsNullOrWhiteSpace(name) ? "root" : name;
    }

    private static bool IsForbiddenPackageEntry(string entryName)
    {
        return entryName.StartsWith(".electron2d/tasks/", StringComparison.Ordinal)
            || entryName.Equals("export_presets.e2export.json", StringComparison.Ordinal)
            || entryName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
            || entryName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryResolveProjectFile(string projectRoot, string relativePath, out string fullPath)
    {
        fullPath = Path.GetFullPath(Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var normalizedRoot = Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
            Path.DirectorySeparatorChar;
        return fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static JsonArray WriteStringArray(IEnumerable<string> values)
    {
        var result = new JsonArray();
        foreach (var value in values)
        {
            result.Add(value);
        }

        return result;
    }

    private static string NormalizePortablePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private static Electron2DExportDiagnostic Error(string code, string presetName, string message)
    {
        return new Electron2DExportDiagnostic(code, message, Electron2DExportDiagnosticSeverity.Error, presetName);
    }

    private sealed record ResourcePackageManifestEntry(
        string Id,
        string Kind,
        string Path,
        bool Autoload,
        string Scene,
        IReadOnlyList<string> Entries,
        IReadOnlyList<string> OutputFiles)
    {
        public static ResourcePackageManifestEntry Create(
            string id,
            string kind,
            string path,
            bool autoload,
            string scene,
            IReadOnlyList<string> entries)
        {
            var outputFiles = entries
                .Select(entry => $"{path}::{entry}")
                .Prepend(path)
                .ToArray();
            return new ResourcePackageManifestEntry(id, kind, path, autoload, scene, entries, outputFiles);
        }
    }
}
