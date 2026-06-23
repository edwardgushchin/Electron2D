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
namespace Electron2D.Editor.FileSystemDock;

internal sealed class FileSystemDock
{
    private const string ImportCacheManifestPath = ".electron2d/import-cache/import-cache.json";
    private readonly string projectRoot;
    private readonly Func<string, string?> liveImportStateProvider;
    private readonly Electron2D.ResourceImportOptions importOptions;
    private Electron2D.ResourceImportReport? lastImportReport;

    public FileSystemDock(string projectRoot, Func<string, string?>? liveImportStateProvider = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        this.projectRoot = Path.GetFullPath(projectRoot);
        this.liveImportStateProvider = liveImportStateProvider ?? (_ => null);
        Directory.CreateDirectory(this.projectRoot);
        importOptions = Electron2D.ResourceImportOptions.CreateDefault(this.projectRoot);
        RegisterManifestResourceUids();
    }

    public IReadOnlyList<FileSystemItemSnapshot> Browse()
    {
        var manifest = LoadManifest();
        var manifestBySource = manifest.Entries.ToDictionary(entry => entry.SourcePath, StringComparer.Ordinal);
        var reportsBySource = BuildReportMap();
        var result = new List<FileSystemItemSnapshot>();

        foreach (var directory in EnumerateDirectories(projectRoot))
        {
            var relativePath = ToRelativePath(directory);
            result.Add(new FileSystemItemSnapshot(
                relativePath,
                string.Empty,
                Path.GetFileName(directory),
                FileSystemItemKind.Folder,
                IsImportable: false,
                UidText: string.Empty,
                ImportStatus: string.Empty,
                ImportError: string.Empty));
        }

        foreach (var file in EnumerateFiles(projectRoot))
        {
            var relativePath = ToRelativePath(file);
            var resourcePath = ToResourcePath(relativePath);
            var isImportable = CanImport(file);
            manifestBySource.TryGetValue(resourcePath, out var manifestEntry);
            reportsBySource.TryGetValue(resourcePath, out var reportEntry);
            var uid = manifestEntry?.Uid ?? TryReadResourceFileUid(file);
            var status = liveImportStateProvider(relativePath) ??
                reportEntry?.Status.ToString() ??
                (manifestEntry is null ? string.Empty : "Cached");
            var error = reportEntry?.ErrorMessage ?? string.Empty;

            result.Add(new FileSystemItemSnapshot(
                relativePath,
                resourcePath,
                Path.GetFileName(file),
                FileSystemItemKind.File,
                isImportable,
                uid > 0 ? Electron2D.ResourceUid.IdToText(uid) : string.Empty,
                status,
                error));
        }

        return result
            .OrderBy(item => item.Kind)
            .ThenBy(item => item.RelativePath, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<FileSystemImportError> GetImportErrors()
    {
        return (lastImportReport?.Items ?? Array.Empty<Electron2D.ResourceImportItemReport>())
            .Where(item => item.Status == Electron2D.ResourceImportItemStatus.Failed)
            .OrderBy(item => item.SourcePath, StringComparer.Ordinal)
            .Select(item => new FileSystemImportError(
                item.SourcePath,
                item.Reason.ToString(),
                item.ErrorMessage))
            .ToArray();
    }

    public IReadOnlyList<FileSystemItemSnapshot> Search(string query)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        return Browse()
            .Where(item =>
                item.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                item.RelativePath.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                item.ResourcePath.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.RelativePath, StringComparer.Ordinal)
            .ToArray();
    }

    public void CreateFolder(string parentRelativePath, string name)
    {
        ValidateSimpleName(name, nameof(name));
        var parent = ResolveProjectPath(parentRelativePath);
        if (!Directory.Exists(parent))
        {
            throw new DirectoryNotFoundException($"FileSystem dock parent folder '{parentRelativePath}' was not found.");
        }

        Directory.CreateDirectory(Path.Combine(parent, name));
    }

    public string Rename(string relativePath, string newName)
    {
        ValidateSimpleName(newName, nameof(newName));
        var source = ResolveProjectPath(relativePath);
        var target = Path.Combine(Path.GetDirectoryName(source) ?? projectRoot, newName);
        var targetRelativePath = ToRelativePath(target);
        MovePath(relativePath, targetRelativePath);
        return ToResourcePath(targetRelativePath);
    }

    public string Move(string relativePath, string targetDirectoryRelativePath)
    {
        var source = ResolveProjectPath(relativePath);
        var targetDirectory = ResolveProjectPath(targetDirectoryRelativePath);
        if (!Directory.Exists(targetDirectory))
        {
            throw new DirectoryNotFoundException($"FileSystem dock target folder '{targetDirectoryRelativePath}' was not found.");
        }

        var targetRelativePath = ToRelativePath(Path.Combine(targetDirectory, Path.GetFileName(source)));
        MovePath(relativePath, targetRelativePath);
        return ToResourcePath(targetRelativePath);
    }

    public Electron2D.ResourceImportReport Reimport()
    {
        RegisterManifestResourceUids();
        var pipeline = new Electron2D.ResourceImportPipeline(importOptions);
        lastImportReport = pipeline.ImportAll();
        RegisterManifestResourceUids();
        return lastImportReport;
    }

    public FileSystemResourceReference GetResourceReference(string resourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourcePath);
        var manifestEntry = LoadManifest().Entries.SingleOrDefault(entry => entry.SourcePath == resourcePath);
        if (manifestEntry is not null)
        {
            return new FileSystemResourceReference(
                manifestEntry.SourcePath,
                manifestEntry.Uid,
                manifestEntry.UidText,
                manifestEntry.Type);
        }

        var filePath = ResolveResourcePath(resourcePath);
        if (Path.GetExtension(filePath).Equals(".e2res", StringComparison.OrdinalIgnoreCase))
        {
            var document = Electron2D.ResourceFileTextSerializer.Deserialize(File.ReadAllText(filePath));
            return new FileSystemResourceReference(
                resourcePath,
                document.Uid,
                document.UidText,
                document.Type);
        }

        throw new InvalidOperationException($"FileSystem dock resource '{resourcePath}' does not have import metadata.");
    }

