using System.Diagnostics;
using System.Runtime.CompilerServices;
using Electron2D;

internal static partial class FontAntialiasingTests
{
    private static FontGlyph Glyph(Font font, int size = 24, int outline = 0, float scale = 1)
    { var data = font.PrimaryData!; return data.GetGlyph(data.GetGlyphIndex('A'), size, outline, scale); }
    private static byte[] Pixels(FontGlyph glyph)
    {
        using var image = glyph.Texture!.GetImage()!; var region = glyph.Region ?? new(0, 0, image.Width, image.Height);
        var bytes = new byte[(int)(region.Size.X * region.Size.Y) * 4]; var at = 0;
        for (var y = (int)region.Position.Y; y < region.End.Y; y++) for (var x = (int)region.Position.X; x < region.End.X; x++)
            { var color = image.GetPixel(x, y); foreach (var value in new[] { color.R, color.G, color.B, color.A }) bytes[at++] = (byte)MathF.Round(value * 255); }
        return bytes;
    }
    internal static void RunManaged()
    {
        Check(ProjectSettings.Get(ProjectSettings.LCDSubpixelLayout) == FontLCDSubpixelLayout.HorizontalRGB && ProjectSettings.IsRestartRequired(ProjectSettings.LCDSubpixelLayout), "Subpixel startup default and restart metadata.");
        Reject<ArgumentOutOfRangeException>(() => ProjectSettings.Set(ProjectSettings.LCDSubpixelLayout, FontLCDSubpixelLayout.Max));
        using var empty = new FontFile(); var changed = 0; empty.Changed += _ => changed++;
        Check(empty.Antialiasing == FontAntialiasing.Gray && empty.GetCacheCount() == 0, "Empty defaults do not realize native faces.");
        empty.Antialiasing = FontAntialiasing.Gray; Check(changed == 0, "Equal antialias assignments are silent.");
        empty.Antialiasing = FontAntialiasing.LCD; Check(changed == 1 && empty.GetCacheCount() == 0, "Deferred preference publishes before native use.");
        using var font = new FontFile { Data = FontTestFixtures.OpenSans, SubpixelPositioning = FontSubpixelPositioning.Disabled, AllowSystemFallback = false };
        var metrics = font.GetStringSize("ffi A"); var old = font.PrimaryData!; var gray = Glyph(font);
        using (font.BeginRead()) { font.Antialiasing = FontAntialiasing.None; using var priorImage = gray.Texture!.GetImage(); Check(!ReferenceEquals(old, font.PrimaryData) && font.GetStringSize("ffi A") == metrics && priorImage is not null, "Raster change preserves shaping metrics and active prior texture snapshot."); }
        var mono = Pixels(Glyph(font)); Check(mono.Where((_, index) => index % 4 == 3).All(a => a is 0 or 255), "None produces bit coverage.");
        font.Antialiasing = FontAntialiasing.LCD; Check(Glyph(font).LCD, "LCD uses native encoded coverage.");
        using var variation = new FontVariation { BaseFont = font }; Check(Glyph(variation).LCD, "Variations inherit coverage policy.");
        font.Antialiasing = FontAntialiasing.Gray; Check(!Glyph(variation).LCD, "Source changes invalidate realized variations.");
        font.Antialiasing = (FontAntialiasing)99; Check(font.Antialiasing == (FontAntialiasing)99 && !Glyph(font).LCD, "Unknown values remain stored and render grayscale.");
        font.Antialiasing = FontAntialiasing.LCD; using var copy = (FontFile)font.Duplicate(); Check(copy.Antialiasing == FontAntialiasing.LCD && Glyph(copy).LCD, "Duplicate carries executable policy.");
        using var system = new SystemFont { Antialiasing = FontAntialiasing.LCD }; Check(system.PrimaryData!.Antialiasing == FontAntialiasing.LCD && Glyph(system).LCD, "System fallback source applies policy.");
        using var systemCopy = (SystemFont)system.Duplicate(); Check(systemCopy.Antialiasing == FontAntialiasing.LCD, "System duplication preserves policy.");
        Storage(copy, systemCopy);
        var captured = FontThread.LCDLayout; ProjectSettings.Set(ProjectSettings.LCDSubpixelLayout, FontLCDSubpixelLayout.VerticalBGR);
        Check(captured == FontLCDSubpixelLayout.HorizontalRGB && FontThread.LCDLayout == captured, "Live writes preserve captured text-owner layout until restart."); ProjectSettings.Set(ProjectSettings.LCDSubpixelLayout, captured);
        using var bitmap = FontSupportTests.Bitmap('A', 6, Colors.Cyan); bitmap.Antialiasing = FontAntialiasing.LCD; Check(!Glyph(bitmap, 8).LCD, "Bitmap-only sources retain their authored ordinary texture contract.");
        using var colorStrike = new FontFile { Data = FontTestFixtures.ColorStrike, Antialiasing = FontAntialiasing.LCD, DisableEmbeddedBitmaps = false };
        var colorGlyph = Glyph(colorStrike, 16); Check(colorGlyph.Colored && !colorGlyph.LCD, "Intrinsic-color strikes preserve their color image role.");
        using var automatic = new FontFile { Data = FontTestFixtures.OpenSans, Antialiasing = FontAntialiasing.LCD };
        if (automatic.PrimaryData!.FindSystemFallback('س', "ar") is { } fallback) Check(fallback.Antialiasing == FontAntialiasing.LCD && fallback.GetGlyph(fallback.GetGlyphIndex('س'), 24, 0, 1).LCD, "Automatic native fallback inherits executable coverage policy.");
        font.Changed += Fail; Reject<Marker>(() => font.Antialiasing = FontAntialiasing.None); font.Changed -= Fail; Check(font.Antialiasing == FontAntialiasing.None, "Observer failure preserves committed coverage.");
        font.ResetState(); system.ResetState(); Check(font.Antialiasing == FontAntialiasing.Gray && system.Antialiasing == FontAntialiasing.Gray, "Reset restores gray policy.");
        empty.Dispose(); Reject<ObjectDisposedException>(() => empty.Antialiasing = FontAntialiasing.None);
        Console.WriteLine("Font AA defaults, publication, active snapshots, metrics, variations, bitmap roles and copies passed.");
    }
    internal static void RunRaster()
    {
        var oracle = Environment.GetEnvironmentVariable("ELECTRON2D_FONT_AA_ORACLE") ?? throw new InvalidOperationException("Specify the independent FreeType oracle.");
        var path = System.IO.Path.GetTempFileName(); File.WriteAllBytes(path, FontTestFixtures.OpenSans); var count = 0;
        try
        {
            using var face = new NativeFontPrecision(FontTestFixtures.OpenSans); var glyph = face.GetGlyphIndex('A');
            LibraryVersion(null, Library(face), out var major, out var minor, out var patch);
            var version = $"{major}.{minor}.{patch}";
            foreach (var aa in new[] { FontAntialiasing.None, FontAntialiasing.Gray, FontAntialiasing.LCD })
                foreach (var layout in new[] { FontLCDSubpixelLayout.None, FontLCDSubpixelLayout.HorizontalRGB, FontLCDSubpixelLayout.HorizontalBGR, FontLCDSubpixelLayout.VerticalRGB, FontLCDSubpixelLayout.VerticalBGR })
                    foreach (var profile in new[] { (16, FontHinting.Normal, 0, 0), (16, FontHinting.Light, 16, 0), (28, FontHinting.None, 0, 0), (16, FontHinting.Light, 32, 64) })
                    {
                        face.SetSize(profile.Item1); var raster = face.Rasterize(glyph, profile.Item2, profile.Item3, profile.Item4, antialiasing: aa, layout: layout);
                        var start = new ProcessStartInfo(oracle) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
                        foreach (var arg in new[] { path, profile.Item1.ToString(), ((int)profile.Item2).ToString(), profile.Item3.ToString(), profile.Item4.ToString(), ((int)aa).ToString(), ((int)layout).ToString() }) start.ArgumentList.Add(arg);
                        start.Environment["LD_PRELOAD"] = System.IO.Path.Combine(AppContext.BaseDirectory, "runtimes", "linux-x64", "native", "libfreetype.so");
                        using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEnd(); var error = process.StandardError.ReadToEnd(); process.WaitForExit();
                        Check(process.ExitCode == 0 && string.IsNullOrWhiteSpace(error), "Raster oracle executes: " + error);
                        var lines = output.Trim().Split('\n'); var header = lines[0].Split(' ');
                        Check(header[0] == version && raster.Width == int.Parse(header[1]) && raster.Height == int.Parse(header[2]) && raster.Left == int.Parse(header[3]) && raster.Top == int.Parse(header[4]) && raster.LCD == (header[5] == "1") && raster.Pixels.AsSpan().SequenceEqual(Convert.FromHexString(lines[1])), $"Exact native raster aa={aa}, layout={layout}, profile={profile}."); count++;
                    }
        }
        finally { File.Delete(path); }
        Check(count == 60, "Complete raster matrix."); Console.WriteLine("60 packaged FreeType oracle cases passed for Mono/Gray/LCD, all layouts, phases, hinting and outlines.");
    }
    private static void Fail(Resource _) => throw new Marker();
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_library")] private static extern ref nint Library(NativeFontPrecision font);
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "FTLibraryVersion")] private static extern void LibraryVersion(NativeFontPrecision? owner, nint library, out int major, out int minor, out int patch);
    private sealed class Marker : Exception { }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
