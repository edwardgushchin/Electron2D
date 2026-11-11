using Electron2D;
using ToneMapper = Electron2D.CanvasEnvironment.ToneMapper;

internal static class CanvasTonemapTests
{
    private static readonly Color[] Source =
    [
        new(0.50000000f, 4.00000000f, 0.12500000f, 1.00000000f),
        new(0.06250000f, 0.18750000f, 8.00000000f, 1.00000000f),
        new(-0.50000000f, 0.25000000f, 1.50000000f, 1.00000000f),
        new(0.00000000f, 0.00000000f, 0.00000000f, 0.50000000f),
        new(8.00000000f, 8.00000000f, 8.00000000f, 1.00000000f)
    ];

    // Double-precision reference results for exposure 1.25, white 4, AgX white 8 and contrast 1.4.
    private static readonly Color[][] Expected =
    [
        [
            new(0.62500000f, 5.00000000f, 0.15625000f, 1.00000000f),
            new(0.07812500f, 0.23437500f, 10.00000000f, 1.00000000f),
            new(-0.62500000f, 0.31250000f, 1.87500000f, 1.00000000f),
            new(0.00000000f, 0.00000000f, 0.00000000f, 0.50000000f),
            new(10.00000000f, 10.00000000f, 10.00000000f, 1.00000000f)
        ],
        [
            new(0.39963942f, 1.09375000f, 0.13645481f, 1.00000000f),
            new(0.07281760f, 0.19265477f, 1.47727273f, 1.00000000f),
            new(0.00000000f, 0.24274554f, 0.72860054f, 1.00000000f),
            new(0.00000000f, 0.00000000f, 0.00000000f, 0.50000000f),
            new(1.47727273f, 1.47727273f, 1.47727273f, 1.00000000f)
        ],
        [
            new(0.55106822f, 1.02887848f, 0.18519025f, 1.00000000f),
            new(0.08899774f, 0.27066531f, 1.09137080f, 1.00000000f),
            new(0.00000000f, 0.34427139f, 0.86056297f, 1.00000000f),
            new(0.00000000f, 0.00000000f, 0.00000000f, 0.50000000f),
            new(1.09137080f, 1.09137080f, 1.09137080f, 1.00000000f)
        ],
        [
            new(0.92670708f, 1.01585690f, 0.74724744f, 1.00000000f),
            new(0.74572290f, 0.48356214f, 1.07236160f, 1.00000000f),
            new(0.14970431f, 0.44908775f, 0.94384986f, 1.00000000f),
            new(-0.00039735f, -0.00039735f, -0.00039734f, 0.50000000f),
            new(1.03747922f, 1.03747922f, 1.03746885f, 1.00000000f)
        ],
        [
            new(0.73867016f, 0.93219953f, 0.54980284f, 1.00000000f),
            new(0.50726294f, 0.68850044f, 1.12555553f, 1.00000000f),
            new(0.11749314f, 0.42396441f, 0.85602461f, 1.00000000f),
            new(0.00000000f, 0.00000000f, 0.00000000f, 0.50000000f),
            new(1.00000000f, 1.00000000f, 1.00000000f, 1.00000000f)
        ]
    ];

    internal static void Run(string profile)
    {
        if (profile == "managed") Managed();
        else Native(profile == "compatibility");
    }

    private static void Managed()
    {
        using var environment = new CanvasEnvironment();
        Check(environment.TonemapMode == ToneMapper.Linear && environment.TonemapExposure == 1 && environment.TonemapWhite == 1 &&
            environment.TonemapAGXWhite == 16.29f && environment.TonemapAGXContrast == 1.25f, "Tone defaults.");
        Reject<ArgumentOutOfRangeException>(() => environment.TonemapMode = (ToneMapper)5);
        Reject<ArgumentOutOfRangeException>(() => environment.TonemapWhite = 0);
        Reject<ArgumentOutOfRangeException>(() => environment.TonemapAGXWhite = float.PositiveInfinity);
        Reject<ArgumentOutOfRangeException>(() => environment.TonemapAGXContrast = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => environment.TonemapExposure = -1);
        Reject<InvalidOperationException>(() => RenderingServer.EnvironmentSetTonemap(environment.GetRID(), ToneMapper.Linear, 1, 1));

        environment.TonemapMode = ToneMapper.AGX;
        environment.TonemapWhite = 3;
        environment.TonemapAGXWhite = 8;
        environment.TonemapExposure = 2;
        environment.TonemapAGXContrast = 1.4f;
        environment.ResourceLocalToScene = true;
        using var root = new WorldEnvironment { Environment = environment };
        using var scene = new PackedScene();
        scene.Pack(root);
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-tonemap-" + Guid.NewGuid().ToString("N") + ".e2dscene");
        try
        {
            ResourceSaver.Save(scene, path);
            using var saved = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep);
            using var node = (WorldEnvironment)saved.Instantiate();
            using var copy = node.Environment!;
            Check(!ReferenceEquals(copy, environment) && copy.TonemapMode == ToneMapper.AGX && copy.TonemapExposure == 2 &&
                copy.TonemapWhite == 3 && copy.TonemapAGXWhite == 8 && copy.TonemapAGXContrast == 1.4f, "Typed local scene copy retains both white points.");
        }
        finally { File.Delete(path); }

