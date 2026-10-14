using System.Diagnostics;
using Electron2D;

internal static partial class Type1FontTests
{
    private static byte[] Fixture(string extension)
    {
        using var stream = typeof(Type1FontTests).Assembly.GetManifestResourceStream("TestFonts.type1." + extension)!;
        using var bytes = new MemoryStream(); stream.CopyTo(bytes); return bytes.ToArray();
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static FontGlyph Glyph(Font font, int size = 24, int outline = 0) => font.PrimaryData!.GetGlyph(font.PrimaryData.GetGlyphIndex('A'), size, outline, 1);
    private static float PairWidth(Font font) => font.GetStringSize("AV", fontSize: 24).X;
    private static void Write(string directory, string extension) => System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory, "type1." + extension), Fixture(extension));
    internal static void RunManaged()
    {
        Native();
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-type1-" + Guid.NewGuid()); Directory.CreateDirectory(directory);
        try
        {
            foreach (var extension in new[] { "pfb", "pfa", "pfm", "afm" }) Write(directory, extension);
            using var bare = new FontFile { Data = Fixture("pfb"), AllowSystemFallback = false };
            var bareWidth = PairWidth(bare);
            foreach (var extension in new[] { "pfb", "pfa", "pfm", "afm" })
            {
                using var font = new FontFile(); font.LoadDynamicFont(System.IO.Path.Combine(directory, "type1." + extension)); font.AllowSystemFallback = false;
                Check(font.FontName == "Electron2D Type 1 Test" && font.GetFaceCount() == 1 && font.HasChar('A') && !font.HasChar('Ж'), "Source metadata and Unicode cmap.");
                Check(PairWidth(font) < bareWidth && Glyph(font).Texture != null && Glyph(font, 24, 2).Texture != null, "Attached native kerning, fill and stroke.");
                font.OpenTypeFeatureOverrides = new() { ["kern"] = 0 }; Check(PairWidth(font) == bareWidth, "Feature override reaches native callback."); font.OpenTypeFeatureOverrides = [];
                using var copy = (FontFile)font.Duplicate(); using var variation = new FontVariation { BaseFont = font };
                Check(PairWidth(copy) == PairWidth(font) && PairWidth(variation) == PairWidth(font), "Copies and variations retain metrics.");
                font.MultichannelSignedDistanceField = true; font.MSDFSize = 32; font.MSDFPixelRange = 8;
                Check(Glyph(font).MSDFPixelRange == 8 && Glyph(font, 96).Texture == Glyph(font).Texture && Glyph(variation).MSDFPixelRange == 8, "Canonical MSDF and variation invalidation.");
                var path = System.IO.Path.Combine(directory, "font.e2dres"); ResourceSaver.Save(font, path);
                using var restored = ResourceLoader.Load<FontFile>(path, ResourceLoader.CacheMode.Ignore);
                Check(PairWidth(restored) == PairWidth(font) && Glyph(restored).MSDFPixelRange == 8, "Archive source, metrics and generated cache policy.");
                FreshArchive(path);
                var oldData = font.PrimaryData!; var oldGlyph = Glyph(font);
                using (font.BeginRead())
                {
                    font.Data = Fixture("pfa"); Check(PairWidth(font) == bareWidth, "Raw source assignment clears old auxiliary metrics.");
                    Check(oldData.GetKerning(new((int)oldData.GetGlyphIndex('A'), (int)oldData.GetGlyphIndex('V')), 24).X < 0 && oldGlyph.Texture!.GetImage() is { } image && DisposeImage(image), "Active old face and recorded pixels survive replacement.");
                }
                font.ResetState(); Check(font.Data.Length == 0 && font.GetCacheCount() == 0 && !font.MultichannelSignedDistanceField, "Reset releases source pair and preferences.");
            }
            VirtualPaths(bareWidth);
            Catalog(directory);
            FailureAndSidecars(directory, bareWidth);
            Console.WriteLine("Type 1 managed/native source, shaping, raster, MSDF, storage and failure checks passed.");
        }
        finally { Directory.Delete(directory, true); }
    }
    private static void VirtualPaths(float bareWidth)
    {
        foreach (var prefix in new[] { "res://bin/", "user://" })
        {
            var path = prefix + "type1-" + Guid.NewGuid(); var directory = ProjectSettings.GlobalizePath(path); Directory.CreateDirectory(directory);
            try { Write(directory, "pfa"); Write(directory, "afm"); using var font = new FontFile(); font.LoadDynamicFont(path + "/type1.afm"); Check(PairWidth(font) < bareWidth, "Virtual source-pair paths."); }
            finally { Directory.Delete(directory, true); }
        }
    }
    private static bool DisposeImage(Image image) { image.Dispose(); return true; }
    private static void FailureAndSidecars(string directory, float bareWidth)
    {
        using var font = new FontFile(); var path = System.IO.Path.Combine(directory, "type1.pfb"); font.LoadDynamicFont(path);
        var changes = 0; font.Changed += _ => changes++;
        var before = PairWidth(font); var data = font.PrimaryData;
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory, "type1.pfm"), [1, 2, 3]);
        Reject<InvalidDataException>(() => font.LoadDynamicFont(path)); Check(PairWidth(font) == before && font.PrimaryData == data && changes == 0, "Malformed preferred metrics preserve old source and publication.");
        System.IO.File.Delete(System.IO.Path.Combine(directory, "type1.pfm")); font.LoadDynamicFont(path); Check(PairWidth(font) == before && changes == 1, "AFM fallback without PFM.");
        System.IO.File.Delete(System.IO.Path.Combine(directory, "type1.afm")); font.LoadDynamicFont(path); Check(PairWidth(font) == bareWidth, "Outline-only source needs no sidecar.");
        System.IO.File.Move(path, System.IO.Path.Combine(directory, "type1.PFB")); Write(directory, "pfm"); font.LoadDynamicFont(System.IO.Path.Combine(directory, "type1.pfm")); Check(PairWidth(font) == before, "Explicit PFM resolves uppercase PFB.");
        System.IO.File.Delete(System.IO.Path.Combine(directory, "type1.PFB")); font.LoadDynamicFont(System.IO.Path.Combine(directory, "type1.pfm")); Check(PairWidth(font) == before, "Explicit PFM resolves PFA without PFB.");
        System.IO.File.Delete(System.IO.Path.Combine(directory, "type1.pfa")); Reject<InvalidDataException>(() => font.LoadDynamicFont(System.IO.Path.Combine(directory, "type1.pfm")));
        Reject<ArgumentNullException>(() => font.LoadDynamicFont(null!));
        Reject<InvalidDataException>(() => font.Data = [1, 2, 3]); Check(PairWidth(font) == before, "Orphan metrics and corrupt outline preserve old source.");
        Write(directory, "pfb"); using (var stream = new FileStream(System.IO.Path.Combine(directory, "type1.pfm"), FileMode.Create)) stream.SetLength(64 * 1024 * 1024 + 1L);
        Reject<InvalidDataException>(() => font.LoadDynamicFont(path)); Check(PairWidth(font) == before, "Oversized metrics preserve old source.");
        using (var stream = new FileStream(System.IO.Path.Combine(directory, "type1.pfm"), FileMode.Create)) stream.SetLength(64 * 1024 * 1024L);
        Reject<InvalidDataException>(() => font.LoadDynamicFont(path)); Check(PairWidth(font) == before, "Combined outline/metrics byte budget is enforced.");
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory, "type1.pfm"), []); Reject<InvalidDataException>(() => font.LoadDynamicFont(path));
        Write(directory, "pfm"); var raw = Fixture("pfb"); font.Data = raw; raw.AsSpan().Clear(); Check(font.HasChar('A') && PairWidth(font) == bareWidth, "Data assignment owns immutable outline bytes.");
        font.Changed += Fail; Reject<Marker>(() => font.LoadDynamicFont(path)); font.Changed -= Fail; Check(PairWidth(font) == before, "Observer failure retains committed source pair.");
        font.Antialiasing = FontAntialiasing.None; Check(Glyph(font).Texture != null && PairWidth(font) == before, "Policy/cache rebuild retains metrics.");
        font.SetGlyphAdvance(0, 24, (int)font.PrimaryData!.GetGlyphIndex('A'), new(30, 0)); Check(PairWidth(font) > before, "Authored cache rebuild retains native source pair.");
    }
    private sealed class Marker : Exception { }
    private static void Fail(Resource resource) => throw new Marker();
    private static void FreshArchive(string path)
    {
        var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        start.ArgumentList.Add(typeof(Type1FontTests).Assembly.Location); start.ArgumentList.Add("--font-type1-archive"); start.ArgumentList.Add(path);
        start.Environment.Remove("ELECTRON2D_TEST_TYPE1"); using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("Type 1 archive child timed out."); }
        Check(process.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh Type 1 archive passed."), "Fresh archive: " + error.GetAwaiter().GetResult());
    }
    internal static void RunArchive(string path)
    {
        using var font = ResourceLoader.Load<FontFile>(path, ResourceLoader.CacheMode.Ignore); using var bare = new FontFile { Data = Fixture("pfb") };
        Check(PairWidth(font) < PairWidth(bare) && Glyph(font).MSDFPixelRange == 8, "Fresh native face receives saved metrics."); Console.WriteLine("Fresh Type 1 archive passed.");
    }
    private static void Native()
    {
        foreach (var extension in new[] { "pfb", "pfa" })
        {
            using var bare = new NativeFontPrecision(Fixture(extension)); bare.SetSize(24);
            foreach (var metrics in new[] { "pfm", "afm" })
            {
                using var face = new NativeFontPrecision(Fixture(extension), auxiliaryMetrics: Fixture(metrics)); face.SetSize(24);
                var a = face.GetGlyphIndex('A'); var v = face.GetGlyphIndex('V'); Check(a != 0 && v != 0 && face.GetGlyphIndex('Ж') == 0 && face.GetGlyphIndex('A', 0xFE0F) == 0, "Native nominal/variation mappings.");
                var scalars = new uint[] { 'A', 'V' }; var plain = bare.Shape(scalars, NativeTextDirection.LTR).ToArray(); var shaped = face.Shape(scalars, NativeTextDirection.LTR).ToArray();
                Check(shaped.Length == 2 && shaped[0].GlyphIndex == a && shaped[1].GlyphIndex == v && plain.Sum(g => g.XAdvance) - shaped.Sum(g => g.XAdvance) == 184, "HarfBuzz applies callback kerning.");
                var noKerning = face.Shape(scalars, NativeTextDirection.LTR, features: [new(0x6b65726e, 0)]);
                Check(noKerning.SequenceEqual(plain), "Native feature disables auxiliary kerning.");
                var run = face.Shape(new uint[] { 'O', 'A', 'V', 'g' }, 1, 2, NativeTextDirection.LTR); Check(run[0].Cluster == 1 && run[1].Cluster == 2, "Full-context subrange clusters.");
                var rtl = face.Shape(scalars, NativeTextDirection.RTL); Check(rtl[0].GlyphIndex == v && rtl[1].GlyphIndex == a, "RTL follows shared shaper.");
                foreach (var hint in new[] { FontHinting.None, FontHinting.Light, FontHinting.Normal })
                    foreach (var mode in new[] { FontAntialiasing.None, FontAntialiasing.Gray, FontAntialiasing.LCD })
                    {
                        var raster = face.Rasterize(a, hint, antialiasing: mode); Check(raster.Width > 0 && raster.Height > 0 && raster.Pixels.Length == raster.Width * raster.Height * 4, "Native outline raster mode.");
                    }
            }
        }
    }
}
