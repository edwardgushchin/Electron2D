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

public sealed class DataStabilityStressTests
{
    [Fact]
    public void SceneAndResourceDocumentsSurviveOneHundredSaveLoadCyclesWithoutPropertyLoss()
    {
        var resource = new Electron2D.SerializedResourceDocument(
            uid: 123456789L,
            type: "Electron2D.Resource",
            path: "res://data/stats.e2res",
            externalReferences:
            [
                new Electron2D.ResourceFileExternalReference(
                    id: 1,
                    uid: 987654321L,
                    path: "res://textures/player.png",
                    type: "Electron2D.Texture2D")
            ],
            properties: CreateStressProperties());
        var scene = new Electron2D.SceneFileDocument(
            externalReferences:
            [
                new Electron2D.ResourceFileExternalReference(
                    id: 1,
                    uid: 987654321L,
                    path: "res://textures/player.png",
                    type: "Electron2D.Texture2D")
            ],
            internalResources:
            [
                new Electron2D.SerializedResourceEntry(1, "Electron2D.Resource", CreateStressProperties())
            ],
            nodes:
            [
                new Electron2D.SceneFileNode(
                    id: 1,
                    type: "Electron2D.Node2D",
                    name: "Root",
                    parentId: null,
                    ownerId: null,
                    persistentGroups: ["level"],
                    properties: CreateStressProperties()),
                new Electron2D.SceneFileNode(
                    id: 2,
                    type: "Electron2D.Sprite2D",
                    name: "Player",
                    parentId: 1,
                    ownerId: 1,
                    persistentGroups: ["persisted"],
                    properties: new Dictionary<string, Electron2D.SerializedPropertyValue>(StringComparer.Ordinal)
                    {
                        ["texture"] = Electron2D.SerializedPropertyValue.ExternalResource(1),
                        ["stats"] = Electron2D.SerializedPropertyValue.InternalResource(1)
                    })
            ]);

        var resourceText = Electron2D.SerializedResourceTextSerializer.Serialize(resource);
        var sceneText = Electron2D.SceneFileTextSerializer.Serialize(scene);

        for (var cycle = 0; cycle < 100; cycle++)
        {
            resourceText = Electron2D.SerializedResourceTextSerializer.Serialize(
                Electron2D.SerializedResourceTextSerializer.Deserialize(resourceText));
            sceneText = Electron2D.SceneFileTextSerializer.Serialize(
                Electron2D.SceneFileTextSerializer.Deserialize(sceneText));
        }

        Assert.Equal(Electron2D.SerializedResourceTextSerializer.Serialize(resource), resourceText);
        Assert.Equal(Electron2D.SceneFileTextSerializer.Serialize(scene), sceneText);
    }

    [Fact]
    public void ImportCacheKeepsArtifactWhenResourceIsMovedWithSameUid()
    {
        using var project = ResourceImportStressProject.Create();
        var originalSource = project.WriteResource("characters/player.e2res", 123456789L);

        var first = project.CreatePipeline().ImportAll();
        var firstItem = Assert.Single(first.Items);
        var originalCacheFile = Assert.Single(firstItem.CacheFiles);
        var originalCachedText = File.ReadAllText(originalCacheFile);

        File.Delete(originalSource);
        project.WriteResource("actors/player.e2res", 123456789L);

        var second = project.CreatePipeline().ImportAll();
        var moved = Assert.Single(second.Items);
        var pruned = Assert.Single(second.PrunedItems);
        var manifestEntry = Assert.Single(project.ReadManifest().Entries);

        Assert.Equal(Electron2D.ResourceImportItemStatus.Imported, moved.Status);
        Assert.Equal(Electron2D.ResourceImportItemStatus.Pruned, pruned.Status);
        Assert.Equal("res://actors/player.e2res", manifestEntry.SourcePath);
        Assert.Equal(123456789L, manifestEntry.Uid);
        Assert.True(File.Exists(originalCacheFile));
        Assert.Contains("res://actors/player.e2res", File.ReadAllText(originalCacheFile), StringComparison.Ordinal);
        Assert.NotEqual(originalCachedText, File.ReadAllText(originalCacheFile));
    }