    public int DragResourceIntoScene(string scenePath, string resourcePath, int parentNodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourcePath);

        var fullScenePath = Path.GetFullPath(scenePath);
        if (!Electron2D.ResourceImportPath.IsSameOrChildOf(projectRoot, fullScenePath))
        {
            throw new ArgumentException("Scene path must stay inside the project root.", nameof(scenePath));
        }

        var resource = GetResourceReference(resourcePath);
        var document = Electron2D.SceneFileTextSerializer.Deserialize(File.ReadAllText(fullScenePath));
        var references = document.ExternalReferences.ToList();
        var reference = references.SingleOrDefault(item => item.Uid == resource.Uid);
        if (reference is null)
        {
            reference = new Electron2D.ResourceFileExternalReference(
                references.Count == 0 ? 1 : references.Max(item => item.Id) + 1,
                resource.Uid,
                resource.ResourcePath,
                resource.Type);
            references.Add(reference);
        }
        else if (!string.Equals(reference.Path, resource.ResourcePath, StringComparison.Ordinal) ||
            !string.Equals(reference.Type, resource.Type, StringComparison.Ordinal))
        {
            references = references
                .Select(item => item.Id == reference.Id
                    ? new Electron2D.ResourceFileExternalReference(item.Id, item.Uid, resource.ResourcePath, resource.Type)
                    : item)
                .ToList();
            reference = references.Single(item => item.Uid == resource.Uid);
        }

        var nodeId = document.Nodes.Max(node => node.Id) + 1;
        var node = new Electron2D.SceneFileNode(
            nodeId,
            "Electron2D.Sprite2D",
            ToDisplayName(resource.ResourcePath),
            parentNodeId,
            GetRootNode(document).Id,
            properties: new Dictionary<string, Electron2D.SerializedPropertyValue>(StringComparer.Ordinal)
            {
                ["texture"] = Electron2D.SerializedPropertyValue.ExternalResource(reference.Id)
            });

