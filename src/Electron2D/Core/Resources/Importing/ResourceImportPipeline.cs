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
using System.Security.Cryptography;

namespace Electron2D;

internal sealed class ResourceImportPipeline
{
    private const string ManifestFileName = "import-cache.json";

    private readonly ResourceImportOptions options;

    public ResourceImportPipeline(ResourceImportOptions options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public ResourceImportReport ImportAll()
    {
        Directory.CreateDirectory(options.CacheRoot);

        var manifest = LoadManifest();
        var previousEntries = manifest.Entries.ToDictionary(entry => entry.SourcePath, StringComparer.Ordinal);
        var nextEntries = new Dictionary<string, ResourceImportManifestEntry>(StringComparer.Ordinal);
        var reports = new List<ResourceImportItemReport>();
        var prunedReports = new List<ResourceImportItemReport>();
        var discoveredSources = DiscoverImportableSources().ToArray();
        var discoveredPaths = discoveredSources.Select(source => source.Source.ResourcePath).ToHashSet(StringComparer.Ordinal);

        foreach (var (source, importer) in discoveredSources)
        {
            var sourceHash = ResourceImportHash.ComputeFileHash(source.AbsolutePath);
            previousEntries.TryGetValue(source.ResourcePath, out var previousEntry);
            var reason = GetImportReason(previousEntry, importer, sourceHash);

            if (reason == ResourceImportReason.None && previousEntry is not null)
            {
                nextEntries.Add(source.ResourcePath, previousEntry);
                reports.Add(new ResourceImportItemReport(
                    source.ResourcePath,
                    ResourceImportItemStatus.UpToDate,
                    ResourceImportReason.None,
                    ToAbsoluteCacheFiles(previousEntry)));
                continue;
            }

            try
            {
                var output = importer.Import(new ResourceImportContext(options, source));
                var dependencies = CaptureDependencies(output.DependencyPaths);
                var cacheFiles = WriteArtifacts(output, previousEntry);
                var entry = new ResourceImportManifestEntry(
                    source.ResourcePath,
                    output.Uid,
                    output.ResourceType,
                    importer.Name,
                    sourceHash,
                    cacheFiles,
                    dependencies);

                nextEntries[source.ResourcePath] = entry;
                reports.Add(new ResourceImportItemReport(
                    source.ResourcePath,
                    ResourceImportItemStatus.Imported,
                    reason,
                    ToAbsoluteCacheFiles(entry)));
            }
            catch (Exception exception) when (IsImportFailure(exception))
            {
                if (previousEntry is not null)
                {
                    nextEntries[source.ResourcePath] = previousEntry;
                }

                reports.Add(new ResourceImportItemReport(
                    source.ResourcePath,
                    ResourceImportItemStatus.Failed,
                    reason,
                    previousEntry is null ? Array.Empty<string>() : ToAbsoluteCacheFiles(previousEntry),
                    exception.Message));
            }
        }

        foreach (var entry in manifest.Entries)
        {
            if (discoveredPaths.Contains(entry.SourcePath))
            {
                continue;
            }

            DeleteCacheFiles(entry);
            prunedReports.Add(new ResourceImportItemReport(
                entry.SourcePath,
                ResourceImportItemStatus.Pruned,
                ResourceImportReason.SourceRemoved,
                ToAbsoluteCacheFiles(entry)));
        }

        SaveManifest(new ResourceImportManifest(nextEntries.Values));

        return new ResourceImportReport(reports, prunedReports);
    }

    private ResourceImportReason GetImportReason(
        ResourceImportManifestEntry? previousEntry,
        IResourceImporter importer,
        string sourceHash)
    {
        if (previousEntry is null)
        {
            return ResourceImportReason.NewSource;
        }

        if (!string.Equals(previousEntry.Importer, importer.Name, StringComparison.Ordinal))
        {
            return ResourceImportReason.ImporterChanged;
        }

        if (!string.Equals(previousEntry.SourceHash, sourceHash, StringComparison.Ordinal))
        {
            return ResourceImportReason.SourceChanged;
        }

        if (previousEntry.CacheFiles.Any(cacheFile => !File.Exists(ToAbsoluteCacheFile(cacheFile))))
        {
            return ResourceImportReason.CacheMissing;
        }

        foreach (var dependency in previousEntry.Dependencies)
        {
            var absolutePath = options.ResolveResourcePath(dependency.Path);
            if (!File.Exists(absolutePath))
            {
                return ResourceImportReason.DependencyChanged;
            }

            var currentHash = ResourceImportHash.ComputeFileHash(absolutePath);
            if (!string.Equals(currentHash, dependency.ContentHash, StringComparison.Ordinal))
            {
                return ResourceImportReason.DependencyChanged;
            }
        }

        return ResourceImportReason.None;
    }

    private IReadOnlyList<ResourceImportManifestDependency> CaptureDependencies(IEnumerable<string> dependencyPaths)
    {
        var dependencies = new List<ResourceImportManifestDependency>();
        foreach (var dependencyPath in dependencyPaths.Distinct(StringComparer.Ordinal).OrderBy(path => path, StringComparer.Ordinal))
        {
            var absolutePath = options.ResolveResourcePath(dependencyPath);
            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException($"Resource dependency '{dependencyPath}' was not found.", absolutePath);
            }

            dependencies.Add(new ResourceImportManifestDependency(
                dependencyPath,
                ResourceImportHash.ComputeFileHash(absolutePath)));
        }

        return dependencies;
    }

