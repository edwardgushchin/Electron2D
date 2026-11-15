using Electron2D;
using F = Electron2D.Image.Format;
using A = Electron2D.Image.AlphaMode;
using U = Electron2D.Image.UsedChannels;
using S = Electron2D.Image.CompressSource;

var checks = 0;
void Check(bool value, string label) { checks++; if (!value) throw new Exception(label); }
void Reject<T>(Action action) where T : Exception
{
    try { action(); } catch (T) { checks++; return; }
    throw new Exception($"Expected {typeof(T).Name}");
}
Check(Enum.GetValues<A>().Select(x => (int)x).SequenceEqual([0, 1, 2]), "Alpha identities");
Check(Enum.GetValues<U>().Select(x => (int)x).SequenceEqual([0, 1, 2, 3, 4, 5]), "Channel identities");
Check(Enum.GetValues<S>().Select(x => (int)x).SequenceEqual([0, 1, 2]), "Source identities");
Check((S)typeof(Image).GetMethod(nameof(Image.DetectUsedChannels))!.GetParameters()[0].DefaultValue! == S.Generic, "Default");
using (var empty = new Image())
{
    Check(empty.DetectAlpha() == A.None && empty.IsInvisible, "Empty metadata");
    Reject<InvalidOperationException>(() => empty.GetUsedRect());
    Reject<InvalidOperationException>(() => empty.DetectUsedChannels());
}
foreach (var format in Enum.GetValues<F>().Where(f => f < F.Max))
{
    using var image = Image.CreateEmpty(4, 4, true, format);
    var before = image.GetData(); var notifications = 0; image.Changed += _ => notifications++;
    if (image.IsCompressed)
    {
        Check(!image.IsInvisible && image.DetectAlpha() == (format is F.Dxt3 or F.Dxt5 ? A.Blend : A.None), $"Block metadata {format}");
        Reject<InvalidOperationException>(() => image.GetUsedRect());
        Reject<InvalidOperationException>(() => image.DetectUsedChannels(S.Normal));
    }
    else
    {
        var hasAlpha = format is F.La8 or F.Rgba8 or F.Rgba4444 or F.Rgbaf or F.Rgbah or F.Rgba16 or F.Rgba16I;
        Check(image.IsInvisible == hasAlpha && image.DetectAlpha() == (hasAlpha ? A.Bit : A.None), $"Zero {format}");
        Check(image.GetUsedRect() == (hasAlpha ? default : new Rect2i(0, 0, 4, 4)), $"Zero bounds {format}");
        Check(image.DetectUsedChannels(S.Normal) == U.RedGreen, $"Normal {format}");
        var opaque = format is F.Rgba16I ? 65535f : 1f;
        image.SetPixel(2, 1, new Color(1, .5f, .25f, opaque));
        Check(image.GetUsedRect() == (hasAlpha ? new Rect2i(2, 1, 1, 1) : new Rect2i(0, 0, 4, 4)), $"Exact bounds {format}");
        Check(!image.IsInvisible, $"Visible {format}");
        before = image.GetData(); notifications = 0;
        _ = image.DetectAlpha(); _ = image.DetectUsedChannels(); _ = image.GetUsedRect(); _ = image.IsInvisible;
    }
    Check(before.AsSpan().SequenceEqual(image.GetData()) && notifications == 0, $"Pure queries {format}");
    Reject<ArgumentOutOfRangeException>(() => image.DetectUsedChannels((S)3));
    image.Dispose();
    Reject<ObjectDisposedException>(() => image.DetectAlpha());
    Reject<ObjectDisposedException>(() => image.DetectUsedChannels());
    Reject<ObjectDisposedException>(() => image.GetUsedRect());
    Reject<ObjectDisposedException>(() => { _ = image.IsInvisible; });
}
foreach (var format in new[] { F.La8, F.Rgba8 })
    foreach (byte alpha in new byte[] { 0, 1, 2, 253, 254, 255 })
    {
        byte[] bytes = format == F.La8 ? [20, alpha] : [20, 30, 40, alpha];
        using var image = Image.CreateFromData(1, 1, false, format, bytes);
        Check(image.DetectAlpha() == (alpha < 2 ? A.Bit : alpha < 254 ? A.Blend : A.None), $"Byte threshold {format}/{alpha}");
        Check(image.IsInvisible == (alpha == 0), "Visibility uses exact zero");
    }
