using Electron2D;

internal static class PhysicsInterpolationNativeTests
{
    internal static void Run()
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
            SpritePixels();
            CameraPixels();
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

    private sealed class MovingCanvasNode : Entity
    {
        protected override void OnPhysicsProcess(double delta) => Position += new Vector2(24, 0);
        protected override void OnDraw() => DrawRect(new(0, 0, 6, 6), Colors.Red);
    }

    private sealed class FixedCanvasNode : Entity
    {
        protected override void OnDraw() => DrawRect(new(0, 0, 6, 6), Colors.Red);
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
