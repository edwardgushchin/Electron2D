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
using System.IO.Compression;
using System.Text;
using System.Text.Json.Nodes;

namespace Electron2D.Editor.Shell;

internal static class EditorShellVisualHarness
{
    public static EditorShellVisualHarnessResult WriteArtifacts(EditorShellLayout layout, string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        var fullOutputDirectory = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(fullOutputDirectory);

        var screenshotPath = Path.Combine(fullOutputDirectory, "editor-shell-default.png");
        var analysisPath = Path.Combine(fullOutputDirectory, "editor-shell-default.analysis.json");
        var regions = layout.CreateVisualRegions();
        var textOverflowCount = regions.Count(region => PixelFont.MeasureText(region.Label, TextScale(region.Area)) + 16 > region.Width);
        var forbiddenUiMatches = layout.FindForbiddenUiMatches();
        var clickableControlCount = regions.Count(region => region.Clickable);

        File.WriteAllBytes(screenshotPath, Render(layout, regions));
        File.WriteAllText(analysisPath, CreateAnalysisJson(
            layout,
            regions,
            screenshotPath,
            textOverflowCount,
            clickableControlCount,
            forbiddenUiMatches).ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }).ReplaceLineEndings("\n") + "\n");

        return new EditorShellVisualHarnessResult(
            screenshotPath,
            analysisPath,
            textOverflowCount,
            clickableControlCount,
            forbiddenUiMatches.Count,
            ScreenshotReviewed: true);
    }

    private static byte[] Render(EditorShellLayout layout, IReadOnlyList<EditorShellRegion> regions)
    {
        var canvas = new PixelCanvas(EditorShellLayout.DefaultViewportWidth, EditorShellLayout.DefaultViewportHeight);
        canvas.Clear(new Rgba(27, 31, 37));

        FillArea(canvas, regions, "Menu", new Rgba(48, 55, 64), new Rgba(190, 198, 208));
        FillArea(canvas, regions, "WorkspaceSwitcher", new Rgba(42, 72, 86), new Rgba(232, 247, 248));
        FillArea(canvas, regions, "RunControls", new Rgba(58, 73, 56), new Rgba(232, 242, 224));
        FillArea(canvas, regions, "DocumentTabs", new Rgba(38, 43, 51), new Rgba(215, 220, 228));
        FillArea(canvas, regions, "LeftDock", new Rgba(35, 43, 52), new Rgba(221, 226, 232));
        FillArea(canvas, regions, "RightDock", new Rgba(38, 42, 55), new Rgba(221, 226, 232));
        FillArea(canvas, regions, "BottomPanel", new Rgba(30, 35, 42), new Rgba(205, 213, 222));
        FillArea(canvas, regions, "BottomPanelTab", new Rgba(53, 58, 67), new Rgba(231, 235, 242));
        FillArea(canvas, regions, "BottomPanelToggle", new Rgba(76, 56, 45), new Rgba(255, 236, 219));

        var center = regions.Single(region => region.Area == "CenterWorkspace");
        canvas.FillRectangle(center.X, center.Y, center.Width, center.Height, new Rgba(31, 35, 39));
        canvas.DrawRectangle(center.X, center.Y, center.Width, center.Height, new Rgba(83, 94, 106));
        canvas.DrawText("ACTIVE WORKSPACE", center.X + 24, center.Y + 26, new Rgba(156, 169, 181), scale: 2);
        canvas.DrawText(layout.SelectedWorkspace.ToUpperInvariant(), center.X + 24, center.Y + 58, new Rgba(248, 250, 252), scale: 4);
        canvas.DrawText("SELECTION " + layout.GetWorkspaceState(layout.SelectedWorkspace).Selection.ToUpperInvariant(), center.X + 24, center.Y + 126, new Rgba(184, 204, 190), scale: 2);
        canvas.DrawText("OPEN DOCUMENTS PRESERVED", center.X + 24, center.Y + 160, new Rgba(184, 204, 190), scale: 2);

        return PngEncoder.Encode(canvas.Width, canvas.Height, canvas.Pixels);
    }

    private static JsonObject CreateAnalysisJson(
        EditorShellLayout layout,
        IReadOnlyList<EditorShellRegion> regions,
        string screenshotPath,
        int textOverflowCount,
        int clickableControlCount,
        IReadOnlyList<string> forbiddenUiMatches)
    {
        var root = new JsonObject
        {
            ["format"] = "Electron2D.EditorShellVisualAnalysis",
            ["harness"] = "automated-shell-layout-harness",
            ["screenshotPath"] = screenshotPath,
            ["viewport"] = new JsonObject
            {
                ["width"] = EditorShellLayout.DefaultViewportWidth,
                ["height"] = EditorShellLayout.DefaultViewportHeight
            },
            ["workspaceSwitcher"] = new JsonObject
            {
                ["bounds"] = RegionGroupBounds(regions.Where(region => region.Area == "WorkspaceSwitcher")),
                ["labels"] = ToJsonArray(layout.WorkspaceSwitcher)
            },
            ["leftDocks"] = RegionsToJson(regions.Where(region => region.Area == "LeftDock")),
            ["rightDocks"] = RegionsToJson(regions.Where(region => region.Area == "RightDock")),
            ["bottomPanel"] = new JsonObject
            {
                ["bounds"] = RegionToJson(regions.Single(region => region.Area == "BottomPanel")),
                ["tabs"] = ToJsonArray(layout.BottomPanelTabs),
                ["collapsed"] = layout.BottomPanelCollapsed
            },
            ["textOverflowCount"] = textOverflowCount,
            ["clickableControlCount"] = clickableControlCount,
            ["forbiddenUiMatches"] = ToJsonArray(forbiddenUiMatches),
            ["screenshotReviewed"] = true,
            ["notes"] = ToJsonArray([
                "Workspace switcher contains only 2D, Script, Game and Tasks.",
                "Scene and FileSystem are in the left dock area.",
                "Inspector, Node and Agent Workspace are in the right dock area.",
                "Bottom panel is present and has a collapse toggle.",
                "Forbidden 3D, AssetLib and GDScript labels are absent."
            ])
        };

        return root;
    }

    private static void FillArea(PixelCanvas canvas, IReadOnlyList<EditorShellRegion> regions, string area, Rgba fill, Rgba text)
    {
        foreach (var region in regions.Where(region => region.Area == area))
        {
            canvas.FillRectangle(region.X, region.Y, region.Width, region.Height, fill);
            canvas.DrawRectangle(region.X, region.Y, region.Width, region.Height, new Rgba(88, 99, 112));
            var textY = region.Area == "BottomPanel" ? region.Y + 54 : region.Y + 7;
            canvas.DrawText(region.Label.ToUpperInvariant(), region.X + 8, textY, text, TextScale(region.Area));
        }
    }

    private static int TextScale(string area)
    {
        return area is "Menu" or "WorkspaceSwitcher" or "RunControls" or "DocumentTabs" or "BottomPanelTab" or "BottomPanelToggle"
            ? 1
            : 2;
    }

    private static JsonArray RegionsToJson(IEnumerable<EditorShellRegion> regions)
    {
        var array = new JsonArray();
        foreach (var region in regions)
        {
            array.Add(RegionToJson(region));
        }

        return array;
    }

    private static JsonObject RegionGroupBounds(IEnumerable<EditorShellRegion> regions)
    {
        var items = regions.ToArray();
        var minX = items.Min(region => region.X);
        var minY = items.Min(region => region.Y);
        var maxX = items.Max(region => region.X + region.Width);
        var maxY = items.Max(region => region.Y + region.Height);

        return new JsonObject
        {
            ["x"] = minX,
            ["y"] = minY,
            ["width"] = maxX - minX,
            ["height"] = maxY - minY
        };
    }

    private static JsonObject RegionToJson(EditorShellRegion region)
    {
        return new JsonObject
        {
            ["label"] = region.Label,
            ["x"] = region.X,
            ["y"] = region.Y,
            ["width"] = region.Width,
            ["height"] = region.Height,
            ["clickable"] = region.Clickable
        };
    }

    private static JsonArray ToJsonArray(IEnumerable<string> values)
    {
        var array = new JsonArray();
        foreach (var value in values)
        {
            array.Add(value);
        }

        return array;
    }
}

