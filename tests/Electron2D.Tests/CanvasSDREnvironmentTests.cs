using Electron2D;

internal static class CanvasSDREnvironmentTests
{
    private static readonly Color Swatch = new(64 / 255f, 144 / 255f, 224 / 255f, 1);
    private static readonly Vector2i Size = new(96, 64);

    internal static void Run(string profile)
    {
        var compatibility = profile == "compatibility";
        ProjectSettings.Set(ProjectSettings.RenderingMethod, compatibility ? "compatibility" : "gpu");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var pixels = Image.CreateEmpty(Size.X, Size.Y, false, Image.Format.Rgba8);
        for (var y = 0; y < Size.Y; y++)
            for (var x = 0; x < Size.X; x++)
                pixels.SetPixel(x, y, x < 32 ? Swatch : x < 48 ? new(0, 0, 0, 128 / 255f) : ((x + y) & 1) == 0 ? Colors.White : Colors.Black);
        using var source = ImageTexture.CreateFromImage(pixels);
        using var rampPixel = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
        var rampColor = new Color(32 / 255f, 160 / 255f, 96 / 255f, 0);
        rampPixel.SetPixel(0, 0, rampColor);
        using var ramp = ImageTexture.CreateFromImage(rampPixel);
        rampPixel.SetPixel(0, 0, Colors.Black);
        using var proxySource = ImageTexture.CreateFromImage(rampPixel);
        using var mapPixels = Image.CreateEmpty(3, 1, false, Image.Format.Rgba8);
        mapPixels.Fill(Colors.White);
        mapPixels.SetPixel(1, 0, Colors.Black);
        using var mapSource = ImageTexture.CreateFromImage(mapPixels);
        using var blackMap = new AtlasTexture { Atlas = mapSource, Region = new(1, 0, 1, 1) };
        using var environment = new CanvasEnvironment();
        using var root = new Window { Size = Size, Title = "SDR canvas environment check", TransparentBG = true };
        var child = new SubViewport { Size = Size, TransparentBG = true, RenderTargetUpdateMode = ViewportUpdateMode.Always };
        if (!compatibility) child.MSAA2D = Viewport.MSAA.Samples4X;
        var childSprite = SpriteFor(source);
        root.AddChild(new WorldEnvironment { Environment = environment });
        root.AddChild(SpriteFor(source));
        root.AddChild(child);
        child.AddChild(new WorldEnvironment { Environment = environment });
        child.AddChild(childSprite);
        using var rootTexture = root.GetTexture();
        using var childTexture = child.GetTexture();

        var completed = false;
        root.Ready += _ =>
        {
            RenderingServer.ForceDraw();
            var initial = ReadBytes(rootTexture);
            var initialChild = ReadBytes(childTexture);
            var rid = environment.GetRID();
            environment.TonemapExposure = 2;
            RenderingServer.EnvironmentSetAdjustment(rid, true, .5f, 1, 0);
            RenderingServer.ForceDraw();
            if (compatibility)
            {
                Check(ReadBytes(rootTexture).AsSpan().SequenceEqual(initial), "Compatibility retains its ordinary byte rendering.");
            }
            else
            {
                var native = RenderingServer.TextureGetRDTexture(rootTexture.GetRID());
                Check(native.IsValid(), "Byte viewport exposes a borrowed native texture.");
                var gray = (Swatch.R + Swatch.G + Swatch.B) / 3;
                Expect(rootTexture, 8, new(gray, gray, gray, 1), "Exposure followed by linear brightness and encoded saturation");
                Expect(childTexture, 8, new(gray, gray, gray, 1), "MSAA byte adjustment");
                Expect(childTexture, 40, new(0, 0, 0, 128 / 255f), "Preserved fractional alpha");

                environment.AdjustmentEnabled = false;
                environment.TonemapMode = CanvasEnvironment.ToneMapper.Reinhard;
                environment.TonemapExposure = 1.5f;
                environment.TonemapWhite = 3;
                float Reference(float channel)
                {
                    var value = Decode(channel) * 1.5;
                    return (float)Encode(value * (1 + value / 9) / (1 + value));
                }
                var tone = new Color(Reference(Swatch.R), Reference(Swatch.G), Reference(Swatch.B), 1);
                RenderingServer.ForceDraw();
                Expect(rootTexture, 8, tone, "SDR Reinhard white point and transfer");

                environment.TonemapMode = CanvasEnvironment.ToneMapper.Linear;
                environment.TonemapExposure = 1;
                environment.GlowEnabled = true;
                environment.GlowBlend = CanvasEnvironment.GlowBlendMode.Mix;
                environment.GlowMix = 1;
                environment.GlowBloom = 1;
                environment.GlowStrength = 1;
                for (var i = 0; i < RenderingServer.MaxGlowLevels; i++) environment.SetGlowLevel(i, i == 0 ? 1 : 0);
                RenderingServer.ForceDraw();
                var half = (float)Encode(.5);
                Expect(rootTexture, 72, new(half, half, half, 1), "Black and white are filtered in linear light");
                Expect(childTexture, 72, new(half, half, half, 1), "MSAA glow retains byte storage");

                environment.GlowMap = blackMap;
                environment.GlowMapStrength = 1;
                RenderingServer.ForceDraw();
                Expect(rootTexture, 72, Colors.Black, "Glow map filtering stays within the atlas region");

                var proxy = RenderingServer.TextureProxyCreate(proxySource.GetRID());
                try
                {
                    RenderingServer.EnvironmentSetGlow(rid, true, [1, 0, 0, 0, 0, 0, 0], 1, 1, 1, 1,
                        CanvasEnvironment.GlowBlendMode.Mix, 1, 2, 12, 1, proxy);
                    RenderingServer.ForceDraw();
                    Expect(rootTexture, 72, Colors.Black, "A glow map proxy samples the current ordinary texture");
                }
                finally
                {
                    environment.GlowMap = null;
                    RenderingServer.FreeRID(proxy);
                }

                environment.GlowStrength = 0;
                environment.GlowMix = .5f;
                RenderingServer.ForceDraw();
                Expect(rootTexture, 8, new((float)Encode(Decode(Swatch.R) * .5), (float)Encode(Decode(Swatch.G) * .5),
                    (float)Encode(Decode(Swatch.B) * .5), 1), "Zero-level mix still decodes and encodes once");

                RenderingServer.EnvironmentSetAdjustment(rid, true, 1, 1, 1, ramp.GetRID());
                RenderingServer.ForceDraw();
                Expect(childTexture, 40, rampColor with { A = 128 / 255f }, "SDR glow composite encodes LUT RGB and keeps source alpha");
                environment.GlowEnabled = false;
                RenderingServer.EnvironmentSetAdjustment(rid, true, .5f, 1, 0);

                child.RenderTargetClearMode = ViewportClearMode.Never;
                childSprite.Visible = false;
                RenderingServer.ForceDraw();
                var retained = ReadBytes(childTexture);
                for (var i = 0; i < 48; i++) RenderingServer.ForceDraw();
                var before = GC.GetAllocatedBytesForCurrentThread();
                for (var i = 0; i < 64; i++) RenderingServer.ForceDraw();
                var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                Check(allocated == 0, $"Prepared SDR environment frames allocated {allocated} owner bytes.");
                Check(ReadBytes(childTexture).AsSpan().SequenceEqual(retained), "Never-clear adjustment does not accumulate.");
                RenderingServer.ViewportSetEnvironmentMode(root.GetViewportRID(), RenderingServer.ViewportEnvironmentMode.Disabled);
                RenderingServer.ForceDraw();
                Check(ReadBytes(rootTexture).AsSpan().SequenceEqual(initial), "Root bypass restores exact ordinary bytes.");
                Check(ReadBytes(childTexture).AsSpan().SequenceEqual(initialChild), "Inherited bypass restores exact unprocessed MSAA history.");
                Check(RenderingServer.TextureGetRDTexture(rootTexture.GetRID()) == native, "Native texture identity survives effect switches.");
                Console.WriteLine($"SDR canvas environment: byte and 4x MSAA pixels, transfer and glow filtering and map, LUT, alpha, history, bypass, native identity and 64 warmed frames ({allocated} owner bytes).");
            }
            completed = true;
            root.Tree!.Quit();
        };
        Check(Engine.Run(root) == 0 && completed, "The native SDR environment scenario completed.");
        if (compatibility) Console.WriteLine("SDR canvas environment: Compatibility byte output passed.");
    }

