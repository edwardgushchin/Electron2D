using Electron2D;
using Format = Electron2D.Image.Format;

internal static class ImagePixelTests
{
    internal static void RunManaged()
    {
        var normalized = new Color(.25f, .75f, .5f, .625f);
        var floating = new Color(-2f, .5f, 4f, 1.25f);
        var integer = new Color(1234.75f, -1f, 65536f, 2.75f);
        (Format Format, Color Input, string Bytes, Color Decoded)[] samples =
        [
            (Format.L8, normalized, "BF", new(191 / 255f, 191 / 255f, 191 / 255f)),
            (Format.La8, normalized, "BF9F", new(191 / 255f, 191 / 255f, 191 / 255f, 159 / 255f)),
            (Format.R8, normalized, "3F", new(63 / 255f, 0, 0)),
            (Format.Rg8, normalized, "3FBF", new(63 / 255f, 191 / 255f, 0)),
            (Format.Rgb8, normalized, "3FBF7F", new(63 / 255f, 191 / 255f, 127 / 255f)),
            (Format.Rgba8, normalized, "3FBF7F9F", new(63 / 255f, 191 / 255f, 127 / 255f, 159 / 255f)),
            (Format.Rgba4444, normalized, "793B", new(3 / 15f, 11 / 15f, 7 / 15f, 9 / 15f)),
            (Format.Rgb565, normalized, "EF3D", new(7 / 31f, 47 / 63f, 15 / 31f)),
            (Format.Rf, floating, "000000C0", new(-2, 0, 0)),
            (Format.Rgf, floating, "000000C00000003F", new(-2, .5f, 0)),
            (Format.Rgbf, floating, "000000C00000003F00008040", new(-2, .5f, 4)),
            (Format.Rgbaf, floating, "000000C00000003F000080400000A03F", floating),
            (Format.Rh, floating, "00C0", new(-2, 0, 0)),
            (Format.Rgh, floating, "00C00038", new(-2, .5f, 0)),
            (Format.Rgbh, floating, "00C000380044", new(-2, .5f, 4)),
            (Format.Rgbah, floating, "00C000380044003D", floating),
            (Format.Rgbe9995, floating, "00400094", new(0, .5f, 4)),
            (Format.R16, normalized, "FF3F", new(16383 / 65535f, 0, 0)),
            (Format.Rg16, normalized, "FF3FFFBF", new(16383 / 65535f, 49151 / 65535f, 0)),
            (Format.Rgb16, normalized, "FF3FFFBFFF7F", new(16383 / 65535f, 49151 / 65535f, 32767 / 65535f)),
            (Format.Rgba16, normalized, "FF3FFFBFFF7FFF9F", new(16383 / 65535f, 49151 / 65535f, 32767 / 65535f, 40959 / 65535f)),
            (Format.R16I, integer, "D204", new(1234, 0, 0)),
            (Format.Rg16I, integer, "D2040000", new(1234, 0, 0)),
            (Format.Rgb16I, integer, "D2040000FFFF", new(1234, 0, 65535)),
            (Format.Rgba16I, integer, "D2040000FFFF0200", new(1234, 0, 65535, 2)),
        ];
        foreach (var sample in samples)
        {
            var bytes = Convert.FromHexString(sample.Bytes);
            using var source = Image.CreateEmpty(2, 2, true, sample.Format);
            var changes = 0;
            source.Changed += _ => changes++;
            source.SetPixel(0, 1, sample.Input);
            source.SetPixel(new Vector2i(1, 1), sample.Input);
            var expected = new byte[5 * bytes.Length];
            bytes.CopyTo(expected, 2 * bytes.Length);
            bytes.CopyTo(expected, 3 * bytes.Length);
            Equal(source, 2, 2, expected, $"{sample.Format} base writes preserve the authored mip");
            Check(source.GetPixel(0, 1) == sample.Decoded && source.GetPixel(new Vector2i(1, 1)) == sample.Decoded,
                $"{sample.Format} typed reads");
            source.Fill(sample.Input);
            for (var offset = 0; offset < expected.Length; offset += bytes.Length)
                bytes.CopyTo(expected, offset);
            Equal(source, 2, 2, expected, $"{sample.Format} all-level fill");
            Check(changes == 3, $"{sample.Format} change count");

            // A single nonzero pixel exposes byte-stride/re-encoding mistakes for each layout.
            using var rotated = Image.CreateFromData(3, 2, false, sample.Format, new byte[6 * bytes.Length]);
            rotated.SetPixel(0, 0, sample.Input);
            rotated.Rotate90(ClockDirection.Clockwise);
            expected = new byte[6 * bytes.Length];
            bytes.CopyTo(expected, bytes.Length);
            Equal(rotated, 2, 3, expected, $"{sample.Format} exact stored-byte rotation");
        }

        using (var half = Image.CreateEmpty(1, 1, false, Format.Rgbah))
        {
            half.Fill(new Color(1.00075f, -0f, -.00005f, 65520f));
            Equal(half, 1, 1, Convert.FromHexString("003C00000000FF7B"), "half truncation and underflow");
        }
        using (var clamped = Image.CreateEmpty(1, 1, false, Format.Rgba8))
        {
            clamped.Fill(new Color(float.MaxValue, float.PositiveInfinity, float.NegativeInfinity, float.NaN));
            Equal(clamped, 1, 1, [255, 255, 0, 0], "clamp before unsigned conversion");
        }

        Check((int)ClockDirection.Clockwise == 0 && (int)ClockDirection.CounterClockwise == 1, "direction identities");
        const string authored = "0A141E28323C46505A646E78828C96E0E1E2";
        Transform(5, 3, authored, x => x.FlipX(), 5, 3, "32281E140A645A50463C968C82786E46323C");
        Transform(5, 3, authored, x => x.FlipY(), 5, 3, "6E78828C963C46505A640A141E28325A6E64");
        Transform(5, 3, authored, x => x.Rotate180(), 5, 3, "968C82786E645A50463C32281E140A78646E");
        Transform(5, 3, authored, x => x.Rotate90(ClockDirection.Clockwise), 3, 5, "6E3C0A78461482501E8C5A289664325A6E64");
        Transform(5, 3, authored, x => x.Rotate90(ClockDirection.CounterClockwise), 3, 5, "326496285A8C1E50821446780A3C6E46323C");
        Transform(1, 5, "0A141E2832F0F1F2", x => x.Rotate90(ClockDirection.Clockwise), 5, 1, "32281E140A2D1923");
        Transform(5, 1, "0A141E2832F0F1F2", x => x.Rotate90(ClockDirection.CounterClockwise), 1, 5, "32281E140A2D1923");
        VerifyFailures();
        Console.WriteLine("Image pixels: 25 format vectors, authored and regenerated mips, narrow and rectangular extents, copies, events and rollback passed.");
    }