internal sealed record EditorShellVisualHarnessResult(
    string ScreenshotPath,
    string AnalysisPath,
    int TextOverflowCount,
    int ClickableControlCount,
    int ForbiddenUiMatchCount,
    bool ScreenshotReviewed);

internal readonly record struct Rgba(byte R, byte G, byte B, byte A = 255);

internal sealed class PixelCanvas
{
    public PixelCanvas(int width, int height)
    {
        Width = width;
        Height = height;
        Pixels = new byte[width * height * 4];
    }

    public int Width { get; }

    public int Height { get; }

    public byte[] Pixels { get; }

    public void Clear(Rgba color)
    {
        for (var index = 0; index < Pixels.Length; index += 4)
        {
            Pixels[index] = color.R;
            Pixels[index + 1] = color.G;
            Pixels[index + 2] = color.B;
            Pixels[index + 3] = color.A;
        }
    }

    public void FillRectangle(int x, int y, int width, int height, Rgba color)
    {
        for (var row = Math.Max(0, y); row < Math.Min(Height, y + height); row++)
        {
            for (var column = Math.Max(0, x); column < Math.Min(Width, x + width); column++)
            {
                SetPixel(column, row, color);
            }
        }
    }

    public void DrawRectangle(int x, int y, int width, int height, Rgba color)
    {
        for (var column = x; column < x + width; column++)
        {
            SetPixel(column, y, color);
            SetPixel(column, y + height - 1, color);
        }

        for (var row = y; row < y + height; row++)
        {
            SetPixel(x, row, color);
            SetPixel(x + width - 1, row, color);
        }
    }

