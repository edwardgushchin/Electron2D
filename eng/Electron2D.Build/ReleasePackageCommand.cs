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
using System.Formats.Tar;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Electron2D.Build;

internal sealed class ReleasePackageCommand(string repositoryRoot, JsonDiagnosticSink diagnostics, ProcessRunner processRunner)
{
    private const string Version = "0.1.0-preview";
    private const string Configuration = "Release";
    private const int TimeoutSeconds = 1800;

    private static readonly JsonSerializerOptions ManifestJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private static readonly ReleaseTarget[] SupportedTargets =
    [
        new("win-x64", "electron2d-0.1.0-preview-win-x64.zip", "zip"),
        new("linux-x64", "electron2d-0.1.0-preview-linux-x64.tar.gz", "tar.gz"),
        new("osx-arm64", "electron2d-0.1.0-preview-osx-arm64.tar.gz", "tar.gz")
    ];

    private static readonly string[] ManifestForbiddenPaths =
    [
        ".git/",
        ".github/",
        ".temp/",
        ".codex/",
        "TASKS.md",
        "dev-diary/",
        "completed-tasks/",
        "data/dev-diary/",
        "data/completed-tasks/",
        "CHANGELOG*",
        "RELEASE-NOTES*",
        "*.ps1",
        "eng/Electron2D.Build/",
        "docs/verdicts/",
        "audit-evidence/",
        "artifacts/",
        "*.zip",
        "*.tar.gz",
        "*.sha256"
    ];

    public async Task<int> PackageAsync(string rid, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var target = FindTarget(rid);
        if (target is null)
        {
            diagnostics.Write(new BuildDiagnostic(
                "package",
                "package",
                "error",
                "E2D-BUILD-PACKAGE-RID-UNSUPPORTED",
                $"Runtime identifier '{rid}' is not supported for release packaging.",
                RuntimeIdentifier: rid));
            return RepositoryBuildExitCodes.Failed;
        }

        var ridRoot = GetRidRoot(target.RuntimeIdentifier);
        var packageRoot = Path.Combine(ridRoot, "package");
        var archivePath = Path.Combine(ridRoot, target.ArchiveName);
        var checksumPath = archivePath + ".sha256";

        if (Directory.Exists(packageRoot))
        {
            Directory.Delete(packageRoot, recursive: true);
        }

        Directory.CreateDirectory(packageRoot);
        Directory.CreateDirectory(Path.Combine(packageRoot, "library"));
        Directory.CreateDirectory(Path.Combine(packageRoot, "editor"));
        Directory.CreateDirectory(Path.Combine(packageRoot, "tools", "e2d"));

        if (!CopyRequiredRootFile("README.md", packageRoot) ||
            !CopyRequiredRootFile("LICENSE", packageRoot))
        {
            return RepositoryBuildExitCodes.Failed;
        }

        var runtimePackageResult = await RunDotnetStepAsync(
            "package runtime library",
            "E2D-BUILD-PACKAGE-PACK-FAILED",
            "src/Electron2D/Electron2D.csproj",
            [
                "pack",
                "src/Electron2D/Electron2D.csproj",
                "-c",
                Configuration,
                "-o",
                Path.Combine(packageRoot, "library")
            ],
            cancellationToken).ConfigureAwait(false);
        if (runtimePackageResult != RepositoryBuildExitCodes.Success)
        {
            return runtimePackageResult;
        }

        var editorPublishResult = await RunDotnetStepAsync(
            "package editor",
            "E2D-BUILD-PACKAGE-PUBLISH-FAILED",
            "src/Electron2D.Editor/Electron2D.Editor.csproj",
            CreatePublishArguments("src/Electron2D.Editor/Electron2D.Editor.csproj", target.RuntimeIdentifier, Path.Combine(packageRoot, "editor")),
            cancellationToken).ConfigureAwait(false);
        if (editorPublishResult != RepositoryBuildExitCodes.Success)
        {
            return editorPublishResult;
        }

        var cliPublishResult = await RunDotnetStepAsync(
            "package e2d",
            "E2D-BUILD-PACKAGE-PUBLISH-FAILED",
            "src/Electron2D.Cli/Electron2D.Cli.csproj",
            CreatePublishArguments("src/Electron2D.Cli/Electron2D.Cli.csproj", target.RuntimeIdentifier, Path.Combine(packageRoot, "tools", "e2d")),
            cancellationToken).ConfigureAwait(false);
        if (cliPublishResult != RepositoryBuildExitCodes.Success)
        {
            return cliPublishResult;
        }

        WriteReleaseManifest(packageRoot, target);

        var forbiddenStagingPaths = FindForbiddenPackagePaths(packageRoot).ToArray();
        if (forbiddenStagingPaths.Length > 0)
        {
            WriteForbiddenPathDiagnostic("package", "package", forbiddenStagingPaths[0], packageRoot);
            return RepositoryBuildExitCodes.Failed;
        }

        if (File.Exists(archivePath))
        {
            File.Delete(archivePath);
        }

        if (File.Exists(checksumPath))
        {
            File.Delete(checksumPath);
        }

        if (target.ArchiveType == "zip")
        {
            CreateZipArchive(packageRoot, archivePath);
        }
        else
        {
            CreateTarGzArchive(packageRoot, archivePath);
        }

        var archiveSha256 = Sha256File(archivePath);
        File.WriteAllText(checksumPath, $"{archiveSha256}  {target.ArchiveName}{Environment.NewLine}", Encoding.UTF8);

        diagnostics.Write(new BuildDiagnostic(
            "package",
            "package",
            "info",
            "E2D-BUILD-PACKAGE-CREATED",
            $"Release package created for '{target.RuntimeIdentifier}'.",
            RuntimeIdentifier: target.RuntimeIdentifier,
            Path: ToRepositoryPath(archivePath),
            OutputPath: ToRepositoryPath(ridRoot)));
        return RepositoryBuildExitCodes.Success;
    }

