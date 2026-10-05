using IOPath = System.IO.Path;
using System.Buffers.Binary;
using System.Text;
using Electron2D;

internal static class ImageCodecTests
{
    private static readonly byte[] Pixels = [255, 0, 0, 128, 0, 255, 0, 255, 0, 0, 255, 255, 255, 255, 0, 255, 255, 0, 255, 255, 0, 255, 255, 255];
    private const string WEBP = "UklGRjQAAABXRUJQVlA4TCgAAAAvAkAAEC8gEEjaH3qN+RcQFPk/moCg6Lrlgh9EMgoCATJEjBIR/Y9Y";
    private const string SVG = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"3px\" height=\"2px\"><rect width=\"3\" height=\"2\" fill=\"#ff0000\"/></svg>";

    internal static void Run()
    {
        VerifyNativeIOCounts();
        using var source = Image.CreateFromData(3, 2, false, Image.Format.Rgba8, Pixels);
        source.GenerateMipmaps();
        var before = source.GetData();
        var changes = 0;
        source.Changed += _ => changes++;
        var png = source.SavePNGToBuffer();
        Check(source.GetData().SequenceEqual(before) && source.HasMipmaps && changes == 0, "Encoding uses a snapshot without changing the source.");
        using var image = new Image();
        var loaded = 0;
        image.Changed += _ => loaded++;
        image.LoadPNGFromBuffer(png);
        CheckPixels(image, Pixels);
        Check(!image.HasMipmaps && loaded == 1, "Load replaces the whole image and emits Changed once.");
        image.LoadWebPFromBuffer(Convert.FromBase64String(WEBP));
        CheckPixels(image, Pixels);
        image.LoadSVGFromString(SVG);
        Check(image.Size == new Vector2i(3, 2) && image.PixelFormat == Image.Format.Rgba8 && image.GetPixel(1, 1) == Colors.Red,
            "SVG string rasterizes at intrinsic size and color.");
        image.LoadSVGFromBuffer(Encoding.UTF8.GetBytes(SVG), 2);
        Check(image.Size == new Vector2i(6, 4) && image.GetPixel(3, 2) == Colors.Red,
            "SVG buffer scale changes raster dimensions and preserves fill.");
        image.LoadSVGFromString("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 4 3\"><rect width=\"4\" height=\"3\" fill=\"#00ff00\"/></svg>");
        Check(image.Size == new Vector2i(4, 3) && image.GetPixel(1, 1) == Colors.Green,
            "SVG viewBox supplies intrinsic dimensions.");
        foreach (var topDown in new[] { false, true })
        {
            image.LoadBMPFromBuffer(BMP(topDown));
            var opaque = (byte[])Pixels.Clone(); opaque[3] = 255;
            CheckPixels(image, opaque);
            image.LoadTGAFromBuffer(TGA(topDown));
            CheckPixels(image, Pixels);
        }

        // Independent PNG vectors catch grayscale palette scaling and RGB16 byte-order corruption.
        foreach (var (encoded, expected) in new (string, byte[])[]
        {
            ("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAAAAAA6fptVAAAACklEQVR4nGP4DwABAQEAsTj2FAAAAABJRU5ErkJggg==", [255, 255, 255, 255]),
            ("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR4nGP43wAAAoEBgFnpL/wAAAAASUVORK5CYII=", [255, 255, 255, 128]),
            ("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABEAIAAADA54+dAAAAD0lEQVR4nGMQMgmrmLUHAAYnAmsO3tV6AAAAAElFTkSuQmCC", [18, 86, 154, 255]),
            ("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABEAYAAABPhRjKAAAAEUlEQVR4nGMQMgmrmLXn3gcADakEOVMAyboAAAAASUVORK5CYII=", [18, 86, 154, 222]),
            ("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABEAAAAABq7kcWAAAAC0lEQVR4nGNYfRYAAiYBee5J6uwAAAAASUVORK5CYII=", [171, 171, 171, 255]),
            ("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABEAQAAADljNBBAAAADUlEQVR4nGMQMll9FgADDAG/brnGXQAAAABJRU5ErkJggg==", [18, 18, 18, 171]),
        })
        {
            image.LoadPNGFromBuffer(Convert.FromBase64String(encoded));
            Check(image.GetData().SequenceEqual(expected), "PNG grayscale, alpha and 16-bit samples convert correctly to 8-bit pixels.");
        }

        using var solid = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8);
        solid.Fill(new Color(0.25f, 0.5f, 0.75f, 0.25f));
        var jpg = solid.SaveJPGToBuffer(1);
        foreach (var quality in new[] { 0.01f, 0.75f, 1f })
        {
            image.LoadJPGFromBuffer(solid.SaveJPGToBuffer(quality));
            var pixel = image.GetPixel(5, 5);
            Check(image.Size == solid.Size && pixel.A == 1 && Math.Abs(pixel.R - 0.25f) < 0.12f && Math.Abs(pixel.B - 0.75f) < 0.12f,
                "JPEG accepts its quality range, preserves dimensions and discards alpha.");
        }
        foreach (var quality in new[] { float.NaN, float.PositiveInfinity, 0, -1, 1.01f })
            Reject<ArgumentOutOfRangeException>(() => source.SaveJPGToBuffer(quality));

