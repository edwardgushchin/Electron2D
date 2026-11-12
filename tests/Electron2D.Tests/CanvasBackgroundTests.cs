using Electron2D;

internal static class CanvasBackgroundTests
{
    private static readonly Color DefaultColor = new(.125f, .25f, .5f, 1);
    private static readonly Color AuthoredColor = new(.5f, .25f, .125f, .3f);
    private static readonly Vector2i Size = new(96, 64);

    internal static void Run(string profile)
    {
        if (profile == "managed") Managed();
        else Native(profile == "compatibility");
    }

    private static void Managed()
    {
        using var environment = new CanvasEnvironment { ResourceLocalToScene = true };
        Check(environment.BackgroundMode == CanvasBackgroundMode.ClearColor && environment.BackgroundColor == Colors.Black &&
            environment.BackgroundEnergyMultiplier == 1, "Background defaults.");
        Reject<ArgumentOutOfRangeException>(() => environment.BackgroundMode = CanvasBackgroundMode.Max);
        Reject<ArgumentOutOfRangeException>(() => environment.BackgroundMode = (CanvasBackgroundMode)3);
        Reject<ArgumentOutOfRangeException>(() => environment.BackgroundColor = new(float.NaN, 0, 0, 1));
        Reject<ArgumentOutOfRangeException>(() => environment.BackgroundEnergyMultiplier = float.PositiveInfinity);
        Reject<ArgumentOutOfRangeException>(() => environment.BackgroundEnergyMultiplier = -1);
        Reject<InvalidOperationException>(() => RenderingServer.EnvironmentSetBackground(environment.GetRID(), CanvasBackgroundMode.Keep));

        environment.BackgroundMode = CanvasBackgroundMode.Color;
        environment.BackgroundColor = AuthoredColor;
        environment.BackgroundEnergyMultiplier = 2;
        using var world = new WorldEnvironment { Environment = environment };
        using var packed = new PackedScene();
        packed.Pack(world);
        var file = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"e2d-canvas-background-{Guid.NewGuid():N}.e2dscene");
        try
        {
            ResourceSaver.Save(packed, file);
            using var loaded = ResourceLoader.Load<PackedScene>(file, ResourceLoader.CacheMode.IgnoreDeep);
            using var instance = (WorldEnvironment)loaded.Instantiate();
            using var copy = instance.Environment!;
            Check(!ReferenceEquals(copy, environment) && copy.BackgroundMode == CanvasBackgroundMode.Color &&
                copy.BackgroundColor == AuthoredColor && copy.BackgroundEnergyMultiplier == 2, "Typed scene round trip keeps authored background.");
        }
        finally { File.Delete(file); }
        Console.WriteLine("Canvas background managed: defaults, guards and typed scene round trip passed.");
    }

    private static void Native(bool compatibility)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, compatibility ? "compatibility" : "gpu");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var environment = new CanvasEnvironment();
        using var root = new Window { Size = Size, Title = "Canvas environment background", UseHDR2D = !compatibility };
        var child = new SubViewport { Size = Size, RenderTargetUpdateMode = ViewportUpdateMode.Always };
        if (!compatibility) child.MSAA2D = Viewport.MSAA.Samples4X;
        var shape = new ColorRect { Size = new(24, 24), Color = Colors.Green };
        var childShape = new ColorRect { Size = shape.Size, Color = shape.Color };
        root.AddChild(new WorldEnvironment { Environment = environment });
        root.AddChild(shape);
        root.AddChild(child);
        child.AddChild(new WorldEnvironment { Environment = environment });
        child.AddChild(childShape);
        using var rootTexture = root.GetTexture();
        using var childTexture = child.GetTexture();
        var finished = false;
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(DefaultColor);
            RenderingServer.ForceDraw();
            Expect(rootTexture, 60, DefaultColor, 1, "Session clear color");
            Expect(childTexture, 60, DefaultColor, 1, "Byte clear color");

            var rid = environment.GetRID();
            environment.BackgroundEnergyMultiplier = .5f;
            RenderingServer.ForceDraw();
            Expect(childTexture, 60, DefaultColor, .5, "Energy also affects the selected session clear");

            RenderingServer.EnvironmentSetBackground(rid, CanvasBackgroundMode.Color);
            RenderingServer.EnvironmentSetBGColor(rid, AuthoredColor);
            RenderingServer.EnvironmentSetBGEnergy(rid, 2, .25f);
            RenderingServer.ForceDraw();
            Expect(rootTexture, 60, AuthoredColor, .5, "Authored color with server energy and exposure");
            Expect(childTexture, 60, AuthoredColor, .5, "Authored byte color with linear energy");
            Expect(childTexture, 8, Colors.Green, 1, "Energy leaves canvas draws independent");
            Reject<ArgumentOutOfRangeException>(() => RenderingServer.EnvironmentSetBGEnergy(rid, 5, float.NaN));
            Reject<ArgumentOutOfRangeException>(() => RenderingServer.EnvironmentSetBackground(rid, CanvasBackgroundMode.Max));
            Reject<ArgumentException>(() => RenderingServer.EnvironmentSetBGColor(root.GetViewportRID(), DefaultColor));
            using (var retired = new CanvasEnvironment())
            {
                var retiredRID = retired.GetRID();
                retired.Dispose();
                Reject<ArgumentException>(() => RenderingServer.EnvironmentSetBackground(retiredRID, CanvasBackgroundMode.Keep));
            }
            Reject<InvalidOperationException>(() => Task.Run(() => RenderingServer.EnvironmentSetBGColor(rid, DefaultColor)).GetAwaiter().GetResult());
            Check(environment.BackgroundEnergyMultiplier == 2 && environment.BackgroundMode == CanvasBackgroundMode.Color,
                "Failed updates preserve authoring.");

            environment.BackgroundEnergyMultiplier = 2;
            RenderingServer.ForceDraw();
            Expect(childTexture, 60, AuthoredColor, 2, "Writing the authored multiplier restores unit exposure");
            environment.BackgroundEnergyMultiplier = 1;

            if (!compatibility) environment.TonemapExposure = .5f;
            RenderingServer.ForceDraw();
            var gain = compatibility ? 1 : .5;
            Expect(rootTexture, 60, AuthoredColor, gain, "Post-processing consumes the selected background");
            var retainedRoot = ReadBytes(rootTexture);
            var retainedChild = ReadBytes(childTexture);
            environment.BackgroundMode = CanvasBackgroundMode.Keep;
            environment.BackgroundColor = Colors.Magenta;
            shape.Visible = childShape.Visible = false;
            for (var i = 0; i < 48; i++) RenderingServer.ForceDraw();
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++) RenderingServer.ForceDraw();
            var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            Check(bytes == 0, $"Prepared background frames allocated {bytes} owner bytes.");
            Check(ReadBytes(rootTexture).AsSpan().SequenceEqual(retainedRoot), "Keep retains HDR or compatibility history.");
            Check(ReadBytes(childTexture).AsSpan().SequenceEqual(retainedChild), "Keep preserves unprocessed byte or MSAA history.");

            child.RenderTargetClearMode = ViewportClearMode.Once;
            RenderingServer.ForceDraw();
            Check(child.RenderTargetClearMode == ViewportClearMode.Never, "Once is consumed after successful submission.");
            Expect(childTexture, 8, DefaultColor, gain, "An explicit Once clears Keep to the session default");
            environment.BackgroundMode = CanvasBackgroundMode.Color;
            RenderingServer.ForceDraw();
            Expect(childTexture, 8, DefaultColor, gain, "Never retains even when an authored color is selected");

            child.RenderTargetClearMode = ViewportClearMode.Always;
            child.TransparentBG = true;
            RenderingServer.ForceDraw();
            ExpectPixel(childTexture, 60, default, "Transparent viewport clear takes priority");
            child.TransparentBG = false;
            var rootRID = root.GetViewportRID();
            RenderingServer.ViewportSetEnvironmentMode(rootRID, RenderingServer.ViewportEnvironmentMode.Disabled);
            RenderingServer.ForceDraw();
            Expect(rootTexture, 60, DefaultColor, 1, "Environment bypass restores session clear");
            Expect(childTexture, 60, DefaultColor, 1, "Child inherits the environment bypass");
            RenderingServer.ViewportSetEnvironmentMode(child.GetViewportRID(), RenderingServer.ViewportEnvironmentMode.Enabled);
            RenderingServer.ForceDraw();
            Expect(childTexture, 60, Colors.Magenta, gain, "Explicit child selection overrides parent bypass");

            RenderingServer.ViewportSetEnvironmentMode(rootRID, RenderingServer.ViewportEnvironmentMode.Enabled);
            environment.TonemapExposure = 1;
            environment.BackgroundColor = new(float.MaxValue, 0, 0, 1);
            environment.BackgroundEnergyMultiplier = float.MaxValue;
            RenderingServer.ForceDraw();
            ExpectPixel(rootTexture, 60, new(compatibility ? 1 : 65504, 0, 0, 1), "Extreme finite energy saturates storage");
            environment.BackgroundEnergyMultiplier = 0;
            RenderingServer.ForceDraw();
            Expect(rootTexture, 60, Colors.Black, 1, "Zero energy produces opaque black");

            finished = true;
            root.Tree!.Quit();
            Console.WriteLine($"Canvas background {(compatibility ? "Compatibility" : "GPU")}: clear, custom color, linear energy, retention, transparent clear, inherited bypass and 64 warmed frames ({bytes} owner bytes).");
        };
        Check(Engine.Run(root) == 0 && finished, "Native background scenario completed.");
    }

    private static byte[] ReadBytes(ViewportTexture texture)
    {
        using var image = texture.GetImage()!;
        return image.GetData();
    }

    private static void Expect(ViewportTexture texture, int x, Color authored, double gain, string label)
    {
        using var image = texture.GetImage()!;
        var hdr = image.PixelFormat == Image.Format.Rgbah;
        float Channel(float value)
        {
            var linear = (value <= .04045 ? value / 12.92 : Math.Pow((value + .055) / 1.055, 2.4)) * gain;
            return (float)(hdr ? linear : linear <= .0031308 ? linear * 12.92 : Math.Clamp(1.055 * Math.Pow(linear, 1 / 2.4) - .055, 0, 1));
        }
        CheckPixel(image.GetPixel(x, 16), new(Channel(authored.R), Channel(authored.G), Channel(authored.B), 1), label);
    }

    private static void ExpectPixel(ViewportTexture texture, int x, Color expected, string label)
    {
        using var image = texture.GetImage()!;
        CheckPixel(image.GetPixel(x, 16), expected, label);
    }

    private static void CheckPixel(Color actual, Color expected, string label)
    {
        const float epsilon = .008f;
        Check(MathF.Abs(actual.R - expected.R) < epsilon && MathF.Abs(actual.G - expected.G) < epsilon &&
            MathF.Abs(actual.B - expected.B) < epsilon && MathF.Abs(actual.A - expected.A) < epsilon, $"{label}: expected {expected}, read {actual}.");
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
