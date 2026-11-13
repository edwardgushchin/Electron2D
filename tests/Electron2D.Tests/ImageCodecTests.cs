using IOPath = System.IO.Path;
using System.Buffers.Binary;
using System.Text;
using Electron2D;

internal static class ImageCodecTests
{
    private static readonly byte[] Pixels = [255, 0, 0, 128, 0, 255, 0, 255, 0, 0, 255, 255, 255, 255, 0, 255, 255, 0, 255, 255, 0, 255, 255, 255];
    private const string WEBP = "UklGRjQAAABXRUJQVlA4TCgAAAAvAkAAEC8gEEjaH3qN+RcQFPk/moCg6Lrlgh9EMgoCATJEjBIR/Y9Y";
    // libwebpmux fixture: a red 2x2 first frame at (2, 2) on a transparent 4x4 canvas, then a blue frame.
    private const string AnimatedWEBP = "UklGRoQAAABXRUJQVlA4WAoAAAACAAAAAwAAAwAAQU5JTQYAAAAAAAAAAABBTk1GKAAAAAEAAAEAAAEAAAEAAGQAAAJWUDhMDwAAAC8BQAAABxD9j/4HIqL/AQBBTk1GKAAAAAAAAAAAAAMAAAMAAGQAAAJWUDhMDwAAAC8DwAAABxDR//4HIqL/AQA=";
    private const string SVG = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"3px\" height=\"2px\"><rect width=\"3\" height=\"2\" fill=\"#ff0000\"/></svg>";

    internal static void Run()
    {
        VerifyNativeIOCounts();
        using var source = Image.CreateFromData(3, 2, false, Image.Format.Rgba8, Pixels);
        source.GenerateMIPMaps();
        var before = source.GetData();
        var changes = 0;
        source.Changed += _ => changes++;
        var png = source.SavePNGToBuffer();
        Check(source.GetData().SequenceEqual(before) && source.HasMIPMaps && changes == 0, "Encoding uses a snapshot without changing the source.");
        using var image = new Image();
        var loaded = 0;
        image.Changed += _ => loaded++;
        image.LoadPNGFromBuffer(png);
        CheckPixels(image, Pixels);
        Check(!image.HasMIPMaps && loaded == 1, "Load replaces the whole image and emits Changed once.");
        image.LoadWebPFromBuffer(Convert.FromBase64String(WEBP));
        CheckPixels(image, Pixels);
        VerifyAnimatedWebP();
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
        }

        VerifyTGA();
        VerifyPNG();

        var jpg = VerifyJPEG();

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

