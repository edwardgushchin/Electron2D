using Electron2D;

internal static class CanvasAdjustmentTests
{
    private static readonly Color[] Source = [new(.125f, .5f, 2), new(.25f, .125f, .375f), new(0, 0, 0, .5f)];
    private static readonly Color[] Ramp = [new(1, .2f, .8f, 0), new(.4f, 1, .2f, .2f)];
    private const float Brightness = .75f, Contrast = 1.3f, Saturation = .6f;

    internal static void Run(string profile)
    {
        if (profile == "managed") Managed();
        else Native(profile == "compatibility");
    }

    private static void Managed()
    {
        using var environment = new CanvasEnvironment();
        Check(!environment.AdjustmentEnabled && environment.AdjustmentBrightness == 1 && environment.AdjustmentContrast == 1 &&
            environment.AdjustmentSaturation == 1 && environment.AdjustmentColorCorrection is null, "Color adjustment defaults.");
        Reject<ArgumentOutOfRangeException>(() => environment.AdjustmentBrightness = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => environment.AdjustmentContrast = float.PositiveInfinity);
        Reject<ArgumentOutOfRangeException>(() => environment.AdjustmentSaturation = float.NegativeInfinity);
        Reject<InvalidOperationException>(() => RenderingServer.EnvironmentSetAdjustment(environment.GetRID(), true, 1, 1, 1));
        using var disposed = new ImageTexture();
        disposed.Dispose();
        Reject<ObjectDisposedException>(() => environment.AdjustmentColorCorrection = disposed);

        using var gradient = new Gradient { Colors = Ramp };
        using var lookup = new GradientRampTexture { Gradient = gradient, Width = 2 };
        environment.AdjustmentEnabled = true;
        environment.AdjustmentBrightness = Brightness;
        environment.AdjustmentContrast = Contrast;
        environment.AdjustmentSaturation = Saturation;
        environment.AdjustmentColorCorrection = lookup;
        environment.GlowMap = lookup;
        using var shallow = (CanvasEnvironment)environment.Duplicate();
        using var deep = (CanvasEnvironment)environment.DuplicateDeep();
        using var deepRamp = (GradientRampTexture)deep.AdjustmentColorCorrection!;
        using var deepGradient = deepRamp.Gradient!;
        Check(ReferenceEquals(shallow.AdjustmentColorCorrection, lookup) && !ReferenceEquals(deepRamp, lookup) &&
            !ReferenceEquals(deepGradient, gradient) && ReferenceEquals(deep.GlowMap, deepRamp), "Copies preserve lookup ownership and graph aliases.");
        environment.ResourceLocalToScene = true;
        using var world = new WorldEnvironment { Environment = environment };
        using var packed = new PackedScene();
        packed.Pack(world);
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-adjustment-" + Guid.NewGuid().ToString("N") + ".e2dscene");
        try
        {
            ResourceSaver.Save(packed, path);
            using var saved = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep);
            using var restored = (WorldEnvironment)saved.Instantiate();
            using var copy = restored.Environment!;
            using var ramp = (GradientRampTexture)copy.AdjustmentColorCorrection!;
            using var savedGradient = ramp.Gradient!;
            Check(copy.AdjustmentEnabled && copy.AdjustmentBrightness == Brightness && copy.AdjustmentContrast == Contrast &&
                copy.AdjustmentSaturation == Saturation && ramp.Width == 2 && savedGradient.Colors.SequenceEqual(Ramp) &&
                ReferenceEquals(copy.GlowMap, ramp), "Typed scene round trip restores adjustments and shared lookup data.");
        }
        finally { File.Delete(path); }
        Console.WriteLine("Canvas adjustment managed: defaults, validation, resource ownership and typed scene round trip passed.");
    }

    private static void Native(bool compatibility)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, compatibility ? "compatibility" : "gpu");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var sourceImage = Image.CreateEmpty(Source.Length, 1, false, compatibility ? Image.Format.Rgba8 : Image.Format.Rgbaf);
        for (var x = 0; x < Source.Length; x++) sourceImage.SetPixel(x, 0, Source[x]);
        using var source = ImageTexture.CreateFromImage(sourceImage);
        using var gradient = new Gradient { Colors = Ramp };
        using var lookup = new GradientRampTexture { Gradient = gradient, Width = 2 };
        using var table = Image.CreateEmpty(4, 2, false, Image.Format.Rgba8);
        table.Fill(Colors.Cyan);
        for (var x = 0; x < Ramp.Length; x++) table.SetPixel(x + 1, 0, Ramp[x]);
        using var atlasImage = ImageTexture.CreateFromImage(table);
        using var atlas = new AtlasTexture { Atlas = atlasImage, Region = new(1, 0, 2, 2) };
        using var empty = new GradientRampTexture();
        using var environment = new CanvasEnvironment();
        using var root = new Window { Size = new(96, 64), Title = "Canvas color adjustment check", UseHDR2D = !compatibility, TransparentBG = true };
        var child = new SubViewport
        {
            Size = root.Size,
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
        using var target = root.GetTexture();
        using var childTarget = child.GetTexture();
        var finished = false;
        root.Ready += _ =>
        {
            RenderingServer.ForceDraw();
            using var baseline = target.GetImage()!;
            var rid = environment.GetRID();
            RenderingServer.EnvironmentSetAdjustment(rid, true, Brightness, Contrast, Saturation, lookup.GetRID());
            RenderingServer.ForceDraw();
            if (compatibility)
            {
                using var sdr = target.GetImage()!;
                Check(sdr.GetData().AsSpan().SequenceEqual(baseline.GetData()), "Compatibility retains ordinary SDR pixels.");
            }
            else
            {
                var alias = RenderingServer.TextureGetRDTexture(target.GetRID());
                Check(alias.IsValid(), "The HDR canvas exports a device identity.");
                Verify(target, c => Expected(c, Ramp), "Gradient correction");
                Verify(childTarget, c => Expected(c, Ramp), "4x MSAA correction");
                Reject<ArgumentOutOfRangeException>(() => RenderingServer.EnvironmentSetAdjustment(rid, false, 0, 0, float.NaN));
                Reject<ArgumentException>(() => RenderingServer.EnvironmentSetAdjustment(rid, false, 0, 0, 0, root.GetViewportRID()));
                Reject<ArgumentException>(() => RenderingServer.EnvironmentSetAdjustment(root.GetViewportRID(), true, 1, 1, 1));
                Reject<InvalidOperationException>(() => Task.Run(() => RenderingServer.EnvironmentSetAdjustment(rid, true, 1, 1, 1)).GetAwaiter().GetResult());
                Check(environment.AdjustmentEnabled && environment.AdjustmentSaturation == Saturation &&
                    ReferenceEquals(environment.AdjustmentColorCorrection, lookup), "Rejected server writes retain the previous settings.");

                environment.AdjustmentColorCorrection = atlas;
                environment.TonemapExposure = 2;
                environment.GlowEnabled = true;
                environment.GlowBlend = CanvasEnvironment.GlowBlendMode.Mix;
                environment.GlowMix = .25f;
                environment.GlowStrength = 0;
                RenderingServer.ForceDraw();
                Verify(target, c => Expected(c, Ramp, 1.5), "Tone exposure and glow mix precede atlas correction");
                environment.GlowEnabled = false;
                environment.TonemapExposure = 1;

                using (var producer = new SubViewport { Size = new(2, 2) })
                {
                    producer.AddChild(new WorldEnvironment { Environment = environment });
                    using var rampPixels = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8);
                    for (var i = 0; i < Ramp.Length; i++) rampPixels.SetPixel(i, 0, new(Ramp[i].R, Ramp[i].G, Ramp[i].B, 1));
                    using var rampSource = ImageTexture.CreateFromImage(rampPixels);
                    producer.AddChild(new Sprite { Texture = rampSource, Centered = false, TextureFilter = TextureFilter.Nearest });
                    root.AddChild(producer);
                    using var produced = producer.GetTexture();
                    environment.AdjustmentColorCorrection = produced;
                    RenderingServer.ForceDraw();
                    Verify(target, c => Expected(c, Ramp), "Sampled SDR viewport renders its current pixels before correction");
                    using var producerImage = produced.GetImage()!;
                    Check(producerImage.PixelFormat == Image.Format.Rgba8 && MathF.Abs(producerImage.GetPixel(0, 0).R - 1) < .006f,
                        "The GPU byte viewport uses ordinary SDR output with the environment assigned.");
                }
                RenderingServer.ForceDraw();
                Verify(target, c => Expected(c, null), "Retired viewport lookup keeps scalar adjustments");

                child.RenderTargetClearMode = ViewportClearMode.Never;
                childSprite.Visible = false;
                environment.AdjustmentColorCorrection = empty;
                RenderingServer.ForceDraw();
                Verify(childTarget, c => Expected(c, null), "Empty lookup keeps scalar adjustments on raw history");
                RenderingServer.ForceDraw();
                Verify(childTarget, c => Expected(c, null), "Repeated adjustment does not accumulate");
                RenderingServer.ViewportSetEnvironmentMode(root.GetViewportRID(), RenderingServer.ViewportEnvironmentMode.Disabled);
                RenderingServer.ForceDraw();
                Verify(childTarget, c => c, "Inherited bypass restores unprocessed pixels");
                RenderingServer.ViewportSetEnvironmentMode(root.GetViewportRID(), RenderingServer.ViewportEnvironmentMode.Inherit);

                RenderingServer.EnvironmentSetAdjustment(rid, false, Brightness, Contrast, Saturation, atlas.GetRID());
                RenderingServer.ForceDraw();
                Verify(target, c => c, "Disabled adjustments retain ordinary HDR pixels");
                RenderingServer.EnvironmentSetAdjustment(rid, true, float.MaxValue, -float.MaxValue, float.MaxValue);
                RenderingServer.ForceDraw();
                using (var extreme = target.GetImage()!) Check(extreme.GetPixel(8, 8).IsFinite(), "Extreme finite controls produce finite output.");

                RenderingServer.EnvironmentSetAdjustment(rid, true, Brightness, Contrast, Saturation, lookup.GetRID());
                Check(RenderingServer.TextureGetRDTexture(target.GetRID()) == alias, "Adjustments preserve the native viewport texture identity.");
                for (var i = 0; i < 48; i++) RenderingServer.ForceDraw();
                var before = GC.GetAllocatedBytesForCurrentThread();
                for (var i = 0; i < 64; i++) RenderingServer.ForceDraw();
                var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
                Check(bytes == 0, $"Prepared adjustment frames allocated {bytes} owner bytes.");
                Console.WriteLine($"Canvas adjustment GPU: scalar and gradient references, atlas and viewport lookup, glow order, alpha, MSAA, raw history, server validation and 64 warmed frames ({bytes} owner bytes).");
            }

            finished = true;
            root.Tree!.Quit();
        };
        Check(Engine.Run(root) == 0 && finished, "The native adjustment scenario completed.");
        if (compatibility) Console.WriteLine("Canvas adjustment Compatibility: ordinary SDR output and lifecycle passed.");
    }

    private static Sprite MakeSprite(Texture texture) => new() { Texture = texture, Centered = false, Scale = new(16, 16), TextureFilter = TextureFilter.Nearest };

    private static Color Expected(Color source, Color[]? ramp, double gain = 1)
    {
        double ContrastChannel(float value) => (Encode(value * gain * Brightness) - .5) * Contrast + .5;
        var red = ContrastChannel(source.R);
        var green = ContrastChannel(source.G);
        var blue = ContrastChannel(source.B);
        var gray = (red + green + blue) / 3;
        double Adjust(double value) => gray + (value - gray) * Saturation;
        float Sample(double value, float first, float last)
        {
            var weight = Math.Clamp(2 * value - .5, 0, 1);
            return (float)((1 - weight) * Decode(first) + weight * Decode(last));
        }
        return ramp is null
            ? new((float)Decode(Adjust(red)), (float)Decode(Adjust(green)), (float)Decode(Adjust(blue)), source.A)
            : new(Sample(Adjust(red), ramp[0].R, ramp[1].R), Sample(Adjust(green), ramp[0].G, ramp[1].G),
                Sample(Adjust(blue), ramp[0].B, ramp[1].B), source.A);
    }

    private static double Encode(double value) => value <= .0031308 ? 12.92 * value : 1.055 * Math.Pow(value, 1 / 2.4) - .055;
    private static double Decode(double value) => value <= .04045 ? value / 12.92 : Math.Pow((value + .055) / 1.055, 2.4);

    private static void Verify(ViewportTexture texture, Func<Color, Color> expected, string label)
    {
        using var image = texture.GetImage()!;
        Check(image.PixelFormat == Image.Format.Rgbah, $"{label} uses half-float storage.");
        for (var x = 0; x < Source.Length; x++)
        {
            var actual = image.GetPixel(8 + 16 * x, 8);
            var reference = expected(Source[x]);
            Check(MathF.Abs(actual.R - reference.R) < .006f && MathF.Abs(actual.G - reference.G) < .006f &&
                MathF.Abs(actual.B - reference.B) < .006f && MathF.Abs(actual.A - reference.A) < .001f,
                $"{label} at sample {x} expected {reference}, read {actual}.");
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