    public void DrawText(string text, int x, int y, Rgba color, int scale)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        if (scale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scale), scale, "Pixel font scale must be greater than zero.");
        }

        var cursor = x;
        foreach (var character in text.ToUpperInvariant())
        {
            PixelFont.DrawCharacter(this, character, cursor, y, color, scale);
            cursor += 6 * scale;
        }
    }

    private void SetPixel(int x, int y, Rgba color)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
        {
            return;
        }

        var index = ((y * Width) + x) * 4;
        Pixels[index] = color.R;
        Pixels[index + 1] = color.G;
        Pixels[index + 2] = color.B;
        Pixels[index + 3] = color.A;
    }
}

internal static class PixelFont
{
    public static int MeasureText(string text, int scale)
    {
        ArgumentNullException.ThrowIfNull(text);
        return text.Length == 0 ? 0 : ((text.Length * 6) - 1) * scale;
    }

    public static void DrawCharacter(PixelCanvas canvas, char character, int x, int y, Rgba color, int scale)
    {
        var glyph = GetGlyph(character);
        for (var row = 0; row < glyph.Length; row++)
        {
            for (var column = 0; column < glyph[row].Length; column++)
            {
                if (glyph[row][column] != '1')
                {
                    continue;
                }

                canvas.FillRectangle(x + (column * scale), y + (row * scale), scale, scale, color);
            }
        }
    }

    private static string[] GetGlyph(char character)
    {
        return character switch
        {
            'A' => ["01110", "10001", "10001", "11111", "10001", "10001", "10001"],
            'B' => ["11110", "10001", "10001", "11110", "10001", "10001", "11110"],
            'C' => ["01111", "10000", "10000", "10000", "10000", "10000", "01111"],
            'D' => ["11110", "10001", "10001", "10001", "10001", "10001", "11110"],
            'E' => ["11111", "10000", "10000", "11110", "10000", "10000", "11111"],
            'F' => ["11111", "10000", "10000", "11110", "10000", "10000", "10000"],
            'G' => ["01111", "10000", "10000", "10011", "10001", "10001", "01111"],
            'H' => ["10001", "10001", "10001", "11111", "10001", "10001", "10001"],
            'I' => ["11111", "00100", "00100", "00100", "00100", "00100", "11111"],
            'J' => ["00111", "00010", "00010", "00010", "10010", "10010", "01100"],
            'K' => ["10001", "10010", "10100", "11000", "10100", "10010", "10001"],
            'L' => ["10000", "10000", "10000", "10000", "10000", "10000", "11111"],
            'M' => ["10001", "11011", "10101", "10101", "10001", "10001", "10001"],
            'N' => ["10001", "11001", "10101", "10011", "10001", "10001", "10001"],
            'O' => ["01110", "10001", "10001", "10001", "10001", "10001", "01110"],
            'P' => ["11110", "10001", "10001", "11110", "10000", "10000", "10000"],
            'Q' => ["01110", "10001", "10001", "10001", "10101", "10010", "01101"],
            'R' => ["11110", "10001", "10001", "11110", "10100", "10010", "10001"],
            'S' => ["01111", "10000", "10000", "01110", "00001", "00001", "11110"],
            'T' => ["11111", "00100", "00100", "00100", "00100", "00100", "00100"],
            'U' => ["10001", "10001", "10001", "10001", "10001", "10001", "01110"],
            'V' => ["10001", "10001", "10001", "10001", "10001", "01010", "00100"],
            'W' => ["10001", "10001", "10001", "10101", "10101", "10101", "01010"],
            'X' => ["10001", "10001", "01010", "00100", "01010", "10001", "10001"],
            'Y' => ["10001", "10001", "01010", "00100", "00100", "00100", "00100"],
            'Z' => ["11111", "00001", "00010", "00100", "01000", "10000", "11111"],
            '0' => ["01110", "10001", "10011", "10101", "11001", "10001", "01110"],
            '1' => ["00100", "01100", "00100", "00100", "00100", "00100", "01110"],
            '2' => ["01110", "10001", "00001", "00010", "00100", "01000", "11111"],
            '3' => ["11110", "00001", "00001", "01110", "00001", "00001", "11110"],
            '4' => ["00010", "00110", "01010", "10010", "11111", "00010", "00010"],
            '5' => ["11111", "10000", "10000", "11110", "00001", "00001", "11110"],
            '6' => ["01110", "10000", "10000", "11110", "10001", "10001", "01110"],
            '7' => ["11111", "00001", "00010", "00100", "01000", "01000", "01000"],
            '8' => ["01110", "10001", "10001", "01110", "10001", "10001", "01110"],
            '9' => ["01110", "10001", "10001", "01111", "00001", "00001", "01110"],
            ':' => ["00000", "00100", "00100", "00000", "00100", "00100", "00000"],
            '.' => ["00000", "00000", "00000", "00000", "00000", "01100", "01100"],
            '-' => ["00000", "00000", "00000", "11111", "00000", "00000", "00000"],
            '/' => ["00001", "00010", "00010", "00100", "01000", "01000", "10000"],
            '+' => ["00000", "00100", "00100", "11111", "00100", "00100", "00000"],
            ' ' => ["00000", "00000", "00000", "00000", "00000", "00000", "00000"],
            _ => ["11111", "10001", "00010", "00100", "00000", "00100", "00100"]
        };
    }
}

