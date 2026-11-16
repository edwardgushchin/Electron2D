using Electron2D;
using F = Electron2D.Image.Format;

var checks = 0;
void Check(bool value, string label) { checks++; if (!value) throw new Exception(label); }
void Near(double actual, double expected, string label) => Check(Math.Abs(actual - expected) < 1e-6, $"{label}: {actual} != {expected}");
void Reject<T>(Action action) where T : Exception
{
    try { action(); } catch (T) { checks++; return; }
    throw new Exception($"Expected {typeof(T).Name}");
}
foreach (var format in Enum.GetValues<F>().Where(f => f < F.Max))
{
    using var image = Image.CreateEmpty(4, 4, true, format);
    var before = image.GetData(); var changes = 0; image.Changed += _ => changes++;
    if (image.IsCompressed)
    {
        Reject<InvalidOperationException>(() => image.AdjustBCS(1, 1, 1));
        Check(before.SequenceEqual(image.GetData()) && changes == 0, $"Compressed BCS refusal {format}");
        continue;
    }
    foreach (var luma in new[] { false, true })
    {
        if (format is >= F.Rh and <= F.Rgbe9995)
            Reject<InvalidOperationException>(() => image.ComputeImageMetrics(image, luma));
        else
            Check(image.ComputeImageMetrics(image, luma).PeakSignalToNoiseRatio == 1e10, $"Format metrics {format}");
    }
    // Constant zero makes every authored mip independently observable after brightness/contrast.
    image.AdjustBCS(0, 0, 1);
    Check(changes == 1 && image.HasMIPMaps && image.PixelFormat == format, $"Publication {format}");
    using var expected = Image.CreateEmpty(4, 4, true, format);
    expected.Fill(new Color(.5f, .5f, .5f, image.GetPixel(0, 0).A));
    Check(expected.GetData().SequenceEqual(image.GetData()), $"All stored levels {format}");
    Check(before.All(x => x == 0), $"Export independence {format}");
}
using (var image = Image.CreateEmpty(1, 1, false, F.Rgbaf))
{
    image.SetPixel(0, 0, new Color(.25f, .5f, 1f, .125f));
    image.AdjustBCS(2, .5f, 0);
    var color = image.GetPixel(0, 0);
    Near(color.R, 5d / 6, "BCS order"); Near(color.G, color.R, "Gray G"); Near(color.B, color.R, "Gray B"); Near(color.A, .125, "Alpha");
    var changes = 0; image.Changed += _ => changes++;
    foreach (var invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
    {
        var before = image.GetData();
        Reject<ArgumentOutOfRangeException>(() => image.AdjustBCS(invalid, 1, 1));
        Reject<ArgumentOutOfRangeException>(() => image.AdjustBCS(1, invalid, 1));
        Reject<ArgumentOutOfRangeException>(() => image.AdjustBCS(1, 1, invalid));
        Check(before.SequenceEqual(image.GetData()) && changes == 0, "Atomic factor rejection");
    }
    // Unsubscribe before reentry so the callback has a finite depth.
    Action<Resource>? reentry = null;
    reentry = _ => { image.Changed -= reentry; image.AdjustBCS(1, 1, 1); };
    image.Changed += reentry; image.AdjustBCS(0, 0, 1);
    Check(changes == 2, "Reentrant mutation");
    Action<Resource> failure = _ => throw new IOException("observer");
    image.Changed += failure;
    Reject<IOException>(() => image.AdjustBCS(0, 1, 1));
    Near(image.GetPixel(0, 0).R, 0, "Observer failure keeps committed state"); image.Changed -= failure;
}
using (var a = Image.CreateEmpty(2, 1, true, F.Rgba8))
using (var b = Image.CreateEmpty(3, 2, false, F.Rgba8))
{
    a.Fill(Colors.Black); b.Fill(Colors.Black); b.SetPixel(0, 0, Colors.White);
    var source = a.GetData(); var target = b.GetData(); var events = 0; a.Changed += _ => events++; b.Changed += _ => events++;
    foreach (var luma in new[] { false, true })
    {
        var m = a.ComputeImageMetrics(b, luma);
        Near(m.Maximum, 255, "Maximum"); Near(m.Mean, luma ? 31.875 : 95.625, "Mean");
        Near(m.MeanSquared, luma ? 8128.125 : 24384.375, "Squared");
        Near(m.RootMeanSquared, Math.Sqrt(luma ? 8128.125 : 24384.375), "RMS");
        Near(m.PeakSignalToNoiseRatio, luma ? 9.030899869919436 : 4.259687322722812, "PSNR");
        Check(a.ComputeImageMetrics(a, luma).PeakSignalToNoiseRatio == 1e10, "Identical sentinel");
        Check(m == b.ComputeImageMetrics(a, luma), "Symmetry with unequal dimensions");
    }
    Check(source.SequenceEqual(a.GetData()) && target.SequenceEqual(b.GetData()) && events == 0, "Queries preserve sources/mips/events");
    Reject<ArgumentNullException>(() => a.ComputeImageMetrics(null!, false));
}
using (var a = Image.CreateEmpty(1, 1, false, F.Rgbaf))
using (var b = Image.CreateEmpty(1, 1, false, F.Rgbaf))
{
    foreach (var value in new[] { -1f, 2f, float.NaN, float.NegativeInfinity, float.PositiveInfinity })
        foreach (var luma in new[] { false, true })
        {
            a.SetPixel(0, 0, new Color(0, value, 0, 1));
            Reject<InvalidOperationException>(() => a.ComputeImageMetrics(b, luma));
        }
}
using (var a = Image.CreateEmpty(1, 1, false, F.Rgbaf))
using (var b = Image.CreateEmpty(1, 1, false, F.Rgbaf))
{
    foreach (var (value, expected) in new[] { (.5f, 128d), (.3f, 77d) })
    {
        a.SetPixel(0, 0, new Color(value, value, value, value));
        foreach (var luma in new[] { false, true })
        {
            var metric = a.ComputeImageMetrics(b, luma);
            Near(metric.Maximum, expected, "Round half-byte away from zero");
            Near(metric.Mean, luma ? expected / 4 : expected, "Rounded mean");
        }
    }
}
using (var a = Image.CreateEmpty(1, 1, false, F.Rgba8))
using (var b = Image.CreateEmpty(2, 1, false, F.Rgbaf))
{
    a.Fill(new Color(1, 0, 0, 0)); b.Fill(new Color(1, 0, 0, 1));
    b.SetPixel(1, 0, new Color(float.NaN, 0, 0, 1));
    Near(a.ComputeImageMetrics(b, false).Mean, 63.75, "Alpha-only error across formats");
    Near(a.ComputeImageMetrics(b, true).Maximum, 0, "Luma ignores valid alpha and out-of-area NaN");
    b.SetPixel(0, 0, new Color(1, 0, 0, float.NaN));
    Reject<InvalidOperationException>(() => a.ComputeImageMetrics(b, true));
}
using (var hdr = Image.CreateEmpty(1, 1, false, F.Rgbaf))
{
    hdr.SetPixel(0, 0, new Color(4, -2, 0, .25f)); hdr.AdjustBCS(1, 1, 1);
    Check(hdr.GetPixel(0, 0) == new Color(4, -2, 0, .25f), "HDR extrapolation without clipping");
    hdr.SetPixel(0, 0, new Color(float.PositiveInfinity, 0, 0, .25f)); hdr.AdjustBCS(1, 1, 1);
    Check(float.IsNaN(hdr.GetPixel(0, 0).R) && hdr.GetPixel(0, 0).A == .25f, "IEEE nonfinite propagation preserves alpha");
}
using (var empty = new Image())
{
    Reject<InvalidOperationException>(() => empty.AdjustBCS(1, 1, 1));
    Reject<InvalidOperationException>(() => empty.ComputeImageMetrics(empty, true));
    empty.Dispose();
    Reject<ObjectDisposedException>(() => empty.AdjustBCS(1, 1, 1));
    Reject<ObjectDisposedException>(() => empty.ComputeImageMetrics(empty, false));
}
using (var image = Image.CreateEmpty(256, 1, false, F.Rgba8))
{
    var data = image.GetData();
    for (var i = 0; i < 256; i++) { data[4 * i] = 100; data[4 * i + 3] = (byte)i; }
    image.SetData(256, 1, false, F.Rgba8, data);
    image.AdjustBCS(-1, 2, -1);
    var adjusted = image.GetData();
    Check(Enumerable.Range(0, 256).All(i => adjusted[4 * i + 3] == i), "Every byte alpha retained with extrapolating factors");
}
if (args.Contains("--native"))
{
    using var blocks = Image.CreateFromData(4, 4, false, F.Dxt1, new byte[8]);
    using var decoded = (Image)blocks.Duplicate(); decoded.Decompress();
    var before = blocks.GetData();
    foreach (var luma in new[] { false, true })
        Check(blocks.ComputeImageMetrics(decoded, luma).Maximum == 0, "Compressed snapshot decode");
    Check(blocks.IsCompressed && before.SequenceEqual(blocks.GetData()), "Compressed source retained");
    using var dpi = DPITexture.CreateFromString("<svg xmlns='http://www.w3.org/2000/svg' width='4' height='4'><rect width='4' height='4' fill='red'/></svg>");
    dpi.Saturation = 0;
    using var image = dpi.GetImage()!;
    var color = image.GetPixel(1, 1);
    Check(Math.Abs(color.R - 1f / 3) < .01 && color.R == color.G && color.G == color.B && color.A == 1, "DPITexture saturation raster path");
}
Console.WriteLine($"Image color/metrics public consumer passed: {checks} checks.");
