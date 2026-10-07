using System.Diagnostics;
using System.Text;
using Electron2D;
using EngineFileAccess = Electron2D.FileAccess;

internal static class FontCacheTests
{
    internal static void Run()
    {
        VerifyAuthored(); VerifyDynamic(); VerifyImport();
        Console.WriteLine("Font cache checks passed.");
    }
    private static FontFile Create()
    {
        var font = new FontFile();
        using var image = Image.CreateEmpty(16, 8, false, Image.Format.Rgba8); image.Fill(Colors.Transparent);
        for (var y = 1; y < 7; y++) for (var x = 1; x < 6; x++) image.SetPixel(x, y, Colors.Cyan);
        font.SetTextureImage(0, new(8, 0), 0, image);
        foreach (var scalar in new[] { 65, 66, 0x1F600, 32 })
        {
            font.SetGlyphOffset(0, new(8, 0), scalar, new(0, -6)); font.SetGlyphSize(0, new(8, 0), scalar, scalar == 32 ? Vector2.Zero : new(5, 6));
            font.SetGlyphUVRect(0, new(8, 0), scalar, new(1, 1, 5, 6)); font.SetGlyphTextureIndex(0, new(8, 0), scalar, 0);
            font.SetGlyphAdvance(0, 8, scalar, new(6, 8));
        }
        font.SetCacheAscent(0, 8, 6); font.SetCacheDescent(0, 8, 2); font.SetCacheUnderlinePosition(0, 8, 1); font.SetCacheUnderlineThickness(0, 8, 1);
        font.SetKerning(0, 8, new(65, 66), new(-2, 0));
        return font;
    }
    private static void VerifyAuthored()
    {
        using var empty = new FontFile(); Check(empty.GetCacheCount() == 0 && empty.FixedSize == 0 && empty.FixedSizeScaleMode == FixedSizeScaleMode.Disable && empty.GetCacheCount() == 0, "Size-policy queries do not create caches.");
        Check((int)FixedSizeScaleMode.Disable == 0 && (int)FixedSizeScaleMode.IntegerOnly == 1 && (int)FixedSizeScaleMode.Enabled == 2, "Exact numeric fixed-size modes.");
        using var font = Create();
        Check(font.GetCacheCount() == 1 && font.GetGlyphList(0, new(8, 0)).Length == 4, "Authored cache records are real.");
        Check(font.GetStringSize("AB", fontSize: 8).X == 10, "Bitmap shaping applies kerning to real advances.");
        Check(font.GetStringSize("😀", fontSize: 8).X == 6 && font.HasChar(0x1F600), "Supplementary bitmap scalars use shared shaping.");
        Check(font.GetCharFromGlyphIndex(8, 65) == 65 && font.GetGlyphIndex(8, 65, 0) == 65, "Bitmap scalar/glyph mapping is bidirectional.");
        Check(font.GetAscent(8) == 6 && font.GetDescent(8) == 2 && font.GetUnderlinePosition(8) == 1, "Authored metrics feed Font.");
        font.FixedSize = 8; font.FixedSizeScaleMode = FixedSizeScaleMode.Enabled;
        Check(font.GetStringSize("AB", fontSize: 16).X == 20 && font.GetHeight(16) == 16, "Fixed bitmap size scales metrics/shaping.");
        font.FixedSizeScaleMode = FixedSizeScaleMode.IntegerOnly;
        Check(font.GetStringSize("AB", fontSize: 12).X == 20, "Integer scaling uses the half boundary.");
        font.FixedSizeScaleMode = FixedSizeScaleMode.Disable;
        Check(font.GetStringSize("AB", fontSize: 16).X == 10, "Disabled scaling selects the fixed source.");
        font.SetExtraSpacing(0, TextSpacingType.Glyph, 2);
        Check(font.GetStringSize("AB", fontSize: 8).X == 12, "Primary indexed spacing executes in common layout.");
        using var copy = (FontFile)font.Duplicate();
        Check(copy.GetStringSize("AB", fontSize: 8).X == 12 && copy.GetTextureCount(0, new(8, 0)) == 1, "Duplication keeps independent authored bitmap state.");
        using var queried = font.GetTextureImage(0, new(8, 0), 0)!; queried.Fill(Colors.Red);
        using var unchanged = font.GetTextureImage(0, new(8, 0), 0)!; Check(unchanged.GetPixel(1, 1) == Colors.Cyan, "Image queries cannot mutate owned page pixels.");
        font.SetTextureOffsets(0, new(8, 0), 0, [7, 0, 9, 8]); var offsets = font.GetTextureOffsets(0, new(8, 0), 0); offsets[0] = 0;
        Check(font.GetTextureOffsets(0, new(8, 0), 0)[0] == 7, "Packing tuples have copied ownership.");
        Reject<ArgumentException>(() => font.SetTextureOffsets(0, new(8, 0), 0, [1, 2]));
        Reject<ArgumentOutOfRangeException>(() => font.SetCacheAscent(-1, 8, 1)); Reject<ArgumentOutOfRangeException>(() => font.SetGlyphSize(0, new(8, 0), 65, new(-1, 1)));
        var width = font.GetStringSize("AB", fontSize: 8).X;
        font.Changed += ThrowObserver;
        Reject<MarkerException>(() => font.SetKerning(0, 8, new(65, 66), Vector2.Zero)); font.Changed -= ThrowObserver;
        Check(font.GetStringSize("AB", fontSize: 8).X == width + 2, "Observer failure does not undo committed cache state.");
        using (var lease = font.BeginRead())
        {
            var previousData = font.PrimaryData!; font.SetCacheAscent(0, 8, 7);
            Check(previousData.GetMetrics(8).Ascent == 6 && font.GetAscent(8) == 7, "Published replacement preserves active old data until lease retirement.");
        }
        font.RemoveTexture(0, new(8, 0), 0); Check(font.GetGlyphList(0, new(8, 0)).Length == 4 && font.GetTextureCount(0, new(8, 0)) == 0, "Removing a page retains glyph records.");
        font.ClearGlyphs(0, new(8, 0)); Check(!font.HasChar(65) && copy.HasChar(65), "Clearing glyphs changes support without mutating a duplicate.");
        for (var i = 0; i < 128; i++) copy.GetStringSize("AB", fontSize: 8);
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 128; i++) copy.GetStringSize("AB", fontSize: 8);
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "128 prepared bitmap layout reads allocate zero managed bytes.");
    }
    private static void ThrowObserver(Resource _) => throw new MarkerException();
    private static void VerifyDynamic()
    {
        using var font = new FontFile { Data = FontTestFixtures.OpenSans, SubpixelPositioning = FontSubpixelPositioning.Disabled };
        var glyph = font.GetGlyphIndex(16, 'A', 0); font.RenderRange(0, new(16, 0), 'A', 'C');
        Check(font.GetGlyphList(0, new(16, 0)).Length == 3 && font.GetTextureCount(0, new(16, 0)) == 1, "Pre-rendering populates a real packed atlas.");
        Check(font.PrimaryData!.GetGlyph((uint)glyph, 16, 0, 1).Texture is { RetainRendererCache: true }, "Earlier glyphs follow the live packed atlas texture and retain residency through unused frames.");
        Check(font.GetTextureOffsets(0, new(16, 0), 0).Length % 4 == 0 && font.GetGlyphSize(0, new(16, 0), glyph).X > 0, "Public glyph and packing metadata describe actual pixels.");
        var region = font.GetGlyphUVRect(0, new(16, 0), glyph); using (var atlas = font.GetTextureImage(0, new(16, 0), 0)!)
        {
            var left = (int)region.Position.X; var top = (int)region.Position.Y;
            for (var y = top; y < top + region.Size.Y; y++) Check(atlas.GetPixel(left - 1, y) == atlas.GetPixel(left, y), "Atlas border preserves clamped source edge colors/coverage under linear scaling.");
        }
        var original = font.GetStringSize("A", fontSize: 16).X; font.SetGlyphAdvance(0, 16, glyph, new(30, 20));
        Check(original != 30 && font.GetStringSize("A", fontSize: 16).X == 30, "Authored dynamic glyph advance changes executable layout.");
        font.ClearTextures(0, new(16, 0)); using var page = Image.CreateEmpty(256, 256, false, Image.Format.Rgba8); font.SetTextureImage(0, new(16, 0), 0, page); font.SetTextureOffsets(0, new(16, 0), 0, [0, 100, 256, 40]); font.RenderGlyph(0, new(16, 0), font.GetGlyphIndex(16, 'Z', 0));
        Check(font.GetGlyphUVRect(0, new(16, 0), font.GetGlyphIndex(16, 'Z', 0)).Position.Y >= 100, "Authored shelf metadata changes real native atlas packing.");
        var count = font.GetCacheCount(); font.SetEmbolden(2, 1); Check(font.GetCacheCount() == 3 && font.GetEmbolden(2) == 1, "Indexed instances are independently realized.");
        font.SetGlyphAdvance(2, 16, glyph, new(40, 20)); using var instance = font.FindVariation(strength: 1); Check(instance.GetStringSize("A", fontSize: 16).X == 40, "Retained variation consumes matching authored indexed cache.");
        var prior = font.GetFaceIndex(2); Reject<InvalidOperationException>(() => font.SetFaceIndex(2, 99)); Check(font.GetFaceIndex(2) == prior, "Failed native instance realization rolls back.");
        font.RemoveCache(1); Check(font.GetCacheCount() == 2, "Cache removal compacts indices."); font.ClearCache(); Check(font.GetCacheCount() == 0 && font.HasChar('A'), "ClearCache retains scalable source and rebuilds on demand.");
    }
    private static string Fixture(string directory)
    {
        using var image = Image.CreateEmpty(16, 8, false, Image.Format.Rgba8); image.Fill(Colors.Transparent);
        for (var y = 1; y < 7; y++) for (var x = 1; x < 6; x++) image.SetPixel(x, y, Colors.Cyan);
        image.SavePNG(System.IO.Path.Combine(directory, "page.png"));
        var path = System.IO.Path.Combine(directory, "bitmap.fnt");
        File.WriteAllText(path, "info face=\"Bitmap Fixture\" size=8 bold=1 italic=0 unicode=1 outline=0\ncommon lineHeight=8 base=6 scaleW=16 scaleH=8 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4\npage id=0 file=\"page.png\"\nchars count=2\nchar id=65 x=1 y=1 width=5 height=6 xoffset=0 yoffset=0 xadvance=6 page=0 chnl=15\nchar id=66 x=1 y=1 width=5 height=6 xoffset=0 yoffset=0 xadvance=6 page=0 chnl=15\nkerning first=65 second=66 amount=-2\n", new UTF8Encoding(false));
        return path;
    }
    private static string BinaryFixture(string directory)
    {
        var path = System.IO.Path.Combine(directory, "binary.font"); using var file = File.Create(path); using var output = new BinaryWriter(file);
        output.Write(new byte[] { 66, 77, 70, 3 });
        void Block(byte type, Action<BinaryWriter> write) { using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream, Encoding.UTF8, true); write(writer); output.Write(type); output.Write((uint)stream.Length); output.Write(stream.ToArray()); }
        Block(1, writer => { writer.Write((short)8); writer.Write((byte)10); writer.Write((byte)0); writer.Write((ushort)100); writer.Write((byte)1); writer.Write(new byte[7]); writer.Write(Encoding.UTF8.GetBytes("Binary Fixture\0")); });
        Block(2, writer => { writer.Write((ushort)8); writer.Write((ushort)6); writer.Write((ushort)16); writer.Write((ushort)8); writer.Write((ushort)1); writer.Write(new byte[] { 0, 0, 4, 4, 4 }); });
        Block(3, writer => writer.Write(Encoding.UTF8.GetBytes("page.png\0")));
        Block(4, writer => { foreach (var scalar in new uint[] { 65, 66 }) { writer.Write(scalar); writer.Write((ushort)1); writer.Write((ushort)1); writer.Write((ushort)5); writer.Write((ushort)6); writer.Write((short)0); writer.Write((short)0); writer.Write((short)6); writer.Write((byte)0); writer.Write((byte)15); } });
        Block(5, writer => { writer.Write((uint)65); writer.Write((uint)66); writer.Write((short)-2); });
        return path;
    }
    private static void VerifyImport()
    {
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-bitmap-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var path = Fixture(directory); var binaryPath = BinaryFixture(directory); Check(ResourceLoader.GetDependencies(path).SequenceEqual([System.IO.Path.Combine(directory, "page.png")]) && ResourceLoader.GetDependencies(binaryPath, true)[0].EndsWith("::ImageTexture", StringComparison.Ordinal), "Bitmap loader dependencies expose real page files and type IDs.");
            using var binary = ResourceLoader.Load<FontFile>(binaryPath, ResourceLoader.CacheMode.Ignore); Check(binary.GetStringSize("AB", fontSize: 8).X == 10 && binary.GetFontName() == "Binary Fixture", "Binary v3 import preserves layout and metadata.");
            using var font = ResourceLoader.Load<FontFile>(path, ResourceLoader.CacheMode.Ignore);
            Check(font.GetFontName() == "Bitmap Fixture" && font.GetFontWeight() == 700 && font.GetStringSize("AB", fontSize: 8).X == 10, "Typed bitmap loading executes metadata and shaping.");
            var packedPath = System.IO.Path.Combine(directory, "packed.fnt");
            File.WriteAllText(packedPath, File.ReadAllText(path).Replace("packed=0", "packed=1").Replace("chnl=15", "chnl=2"));
            using var packed = ResourceLoader.Load<FontFile>(packedPath, ResourceLoader.CacheMode.Ignore); Check(packed.GetStringSize("AB", fontSize: 8).X == 10 && packed.GetTextureCount(0, new(8, 0)) == 4, "Packed channels retain glyph layout and separate pages.");
            using (var page = packed.GetTextureImage(0, new(8, 0), 1)) Check(page!.GetPixel(1, 1).A == 1 && page.GetPixel(1, 1).R == 1, "Packed green coverage converts into monochrome pixels.");
            var invalidBinary = System.IO.Path.Combine(directory, "truncated.font"); File.WriteAllBytes(invalidBinary, File.ReadAllBytes(binaryPath)[..^1]); Reject<InvalidDataException>(() => font.LoadBitmapFont(invalidBinary));
            var stored = System.IO.Path.Combine(directory, "stored.e2dres"); ResourceSaver.Save(font, stored);
            using var loaded = ResourceLoader.Load<FontFile>(stored, ResourceLoader.CacheMode.Ignore);
            Check(loaded.GetStringSize("AB", fontSize: 8).X == 10 && loaded.FixedSize == 8, "Archive restores bitmap state without the descriptor.");
            var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false }; start.ArgumentList.Add(typeof(FontCacheTests).Assembly.Location); start.Environment["ELECTRON2D_TEST_FONT_CACHE_CHILD"] = stored;
            using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync(); if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Fresh bitmap process timed out."); }
            Check(child.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh bitmap cache restored."), "Fresh bitmap process: " + error.GetAwaiter().GetResult());
            File.WriteAllText(path, "info size=8\ncommon lineHeight=8 base=6 pages=1\npage id=0 file=\"missing.png\"\n");
            Reject<IOException>(() => font.LoadBitmapFont(path)); Check(font.GetStringSize("AB", fontSize: 8).X == 10, "Import failure preserves executable old data.");
        }
        finally { Directory.Delete(directory, true); }
    }
    internal static void RunChild(string path)
    {
        using var font = ResourceLoader.Load<FontFile>(path, ResourceLoader.CacheMode.Ignore);
        Check(font.GetStringSize("AB", fontSize: 8).X == 10 && font.GetTextureCount(0, new(8, 0)) == 1, "Fresh process restores executable cache/pages.");
        Console.WriteLine("Fresh bitmap cache restored.");
    }
    internal static void RunHost()
    {
        Run(); var backend = Environment.GetEnvironmentVariable("ELECTRON2D_FONT_CACHE_RENDERER") ?? "gpu";
        var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        try
        {
            using var bitmap = Create(); bitmap.FixedSize = 8; bitmap.FixedSizeScaleMode = FixedSizeScaleMode.Enabled;
            using var native = new FontFile { Data = FontTestFixtures.OpenSans, SubpixelPositioning = FontSubpixelPositioning.Disabled };
            native.RenderRange(0, new(18, 0), 'A', 'Z');
            var window = new Window { Size = new(400, 220) }; var canvas = new CacheCanvas { Bitmap = bitmap, Native = native }; window.AddChild(canvas);
            var label = new Label { Text = "AB", Position = new(180, 40), Size = new(100, 40) }; label.AddThemeFontOverride("font", bitmap); label.AddThemeFontSizeOverride("font_size", 24); window.AddChild(label);
            var rich = new RichTextLabel { Text = "AB", Position = new(180, 95), Size = new(100, 50) }; rich.AddThemeFontOverride("normal_font", bitmap); rich.AddThemeFontSizeOverride("normal_font_size", 24); window.AddChild(rich);
            var frame = 0; long before = 0, total = 0;
            void Pre() => before = GC.GetAllocatedBytesForCurrentThread();
            void Post()
            {
                var after = GC.GetAllocatedBytesForCurrentThread(); if (frame >= 64) total += after - before;
                if (frame == 2)
                {
                    using var image = RenderingServer.Service!.Readback();
                    Check(image.GetPixel(18, 25).G > .8f && image.GetPixel(18, 25).B > .8f, "Native bitmap atlas-region pixels.");
                    if (Environment.GetEnvironmentVariable("ELECTRON2D_FONT_CACHE_CAPTURE") is { } path) image.SavePNG(path);
                }
                canvas.QueueRedraw(); label.QueueRedraw(); rich.QueueRedraw();
                if (frame >= 64) total += GC.GetAllocatedBytesForCurrentThread() - after;
                if (++frame == 128) window.Tree!.Quit();
            }
            window.Ready += _ => { RenderingServer.SetDefaultClearColor(new(.08f, .09f, .12f)); RenderingServer.FramePreDraw += Pre; RenderingServer.FramePostDraw += Post; };
            try { Check(Engine.Run(window) == 0 && frame == 128 && total == 0, "64 prepared bitmap/cache/control render intervals: " + total); }
            finally { if (RenderingServer.IsAvailable) { RenderingServer.FramePreDraw -= Pre; RenderingServer.FramePostDraw -= Post; } }
            Console.WriteLine("64 prepared bitmap/cache/control render intervals: " + total + " managed bytes; backend=" + backend);
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); }
    }
    private sealed class CacheCanvas : Control
    {
        internal required FontFile Bitmap, Native;
        protected override void OnDraw()
        {
            DrawString(Bitmap, new(16, 30), "AB", fontSize: 8);
            DrawString(Bitmap, new(16, 80), "AB", fontSize: 24);
            DrawString(Native, new(16, 165), "PACKED ATLAS", fontSize: 18);
        }
    }
    private sealed class MarkerException : Exception { }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
