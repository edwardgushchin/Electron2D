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
using System.Buffers.Binary;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class TextureImageImportTests
{
    [Fact]
    public void TextureImageImporterImportsPngMetadataSidecarAndAtlasRegions()
    {
        using var project = TextureImportTestProject.Create();
        project.WriteBytes("textures/player.png", TextureImportTestProject.CreatePng(32, 16, colorType: 6));
        project.WriteText(
            "textures/player.png.e2import.json",
            """
            {
              "filter": "Nearest",
              "repeat": "Mirror",
              "mipmaps": true,
              "atlas": [
                {
                  "name": "idle",
                  "region": { "x": 2, "y": 4, "width": 16, "height": 8 },
                  "margin": { "x": 1, "y": 1, "width": 2, "height": 2 },
                  "filterClip": true
                }
              ],
              "platforms": [
                { "name": "desktop", "format": "rgba8", "quality": 100 },
                { "name": "android", "format": "etc2", "quality": 80 }
              ]
            }
            """);

        var report = project.CreatePipeline().ImportAll();

        var item = Assert.Single(report.Items);
        Assert.Equal(Electron2D.ResourceImportItemStatus.Imported, item.Status);
        Assert.Equal(Electron2D.ResourceImportReason.NewSource, item.Reason);

        var metadata = project.ReadTextureMetadata(item);
        Assert.Equal("res://textures/player.png", metadata.SourcePath);
        Assert.Equal(Electron2D.TextureImageFormat.Png, metadata.Format);
        Assert.Equal(32, metadata.Width);
        Assert.Equal(16, metadata.Height);
        Assert.True(metadata.HasAlpha);
        Assert.True(metadata.HasMipmaps);
        Assert.Equal(6, metadata.MipmapCount);
        Assert.Equal(Electron2D.TextureFilterMode.Nearest, metadata.Sampling.FilterMode);
        Assert.Equal(Electron2D.TextureRepeatMode.Mirror, metadata.Sampling.RepeatMode);
        Assert.Equal(["android", "desktop"], metadata.PlatformVariants.Select(variant => variant.Name).ToArray());

        var region = Assert.Single(metadata.AtlasRegions);
        Assert.Equal("idle", region.Name);
        Assert.Equal(new Electron2D.Rect2(2f, 4f, 16f, 8f), region.Region);
        Assert.Equal(new Electron2D.Rect2(1f, 1f, 2f, 2f), region.Margin);
        Assert.True(region.FilterClip);

        var texture = Electron2D.TextureImportResourceFactory.CreateTexture(metadata);
        Assert.Equal(32, texture.GetWidth());
        Assert.Equal(16, texture.GetHeight());
        Assert.True(texture.HasAlpha());
        Assert.True(texture.HasMipmaps());

        var atlas = Assert.Single(Electron2D.TextureImportResourceFactory.CreateAtlasTextures(metadata, texture));
        Assert.Equal(16, atlas.GetWidth());
        Assert.Equal(8, atlas.GetHeight());
        Assert.True(atlas.FilterClip);
        Assert.True(atlas.HasAlpha());
    }

    [Fact]
    public void TextureImageImporterReadsJpegMetadataWithoutAlpha()
    {
        using var project = TextureImportTestProject.Create();
        project.WriteBytes("textures/photo.jpg", TextureImportTestProject.CreateJpeg(11, 7));

        var report = project.CreatePipeline().ImportAll();

        var metadata = project.ReadTextureMetadata(Assert.Single(report.Items));
        Assert.Equal(Electron2D.TextureImageFormat.Jpeg, metadata.Format);
        Assert.Equal(11, metadata.Width);
        Assert.Equal(7, metadata.Height);
        Assert.False(metadata.HasAlpha);
        Assert.False(metadata.HasMipmaps);
        Assert.Equal(0, metadata.MipmapCount);
        Assert.Equal(Electron2D.TextureFilterMode.Linear, metadata.Sampling.FilterMode);
        Assert.Equal(Electron2D.TextureRepeatMode.Disabled, metadata.Sampling.RepeatMode);
        Assert.Empty(metadata.AtlasRegions);
        Assert.Empty(metadata.PlatformVariants);
    }

    [Fact]
    public void TextureImageImporterTracksSidecarAsDependency()
    {
        using var project = TextureImportTestProject.Create();
        project.WriteBytes("textures/player.png", TextureImportTestProject.CreatePng(8, 8, colorType: 2));
        project.WriteText("textures/player.png.e2import.json", "{ \"filter\": \"Nearest\" }");

        var first = project.CreatePipeline().ImportAll();
        Assert.Equal(Electron2D.ResourceImportItemStatus.Imported, Assert.Single(first.Items).Status);

        var second = project.CreatePipeline().ImportAll();
        Assert.Equal(Electron2D.ResourceImportItemStatus.UpToDate, Assert.Single(second.Items).Status);

        project.WriteText("textures/player.png.e2import.json", "{ \"filter\": \"Nearest\", \"repeat\": \"Enabled\" }");

        var third = project.CreatePipeline().ImportAll();
        var item = Assert.Single(third.Items);
        Assert.Equal(Electron2D.ResourceImportItemStatus.Imported, item.Status);
        Assert.Equal(Electron2D.ResourceImportReason.DependencyChanged, item.Reason);

        var metadata = project.ReadTextureMetadata(item);
        Assert.Equal(Electron2D.TextureRepeatMode.Enabled, metadata.Sampling.RepeatMode);
    }

    private sealed class TextureImportTestProject : IDisposable
    {
        private TextureImportTestProject(string root)
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

        public static TextureImportTestProject Create()
        {
            return new TextureImportTestProject(Path.Combine(
                Path.GetTempPath(),
                "Electron2D.TextureImportTests",
                Guid.NewGuid().ToString("N")));
        }

        public Electron2D.ResourceImportPipeline CreatePipeline()
        {
            return new Electron2D.ResourceImportPipeline(new Electron2D.ResourceImportOptions(
                Root,
                SourceRoot,
                CacheRoot,
                [new Electron2D.TextureImageImporter()]));
        }

        public Electron2D.TextureImportMetadata ReadTextureMetadata(Electron2D.ResourceImportItemReport item)
        {
            return Electron2D.TextureImportMetadataTextSerializer.Deserialize(File.ReadAllText(Assert.Single(item.CacheFiles)));
        }

        public void WriteBytes(string relativePath, byte[] bytes)
        {
            var path = Path.Combine(SourceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, bytes);
        }

        public void WriteText(string relativePath, string text)
        {
            var path = Path.Combine(SourceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text);
        }

        public static byte[] CreatePng(int width, int height, byte colorType)
        {
            using var stream = new MemoryStream();
            stream.Write([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]);

            Span<byte> ihdr = stackalloc byte[13];
            BinaryPrimitives.WriteInt32BigEndian(ihdr[..4], width);
            BinaryPrimitives.WriteInt32BigEndian(ihdr.Slice(4, 4), height);
            ihdr[8] = 8;
            ihdr[9] = colorType;
            WritePngChunk(stream, "IHDR", ihdr);
            WritePngChunk(stream, "IEND", ReadOnlySpan<byte>.Empty);
            return stream.ToArray();
        }

        public static byte[] CreateJpeg(int width, int height)
        {
            using var stream = new MemoryStream();
            stream.Write([0xff, 0xd8]);
            WriteJpegSegment(stream, 0xe0, new byte[14]);

            Span<byte> sof = stackalloc byte[15];
            sof[0] = 8;
            BinaryPrimitives.WriteUInt16BigEndian(sof.Slice(1, 2), (ushort)height);
            BinaryPrimitives.WriteUInt16BigEndian(sof.Slice(3, 2), (ushort)width);
            sof[5] = 3;
            sof[6] = 1;
            sof[7] = 0x11;
            sof[8] = 0;
            sof[9] = 2;
            sof[10] = 0x11;
            sof[11] = 0;
            sof[12] = 3;
            sof[13] = 0x11;
            sof[14] = 0;
            WriteJpegSegment(stream, 0xc0, sof);
            stream.Write([0xff, 0xd9]);
            return stream.ToArray();
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }

        private static void WritePngChunk(Stream stream, string type, ReadOnlySpan<byte> data)
        {
            Span<byte> length = stackalloc byte[4];
            BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
            stream.Write(length);
            stream.Write(System.Text.Encoding.ASCII.GetBytes(type));
            stream.Write(data);
            stream.Write(stackalloc byte[4]);
        }

        private static void WriteJpegSegment(Stream stream, byte marker, ReadOnlySpan<byte> data)
        {
            stream.WriteByte(0xff);
            stream.WriteByte(marker);
            Span<byte> length = stackalloc byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(length, (ushort)(data.Length + 2));
            stream.Write(length);
            stream.Write(data);
        }
    }
}