    internal static void RunNative(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var source = Image.CreateEmpty(3, 2, true, Format.Rgba8);
        source.Fill(Colors.Black);
        source.SetPixel(0, 0, Colors.Red);
        source.SetPixel(new Vector2i(1, 0), Colors.Lime);
        source.SetPixel(2, 0, Colors.Blue);
        source.SetPixel(new Vector2i(0, 1), Colors.White);
        source.FlipX();
        using var texture = ImageTexture.CreateFromImage(source);
        var host = new Window { Size = new Vector2i(64, 64) };
        host.AddChild(new Sprite { Texture = texture, Centered = false, Scale = new Vector2(12, 12), TextureFilter = TextureFilter.Nearest });
        using var target = host.GetTexture();
        var frames = 0;
        string? driver = null;
        host.Ready += _ =>
        {
            driver = RenderingServer.GetCurrentRenderingDriverName();
            Check(RenderingServer.GetCurrentRenderingMethod() == backend && (backend != "gpu" || driver != "software"),
                "Requested native texture backend is active");
            RenderingServer.FramePostDraw += ReadFrame;
        };
        Check(Engine.Run(host) == 0 && frames == 6, "Window drew and read back both texture revisions");
        Console.WriteLine($"Image pixels: {backend}/{driver} rendered and read 12 expected texels before and after SetImage.");

        void ReadFrame()
        {
            frames++;
            if (frames != 3 && frames != 6)
                return;
            using var captured = target.GetImage()!;
            Color[] expected = frames == 3
                ? [Colors.Blue, Colors.Lime, Colors.Red, Colors.Black, Colors.Black, Colors.White]
                : [Colors.Black, Colors.Blue, Colors.Black, Colors.Lime, Colors.White, Colors.Red];
            var width = frames == 3 ? 3 : 2;
            for (var i = 0; i < expected.Length; i++)
            {
                var pixel = captured.GetPixel(6 + (i % width) * 12, 6 + (i / width) * 12);
                Check(pixel.ToRGBA32() == expected[i].ToRGBA32(), $"Frame {frames}, texel {i}: expected {expected[i]}, got {pixel}");
            }
            if (frames == 6)
            {
                RenderingServer.FramePostDraw -= ReadFrame;
                host.Tree!.Quit();
                return;
            }
            source.Rotate90(ClockDirection.Clockwise);
            Reject<ArgumentException>(() => texture.Update(source));
            texture.SetImage(source);
            source.Fill(Colors.White);
        }
    }

