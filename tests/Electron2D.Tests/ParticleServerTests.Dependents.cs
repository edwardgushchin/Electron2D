using Electron2D;

internal static partial class ParticleServerTests
{
    private static void Subemitter()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(64, 64) }; var owner = new Entity(); root.AddChild(owner);
        using var mesh = Quad(); using var shader = ProcessShader(); using var material = new ShaderMaterial { Shader = shader };
        material.SetShaderParameter("Tint", Colors.Yellow); material.SetShaderParameter("Velocity", Vector2.Zero); material.SetShaderParameter("Operation", 4u);
        RID parent = default, child = default, item = default; var frames = 0;
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            child = RenderingServer.ParticlesCreate(); parent = RenderingServer.ParticlesCreate();
            foreach (var id in new[] { parent, child }) { RenderingServer.ParticlesSetAmount(id, 1); RenderingServer.ParticlesSetFixedFPS(id, 0); RenderingServer.ParticlesSetLifetime(id, 100); RenderingServer.ParticlesSetUseLocalCoordinates(id, true); RenderingServer.ParticlesSetSpeedScale(id, 0); }
            RenderingServer.ParticlesSetEmissionTransform(parent, new Transform(0, new Vector2(10, 0))); RenderingServer.ParticlesSetEmissionTransform(child, new Transform(0, new Vector2(5, 0)));
            RenderingServer.ParticlesSetProcessMaterial(parent, material.GetRID()); RenderingServer.ParticlesSetSubemitter(parent, child); RenderingServer.ParticlesSetEmitting(child, true);
            try { RenderingServer.ParticlesSetSubemitter(child, parent); throw new InvalidOperationException("Cyclic subemission accepted."); } catch (ArgumentException) { }
            item = Attach(owner, child, mesh); RenderingServer.CanvasItemSetTransform(item, new Transform(0, new Vector2(5, 0)));
            RenderingServer.ParticlesEmit(parent, new Transform(0, new Vector2(10, 20)), Vector2.Zero, Colors.White, Colors.White, 31); RenderingServer.ParticlesRequestProcessTime(parent, .01f);
            RenderingServer.FramePostDraw += () =>
            {
                using var image = root.GetTexture().GetImage()!; Pixel(image, 20, 20, Colors.Yellow);
                Check(!RenderingServer.ParticlesGetEmitting(child), "GPU subemission disables automatic child emission.");
                var bounds = RenderingServer.ParticlesGetCurrentAABB(child); Check(Math.Abs(bounds.Position.X - 11) < .01f, "Subemission converts parent local through world into child local coordinates.");
                RenderingServer.FreeRID(item); RenderingServer.FreeRID(parent); RenderingServer.FreeRID(child); frames++; root.Tree!.Quit();
            };
        };
        Engine.Run(root); Check(frames == 1, "Subemission native frame completed.");
        Console.WriteLine("Particle GPU subemission, reverse creation order, cycle rejection and coordinate conversion passed.");
    }
    private static void Trails()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(64, 64) }; var owner = new Entity(); root.AddChild(owner);
        using var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new()
        {
            Vertices = [new(0, -1), new(0, 1), new(0, -1), new(0, 1)],
            Indices = [0, 1, 2, 1, 3, 2],
            Bones = [0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0],
            Weights = [1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0]
        });
        using var shader = ProcessShader(); using var process = new ShaderMaterial { Shader = shader };
        process.SetShaderParameter("Tint", Colors.White); process.SetShaderParameter("Velocity", new Vector2(60, 0)); process.SetShaderParameter("Operation", 128u);
        RID particles = default, item = default; var frame = 0;
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); particles = RenderingServer.ParticlesCreate(); RenderingServer.ParticlesSetAmount(particles, 1);
            RenderingServer.ParticlesSetUseLocalCoordinates(particles, true); RenderingServer.ParticlesSetLifetime(particles, 100); RenderingServer.ParticlesSetSpeedScale(particles, 0);
            RenderingServer.ParticlesSetProcessMaterial(particles, process.GetRID()); RenderingServer.ParticlesSetInterpolate(particles, false); var poses = new[] { Transform.Identity, Transform.Identity };
            RenderingServer.ParticlesSetTrailBindPoses(particles, poses); poses[0] = new Transform(0, new Vector2(1000, 0)); RenderingServer.ParticlesSetTrails(particles, true, .5f);
            item = Attach(owner, particles, mesh);
            RenderingServer.ParticlesEmit(particles, new Transform(0, new Vector2(20, 20)), new(60, 0), Colors.White, Colors.White, 31); RenderingServer.ParticlesRequestProcessTime(particles, .5f);
            RenderingServer.FramePostDraw += () =>
            {
                using var image = root.GetTexture().GetImage()!;
                if (frame == 0)
                {
                    Pixel(image, 30, 20, new(1f / 6, 0, 5f / 6, 1)); var bounds = RenderingServer.ParticlesGetCurrentAABB(particles);
                    Check(bounds.Position.X <= 20 && bounds.End.X >= 46, "Current bounds include the active trail history.");
                    RenderingServer.ParticlesSetTrails(particles, false, .5f); RenderingServer.ParticlesRequestProcess(particles);
                }
                else
                { Pixel(image, 30, 20, Colors.Black); RenderingServer.FreeRID(item); RenderingServer.FreeRID(particles); root.Tree!.Quit(); }
                frame++;
            };
        };
        Engine.Run(root); Check(frame == 2, "Two native trail frames completed.");
        Console.WriteLine("Particle GPU history, copied planar bind poses, weighted trail mesh, bounds and trail reset passed.");
    }
}
