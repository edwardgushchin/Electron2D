using Electron2D;

internal static partial class GPUParticlesNodeTests
{
    internal static void FileChild(string path)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); using var root = new Window { Size = new(64, 64) };
        var scene = packed.Instantiate(); root.AddChild(scene); var node = (GPUParticles)scene.GetNode("One");
        Check(node.ProcessMaterial is ParticleProcessMaterial { Gravity.X: 2, Gravity.Y: 3 } && node.TrailEnabled && node.TrailSections == 9 && node.TrailSectionSubdivisions == 7 && node.Seed == 17, "Fresh process restores authored scene, process material, trail and seed settings.");
        node.TrailEnabled = false; node.Interpolate = false; node.SubEmitter = string.Empty; var frames = 0;
        root.Ready += _ =>
        {
            node.EmitParticle(new Transform(0, new Vector2(20.5f, 20.5f)), Vector2.Zero, Colors.White, Colors.White, ParticlesEmitFlags.Position);
            RenderingServer.FramePostDraw += () =>
            {
                using var image = root.GetTexture().GetImage()!; var pixel = image.GetPixel(20, 20);
                Check(pixel.R > .98 && pixel.G < .02 && pixel.B < .02 && node.CaptureRect().HasArea(), "Fresh-file process material produces actual native particle pixels.");
                frames++; root.Tree!.Quit();
            };
        };
        Engine.Run(root); Check(frames == 1, "Fresh-file native frame completed."); Console.WriteLine("Fresh GPU particle scene pixels passed.");
    }
    internal static void Sessions()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var image = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8); image.Fill(Colors.White); using var texture = ImageTexture.CreateFromImage(image);
        using var process = new ParticleProcessMaterial { Gravity = Vector2.Zero, Color = Colors.Green };
        using var node = new GPUParticles { Amount = 1, Emitting = false, FixedFPS = 0, SpeedScale = 0, LocalCoords = true, Interpolate = false, ProcessMaterial = process, Texture = texture };
        var sessions = 0;
        for (var session = 0; session < 2; session++)
        {
            var position = new Vector2(20 + session * 10, 20); using var root = new Window { Size = new(64, 64) }; node.Position = position; root.AddChild(node);
            root.Ready += _ =>
            {
                node.EmitParticle(Transform.Identity, Vector2.Zero, Colors.White, Colors.White, ParticlesEmitFlags.Position);
                RenderingServer.FramePostDraw += () =>
                {
                    using var frame = root.GetTexture().GetImage()!; var pixel = frame.GetPixel((int)position.X, 20); Check(pixel.G > .98 && pixel.R < .02 && node.CaptureRect().HasArea(), "Each rendering session owns fresh native state for the retained authored node.");
                    root.RemoveChild(node); sessions++; root.Tree!.Quit();
                };
            };
            Engine.Run(root); Check(!node.IsDisposed && !texture.IsDisposed && !process.IsDisposed && node.CaptureRect() == default, "Detached authoring survives session retirement without retaining live native state.");
        }
        Check(sessions == 2, "Two independent renderer sessions completed."); Console.WriteLine("GPU scene emitter retires and rebuilds native state across two rendering sessions.");
    }
}