foreach (var format in new[] { F.Rgbaf, F.Rgbah, F.Rgba16, F.Rgba16I })
{
    using var image = Image.CreateEmpty(2, 2, true, format);
    var opaque = format == F.Rgba16I ? 65535f : 1f;
    image.Fill(new Color(1, .5f, .25f, opaque));
    for (var y = 0; y < 2; y++) for (var x = 0; x < 2; x++) image.SetPixel(x, y, new Color(1, .5f, .25f, 0));
    Check(image.IsInvisible && image.DetectAlpha() == A.Bit && image.GetUsedRect() == default, "Opaque mip ignored");
    image.SetPixel(1, 1, new Color(1, .5f, .25f, opaque / 2));
    Check(image.DetectAlpha() == A.Blend && image.DetectUsedChannels() == U.Rgba && image.GetUsedRect() == new Rect2i(1, 1, 1, 1), "Fractional typed alpha");
}
foreach (var (color, generic, srgb) in new (Color, U, U)[] {
    (new(0, 0, 0), U.Luminance, U.Luminance), (new(.5f, .5f, .5f, .5f), U.LuminanceAlpha, U.LuminanceAlpha),
    (new(1, 0, 0), U.Red, U.Rgb), (new(1, 1, 0), U.RedGreen, U.Rgb), (new(0, 0, 1), U.Rgb, U.Rgb),
    (new(.0005f, 0, 0), U.Red, U.Rgb), (new(1, .001f, 0), U.Red, U.Rgb), (new(1, .0011f, 0), U.RedGreen, U.Rgb),
    (new(1, 0, 0, .999f), U.Red, U.Rgb), (new(1, 0, 0, .9989f), U.Rgba, U.Rgba) })
{
    using var image = Image.CreateEmpty(4, 4, false, F.Rgbaf); image.Fill(color);
    Check(image.DetectUsedChannels() == generic && image.DetectUsedChannels(S.Srgb) == srgb, $"Channels {color}");
    if (args.Contains("--codecs"))
    {
        using var copy = Image.CreateFromData(4, 4, false, F.Rgbaf, image.GetData());
        copy.Compress(Image.CompressMode.S3tc, S.Srgb);
        Check(copy.PixelFormat == (srgb is U.Rgba or U.LuminanceAlpha ? F.Dxt5 : F.Dxt1), "BC channel selection");
    }
}
using (var image = Image.CreateEmpty(2, 1, false, F.Rgbaf))
{
    image.SetPixel(0, 0, new Color(1, 0, 0, float.NaN));
    image.SetPixel(1, 0, new Color(1, 0, 0, float.Epsilon));
    Check(image.GetUsedRect() == new Rect2i(1, 0, 1, 1), "Bounds require positive alpha");
}
using (var image = Image.CreateFromData(2, 1, false, F.Rgba8, [0, 0, 0, 0, 0, 0, 0, 2]))
{
    Check(image.DetectAlpha() == A.Blend, "Blend wins over transparent endpoint");
    var snapshot = image.GetData(); snapshot[7] = 255;
    Check(image.DetectAlpha() == A.Blend, "Exported bytes are independent");
    image.SetPixel(1, 0, new Color(0, 0, 0, 1));
    Check(image.DetectAlpha() == A.Bit, "Query observes later mutation");
}
using (var image = Image.CreateEmpty(4, 4, false, F.Rgba8))
{
    var writer = Task.Run(() => { for (var i = 0; i < 100; i++) image.Fill(new Color(1, 0, 0, i % 2)); });
    for (var i = 0; i < 100; i++)
    {
        Check(image.GetUsedRect() is var r && (r == default || r == new Rect2i(0, 0, 4, 4)), "Concurrent bounds use a complete snapshot");
        Check(image.DetectAlpha() is A.None or A.Bit, "Concurrent classification");
    }
    writer.GetAwaiter().GetResult();
}
if (args.Contains("--codecs"))
    foreach (float alpha in new[] { 0f, .5f, 1f })
    {
        using var image = Image.CreateEmpty(4, 4, true, F.Rgbaf); image.Fill(new Color(1, .5f, 0, alpha));
        var original = image.GetData(); var notifications = 0; image.Changed += _ => notifications++;
        var encoded = image.SavePNGToBuffer();
        using var decoded = new Image(); decoded.LoadPNGFromBuffer(encoded);
        Check(decoded.PixelFormat == (alpha == 1 ? F.Rgb8 : F.Rgba8), "PNG alpha selection");
        Check(decoded.DetectAlpha() == image.DetectAlpha(), "PNG classification");
        Check(original.AsSpan().SequenceEqual(image.GetData()) && notifications == 0, "PNG snapshot");
    }
Console.WriteLine($"Image analysis: {checks} checks passed; codecs={args.Contains("--codecs")}");
