using Electron2D;

internal static partial class ParticleServerTests
{
    private sealed class ParticleTextureFill : Entity
    {
        internal Color Fill = Colors.Red;
        internal int Draws;
        protected override void OnDraw() { Draws++; DrawRect(new(0, 0, 8, 8), Fill); }
    }
    private static void MaterialTextures()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(64, 64) }; var owner = new Entity(); root.AddChild(owner);
        var viewport = new SubViewport { Size = new(8, 8), RenderTargetUpdateMode = ViewportUpdateMode.WhenVisible };
        var fill = new ParticleTextureFill(); viewport.AddChild(fill); root.AddChild(viewport);
        using var mesh = Quad(); using var material = new ParticleProcessMaterial { Gravity = Vector2.Zero, ColorRamp = viewport.GetTexture(), DirectionalVelocityCurve = viewport.GetTexture() };
        RID particles = default, item = default; var frames = 0;
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); particles = RenderingServer.ParticlesCreate(); RenderingServer.ParticlesSetAmount(particles, 1);
            RenderingServer.ParticlesSetLifetime(particles, 100); RenderingServer.ParticlesSetSpeedScale(particles, 0); RenderingServer.ParticlesSetFixedFPS(particles, 0);
            RenderingServer.ParticlesSetUseLocalCoordinates(particles, true); RenderingServer.ParticlesSetProcessMaterial(particles, material.GetRID()); item = Attach(owner, particles, mesh);
            RenderingServer.ParticlesEmit(particles, new Transform(0, new Vector2(20, 20)), Vector2.Zero, Colors.White, Colors.White, 1);
            RenderingServer.FramePostDraw += () =>
            {
                if (frames is 1 or 3)
                {
                    var expected = frames == 1 ? Colors.Red : Colors.Green;
                    using var image = root.GetTexture().GetImage()!; Pixel(image, 20, 20, expected);
                    var state = State(particles); var particle = state.GPU!.Read(state.GPU.State, 1)[0];
                    Check(new Vector2(particle.VelocityAge.X, particle.VelocityAge.Y).DistanceTo(new(expected.R, expected.G)) < .001f, "The motion stage samples the same live producer.");
                }
                if (frames == 1) { fill.Fill = Colors.Green; fill.QueueRedraw(); }
                if (frames == 3) { Check(fill.Draws >= 2, "Visible process input redraws its producer after mutation."); RenderingServer.FreeRID(item); RenderingServer.FreeRID(particles); root.Tree!.Quit(); }
                frames++;
            };
        };
        Engine.Run(root); Check(frames == 4, "Four live process texture frames completed.");
        Console.WriteLine("Process-only WhenVisible viewport texture produces and refreshes native particle color pixels.");
    }
}