    private static void Transform(int width, int height, string source, Action<Image> action, int outputWidth, int outputHeight, string output)
    {
        var input = Convert.FromHexString(source);
        var expected = Convert.FromHexString(output);
        using var image = Image.CreateFromData(width, height, true, Format.R8, input);
        using var texture = ImageTexture.CreateFromImage(image);
        Array.Clear(input);
        Array.Clear(image.GetData());
        var changes = 0;
        image.Changed += _ =>
        {
            changes++;
            Equal(image, outputWidth, outputHeight, expected, "Changed sees the complete state");
        };
        action(image);
        Check(changes == 1 && image.HasMIPMaps && image.MIPMapCount == 2, "one committed transform with two mips");
        using var copied = texture.GetImage()!;
        Equal(copied, width, height, Convert.FromHexString(source), "texture snapshot is independent");
    }

    private static void VerifyFailures()
    {
        using var empty = new Image();
        using var compressed = Image.CreateEmpty(4, 4, false, Format.Dxt1);
        var disposed = Image.CreateEmpty(2, 2, false, Format.Rgba8);
        disposed.Dispose();
        Action<Image>[] operations =
        [
            x => x.GetPixel(0, 0), x => x.GetPixel(Vector2i.Zero),
            x => x.SetPixel(0, 0, Colors.White), x => x.SetPixel(Vector2i.Zero, Colors.White),
            x => x.Fill(Colors.White), x => x.FlipX(), x => x.FlipY(),
            x => x.Rotate90(ClockDirection.Clockwise), x => x.Rotate180(),
        ];
        var changes = 0;
        empty.Changed += _ => changes++;
        compressed.Changed += _ => changes++;
        foreach (var operation in operations)
        {
            Reject<InvalidOperationException>(() => operation(empty));
            Reject<InvalidOperationException>(() => operation(compressed));
            Reject<ObjectDisposedException>(() => operation(disposed));
        }
        Check(empty.IsEmpty && changes == 0 && compressed.GetData().All(value => value == 0), "invalid states retain storage");

        using var image = Image.CreateFromData(2, 1, true, Format.R8, [10, 20, 255]);
        image.Changed += _ => changes++;
        foreach (var point in new Vector2i[] { new(-1, 0), new(0, -1), new(2, 0), new(0, 1) })
        {
            Reject<ArgumentOutOfRangeException>(() => image.GetPixel(point));
            Reject<ArgumentOutOfRangeException>(() => image.GetPixel(point.X, point.Y));
            Reject<ArgumentOutOfRangeException>(() => image.SetPixel(point, Colors.White));
            Reject<ArgumentOutOfRangeException>(() => image.SetPixel(point.X, point.Y, Colors.White));
        }
        Reject<ArgumentOutOfRangeException>(() => image.Rotate90((ClockDirection)2));
        Equal(image, 2, 1, [10, 20, 255], "rejected coordinates and direction retain the full state");
        Check(changes == 0, "rejections do not emit Changed");
        image.Changed += _ => throw new ApplicationException("subscriber");
        Reject<ApplicationException>(() => image.FlipX());
        Equal(image, 2, 1, [20, 10, 15], "observer failure follows commitment");
        Check(changes == 1, "committed flip emits one change");
    }

    private static void Equal(Image image, int width, int height, byte[] expected, string description)
    {
        var actual = image.GetData(out var size, out _, out _);
        Check(size == new Vector2i(width, height) && actual.AsSpan().SequenceEqual(expected),
            $"{description}: {size}, expected {Convert.ToHexString(expected)}, got {Convert.ToHexString(actual)}");
    }

    private static void Reject<T>(Action operation) where T : Exception
    {
        try { operation(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    private static void Check(bool condition, string description)
    {
        if (!condition)
            throw new InvalidOperationException(description);
    }
}