    [Fact]
    public void ImportCacheRebuildsAfterCacheRootIsDeleted()
    {
        using var project = ResourceImportStressProject.Create();
        project.WriteResource("characters/player.e2res", 123456789L);

        var first = project.CreatePipeline().ImportAll();
        Assert.True(File.Exists(Assert.Single(first.Items).CacheFiles[0]));

        Directory.Delete(project.CacheRoot, recursive: true);

        var rebuilt = project.CreatePipeline().ImportAll();
        var rebuiltItem = Assert.Single(rebuilt.Items);
        var manifestEntry = Assert.Single(project.ReadManifest().Entries);

        Assert.Equal(Electron2D.ResourceImportItemStatus.Imported, rebuiltItem.Status);
        Assert.Equal(123456789L, manifestEntry.Uid);
        Assert.True(File.Exists(Assert.Single(rebuiltItem.CacheFiles)));
        Assert.False(Directory.Exists(Path.Combine(project.SourceRoot, ".electron2d")));
    }

    [Fact]
    public void ImportCacheReportsCorruptionWithoutReplacingPreviousValidArtifact()
    {
        using var project = ResourceImportStressProject.Create();
        project.WriteResource("characters/player.e2res", 123456789L);

        var first = project.CreatePipeline().ImportAll();
        var cachedFile = Assert.Single(Assert.Single(first.Items).CacheFiles);
        var cachedText = File.ReadAllText(cachedFile);
        var manifestText = File.ReadAllText(project.ManifestPath);

        project.WriteText("characters/player.e2res", "{ corrupted");

        var failed = project.CreatePipeline().ImportAll();
        var item = Assert.Single(failed.Items);

        Assert.Equal(Electron2D.ResourceImportItemStatus.Failed, item.Status);
        Assert.Contains("malformed", item.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(cachedText, File.ReadAllText(cachedFile));
        Assert.Equal(manifestText, File.ReadAllText(project.ManifestPath));
    }

    private static IReadOnlyDictionary<string, Electron2D.SerializedPropertyValue> CreateStressProperties()
    {
        return new Dictionary<string, Electron2D.SerializedPropertyValue>(StringComparer.Ordinal)
        {
            ["alignment"] = Electron2D.SerializedPropertyValue.FromEnum(Electron2D.HorizontalAlignment.Center),
            ["maybe_lives"] = Electron2D.SerializedPropertyValue.FromNullable(
                typeof(int),
                Electron2D.SerializedPropertyValue.FromVariant(3)),
            ["numbers"] = Electron2D.SerializedPropertyValue.FromArray(
            [
                Electron2D.SerializedPropertyValue.FromVariant(1),
                Electron2D.SerializedPropertyValue.FromVariant(2),
                Electron2D.SerializedPropertyValue.FromVariant(3)
            ]),
            ["scores"] = Electron2D.SerializedPropertyValue.FromDictionary(
            [
                new Electron2D.SerializedPropertyDictionaryEntry(
                    Electron2D.SerializedPropertyValue.FromVariant("coins"),
                    Electron2D.SerializedPropertyValue.FromVariant(12)),
                new Electron2D.SerializedPropertyDictionaryEntry(
                    Electron2D.SerializedPropertyValue.FromVariant("gems"),
                    Electron2D.SerializedPropertyValue.FromVariant(2))
            ]),
            ["texture"] = Electron2D.SerializedPropertyValue.ExternalResource(1)
        };
    }

    private sealed class ResourceImportStressProject : IDisposable
    {
        private ResourceImportStressProject(string root)
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

        public static ResourceImportStressProject Create()
        {
            return new ResourceImportStressProject(Path.Combine(
                Path.GetTempPath(),
                "Electron2D.DataStabilityStressTests",
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

        public string WriteResource(string relativePath, long uid)
        {
            var resourcePath = "res://" + relativePath.Replace('\\', '/');
            var document = new Electron2D.ResourceFileDocument(
                uid,
                "Electron2D.Resource",
                resourcePath,
                Array.Empty<Electron2D.ResourceFileExternalReference>(),
                Array.Empty<Electron2D.ResourceFileInternalResource>(),
                new Dictionary<string, Electron2D.Variant>(StringComparer.Ordinal)
                {
                    ["resource_name"] = Path.GetFileNameWithoutExtension(relativePath),
                    ["path_echo"] = resourcePath
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