    internal static byte[] VerifyJPEG()
    {
        // cjpeg -optimize -quality 98, with -progressive -restart 1 for the RGB fixture.
        var gray = Convert.FromBase64String("/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAEBAQEBAQEBAQEBAQEBAQIBAQEBAQIBAQECAgICAgICAgIDAwQDAwMDAwICAwQDAwQEBAQEAgMFBQQEBQQEBAT/wAALCAAIAAkBAREA/8QAFQABAQAAAAAAAAAAAAAAAAAACAr/xAAYEAACAwAAAAAAAAAAAAAAAAAACUaDwv/aAAgBAQAAPwAxpcidGSzw/9k=");
        var progressive = Convert.FromBase64String("/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAEBAQEBAQEBAQEBAQEBAQIBAQEBAQIBAQECAgICAgICAgIDAwQDAwMDAwICAwQDAwQEBAQEAgMFBQQEBQQEBAT/2wBDAQEBAQEBAQIBAQIEAwIDBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAT/wgARCAAIAAkDASIAAhEBAxEB/8QAFgABAQEAAAAAAAAAAAAAAAAAAAcI/8QAFQEBAQAAAAAAAAAAAAAAAAAABAj/3QAEAAH/2gAMAwEAAhADEAAAAYNoInQ3/8QAFxABAAMAAAAAAAAAAAAAAAAAAAYHF//dAAQAAv/aAAgBAQABBQKP1Yy1/8QAGxEAAQQDAAAAAAAAAAAAAAAAUQADBgcWJHH/3QAEAAH/2gAIAQMBAT8Boe3pzhTe0COr/8QAGxEAAQQDAAAAAAAAAAAAAAAABgABBxUksdH/2gAIAQIBAT8BKJLLrJ8jfV//xAAYEAACAwAAAAAAAAAAAAAAAAAAogIkNP/dAAQAAv/aAAgBAQAGPwKFdTOp/8QAFRABAQAAAAAAAAAAAAAAAAAAEKH/2gAIAQEAAT8hln//3QAEAAH/2gAMAwEAAgADAAAAEP8A/8QAFREBAQAAAAAAAAAAAAAAAAAAAMH/2gAIAQMBAT8Qnkf/xAAXEQADAQAAAAAAAAAAAAAAAAAAQVGB/9oACAECAQE/ENiVD//EAB0QAAECBwAAAAAAAAAAAAAAAAABETFBUWFxsfD/3QAEAAL/2gAIAQEAAT8Q1JJoWH0cwf/Z");
        using var image = new Image();
        var changes = 0;
        image.Changed += _ => changes++;
        image.LoadJPGFromBuffer(gray);
        var grayscale = image.GetData();
        Check(image.PixelFormat == Image.Format.L8 && image.Size == new Vector2i(9, 8) &&
            grayscale.Select((sample, index) => Math.Abs(sample - (28 + index % 9 * 23))).Max() <= 2, "Grayscale JPEG retains one byte per pixel, including odd-width rows.");
        image.LoadJPGFromBuffer(progressive);
        var first = image.GetPixel(1, 1);
        var last = image.GetPixel(7, 6);
        Check(image.PixelFormat == Image.Format.Rgb8 && image.Size == new Vector2i(9, 8) && first.A == 1 &&
            last.R - first.R > 0.4f && last.G - first.G > 0.4f && Math.Abs(last.B - 80 / 255f) < 0.12f, "Progressive JPEG reconstructs RGB without alpha storage.");
        image.GenerateMIPMaps();
        var before = image.GetData();
        var changeCount = changes;
        var frame = gray.AsSpan().IndexOf(new byte[] { 0xff, 0xc0 });
        var highPrecision = (byte[])gray.Clone();
        highPrecision[frame + 4] = 12;
        var excessive = (byte[])gray.Clone();
        BinaryPrimitives.WriteUInt32BigEndian(excessive.AsSpan(frame + 5), 0x30003000);
        Reject<NotSupportedException>(() => image.LoadJPGFromBuffer(highPrecision));
        Reject<InvalidDataException>(() => image.LoadJPGFromBuffer(excessive));
        Reject<InvalidDataException>(() => image.LoadJPGFromBuffer(progressive[..^2]));
        Check(image.HasMIPMaps && image.GetData().SequenceEqual(before) && changes == changeCount, "JPEG preflight preserves a populated image on refusal.");

        using var source = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8);
        source.Fill(new Color(0.25f, 0.5f, 0.75f, 0.25f));
        source.GenerateMIPMaps();
        var saved = source.GetData();
        foreach (var quality in new[] { 0.01f, 0.75f, 1f })
        {
            image.LoadJPGFromBuffer(source.SaveJPGToBuffer(quality));
            var pixel = image.GetPixel(5, 5);
            Check(image.Size == source.Size && image.PixelFormat == Image.Format.Rgb8 && !image.HasMIPMaps &&
                pixel.A == 1 && Math.Abs(pixel.R - 0.25f) < 0.12f && Math.Abs(pixel.G - 0.5f) < 0.12f && Math.Abs(pixel.B - 0.75f) < 0.12f,
                "JPEG accepts its quality range, encodes the base level and discards alpha.");
        }
        Check(source.HasMIPMaps && source.GetData().SequenceEqual(saved), "JPEG encoding preserves the source mip chain.");
        Check(source.SaveJPGToBuffer(0.759f).SequenceEqual(source.SaveJPGToBuffer(0.75f)),
            "JPEG passes the same truncated quality percentage for both inputs.");
        foreach (var quality in new[] { float.NaN, float.PositiveInfinity, 0, -1, 1.01f })
            Reject<ArgumentOutOfRangeException>(() => source.SaveJPGToBuffer(quality));
        using var empty = new Image();
        Reject<InvalidOperationException>(() => empty.SaveJPGToBuffer());
        using var block = Image.CreateFromData(4, 4, false, Image.Format.Dxt1, [0, 248, 0, 0, 0, 0, 0, 0]);
        image.LoadJPGFromBuffer(block.SaveJPGToBuffer(1));
        Check(block.IsCompressed && image.GetPixel(3, 3).R > 0.95f, "JPEG accepts a supported block source without changing it.");

