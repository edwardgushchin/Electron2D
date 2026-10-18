using Electron2D;

internal static class PhysicsInterpolationNativeTests
{
    internal static void Run(bool topLevelOnly = false)
    {
        var settings = ProjectSettings.Service;
        var previousInterpolation = ProjectSettings.Get(ProjectSettings.PhysicsInterpolation);
        var previousMethod = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        var previousTicks = Engine.PhysicsTicksPerSecond;
        var previousFPS = Engine.MaxFPS;
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_INTERPOLATION_BACKEND") ?? "compatibility";
        try
        {
            ProjectSettings.Set(ProjectSettings.PhysicsInterpolation, true);
            ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
            Engine.PhysicsTicksPerSecond = 30;
            Engine.MaxFPS = 120;
            if (topLevelOnly) TopLevelPixels();
            else
            {
                SpritePixels();
                TopLevelPixels();
                CameraPixels();
                NestedViewportPixels(false, backend);
                NestedViewportPixels(true, backend);
            }
        }
        finally
        {
            Engine.MaxFPS = previousFPS;
            Engine.PhysicsTicksPerSecond = previousTicks;
            ProjectSettings.Set(ProjectSettings.RenderingMethod, previousMethod);
            ProjectSettings.Set(ProjectSettings.PhysicsInterpolation, previousInterpolation);
        }
        Console.WriteLine($"Native physics-interpolation pixels passed: {backend}.");
    }