        image.LoadPNGFromBuffer(png);
        var count = loaded;
        var badDimensions = (byte[])png.Clone();
        BinaryPrimitives.WriteUInt32BigEndian(badDimensions.AsSpan(16), 0x7fffffff);
        foreach (var bytes in new[] { Array.Empty<byte>(), png[..12], png[..^1], badDimensions, jpg, new byte[] { 0, 1, 2, 3 } })
            Reject<InvalidDataException>(() => image.LoadPNGFromBuffer(bytes));
        Reject<InvalidDataException>(() => image.LoadJPGFromBuffer(png));
        Reject<InvalidDataException>(() => image.LoadWebPFromBuffer(png));
        Reject<InvalidDataException>(() => image.LoadBMPFromBuffer(png));
        Reject<InvalidDataException>(() => image.LoadTGAFromBuffer(png));
        Reject<InvalidDataException>(() => image.LoadSVGFromBuffer(png));
        var corrupt = (byte[])png.Clone();
        Array.Fill(corrupt, (byte)255, 41, 8);
        Reject<InvalidDataException>(() => image.LoadPNGFromBuffer(corrupt));
        CheckPixels(image, Pixels);
        Check(loaded == count, "Header and decoder failures leave pixels and notifications unchanged.");
        foreach (var svg in new[] { "<svg width=\"50000\" height=\"2\"/>", "<!DOCTYPE svg [<!ENTITY x SYSTEM 'file:///etc/passwd'>]><svg>&x;</svg>", "<svg width=\"3\" height=\"2\"><rect" })
            Reject<InvalidDataException>(() => image.LoadSVGFromString(svg));
        foreach (var scale in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
            Reject<ArgumentOutOfRangeException>(() => image.LoadSVGFromString(SVG, scale));
        CheckPixels(image, Pixels);
        Check(loaded == count, "SVG preflight failures preserve image state and notifications.");
        Reject<InvalidDataException>(() => image.LoadPNGFromBuffer(new byte[64 * 1024 * 1024 + 1]));

        using var empty = new Image();
        Reject<InvalidOperationException>(() => empty.SavePNGToBuffer());
        using var compressed = Image.CreateFromData(4, 4, false, Image.Format.Dxt1, new byte[8]);
        Reject<NotSupportedException>(() => compressed.SavePNGToBuffer());
        VerifyFiles(source, png, jpg);
        using var callback = new Image();
        callback.Changed += _ => throw new ApplicationException("codec callback");
        Reject<ApplicationException>(() => callback.LoadPNGFromBuffer(png));
        CheckPixels(callback, Pixels);
        image.Dispose();
        Reject<ObjectDisposedException>(() => image.LoadPNGFromBuffer(png));
        Reject<ObjectDisposedException>(() => image.LoadSVGFromString(SVG));
        Reject<ObjectDisposedException>(() => image.SavePNGToBuffer());
        if (!OperatingSystem.IsBrowser())
            Parallel.For(0, 12, _ => { using var copy = new Image(); copy.LoadPNGFromBuffer(png); copy.LoadPNGFromBuffer(copy.SavePNGToBuffer()); CheckPixels(copy, Pixels); });
        Console.WriteLine("Image codec checks passed (PNG/JPEG/WebP/BMP/TGA/SVG; buffers, files, failures, ownership).");
    }

    private static unsafe void VerifyNativeIOCounts()
    {
        byte[] bytes = [11, 22, 33];
        fixed (byte* data = bytes)
        {
            var stream = SDL3.SDL.IOFromConstMem((nint)data, (nuint)bytes.Length);
            if (stream == 0) throw new InvalidOperationException(SDL3.SDL.GetError());
            try
            {
                var output = stackalloc byte[4];
                Check(SDL3.SDL.ReadIO(stream, (nint)output, 4) == 3 && new ReadOnlySpan<byte>(output, 3).SequenceEqual(bytes), "Native size_t read counts and copied bytes are exact.");
                Check(SDL3.SDL.ReadIO(stream, (nint)output, 4) == 0, "Native size_t EOF count is zero.");
            }
            finally { SDL3.SDL.CloseIO(stream); }
            stream = SDL3.SDL.IOFromDynamicMem();
            if (stream == 0) throw new InvalidOperationException(SDL3.SDL.GetError());
            try
            {
                Check(SDL3.SDL.WriteIO(stream, (nint)data, (nuint)bytes.Length) == 3 && SDL3.SDL.GetIOSize(stream) == 3, "Native size_t write counts and output length are exact.");
            }
            finally { SDL3.SDL.CloseIO(stream); }
        }
    }