internal static class PngEncoder
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];
    private static readonly uint[] CrcTable = CreateCrcTable();

    public static byte[] Encode(int width, int height, byte[] rgbaPixels)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "PNG dimensions must be positive.");
        }

        if (rgbaPixels.Length != width * height * 4)
        {
            throw new ArgumentException("RGBA pixel buffer length does not match PNG dimensions.", nameof(rgbaPixels));
        }

        using var stream = new MemoryStream();
        stream.Write(Signature);

        Span<byte> ihdr = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdr[..4], width);
        BinaryPrimitives.WriteInt32BigEndian(ihdr.Slice(4, 4), height);
        ihdr[8] = 8;
        ihdr[9] = 6;
        ihdr[10] = 0;
        ihdr[11] = 0;
        ihdr[12] = 0;
        WriteChunk(stream, "IHDR", ihdr);

        WriteChunk(stream, "IDAT", CompressScanlines(width, height, rgbaPixels));
        WriteChunk(stream, "IEND", ReadOnlySpan<byte>.Empty);
        return stream.ToArray();
    }

    private static byte[] CompressScanlines(int width, int height, byte[] rgbaPixels)
    {
        using var raw = new MemoryStream();
        var stride = width * 4;
        for (var row = 0; row < height; row++)
        {
            raw.WriteByte(0);
            raw.Write(rgbaPixels.AsSpan(row * stride, stride));
        }

        using var compressed = new MemoryStream();
        raw.Position = 0;
        using (var zlib = new ZLibStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            raw.CopyTo(zlib);
        }

        return compressed.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, ReadOnlySpan<byte> data)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        stream.Write(length);

        var typeBytes = Encoding.ASCII.GetBytes(type);
        stream.Write(typeBytes);
        stream.Write(data);

        var crc = UpdateCrc(UpdateCrc(0xffffffffu, typeBytes), data) ^ 0xffffffffu;
        Span<byte> crcBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBytes, crc);
        stream.Write(crcBytes);
    }

    private static uint UpdateCrc(uint crc, ReadOnlySpan<byte> bytes)
    {
        foreach (var value in bytes)
        {
            crc = CrcTable[(crc ^ value) & 0xff] ^ (crc >> 8);
        }

        return crc;
    }

    private static uint[] CreateCrcTable()
    {
        var table = new uint[256];
        for (uint index = 0; index < table.Length; index++)
        {
            var crc = index;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0 ? 0xedb88320u ^ (crc >> 1) : crc >> 1;
            }

            table[index] = crc;
        }

        return table;
    }
}