    private static Sprite SpriteFor(Texture texture) => new() { Texture = texture, Centered = false, TextureFilter = TextureFilter.Nearest };
    private static double Decode(double value) => value <= .04045 ? value / 12.92 : Math.Pow((value + .055) / 1.055, 2.4);
    private static double Encode(double value) => value <= .0031308 ? 12.92 * value : 1.055 * Math.Pow(value, 1 / 2.4) - .055;

    private static byte[] ReadBytes(ViewportTexture texture)
    {
        using var image = texture.GetImage()!;
        Check(image.PixelFormat == Image.Format.Rgba8, "The output remains RGBA8.");
        return image.GetData();
    }

    private static void Expect(ViewportTexture texture, int x, Color expected, string scenario)
    {
        using var image = texture.GetImage()!;
        Check(image.PixelFormat == Image.Format.Rgba8, "The output remains RGBA8.");
        var actual = image.GetPixel(x, 16);
        const float tolerance = 1.1f / 255;
        Check(MathF.Abs(actual.R - expected.R) < tolerance && MathF.Abs(actual.G - expected.G) < tolerance &&
            MathF.Abs(actual.B - expected.B) < tolerance && MathF.Abs(actual.A - expected.A) < tolerance,
            $"{scenario} expected {expected}, read {actual}.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