    private static void VerifyFiles(Image source, byte[] png, byte[] jpg)
    {
        var settings = ProjectSettings.Service;
        var roots = (ProjectSettings.ProjectRoot, ProjectSettings.UserDataRoot);
        var root = IOPath.Combine(IOPath.GetTempPath(), "Electron2D-codecs-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(IOPath.Combine(root, "user"));
        try
        {
            ProjectSettings.ConfigurePaths(root, IOPath.Combine(root, "user"));
            source.SavePNG("res://texture.PNG");
            source.SavePNG("user://texture.png");
            source.SaveJPG("user://texture.jpeg");
            foreach (var path in new[] { "res://texture.PNG", "user://texture.png", IOPath.Combine(root, "texture.PNG") })
            {
                using var decoded = Image.LoadFromFile(path);
                CheckPixels(decoded, Pixels);
                using var texture = ImageTexture.CreateFromImage(decoded);
                decoded.Fill(Colors.Black);
                using var snapshot = texture.GetImage();
                CheckPixels(snapshot!, Pixels);
            }
            var inputs = new[] { (".png", png), (".jpg", jpg), (".webp", Convert.FromBase64String(WEBP)), (".bmp", BMP(false)), (".tga", TGA(false)), (".svg", Encoding.UTF8.GetBytes(SVG)) };
            foreach (var (extension, bytes) in inputs)
            {
                var path = IOPath.Combine(root, "input" + extension);
                File.WriteAllBytes(path, bytes);
                using var decoded = Image.LoadFromFile(path);
                Check(decoded.Width > 0 && decoded.Height > 0, "Every integrated file extension reaches its decoder.");
            }
            var destination = IOPath.Combine(root, "texture.PNG");
            var old = File.ReadAllBytes(destination);
            using var empty = new Image();
            Reject<InvalidOperationException>(() => empty.SavePNG(destination));
            Reject<ArgumentOutOfRangeException>(() => source.SaveJPG(destination, float.NaN));
            Check(File.ReadAllBytes(destination).SequenceEqual(old), "Failed encoding preserves an existing destination.");
            Reject<IOException>(() => source.SavePNG(root));
            Reject<DirectoryNotFoundException>(() => source.SavePNG(IOPath.Combine(root, "absent", "out.png")));
            Reject<FileNotFoundException>(() => Image.LoadFromFile("res://missing.png"));
            Reject<NotSupportedException>(() => Image.LoadFromFile("res://texture.unknown"));
            Reject<UnauthorizedAccessException>(() => source.SavePNG("res://../escape.png"));
            Reject<UnauthorizedAccessException>(() => Image.LoadFromFile("res://../escape.png"));
            var large = IOPath.Combine(root, "large.png");
            using (var file = File.Create(large)) file.SetLength(64 * 1024 * 1024 + 1);
            Reject<InvalidDataException>(() => Image.LoadFromFile(large));
            Check(!Directory.EnumerateFiles(root).Any(path => path.EndsWith(".tmp", StringComparison.Ordinal)), "Failed atomic writes clean their temporary files.");
        }
        finally { ProjectSettings.ConfigurePaths(roots.ProjectRoot, roots.UserDataRoot); Directory.Delete(root, true); }
    }

    private static byte[] BMP(bool topDown)
    {
        var bytes = new byte[54 + 24];
        bytes[0] = (byte)'B'; bytes[1] = (byte)'M';
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(2), bytes.Length);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(10), 54);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(14), 40);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(18), 3);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(22), topDown ? -2 : 2);
        bytes[26] = 1; bytes[28] = 24;
        for (var y = 0; y < 2; y++)
            for (var x = 0; x < 3; x++)
                for (var channel = 0; channel < 3; channel++)
                    bytes[54 + y * 12 + x * 3 + channel] = Pixels[((topDown ? y : 1 - y) * 3 + x) * 4 + 2 - channel];
        return bytes;
    }

    private static byte[] TGA(bool topDown)
    {
        var bytes = new byte[18 + Pixels.Length];
        bytes[2] = 2; bytes[12] = 3; bytes[14] = 2; bytes[16] = 32; bytes[17] = (byte)(topDown ? 40 : 8);
        for (var y = 0; y < 2; y++)
            for (var x = 0; x < 3; x++)
                for (var channel = 0; channel < 4; channel++)
                    bytes[18 + (y * 3 + x) * 4 + channel] = Pixels[((topDown ? y : 1 - y) * 3 + x) * 4 + (channel == 3 ? 3 : 2 - channel)];
        return bytes;
    }

    private static void CheckPixels(Image image, byte[] expected) => Check(image.Size == new Vector2i(3, 2) && image.PixelFormat == Image.Format.Rgba8 && image.GetData().SequenceEqual(expected), "Decoded color, alpha, orientation and row pitch are exact.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