    private IReadOnlyList<string> WriteArtifacts(
        ResourceImportOutput output,
        ResourceImportManifestEntry? previousEntry)
    {
        var uidText = ResourceUid.IdToText(output.Uid);
        var uidDirectory = uidText["uid://".Length..];
        var preparedFiles = new List<(string TemporaryPath, string FinalPath, string RelativePath)>();

        try
        {
            foreach (var artifact in output.Artifacts)
            {
                var relativePath = ResourceImportPath.CombineUnix("resources", uidDirectory, artifact.Name);
                var finalPath = ToAbsoluteCacheFile(relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);

                var temporaryPath = finalPath + ".tmp." + Guid.NewGuid().ToString("N");
                File.WriteAllBytes(temporaryPath, artifact.Bytes.ToArray());
                preparedFiles.Add((temporaryPath, finalPath, relativePath));
            }

            foreach (var (temporaryPath, finalPath, _) in preparedFiles)
            {
                File.Move(temporaryPath, finalPath, overwrite: true);
            }
        }
        catch
        {
            foreach (var (temporaryPath, _, _) in preparedFiles)
            {
                DeleteFileIfExists(temporaryPath);
            }

            throw;
        }

        var nextCacheFiles = preparedFiles
            .Select(file => file.RelativePath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        if (previousEntry is not null)
        {
            foreach (var staleFile in previousEntry.CacheFiles.Except(nextCacheFiles, StringComparer.Ordinal))
            {
                DeleteFileIfExists(ToAbsoluteCacheFile(staleFile));
            }
        }

        return nextCacheFiles;
    }

    private ResourceImportManifest LoadManifest()
    {
        var manifestPath = GetManifestPath();
        return File.Exists(manifestPath)
            ? ResourceImportManifestTextSerializer.Deserialize(File.ReadAllText(manifestPath))
            : new ResourceImportManifest();
    }

    private void SaveManifest(ResourceImportManifest manifest)
    {
        var manifestPath = GetManifestPath();
        Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);

        var temporaryPath = manifestPath + ".tmp." + Guid.NewGuid().ToString("N");
        File.WriteAllText(temporaryPath, ResourceImportManifestTextSerializer.Serialize(manifest));
        File.Move(temporaryPath, manifestPath, overwrite: true);
    }

    private IReadOnlyList<string> ToAbsoluteCacheFiles(ResourceImportManifestEntry entry)
    {
        return entry.CacheFiles.Select(ToAbsoluteCacheFile).ToArray();
    }

