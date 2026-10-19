using Electron2D;

internal static partial class ParticleServerTests
{
    private static void EmitFlags()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(64, 64) }; var owner = new Entity(); root.AddChild(owner); using var mesh = Quad(); using var shader = ProcessShader(); using var process = new ShaderMaterial { Shader = shader };
        process.SetShaderParameter("Tint", Colors.White); process.SetShaderParameter("Velocity", Vector2.Zero); process.SetShaderParameter("Operation", 256u);
        RID particles = default, item = default; var frame = 0;
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); particles = RenderingServer.ParticlesCreate(); RenderingServer.ParticlesSetAmount(particles, 1); RenderingServer.ParticlesSetFixedFPS(particles, 0); RenderingServer.ParticlesSetSpeedScale(particles, 0); RenderingServer.ParticlesSetLifetime(particles, 100); RenderingServer.ParticlesSetUseLocalCoordinates(particles, true); RenderingServer.ParticlesSetProcessMaterial(particles, process.GetRID()); item = Attach(owner, particles, mesh);
            RenderingServer.ParticlesEmit(particles, new Transform(0, new Vector2(20, 20)), Vector2.Zero, Colors.Green, Colors.White, 13);
            RenderingServer.FramePostDraw += () =>
            {
                using var image = root.GetTexture().GetImage()!; Pixel(image, 20, 20, frame == 0 ? Colors.White : Colors.Black);
                if (++frame != 2) return;
                RenderingServer.FreeRID(item); RenderingServer.FreeRID(particles); root.Tree!.Quit();
            };
        };
        Engine.Run(root); Check(frame == 2, "Two native emission mask frames completed."); Console.WriteLine("Supplied particle fields reach process initialization and clear after the newborn step.");
    }
}
