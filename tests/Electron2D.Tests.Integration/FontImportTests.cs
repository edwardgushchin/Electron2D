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
using System.Text;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class FontImportTests
{
    [Fact]
    public void FontImporterImportsTtfMetadataFallbacksAndSdfPolicy()
    {
        using var project = FontImportTestProject.Create();
        project.WriteBytes("fonts/main.ttf", FontImportTestProject.CreateSfnt("ttf", "Electron Sans", "Regular"));
        project.WriteBytes("fonts/fallback.ttf", FontImportTestProject.CreateSfnt("ttf", "Fallback Sans", "Regular"));
        project.WriteText(
            "fonts/main.ttf.e2import.json",
            """
            {
              "fallbacks": [ "res://fonts/fallback.ttf" ],
              "rasterization": {
                "mode": "Sdf",
                "baseSize": 48,
                "sdfSpread": 8
              }
            }
            """);

        var report = project.CreatePipeline().ImportAll();

        var item = report.Items.Single(item => item.SourcePath == "res://fonts/main.ttf");
        Assert.Equal(Electron2D.ResourceImportItemStatus.Imported, item.Status);

        var metadata = project.ReadFontMetadata(item);
        Assert.Equal(Electron2D.FontSourceFormat.Ttf, metadata.Format);
        Assert.Equal("Electron Sans", metadata.FamilyName);
        Assert.Equal("Regular", metadata.StyleName);
        Assert.Equal("Electron Sans Regular", metadata.FullName);
        Assert.Equal("ElectronSans-Regular", metadata.PostScriptName);
        Assert.Equal(["res://fonts/fallback.ttf"], metadata.FallbackFontPaths);
        Assert.Equal(Electron2D.FontRasterizationMode.Sdf, metadata.Rasterization.Mode);
        Assert.Equal(48, metadata.Rasterization.BaseSize);
        Assert.Equal(8, metadata.Rasterization.SdfSpread);

        var font = Electron2D.FontImportResourceFactory.CreateFont(metadata);
        Assert.Equal(16f, font.GetHeight(16));
        Assert.True(font.HasChar('A'));
    }

    [Fact]
    public void FontImporterReadsOtfMetadataWithDefaultBitmapPolicy()
    {
        using var project = FontImportTestProject.Create();
        project.WriteBytes("fonts/display.otf", FontImportTestProject.CreateSfnt("otf", "Electron Display", "Bold"));

        var report = project.CreatePipeline().ImportAll();

        var metadata = project.ReadFontMetadata(Assert.Single(report.Items));
        Assert.Equal(Electron2D.FontSourceFormat.Otf, metadata.Format);
        Assert.Equal("Electron Display", metadata.FamilyName);
        Assert.Equal("Bold", metadata.StyleName);
        Assert.Equal(Electron2D.FontRasterizationMode.Bitmap, metadata.Rasterization.Mode);
        Assert.Equal(16, metadata.Rasterization.BaseSize);
        Assert.Equal(0, metadata.Rasterization.SdfSpread);
        Assert.Empty(metadata.FallbackFontPaths);
    }

    [Fact]
    public void FontImporterTracksFallbackFontAsDependency()
    {
        using var project = FontImportTestProject.Create();
        project.WriteBytes("fonts/main.ttf", FontImportTestProject.CreateSfnt("ttf", "Electron Sans", "Regular"));
        project.WriteBytes("fonts/fallback.ttf", FontImportTestProject.CreateSfnt("ttf", "Fallback Sans", "Regular"));
        project.WriteText("fonts/main.ttf.e2import.json", "{ \"fallbacks\": [ \"res://fonts/fallback.ttf\" ] }");

        var first = project.CreatePipeline().ImportAll();
        Assert.Equal(Electron2D.ResourceImportItemStatus.Imported, first.Items.Single(item => item.SourcePath == "res://fonts/main.ttf").Status);

        var second = project.CreatePipeline().ImportAll();
        Assert.Equal(Electron2D.ResourceImportItemStatus.UpToDate, second.Items.Single(item => item.SourcePath == "res://fonts/main.ttf").Status);

        project.WriteBytes("fonts/fallback.ttf", FontImportTestProject.CreateSfnt("ttf", "Fallback Sans", "Italic"));

        var third = project.CreatePipeline().ImportAll();
        var primary = third.Items.Single(item => item.SourcePath == "res://fonts/main.ttf");
        Assert.Equal(Electron2D.ResourceImportItemStatus.Imported, primary.Status);
        Assert.Equal(Electron2D.ResourceImportReason.DependencyChanged, primary.Reason);
    }

    private sealed class FontImportTestProject : IDisposable
    {
        private FontImportTestProject(string root)
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

        public static FontImportTestProject Create()
        {
            return new FontImportTestProject(Path.Combine(
                Path.GetTempPath(),
                "Electron2D.FontImportTests",
                Guid.NewGuid().ToString("N")));
        }

        public Electron2D.ResourceImportPipeline CreatePipeline()
        {
            return new Electron2D.ResourceImportPipeline(new Electron2D.ResourceImportOptions(
                Root,
                SourceRoot,
                CacheRoot,
                [new Electron2D.FontImporter()]));
        }

        public Electron2D.FontImportMetadata ReadFontMetadata(Electron2D.ResourceImportItemReport item)
        {
            return Electron2D.FontImportMetadataTextSerializer.Deserialize(File.ReadAllText(Assert.Single(item.CacheFiles)));
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

        public static byte[] CreateSfnt(string kind, string familyName, string styleName)
        {
            var signature = kind == "otf"
                ? Encoding.ASCII.GetBytes("OTTO")
                : new byte[] { 0x00, 0x01, 0x00, 0x00 };
            var nameTable = CreateNameTable(familyName, styleName);
            var tableOffset = 28;

            using var stream = new MemoryStream();
            stream.Write(signature);
            WriteUInt16(stream, 1);
            WriteUInt16(stream, 0);
            WriteUInt16(stream, 0);
            WriteUInt16(stream, 0);
            stream.Write(Encoding.ASCII.GetBytes("name"));
            WriteUInt32(stream, 0);
            WriteUInt32(stream, (uint)tableOffset);
            WriteUInt32(stream, (uint)nameTable.Length);
            stream.Write(nameTable);
            return stream.ToArray();
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }

        private static byte[] CreateNameTable(string familyName, string styleName)
        {
            var records = new[]
            {
                (NameId: 1, Value: familyName),
                (NameId: 2, Value: styleName),
                (NameId: 4, Value: familyName + " " + styleName),
                (NameId: 6, Value: familyName.Replace(" ", string.Empty, StringComparison.Ordinal) + "-" + styleName)
            };
            var encoded = records.Select(record => Encoding.BigEndianUnicode.GetBytes(record.Value)).ToArray();
            var stringOffset = 6 + records.Length * 12;
            var cursor = 0;

            using var stream = new MemoryStream();
            WriteUInt16(stream, 0);
            WriteUInt16(stream, (ushort)records.Length);
            WriteUInt16(stream, (ushort)stringOffset);

            for (var index = 0; index < records.Length; index++)
            {
                WriteUInt16(stream, 3);
                WriteUInt16(stream, 1);
                WriteUInt16(stream, 0x0409);
                WriteUInt16(stream, (ushort)records[index].NameId);
                WriteUInt16(stream, (ushort)encoded[index].Length);
                WriteUInt16(stream, (ushort)cursor);
                cursor += encoded[index].Length;
            }

            foreach (var value in encoded)
            {
                stream.Write(value);
            }

            return stream.ToArray();
        }

        private static void WriteUInt16(Stream stream, ushort value)
        {
            Span<byte> bytes = stackalloc byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(bytes, value);
            stream.Write(bytes);
        }

        private static void WriteUInt32(Stream stream, uint value)
        {
            Span<byte> bytes = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
            stream.Write(bytes);
        }
    }
}
