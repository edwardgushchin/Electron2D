using System.Diagnostics;
using Electron2D;

internal static class FrameMonitorRenderingTests
{
    internal static void Run(string backend)
    {
        if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_HOST_DELTA") == "1") { HostDeltaSmoothingTests.RunNative(backend); return; }
        var method = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        var fallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        var limit = Engine.MaxFPS;
        var print = Engine.PrintErrorMessages;
        try
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
            ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
            Engine.MaxFPS = 60;
            Engine.PrintErrorMessages = false;
            FailedPresentation();
            using var shape = new CircleShape { Radius = 3 };
            using var world = new World(backend == "gpu" ? PhysicsServer.Backend.GPU : PhysicsServer.Backend.CPU);
            var body = new RigidBody { Position = new(40, 10), CanSleep = false };
            body.AddChild(new CollisionShape { Shape = shape });
            var probe = new DrawingProbe(body, backend);
            var window = new Window { Size = new(96, 80), Title = "Frame timing", World = world };
            window.AddChild(body); window.AddChild(probe);
            using var target = window.GetTexture();
            probe.Target = target;
            Engine.Run(window);
            Check(probe.Passed && probe.Draws > 0 && probe.LastPosition.Y > 10, "Actual rendered frames and physical motion.");
            Check(!DisplayServer.IsAvailable && Engine.MainLoop is null, "Native timing host retired.");
            Console.WriteLine($"Frame timing {backend}: actual red pixels, moving body, presentation-inclusive process peak {probe.ProcessPeak:F6}s passed.");
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, method);
            ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback);
            Engine.MaxFPS = limit; Engine.PrintErrorMessages = print;
        }
    }

    private static void FailedPresentation()
    {
        var window = new Window { Size = new(48, 40), Title = "Failed frame timing" };
        window.AddChild(new FailingDraw());
        Exception? failure = null;
        try { Engine.Run(window); } catch (Exception error) { failure = error; }
        Check(failure is not null && failure.ToString().Contains("Presentation timing failure.", StringComparison.Ordinal) &&
            FrameMonitorContract.Values().Process == 0 && Engine.FramesPerSecond > 0,
            "Failed presentation does not publish the elapsed window even after completed process callbacks.");
    }

    private sealed class FailingDraw : Entity
    {
        private int _frames;
        protected override void OnProcess(double delta)
        {
            _frames++;
            if (_frames == 1) FrameMonitorContract.Burn(1.02);
            if (_frames > 3) Tree!.Quit();
            QueueRedraw();
        }
        protected override void OnDraw()
        {
            if (_frames >= 2) throw new InvalidOperationException("Presentation timing failure.");
            DrawRect(new Rect2(0, 0, 8, 8), Colors.Red);
        }
    }

    private sealed class DrawingProbe(RigidBody body, string backend) : Entity
    {
        private readonly Stopwatch _clock = new();
        internal Texture? Target;
        internal bool Passed;
        internal int Draws;
        internal double ProcessPeak;
        internal Vector2 LastPosition;

        protected override void OnProcess(double delta)
        {
            if (!_clock.IsRunning)
            {
                _clock.Start();
                Check(FrameMonitorContract.Values() == default, "Native attachment resets timings.");
                Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Exact renderer without fallback.");
            }
            QueueRedraw();
            if (_clock.Elapsed.TotalSeconds < 1.2) return;
            var sample = FrameMonitorContract.Values();
            using var image = Target!.GetImage() ?? throw new InvalidOperationException("Missing rendered image.");
            var pixel = image.GetPixel(6, 6);
            Check(pixel.R > .9f && pixel.G < .1f && pixel.B < .1f && pixel.A > .9f, "Actual rendered red pixel.");
            Check(sample.FPS > 0 && sample.FPS == Engine.FramesPerSecond && sample.Process >= .006 &&
                sample.Physics > 0 && sample.Navigation > 0, "Windowed producers include actual draw cost and physical/navigation execution.");
            ProcessPeak = sample.Process; LastPosition = body.Position; Passed = true;
            Tree!.Quit();
        }

        protected override void OnDraw()
        {
            FrameMonitorContract.Burn(.006);
            DrawRect(new Rect2(4, 4, 12, 12), Colors.Red);
            Draws++;
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