    private static void SpritePixels()
    {
        var window = new Window { Size = new(256, 96) };
        var mover = new MovingCanvasNode { Name = "Mover", Position = new(20, 40), PhysicsProcessEnabled = true };
        window.AddChild(mover);
        var sampled = false;
        var frames = 0;
        ulong startPhysicsFrames = 0;
        window.Ready += _ =>
        {
            startPhysicsFrames = Engine.PhysicsFrames;
            var server = RenderingServer.Service!;
            RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                if (++frames > 500) throw new InvalidOperationException("No suitable interpolation frame was presented.");
                var fraction = Engine.PhysicsInterpolationFraction;
                if (Engine.PhysicsFrames - startPhysicsFrames < 3 || fraction is < .2 or > .6) return;
                using var pixels = server.Readback();
                var scaleX = pixels.Width / (float)window.Size.X;
                var scaleY = pixels.Height / (float)window.Size.Y;
                var interpolatedX = mover.Position.X - 24 + 24 * (float)fraction;
                var expected = pixels.GetPixel((int)MathF.Round((interpolatedX + 2) * scaleX), (int)MathF.Round(42 * scaleY));
                var logical = pixels.GetPixel((int)MathF.Round((mover.Position.X + 2) * scaleX), (int)MathF.Round(42 * scaleY));
                Check(expected.R > .95f && expected.G < .05f && logical.R < .05f,
                    $"The renderer presents the interpolated pose (fraction={fraction}, logicalX={mover.Position.X}, red={expected}, current={logical}).");
                sampled = true;
                window.Tree!.Quit();
            };
        };
        Engine.Run(window);
        Check(sampled && window.IsDisposed, "The native interpolation probe completed and released the window.");
    }

    private static void TopLevelPixels()
    {
        var window = new Window { Size = new(256, 96) };
        var ancestor = new Entity { Position = new(30, 0) }; var parent = new Entity { Position = new(20, 0) };
        var child = new RebindingCanvasNode { Position = new(24, 40), PhysicsProcessEnabled = true };
        window.AddChild(ancestor); ancestor.AddChild(parent); parent.AddChild(child);
        var sampled = false; var frames = 0; var pending = false; var expectedX = 0f; var staleX = 0f;
        window.Ready += _ =>
        {
            var start = Engine.PhysicsFrames; var server = RenderingServer.Service!;
            RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePreDraw += () =>
            {
                if (++frames > 500) throw new InvalidOperationException("No suitable TopLevel interpolation frame was presented.");
                var fraction = Engine.PhysicsInterpolationFraction;
                if (sampled || pending || Engine.PhysicsFrames - start < 3 || fraction is < .2 or > .6) return;
                var local = child.Transform; parent.TopLevel = true;
                Check(child.Transform == local, "Rebinding preserves the child's exact logical transform.");
                expectedX = child.GlobalPosition.X;
                staleX = parent.Position.X + child.PreviousX + (child.Position.X - child.PreviousX) * (float)fraction; pending = true;
            };
            RenderingServer.FramePostDraw += () =>
            {
                if (!pending) return;
                using var pixels = server.Readback();
                var sx = pixels.Width / (float)window.Size.X; var sy = pixels.Height / (float)window.Size.Y;
                var current = pixels.GetPixel((int)MathF.Round((expectedX + 2) * sx), (int)MathF.Round(42 * sy));
                var stale = pixels.GetPixel((int)MathF.Round((staleX + 2) * sx), (int)MathF.Round(42 * sy));
                Check(current.R > .95f && current.G < .05f && stale.R < .05f,
                    $"TopLevel rebinding presents the current descendant endpoint, not stale history: current={current}, stale={stale}.");
                sampled = true; window.Tree!.Quit();
            };
        };
        Engine.Run(window);
        Check(sampled && window.IsDisposed, "The TopLevel pixel probe completes and releases its window.");
    }

    private static void CameraPixels()
    {
        var window = new Window { Size = new(256, 96) };
        var camera = new Camera
        {
            Name = "Camera",
            AnchorMode = AnchorMode.FixedTopLeft,
            LimitEnabled = false
        };
        var fixedNode = new FixedCanvasNode { Name = "Fixed", Position = new(120, 40) };
        window.AddChild(camera); window.AddChild(fixedNode);
        var sampled = false;
        var frames = 0;
        ulong startPhysicsFrames = 0;
        window.Ready += _ =>
        {
            var tree = window.Tree!;
            startPhysicsFrames = Engine.PhysicsFrames;
            tree.PhysicsFrameStarted += _ => camera.Position += new Vector2(24, 0);
            var server = RenderingServer.Service!;
            RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                if (++frames > 500) throw new InvalidOperationException("No suitable camera interpolation frame was presented.");
                var fraction = Engine.PhysicsInterpolationFraction;
                if (Engine.PhysicsFrames - startPhysicsFrames < 3 || fraction is < .2 or > .6) return;
                using var pixels = server.Readback();
                var scaleX = pixels.Width / (float)window.Size.X;
                var scaleY = pixels.Height / (float)window.Size.Y;
                var interpolatedX = fixedNode.Position.X - camera.Position.X + 24 * (1 - (float)fraction);
                var expected = pixels.GetPixel((int)MathF.Round((interpolatedX + 2) * scaleX), (int)MathF.Round(42 * scaleY));
                var logical = pixels.GetPixel((int)MathF.Round((fixedNode.Position.X - camera.Position.X + 2) * scaleX),
                    (int)MathF.Round(42 * scaleY));
                Check(expected.R > .95f && expected.G < .05f && logical.R < .05f,
                    $"The camera renders its interpolated view (fraction={fraction}, cameraX={camera.Position.X}, red={expected}, current={logical}).");
                sampled = true;
                tree.Quit();
            };
        };
        Engine.Run(window);
        Check(sampled && window.IsDisposed, "The native camera interpolation probe completed and released the window.");
    }

    private static void NestedViewportPixels(bool cameraMotion, string backend)
    {
        var window = new Window { Size = new(256, 96) };
        var off = new Node { PhysicsInterpolationMode = PhysicsInterpolationMode.Off };
        var viewport = new SubViewport { Size = window.Size, RenderTargetUpdateMode = ViewportUpdateMode.Always };
        Entity source = cameraMotion ? new FixedCanvasNode { Position = new(120, 40) } : new MovingCanvasNode { Position = new(20, 40) };
        source.PhysicsProcessEnabled = !cameraMotion;
        var camera = new Camera { AnchorMode = AnchorMode.FixedTopLeft, LimitEnabled = false };
        viewport.AddChild(source); if (cameraMotion) viewport.AddChild(camera);
        off.AddChild(viewport); window.AddChild(off);
        using var texture = viewport.GetTexture();
        window.AddChild(new Sprite { Texture = texture, Centered = false, TextureFilter = TextureFilter.Nearest });
        var sampled = false; var frames = 0; ulong start = 0;
        window.Ready += _ =>
        {
            var tree = window.Tree!; start = Engine.PhysicsFrames;
            if (cameraMotion) tree.PhysicsFrameStarted += _ => camera.Position += new Vector2(24, 0);
            Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Nested viewport pixels use the requested renderer");
            RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                if (++frames > 500) throw new InvalidOperationException("No suitable nested viewport interpolation frame was presented.");
                var fraction = (float)Engine.PhysicsInterpolationFraction;
                if (Engine.PhysicsFrames - start < 3 || fraction is < .2f or > .6f) return;
                var logicalX = source.Position.X - (cameraMotion ? camera.Position.X : 0);
                var interpolatedX = logicalX + (cameraMotion ? 24 : -24) * (1 - fraction);
                using var image = RenderingServer.Service!.Readback();
                var sx = image.Width / (float)window.Size.X; var sy = image.Height / (float)window.Size.Y;
                var presented = image.GetPixel((int)MathF.Round((interpolatedX + 2) * sx), (int)MathF.Round(42 * sy));
                var current = image.GetPixel((int)MathF.Round((logicalX + 2) * sx), (int)MathF.Round(42 * sy));
                Check(presented.R > .95f && presented.G < .05f && current.R < .05f && source.IsPhysicsInterpolatedAndEnabled() && !off.IsPhysicsInterpolated(),
                    "A nested viewport renders interpolated source/camera pixels independently of its Off parent");
                sampled = true; tree.Quit();
            };
        };
        Check(Engine.Run(window) == 0 && sampled && window.IsDisposed && viewport.IsDisposed,
            "Nested interpolation pixels complete with viewport/window retirement");
    }

    private sealed class MovingCanvasNode : Entity
    {
        protected override void OnPhysicsProcess(double delta) => Position += new Vector2(24, 0);
        protected override void OnDraw() => DrawRect(new(0, 0, 6, 6), Colors.Red);
    }

    private sealed class RebindingCanvasNode : Entity
    {
        internal float PreviousX;
        protected override void OnPhysicsProcess(double delta)
        {
            PreviousX = Position.X;
            Position = new(Position.X >= 96 ? 24 : Position.X + 24, 40);
        }
        protected override void OnDraw() => DrawRect(new(0, 0, 6, 6), Colors.Red);
    }

    private sealed class FixedCanvasNode : Entity
    {
        protected override void OnDraw() => DrawRect(new(0, 0, 6, 6), Colors.Red);
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
