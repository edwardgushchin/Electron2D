using Electron2D;

internal static partial class ParticleServerTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Pixel(Image image, int x, int y, Color expected)
    {
        var actual = image.GetPixel(x, y);
        Check(Math.Abs(actual.R - expected.R) < .02 && Math.Abs(actual.G - expected.G) < .02 && Math.Abs(actual.B - expected.B) < .02,
            $"Particle pixel {x},{y}: {actual}, expected {expected}.");
    }
    private sealed class Control : Entity { protected override void OnDraw() => DrawRect(new(40, 10, 8, 8), Colors.Green); }
    internal static void Run(string profile)
    {
        if (profile == "visual-graphs")
        {
            try { VisualGraphs(Environment.GetEnvironmentVariable("ELECTRON2D_GRAPH_FIXTURES") ?? throw new ArgumentException("Missing graph fixture directory.")); }
            catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
            return;
        }
        if (profile == "node-cpu-curves") { GPUParticlesNodeTests.CPUCurves(); return; }
        if (profile == "node-cpu-file") { GPUParticlesNodeTests.CPUCurves(Environment.GetEnvironmentVariable("ELECTRON2D_GPU_NODE_FILE") ?? throw new ArgumentException("Missing GPU curve file.")); return; }
        if (profile == "node-file") { GPUParticlesNodeTests.FileChild(Environment.GetEnvironmentVariable("ELECTRON2D_GPU_NODE_FILE") ?? throw new ArgumentException("Missing GPU scene file.")); return; }
        if (profile == "node-sessions") { GPUParticlesNodeTests.Sessions(); return; }
        if (profile == "node-routing") { GPUParticlesNodeTests.Routing(); return; }
        if (profile == "node-atlas") { GPUParticlesNodeTests.Atlas(); return; }
        if (profile == "node-world") { GPUParticlesNodeTests.WorldAndPause(); return; }
        if (profile == "node-callbacks") { GPUParticlesNodeTests.Callbacks(); return; }
        if (profile == "node-warm") { GPUParticlesNodeTests.Warm(); return; }
        if (profile == "node-managed") { GPUParticlesNodeTests.RunManaged(); return; }
        if (profile == "node") { GPUParticlesNodeTests.Run(); return; }
        if (profile == "material-callbacks") { MaterialCallbacks(); return; }
        if (profile == "material-textures") { MaterialTextures(); return; }
        if (profile == "material-allocations") { Allocations(true); return; }
        if (profile == "material-edges") { MaterialEdges(); return; }
        if (profile == "material-subemission") { MaterialSubemission(); return; }
        if (profile == "material-contacts") { MaterialContacts(); return; }
        if (profile == "material-behavior") { MaterialBehavior(); return; }
        if (profile == "material-managed") { MaterialManaged(); return; }
        if (profile == "process-material") { ProcessMaterial(); return; }
        if (profile == "emit-flags") { EmitFlags(); return; }
        if (profile == "instances") { Instances(); return; }
        if (profile == "world-group") { WorldGroup(); return; }
        if (profile == "group") { Group(); return; }
        if (profile == "streams") { Streams(); return; }
        if (profile == "rollback") { Rollback(); return; }
        if (profile == "inputs") { Inputs(); return; }
        if (profile == "sheet") { Sheet(); return; }
        if (profile == "sdf") { SDF(); return; }
        if (profile == "ownership") { Ownership(); return; }
        if (profile == "rendering") { Rendering(); return; }
        if (profile == "allocations") { Allocations(); return; }
        if (profile == "subemitter") { Subemitter(); return; }
        if (profile == "trails") { Trails(); return; }
        if (profile == "simulation") { Simulation(); return; }
        if (profile is not ("gpu" or "compatibility")) throw new ArgumentException("Unknown particle-server test profile.", nameof(profile));
        ProjectSettings.Set(ProjectSettings.RenderingMethod, profile == "compatibility" ? "compatibility" : "gpu");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(64, 64), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        var surface = new Entity(); root.AddChild(surface); root.AddChild(new Control());
        using var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new() { Vertices = [new(-2, -2), new(2, -2), new(2, 2), new(-2, 2)], Indices = [0, 1, 2, 0, 2, 3] });
        using var input = typeof(ParticleServerTests).Assembly.GetManifestResourceStream("TestShaders.ParticleHLSL.comp.spv")!;
        using var bytes = new MemoryStream(); input.CopyTo(bytes);
        using var shader = Shader.CreateFromSPIRV(bytes.ToArray(), Shader.Mode.Particles);
        using var process = new ShaderMaterial { Shader = shader }; process.SetShaderParameter("Enabled", true); process.SetShaderParameter("Tint", Colors.Blue); process.SetShaderParameter("Drift", Vector2.Zero);
        var particle = default(RID); var canvasItem = default(RID); var frame = 0;
        root.Ready += _ =>
        {
            if (profile == "compatibility")
            {
                try { RenderingServer.ParticlesCreate(); } catch (NotSupportedException) { root.Tree!.Quit(); return; }
                throw new InvalidOperationException("Compatibility accepted GPU particles.");
            }
            RenderingServer.SetDefaultClearColor(Colors.Black);
            particle = RenderingServer.ParticlesCreate(); RenderingServer.ParticlesSetAmount(particle, 1); RenderingServer.ParticlesSetFixedFPS(particle, 0);
            RenderingServer.ParticlesSetLifetime(particle, 100); RenderingServer.ParticlesSetCustomAABB(particle, new(0, 0, 64, 64));
            RenderingServer.ParticlesSetDrawPasses(particle, 1); RenderingServer.ParticlesSetDrawPassMesh(particle, 0, mesh.GetRID());
            canvasItem = RenderingServer.CanvasItemCreate(); RenderingServer.CanvasItemSetParent(canvasItem, surface.GetCanvasItem());
            RenderingServer.CanvasItemAddParticles(canvasItem, particle);
            RenderingServer.ParticlesEmit(particle, new Transform(0, new Vector2(20, 20)), Vector2.Zero, Colors.Red, Colors.White, 31);
            RenderingServer.ParticlesRequestProcess(particle);
            RenderingServer.FramePostDraw += () =>
            {
                using var capture = root.GetTexture().GetImage()!;
                var bounds = RenderingServer.ParticlesGetCurrentAABB(particle); Check(bounds.IsFinite(), "Native current bounds are finite.");
                if (frame == 0)
                {
                    Pixel(capture, 20, 20, Colors.Red);
                    RenderingServer.ParticlesRestart(particle); RenderingServer.ParticlesSetProcessMaterial(particle, process.GetRID());
                    RenderingServer.ParticlesEmit(particle, new Transform(0, new Vector2(30, 20)), Vector2.Zero, Colors.Green, Colors.White, 31);
                    RenderingServer.ParticlesRequestProcess(particle);
                }
                else if (frame == 1)
                {
                    Pixel(capture, 30, 20, Colors.Blue);
                    RenderingServer.ParticlesSetProcessMaterial(particle, default); RenderingServer.ParticlesSetEmissionTransform(particle, new Transform(0, new Vector2(10, 0)));
                }
                else if (frame == 2)
                {
                    Pixel(capture, 20, 20, Colors.Blue); RenderingServer.ParticlesSetUseLocalCoordinates(particle, true);
                }
                else
                {
                    Pixel(capture, 30, 20, Colors.Blue); RenderingServer.FreeRID(canvasItem); RenderingServer.FreeRID(particle); root.Tree!.Quit();
                }
                frame++;
            };
        };
        Engine.Run(root);
        Check(profile == "compatibility" || frame == 4, "Four native particle frames completed.");
        Console.WriteLine("Particle server " + profile + " initial canvas/material/coordinate checks passed.");
    }
}
