using Electron2D;

internal static partial class ParticleServerTests
{
    private static void ProcessMaterial()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(64, 64) }; var owner = new Entity(); root.AddChild(owner); using var mesh = Quad();
        using var process = new ParticleProcessMaterial { Direction = new(1, 0), Spread = 0, Gravity = new(0, 10), Color = Colors.Green, InitialVelocityMin = 10, InitialVelocityMax = 10 };
        RID particles = default, item = default; var frame = 0;
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); particles = RenderingServer.ParticlesCreate(); RenderingServer.ParticlesSetAmount(particles, 1); RenderingServer.ParticlesSetFixedFPS(particles, 10); RenderingServer.ParticlesSetSpeedScale(particles, 0); RenderingServer.ParticlesSetLifetime(particles, 10); RenderingServer.ParticlesSetUseLocalCoordinates(particles, true); RenderingServer.ParticlesSetProcessMaterial(particles, process.GetRID()); item = Attach(owner, particles, mesh);
            RenderingServer.ParticlesEmit(particles, new Transform(0, new Vector2(20, 20)), Vector2.Zero, Colors.White, Colors.White, 1); RenderingServer.ParticlesRequestProcessTime(particles, .1f);
            RenderingServer.FramePostDraw += () =>
            {
                var state = State(particles); var particle = state.GPU!.Read(state.GPU.State, 1)[0];
                var expectedPosition = frame < 3 ? new Vector2(21 + frame, 20 + .05f * (frame + 1) * (frame + 2)) : new Vector2(23, 20.6f);
                var expectedVelocity = frame < 3 ? new Vector2(10, frame + 1) : Vector2.Zero;
                Check(new Vector2(particle.TransformX.W, particle.TransformY.W).DistanceTo(expectedPosition) < .0001f && new Vector2(particle.VelocityAge.X, particle.VelocityAge.Y).DistanceTo(expectedVelocity) < .0001f, $"Built-in particle frame {frame}: {particle.TransformX}, {particle.TransformY}, {particle.VelocityAge}.");
                using var image = root.GetTexture().GetImage()!; Pixel(image, (int)expectedPosition.X, (int)expectedPosition.Y, Colors.Green);
                Check(particle.Color == new Vector4(0, 1, 0, 1) && Math.Abs(particle.Custom.Y - .01f * (frame + 1)) < .00001f, "Built-in color and normalized lifetime reach GPU state.");
                if (++frame == 4) { RenderingServer.FreeRID(item); RenderingServer.FreeRID(particles); root.Tree!.Quit(); return; }
                if (frame == 3) { process.DampingMin = 200; process.DampingMax = 200; }
                RenderingServer.ParticlesRequestProcessTime(particles, .1f);
            };
        };
        Engine.Run(root); Check(frame == 4, "Four built-in compute integration frames completed."); Console.WriteLine("Built-in particle compute integrates birth, velocity, gravity, damping, color and normalized lifetime on native storage.");
    }
}
