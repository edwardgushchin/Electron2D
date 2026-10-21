using Electron2D;

internal static partial class ParticleServerTests
{
    private static void SDF()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(64, 64), SDFOversize = CanvasSDFOversize.Percent100, SDFScale = CanvasSDFScale.Percent100 };
        var owner = new Entity { Position = new(10, 0) }; root.AddChild(owner);
        using var contour = new OccluderPolygon { Polygon = [new(10, 10), new(26, 10), new(26, 26), new(10, 26)] }; var occluder = new LightOccluder { Occluder = contour }; root.AddChild(occluder);
        using var mesh = Quad(); using var source = typeof(ParticleServerTests).Assembly.GetManifestResourceStream("TestShaders.ParticleSDF.comp.spv")!;
        using var bytes = new MemoryStream(); source.CopyTo(bytes); using var shader = Shader.CreateFromSPIRV(bytes.ToArray(), Shader.Mode.Particles); using var material = new ShaderMaterial { Shader = shader };
        using var sampling = new RDSamplerState { MinFilter = RenderingDevice.SamplerFilter.Linear, MagFilter = RenderingDevice.SamplerFilter.Linear, MaxLOD = 0 }; shader.SetTextureSamplerState("SDF_TEXTURE", sampling);
        RID particles = default, item = default; var frame = 0;
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); particles = RenderingServer.ParticlesCreate(); RenderingServer.ParticlesSetAmount(particles, 2);
            RenderingServer.ParticlesSetFixedFPS(particles, 0); RenderingServer.ParticlesSetUseLocalCoordinates(particles, true); RenderingServer.ParticlesSetLifetime(particles, 100); RenderingServer.ParticlesSetSpeedScale(particles, 0);
            RenderingServer.ParticlesSetProcessMaterial(particles, material.GetRID()); item = Attach(owner, particles, mesh);
            RenderingServer.ParticlesEmit(particles, new Transform(0, new Vector2(8, 18)), Vector2.Zero, Colors.White, Colors.White, 31);
            RenderingServer.ParticlesEmit(particles, new Transform(0, new Vector2(24, 18)), Vector2.Zero, Colors.White, Colors.White, 31);
            RenderingServer.FramePostDraw += () =>
            {
                using var image = root.GetTexture().GetImage()!; Pixel(image, 18, 18, frame == 0 ? Colors.Red : Colors.Blue); Pixel(image, 34, 18, Colors.Blue);
                if (frame++ == 0) occluder.SDFCollision = false;
                else { RenderingServer.FreeRID(item); RenderingServer.FreeRID(particles); root.Tree!.Quit(); }
            };
        };
        Engine.Run(root); Check(frame == 2, "Particle SDF frames completed."); Console.WriteLine("Particle process SDF sampling, canvas coordinate conversion and occluder policy pixels passed.");
    }
}