    public Task<int> VerifyAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var target in SupportedTargets)
        {
            var result = VerifyTarget(target);
            if (result != RepositoryBuildExitCodes.Success)
            {
                return Task.FromResult(result);
            }
        }

        diagnostics.Write(new BuildDiagnostic(
            "release",
            "release verify",
            "info",
            "E2D-BUILD-RELEASE-VERIFY-PASSED",
            $"Local release artifacts for version {Version} passed verification.",
            OutputPath: ToRepositoryPath(GetReleaseRoot())));
        return Task.FromResult(RepositoryBuildExitCodes.Success);
    }

    private int VerifyTarget(ReleaseTarget target)
    {
        var ridRoot = GetRidRoot(target.RuntimeIdentifier);
        var packageRoot = Path.Combine(ridRoot, "package");
        var archivePath = Path.Combine(ridRoot, target.ArchiveName);
        var checksumPath = archivePath + ".sha256";
        var manifestPath = Path.Combine(packageRoot, "release-manifest.json");

        foreach (var requiredPath in new[] { packageRoot, archivePath, checksumPath, manifestPath })
        {
            if (!PathExists(requiredPath))
            {
                diagnostics.Write(new BuildDiagnostic(
                    "release",
                    "release verify",
                    "error",
                    "E2D-BUILD-RELEASE-ARTIFACT-MISSING",
                    $"Required release artifact path is missing: {ToRepositoryPath(requiredPath)}.",
                    RuntimeIdentifier: target.RuntimeIdentifier,
                    Path: ToRepositoryPath(requiredPath)));
                return RepositoryBuildExitCodes.Failed;
            }
        }

        var expectedChecksum = $"{Sha256File(archivePath)}  {target.ArchiveName}";
        var actualChecksum = File.ReadAllText(checksumPath, Encoding.UTF8).Trim();
        if (!string.Equals(expectedChecksum, actualChecksum, StringComparison.Ordinal))
        {
            diagnostics.Write(new BuildDiagnostic(
                "release",
                "release verify",
                "error",
                "E2D-BUILD-RELEASE-CHECKSUM-MISMATCH",
                $"Checksum file does not match archive SHA-256 for '{target.ArchiveName}'.",
                RuntimeIdentifier: target.RuntimeIdentifier,
                Path: ToRepositoryPath(checksumPath)));
            return RepositoryBuildExitCodes.Failed;
        }

        var forbiddenStagingPaths = FindForbiddenPackagePaths(packageRoot).ToArray();
        if (forbiddenStagingPaths.Length > 0)
        {
            WriteForbiddenPathDiagnostic("release", "release verify", forbiddenStagingPaths[0], packageRoot, target.RuntimeIdentifier);
            return RepositoryBuildExitCodes.Failed;
        }

        if (!VerifyRequiredStagingPaths(packageRoot, target.RuntimeIdentifier))
        {
            return RepositoryBuildExitCodes.Failed;
        }

        var stagingManifestText = ReadManifestFile(manifestPath, target);
        if (stagingManifestText is null ||
            !VerifyManifestText(stagingManifestText, target, ToRepositoryPath(manifestPath)) ||
            !VerifyManifestInventory(stagingManifestText, GetPackageOutputFiles(packageRoot), target, ToRepositoryPath(manifestPath)))
        {
            return RepositoryBuildExitCodes.Failed;
        }

        var archiveEntries = ReadArchiveEntries(archivePath, target).ToArray();
        var forbiddenArchiveEntry = archiveEntries.FirstOrDefault(IsForbiddenPackagePath);
        if (forbiddenArchiveEntry is not null)
        {
            WriteForbiddenPathDiagnostic("release", "release verify", forbiddenArchiveEntry, archivePath, target.RuntimeIdentifier);
            return RepositoryBuildExitCodes.Failed;
        }

        if (!VerifyRequiredArchiveEntries(archiveEntries, target.RuntimeIdentifier))
        {
            return RepositoryBuildExitCodes.Failed;
        }

        var manifestEntry = ReadArchiveEntryText(archivePath, target, "release-manifest.json");
        if (manifestEntry is null ||
            !VerifyManifestText(manifestEntry, target, ToRepositoryPath(archivePath)) ||
            !VerifyManifestMatchesStaging(stagingManifestText, manifestEntry, target, ToRepositoryPath(archivePath)) ||
            !VerifyManifestInventory(manifestEntry, GetArchiveOutputFiles(archiveEntries), target, ToRepositoryPath(archivePath)))
        {
            return RepositoryBuildExitCodes.Failed;
        }

        return RepositoryBuildExitCodes.Success;
    }

    private bool CopyRequiredRootFile(string fileName, string packageRoot)
    {
        var source = Path.Combine(repositoryRoot, fileName);
        if (!File.Exists(source))
        {
            diagnostics.Write(new BuildDiagnostic(
                "package",
                "package",
                "error",
                "E2D-BUILD-PACKAGE-SOURCE-MISSING",
                $"Required package source file was not found: {fileName}.",
                Path: fileName));
            return false;
        }

        File.Copy(source, Path.Combine(packageRoot, fileName), overwrite: true);
        return true;
    }

    private async Task<int> RunDotnetStepAsync(
        string step,
        string failureCode,
        string projectPath,
        string[] arguments,
        CancellationToken cancellationToken)
    {
        diagnostics.Write(new BuildDiagnostic(
            "package",
            step,
            "info",
            "E2D-BUILD-PACKAGE-STEP-STARTED",
            $"Running dotnet {arguments[0]} for '{projectPath}'.",
            RuntimeIdentifier: TryFindRuntimeIdentifier(arguments),
            ProjectPath: projectPath,
            TimeoutSeconds: TimeoutSeconds));

        var result = await processRunner.RunAsync(
            new ProcessRunRequest(
                step,
                "dotnet",
                arguments,
                repositoryRoot,
                TimeSpan.FromSeconds(TimeoutSeconds)),
            cancellationToken).ConfigureAwait(false);

        foreach (var diagnostic in result.Diagnostics)
        {
            diagnostics.Write(diagnostic);
        }

        if (result.TimedOut)
        {
            diagnostics.Write(new BuildDiagnostic(
                "package",
                step,
                "error",
                failureCode,
                $"dotnet {arguments[0]} timed out for '{projectPath}' after {TimeoutSeconds} seconds.",
                TimedOut: true,
                ProjectPath: projectPath,
                TimeoutSeconds: TimeoutSeconds));
            return RepositoryBuildExitCodes.Failed;
        }

        if (result.ExitCode != 0)
        {
            diagnostics.Write(new BuildDiagnostic(
                "package",
                step,
                "error",
                failureCode,
                $"dotnet {arguments[0]} failed for '{projectPath}' with exit code {result.ExitCode}.",
                ProcessExitCode: result.ExitCode,
                TimedOut: false,
                ProjectPath: projectPath,
                TimeoutSeconds: TimeoutSeconds));
            return result.ExitCode ?? RepositoryBuildExitCodes.Failed;
        }

        return RepositoryBuildExitCodes.Success;
    }

    private void WriteReleaseManifest(string packageRoot, ReleaseTarget target)
    {
        var manifest = new ReleaseManifest(
            "Electron2D.ReleaseManifest",
            Version,
            target.RuntimeIdentifier,
            Configuration,
            new ReleaseArchive(target.ArchiveName, target.ArchiveType),
            CreateReleaseOutputs(packageRoot),
            ManifestForbiddenPaths,
            DryRun: true);
        var json = JsonSerializer.Serialize(manifest, ManifestJsonOptions) + Environment.NewLine;
        File.WriteAllText(Path.Combine(packageRoot, "release-manifest.json"), json, Encoding.UTF8);
    }

    private static ReleaseOutput[] CreateReleaseOutputs(string packageRoot)
    {
        return
        [
            CreateReleaseOutput(packageRoot, "runtimeLibraryPackage", "library/"),
            CreateReleaseOutput(packageRoot, "editorPublishOutput", "editor/"),
            CreateReleaseOutput(packageRoot, "cliPublishOutput", "tools/e2d/")
        ];
    }

    private static ReleaseOutput CreateReleaseOutput(string packageRoot, string kind, string relativeRoot)
    {
        var absoluteRoot = Path.Combine(packageRoot, relativeRoot.Replace('/', Path.DirectorySeparatorChar));
        var files = Directory.EnumerateFiles(absoluteRoot, "*", SearchOption.AllDirectories)
            .Select(path => ToRelativePackagePath(packageRoot, path))
            .Order(StringComparer.Ordinal)
            .ToArray();
        return new ReleaseOutput(kind, relativeRoot, files);
    }

    private bool VerifyRequiredStagingPaths(string packageRoot, string rid)
    {
        var requiredFiles = new[]
        {
            "README.md",
            "LICENSE",
            "release-manifest.json"
        };
        foreach (var relativePath in requiredFiles)
        {
            var path = Path.Combine(packageRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                WriteMissingRequiredPathDiagnostic(relativePath, rid);
                return false;
            }
        }

        foreach (var relativePath in new[] { "library", "editor", "tools/e2d" })
        {
            var path = Path.Combine(packageRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(path) || !Directory.EnumerateFileSystemEntries(path).Any())
            {
                WriteMissingRequiredPathDiagnostic(relativePath, rid);
                return false;
            }
        }

        return true;
    }

    private bool VerifyRequiredArchiveEntries(IReadOnlyCollection<string> entries, string rid)
    {
        foreach (var requiredFile in new[] { "README.md", "LICENSE", "release-manifest.json" })
        {
            if (!entries.Contains(requiredFile, StringComparer.Ordinal))
            {
                WriteMissingRequiredPathDiagnostic(requiredFile, rid);
                return false;
            }
        }

        foreach (var requiredPrefix in new[] { "library/", "editor/", "tools/e2d/" })
        {
            if (!entries.Any(entry => entry.StartsWith(requiredPrefix, StringComparison.Ordinal)))
            {
                WriteMissingRequiredPathDiagnostic(requiredPrefix, rid);
                return false;
            }
        }

        return true;
    }

    private void WriteMissingRequiredPathDiagnostic(string path, string rid)
    {
        diagnostics.Write(new BuildDiagnostic(
            "release",
            "release verify",
            "error",
            "E2D-BUILD-RELEASE-REQUIRED-PATH-MISSING",
            $"Release package is missing required path '{path}'.",
            RuntimeIdentifier: rid,
            Path: path));
    }

    private string? ReadManifestFile(string manifestPath, ReleaseTarget target)
    {
        try
        {
            return File.ReadAllText(manifestPath, Encoding.UTF8);
        }
        catch (IOException ex)
        {
            diagnostics.Write(new BuildDiagnostic(
                "release",
                "release verify",
                "error",
                "E2D-BUILD-RELEASE-MANIFEST-SCHEMA",
                $"Release manifest could not be read: {ex.Message}",
                RuntimeIdentifier: target.RuntimeIdentifier,
                Path: ToRepositoryPath(manifestPath)));
            return null;
        }
    }

    private bool VerifyManifestText(string text, ReleaseTarget target, string path)
    {
        try
        {
            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !TryGetString(root, "format", out var format) ||
                !string.Equals(format, "Electron2D.ReleaseManifest", StringComparison.Ordinal) ||
                !TryGetString(root, "version", out var version) ||
                !string.Equals(version, Version, StringComparison.Ordinal) ||
                !TryGetString(root, "runtimeIdentifier", out var rid) ||
                !string.Equals(rid, target.RuntimeIdentifier, StringComparison.Ordinal) ||
                !TryGetString(root, "configuration", out var configuration) ||
                !string.Equals(configuration, Configuration, StringComparison.Ordinal) ||
                !root.TryGetProperty("dryRun", out var dryRun) ||
                dryRun.ValueKind != JsonValueKind.True ||
                !VerifyManifestArchive(root, target) ||
                !VerifyManifestOutputs(root) ||
                !VerifyManifestForbiddenPaths(root))
            {
                WriteManifestSchemaDiagnostic(target.RuntimeIdentifier, path);
                return false;
            }
        }
        catch (JsonException)
        {
            WriteManifestSchemaDiagnostic(target.RuntimeIdentifier, path);
            return false;
        }

        return true;
    }

    private bool VerifyManifestArchive(JsonElement root, ReleaseTarget target)
    {
        return root.TryGetProperty("archive", out var archive) &&
            archive.ValueKind == JsonValueKind.Object &&
            TryGetString(archive, "name", out var name) &&
            string.Equals(name, target.ArchiveName, StringComparison.Ordinal) &&
            TryGetString(archive, "type", out var type) &&
            string.Equals(type, target.ArchiveType, StringComparison.Ordinal);
    }

    private static bool VerifyManifestOutputs(JsonElement root)
    {
        if (!root.TryGetProperty("outputs", out var outputs) || outputs.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var outputPairs = outputs.EnumerateArray()
            .Where(output => output.ValueKind == JsonValueKind.Object)
            .Select(output => new ManifestOutputInfo(
                TryGetString(output, "kind", out var kind) ? kind : string.Empty,
                TryGetString(output, "path", out var path) ? path : string.Empty,
                TryGetManifestFileList(output, out var files) ? files : []))
            .ToArray();
        return outputPairs.Length == 3 &&
            OutputHasValidInventory(outputPairs, "runtimeLibraryPackage", "library/") &&
            OutputHasValidInventory(outputPairs, "editorPublishOutput", "editor/") &&
            OutputHasValidInventory(outputPairs, "cliPublishOutput", "tools/e2d/");
    }

    private static bool OutputHasValidInventory(IEnumerable<ManifestOutputInfo> outputs, string kind, string path)
    {
        var matches = outputs
            .Where(output => output.Kind == kind && output.Path == path)
            .ToArray();
        if (matches.Length != 1)
        {
            return false;
        }

        var files = matches[0].Files;
        return files.Length > 0 &&
            files.SequenceEqual(files.Order(StringComparer.Ordinal), StringComparer.Ordinal) &&
            files.Distinct(StringComparer.Ordinal).Count() == files.Length &&
            files.All(file => file.StartsWith(path, StringComparison.Ordinal) && !IsForbiddenPackagePath(file));
    }

    private static bool TryGetManifestFileList(JsonElement output, out string[] files)
    {
        files = [];
        if (!output.TryGetProperty("files", out var filesProperty) || filesProperty.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var parsed = new List<string>();
        foreach (var file in filesProperty.EnumerateArray())
        {
            if (file.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            var value = file.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            parsed.Add(value);
        }

        files = [.. parsed];
        return true;
    }

    private bool VerifyManifestInventory(string text, IReadOnlyCollection<string> actualOutputFiles, ReleaseTarget target, string path)
    {
        var manifestFiles = ReadManifestOutputFiles(text);
        if (manifestFiles is null ||
            !manifestFiles.SetEquals(actualOutputFiles))
        {
            WriteManifestInventoryDiagnostic(target.RuntimeIdentifier, path);
            return false;
        }

        return true;
    }

    private bool VerifyManifestMatchesStaging(string stagingManifestText, string archiveManifestText, ReleaseTarget target, string path)
    {
        if (!string.Equals(stagingManifestText, archiveManifestText, StringComparison.Ordinal))
        {
            WriteManifestInventoryDiagnostic(target.RuntimeIdentifier, path);
            return false;
        }

        return true;
    }

    private void WriteManifestInventoryDiagnostic(string rid, string path)
    {
        diagnostics.Write(new BuildDiagnostic(
            "release",
            "release verify",
            "error",
            "E2D-BUILD-RELEASE-MANIFEST-INVENTORY",
            "Release manifest file inventory does not match release package contents.",
            RuntimeIdentifier: rid,
            Path: path));
    }

    private static HashSet<string>? ReadManifestOutputFiles(string text)
    {
        using var document = JsonDocument.Parse(text);
        if (!document.RootElement.TryGetProperty("outputs", out var outputs) || outputs.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var files = new HashSet<string>(StringComparer.Ordinal);
        foreach (var output in outputs.EnumerateArray())
        {
            if (!TryGetManifestFileList(output, out var outputFiles))
            {
                return null;
            }

            foreach (var file in outputFiles)
            {
                files.Add(file);
            }
        }

        return files;
    }

    private static IReadOnlyCollection<string> GetPackageOutputFiles(string packageRoot)
    {
        return Directory.EnumerateFiles(packageRoot, "*", SearchOption.AllDirectories)
            .Select(path => ToRelativePackagePath(packageRoot, path))
            .Where(IsReleaseOutputFile)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static IReadOnlyCollection<string> GetArchiveOutputFiles(IEnumerable<string> entries)
    {
        return entries
            .Where(IsReleaseOutputFile)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static bool IsReleaseOutputFile(string relativePath)
    {
        return relativePath.StartsWith("library/", StringComparison.Ordinal) ||
            relativePath.StartsWith("editor/", StringComparison.Ordinal) ||
            relativePath.StartsWith("tools/e2d/", StringComparison.Ordinal);
    }

    private static bool VerifyManifestForbiddenPaths(JsonElement root)
    {
        if (!root.TryGetProperty("forbiddenPathsChecked", out var forbiddenPaths) || forbiddenPaths.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var actual = forbiddenPaths.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString())
            .ToHashSet(StringComparer.Ordinal);
        return ManifestForbiddenPaths.All(actual.Contains);
    }

    private void WriteManifestSchemaDiagnostic(string rid, string path)
    {
        diagnostics.Write(new BuildDiagnostic(
            "release",
            "release verify",
            "error",
            "E2D-BUILD-RELEASE-MANIFEST-SCHEMA",
            "Release manifest shape does not match the release packaging contract.",
            RuntimeIdentifier: rid,
            Path: path));
    }

    private IEnumerable<string> FindForbiddenPackagePaths(string packageRoot)
    {
        foreach (var directory in Directory.EnumerateDirectories(packageRoot, "*", SearchOption.AllDirectories))
        {
            var relativePath = ToRelativePackagePath(packageRoot, directory) + "/";
            if (IsForbiddenPackagePath(relativePath))
            {
                yield return relativePath;
            }
        }

        foreach (var file in Directory.EnumerateFiles(packageRoot, "*", SearchOption.AllDirectories))
        {
            var relativePath = ToRelativePackagePath(packageRoot, file);
            if (IsForbiddenPackagePath(relativePath))
            {
                yield return relativePath;
            }
        }
    }

    private static bool IsForbiddenPackagePath(string relativePath)
    {
        var path = relativePath.Replace('\\', '/').TrimStart('/');
        if (string.IsNullOrWhiteSpace(path) ||
            Path.IsPathRooted(path) ||
            path.StartsWith("../", StringComparison.Ordinal) ||
            path.Contains("/../", StringComparison.Ordinal) ||
            path.Contains('\\') ||
            path.Any(char.IsControl))
        {
            return true;
        }

        if (string.Equals(path, "TASKS.md", StringComparison.Ordinal) ||
            path.StartsWith(".git/", StringComparison.Ordinal) ||
            path.StartsWith(".github/", StringComparison.Ordinal) ||
            path.StartsWith(".temp/", StringComparison.Ordinal) ||
            path.StartsWith(".codex/", StringComparison.Ordinal) ||
            path.StartsWith("dev-diary/", StringComparison.Ordinal) ||
            path.StartsWith("completed-tasks/", StringComparison.Ordinal) ||
            path.StartsWith("data/dev-diary/", StringComparison.Ordinal) ||
            path.StartsWith("data/completed-tasks/", StringComparison.Ordinal) ||
            path.StartsWith("eng/Electron2D.Build/", StringComparison.Ordinal) ||
            path.StartsWith("docs/verdicts/", StringComparison.Ordinal) ||
            path.StartsWith("audit-evidence/", StringComparison.Ordinal) ||
            path.StartsWith("artifacts/", StringComparison.Ordinal))
        {
            return true;
        }

        var fileName = path.Split('/').Last();
        return fileName.StartsWith("CHANGELOG", StringComparison.OrdinalIgnoreCase) ||
            fileName.StartsWith("RELEASE-NOTES", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".sha256", StringComparison.OrdinalIgnoreCase);
    }

    private void WriteForbiddenPathDiagnostic(string command, string step, string path, string rootPath, string? rid = null)
    {
        diagnostics.Write(new BuildDiagnostic(
            command,
            step,
            "error",
            command == "package" ? "E2D-BUILD-PACKAGE-FORBIDDEN-PATH" : "E2D-BUILD-RELEASE-FORBIDDEN-PATH",
            $"Release package contains forbidden path '{path}'.",
            RuntimeIdentifier: rid,
            Path: Directory.Exists(rootPath) ? Path.Combine(ToRepositoryPath(rootPath), path).Replace('\\', '/') : path));
    }

    private void CreateZipArchive(string packageRoot, string archivePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(archivePath)!);
        using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create);
        foreach (var file in Directory.EnumerateFiles(packageRoot, "*", SearchOption.AllDirectories).OrderBy(ToRelative))
        {
            var relativePath = ToRelativePackagePath(packageRoot, file);
            var entry = archive.CreateEntry(relativePath, CompressionLevel.SmallestSize);
            entry.LastWriteTime = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
            using var input = File.OpenRead(file);
            using var output = entry.Open();
            input.CopyTo(output);
        }

        string ToRelative(string file)
        {
            return ToRelativePackagePath(packageRoot, file);
        }
    }

    private static void CreateTarGzArchive(string packageRoot, string archivePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(archivePath)!);
        using var file = File.Create(archivePath);
        using var gzip = new GZipStream(file, CompressionLevel.SmallestSize);
        TarFile.CreateFromDirectory(packageRoot, gzip, includeBaseDirectory: false);
    }

    private static IReadOnlyList<string> ReadArchiveEntries(string archivePath, ReleaseTarget target)
    {
        return target.ArchiveType == "zip"
            ? ReadZipArchiveEntries(archivePath)
            : ReadTarGzArchiveEntries(archivePath);
    }

    private static IReadOnlyList<string> ReadZipArchiveEntries(string archivePath)
    {
        using var archive = ZipFile.OpenRead(archivePath);
        return archive.Entries
            .Where(entry => !string.IsNullOrEmpty(entry.Name))
            .Select(entry => entry.FullName.Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<string> ReadTarGzArchiveEntries(string archivePath)
    {
        using var file = File.OpenRead(archivePath);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        using var reader = new TarReader(gzip, leaveOpen: false);
        var entries = new List<string>();
        TarEntry? entry;
        while ((entry = reader.GetNextEntry()) is not null)
        {
            if (entry.EntryType != TarEntryType.Directory)
            {
                entries.Add(entry.Name.Replace('\\', '/'));
            }
        }

        entries.Sort(StringComparer.Ordinal);
        return entries;
    }

    private static string? ReadArchiveEntryText(string archivePath, ReleaseTarget target, string entryName)
    {
        return target.ArchiveType == "zip"
            ? ReadZipArchiveEntryText(archivePath, entryName)
            : ReadTarGzArchiveEntryText(archivePath, entryName);
    }

    private static string? ReadZipArchiveEntryText(string archivePath, string entryName)
    {
        using var archive = ZipFile.OpenRead(archivePath);
        var entry = archive.GetEntry(entryName);
        if (entry is null)
        {
            return null;
        }

        using var stream = entry.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    private static string? ReadTarGzArchiveEntryText(string archivePath, string entryName)
    {
        using var file = File.OpenRead(archivePath);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        using var reader = new TarReader(gzip, leaveOpen: false);
        TarEntry? entry;
        while ((entry = reader.GetNextEntry()) is not null)
        {
            if (entry.EntryType == TarEntryType.Directory || !string.Equals(entry.Name.Replace('\\', '/'), entryName, StringComparison.Ordinal))
            {
                continue;
            }

            if (entry.DataStream is null)
            {
                return string.Empty;
            }

            using var textReader = new StreamReader(entry.DataStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return textReader.ReadToEnd();
        }

        return null;
    }

    private static string[] CreatePublishArguments(string projectPath, string rid, string outputPath)
    {
        return
        [
            "publish",
            projectPath,
            "-c",
            Configuration,
            "-r",
            rid,
            "--self-contained",
            "true",
            "-o",
            outputPath
        ];
    }

    private static string? TryFindRuntimeIdentifier(string[] arguments)
    {
        for (var index = 0; index < arguments.Length - 1; index++)
        {
            if (arguments[index] == "-r")
            {
                return arguments[index + 1];
            }
        }

        return null;
    }

    private static bool TryGetString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool PathExists(string path)
    {
        return File.Exists(path) || Directory.Exists(path);
    }

    private string GetReleaseRoot()
    {
        return Path.Combine(repositoryRoot, "artifacts", "release", Version);
    }

    private string GetRidRoot(string rid)
    {
        return Path.Combine(GetReleaseRoot(), rid);
    }

    private string ToRepositoryPath(string path)
    {
        return Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/');
    }

    private static string ToRelativePackagePath(string packageRoot, string path)
    {
        return Path.GetRelativePath(packageRoot, path).Replace('\\', '/');
    }

    private static string Sha256File(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static ReleaseTarget? FindTarget(string rid)
    {
        return SupportedTargets.FirstOrDefault(target => string.Equals(target.RuntimeIdentifier, rid, StringComparison.Ordinal));
    }

    private sealed record ReleaseTarget(string RuntimeIdentifier, string ArchiveName, string ArchiveType);

    private sealed record ReleaseManifest(
        string Format,
        string Version,
        string RuntimeIdentifier,
        string Configuration,
        ReleaseArchive Archive,
        ReleaseOutput[] Outputs,
        string[] ForbiddenPathsChecked,
        bool DryRun);

    private sealed record ReleaseArchive(string Name, string Type);

    private sealed record ReleaseOutput(string Kind, string Path, string[] Files);

    private sealed record ManifestOutputInfo(string Kind, string Path, string[] Files);
}
