using Electron2D;

internal static class LineGeometryRenderingTests
{
    internal static void Run(string backend)
    {
        var oldMethod = ProjectSettings.Get(ProjectSettings.RenderingMethod); var oldFallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        try
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
            using var shader = Environment.GetEnvironmentVariable("ELECTRON2D_LINE_GEOMETRY_SHADER") is { } name ? LoadShader(name) : null;
            using var material = shader is null ? null : new ShaderMaterial { Shader = shader };
            using var root = new Window { Size = new(256, 192), Borderless = true, CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
            var lines = new List<Line>();
            for (var cap = 0; cap < 3; cap++)
            {
                var line = new Line { Points = [Vector2.Zero, new(40, 0)], Position = new(20.25f, 30.25f + cap * 35), Width = 10, DefaultColor = new(1, 0, 0, .5f), BeginCapMode = (LineCapMode)cap, EndCapMode = (LineCapMode)cap, Material = material };
                lines.Add(line); root.AddChild(line);
            }
            var corner = new Line { Points = [new(110.25f, 30.25f), new(150.25f, 30.25f), new(150.25f, 70.25f)], Width = 10, DefaultColor = new(0, 1, 0, .5f), JointMode = LineJointMode.Round, Material = material }; root.AddChild(corner);
            var reflected = new Line { Points = [Vector2.Zero, new(40, 0)], Position = new(220.25f, 110.25f), Scale = new(-1, 2), Width = 10, DefaultColor = new(1, 0, 0, .5f), Material = material }; root.AddChild(reflected);
            using var pixels = Image.CreateFromData(2, 1, false, Image.Format.Rgba8, [255, 0, 0, 255, 0, 255, 0, 128]); using var texture = ImageTexture.CreateFromImage(pixels);
            using var atlas = new AtlasTexture { Atlas = texture, Region = new(1, 0, 1, 1) };
            var textured = new Line { Points = [Vector2.Zero, new(40, 0)], Position = new(20.25f, 150.25f), Width = 10, Texture = atlas, TextureMode = LineTextureMode.Stretch, BeginCapMode = LineCapMode.Round, EndCapMode = LineCapMode.Round, Material = material }; root.AddChild(textured);
            using var identityShader = backend == "gpu" ? LoadIdentityShader() : null;
            using var identityMaterial = identityShader is null ? null : new ShaderMaterial { Shader = identityShader };
            if (identityMaterial is not null) root.AddChild(new Line { Points = [Vector2.Zero, new(40, 0)], Position = new(110.25f, 150.25f), Width = 8, Material = identityMaterial });
            root.Ready += _ =>
            {
                RenderingServer.RenderLoopEnabled = false; RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.ForceDraw();
                using var sharp = root.GetTexture().GetImage()!;
                foreach (var line in lines) line.Antialiased = true; corner.Antialiased = reflected.Antialiased = textured.Antialiased = true;
                RenderingServer.ForceDraw(); using var smooth = root.GetTexture().GetImage()!;
                if (backend == "gpu") { Near(smooth.GetPixel(140, 150).B, .75625f, "Authored vertex programs receive source vertex IDs across the second body triangle"); Near(smooth.GetPixel(120, 150).B, .25625f, "Source vertex IDs remain shared across the first body triangle"); }
                for (var i = 0; i < 3; i++)
                {
                    Near(sharp.GetPixel(40, 24 + i * 35).R, 0, "Disabled straight-side smoothing");
                    Near(smooth.GetPixel(40, 24 + i * 35).R, .125f, "The full line side has analytic framebuffer coverage");
                    Near(smooth.GetPixel(40, 30 + i * 35).R, .5f, "The translucent body keeps its energy");
                }
                Check(smooth.GetPixel(14, 100).R is > .01f and < .3f && smooth.GetPixel(65, 100).R is > .01f and < .4f, "Both rounded endpoints receive fractional outside coverage.");
                Check(smooth.GetPixel(154, 26).G is > .01f and < .4f, "The outer round joint receives boundary coverage.");
                Near(smooth.GetPixel(200, 99).R, .125f, "Reflection and nonuniform scale retain one-pixel smoothing");
                Near(smooth.GetPixel(40, 144).G, 32f / 255, "Atlas texture alpha multiplies the side fringe");
                for (var i = 0; i < 3; i++) for (var x = 18; x < 63; x++) for (var y = 26 + i * 35; y < 34 + i * 35; y++)
                            if (MathF.Abs(sharp.GetPixel(x, y).R - .5f) < .01f) Near(smooth.GetPixel(x, y).R, .5f, "Interior diagonals and cap/body seams never receive extra feather overdraw");
                using var premultiplied = new CanvasItemMaterial { BlendMode = BlendMode.PremultAlpha }; lines[0].Material = premultiplied; lines[0].DefaultColor = new(.5f, 0, 0, .5f); RenderingServer.ForceDraw();
                using (var image = root.GetTexture().GetImage()!) { Near(image.GetPixel(40, 24).R, .125f, "Premultiplied alpha fades fringe RGB"); Near(image.GetPixel(40, 30).R, .5f, "Premultiplied interior remains unchanged"); }
                using var multiply = new CanvasItemMaterial { BlendMode = BlendMode.Mul }; lines[0].Material = multiply; Reject<NotSupportedException>(() => RenderingServer.ForceDraw());
                lines[0].Material = material; lines[0].DefaultColor = new(1, 0, 0, .5f); RenderingServer.ForceDraw();
                root.SnapVerticesToPixel = true; RenderingServer.ForceDraw(); using (var image = root.GetTexture().GetImage()!) Near(image.GetPixel(40, 24).R, .25f, "Pixel snapping precedes fringe construction"); root.SnapVerticesToPixel = false;
                if (backend == "gpu")
                {
                    root.UseHDR2D = true; RenderingServer.ForceDraw(); using (var image = root.GetTexture().GetImage()!) Near(image.GetPixel(40, 24).R, .125f, "HDR preserves coverage alpha");
                    root.UseHDR2D = false; root.MSAA2D = Viewport.MSAA.Samples4X; RenderingServer.ForceDraw(); using (var image = root.GetTexture().GetImage()!) Check(image.GetPixel(40, 24).R is > .01f and < .4f, "MSAA composes the shared fringe geometry."); root.MSAA2D = Viewport.MSAA.Disabled;
                }
                for (var i = 0; i < 32; i++) { lines[2].Antialiased = (i & 1) == 0; corner.RoundPrecision = (i & 1) == 0 ? 8 : 16; RenderingServer.ForceDraw(); }
                var before = GC.GetAllocatedBytesForCurrentThread();
                for (var i = 0; i < 64; i++) { lines[2].Antialiased = (i & 1) == 0; corner.RoundPrecision = (i & 1) == 0 ? 8 : 16; RenderingServer.ForceDraw(); }
                var allocated = GC.GetAllocatedBytesForCurrentThread() - before; Check(allocated == 0, $"Prepared active strip/AA rebuild and native replay allocate {allocated} owner bytes.");
                if (Environment.GetEnvironmentVariable("ELECTRON2D_LINE_GEOMETRY_OUTPUT") is { } output) { Directory.CreateDirectory(output); smooth.SavePNG(System.IO.Path.Combine(output, backend + "-line-geometry.png")); }
                root.Tree!.Quit();
            };
            Check(Engine.Run(root) == 0 && root.IsDisposed, "The line geometry rendering session retires cleanly.");
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, oldMethod); ProjectSettings.Set(ProjectSettings.RenderingFallback, oldFallback); }
        Console.WriteLine(backend + ": line cap/joint/side/atlas/alpha/scale/snap" + (backend == "gpu" ? "/vertex-ID/HDR/MSAA" : "") + " pixels and 64 allocation-free active redraws passed.");
    }
    private static Shader LoadIdentityShader()
    {
        using var vertex = typeof(LineGeometryRenderingTests).Assembly.GetManifestResourceStream("Electron2D.Device.DeviceLineIdentity.vert.spv")!;
        using var fragment = typeof(LineGeometryRenderingTests).Assembly.GetManifestResourceStream("Electron2D.Device.DeviceLineIdentity.frag.spv")!;
        using var a = new MemoryStream(); using var b = new MemoryStream(); vertex.CopyTo(a); fragment.CopyTo(b); return Shader.CreateFromSPIRV(a.ToArray(), b.ToArray());
    }
    private static Shader LoadShader(string name)
    {
        using var stream = typeof(LineGeometryRenderingTests).Assembly.GetManifestResourceStream("TestShaders." + name + ".spv")!;
        using var bytes = new MemoryStream(); stream.CopyTo(bytes); return Shader.CreateFromSPIRV(bytes.ToArray());
    }
    private static void Near(float actual, float expected, string message) => Check(MathF.Abs(actual - expected) < .025f, $"{message}: {actual}, expected {expected}.");
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