        var settings = TonemapSettings.Default;
        settings.Mode = ToneMapper.Reinhard;
        settings.White = 2;
        var reinhard = TonemapUniforms.Prepare(settings, 4);
        Check(reinhard.Options.Z == 4 && reinhard.Options.W == 4 && reinhard.Curve.X == .25f, "Reinhard follows output headroom.");
        settings.Mode = ToneMapper.AGX;
        settings.AGXWhite = 8;
        var agx = TonemapUniforms.Prepare(settings, 4);
        Check(agx.Options.W == 32 && MathF.Abs(agx.Curve.Y - 1f / 31.82f) < 1e-6f, "AgX scales its white point with headroom.");
        Console.WriteLine("Canvas tonemap managed: authoring, validation, typed scene files and output headroom passed.");
    }

    private static void Native(bool compatibility)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, compatibility ? "compatibility" : "gpu");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var environment = new CanvasEnvironment();
        using var image = Image.CreateEmpty(Source.Length, 1, false, compatibility ? Image.Format.Rgba8 : Image.Format.Rgbaf);
        for (var x = 0; x < Source.Length; x++) image.SetPixel(x, 0, Source[x]);
        using var source = ImageTexture.CreateFromImage(image);
        using var root = new Window { Size = new(96, 64), Title = "Canvas tone mapping check", UseHDR2D = !compatibility, TransparentBG = true };
        var child = new SubViewport
        {
            Size = new(96, 64),
            UseHDR2D = !compatibility,
            TransparentBG = true,
            RenderTargetUpdateMode = ViewportUpdateMode.Always,
            MSAA2D = compatibility ? Viewport.MSAA.Disabled : Viewport.MSAA.Samples4X
        };
        root.AddChild(new WorldEnvironment { Environment = environment });
        root.AddChild(MakeSprite(source));
        root.AddChild(child);
        child.AddChild(new WorldEnvironment { Environment = environment });
        var childSprite = MakeSprite(source);
        child.AddChild(childSprite);
        using var texture = root.GetTexture();
        using var childTexture = child.GetTexture();
        var finished = false;
        root.Ready += _ =>
        {
            RenderingServer.ForceDraw();
            using var baseline = texture.GetImage()!;
            var baselineBytes = baseline.GetData();
            environment.TonemapMode = ToneMapper.ACES;
            if (compatibility)
            {
                RenderingServer.ForceDraw();
                using var unchanged = texture.GetImage()!;
                Check(unchanged.GetData().AsSpan().SequenceEqual(baselineBytes), "Compatibility uses ordinary SDR output.");
                finished = true;
                root.Tree!.Quit();
                return;
            }

            Verify(baseline, Source, "Raw HDR source");
            var rootRID = root.GetViewportRID();
            var alias = RenderingServer.TextureGetRDTexture(texture.GetRID());
            Check(alias.IsValid(), "The HDR canvas exports a device identity.");
            environment.TonemapExposure = 1.25f;
            environment.TonemapWhite = 4;
            environment.TonemapAGXWhite = 8;
            environment.TonemapAGXContrast = 1.4f;
            foreach (var mode in Enum.GetValues<ToneMapper>())
            {
                environment.TonemapMode = mode;
                RenderingServer.ForceDraw();
                using var actual = texture.GetImage()!;
                using var actualChild = childTexture.GetImage()!;
                Verify(actual, Expected[(int)mode], $"{mode} root");
                Verify(actualChild, Expected[(int)mode], $"{mode} MSAA child");
                Check(RenderingServer.TextureGetRDTexture(texture.GetRID()) == alias, "Tone mode does not replace the native target.");
            }

            var output = Environment.GetEnvironmentVariable("ELECTRON2D_TONEMAP_OUTPUT");
            using (var screen = RenderingServer.Service!.Readback())
            {
                var actual = screen.GetPixel(8, 8);
                var linear = Expected[(int)ToneMapper.AGX][0];
                Check(MathF.Abs(actual.R - Encode(linear.R)) <= 2f / 255 &&
                    MathF.Abs(actual.G - Encode(linear.G)) <= 2f / 255 &&
                    MathF.Abs(actual.B - Encode(linear.B)) <= 2f / 255, "Window presentation encodes the tone-mapped result.");
                if (output is not null) screen.SavePNG(output);
            }

            var rid = environment.GetRID();
            RenderingServer.EnvironmentSetTonemap(rid, ToneMapper.Reinhard, .5f, 6);
            Check(environment.TonemapWhite == 6 && environment.TonemapAGXWhite == 8, "The server uses the selected white point.");
            Reject<ArgumentOutOfRangeException>(() => RenderingServer.EnvironmentSetTonemap(rid, ToneMapper.AGX, 2, 0));
            Check(environment.TonemapMode == ToneMapper.Reinhard && environment.TonemapExposure == .5f, "Invalid server batch is atomic.");
            Reject<ArgumentException>(() => RenderingServer.EnvironmentSetTonemap(rootRID, ToneMapper.Linear, 1, 1));
            Reject<InvalidOperationException>(() => Task.Run(() => RenderingServer.EnvironmentSetTonemapAGXContrast(rid, 1)).GetAwaiter().GetResult());
            RenderingServer.EnvironmentSetTonemap(rid, ToneMapper.AGX, 1.25f, 8);
            RenderingServer.EnvironmentSetTonemapAGXContrast(rid, 1.4f);
            Check(environment.TonemapWhite == 6 && environment.TonemapAGXContrast == 1.4f, "AgX server edits retain independent regular white.");

            child.RenderTargetClearMode = ViewportClearMode.Never;
            childSprite.Visible = false;
            RenderingServer.ForceDraw();
            using (var retained = childTexture.GetImage()!) Verify(retained, Expected[(int)ToneMapper.AGX], "Never-clear raw history");
            RenderingServer.ViewportSetEnvironmentMode(rootRID, RenderingServer.ViewportEnvironmentMode.Disabled);
            RenderingServer.ForceDraw();
            using (var raw = childTexture.GetImage()!) Verify(raw, Source, "Inherited bypass restores unprocessed history");
            RenderingServer.ViewportSetEnvironmentMode(rootRID, RenderingServer.ViewportEnvironmentMode.Inherit);

            environment.TonemapMode = ToneMapper.Filmic;
            environment.TonemapExposure = .25f;
            environment.GlowEnabled = true;
            environment.GlowBlend = CanvasEnvironment.GlowBlendMode.Additive;
            environment.GlowHDRThreshold = 7;
            environment.GlowIntensity = 1;
            RenderingServer.ForceDraw();
            using (var below = texture.GetImage()!) Check(MathF.Abs(below.GetPixel(72, 20).R) < .002f, "Exposure below the extraction threshold.");
            environment.TonemapExposure = 2;
            RenderingServer.ForceDraw();
            using (var halo = texture.GetImage()!) Check(halo.GetPixel(72, 20).R > .02f, "Exposed highlights contribute before filmic mapping.");
            environment.GlowEnabled = false;
            environment.TonemapMode = ToneMapper.Linear;
            environment.TonemapExposure = 0;
            RenderingServer.ForceDraw();
            using (var black = texture.GetImage()!)
            {
                Check(black.GetPixel(8, 8) == Colors.Black, "Zero linear exposure produces opaque black on an opaque source.");
                Check(black.GetPixel(56, 8).A == .5f, "Exposure preserves fractional alpha.");
            }
            environment.TonemapMode = ToneMapper.AGX;
            environment.TonemapExposure = float.MaxValue;
            environment.TonemapAGXContrast = float.MaxValue;
            RenderingServer.ForceDraw();
            using (var extreme = texture.GetImage()!) Check(extreme.GetPixel(8, 8).IsFinite(), "Extreme finite authoring produces finite output.");

            environment.TonemapExposure = 1.25f;
            environment.TonemapAGXContrast = 1.4f;
            for (var i = 0; i < 48; i++) RenderingServer.ForceDraw();
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++) RenderingServer.ForceDraw();
            var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            Check(bytes == 0, $"Prepared tone mapping allocated {bytes} owner bytes.");
            finished = true;
            root.Tree!.Quit();
            Console.WriteLine($"Canvas tonemap GPU: five independent color references, root and 4x MSAA child, alpha, raw history, glow order, RID edits, finite extremes and 64 warmed frames ({bytes} owner bytes).");
        };
        Check(Engine.Run(root) == 0 && finished, "The native tone mapping scenario completed.");
        if (compatibility) Console.WriteLine("Canvas tonemap compatibility: ordinary SDR output and lifecycle passed.");
    }

    private static Sprite MakeSprite(Texture source) => new() { Texture = source, Centered = false, Scale = new(16, 16), TextureFilter = TextureFilter.Nearest };

    private static float Encode(float linear) =>
        Math.Clamp(linear <= .0031308f ? 12.92f * linear : 1.055f * MathF.Pow(linear, 1 / 2.4f) - .055f, 0, 1);

    private static void Verify(Image actual, Color[] expected, string label)
    {
        Check(actual.PixelFormat == Image.Format.Rgbah, $"{label} is linear half-float.");
        for (var i = 0; i < expected.Length; i++)
        {
            var read = actual.GetPixel(8 + 16 * i, 8);
            var reference = expected[i];
            Check(MathF.Abs(read.R - reference.R) <= .005f && MathF.Abs(read.G - reference.G) <= .005f &&
                MathF.Abs(read.B - reference.B) <= .005f && MathF.Abs(read.A - reference.A) <= .001f,
                $"{label} sample {i} expected {reference}, read {read}.");
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