    private string ToAbsoluteCacheFile(string relativePath)
    {
        var absolutePath = Path.GetFullPath(Path.Combine(
            options.CacheRoot,
            relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!ResourceImportPath.IsSameOrChildOf(options.CacheRoot, absolutePath))
        {
            throw new InvalidOperationException($"Import cache file '{relativePath}' escapes the cache root.");
        }

        return absolutePath;
    }

    private void DeleteCacheFiles(ResourceImportManifestEntry entry)
    {
        foreach (var cacheFile in entry.CacheFiles)
        {
            DeleteFileIfExists(ToAbsoluteCacheFile(cacheFile));
        }
    }

    private string GetManifestPath()
    {
        return Path.Combine(options.CacheRoot, ManifestFileName);
    }

    private IReadOnlyList<(ResourceImportSourceFile Source, IResourceImporter Importer)> DiscoverImportableSources()
    {
        var sources = new List<(ResourceImportSourceFile Source, IResourceImporter Importer)>();
        foreach (var absolutePath in EnumerateSourceFiles(options.SourceRoot).OrderBy(path => path, StringComparer.Ordinal))
        {
            var resourcePath = ToResourcePath(absolutePath);
            var source = new ResourceImportSourceFile(absolutePath, resourcePath);
            var importer = options.Importers.FirstOrDefault(importer => importer.CanImport(source));
            if (importer is not null)
            {
                sources.Add((source, importer));
            }
        }

        return sources;
    }

    private IEnumerable<string> EnumerateSourceFiles(string directory)
    {
        if (ShouldSkipDirectory(directory))
        {
            yield break;
        }

        foreach (var file in Directory.EnumerateFiles(directory))
        {
            var absolutePath = Path.GetFullPath(file);
            if (!ResourceImportPath.IsSameOrChildOf(options.CacheRoot, absolutePath))
            {
                yield return absolutePath;
            }
        }

        foreach (var childDirectory in Directory.EnumerateDirectories(directory))
        {
            foreach (var file in EnumerateSourceFiles(childDirectory))
            {
                yield return file;
            }
        }
    }

    private bool ShouldSkipDirectory(string directory)
    {
        var absolutePath = Path.GetFullPath(directory);
        if (!ResourceImportPath.IsSameOrChildOf(options.SourceRoot, absolutePath))
        {
            return true;
        }

        if (ResourceImportPath.IsSameOrChildOf(options.CacheRoot, absolutePath))
        {
            return true;
        }

        var name = Path.GetFileName(absolutePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return name is ".git" or ".electron2d" or ".temp" or "bin" or "obj" or "artifacts" or "publish" or
            "packages" or "TestResults" or "coverage";
    }

    private string ToResourcePath(string absolutePath)
    {
        var relativePath = Path.GetRelativePath(options.SourceRoot, absolutePath)
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');
        return "res://" + relativePath;
    }

    private static bool IsImportFailure(Exception exception)
    {
        return exception is IOException or UnauthorizedAccessException or FormatException or ArgumentException or
            InvalidOperationException;
    }

    private static void DeleteFileIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}

internal static class ResourceImportHash
{
    public static string ComputeFileHash(string path)
    {
        using var stream = File.OpenRead(path);
        var hash = SHA256.HashData(stream);
        return "sha256:" + Convert.ToHexString(hash).ToLowerInvariant();
    }
}

internal static class ResourceImportPath
{
    public static bool IsSameOrChildOf(string parentPath, string candidatePath)
    {
        var parent = EnsureTrailingSeparator(Path.GetFullPath(parentPath));
        var candidate = Path.GetFullPath(candidatePath);
        return string.Equals(candidate.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                parent.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase) ||
            candidate.StartsWith(parent, StringComparison.OrdinalIgnoreCase);
    }

    public static string CombineUnix(params string[] parts)
    {
        return string.Join("/", parts.Select(part => part.Trim('/')).Where(part => part.Length > 0));
    }

    private static string EnsureTrailingSeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
    }
}
