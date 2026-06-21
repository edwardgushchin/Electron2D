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
using System.Collections.ObjectModel;
using System.Text;

namespace Electron2D;

internal enum ResourceImportItemStatus
{
    UpToDate,
    Imported,
    Failed,
    Pruned
}

internal enum ResourceImportReason
{
    None,
    NewSource,
    SourceChanged,
    DependencyChanged,
    CacheMissing,
    ImporterChanged,
    SourceRemoved
}

internal sealed class ResourceImportOptions
{
    public ResourceImportOptions(
        string projectRoot,
        string sourceRoot,
        string cacheRoot,
        IEnumerable<IResourceImporter> importers)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheRoot);
        ArgumentNullException.ThrowIfNull(importers);

        ProjectRoot = Path.GetFullPath(projectRoot);
        SourceRoot = Path.GetFullPath(sourceRoot);
        CacheRoot = Path.GetFullPath(cacheRoot);
        Importers = importers.ToArray();

        if (Importers.Count == 0)
        {
            throw new ArgumentException("At least one resource importer is required.", nameof(importers));
        }
    }

    public string ProjectRoot { get; }

    public string SourceRoot { get; }

    public string CacheRoot { get; }

    public IReadOnlyList<IResourceImporter> Importers { get; }

    public static ResourceImportOptions CreateDefault(string projectRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        var fullProjectRoot = Path.GetFullPath(projectRoot);
        return new ResourceImportOptions(
            fullProjectRoot,
            fullProjectRoot,
            Path.Combine(fullProjectRoot, ".electron2d", "import-cache"),
            [new ResourceFileImporter()]);
    }

    public string ResolveResourcePath(string resourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourcePath);
        if (!resourcePath.StartsWith("res://", StringComparison.Ordinal))
        {
            throw new ArgumentException($"Resource dependency path '{resourcePath}' must start with res://.", nameof(resourcePath));
        }

        var relativePath = resourcePath["res://".Length..].Replace('/', Path.DirectorySeparatorChar);
        var absolutePath = Path.GetFullPath(Path.Combine(SourceRoot, relativePath));
        if (!ResourceImportPath.IsSameOrChildOf(SourceRoot, absolutePath))
        {
            throw new ArgumentException($"Resource dependency path '{resourcePath}' escapes the source root.", nameof(resourcePath));
        }

        return absolutePath;
    }
}

internal sealed class ResourceImportSourceFile
{
    public ResourceImportSourceFile(string absolutePath, string resourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(absolutePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourcePath);

        AbsolutePath = Path.GetFullPath(absolutePath);
        ResourcePath = resourcePath;
        Extension = Path.GetExtension(AbsolutePath);
    }

    public string AbsolutePath { get; }

    public string ResourcePath { get; }

    public string Extension { get; }
}

internal sealed class ResourceImportContext
{
    public ResourceImportContext(ResourceImportOptions options, ResourceImportSourceFile source)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public ResourceImportOptions Options { get; }

    public ResourceImportSourceFile Source { get; }

    public string ReadSourceText()
    {
        return File.ReadAllText(Source.AbsolutePath, Encoding.UTF8);
    }
}

internal sealed class ResourceImportArtifact
{
    public ResourceImportArtifact(string name, byte[] bytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(bytes);

        if (Path.IsPathRooted(name) || name.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException("Import artifact names must be relative cache paths.", nameof(name));
        }

        Name = name.Replace('\\', '/');
        Bytes = bytes.ToArray();
    }

    public string Name { get; }

    public IReadOnlyList<byte> Bytes { get; }

    public static ResourceImportArtifact FromUtf8Text(string name, string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return new ResourceImportArtifact(name, Encoding.UTF8.GetBytes(text));
    }
}

internal sealed class ResourceImportOutput
{
    public ResourceImportOutput(
        long uid,
        string resourceType,
        IEnumerable<ResourceImportArtifact> artifacts,
        IEnumerable<string>? dependencyPaths = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceType);
        ArgumentNullException.ThrowIfNull(artifacts);

        if (uid <= 0 || uid == ResourceUid.InvalidId)
        {
            throw new ArgumentException("Imported resource UID must be positive.", nameof(uid));
        }

        Uid = uid;
        ResourceType = resourceType;
        Artifacts = artifacts.ToArray();
        DependencyPaths = (dependencyPaths ?? Array.Empty<string>())
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        if (Artifacts.Count == 0)
        {
            throw new ArgumentException("Import output must contain at least one artifact.", nameof(artifacts));
        }
    }

    public long Uid { get; }

    public string ResourceType { get; }

    public IReadOnlyList<ResourceImportArtifact> Artifacts { get; }

    public IReadOnlyList<string> DependencyPaths { get; }
}

internal sealed class ResourceImportItemReport
{
    public ResourceImportItemReport(
        string sourcePath,
        ResourceImportItemStatus status,
        ResourceImportReason reason,
        IEnumerable<string>? cacheFiles = null,
        string? errorMessage = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        SourcePath = sourcePath;
        Status = status;
        Reason = reason;
        CacheFiles = (cacheFiles ?? Array.Empty<string>()).ToArray();
        ErrorMessage = errorMessage ?? string.Empty;
    }

    public string SourcePath { get; }

    public ResourceImportItemStatus Status { get; }

    public ResourceImportReason Reason { get; }

    public IReadOnlyList<string> CacheFiles { get; }

    public string ErrorMessage { get; }
}

internal sealed class ResourceImportReport
{
    public ResourceImportReport(
        IEnumerable<ResourceImportItemReport> items,
        IEnumerable<ResourceImportItemReport>? prunedItems = null)
    {
        ArgumentNullException.ThrowIfNull(items);

        Items = new ReadOnlyCollection<ResourceImportItemReport>(
            items.OrderBy(item => item.SourcePath, StringComparer.Ordinal).ToArray());
        PrunedItems = new ReadOnlyCollection<ResourceImportItemReport>(
            (prunedItems ?? Array.Empty<ResourceImportItemReport>())
            .OrderBy(item => item.SourcePath, StringComparer.Ordinal)
            .ToArray());
    }

    public IReadOnlyList<ResourceImportItemReport> Items { get; }

    public IReadOnlyList<ResourceImportItemReport> PrunedItems { get; }

    public bool HasErrors => Items.Any(item => item.Status == ResourceImportItemStatus.Failed);
}
