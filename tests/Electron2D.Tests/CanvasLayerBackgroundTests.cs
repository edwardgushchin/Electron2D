using Electron2D;

internal static class CanvasLayerBackgroundTests
{
    private static readonly Vector2i Size = new(96, 64);
    private static readonly Color BlueOverlay = new(0, 0, 1, .5f);

    internal static void Run(string profile)
    {
        if (profile == "managed") Managed();
        else Native(profile == "compatibility");
    }

    private static void Managed()
    {
        using var environment = new CanvasEnvironment { ResourceLocalToScene = true };
        Check(environment.BackgroundCanvasMaxLayer == 0, "The initial canvas background limit is zero.");
        environment.BackgroundMode = CanvasBackgroundMode.Canvas;
        environment.BackgroundCanvasMaxLayer = int.MinValue;
        using var world = new WorldEnvironment { Environment = environment };
        using var packed = new PackedScene();
        packed.Pack(world);
        var file = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"e2d-layer-background-{Guid.NewGuid():N}.e2dscene");
        try
        {
            ResourceSaver.Save(packed, file);
            using var loaded = ResourceLoader.Load<PackedScene>(file, ResourceLoader.CacheMode.IgnoreDeep);
            using var instance = (WorldEnvironment)loaded.Instantiate();
            using var copy = instance.Environment!;
            Check(copy.BackgroundMode == CanvasBackgroundMode.Canvas && copy.BackgroundCanvasMaxLayer == int.MinValue &&
                !ReferenceEquals(copy, environment), "Typed scene retains the mode and signed inclusive limit.");
        }
        finally { File.Delete(file); }
        Console.WriteLine("Layer background managed: defaults and typed scene round trip passed.");
    }

    private static void Native(bool compatibility)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, compatibility ? "compatibility" : "gpu");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var environment = new CanvasEnvironment { BackgroundMode = CanvasBackgroundMode.Canvas, TonemapExposure = .25f };
        using var shader = compatibility ? null : LoadScreenShader();
        using var material = shader is null ? null : new ShaderMaterial { Shader = shader };
        material?.SetShaderParameter("tint", Colors.White);
        material?.SetShaderParameter("offset", new Vector2(-64, 0));

        using var root = new Window { Title = "Layered canvas background", Size = Size, UseHDR2D = !compatibility };
        var child = new SubViewport { Size = Size, RenderTargetUpdateMode = ViewportUpdateMode.Always };
        if (!compatibility) child.MSAA2D = Viewport.MSAA.Samples4X;
        var rootLayers = BuildLayers(root, environment, material);
        root.AddChild(child);
        var childLayers = BuildLayers(child, environment, material);
        using var rootTexture = root.GetTexture();
        using var childTexture = child.GetTexture();

        var finished = false;
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            var gain = compatibility ? 1f : .25f;
            RenderingServer.ForceDraw();
            Expect(rootTexture, 8, 16, new(gain, 0, 0, 1), "Lower layer uses the effect");
            Expect(childTexture, 8, 48, new(0, gain, 0, 1), "The cutoff includes its own layer");
            Expect(rootTexture, 32, 16, Blend(gain, !compatibility), "Upper layer blends over processed color");
            Expect(childTexture, 32, 16, Blend(gain, false), "RGBA8 foreground blends over the resolved background");
            if (!compatibility)
            {
                Expect(rootTexture, 72, 16, new(gain, 0, 0, 1), "Foreground screen read refreshes the lower layer copy");
                Expect(childTexture, 72, 16, new(gain, 0, 0, 1), "Foreground screen read uses the resolved MSAA background");
            }

            child.RenderTargetClearMode = ViewportClearMode.Never;
            childLayers.Lower.Visible = false;
            childLayers.Equal.Visible = false;
            for (var i = 0; i < 48; i++) RenderingServer.ForceDraw();
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++) RenderingServer.ForceDraw();
            var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            Check(bytes == 0, $"Prepared layered frames allocated {bytes} owner bytes.");
            Expect(childTexture, 8, 16, new(gain, 0, 0, 1), "Never-clear retains unprocessed background");
            Expect(childTexture, 32, 16, Blend(gain, false), "Repeated foreground blending does not accumulate");
            environment.TonemapExposure = .5f;
            gain = compatibility ? 1f : .5f;
            RenderingServer.ForceDraw();
            Expect(childTexture, 8, 16, new(gain, 0, 0, 1), "A live exposure edit reuses the original background");
            Expect(childTexture, 32, 16, Blend(gain, false), "Foreground starts from the new effect result");

            childLayers.Upper.Visible = false;
            RenderingServer.ForceDraw();
            Expect(childTexture, 32, 16, new(gain, 0, 0, 1), "Removed foreground leaves no history trail");
            child.RenderTargetClearMode = ViewportClearMode.Once;
            RenderingServer.ForceDraw();
            Expect(childTexture, 8, 16, Colors.Black, "Once clears retained background");
            Check(child.RenderTargetClearMode == ViewportClearMode.Never, "Once transitions after rendering.");

            rootLayers.ReadBox?.Hide();
            var rid = environment.GetRID();
            RenderingServer.EnvironmentSetCanvasMaxLayer(rid, int.MaxValue);
            RenderingServer.ForceDraw();
            if (!compatibility) Expect(rootTexture, 32, 16, new(.25f, 0, .25f, 1), "Moving the boundary includes the upper layer");
            Check(environment.BackgroundCanvasMaxLayer == int.MaxValue, "The live server operation updates authoring.");
            RenderingServer.EnvironmentSetCanvasMaxLayer(rid, int.MinValue);
            RenderingServer.ForceDraw();
            Expect(rootTexture, 8, 16, Colors.Red, "All visible layers can be above the boundary");
            Expect(rootTexture, 32, 16, Blend(1f, !compatibility), "Upper layers retain their ordinary blend order");
            RenderingServer.ViewportSetEnvironmentMode(root.GetViewportRID(), RenderingServer.ViewportEnvironmentMode.Disabled);
            RenderingServer.ForceDraw();
            Expect(rootTexture, 8, 16, Colors.Red, "Environment bypass uses ordinary canvas rendering");

            finished = true;
            root.Tree!.Quit();
            Console.WriteLine($"Layer background {(compatibility ? "Compatibility" : "GPU")}: inclusive layers, blend, screen reads where supported, history, live boundary, and 64 warmed frames ({bytes} owner bytes).");
        };
        Check(Engine.Run(root) == 0 && finished, "Layer background native scenario completed.");
    }

    private static (ColorRect Lower, ColorRect Equal, ColorRect Upper, ColorRect? ReadBox) BuildLayers(Viewport view, CanvasEnvironment environment, ShaderMaterial? reader)
    {
        view.AddChild(new WorldEnvironment { Environment = environment });
        var lower = new CanvasLayer { Layer = -1 };
        var lowerColor = new ColorRect { Size = Size, Color = Colors.Red };
        lower.AddChild(lowerColor);
        if (reader is not null) lower.AddChild(new BackBufferCopy { CopyMode = BackBufferCopyMode.Viewport });
        view.AddChild(lower);

        var equal = new CanvasLayer { Layer = 0 };
        var equalColor = new ColorRect { Position = new(0, 40), Size = new(24, 24), Color = Colors.Lime };
        equal.AddChild(equalColor);
        view.AddChild(equal);

        var upper = new CanvasLayer { Layer = 1 };
        var upperColor = new ColorRect { Position = new(24, 0), Size = new(24, 32), Color = BlueOverlay };
        upper.AddChild(upperColor);
        ColorRect? readBox = null;
        if (reader is not null)
        {
            readBox = new ColorRect { Position = new(64, 0), Size = new(16, 24), Color = Colors.White, Material = reader };
            upper.AddChild(readBox);
        }
        view.AddChild(upper);
        return (lowerColor, equalColor, upperColor, readBox);
    }

    private static Shader LoadScreenShader()
    {
        using var input = typeof(CanvasLayerBackgroundTests).Assembly.GetManifestResourceStream("TestShaders.ScreenHlsl.spv")!;
        using var memory = new MemoryStream();
        input.CopyTo(memory);
        return Shader.CreateFromSPIRV(memory.ToArray());
    }

    private static Color Blend(float gain, bool hdr) =>
        hdr ? new(gain * .5f, 0, .5f, 1) : new Color(new Color(gain, 0, 0, 1).LinearToSRGB().R * .5f, 0, .5f, 1).SRGBToLinear();

    private static void Expect(ViewportTexture texture, int x, int y, Color linear, string label)
    {
        using var image = texture.GetImage()!;
        var expected = image.PixelFormat == Image.Format.Rgbah ? linear : linear.LinearToSRGB();
        var actual = image.GetPixel(x, y);
        const float epsilon = .014f;
        Check(MathF.Abs(actual.R - expected.R) < epsilon && MathF.Abs(actual.G - expected.G) < epsilon &&
            MathF.Abs(actual.B - expected.B) < epsilon && MathF.Abs(actual.A - expected.A) < epsilon,
            $"{label}: expected {expected}, read {actual}.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