        var folder = Directory.CreateTempSubdirectory("Electron2D-jpeg-");
        try
        {
            var path = IOPath.Combine(folder.FullName, "gray.jpeg");
            File.WriteAllBytes(path, gray);
            using var decoded = Image.LoadFromFile(path);
            using var texture = ResourceLoader.Load<ImageTexture>(path, ResourceLoader.CacheMode.Ignore);
            using var snapshot = texture.GetImage()!;
            Check(decoded.PixelFormat == Image.Format.L8 && snapshot.PixelFormat == Image.Format.L8 &&
                snapshot.GetData().SequenceEqual(grayscale), "JPEG file and resource loaders retain the grayscale layout.");
            block.SaveJPG(path, 1);
            using var encoded = Image.LoadFromFile(path);
            Check(encoded.PixelFormat == Image.Format.Rgb8 && encoded.Size == new Vector2i(4, 4), "JPEG file save shares block encoding.");
        }
        finally { folder.Delete(recursive: true); }
        Console.WriteLine("JPEG grayscale, progressive color, block export, file/resource and failure checks passed.");
        return source.SaveJPGToBuffer(1);
    }
    internal static void VerifyPNG()
    {
        var nativeBefore = NativeAllocation.Snapshot().Current;
        using var image = new Image();
        foreach (var (encoded, width, height, format, expected) in new (string, int, int, Image.Format, byte[])[]
        {
            ("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAAAAAA6fptVAAAACklEQVR4nGP4DwABAQEAsTj2FAAAAABJRU5ErkJggg==", 1, 1, Image.Format.L8, [255]),
            ("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR4nGP43wAAAoEBgFnpL/wAAAAASUVORK5CYII=", 1, 1, Image.Format.La8, [255, 128]),
            ("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABEAIAAADA54+dAAAAD0lEQVR4nGMQMgmrmLUHAAYnAmsO3tV6AAAAAElFTkSuQmCC", 1, 1, Image.Format.Rgb8, [18, 86, 154]),
            ("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABEAYAAABPhRjKAAAAEUlEQVR4nGMQMgmrmLXn3gcADakEOVMAyboAAAAASUVORK5CYII=", 1, 1, Image.Format.Rgba8, [18, 86, 154, 222]),
            // 16-bit alpha stays linear while gAMA converts color to display samples.
            ("iVBORw0KGgoAAAANSUhEUgAAAAIAAAABEAQAAAAOu2tCAAAABGdBTUEAAYagMeiWXwAAAA9JREFUeJxjcGBoAMIDDAAIiQIBLVBEoAAAAABJRU5ErkJggg==", 2, 1, Image.Format.La8, [136, 128, 186, 191]),
            // Both colors quantize to 18. Only the exact source key is transparent.
            ("iVBORw0KGgoAAAANSUhEUgAAAAIAAAABEAAAAACB2fwVAAAAAnRSTlMSNC/TSV4AAAANSURBVHicYxAyEQoFAAFiAK6jp76HAAAAAElFTkSuQmCC", 2, 1, Image.Format.La8, [18, 0, 18, 255]),
            ("iVBORw0KGgoAAAANSUhEUgAAAAMAAAABAgMAAABmjvwnAAAACVBMVEX/AAAA/wAAAP8tSs2KAAAAAnRSTlMAgJsrThgAAAAKSURBVHicY5AAAAAaABktiPQ2AAAAAElFTkSuQmCC", 3, 1, Image.Format.Rgba8, [255, 0, 0, 0, 0, 255, 0, 128, 0, 0, 255, 255]),
            ("iVBORw0KGgoAAAANSUhEUgAAAAIAAAACEAAAAAFwSr4tAAAAE0lEQVR4nGMQMmEIq2CYtefeBwAPBQQ5DUPaigAAAABJRU5ErkJggg==", 2, 2, Image.Format.L8, [18, 86, 154, 222]),
        })
        {
            var data = Convert.FromBase64String(encoded);
            image.LoadPNGFromBuffer(data);
            Check(image.Size == new Vector2i(width, height) && image.PixelFormat == format && image.GetData().SequenceEqual(expected), "PNG channel, transfer and transparency contract.");
            image.GenerateMIPMaps();
            var before = image.GetData();
            var png = image.SavePNGToBuffer();
            var colorType = format switch { Image.Format.L8 => 0, Image.Format.La8 => 4, Image.Format.Rgb8 => 2, _ => 6 };
            Check(png[24] == 8 && png[25] == colorType && image.GetData().SequenceEqual(before), "PNG IHDR matches the source layout and encoding preserves the source.");
            image.LoadPNGFromBuffer(png);
            Check(!image.HasMIPMaps && image.PixelFormat == format && image.GetData().SequenceEqual(expected), "PNG saves the base level with exact channel bytes.");
            var invalidCRC = (byte[])data.Clone();
            invalidCRC[^1] ^= 1;
            Reject<InvalidDataException>(() => image.LoadPNGFromBuffer(invalidCRC));
            var idat = data.AsSpan().IndexOf("IDAT"u8);
            var payloadLength = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(idat - 4));
            data[idat + 4 + payloadLength] ^= 1;
            Reject<InvalidDataException>(() => image.LoadPNGFromBuffer(data));
            Check(image.GetData().SequenceEqual(expected), "A late PNG failure preserves the published pixels.");
        }

        using var block = Image.CreateFromData(4, 4, false, Image.Format.Dxt1, [0, 248, 0, 0, 0, 0, 0, 0]);
        image.LoadPNGFromBuffer(block.SavePNGToBuffer());
        Check(block.IsCompressed && image.GetPixel(3, 3) == Colors.Red, "PNG export decompresses blocks in a private snapshot.");
        foreach (var alpha in new[] { 1f, 0.5f })
        {
            using var floating = Image.CreateEmpty(1, 1, false, Image.Format.Rgbaf);
            floating.SetPixel(0, 0, new Color(0, 1, 0, alpha));
            image.LoadPNGFromBuffer(floating.SavePNGToBuffer());
            Check(image.PixelFormat == (alpha == 1f ? Image.Format.Rgb8 : Image.Format.Rgba8) &&
                image.GetPixel(0, 0).G == 1, "PNG converts other formats according to used alpha.");
        }

        // A cached texture must retain the source channel layout as well as its pixel values.
        var folder = Directory.CreateTempSubdirectory("Electron2D-png-");
        try
        {
            var path = IOPath.Combine(folder.FullName, "gray.png");
            using var source = Image.CreateFromData(2, 1, false, Image.Format.La8, [37, 0, 144, 255]);
            source.SavePNG(path);
            using var decoded = Image.LoadFromFile(path);
            using var texture = ResourceLoader.Load<ImageTexture>(path, ResourceLoader.CacheMode.Ignore);
            using var snapshot = texture.GetImage()!;
            Check(decoded.PixelFormat == Image.Format.La8 && snapshot.PixelFormat == Image.Format.La8 &&
                snapshot.GetData().SequenceEqual(source.GetData()), "PNG file and resource texture paths retain LA8 samples.");
        }
        finally { folder.Delete(recursive: true); }
        Check(NativeAllocation.Snapshot().Current == nativeBefore, "PNG temporaries drain after successful and failed operations.");
        Console.WriteLine("PNG channel, transfer, file/resource, failure and ownership checks passed.");
    }

    private static void VerifyAnimatedWebP()
    {
        var bytes = Convert.FromBase64String(AnimatedWEBP);
        using var image = new Image();
        var changes = 0;
        image.Changed += _ => changes++;
        image.LoadWebPFromBuffer(bytes);
        Check(image.Size == new Vector2i(4, 4) && !image.HasMIPMaps && changes == 1, "Animated WebP publishes one complete base canvas.");
        for (var y = 0; y < 4; y++)
            for (var x = 0; x < 4; x++)
                Check(image.GetPixel(x, y) == (x >= 2 && y >= 2 ? Colors.Red : new Color(0, 0, 0, 0)), "The first WebP frame retains its canvas offset and transparent background.");
        var before = image.GetData();
        var malformed = (byte[])bytes.Clone();
        malformed[malformed.AsSpan().IndexOf("VP8L"u8) + 8] = 0;
        foreach (var invalid in new[] { bytes[..^1], malformed })
            Reject<InvalidDataException>(() => image.LoadWebPFromBuffer(invalid));
        Check(changes == 1 && image.GetData().AsSpan().SequenceEqual(before), "Animation parse/decode failures preserve pixels and events.");
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

    internal static void VerifyTGA()
    {
        using var image = new Image();
        foreach (var top in new[] { false, true })
            foreach (var right in new[] { false, true })
            {
                image.LoadTGAFromBuffer(TGA(top, right));
                CheckPixels(image, Pixels);
            }

        var indexed = TGAVector(9, 8, 0x30, 3, 2,
            [0x81, 6, 0x02, 7, 5, 6, 0x80, 7], palette: [0, 0, 255, 0, 255, 0, 255, 0, 0], first: 5);
        byte[] expected = [0, 0, 255, 255, 0, 255, 0, 255, 0, 255, 0, 255, 0, 0, 255, 255, 0, 255, 0, 255, 255, 0, 0, 255];
        image.LoadTGAFromBuffer(indexed);
        CheckPixels(image, expected);

        var indices16 = TGAVector(1, 16, 0x20, 2, 1, [0x00, 0x01, 0x01, 0x01],
            palette: [8, 17, 35, 64, 120, 65, 7, 192], first: 256, paletteDepth: 32);
        image.LoadTGAFromBuffer(indices16);
        Check(image.GetData().AsSpan().SequenceEqual(new byte[] { 35, 17, 8, 64, 7, 65, 120, 192 }), "Indexed TGA preserves palette origin and byte alpha.");

        image.LoadTGAFromBuffer(TGAVector(10, 16, 0x21, 2, 1, [0x01, 0x00, 0x7c, 0xe0, 0x83]));
        Check(image.GetData().AsSpan().SequenceEqual(new byte[] { 248, 0, 0, 0, 0, 248, 0, 255 }), "TGA preserves the optional 16-bit alpha bit.");
        image.LoadTGAFromBuffer(TGAVector(2, 16, 0x20, 1, 1, [0x1f, 0x00]));
        Check(image.GetData().AsSpan().SequenceEqual(new byte[] { 0, 0, 248, 255 }), "TGA packed color without alpha is opaque.");
        image.LoadTGAFromBuffer(TGAVector(2, 24, 0x20, 1, 1, [3, 2, 1]));
        Check(image.GetData().AsSpan().SequenceEqual(new byte[] { 1, 2, 3, 255 }), "TGA 24-bit color uses BGR storage.");
        image.LoadTGAFromBuffer(TGAVector(11, 8, 0x20, 3, 1, [0x82, 37]));
        Check(image.GetData().AsSpan().SequenceEqual(new byte[] { 37, 37, 37, 255, 37, 37, 37, 255, 37, 37, 37, 255 }), "TGA grayscale expands each shade to RGB.");
        image.LoadTGAFromBuffer(TGAVector(3, 16, 0x28, 1, 1, [200, 17]));
        Check(image.GetData().AsSpan().SequenceEqual(new byte[] { 200, 200, 200, 17 }), "TGA grayscale alpha is stored separately.");

        var crossRow = TGAVector(10, 24, 0x20, 3, 2, [0x83, 0, 0, 255, 1, 0, 255, 0, 255, 0, 0]);
        image.LoadTGAFromBuffer(crossRow);
        Check(image.GetPixel(0, 1) == Colors.Red && image.GetPixel(1, 1) == Colors.Green && image.GetPixel(2, 1) == Colors.Blue,
            "TGA repeat and literal packets can span row boundaries.");

        var footer = new byte[26];
        "TRUEVISION-XFILE.\0"u8.CopyTo(footer.AsSpan(8));
        image.LoadTGAFromBuffer([.. indexed, .. footer]);
        CheckPixels(image, expected);
        var unsupportedIndex = (byte[])indexed.Clone();
        unsupportedIndex[^1] = 8;
        var changes = 0;
        image.GenerateMIPMaps();
        var before = image.GetData();
        image.Changed += _ => changes++;
        foreach (var invalid in new byte[][]
        {
            indexed[..17], indexed[..19], indexed[..^1], unsupportedIndex, [.. indexed[..^1], .. footer],
            TGAVector(11, 8, 0x20, 1, 1, [0x81, 33]),
            TGAVector(11, 8, 0x20, ushort.MaxValue, ushort.MaxValue, [0x80, 1])
        })
            Reject<InvalidDataException>(() => image.LoadTGAFromBuffer(invalid));
        Check(image.HasMIPMaps && image.GetData().AsSpan().SequenceEqual(before) && changes == 0, "Malformed TGA keeps pixels, mipmaps and event count.");

        var folder = Directory.CreateTempSubdirectory("Electron2D-tga-");
        try
        {
            var path = IOPath.Combine(folder.FullName, "indexed.TGA");
            File.WriteAllBytes(path, indexed);
            image.Load(path);
            CheckPixels(image, expected);
            Check(changes == 1 && !image.HasMIPMaps, "TGA file success publishes a single base level.");
            using var texture = ResourceLoader.Load<ImageTexture>(path, ResourceLoader.CacheMode.Ignore);
            using var snapshot = texture.GetImage()!;
            CheckPixels(snapshot, expected);
        }
        finally { folder.Delete(recursive: true); }
        Console.WriteLine("TGA origins, palette, RLE, alpha, failure and resource checks passed.");
    }

    private static byte[] TGAVector(byte type, byte depth, byte descriptor, ushort width, ushort height,
        byte[] pixels, byte[]? palette = null, ushort first = 0, byte paletteDepth = 24)
    {
        var header = new byte[19];
        header[0] = 1;
        header[18] = 0x41;
        header[2] = type;
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(12), width);
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(14), height);
        header[16] = depth;
        header[17] = descriptor;
        if (palette is not null)
        {
            header[1] = 1;
            BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(3), first);
            BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(5), (ushort)(palette.Length / (paletteDepth / 8)));
            header[7] = paletteDepth;
        }
        return [.. header, .. (palette ?? []), .. pixels];
    }

    private static byte[] TGA(bool topDown, bool right = false)
    {
        var bytes = new byte[18 + Pixels.Length];
        bytes[2] = 2; bytes[12] = 3; bytes[14] = 2; bytes[16] = 32; bytes[17] = (byte)((topDown ? 40 : 8) | (right ? 16 : 0));
        for (var y = 0; y < 2; y++)
            for (var x = 0; x < 3; x++)
                for (var channel = 0; channel < 4; channel++)
                    bytes[18 + (y * 3 + x) * 4 + channel] = Pixels[((topDown ? y : 1 - y) * 3 + (right ? 2 - x : x)) * 4 + (channel == 3 ? 3 : 2 - channel)];
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
