using Electron2D;
using Path = System.IO.Path;
using System.Diagnostics;

internal static class ImageWebPTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    internal static void RunManaged()
    {
        var data = new byte[19 * 13 * 4];
        new Random(173).NextBytes(data);
        for (var i = 3; i < data.Length; i += 4) data[i] = (byte)((i / 4 % 3) * 127);
        using var source = Image.CreateFromData(19, 13, false, Image.Format.Rgba8, data);
        source.GenerateMIPMaps(); var original = source.GetData(); var changes = 0; source.Changed += _ => changes++;
        using var decoded = new Image();
        foreach (var method in new[] { 0, 2, 6 })
            foreach (var effort in new[] { 0f, 25f, 100f })
            {
                ProjectSettings.Set(ProjectSettings.WebPCompressionMethod, method);
                ProjectSettings.Set(ProjectSettings.WebPLosslessCompressionFactor, effort);
                var bytes = source.SaveWebPToBuffer();
                Check(bytes.AsSpan(12, 4).SequenceEqual("VP8L"u8), "Lossless mode emits the lossless WebP bitstream.");
                decoded.LoadWebPFromBuffer(bytes);
                Check(decoded.GetData().AsSpan().SequenceEqual(data), "Lossless preserves every RGB and alpha byte, including hidden RGB.");
            }
        ProjectSettings.Set(ProjectSettings.WebPCompressionMethod, 2); ProjectSettings.Set(ProjectSettings.WebPLosslessCompressionFactor, 25);
        foreach (var quality in new[] { 0f, 0.75f, 1f })
        {
            var bytes = source.SaveWebPToBuffer(true, quality);
            Check(bytes.AsSpan().IndexOf("VP8 "u8) >= 0 && bytes.AsSpan().IndexOf("VP8L"u8) < 0, "Lossy quality one remains lossy.");
            decoded.LoadWebPFromBuffer(bytes); var actual = decoded.GetData();
            Check(decoded.Size == source.Size && !decoded.HasMIPMaps, "WebP stores the base level dimensions.");
            for (var i = 3; i < data.Length; i += 4) Check(actual[i] == data[i], "Lossy WebP preserves alpha exactly.");
        }
        Check(source.GetData().AsSpan().SequenceEqual(original) && source.HasMIPMaps && changes == 0, "Saving leaves source mips and events untouched.");
        foreach (var format in Enum.GetValues<Image.Format>().Where(value => value < Image.Format.Dxt1 || value is >= Image.Format.R16 and < Image.Format.Max))
        {
            using var input = Image.CreateEmpty(7, 5, false, format); input.Fill(new Color(0.23f, 0.71f, 0.39f, 0.5f));
            using var expected = (Image)input.Duplicate(); expected.Convert(Image.Format.Rgba8);
            decoded.LoadWebPFromBuffer(input.SaveWebPToBuffer()); Check(decoded.GetData().AsSpan().SequenceEqual(expected.GetData()), "All uncompressed source formats convert to exact RGBA8.");
        }
        using var compressed = (Image)source.Duplicate(); compressed.Compress(Image.CompressMode.S3tc);
        using var decompressed = (Image)compressed.Duplicate(); decompressed.Decompress(); decompressed.Convert(Image.Format.Rgba8);
        decoded.LoadWebPFromBuffer(compressed.SaveWebPToBuffer());
        Check(decoded.GetData().AsSpan().SequenceEqual(decompressed.GetData().AsSpan(0, data.Length)) && compressed.IsCompressed, "BC authoring can save WebP without changing compressed source storage.");
        foreach (var quality in new[] { float.NaN, float.PositiveInfinity, -0.01f, 1.01f })
            foreach (var lossy in new[] { false, true }) Reject<ArgumentOutOfRangeException>(() => source.SaveWebPToBuffer(lossy, quality));
        Reject<ArgumentOutOfRangeException>(() => ProjectSettings.Set(ProjectSettings.WebPCompressionMethod, 7));
        Reject<ArgumentException>(() => ProjectSettings.Set(ProjectSettings.WebPLosslessCompressionFactor, float.NaN));
        using var empty = new Image(); Reject<InvalidOperationException>(() => empty.SaveWebPToBuffer());
        using var tooWide = Image.CreateEmpty(16384, 1, false, Image.Format.L8); Reject<IOException>(() => tooWide.SaveWebPToBuffer());
        using var tooLarge = Image.CreateEmpty(4097, 4096, false, Image.Format.L8); Reject<IOException>(() => tooLarge.SaveWebPToBuffer());
        using var maximumWidth = Image.CreateEmpty(16383, 1, false, Image.Format.Rgb8); decoded.LoadWebPFromBuffer(maximumWidth.SaveWebPToBuffer()); Check(decoded.Width == 16383, "Exact native maximum width is accepted.");
        using var etc = Image.CreateEmpty(4, 4, false, Image.Format.Etc); using var decodedETC = new Image(); decodedETC.LoadWebPFromBuffer(etc.SaveWebPToBuffer()); Check(decodedETC.Width == 4 && etc.IsCompressed, "WebP saving decodes ETC without changing source storage.");
        var disposed = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); disposed.Dispose(); Reject<ObjectDisposedException>(() => disposed.SaveWebPToBuffer());
        Parallel.For(0, 12, _ => { using var result = new Image(); result.LoadWebPFromBuffer(source.SaveWebPToBuffer()); Check(result.GetData().AsSpan().SequenceEqual(data), "Concurrent encoders own independent native pictures and output."); });
        var settingsFile = Path.GetTempFileName();
        try { ProjectSettings.SaveCustom(settingsFile); } finally { File.Delete(settingsFile); }
        VerifyFiles(source, data);
        Console.WriteLine("WebP: 9 exact RGBA effort profiles, 3 lossy qualities, source formats, BC/mips, concurrency, limits, atomic files and fresh archives passed.");
    }

    private static void VerifyFiles(Image source, byte[] data)
    {
        var roots = (ProjectSettings.ProjectRoot, ProjectSettings.UserDataRoot);
        var root = Path.Combine(Path.GetTempPath(), "Electron2D-WebP-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root); Directory.CreateDirectory(Path.Combine(root, "user"));
        try
        {
            ProjectSettings.ConfigurePaths(root, Path.Combine(root, "user"));
            foreach (var path in new[] { "res://image.WEBP", "user://image.webp", Path.Combine(root, "absolute.webp") })
            {
                source.SaveWebP(path); using var image = Image.LoadFromFile(path);
                Check(image.GetData().AsSpan().SequenceEqual(data), "Every supported file path round trips WebP.");
                using var texture = ResourceLoader.Load<ImageTexture>(path, ResourceLoader.CacheMode.Ignore); using var pixels = texture.GetImage()!;
                Check(pixels.GetData().AsSpan().SequenceEqual(data), "ResourceLoader supplies the saved texture pixels.");
            }
            using var stored = ResourceLoader.Load<ImageTexture>("res://image.WEBP", ResourceLoader.CacheMode.Ignore);
            ResourceSaver.Save(stored, Path.Combine(root, "texture.e2dres"));
            var process = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            process.ArgumentList.Add(typeof(ImageWebPTests).Assembly.Location); process.ArgumentList.Add("--image-webp-archive"); process.ArgumentList.Add(root); process.Environment.Remove("ELECTRON2D_TEST_IMAGE_WEBP");
            using var child = Process.Start(process)!; var output = child.StandardOutput.ReadToEnd(); var error = child.StandardError.ReadToEnd();
            Check(child.WaitForExit(30000) && child.ExitCode == 0, "Fresh WebP archive failed: " + output + error);
            var destination = Path.Combine(root, "image.WEBP"); var before = File.ReadAllBytes(destination);
            using var empty = new Image(); Reject<InvalidOperationException>(() => empty.SaveWebP(destination)); Reject<ArgumentOutOfRangeException>(() => source.SaveWebP(destination, true, float.NaN));
            Check(File.ReadAllBytes(destination).AsSpan().SequenceEqual(before), "Failed encode preserves the existing file.");
            Reject<IOException>(() => source.SaveWebP(Path.Combine(root, "missing", "image.webp")));
            Check(Directory.GetFiles(root).All(path => !path.EndsWith(".tmp", StringComparison.Ordinal)), "Failed atomic save leaves no temporary files.");
        }
        finally { ProjectSettings.ConfigurePaths(roots.ProjectRoot, roots.UserDataRoot); Directory.Delete(root, true); }
    }

    internal static void RunArchive(string root)
    {
        using var expected = Image.LoadFromFile(Path.Combine(root, "image.WEBP"));
        using var texture = ResourceLoader.Load<ImageTexture>(Path.Combine(root, "texture.e2dres"), ResourceLoader.CacheMode.Ignore); using var pixels = texture.GetImage()!;
        Check(pixels.GetData().AsSpan().SequenceEqual(expected.GetData()), "Fresh process restores saved WebP-derived texture pixels.");
    }

    private sealed class Painter(ImageTexture texture) : Entity
    {
        protected override void OnDraw() => DrawTextureRect(texture, new Rect2(4, 4, 48, 40), false);
    }
    internal static void RunNative(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var source = Image.CreateEmpty(6, 5, false, Image.Format.Rgba8); source.Fill(new Color(0.23f, 0.71f, 0.39f));
        using var loaded = new Image(); loaded.LoadWebPFromBuffer(source.SaveWebPToBuffer()); using var texture = ImageTexture.CreateFromImage(loaded);
        var window = new Window { Size = new(64, 56) }; window.AddChild(new Painter(texture) { TextureFilter = TextureFilter.Nearest }); using var target = window.GetTexture(); var frames = 0; var verified = false;
        window.Ready += _ => RenderingServer.FramePostDraw += Capture;
        void Capture()
        {
            if (++frames != 4) return;
            using var pixels = target.GetImage()!; Check(pixels.GetPixel(20, 20).IsEqualApprox(source.GetPixel(2, 2)), "Saved WebP pixels reach the real texture sampler.");
            using var restored = new Image(); restored.LoadWebPFromBuffer(pixels.SaveWebPToBuffer());
            Check(restored.GetData().AsSpan().SequenceEqual(pixels.GetData()), "Real rendered output round trips exact lossless WebP.");
            verified = true; RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit();
        }
        Check(Engine.Run(window) == 0 && verified, "Native WebP rendering and capture finish."); Console.WriteLine("WebP real texture and capture passed: " + backend);
    }
}