        var nextDocument = new Electron2D.SceneFileDocument(
            references,
            document.InternalResources,
            document.Nodes.Concat([node]));
        WriteSceneFile(fullScenePath, nextDocument);

        return nodeId;
    }

    private void MovePath(string sourceRelativePath, string targetRelativePath)
    {
        var source = ResolveProjectPath(sourceRelativePath);
        var target = ResolveProjectPath(targetRelativePath);
        if (!File.Exists(source) && !Directory.Exists(source))
        {
            throw new FileNotFoundException($"FileSystem dock source '{sourceRelativePath}' was not found.", source);
        }

        if (File.Exists(target) || Directory.Exists(target))
        {
            throw new IOException($"FileSystem dock target '{targetRelativePath}' already exists.");
        }

        var movedResources = CollectMovedResources(sourceRelativePath, targetRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        if (Directory.Exists(source))
        {
            Directory.Move(source, target);
        }
        else
        {
            File.Move(source, target);
        }

        foreach (var movedResource in movedResources)
        {
            UpdateMovedResourcePath(movedResource.NewRelativePath, movedResource.NewResourcePath);
            RegisterResourceUid(movedResource.Uid, movedResource.NewResourcePath);
            UpdateStoredReferences(movedResource.OldResourcePath, movedResource.NewResourcePath, movedResource.Uid);
        }
    }

    private IReadOnlyList<MovedResource> CollectMovedResources(string sourceRelativePath, string targetRelativePath)
    {
        var source = ResolveProjectPath(sourceRelativePath);
        var target = ResolveProjectPath(targetRelativePath);
        var manifest = LoadManifest();
        var manifestBySource = manifest.Entries.ToDictionary(entry => entry.SourcePath, StringComparer.Ordinal);
        var files = Directory.Exists(source) ? EnumerateFiles(source) : [source];
        var movedResources = new List<MovedResource>();

        foreach (var file in files)
        {
            var oldRelativePath = ToRelativePath(file);
            var oldResourcePath = ToResourcePath(oldRelativePath);
            var relativeTail = Path.GetRelativePath(source, file);
            var newAbsolutePath = File.Exists(source)
                ? target
                : Path.Combine(target, relativeTail);
            var newRelativePath = ToRelativePath(newAbsolutePath);
            var newResourcePath = ToResourcePath(newRelativePath);
            var uid = manifestBySource.TryGetValue(oldResourcePath, out var entry)
                ? entry.Uid
                : TryReadResourceFileUid(file);

            if (uid > 0)
            {
                movedResources.Add(new MovedResource(
                    oldRelativePath,
                    newRelativePath,
                    oldResourcePath,
                    newResourcePath,
                    uid));
            }
        }

        return movedResources;
    }

    private void UpdateMovedResourcePath(string relativePath, string resourcePath)
    {
        var file = ResolveProjectPath(relativePath);
        if (!Path.GetExtension(file).Equals(".e2res", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var document = Electron2D.ResourceFileTextSerializer.Deserialize(File.ReadAllText(file));
        if (string.Equals(document.Path, resourcePath, StringComparison.Ordinal))
        {
            return;
        }

        var updated = new Electron2D.ResourceFileDocument(
            document.Uid,
            document.Type,
            resourcePath,
            document.ExternalReferences,
            document.InternalResources,
            document.Properties);
        File.WriteAllText(file, Electron2D.ResourceFileTextSerializer.Serialize(updated));
    }

    private void UpdateStoredReferences(string oldResourcePath, string newResourcePath, long uid)
    {
        foreach (var file in EnumerateFiles(projectRoot))
        {
            UpdateSceneReferences(file, oldResourcePath, newResourcePath, uid);
            UpdateResourceReferences(file, oldResourcePath, newResourcePath, uid);
        }
    }

    private void UpdateSceneReferences(string file, string oldResourcePath, string newResourcePath, long uid)
    {
        string text;
        try
        {
            text = File.ReadAllText(file);
        }
        catch (IOException)
        {
            return;
        }

        if (!text.Contains(Electron2D.SceneFileDocument.FormatName, StringComparison.Ordinal))
        {
            return;
        }

        Electron2D.SceneFileDocument document;
        try
        {
            document = Electron2D.SceneFileTextSerializer.Deserialize(text);
        }
        catch (FormatException)
        {
            return;
        }

        var changed = false;
        var references = document.ExternalReferences.Select(reference =>
        {
            if (reference.Uid != uid && !string.Equals(reference.Path, oldResourcePath, StringComparison.Ordinal))
            {
                return reference;
            }

            changed = changed || !string.Equals(reference.Path, newResourcePath, StringComparison.Ordinal);
            return new Electron2D.ResourceFileExternalReference(reference.Id, reference.Uid, newResourcePath, reference.Type);
        }).ToArray();

        if (changed)
        {
            WriteSceneFile(file, new Electron2D.SceneFileDocument(references, document.InternalResources, document.Nodes));
        }
    }

    private void UpdateResourceReferences(string file, string oldResourcePath, string newResourcePath, long uid)
    {
        if (!Path.GetExtension(file).Equals(".e2res", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Electron2D.ResourceFileDocument document;
        try
        {
            document = Electron2D.ResourceFileTextSerializer.Deserialize(File.ReadAllText(file));
        }
        catch (FormatException)
        {
            return;
        }

        var changed = false;
        var references = document.ExternalReferences.Select(reference =>
        {
            if (reference.Uid != uid && !string.Equals(reference.Path, oldResourcePath, StringComparison.Ordinal))
            {
                return reference;
            }

            changed = changed || !string.Equals(reference.Path, newResourcePath, StringComparison.Ordinal);
            return new Electron2D.ResourceFileExternalReference(reference.Id, reference.Uid, newResourcePath, reference.Type);
        }).ToArray();

        if (changed)
        {
            File.WriteAllText(file, Electron2D.ResourceFileTextSerializer.Serialize(new Electron2D.ResourceFileDocument(
                document.Uid,
                document.Type,
                document.Path,
                references,
                document.InternalResources,
                document.Properties)));
        }
    }

    private void RegisterManifestResourceUids()
    {
        foreach (var entry in LoadManifest().Entries)
        {
            RegisterResourceUid(entry.Uid, entry.SourcePath);
        }
    }

    private static void RegisterResourceUid(long uid, string resourcePath)
    {
        if (uid <= 0 || uid == Electron2D.ResourceUid.InvalidId)
        {
            return;
        }

        if (Electron2D.ResourceUid.HasId(uid))
        {
            try
            {
                Electron2D.ResourceUid.SetId(uid, resourcePath);
            }
            catch (InvalidOperationException)
            {
                // Another path in this short-lived editor process owns the UID.
                // Keep the existing mapping instead of breaking file operations.
            }

            return;
        }

        try
        {
            Electron2D.ResourceUid.AddId(uid, resourcePath);
        }
        catch (InvalidOperationException)
        {
            // The path already has a UID mapping. Import will keep using that mapping.
        }
    }

    private Electron2D.ResourceImportManifest LoadManifest()
    {
        var manifestPath = Path.Combine(projectRoot, ImportCacheManifestPath.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(manifestPath)
            ? Electron2D.ResourceImportManifestTextSerializer.Deserialize(File.ReadAllText(manifestPath))
            : new Electron2D.ResourceImportManifest();
    }

    private Dictionary<string, Electron2D.ResourceImportItemReport> BuildReportMap()
    {
        return (lastImportReport?.Items ?? Array.Empty<Electron2D.ResourceImportItemReport>())
            .Concat(lastImportReport?.PrunedItems ?? Array.Empty<Electron2D.ResourceImportItemReport>())
            .GroupBy(item => item.SourcePath, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);
    }

    private bool CanImport(string file)
    {
        var relativePath = ToRelativePath(file);
        var source = new Electron2D.ResourceImportSourceFile(file, ToResourcePath(relativePath));
        return importOptions.Importers.Any(importer => importer.CanImport(source));
    }

    private long TryReadResourceFileUid(string file)
    {
        if (!Path.GetExtension(file).Equals(".e2res", StringComparison.OrdinalIgnoreCase))
        {
            return Electron2D.ResourceUid.InvalidId;
        }

        try
        {
            return Electron2D.ResourceFileTextSerializer.Deserialize(File.ReadAllText(file)).Uid;
        }
        catch (FormatException)
        {
            return Electron2D.ResourceUid.InvalidId;
        }
    }

    private string ResolveResourcePath(string resourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourcePath);
        if (!resourcePath.StartsWith("res://", StringComparison.Ordinal))
        {
            throw new ArgumentException("FileSystem dock resource path must start with res://.", nameof(resourcePath));
        }

        return ResolveProjectPath(resourcePath["res://".Length..]);
    }

    private string ResolveProjectPath(string relativePath)
    {
        var candidate = Path.GetFullPath(Path.Combine(
            projectRoot,
            (relativePath ?? string.Empty).Replace('/', Path.DirectorySeparatorChar)));
        if (!Electron2D.ResourceImportPath.IsSameOrChildOf(projectRoot, candidate))
        {
            throw new ArgumentException("FileSystem dock path must stay inside the project root.", nameof(relativePath));
        }

        return candidate;
    }

    private string ToRelativePath(string absolutePath)
    {
        return Path.GetRelativePath(projectRoot, Path.GetFullPath(absolutePath))
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');
    }

    private static string ToResourcePath(string relativePath)
    {
        return "res://" + relativePath.Replace('\\', '/').TrimStart('/');
    }

    private IEnumerable<string> EnumerateDirectories(string root)
    {
        if (ShouldSkipDirectory(root))
        {
            yield break;
        }

        foreach (var directory in Directory.EnumerateDirectories(root).OrderBy(path => path, StringComparer.Ordinal))
        {
            if (ShouldSkipDirectory(directory))
            {
                continue;
            }

            yield return Path.GetFullPath(directory);
            foreach (var child in EnumerateDirectories(directory))
            {
                yield return child;
            }
        }
    }

    private IEnumerable<string> EnumerateFiles(string root)
    {
        if (ShouldSkipDirectory(root))
        {
            yield break;
        }

        foreach (var file in Directory.EnumerateFiles(root).OrderBy(path => path, StringComparer.Ordinal))
        {
            yield return Path.GetFullPath(file);
        }

        foreach (var directory in Directory.EnumerateDirectories(root).OrderBy(path => path, StringComparer.Ordinal))
        {
            foreach (var file in EnumerateFiles(directory))
            {
                yield return file;
            }
        }
    }

    private bool ShouldSkipDirectory(string directory)
    {
        var fullPath = Path.GetFullPath(directory);
        if (!Electron2D.ResourceImportPath.IsSameOrChildOf(projectRoot, fullPath))
        {
            return true;
        }

        var name = Path.GetFileName(fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return name is ".git" or ".electron2d" or ".temp" or "bin" or "obj" or "artifacts" or "publish" or
            "packages" or "TestResults" or "coverage";
    }

    private static void WriteSceneFile(string path, Electron2D.SceneFileDocument document)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, Electron2D.SceneFileTextSerializer.Serialize(document));
    }

    private static Electron2D.SceneFileNode GetRootNode(Electron2D.SceneFileDocument document)
    {
        return document.Nodes.Single(node => node.ParentId is null);
    }

    private static string ToDisplayName(string resourcePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(resourcePath["res://".Length..]);
        return string.Join(
            ' ',
            fileName.Split(['_', '-', ' '], StringSplitOptions.RemoveEmptyEntries)
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    }

    private static void ValidateSimpleName(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            value.Contains('/', StringComparison.Ordinal) ||
            value.Contains('\\', StringComparison.Ordinal))
        {
            throw new ArgumentException("FileSystem dock names must be a single file or folder name.", parameterName);
        }
    }

    private sealed record MovedResource(
        string OldRelativePath,
        string NewRelativePath,
        string OldResourcePath,
        string NewResourcePath,
        long Uid);
}
