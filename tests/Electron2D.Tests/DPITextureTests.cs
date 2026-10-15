using Electron2D;
using Path = System.IO.Path;

internal static class DPITextureTests
{
    internal const string Source = """<svg xmlns="http://www.w3.org/2000/svg" width="16" height="8"><rect width="8" height="8" fill="red"/><rect x="8" width="8" height="8" fill="blue"/></svg>""";
    private static int _checks;
    internal static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); _checks++; }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { _checks++; return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    internal static void Pixel(Image image, int x, int y, Color expected)
    {
        var p = image.GetPixel(x, y); Check(MathF.Abs(p.R - expected.R) < .025f && MathF.Abs(p.G - expected.G) < .025f && MathF.Abs(p.B - expected.B) < .025f && MathF.Abs(p.A - expected.A) < .025f, $"Pixel {x},{y}: {p}, expected {expected}.");
    }
    internal static void Run()
    {
        using var empty = new DPITexture(); Check(empty.GetSize() == Vector2.Zero && empty.GetImage() is null && empty.HasAlpha && !empty.ResourceLocalToScene, "Empty texture/default locality.");
        Check(empty.GetScaledRID() == empty.GetRID() && empty.GetRID().IsValid(), "Base identity exists without native rasterization.");
        using var dpi = DPITexture.CreateFromString(Source); var changes = 0; dpi.Changed += _ => changes++;
        var map = new Dictionary<Color, Color> { [Colors.Red] = Colors.Lime, [Colors.Blue] = Colors.Yellow };
        dpi.ColorMap = map; map.Clear(); var copyMap = dpi.ColorMap; copyMap.Clear(); Check(dpi.ColorMap.Count == 2 && changes == 1, "Map input and output are copied.");
        dpi.ColorMap = dpi.ColorMap; dpi.SetSource(Source); dpi.BaseScale = 1; Check(changes == 1, "Equal settings stay silent.");
        using (var image = dpi.GetImage()!) { Pixel(image, 2, 2, Colors.Lime); Pixel(image, 12, 2, Colors.Yellow); }
        dpi.BaseScale = 2; Check(dpi.GetSize() == new Vector2(32, 16), "Base scale affects logical dimensions.");
        using (var image = dpi.GetImage()!) Check(image.Width == 32 && image.Height == 16, "Native raster dimensions follow base scale.");
        dpi.SetSizeOverride(new(7, 0)); Check(dpi.GetSize() == new Vector2(7, 16), "One overridden axis.");
        dpi.SetSizeOverride(new(0, 5)); Check(dpi.GetSize() == new Vector2(32, 5), "Zero restores an intrinsic axis.");
        dpi.SetSizeOverride(default); Check(dpi.GetSize() == new Vector2(32, 16), "Both intrinsic axes restored.");
        using (var clone = (DPITexture)dpi.Duplicate(true))
        {
            Check(clone.GetSource() == Source && clone.ColorMap.Count == 2 && clone.BaseScale == 2 && clone.GetRID() != dpi.GetRID(), "Independent resource copy and identity.");
            dpi.ColorMap = []; using var image = clone.GetImage()!; Pixel(image, 2, 2, Colors.Lime);
        }
        var path = Path.Combine(Path.GetTempPath(), "electron2d-dpi-" + Guid.NewGuid().ToString("N") + ".e2dres");
        try
        {
            dpi.ColorMap = new() { [Colors.Red] = Colors.Lime }; ResourceSaver.Save(dpi, path);
            using var restored = ResourceLoader.Load<DPITexture>(path)!; Check(restored.GetSource() == Source && restored.BaseScale == 2 && restored.ColorMap.Count == 1, "Typed resource archive restores source/settings/map.");
            using var image = restored.GetImage()!; Pixel(image, 2, 2, Colors.Lime);
        }
        finally { File.Delete(path); }
        using (var paints = DPITexture.CreateFromString("""<svg xmlns="http://www.w3.org/2000/svg" width="24" height="8"><defs><linearGradient id="g"><stop stop-color="red"/><stop offset="1" stop-color="red"/></linearGradient></defs><rect width="8" height="8" fill="url(#g)"/><path d="M12 0 V8" stroke="red" stroke-width="4"/><rect x="16" width="8" height="8" fill="#f00" fill-opacity="0.5"/></svg>""", colorMap: new() { [Colors.Red] = new(0, 1, 0, .1f) }))
        {
            using var image = paints.GetImage()!; Pixel(image, 2, 2, Colors.Lime); Pixel(image, 12, 2, Colors.Lime); Pixel(image, 20, 2, new(0, 1, 0, .5f));
            paints.PremultAlpha = true; using var premult = paints.GetImage()!; Pixel(premult, 20, 2, new(0, .5f, 0, .5f));
            paints.Saturation = 0; using var gray = paints.GetImage()!; var p = gray.GetPixel(2, 2); Check(MathF.Abs(p.R - p.G) < .01f && MathF.Abs(p.G - p.B) < .01f, "Saturation is applied to native pixels.");
        }
        using (var edge = DPITexture.CreateFromString("""<svg xmlns="http://www.w3.org/2000/svg" width="10" height="10"><rect x="3" y="3" width="4" height="4" fill="red"/></svg>"""))
        {
            edge.FixAlphaBorder = true; using var image = edge.GetImage()!;
            var pixel = image.GetPixel(1, 5); Check(pixel.R > .9f && pixel.G < .01f && pixel.A == 0, "Alpha-edge processing fills transparent RGB without changing alpha.");
        }
        using (var notifying = DPITexture.CreateFromString(Source))
        {
            var failure = new InvalidOperationException("DPI observer"); notifying.Changed += _ => throw failure;
            Exception? seen = null; try { notifying.Saturation = .5f; } catch (Exception error) { seen = error; }
            Check(ReferenceEquals(seen, failure) && notifying.Saturation == .5f, "Observer failure propagates after setting commitment.");
        }
        Reject<InvalidDataException>(() => dpi.BaseScale = float.MaxValue);
        Reject<ArgumentNullException>(() => dpi.ColorMap = null!);
        var source = dpi.GetSource(); Reject<InvalidDataException>(() => dpi.SetSource("<svg>")); Check(dpi.GetSource() == source, "Failed source validation preserves state.");
        Reject<InvalidDataException>(() => dpi.SetSource("<!DOCTYPE svg><svg width=\"1\" height=\"1\"/>"));
        Reject<InvalidDataException>(() => dpi.SetSource("\ud800")); Reject<ArgumentNullException>(() => dpi.SetSource(null!));
        Reject<ArgumentOutOfRangeException>(() => dpi.BaseScale = 0); Reject<ArgumentOutOfRangeException>(() => dpi.BaseScale = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => dpi.Saturation = float.PositiveInfinity); Reject<ArgumentOutOfRangeException>(() => dpi.SetSizeOverride(new(-1, 0)));
        Reject<ArgumentException>(() => dpi.ColorMap = new() { [new(float.NaN, 0, 0)] = Colors.Red });
        using var view = new SubViewport { Size = new(40, 20), Size2DOverride = new(10, 10), Size2DOverrideStretch = true };
        Check(view.GetOversampling() == 4, "Automatic oversampling uses the maximum stretch axis.");
        view.OversamplingOverride = 2.5f; Check(view.GetOversampling() == 2.5f, "Positive explicit oversampling.");
        view.Oversampling = false; Check(view.GetOversampling() == 1, "Disabled oversampling overrides the explicit value.");
        view.Oversampling = true; view.OversamplingOverride = -1; Check(view.GetOversampling() == 4, "Nonpositive override selects automatic scaling.");
        Reject<ArgumentOutOfRangeException>(() => view.OversamplingOverride = float.NaN);
        dpi.Dispose(); Reject<ObjectDisposedException>(() => dpi.GetImage()); Reject<ObjectDisposedException>(() => dpi.SetSource(Source));
        Console.WriteLine($"DPI texture source, paint/gradient/stroke/opacity processing, dimensions, guards, copy/archive and viewport policy: {_checks} assertions passed.");
    }
}
