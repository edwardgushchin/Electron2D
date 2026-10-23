using Electron2D;
using System.Buffers.Binary;
using System.Diagnostics;
using Path = System.IO.Path;

internal static class ImageEXRTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static readonly Image.Format[] Formats = [Image.Format.R8, Image.Format.Rg8, Image.Format.Rgb8, Image.Format.Rgba8, Image.Format.Rh, Image.Format.Rgh, Image.Format.Rgbh, Image.Format.Rgbah, Image.Format.Rf, Image.Format.Rgf, Image.Format.Rgbf, Image.Format.Rgbaf];
    internal static void RunManaged()
    {
        foreach (var format in Formats)
        {
            using var source = Image.CreateEmpty(7, 5, false, format);
            for (var y = 0; y < 5; y++) for (var x = 0; x < 7; x++) source.SetPixel(x, y, new Color((x - 3) * 0.25f, 3.5f, (y - 2) * 0.5f, 0.375f));
            source.GenerateMIPMaps(); var before = source.GetData(); var events = 0; source.Changed += _ => events++;
            var bytes = source.SaveEXRToBuffer(); using var decoded = new Image(); decoded.LoadEXRFromBuffer(bytes);
            using var expected = (Image)source.Duplicate(); expected.ClearMIPMaps();
            var target = format switch { Image.Format.R8 => Image.Format.Rh, Image.Format.Rg8 => Image.Format.Rgh, Image.Format.Rgb8 => Image.Format.Rgbh, Image.Format.Rgba8 => Image.Format.Rgbah, _ => format }; expected.Convert(target);
            Check(decoded.PixelFormat == target && decoded.GetData().AsSpan().SequenceEqual(expected.GetData()), "All EXR source precision/channel roles round trip exact samples.");
            Check(source.HasMIPMaps && source.GetData().AsSpan().SequenceEqual(before) && events == 0 && !decoded.HasMIPMaps, "EXR saves stable base pixels without changing source mips/events.");
            if (Environment.GetEnvironmentVariable("ELECTRON2D_EXR_ORACLE_DIR") is { } root) { Directory.CreateDirectory(root); File.WriteAllBytes(Path.Combine(root, format + ".exr"), bytes); File.WriteAllBytes(Path.Combine(root, format + ".expected"), expected.GetData()); }
        }
        using var raw = Image.CreateEmpty(3, 2, false, Image.Format.Rgbaf); raw.Fill(new Color(-2, 3.5f, 0.25f, -0.5f)); using var loaded = new Image();
        loaded.LoadEXRFromBuffer(raw.SaveEXRToBuffer(colorImage: true, maxLinearValue: 2)); Check(loaded.GetPixel(1, 1) == new Color(0, 2, 0.25f, 0), "Color and upper clamp apply to every stored component.");
        loaded.LoadEXRFromBuffer(raw.SaveEXRToBuffer(maxLinearValue: 0)); Check(loaded.GetPixel(1, 1) == new Color(-2, 0, 0, -0.5f), "Upper clamp preserves negative raw data.");
        foreach (var format in new[] { Image.Format.R8, Image.Format.Rh, Image.Format.Rf })
        {
            using var gray = Image.CreateEmpty(3, 2, false, format); gray.Fill(new Color(0.375f, 0, 0)); loaded.LoadEXRFromBuffer(gray.SaveEXRToBuffer(true));
            var expected = format == Image.Format.R8 ? (float)(Half)gray.GetPixel(1, 1).R : gray.GetPixel(1, 1).R;
            Check(loaded.GetPixel(1, 1) == new Color(expected, expected, expected), "Monochrome Y expands to RGB.");
        }
        var specials = new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, -0f, float.MaxValue, float.Epsilon };
        var specialBytes = new byte[specials.Length * 4]; for (var i = 0; i < specials.Length; i++) BinaryPrimitives.WriteSingleLittleEndian(specialBytes.AsSpan(i * 4), specials[i]);
        using var special = Image.CreateFromData(specials.Length, 1, false, Image.Format.Rf, specialBytes); loaded.LoadEXRFromBuffer(special.SaveEXRToBuffer());
        Check(loaded.GetData().AsSpan().SequenceEqual(specialBytes), "Raw float EXR preserves nonfinite values, negative zero and extrema.");
        Reject<ArgumentException>(() => raw.SaveEXRToBuffer(true)); Reject<ArgumentOutOfRangeException>(() => raw.SaveEXRToBuffer(maxLinearValue: float.NaN));
        using var unsupported = Image.CreateEmpty(1, 1, false, Image.Format.Rgba16); Reject<NotSupportedException>(() => unsupported.SaveEXRToBuffer());
        using var empty = new Image(); Reject<InvalidOperationException>(() => empty.SaveEXRToBuffer());
        var disposed = Image.CreateEmpty(1, 1, false, Image.Format.Rf); disposed.Dispose(); Reject<ObjectDisposedException>(() => disposed.SaveEXRToBuffer());
        using var large = Image.CreateEmpty(8193, 4096, false, Image.Format.R8); Reject<IOException>(() => large.SaveEXRToBuffer());
        var valid = raw.SaveEXRToBuffer(); loaded.LoadEXRFromBuffer(valid); var saved = loaded.GetData(); var notifications = 0; loaded.Changed += _ => notifications++;
        foreach (var bad in new[] { Array.Empty<byte>(), valid[..7], valid[..^4], new byte[12] }) { Reject<InvalidDataException>(() => loaded.LoadEXRFromBuffer(bad)); Check(loaded.GetData().AsSpan().SequenceEqual(saved) && notifications == 0, "Decode failure is atomic."); }
        Parallel.For(0, 12, _ => { using var copy = new Image(); copy.LoadEXRFromBuffer(raw.SaveEXRToBuffer()); Check(copy.GetData().AsSpan().SequenceEqual(raw.GetData()), "Parallel codec state is independent."); });
        var rootPath = Path.Combine(Path.GetTempPath(), "Electron2D-EXR-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(rootPath);
        try
        {
            var path = Path.Combine(rootPath, "image.EXR"); raw.SaveEXR(path); using var file = Image.LoadFromFile(path); Check(file.GetData().AsSpan().SequenceEqual(raw.GetData()), "EXR file extension reaches the precision-preserving decoder.");
            using var texture = ResourceLoader.Load<ImageTexture>(path, ResourceLoader.CacheMode.Ignore); using var pixels = texture.GetImage()!; Check(pixels.PixelFormat == raw.PixelFormat && pixels.GetData().AsSpan().SequenceEqual(raw.GetData()), "ResourceLoader retains HDR texture storage.");
            ResourceSaver.Save(texture, Path.Combine(rootPath, "texture.e2dres"));
            var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false }; if (System.IO.Path.GetFileNameWithoutExtension(start.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(typeof(ImageEXRTests).Assembly.Location); start.ArgumentList.Add("--image-exr-archive"); start.ArgumentList.Add(rootPath); start.Environment.Remove("ELECTRON2D_TEST_IMAGE_EXR");
            using var child = Process.Start(start)!; var report = child.StandardOutput.ReadToEnd(); var error = child.StandardError.ReadToEnd();
            Check(child.WaitForExit(30000) && child.ExitCode == 0, "Fresh HDR resource archive failed: " + report + error);
            var previous = File.ReadAllBytes(path); Reject<ArgumentException>(() => raw.SaveEXR(path, true)); Check(File.ReadAllBytes(path).AsSpan().SequenceEqual(previous), "Encode rejection preserves an existing file."); Reject<IOException>(() => raw.SaveEXR(Path.Combine(rootPath, "missing", "image.exr")));
        }
        finally { Directory.Delete(rootPath, true); }
        Console.WriteLine("EXR: 12 source roles, raw/monochrome/clamps, nonfinite bits, mips, limits, failure atomicity, concurrency and HDR file/resource checks passed.");
    }
    internal static void RunOracle(string root)
    {
        var count = 0;
        foreach (var expected in Directory.GetFiles(root, "native-*.decoded"))
        {
            var data = File.ReadAllBytes(expected);
            Console.WriteLine("Independent EXR " + Path.GetFileName(expected));
            using var image = Image.LoadFromFile(Path.ChangeExtension(expected, ".exr"));
            var width = BinaryPrimitives.ReadInt32LittleEndian(data); var height = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(4));
            Check(image.Size == new Vector2i(width, height) && image.GetData().AsSpan().SequenceEqual(data.AsSpan(16)), "Independent OpenEXR precision, channel, compression and tile values match exactly."); count++;
        }
        Check(count >= 192, "The complete independent compression/precision/channel/scanline/tile matrix is present.");
        Console.WriteLine("EXR: " + count + " independent OpenEXR channel/precision/compression/scanline/tile/negative-origin vectors passed.");
    }
    internal static void RunArchive(string root)
    {
        using var expected = Image.LoadFromFile(Path.Combine(root, "image.EXR"));
        using var texture = ResourceLoader.Load<ImageTexture>(Path.Combine(root, "texture.e2dres"), ResourceLoader.CacheMode.Ignore);
        using var actual = texture.GetImage()!;
        Check(actual.PixelFormat == expected.PixelFormat && actual.GetData().AsSpan().SequenceEqual(expected.GetData()), "Fresh archive restores exact HDR channels and precision.");
    }
    private sealed class Painter(ImageTexture texture) : Entity
    {
        protected override void OnDraw() => DrawTextureRect(texture, new Rect2(4, 4, 48, 40), false);
    }
    internal static void RunNative(string profile)
    {
        var backend = profile.Split(':')[0];
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var source = Image.CreateEmpty(6, 5, false, Image.Format.Rgba8); source.Fill(new Color(0.25f, 0.5f, 0.75f)); using var loaded = new Image(); loaded.LoadEXRFromBuffer(source.SaveEXRToBuffer()); if (profile == "compatibility") loaded.Convert(Image.Format.Rgba8);
        using var texture = ImageTexture.CreateFromImage(loaded);
        var window = new Window { Size = new(64, 56) }; window.AddChild(new Painter(texture) { TextureFilter = TextureFilter.Nearest }); using var target = window.GetTexture(); var frames = 0; var verified = false;
        window.Ready += _ => RenderingServer.FramePostDraw += Capture;
        void Capture()
        {
            if (++frames != 4) return;
            using var pixels = target.GetImage()!; Check(pixels.GetPixel(20, 20).IsEqualApprox(source.GetPixel(2, 2)), "EXR-derived pixels reach native texture sampling.");
            using var restored = new Image(); restored.LoadEXRFromBuffer(pixels.SaveEXRToBuffer(colorImage: true, maxLinearValue: 1)); restored.Convert(Image.Format.Rgba8); Check(restored.GetData().AsSpan().SequenceEqual(pixels.GetData()), "Rendered capture persists through half EXR and restores its visible bytes.");
            verified = true; RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit();
        }
        if (profile == "compatibility:hdr")
        {
            Reject<NotSupportedException>(() => Engine.Run(window)); Check(window.IsDisposed && RenderingServer.Service == null, "Unsupported HDR driver releases the host.");
            Console.WriteLine("EXR Compatibility: actual float-precision rejection and cleanup passed."); return;
        }
        Check(Engine.Run(window) == 0 && verified, "EXR native texture/capture run finishes."); Console.WriteLine("EXR native texture/capture passed: " + backend);
    }
}
