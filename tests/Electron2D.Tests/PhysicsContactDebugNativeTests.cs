using Electron2D;
using static PhysicsDebugTests;

internal static class PhysicsContactDebugNativeTests
{
    internal static void Run()
    {
        var renderer = ProjectSettings.Get(ProjectSettings.RenderingMethod); var color = ProjectSettings.Get(ProjectSettings.DebugCollisionContactColor);
        var limit = ProjectSettings.Get(ProjectSettings.DebugCollisionMaxContacts); var outlines = ProjectSettings.Get(ProjectSettings.DebugCollisionDrawOutlines);
        try
        {
            ProjectSettings.Set(ProjectSettings.DebugCollisionContactColor, new Color(1, 0, 0, .5f)); ProjectSettings.Set(ProjectSettings.DebugCollisionMaxContacts, 8);
            ProjectSettings.Set(ProjectSettings.DebugCollisionDrawOutlines, false);
            foreach (var method in new[] { "gpu", "compatibility" })
                foreach (var backend in new[] { PhysicsServer.Backend.CPU, PhysicsServer.Backend.GPU }) Verify(method, backend);
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); ProjectSettings.Set(ProjectSettings.DebugCollisionContactColor, color);
            ProjectSettings.Set(ProjectSettings.DebugCollisionMaxContacts, limit); ProjectSettings.Set(ProjectSettings.DebugCollisionDrawOutlines, outlines);
        }
    }
    private static void Verify(string renderer, PhysicsServer.Backend backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer);
        using var world = new World(backend); using var shape = new CircleShape { Radius = 10 };
        var window = new Window { Title = "Contact point diagnostics", Size = new(128, 128), World = world, CanvasTransform = new(0, new Vector2(10, 0)) };
        var sub = new SubViewport { Name = "Shared", World = world, Size = new(128, 128), CanvasTransform = new(0, new Vector2(0, 10)), RenderTargetUpdateMode = ViewportUpdateMode.Always }; window.AddChild(sub);
        var a = new StaticBody { Name = "A", Position = new(60, 60) }; a.AddChild(new CollisionShape { Shape = shape, DebugColor = new(0, 0, 0, 0) }); window.AddChild(a);
        var b = new RigidBody { Name = "B", Position = new(60, 75), GravityScale = 0, CanSleep = false }; b.AddChild(new CollisionShape { Shape = shape, DebugColor = new(0, 0, 0, 0) }); window.AddChild(b);
        var stage = 0; var frames = 0; var warm = 0; long start = 0, allocated = 0;
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); window.Tree!.DebugCollisionsHint = true;
            RenderingServer.FramePreDraw += () => start = GC.GetAllocatedBytesForCurrentThread();
            RenderingServer.FramePostDraw += () =>
            {
                Check(++frames < 12000, "Contact diagnostic native test exceeded its frame budget");
                if (window.Tree!.PhysicsFrameCount < 32) return;
                if (stage == 0 && warm++ < 128) { if (warm > 64) allocated += GC.GetAllocatedBytesForCurrentThread() - start; return; }
                using var pixels = window.GetTexture().GetImage()!; using var shared = sub.GetTexture().GetImage()!;
                var data = PhysicsServer.Service.GetSceneSpace(world.Space);
                if (stage == 0)
                {
                    Check(data.DebugContacts.Length == 2, "Native pair has two contact points");
                    Pixel(pixels, 70, 70, .75f); Pixel(shared, 60, 80, .75f);
                    Check(allocated == 0, $"Native contact recording/rendering allocated {allocated} B after warmup");
                    pixels.SavePNG($"/tmp/e2d-contact-debug-{renderer}-{backend}.png"); window.Tree.DebugCollisionsHint = false; stage++;
                }
                else if (stage == 1) { Pixel(pixels, 70, 70, 0); Pixel(shared, 60, 80, 0); window.Tree.DebugCollisionsHint = true; stage++; }
                else if (stage == 2 && data.DebugContacts.Length == 2) { Pixel(pixels, 70, 70, .75f); b.Position = new(110, 110); stage++; }
                else if (stage == 3 && data.DebugContacts.IsEmpty) { Pixel(pixels, 70, 70, 0); Pixel(shared, 60, 80, 0); window.Tree.Quit(); stage++; }
            };
        };
        Check(Engine.Run(window) == 0 && stage == 4, "Contact native sequence completed");
        Check(!RenderingServer.IsAvailable && !DisplayServer.IsAvailable, "Contact canvas/native resources released");
        Console.WriteLine($"Contact diagnostic pixels passed: {renderer}/{backend}, shared world once per canvas, viewport transforms, toggle, separation, 0 managed B in 64 warm render frames.");
    }
    private static void Pixel(Image image, int x, int y, float red)
    {
        var value = image.GetPixel(x, y); Check(Math.Abs(value.R - red) < .05f && value.G < .04f && value.B < .04f, $"Contact pixel {x},{y}: {value}, expected red {red}");
    }
}
