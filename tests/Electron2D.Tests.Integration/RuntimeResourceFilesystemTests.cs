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
using System.Text.Json.Nodes;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class RuntimeResourceFilesystemTests
{
    private const string RgbaPng2x2Base64 = "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAYAAABytg0kAAAAFUlEQVR4nGP4z8DwHwhB4D8QMDQAAD1VB3peF7pjAAAAAElFTkSuQmCC";

    [Fact]
    public void ImageTextureLoadFromFileReadsResPathFromMountedProjectRoot()
    {
        var projectRoot = CreateTemporaryDirectory("Electron2D-ResourceRootTests");
        var texturePath = Path.Combine(projectRoot, "assets", "runtime", "sprite.png");
        Directory.CreateDirectory(Path.GetDirectoryName(texturePath)!);
        File.WriteAllBytes(texturePath, Convert.FromBase64String(RgbaPng2x2Base64));

        using var mount = Electron2D.ResourceFileSystem.MountProjectRoot(projectRoot);

        var texture = Electron2D.ImageTexture.LoadFromFile("res://assets/runtime/sprite.png");

        Assert.Equal(2, texture.GetWidth());
        Assert.Equal(2, texture.GetHeight());
        Assert.True(texture.HasAlpha());
        Assert.False(texture.IsPixelOpaque(1, 0));
    }

    [Fact]
    public void ResourceFileSystemReadsPackEntriesWithoutExtraction()
    {
        var outputRoot = CreateTemporaryDirectory("Electron2D-ResourcePackTests");
        var projectPackPath = Path.Combine(outputRoot, "packs", "project.e2dpkg");
        var assetPackPath = Path.Combine(outputRoot, "packs", "assets", "runtime.e2dpkg");
        Directory.CreateDirectory(Path.GetDirectoryName(projectPackPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(assetPackPath)!);
        WritePackage(
            projectPackPath,
            new Dictionary<string, byte[]>(StringComparer.Ordinal)
            {
                ["project.e2d.json"] = System.Text.Encoding.UTF8.GetBytes("{\"name\":\"Packed\"}"),
                ["data/info.txt"] = System.Text.Encoding.UTF8.GetBytes("packed data")
            });
        WritePackage(
            assetPackPath,
            new Dictionary<string, byte[]>(StringComparer.Ordinal)
            {
                ["assets/runtime/sprite.png"] = Convert.FromBase64String(RgbaPng2x2Base64)
            });

        var manifestPath = Path.Combine(outputRoot, "electron2d.pack.json");
        File.WriteAllText(
            manifestPath,
            new JsonObject
            {
                ["format"] = "Electron2D.ResourcePackManifest",
                ["formatVersion"] = 1,
                ["packs"] = new JsonArray
                {
                    new JsonObject { ["path"] = "packs/project.e2dpkg" },
                    new JsonObject { ["path"] = "packs/assets/runtime.e2dpkg" }
                }
            }.ToJsonString());

        using var mount = Electron2D.ResourceFileSystem.MountPacks(outputRoot, manifestPath);

        Assert.Equal("packed data", Electron2D.ResourceFileSystem.ReadAllText("res://data/info.txt"));
        Assert.False(File.Exists(Path.Combine(outputRoot, "data", "info.txt")), "Resource pack entries must not be extracted as loose files.");
        Assert.False(Directory.Exists(Path.Combine(outputRoot, "assets")), "Resource pack entries must not recreate a loose assets directory.");

        var texture = Electron2D.ImageTexture.LoadFromFile("res://assets/runtime/sprite.png");
        Assert.Equal(2, texture.GetWidth());
        Assert.Equal(2, texture.GetHeight());
    }

    [Fact]
    public void ResourcePackMountingDoesNotBecomePublicGameApi()
    {
        var publicTypes = typeof(Electron2D.Node)
            .Assembly
            .GetExportedTypes()
            .Select(type => type.FullName)
            .ToArray();

        Assert.DoesNotContain("Electron2D.ResourceFileSystem", publicTypes);
        Assert.DoesNotContain("Electron2D.ResourceMount", publicTypes);
    }

    private static void WritePackage(string packagePath, IReadOnlyDictionary<string, byte[]> entries)
    {
        using var archive = ZipFile.Open(packagePath, ZipArchiveMode.Create);
        foreach (var entry in entries.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            var archiveEntry = archive.CreateEntry(entry.Key, CompressionLevel.SmallestSize);
            using var output = archiveEntry.Open();
            output.Write(entry.Value);
        }
    }

    private static string CreateTemporaryDirectory(string prefix)
    {
        var directory = Path.Combine(Path.GetTempPath(), prefix, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
