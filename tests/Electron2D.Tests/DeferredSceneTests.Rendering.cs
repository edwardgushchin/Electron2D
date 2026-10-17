using Electron2D;
using static TextureArrayTests;

internal static partial class DeferredSceneTests
{
    internal static void RunNative(string backend)
    {
        var folder = At(System.IO.Path.GetTempPath(), "electron2d-deferred-render-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            Child("produce", folder); Register();
            ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
            Engine.MaxFPS = 120;
            using var scene = ResourceLoader.Load<PackedScene>(At(folder, "bundled.e2dscene"), ResourceLoader.CacheMode.IgnoreDeep);
            var root = scene.Instantiate(); var pending = root.GetNode<InstancePlaceholder>("Pending0");
            Check(pending.GetChildCount() == 0, "Deferred physical and visual children do not exist before creation.");
            var window = new Window { Size = new(96, 80), Title = "Deferred scene runtime" }; window.AddChild(root);
            using var target = window.GetTexture();
            Actor? actor = null; Sprite? visual = null; RigidBody? body = null;
            var frames = 0; long before = 0, bytes = 0; var initial = Vector2.Zero;
            window.Ready += _ =>
            {
                Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Requested renderer executes without fallback.");
                RenderingServer.SetDefaultClearColor(Colors.Green);
                window.Tree!.ProcessFrameStarted += _ =>
                {
                    if (frames == 2)
                    {
                        actor = (Actor)pending.CreateInstance(replace: true)!;
                        visual = actor.GetNode<Sprite>("Visual"); body = actor.GetNode<RigidBody>("Body"); initial = body.GlobalPosition;
                    }
                    if (frames >= 16) { before = GC.GetAllocatedBytesForCurrentThread(); visual!.QueueRedraw(); }
                };
                RenderingServer.FramePostDraw += Capture;
            };
            void Capture()
            {
                if (frames >= 16) bytes += GC.GetAllocatedBytesForCurrentThread() - before;
                frames++;
                if (frames == 1) { using var image = target.GetImage()!; Pixel(image, 44, 55, Colors.Green); }
                if (frames == 5) { using var image = target.GetImage()!; Pixel(image, 44, 55, Colors.Red); Check(pending.IsDisposed, "Replacement retires at the ordinary tree safe point."); }
                if (frames == 12) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers(); }
                if (frames != 80) return;
                Check(bytes == 0, $"Deferred runtime has {bytes} owner-thread managed bytes across 64 prepared render frames.");
                Check(body!.GlobalPosition.X > initial.X + 1, "The materialized physical child executes numerical motion.");
                using var query = new PhysicsPointQueryParameters { Position = body.GlobalPosition };
                Check(body.GetWorld()!.DirectSpaceState.IntersectPoint(query).Any(hit => hit.ColliderRID == body.GetRID()), "The materialized body participates in current world point queries.");
                using var final = target.GetImage()!; Pixel(final, 44, 55, Colors.Red);
                if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } output) { Directory.CreateDirectory(output); final.SavePNG(At(output, "deferred-" + backend + ".png")); }
                RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit();
            }
            Engine.Run(window);
            Check(window.IsDisposed && actor is { IsDisposed: true } && body is { IsDisposed: true }, "Shutdown releases materialized scene and physical nodes.");
            Console.WriteLine($"Deferred {backend}: lazy pixels, active replacement, body motion/query and 64 render frames at {bytes} owner-thread bytes passed.");
        }
        finally { Directory.Delete(folder, true); }
    }
    private static void Pixel(Image image, int x, int y, Color expected)
    {
        var actual = image.GetPixel(x, y);
        Check(MathF.Abs(actual.R - expected.R) < .012f && MathF.Abs(actual.G - expected.G) < .012f && MathF.Abs(actual.B - expected.B) < .012f,
            $"Deferred pixel {x},{y}: {actual}; expected {expected}.");
    }
}
