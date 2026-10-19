using Electron2D;

internal static partial class GPUParticlesNodeTests
{
    internal static void Routing()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(64, 64) }; using var image = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8); image.Fill(Colors.White); using var texture = ImageTexture.CreateFromImage(image);
        using var parentMaterial = new ParticleProcessMaterial { Gravity = Vector2.Zero, Color = Colors.Green, SubEmitterMode = ParticleProcessMaterial.SubEmitterModeEnum.AtStart, SubEmitterAmountAtStart = 2 };
        using var childMaterial = new ParticleProcessMaterial { Gravity = Vector2.Zero, Color = Colors.Blue };
        var parent = new GPUParticles { Name = "Parent", Amount = 1, Emitting = false, FixedFPS = 0, SpeedScale = 0, LocalCoords = true, Interpolate = false, Position = new(20, 20), Texture = texture, ProcessMaterial = parentMaterial, SubEmitter = "../Child" };
        var child = new GPUParticles { Name = "Child", Amount = 2, Emitting = false, FixedFPS = 0, SpeedScale = 0, LocalCoords = true, Interpolate = false, Position = new(40, 20), ZIndex = 1, Texture = texture, ProcessMaterial = childMaterial };
        root.AddChild(parent); root.AddChild(child); var frames = 0;
        root.Ready += _ =>
        {
            parent.EmitParticle(Transform.Identity, Vector2.Zero, Colors.White, Colors.White, GPUParticles.EmitFlags.Position);
            RenderingServer.FramePostDraw += () =>
            {
                using var capture = root.GetTexture().GetImage()!; var pixel = capture.GetPixel(20, 20);
                Check(pixel.B > .98 && pixel.R < .02 && pixel.G < .02 && child.CaptureRect().GetCenter().DistanceTo(new(-20, 0)) < .001f, "Relative scene sub-emitter routing converts parent-local through world into child-local storage.");
                frames++; root.Tree!.Quit();
            };
        };
        Engine.Run(root); Check(frames == 1, "Native scene child stream completed.");
        Console.WriteLine("GPU scene sub-emitter path, child capacity and local/world conversion pixels passed.");
    }
    internal static void Atlas()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(64, 64) }; using var image = Image.CreateEmpty(8, 4, false, Image.Format.Rgba8); image.Fill(Colors.Red); for (var y = 0; y < 4; y++) for (var x = 4; x < 8; x++) image.SetPixel(x, y, Colors.Blue);
        using var texture = ImageTexture.CreateFromImage(image); using var inner = new AtlasTexture { Atlas = texture, Region = new(2, 0, 6, 4) }; using var atlas = new AtlasTexture { Atlas = inner, Region = new(2, 0, 4, 4) };
        using var process = new ParticleProcessMaterial { Gravity = Vector2.Zero };
        var node = new GPUParticles { Amount = 1, Emitting = false, FixedFPS = 0, SpeedScale = 0, Interpolate = false, LocalCoords = true, Texture = atlas, Position = new(20, 20), ProcessMaterial = process }; root.AddChild(node); var frames = 0;
        root.Ready += _ =>
        {
            node.EmitParticle(Transform.Identity, Vector2.Zero, Colors.White, Colors.White, GPUParticles.EmitFlags.Position);
            RenderingServer.FramePostDraw += () =>
            {
                using var capture = root.GetTexture().GetImage()!; var color = capture.GetPixel(20, 20);
                Check(frames == 0 ? color.B > .98 && color.R < .02 : color.R > .98 && color.B < .02, "Nested atlas UVs and live region changes reach native particle pixels.");
                if (++frames == 1) atlas.Region = new(0, 0, 2, 4); else root.Tree!.Quit();
            };
        };
        Engine.Run(root); Check(frames == 2, "Two atlas frames completed."); Console.WriteLine("GPU scene nested atlas and live region pixels passed.");
    }
    internal static void WorldAndPause()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(64, 64) }; using var image = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8); image.Fill(Colors.White); using var texture = ImageTexture.CreateFromImage(image);
        using var material = new ParticleProcessMaterial { Gravity = Vector2.Zero, Direction = new(1, 0), Spread = 0, InitialVelocityMin = 8, Color = Colors.Red };
        var node = new GPUParticles { Amount = 1, Emitting = false, Lifetime = 10, FixedFPS = 0, Interpolate = false, SpeedScale = 1, Texture = texture, ProcessMaterial = material, Position = new(20, 20) }; root.AddChild(node); var frame = 0; var pausedWorld = Vector2.Zero; var resumed = false;
        root.Ready += _ =>
        {
            node.EmitParticle(new Transform(0, new Vector2(20, 20)), Vector2.Zero, Colors.White, Colors.White, GPUParticles.EmitFlags.Position); node.RequestParticlesProcess(.125f);
            RenderingServer.FramePostDraw += () =>
            {
                var world = node.CaptureRect().GetCenter() + node.Position;
                if (frame == 0) { pausedWorld = world; root.Tree!.Paused = true; node.Position = new(30, 20); }
                else if (frame == 1) { Check(world.DistanceTo(pausedWorld) < .0001f, "Paused world particles retain their position independently of emitter movement."); root.Tree!.Paused = false; }
                else if (world.X > pausedWorld.X + .0001f) { resumed = true; root.Tree!.Quit(); }
                if (++frame == 64) root.Tree!.Quit();
            };
        };
        Engine.Run(root); Check(resumed, "Scene pause restored automatic native speed without changing the authored scale."); Console.WriteLine("GPU scene world persistence, pause and native resume passed.");
    }
}
