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
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class ResourceImportCacheTests
{
    [Fact]
    public void ImportAllDiscoversResourceFilesAndWritesCacheOutsideSources()
    {
        using var project = ResourceImportTestProject.Create();
        project.WriteResource(
            "characters/player.e2res",
            uid: 123456789L,
            externalReferences: Array.Empty<Electron2D.ResourceFileExternalReference>());

        var report = project.CreatePipeline().ImportAll();

        var item = Assert.Single(report.Items);
        Assert.Equal(Electron2D.ResourceImportItemStatus.Imported, item.Status);
        Assert.Equal(Electron2D.ResourceImportReason.NewSource, item.Reason);
        Assert.Equal("res://characters/player.e2res", item.SourcePath);
        Assert.StartsWith(project.CacheRoot, item.CacheFiles[0], StringComparison.Ordinal);
        Assert.False(item.CacheFiles[0].StartsWith(project.SourceRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal));
        Assert.True(File.Exists(project.ManifestPath));

        var manifest = project.ReadManifest();
        var entry = Assert.Single(manifest.Entries);
        Assert.Equal("res://characters/player.e2res", entry.SourcePath);
        Assert.Equal("Electron2D.ResourceFile", entry.Importer);
        Assert.Single(entry.CacheFiles);
    }

    [Fact]
    public void ImportAllKeepsUnchangedSourcesAndReimportsWhenDependencyChanges()
    {
        using var project = ResourceImportTestProject.Create();
        project.WriteText("textures/player.txt", "first");
        project.WriteResource(
            "characters/player.e2res",
            uid: 123456789L,
            externalReferences:
            [
                new Electron2D.ResourceFileExternalReference(
                    id: 1,
                    uid: 987654321L,
                    path: "res://textures/player.txt",
                    type: "Electron2D.Texture2D")
            ]);

        var first = project.CreatePipeline().ImportAll();
        Assert.Equal(Electron2D.ResourceImportItemStatus.Imported, Assert.Single(first.Items).Status);

        var second = project.CreatePipeline().ImportAll();
        var unchanged = Assert.Single(second.Items);
        Assert.Equal(Electron2D.ResourceImportItemStatus.UpToDate, unchanged.Status);
        Assert.Equal(Electron2D.ResourceImportReason.None, unchanged.Reason);

        project.WriteText("textures/player.txt", "changed");

        var third = project.CreatePipeline().ImportAll();
        var reimported = Assert.Single(third.Items);
        Assert.Equal(Electron2D.ResourceImportItemStatus.Imported, reimported.Status);
        Assert.Equal(Electron2D.ResourceImportReason.DependencyChanged, reimported.Reason);
    }

    [Fact]
    public void ImportAllDoesNotCorruptPreviousCachedArtifactWhenSourceBecomesInvalid()
    {
        using var project = ResourceImportTestProject.Create();
        project.WriteResource(
            "characters/player.e2res",
            uid: 123456789L,
            externalReferences: Array.Empty<Electron2D.ResourceFileExternalReference>());

        var first = project.CreatePipeline().ImportAll();
        var firstItem = Assert.Single(first.Items);
        var cachedFile = firstItem.CacheFiles[0];
        var cachedText = File.ReadAllText(cachedFile);
        var manifestText = File.ReadAllText(project.ManifestPath);

        project.WriteText("characters/player.e2res", "{ this is not valid json");

        var failed = project.CreatePipeline().ImportAll();
        var failedItem = Assert.Single(failed.Items);
        Assert.Equal(Electron2D.ResourceImportItemStatus.Failed, failedItem.Status);
        Assert.Equal(Electron2D.ResourceImportReason.SourceChanged, failedItem.Reason);
        Assert.Contains("malformed", failedItem.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(cachedText, File.ReadAllText(cachedFile));
        Assert.Equal(manifestText, File.ReadAllText(project.ManifestPath));
    }

    [Fact]
    public void ImportAllPrunesCacheEntryWhenSourceAssetIsRemoved()
    {
        using var project = ResourceImportTestProject.Create();
        var source = project.WriteResource(
            "characters/player.e2res",
            uid: 123456789L,
            externalReferences: Array.Empty<Electron2D.ResourceFileExternalReference>());

        var first = project.CreatePipeline().ImportAll();
        var cachedFile = Assert.Single(first.Items).CacheFiles[0];
        Assert.True(File.Exists(cachedFile));

        File.Delete(source);

        var second = project.CreatePipeline().ImportAll();
        var pruned = Assert.Single(second.PrunedItems);
        Assert.Equal(Electron2D.ResourceImportItemStatus.Pruned, pruned.Status);
        Assert.Equal("res://characters/player.e2res", pruned.SourcePath);
        Assert.False(File.Exists(cachedFile));
        Assert.Empty(project.ReadManifest().Entries);
    }

    private sealed class ResourceImportTestProject : IDisposable
    {
        private ResourceImportTestProject(string root)
        {
            Root = root;
            SourceRoot = Path.Combine(root, "sources");
            CacheRoot = Path.Combine(root, ".electron2d", "import-cache");
            Directory.CreateDirectory(SourceRoot);
            Directory.CreateDirectory(CacheRoot);
        }

        public string Root { get; }

        public string SourceRoot { get; }

        public string CacheRoot { get; }

        public string ManifestPath => Path.Combine(CacheRoot, "import-cache.json");

        public static ResourceImportTestProject Create()
        {
            return new ResourceImportTestProject(Path.Combine(
                Path.GetTempPath(),
                "Electron2D.ResourceImportTests",
                Guid.NewGuid().ToString("N")));
        }

        public Electron2D.ResourceImportPipeline CreatePipeline()
        {
            return new Electron2D.ResourceImportPipeline(new Electron2D.ResourceImportOptions(
                Root,
                SourceRoot,
                CacheRoot,
                [new Electron2D.ResourceFileImporter()]));
        }

        public Electron2D.ResourceImportManifest ReadManifest()
        {
            return Electron2D.ResourceImportManifestTextSerializer.Deserialize(File.ReadAllText(ManifestPath));
        }

        public string WriteResource(
            string relativePath,
            long uid,
            IReadOnlyList<Electron2D.ResourceFileExternalReference> externalReferences)
        {
            var resourcePath = "res://" + relativePath.Replace('\\', '/');
            var document = new Electron2D.ResourceFileDocument(
                uid,
                "Electron2D.Resource",
                resourcePath,
                externalReferences,
                Array.Empty<Electron2D.ResourceFileInternalResource>(),
                new Dictionary<string, Electron2D.Variant>(StringComparer.Ordinal)
                {
                    ["resource_name"] = Path.GetFileNameWithoutExtension(relativePath)
                });

            return WriteText(relativePath, Electron2D.ResourceFileTextSerializer.Serialize(document));
        }

        public string WriteText(string relativePath, string text)
        {
            var path = Path.Combine(SourceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text);
            return path;
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
