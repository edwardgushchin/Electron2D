using Electron2D;
using static PhysicsDebugTests;

internal static class PhysicsDebugNativeTests
{
    internal static void Run()
    {
        var rendering = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        try
        {
            foreach (var renderer in new[] { "gpu", "compatibility" })
                foreach (var backend in new[] { PhysicsServer.Backend.CPU, PhysicsServer.Backend.GPU }) Verify(renderer, backend);
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, rendering); }
    }
    private static void Verify(string renderer, PhysicsServer.Backend backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer);
        using var world = new World(backend); using var box = new RectangleShape { Size = new(20, 20) };
        var window = new Window { Title = "Physics canvas diagnostics", Size = new(420, 280), World = world };
        var body = new StaticBody { Name = "Body", Position = new(30, 30) };
        var collision = new CollisionShape { Name = "Collision", Shape = box, DebugColor = Colors.Green }; body.AddChild(collision); window.AddChild(body);
        window.AddChild(new PinJoint { Name = "Pin", Position = new(230, 30) });
        window.AddChild(new GrooveJoint { Name = "Groove", Position = new(300, 20) });
        window.AddChild(new DampedSpringJoint { Name = "Spring", Position = new(380, 20) });
        var shapes = Shapes(); var painters = new Painter[shapes.Length];
        for (var i = 0; i < shapes.Length; i++) { painters[i] = new(shapes[i]) { Name = "Shape" + i, Position = new(40 + i % 5 * 75, 100 + i / 5 * 100) }; window.AddChild(painters[i]); }
        var stage = 0; long start = 0, bytes = 0; RID owned = default;
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            owned = RenderingServer.CanvasItemCreate(); RenderingServer.CanvasItemSetParent(owned, body.GetCanvas());
            RenderingServer.CanvasItemSetTransform(owned, new(0, new Vector2(110, 30)));
            using (var temporary = new RectangleShape { Size = new(20, 20) }) temporary.Draw(owned, Colors.Red);
            Reject<ArgumentException>(() => box.Draw(box.GetRID(), Colors.Red));
            using (var other = new SubViewport())
            using (var otherTree = new SceneTree(other))
            {
                var foreign = new Entity(); other.AddChild(foreign);
                Reject<InvalidOperationException>(() => box.Draw(foreign.GetCanvasItem(), Colors.Red));
            }
            Reject<InvalidOperationException>(() => Task.Run(() => box.Draw(owned, Colors.Red)).GetAwaiter().GetResult());
            RenderingServer.FramePreDraw += () => start = GC.GetAllocatedBytesForCurrentThread();
            RenderingServer.FramePostDraw += () =>
            {
                if (stage >= 96) bytes += GC.GetAllocatedBytesForCurrentThread() - start;
                if (stage >= 16)
                {
                    collision.QueueRedraw(); foreach (var painter in painters) painter.QueueRedraw();
                    if (++stage == 160) { Check(bytes == 0, $"Native warmed diagnostic redraw allocates {bytes} B"); RenderingServer.FreeRID(owned); Reject<ArgumentException>(() => box.Draw(owned, Colors.Red)); window.Tree!.Quit(); }
                    return;
                }
                using var pixels = window.GetTexture().GetImage()!;
                switch (stage++)
                {
                    case 0: Pixel(pixels, 30, 30, Colors.Black); Pixel(pixels, 110, 30, Colors.Red); window.Tree!.DebugCollisionsHint = true; break;
                    case 1: Pixel(pixels, 30, 30, Colors.Green); Pixel(pixels, 234, 30, new(.35f, .3f, 0)); Pixel(pixels, 306, 45, new(.4f, .4f, .45f)); Pixel(pixels, 386, 20, new(.35f, .3f, 0)); collision.DebugColor = Colors.Blue; break;
                    case 2: Pixel(pixels, 30, 30, Colors.Blue); collision.Disabled = true; break;
                    case 3: Pixel(pixels, 30, 30, new(.5f, .5f, .5f, 1)); collision.Disabled = false; collision.Hide(); break;
                    case 4: Pixel(pixels, 30, 30, Colors.Black); collision.Show(); Task.Run(() => box.Size = new(40, 20)).GetAwaiter().GetResult(); break;
                    case 5: Pixel(pixels, 45, 30, Colors.Blue); Pixel(pixels, 125, 30, Colors.Black); window.Tree!.DebugCollisionsHint = false; break;
                    case 6: Pixel(pixels, 30, 30, Colors.Black); window.Tree!.DebugCollisionsHint = true; collision.Position = new(20, 0); break;
                    case 7: Pixel(pixels, 50, 30, Colors.Blue); collision.Shape = null; break;
                    case 8: Pixel(pixels, 50, 30, Colors.Black); collision.Shape = box; break;
                    case 9: Pixel(pixels, 50, 30, Colors.Blue); pixels.SavePNG($"/tmp/e2d-physics-debug-{renderer}-{backend}.png"); break;
                }
            };
        };
        try { Check(Engine.Run(window) == 0 && stage == 160, "Native diagnostic scene completed"); }
        finally { foreach (var shape in shapes) shape.Dispose(); }
        Check(!DisplayServer.IsAvailable && !RenderingServer.IsAvailable, "Debug scene releases native services");
        Console.WriteLine($"Native physics debug passed: renderer={renderer}, physics={backend}, public Shape.Draw, scene/owned RIDs, live edits, pixel readback and {bytes} B over 64 warmed redraw frames.");
    }
    private static void Pixel(Image pixels, int x, int y, Color expected)
    {
        var color = pixels.GetPixel(x, y);
        Check(Math.Abs(color.R - expected.R) < .04f && Math.Abs(color.G - expected.G) < .04f && Math.Abs(color.B - expected.B) < .04f, $"Pixel {x},{y}: {color} vs {expected}");
    }
    private sealed class Painter(Shape shape) : Entity
    {
        protected override void OnDraw() => shape.Draw(GetCanvasItem(), Colors.Pink);
    }
}
