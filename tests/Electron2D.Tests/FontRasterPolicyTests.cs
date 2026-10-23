using System.Diagnostics;
using Electron2D;

internal static partial class FontRasterPolicyTests
{
    private static FontGlyph Glyph(Font font, int size = 16, int outline = 0, float oversampling = 1)
    { var data = font.PrimaryData!; return data.GetGlyph(data.GetGlyphIndex('A'), size, outline, oversampling); }
    private static byte[] Alpha(FontGlyph glyph)
    {
        using var image = glyph.Texture!.GetImage()!; var region = glyph.Region ?? new Rect2(0, 0, image.Width, image.Height);
        var result = new byte[(int)(region.Size.X * region.Size.Y)]; var at = 0;
        for (var y = (int)region.Position.Y; y < region.End.Y; y++) for (var x = (int)region.Position.X; x < region.End.X; x++) result[at++] = (byte)MathF.Round(image.GetPixel(x, y).A * 255);
        return result;
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    internal static void Run()
    {
        using var empty = new FontFile(); var notifications = 0; empty.Changed += _ => notifications++;
        Check(empty.DisableEmbeddedBitmaps && !empty.ForceAutohinter && !empty.GenerateMIPMaps && empty.GetCacheCount() == 0, "Raster defaults do not initialize native caches.");
        empty.DisableEmbeddedBitmaps = true; empty.ForceAutohinter = false; empty.GenerateMIPMaps = false; Check(notifications == 0, "Equal policies are silent.");
        using var font = new FontFile { Data = FontTestFixtures.Strike, Hinting = FontHinting.Normal, SubpixelPositioning = FontSubpixelPositioning.Disabled, AllowSystemFallback = false };
        var outline = Glyph(font); var priorData = font.PrimaryData!; var advance = font.GetStringSize("A").X;
        using (font.BeginRead())
        {
            font.DisableEmbeddedBitmaps = false; var bitmap = Glyph(font);
            Check(bitmap.Size == new Vector2(5, 7) && outline.Size != bitmap.Size, "Embedded strike selection changes real glyph pixels.");
            var expected = new List<byte>(); foreach (var row in new byte[] { 0x70, 0x88, 0x88, 0xF8, 0x88, 0x88, 0x88 }) for (var x = 0; x < 5; x++) expected.Add((byte)((row & (0x80 >> x)) != 0 ? 255 : 0));
            Check(Alpha(bitmap).SequenceEqual(expected), "Monochrome strike alpha matches independently authored bits.");
            Check(priorData.DisableEmbeddedBitmaps && priorData.GetGlyph(priorData.GetGlyphIndex('A'), 16, 0, 1).Size == outline.Size, "Active old data retains its old policy.");
            Check(Glyph(font, outline: 1).Size.X > bitmap.Size.X, "Stroke still chooses scalable outlines.");
        }
        font.DisableEmbeddedBitmaps = true; Check(Glyph(font).Size == outline.Size && font.GetStringSize("A").X == advance, "Native policy changes preserve unhinted shaping.");
        using var variation = new FontVariation { BaseFont = font }; font.DisableEmbeddedBitmaps = false; Check(Glyph(variation).Size == new Vector2(5, 7), "Variation inherits native strike policy.");
        using (var color = new FontFile { Data = FontTestFixtures.ColorStrike, DisableEmbeddedBitmaps = false }) { Check(Glyph(color).Colored && Glyph(color).Size == new Vector2(5, 7), "Native color bitmap strike is selected."); color.DisableEmbeddedBitmaps = true; Check(Glyph(color).Colored && Glyph(color).Size == new Vector2(5, 7), "Bitmap bypass preserves actual color strikes."); }
        font.GenerateMIPMaps = true; using (var image = Glyph(font).Texture!.GetImage()) Check(image!.HasMIPMaps && image.MIPMapCount > 0, "Indexed/native glyph pages publish complete mip chains.");
        using (var image = Glyph(font, oversampling: 2).Texture!.GetImage()) Check(image!.HasMIPMaps, "Standalone oversampled rasters publish mip chains.");
        font.GenerateMIPMaps = false; using (var image = Glyph(font).Texture!.GetImage()) Check(!image!.HasMIPMaps, "Disabling mipmaps invalidates prior textures.");
        using var manual = FontSupportTests.Bitmap('A', 6, Colors.Cyan); manual.GenerateMIPMaps = true; manual.ForceAutohinter = true; manual.DisableEmbeddedBitmaps = false;
        Check(manual.GetStringSize("A", fontSize: 8).X == 6, "Native policy edits preserve authored bitmap records.");
        using (var image = Glyph(manual, 8).Texture!.GetImage()) Check(image!.HasMIPMaps, "Authored bitmap textures execute mipmap policy.");
        using var latin = new FontFile { Data = FontTestFixtures.OpenSans, Hinting = FontHinting.Normal, SubpixelPositioning = FontSubpixelPositioning.Disabled };
        var original = Alpha(Glyph(latin, 15)); latin.ForceAutohinter = true; var automatic = Alpha(Glyph(latin, 15)); Check(!original.SequenceEqual(automatic), "Forced automatic hinting changes real raster coverage.");
        VerifyOracle(latin, 15, true); latin.ForceAutohinter = false; VerifyOracle(latin, 15, false); VerifyOracle(font, 16, false);
        latin.ForceAutohinter = true; latin.GenerateMIPMaps = true; latin.DisableEmbeddedBitmaps = false;
        using var duplicate = (FontFile)latin.Duplicate(); Check(!duplicate.DisableEmbeddedBitmaps && duplicate.ForceAutohinter && duplicate.GenerateMIPMaps, "Duplicate keeps source policies.");
        duplicate.GenerateMIPMaps = false; Check(latin.GenerateMIPMaps, "Duplicate settings are independent.");
        using var system = new SystemFont { GenerateMIPMaps = true, ForceAutohinter = true, DisableEmbeddedBitmaps = false };
        Check(system.PrimaryData!.GenerateMIPMaps && system.PrimaryData.ForceAutohinter && !system.PrimaryData.DisableEmbeddedBitmaps, "Unresolved SystemFont applies settings to its realized theme source.");
        using var systemCopy = (SystemFont)system.Duplicate(); Check(systemCopy.GenerateMIPMaps && systemCopy.ForceAutohinter && !systemCopy.DisableEmbeddedBitmaps, "SystemFont duplication retains preferences.");
        using var systemVariation = new FontVariation { BaseFont = system }; Check(systemVariation.PrimaryData!.GenerateMIPMaps, "SystemFont variation carries mipmaps.");
        system.GenerateMIPMaps = false; Check(!systemVariation.PrimaryData!.GenerateMIPMaps, "Realized system variants follow source policy changes.");
        var families = OS.GetSystemFonts();
        if (families.Length > 0)
        {
            using var installed = new SystemFont { FontNames = [families[0]], GenerateMIPMaps = true, ForceAutohinter = true, DisableEmbeddedBitmaps = false };
            using var installedPixels = Glyph(installed).Texture!.GetImage();
            Check(installed.GetFaceCount() > 0 && installedPixels!.HasMIPMaps && installed.PrimaryData!.ForceAutohinter && !installed.PrimaryData.DisableEmbeddedBitmaps, "Installed matches execute native raster preferences.");
            using var fallback = new FontFile { Data = FontTestFixtures.OpenSans, GenerateMIPMaps = true, ForceAutohinter = true, DisableEmbeddedBitmaps = false };
            if (fallback.PrimaryData!.FindSystemFallback('س', "ar") is { } automaticSource)
            {
                var automaticGlyph = automaticSource.GetGlyph(automaticSource.GetGlyphIndex('س'), 16, 0, 1); using var automaticPixels = automaticGlyph.Texture!.GetImage();
                Check(automaticPixels!.HasMIPMaps && automaticSource.ForceAutohinter && !automaticSource.DisableEmbeddedBitmaps, "Owned automatic sources inherit actual raster/MIP policy.");
            }
        }
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-raster-policy-" + Guid.NewGuid()); Directory.CreateDirectory(directory);
        try
        {
            var path = System.IO.Path.Combine(directory, "font.e2dres"); ResourceSaver.Save(latin, path); using var loaded = ResourceLoader.Load<FontFile>(path, ResourceLoader.CacheMode.Ignore); Check(loaded.GenerateMIPMaps && loaded.ForceAutohinter && !loaded.DisableEmbeddedBitmaps, "Archive restores source policy.");
            var systemPath = System.IO.Path.Combine(directory, "system.e2dres"); ResourceSaver.Save(systemCopy, systemPath); using var loadedSystem = ResourceLoader.Load<SystemFont>(systemPath, ResourceLoader.CacheMode.Ignore); Check(loadedSystem.GenerateMIPMaps && loadedSystem.ForceAutohinter && !loadedSystem.DisableEmbeddedBitmaps, "System archive stores preferences rather than native identity.");
            var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false }; if (System.IO.Path.GetFileNameWithoutExtension(start.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(typeof(FontRasterPolicyTests).Assembly.Location); start.Environment["ELECTRON2D_TEST_FONT_RASTER_CHILD"] = path;
            using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync(); if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException(); }
            Check(child.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh raster policy restored."), "Fresh native archive: " + error.GetAwaiter().GetResult());
            latin.LoadBitmapFont(FontCacheTests.Fixture(directory)); Check(latin.DisableEmbeddedBitmaps && !latin.ForceAutohinter && !latin.GenerateMIPMaps, "Bitmap loading resets raster defaults.");
            var dynamicPath = System.IO.Path.Combine(directory, "native.ttf"); File.WriteAllBytes(dynamicPath, FontTestFixtures.Strike); font.LoadDynamicFont(dynamicPath); Check(font.DisableEmbeddedBitmaps && !font.ForceAutohinter && !font.GenerateMIPMaps, "Dynamic file loading resets raster defaults.");
        }
        finally { Directory.Delete(directory, true); }
        using var concurrently = new FontFile { Data = FontTestFixtures.Strike };
        Parallel.Invoke(() => concurrently.DisableEmbeddedBitmaps = false, () => concurrently.ForceAutohinter = true, () => concurrently.GenerateMIPMaps = true);
        Check(!concurrently.DisableEmbeddedBitmaps && concurrently.ForceAutohinter && concurrently.GenerateMIPMaps, "Concurrent policy publication retains all fields.");
        concurrently.Changed += ThrowObserver; Reject<MarkerException>(() => concurrently.GenerateMIPMaps = false); concurrently.Changed -= ThrowObserver; Check(!concurrently.GenerateMIPMaps, "Observer failure leaves committed raster policy.");
        VerifyMixedPages(); VerifyLegacyCache();
        using var noise = new NoiseTexture(); using var group = new CanvasGroup();
        Check(noise.GetPropertyList().Any(value => value.Name == "GenerateMipmaps") && group.GetPropertyList().Any(value => value.Name == "UseMipmaps"), "Established persisted MIP keys retain their spelling.");
        empty.Dispose(); Reject<ObjectDisposedException>(() => empty.GenerateMIPMaps = true);
        Warm(latin); Console.WriteLine("Font native strike/autohinter/mipmap policies passed.");
    }
    internal static void RunChild(string path)
    {
        using var font = ResourceLoader.Load<FontFile>(path, ResourceLoader.CacheMode.Ignore); using var image = Glyph(font).Texture!.GetImage(); Check(image!.HasMIPMaps && font.ForceAutohinter && !font.DisableEmbeddedBitmaps, "Fresh policy executes native mipmap generation."); Console.WriteLine("Fresh raster policy restored.");
    }
    private static void ThrowObserver(Resource _) => throw new MarkerException();
    private sealed class MarkerException : Exception { }
    private static void VerifyMixedPages()
    {
        using var font = new FontFile { Data = FontTestFixtures.Strike, SubpixelPositioning = FontSubpixelPositioning.Disabled };
        _ = Glyph(font); using var page = font.GetTextureImage(0, new(16, 0), 0)!;
        for (var y = 100; y < 104; y++) for (var x = 100; x < 104; x++) page.SetPixel(x, y, Colors.Magenta);
        font.SetTextureImage(0, new(16, 0), 0, page); var space = (int)font.PrimaryData!.GetGlyphIndex(' ');
        font.SetGlyphSize(0, new(16, 0), space, new(4, 4)); font.SetGlyphUVRect(0, new(16, 0), space, new(100, 100, 4, 4)); font.SetGlyphTextureIndex(0, new(16, 0), space, 0);
        for (var i = 0; i < 24; i++) { font.DisableEmbeddedBitmaps = (i & 1) != 0; var data = font.PrimaryData!; _ = data.GetGlyph(data.GetGlyphIndex('B'), 16, 0, 1); }
        Check(font.GetTextureCount(0, new(16, 0)) <= 2 && Alpha(font.PrimaryData!.GetGlyph((uint)space, 16, 0, 1)).All(value => value == 255), "Policy toggles retain authored atlas content and bounded native page slots.");
    }
    private static void VerifyLegacyCache()
    {
        using var font = new FontFile(); using var image = Image.CreateEmpty(5, 6, false, Image.Format.Rgba8); image.Fill(Colors.Cyan); font.SetTextureImage(0, new(8, 0), 0, image);
        var descriptor = (PropertyDescriptor<FontFile, byte[]>)font.GetPropertyList().Single(value => value.Name == "_font_cache"); var current = descriptor.GetValue(font);
        var legacy = current[..^2]; System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(legacy, 1);
        descriptor.SetValue(font, legacy); Check(font.GetTextureCount(0, new(8, 0)) == 1, "Version-one cache storage remains readable.");
        var invalid = (byte[])current.Clone(); invalid[^1] = 2; Reject<InvalidDataException>(() => descriptor.SetValue(font, invalid)); Check(font.GetTextureCount(0, new(8, 0)) == 1, "Invalid page ownership rejects before publication.");
    }
    private static void Warm(FontFile font)
    {
        _ = Glyph(font, 8); _ = font.GetStringSize("A", fontSize: 8); var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100; i++) { _ = Glyph(font, 8); _ = font.GetStringSize("A", fontSize: 8); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared raster-policy reads allocate zero managed bytes.");
    }
    private static void VerifyOracle(FontFile font, int size, bool force)
    {
        if (Environment.GetEnvironmentVariable("ELECTRON2D_FONT_POLICY_ORACLE") is not { } oracle || !OperatingSystem.IsLinux()) return;
        var path = System.IO.Path.GetTempFileName(); File.WriteAllBytes(path, font.Data);
        try
        {
            var start = new ProcessStartInfo(oracle) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            start.ArgumentList.Add(path); start.ArgumentList.Add(size.ToString()); start.ArgumentList.Add(font.DisableEmbeddedBitmaps ? "1" : "0"); start.ArgumentList.Add(force ? "1" : "0");
            start.Environment["LD_PRELOAD"] = System.IO.Path.Combine(AppContext.BaseDirectory, "runtimes", "linux-x64", "native", "libfreetype.so");
            using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEnd(); var error = child.StandardError.ReadToEnd(); child.WaitForExit(); Check(child.ExitCode == 0 && string.IsNullOrWhiteSpace(error), "Independent FreeType oracle: " + error);
            var lines = output.Trim().Split('\n'); var header = lines[0].Split(' '); var glyph = Glyph(font, size); Check(glyph.Size == new Vector2(int.Parse(header[0]), int.Parse(header[1])) && Alpha(glyph).SequenceEqual(Convert.FromHexString(lines[1])), "Native pixels match independent public FreeType oracle.");
        }
        finally { File.Delete(path); }
    }
}
