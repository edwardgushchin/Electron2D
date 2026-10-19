using Electron2D;

internal static partial class GPUParticlesNodeTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    internal static void Run()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(64, 64) };
        using var image = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8); image.Fill(Colors.White);
        using var texture = ImageTexture.CreateFromImage(image);
        using var material = new ParticleProcessMaterial { Gravity = Vector2.Zero, Direction = new(1, 0), Spread = 0, InitialVelocityMin = 8, Color = Colors.Green };
        var node = new GPUParticles { Name = "Particles", Amount = 1, Emitting = false, Lifetime = 10, FixedFPS = 8, Interpolate = false, LocalCoords = true, SpeedScale = 0, ProcessMaterial = material, Texture = texture, Position = new(20, 20), UseFixedSeed = true, Seed = 123 };
        root.AddChild(node); var frames = 0;
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            node.EmitParticle(Transform.Identity, Vector2.Zero, Colors.White, Colors.White, GPUParticles.EmitFlags.Position);
            node.RequestParticlesProcess(.125f);
            RenderingServer.FramePostDraw += () =>
            {
                using var capture = root.GetTexture().GetImage()!;
                var expectedX = frames == 0 ? 21 : 30 + frames; var expected = frames < 3 ? Colors.Green : Colors.Red; var actual = capture.GetPixel(expectedX, 20);
                Check(Math.Abs(actual.R - expected.R) < .02 && Math.Abs(actual.G - expected.G) < .02 && Math.Abs(actual.B - expected.B) < .02, $"GPU scene particle frame {frames}: {actual}, expected {expected} at {expectedX},20.");
                var bounds = node.CaptureRect(); Check(bounds.IsFinite() && bounds.HasArea() && bounds.GetCenter().DistanceTo(new(expectedX - node.Position.X, 0)) < .001f, "The scene emitter exposes actual current native position and bounds.");
                if (frames == 0) node.Position = new(30, 20);
                if (frames == 1) node.RequestParticlesProcess(.125f);
                if (frames == 2) { material.Color = Colors.Red; node.RequestParticlesProcess(.125f); }
                if (++frames == 4) root.Tree!.Quit();
            };
        };
        Engine.Run(root); Check(frames == 4 && !material.IsDisposed && !texture.IsDisposed, "Four scene particle frames completed with borrowed resources preserved.");
        Console.WriteLine("GPU scene emitter native birth, explicit seek, local transform, live material and bounds pixels passed.");
    }
}
