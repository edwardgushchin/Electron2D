using Electron2D;

internal static class CanvasShadowAtlasChecks
{
    private sealed class Surface : Entity
    {
        internal Action? Drawing;
        protected override void OnDraw() { Drawing?.Invoke(); DrawRect(new(0, 0, 128, 96), Colors.White); }
    }
    private readonly record struct Scenario(int Size, bool Directional, bool Material, LightShadowFilter Filter = LightShadowFilter.None, float Smooth = 0, int Cull = 0, uint Mask = 1, bool Shadows = true, float Rotation = 0, float Distance = 10000, bool Authored = false, bool FarShadow = false);
    private static readonly Vector2[] Points = [new(22.3f, 8.1f), new(27.7f, 88.4f), new(101.2f, 34.7f)];
    private static int _checks;
    internal static void Run(string profile, string shaders)
    {
        _checks = 0;
        Check(!ProjectSettings.IsBasic(ProjectSettings.CanvasShadowAtlasSize) && ProjectSettings.IsRestartRequired(ProjectSettings.CanvasShadowAtlasSize), "Shadow startup setting metadata.");
        Reject<InvalidOperationException>(() => RenderingServer.CanvasSetShadowTextureSize(64));
        var method = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        var oldSize = ProjectSettings.Get(ProjectSettings.CanvasShadowAtlasSize); var fps = Engine.MaxFPS;
        ProjectSettings.Set(ProjectSettings.RenderingMethod, profile == "compatibility" ? "compatibility" : "gpu");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false); ProjectSettings.Set(ProjectSettings.CanvasShadowAtlasSize, 17); Engine.MaxFPS = 0;
        try
        {
            Reject<ArgumentOutOfRangeException>(() => ProjectSettings.Set(ProjectSettings.CanvasShadowAtlasSize, 16385));
            Reject<ArgumentOutOfRangeException>(() => ProjectSettings.Set(ProjectSettings.CanvasShadowAtlasSize, 0));
            if (profile == "compatibility") Compatibility();
            else { Pixels(shaders, profile); MultipleViewports(shaders); }
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, method); ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback);
            ProjectSettings.Set(ProjectSettings.CanvasShadowAtlasSize, oldSize); Engine.MaxFPS = fps;
        }
        Reject<InvalidOperationException>(() => RenderingServer.CanvasSetShadowTextureSize(64));
        Console.WriteLine($"Canvas shadow atlas {profile}: {_checks} public checks passed.");
    }
    internal static void Author(string folder)
    {
        System.IO.Directory.CreateDirectory(folder);
        using var graph = new VisualShader();
        using var white = new VisualShaderNodeVec3Constant { Constant = Vector3.One };
        graph.AddNode(VisualShader.Type.Light, white, default, 2); graph.ConnectNodes(VisualShader.Type.Light, 2, 0, 0, 0);
        graph.GetNode(VisualShader.Type.Light, 0).SetInputPortDefaultValue(1, 1f);
        graph.GetNode(VisualShader.Type.Fragment, 0).SetInputPortDefaultValue(0, Vector3.One);
        graph.GetNode(VisualShader.Type.Fragment, 0).SetInputPortDefaultValue(1, 1f);
        using var shadow = new VisualShaderNodeVec2Constant { Constant = new(75, 75) };
        graph.AddNode(VisualShader.Type.Fragment, shadow, default, 2); graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 6);
        ResourceSaver.Save(graph, System.IO.Path.Combine(folder, "Authored.source.e2dres"), SaverFlags.BundleResources);
        shadow.Constant = new(8, float.MaxValue);
        ResourceSaver.Save(graph, System.IO.Path.Combine(folder, "AuthoredFar.source.e2dres"), SaverFlags.BundleResources);
    }
    private static void Pixels(string folder, string profile)
    {
        var cases = new List<Scenario>();
        foreach (var directional in new[] { false, true }) foreach (var material in new[] { false, true })
            {
                foreach (var size in new[] { 17, 0, int.MinValue, 1, 2, 64, 127, 1024, 16384, 64 }) cases.Add(new(size, directional, material));
                foreach (var filter in new[] { LightShadowFilter.PCF5, LightShadowFilter.PCF13 }) foreach (var smooth in new[] { 0f, .5f, 2f, float.MaxValue, -float.MaxValue })
                        cases.Add(new(64, directional, material, filter, smooth));
                foreach (var cull in new[] { 1, 2 }) cases.Add(new(64, directional, material, Cull: cull));
                cases.Add(new(64, directional, material, Mask: 2)); cases.Add(new(64, directional, material, Shadows: false));
                cases.Add(new(64, directional, material, Rotation: .41f, Distance: 0));
            }
        foreach (var directional in new[] { false, true }) foreach (var filter in new[] { LightShadowFilter.None, LightShadowFilter.PCF13 })
                cases.Add(new(64, directional, true, filter, .5f, Authored: true));
        cases.Add(new(64, true, true, Authored: true, FarShadow: true));
        using var farAuthored = ResourceLoader.Load<VisualShader>(System.IO.Path.Combine(folder, "AuthoredFar.e2dres"), ResourceLoader.CacheMode.IgnoreDeep);
        using var farMaterial = new ShaderMaterial { Shader = farAuthored };
        using var authored = ResourceLoader.Load<VisualShader>(System.IO.Path.Combine(folder, "Authored.e2dres"), ResourceLoader.CacheMode.IgnoreDeep);
        using var authoredMaterial = new ShaderMaterial { Shader = authored };
        using var shader = Shader.CreateFromSPIRV(System.IO.File.ReadAllBytes(System.IO.Path.Combine(folder, "White.frag.spv")));
        using var materialResource = new ShaderMaterial { Shader = shader };
        using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); image.Fill(Colors.White);
        using var texture = ImageTexture.CreateFromImage(image); texture.SetSizeOverride(new(512, 512));
        using var polygon = new OccluderPolygon { Closed = false, Polygon = Points };
        var window = new Window { Size = new(128, 96), Title = "Canvas shadow atlas" };
        if (profile == "gpu-hdr") window.UseHDR2D = true;
        if (profile == "gpu-msaa") window.MSAA2D = Viewport.MSAA.Samples4X;
        var surface = new Surface(); var occluder = new LightOccluder { Occluder = polygon };
        var point = new PointLight { Position = new(61.17f, 43.31f), Texture = texture, ShadowEnabled = true, ShadowColor = Colors.Black };
        var directionalLight = new DirectionalLight { ShadowEnabled = true, ShadowColor = Colors.Black, Enabled = false };
        window.AddChild(surface); window.AddChild(occluder); window.AddChild(point); window.AddChild(directionalLight);
        var frame = 0; long active = 0, idle = 0;
        void Apply(Scenario scenario, bool startup = false)
        {
            if (!startup) RenderingServer.CanvasSetShadowTextureSize(scenario.Size);
            point.Enabled = !scenario.Directional; directionalLight.Enabled = scenario.Directional;
            Light light = scenario.Directional ? directionalLight : point;
            light.ShadowEnabled = scenario.Shadows; light.ShadowFilter = scenario.Filter; light.ShadowFilterSmooth = scenario.Smooth;
            light.ShadowItemCullMask = scenario.Mask; light.Rotation = scenario.Rotation;
            directionalLight.MaxDistance = scenario.Distance; polygon.CullMode = (OccluderCullMode)scenario.Cull;
            surface.Material = scenario.FarShadow ? farMaterial : scenario.Authored ? authoredMaterial : scenario.Material ? materialResource : null;
        }
        window.Ready += _ =>
        {
            RenderingServer.CanvasSetModulate(window.FindWorld()!.Canvas, Colors.Black); RenderingServer.SetDefaultClearColor(Colors.Black);
            Check(RenderingServer.GetCurrentRenderingMethod() == "gpu", "Required GPU path.");
            Apply(cases[0], startup: true);
            Reject<ArgumentOutOfRangeException>(() => RenderingServer.CanvasSetShadowTextureSize(16385));
            Reject<ArgumentOutOfRangeException>(() => RenderingServer.CanvasSetShadowTextureSize(int.MaxValue));
            Exception? offOwner = null; var thread = new Thread(() => { try { RenderingServer.CanvasSetShadowTextureSize(64); } catch (Exception e) { offOwner = e; } }); thread.Start(); thread.Join();
            Check(offOwner is InvalidOperationException, "Off-owner mutation rejected.");
            foreach (var scenario in cases)
            {
                if (frame != 0) Apply(scenario);
                RenderingServer.ForceDraw(false);
                using var pixels = window.GetTexture().GetImage()!;
                Compare(pixels, scenario, point.Position); frame++;
            }
            Apply(new(64, false, true, LightShadowFilter.PCF13, .5f));
            for (var i = 0; i < 32; i++) { point.Position = new(i % 2 == 0 ? 60.17f : 61.17f, 43.31f); RenderingServer.ForceDraw(false); }
            var started = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++) { point.Position = new(i % 2 == 0 ? 60.17f : 61.17f, 43.31f); RenderingServer.CanvasSetShadowTextureSize(64); RenderingServer.ForceDraw(false); }
            active = GC.GetAllocatedBytesForCurrentThread() - started;
            started = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++) RenderingServer.ForceDraw(false);
            idle = GC.GetAllocatedBytesForCurrentThread() - started;
            Check(active == 0 && idle == 0, $"Prepared active/idle draws allocated {active}/{idle} B.");
            window.Tree!.Quit();
        };
        Check(Engine.Run(window) == 0 && frame == cases.Count, "All pixel and prepared-frame scenarios completed.");
        Console.WriteLine($"Verified {cases.Count} full-image states, 64 active/64 idle frames = {active}/{idle} managed B.");
    }
    private static void Compare(Image pixels, Scenario scenario, Vector2 source)
    {
        var width = pixels.Width; var height = pixels.Height;
        var size = (int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)Math.Max(1, scenario.Size));
        var direction = new Transform(scenario.Rotation, Vector2.Zero).Y;
        var samples = scenario.Filter == LightShadowFilter.None ? 1 : scenario.Filter == LightShadowFilter.PCF5 ? 5 : 13;
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
            {
                double expected = 1;
                if (scenario.Shadows && scenario.Mask == 1)
                {
                    expected = 0;
                    for (var sample = 0; sample < samples; sample++)
                        expected += Visible(scenario.FarShadow ? 8 : scenario.Authored ? 75 : x + .5, scenario.FarShadow ? float.MaxValue : scenario.Authored ? 75 : y + .5, width, height, size, scenario, direction, source, (sample - (samples - 1) * .5) * (scenario.Directional ? Math.Min(Math.Abs((double)(1 + scenario.Smooth)), size) : (double)(1 + scenario.Smooth) % size)) ? 1 : 0;
                    expected /= samples;
                }
                var actual = pixels.GetPixel(x, y);
                if (Math.Abs(actual.R - expected) > .009 || Math.Abs(actual.G - expected) > .009 || Math.Abs(actual.B - expected) > .009)
                    throw new InvalidOperationException($"{scenario} pixel {x},{y}: expected {expected}, got {actual}.");
            }
        if (scenario.Size == 64 && scenario.Filter == LightShadowFilter.PCF13 && scenario.Smooth == .5f && scenario.Material &&
            Environment.GetEnvironmentVariable("ELECTRON2D_SHADOW_ATLAS_OUTPUT") is { } output)
        {
            System.IO.Directory.CreateDirectory(output);
            pixels.SavePNG(System.IO.Path.Combine(output, scenario.Directional ? "directional-pcf13.png" : "point-pcf13.png"));
        }
        Check(true, "Independent quantized depth/PCF image.");
    }
    private static bool Visible(double x, double y, int width, int height, int size, Scenario scenario, Vector2 direction, Vector2 source, double offset)
    {
        double rayX, rayY, startX, startY, lower = .00001, upper = double.PositiveInfinity, depth;
        double column;
        if (scenario.Directional)
        {
            var diagonal = Math.Sqrt(width * width + height * height);
            column = Math.Floor(((-(x - width * .5) * direction.Y + (y - height * .5) * direction.X) / diagonal + .5) * size + offset);
            if (column < 0 || column >= size) return true;
            var transverse = ((column + .5) / size - .5) * diagonal;
            rayX = direction.X; rayY = direction.Y;
            var centerDepth = (width * rayX + height * rayY) * .5;
            startX = width * .5 - rayY * transverse - rayX * centerDepth;
            startY = height * .5 + rayX * transverse - rayY * centerDepth;
            var extent = (Math.Abs(rayX) * width + Math.Abs(rayY) * height) * .5;
            lower = centerDepth - extent - Math.Max(0, scenario.Distance); upper = centerDepth + extent;
            depth = x * rayX + y * rayY;
        }
        else
        {
            column = Math.Floor((Math.Atan2(y - source.Y, x - source.X) / (Math.PI * 2) + .5) * size + offset);
            column = (column % size + size) % size;
            var angle = ((column + .5) / size - .5) * Math.PI * 2;
            rayX = Math.Cos(angle); rayY = Math.Sin(angle); startX = source.X; startY = source.Y;
            depth = Math.Sqrt((x - source.X) * (x - source.X) + (y - source.Y) * (y - source.Y));
        }
        var nearest = double.PositiveInfinity;
        for (var i = 1; i < Points.Length; i++)
        {
            var a = Points[i - 1]; var b = Points[i]; var sx = (double)b.X - a.X; var sy = (double)b.Y - a.Y;
            var determinant = rayX * sy - rayY * sx;
            if (Math.Abs(determinant) < .000001 || scenario.Cull == 1 && determinant > 0 || scenario.Cull == 2 && determinant < 0) continue;
            var dx = a.X - startX; var dy = a.Y - startY;
            var t = (dx * sy - dy * sx) / determinant; var u = (dx * rayY - dy * rayX) / determinant;
            if (t >= lower && t <= upper && u >= 0 && u <= 1) nearest = Math.Min(nearest, t);
        }
        return depth <= nearest + Math.Max(.00001, Math.Abs(depth) * .00001);
    }
    private static void MultipleViewports(string folder)
    {
        using var shader = Shader.CreateFromSPIRV(System.IO.File.ReadAllBytes(System.IO.Path.Combine(folder, "White.frag.spv")));
        using var material = new ShaderMaterial { Shader = shader };
        using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); image.Fill(Colors.White);
        using var texture = ImageTexture.CreateFromImage(image); texture.SetSizeOverride(new(512, 512));
        using var polygon = new OccluderPolygon { Closed = false, Polygon = Points };
        var window = new Window { Size = new(128, 96), Title = "Independent shadow targets" };
        var surface = new Surface { Material = material }; window.AddChild(surface);
        var point = new PointLight { Texture = texture, Position = new(61.17f, 43.31f), Color = Colors.Red, Energy = .5f, ShadowEnabled = true, ShadowColor = Colors.Black };
        window.AddChild(point); window.AddChild(new LightOccluder { Occluder = polygon });
        var child = new SubViewport { Size = new(64, 48), RenderTargetUpdateMode = ViewportUpdateMode.Always };
        child.AddChild(new Surface { Material = material }); child.AddChild(new DirectionalLight { ShadowEnabled = true, ShadowColor = Colors.Black });
        child.AddChild(new LightOccluder { Occluder = polygon }); window.AddChild(child);
        var complete = false;
        window.Ready += _ =>
        {
            RenderingServer.CanvasSetModulate(window.FindWorld()!.Canvas, Colors.Black); RenderingServer.CanvasSetModulate(child.FindWorld()!.Canvas, Colors.Black);
            var blue = RenderingServer.CanvasLightCreate(); RenderingServer.CanvasLightAttachToCanvas(blue, window.FindWorld()!.Canvas);
            RenderingServer.CanvasLightSetTexture(blue, texture.GetRID()); RenderingServer.CanvasLightSetColor(blue, Colors.Blue); RenderingServer.CanvasLightSetEnergy(blue, .5f);
            RenderingServer.CanvasLightSetShadowEnabled(blue, true); RenderingServer.CanvasLightSetShadowColor(blue, Colors.Black);
            surface.Drawing = () => Reject<InvalidOperationException>(() => RenderingServer.CanvasSetShadowTextureSize(128));
            for (var phase = 0; phase < 3; phase++)
            {
                var second = phase == 1 ? new Vector2(3.7f, 90.2f) : new Vector2(88.37f, 18.23f);
                RenderingServer.CanvasSetShadowTextureSize(phase == 0 ? 17 : 127);
                RenderingServer.CanvasLightSetTransform(blue, new(0, second));
                if (phase == 2) child.Size = new(48, 32);
                RenderingServer.ForceDraw(false); surface.Drawing = null;
                using var pixels = window.GetTexture().GetImage()!; using var offscreen = child.GetTexture().GetImage()!;
                Compare(offscreen, new(phase == 0 ? 17 : 127, true, true), Vector2.Zero);
                var resolution = phase == 0 ? 32 : 128;
                for (var y = 0; y < pixels.Height; y++) for (var x = 0; x < pixels.Width; x++)
                    {
                        var red = Visible(x + .5, y + .5, pixels.Width, pixels.Height, resolution, new(resolution, false, true), Vector2.Down, point.Position, 0) ? .5f : 0;
                        var b = Visible(x + .5, y + .5, pixels.Width, pixels.Height, resolution, new(resolution, false, true), Vector2.Down, second, 0) ? .5f : 0;
                        var actual = pixels.GetPixel(x, y);
                        if (Math.Abs(actual.R - red) > .009 || Math.Abs(actual.G) > .009 || Math.Abs(actual.B - b) > .009) throw new InvalidOperationException($"Independent atlas rows at {x},{y}: {actual}.");
                    }
                Check(true, "Scene/server rows remain isolated beside an independently sized viewport.");
            }
            for (var i = 0; i < 32; i++) RenderingServer.ForceDraw(false);
            var started = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++) { point.Position = new(i % 2 == 0 ? 60.17f : 61.17f, 43.31f); RenderingServer.ForceDraw(false); }
            var bytes = GC.GetAllocatedBytesForCurrentThread() - started;
            Check(bytes == 0, $"Alternating viewport atlases allocated {bytes} managed B.");
            RenderingServer.FreeRID(blue); Check(!texture.IsDisposed, "Retained light retirement preserves the borrowed mask.");
            child.Dispose(); RenderingServer.ForceDraw(false); complete = true; window.Tree!.Quit();
        };
        Check(Engine.Run(window) == 0 && complete, "Multiple viewport retirement completed.");
    }
    private static void Compatibility()
    {
        var window = new Window { Size = new(32, 32) }; window.AddChild(new Surface());
        var frames = 0;
        window.Ready += _ =>
        {
            RenderingServer.CanvasSetShadowTextureSize(17); RenderingServer.CanvasSetShadowTextureSize(int.MinValue);
            RenderingServer.FramePostDraw += () => { using var pixels = window.GetTexture().GetImage()!; Check(pixels.GetPixel(16, 16).R == 1, "Unlit compatibility survives shadow settings."); frames++; window.Tree!.Quit(); };
        };
        Check(Engine.Run(window) == 0 && frames == 1, "Compatibility session completed.");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); _checks++; }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { _checks++; return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
