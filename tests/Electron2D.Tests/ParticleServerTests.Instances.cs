using Electron2D;

internal static partial class ParticleServerTests
{
    private static byte[] Code(string name)
    {
        using var source = typeof(ParticleServerTests).Assembly.GetManifestResourceStream("TestShaders." + name + ".spv")!;
        using var bytes = new MemoryStream(); source.CopyTo(bytes); return bytes.ToArray();
    }
    private static void Instances()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(64, 64) }; using var shader = Shader.CreateFromSPIRV(Code("ParticleDraw.vert"), Code("ParticleDraw.frag"));
        using var material = new ShaderMaterial { Shader = shader }; var owner = new Entity { Material = material }; root.AddChild(owner); using var mesh = Quad(); var completed = false;
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); var particles = RenderingServer.ParticlesCreate(); RenderingServer.ParticlesSetAmount(particles, 2); RenderingServer.ParticlesSetFixedFPS(particles, 0); RenderingServer.ParticlesSetSpeedScale(particles, 0); RenderingServer.ParticlesSetUseLocalCoordinates(particles, true); RenderingServer.ParticlesSetLifetime(particles, 100);
            var item = Attach(owner, particles, mesh); RenderingServer.CanvasItemSetUseParentMaterial(item, true);
            RenderingServer.ParticlesEmit(particles, new Transform(0, new Vector2(20, 20)), Vector2.Zero, Colors.White, Colors.White, 31);
            RenderingServer.ParticlesEmit(particles, new Transform(0, new Vector2(40, 20)), Vector2.Zero, Colors.White, Colors.White, 31);
            RenderingServer.ForceDraw(false); using var image = root.GetTexture().GetImage()!; var a = image.GetPixel(20, 20); var b = image.GetPixel(40, 20);
            Check(a.R > .9f && b.G > .9f || a.G > .9f && b.R > .9f, $"Particle draw ordinals must reach the actual vertex builtin: {a}/{b}.");
            RenderingServer.FreeRID(item); RenderingServer.FreeRID(particles); completed = true; root.Tree!.Quit();
        };
        Engine.Run(root); Check(completed, "Particle vertex builtin frame completed."); Console.WriteLine("Particle vertex instance builtin preserves distinct indirect draw ordinals.");
    }
}
