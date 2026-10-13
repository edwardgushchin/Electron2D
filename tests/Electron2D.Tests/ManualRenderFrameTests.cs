using System.Runtime.CompilerServices;
using System.Text.Json;
using Electron2D;
using static TextureArrayTests;

internal static class ManualRenderFrameTests
{
    internal static void RunManaged()
    {
        Check(!RenderingServer.IsAvailable, "Managed frame checks require no native renderer.");
        var count = Engine.GetFramesDrawn();
        Reject<InvalidOperationException>(() => RenderingServer.ForceDraw());
        Reject<InvalidOperationException>(() => RenderingServer.ForceSync());
        Reject<InvalidOperationException>(() => RenderingServer.RequestFrameDrawnCallback(static () => { }));
        Reject<ArgumentNullException>(() => RenderingServer.RequestFrameDrawnCallback(null!));
        Check(Engine.GetFramesDrawn() == count, "Unavailable-host calls do not draw a frame.");
        Console.WriteLine("Manual rendering unavailable-host checks passed.");
    }

    internal static void RunHeadless()
    {
        var count = Engine.GetFramesDrawn();
        using var root = new Node(); using var tree = new SceneTree(root);
        Engine.Start(tree); try { Engine.AdvanceFrame(1d / 60); } finally { Engine.Stop(); }
        Check(Engine.GetFramesDrawn() == count, "Headless process/physics work does not draw a frame.");
        Console.WriteLine("Headless frame accounting passed.");
    }

    internal static void RunNative()
    {
        var method = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        try
        {
            ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
            foreach (var backend in new[] { "gpu", "compatibility" })
            {
                ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
                var window = new Window { Size = new(64, 48), Title = "Electron2D manual frames" };
                var box = new Box { Position = new(8, 8), Size = new(24, 24), Color = Colors.Red };
                var sub = new SubViewport { Size = new(32, 32), RenderTargetUpdateMode = ViewportUpdateMode.Always };
                sub.AddChild(new Box { Size = new(32, 32), Color = Colors.Green }); window.AddChild(box); window.AddChild(sub);
                var driver = new Driver(window, box, sub, backend); window.AddChild(driver);
                window.Ready += _ =>
                {
                    Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Requested native backend without fallback.");
                    RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.RenderLoopEnabled = false;
                };
                try { Check(Engine.Run(window) == 0 && driver.Completed && window.IsDisposed, "Manual native session and cleanup."); }
                finally { if (!window.IsDisposed) window.Dispose(); }
                Collect(); Check(driver.DiscardedCapture is { IsAlive: false }, "Teardown releases pending callback captures without invoking them.");
                Console.WriteLine(JsonSerializer.Serialize(new { scenario = "manual-render-frames", backend, frames = driver.Frames, warmBytes = driver.WarmBytes, cleanup = window.IsDisposed }));
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, method); ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback); }
    }

    private sealed class Driver : Node
    {
        private readonly Window window; private readonly Box box; private readonly SubViewport sub; private readonly string backend;
        private readonly List<string> order = new(32);
        internal bool Completed; internal ulong Frames; internal long WarmBytes; internal WeakReference? DiscardedCapture;
        internal Driver(Window window, Box box, SubViewport sub, string backend) { this.window = window; this.box = box; this.sub = sub; this.backend = backend; ProcessEnabled = true; }
        protected override void OnProcess(double delta)
        {
            if (Completed) return;
            var initial = Engine.GetFramesDrawn(); var process = Engine.ProcessFrames; var physics = Engine.PhysicsFrames;
            var time = RenderingServer.Service!.CanvasTime;
            var postQueued = false;
            Action post = () => { order.Add("post"); RenderingServer.ForceSync(); if (!postQueued) { postQueued = true; RenderingServer.RequestFrameDrawnCallback(() => order.Add("late")); } };
            Action pre = () =>
            {
                Reject<InvalidOperationException>(() => RenderingServer.ForceDraw());
                Reject<InvalidOperationException>(() => RenderingServer.ForceSync());
                RenderingServer.RequestFrameDrawnCallback(() => order.Add("pre-request"));
            };
            RenderingServer.FramePreDraw += pre; RenderingServer.FramePostDraw += post;
            RenderingServer.RequestFrameDrawnCallback(() => { order.Add("a"); Check(Engine.GetFramesDrawn() == initial + 1, "Count is published before completion callbacks."); });
            RenderingServer.RequestFrameDrawnCallback(() => order.Add("b"));
            var capture = QueueCapture();
            foreach (var step in new[] { -1d, double.NaN, double.PositiveInfinity, double.NegativeInfinity }) Reject<ArgumentOutOfRangeException>(() => RenderingServer.ForceDraw(false, step));
            Check(order.Count == 0 && Engine.GetFramesDrawn() == initial && RenderingServer.Service.CanvasTime == time, "Invalid steps preserve time, count and queued callbacks.");
            Task.Run(() => { Reject<InvalidOperationException>(() => RenderingServer.ForceDraw()); Reject<InvalidOperationException>(() => RenderingServer.ForceSync()); Reject<InvalidOperationException>(() => RenderingServer.RequestFrameDrawnCallback(static () => { })); }).GetAwaiter().GetResult();
            RenderingServer.ForceDraw(false, .25);
            Check(order.SequenceEqual(new[] { "post", "a", "b", "pre-request" }), "Ordered one-shot callbacks; post-submission requests wait for the next frame.");
            Check(!RenderingServer.RenderLoopEnabled && Engine.ProcessFrames == process && Engine.PhysicsFrames == physics, "Manual rendering leaves the scene scheduler and render-loop policy unchanged.");
            Check(Math.Abs(RenderingServer.Service.CanvasTime - time - .25) < 1e-8, "Explicit render-clock step.");
            Pixels(window, Colors.Red); Pixels(sub, Colors.Green);
            Collect(); Check(!capture.IsAlive, "A completed callback releases captured state.");
            RenderingServer.FramePreDraw -= pre;
            var beforeSync = Engine.GetFramesDrawn();
            if (backend == "gpu")
            {
                var device = RenderingServer.GetRenderingDevice()!; var buffer = device.StorageBufferCreate(4, new byte[] { 17, 0, 0, 0 }); var delivered = false;
                try
                {
                    device.BufferGetDataAsync(buffer, bytes => { Check(bytes.SequenceEqual(new byte[] { 17, 0, 0, 0 }), "Renderer readback bytes."); delivered = true; });
                    Check(!delivered, "Renderer-owned readback starts deferred."); RenderingServer.ForceSync(); Check(delivered, "ForceSync submits and completes pending renderer work without drawing.");
                }
                finally { device.FreeRID(buffer); }
            }
            else RenderingServer.ForceSync();
            Check(Engine.GetFramesDrawn() == beforeSync && !order.Contains("late"), "Sync does not draw or consume frame requests.");
            Action failingPost = () => throw new InvalidOperationException("post failure");
            RenderingServer.FramePostDraw += failingPost;
            RenderingServer.RequestFrameDrawnCallback(() => { order.Add("throws"); throw new ArgumentException("callback failure"); });
            RenderingServer.RequestFrameDrawnCallback(() => { order.Add("survives"); RenderingServer.RequestFrameDrawnCallback(() => order.Add("future")); });
            box.Color = Colors.Blue;
            try { RenderingServer.ForceDraw(true); throw new InvalidOperationException("Missing aggregated observer failure."); }
            catch (AggregateException errors) { Check(errors.Flatten().InnerExceptions.Count == 2, "Post observer and callback errors are both retained."); }
            RenderingServer.FramePostDraw -= failingPost;
            Check(Engine.GetFramesDrawn() == initial + 2 && order.TakeLast(4).SequenceEqual(new[] { "post", "late", "throws", "survives" }), "All selected callbacks execute once despite observer errors.");
            Pixels(window, Colors.Blue);
            RenderingServer.ForceDraw(false);
            Check(order.TakeLast(2).SequenceEqual(new[] { "post", "future" }), "Requests made by a completion callback wait for a later frame.");
            RenderingServer.FramePostDraw -= post;
            Action empty = static () => { };
            for (var i = 0; i < 4; i++) { RenderingServer.RequestFrameDrawnCallback(empty); RenderingServer.ForceDraw(false); }
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++) { RenderingServer.RequestFrameDrawnCallback(empty); RenderingServer.ForceDraw(false); }
            WarmBytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
            Check(WarmBytes == 0, "Prepared manual drawing and cached one-shot callbacks allocate no owner-thread managed bytes.");
            Frames = Engine.GetFramesDrawn() - initial; Check(Frames == 71, "Exact manually completed frame count across all viewport targets.");
            DiscardedCapture = QueueCapture(); Completed = true; window.Tree!.Quit();
        }
    }

    private static void Pixels(Viewport viewport, Color expected)
    {
        using var image = viewport.GetTexture().GetImage() ?? throw new InvalidOperationException("No native frame image.");
        var color = image.GetPixel(12, 12); Check(Math.Abs(color.R - expected.R) < .02 && Math.Abs(color.G - expected.G) < .02 && Math.Abs(color.B - expected.B) < .02, "Real root/offscreen pixels from the public texture path.");
    }
    private sealed class Box : Control
    {
        private Color color = Colors.White;
        internal Color Color { get => color; set { color = value; QueueRedraw(); } }
        protected override void OnDraw() => DrawRect(new(Vector2.Zero, Size), color);
    }
    private sealed class Payload { internal int Calls; }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference QueueCapture()
    {
        var payload = new Payload(); RenderingServer.RequestFrameDrawnCallback(() => payload.Calls++); return new WeakReference(payload);
    }
    private static void Collect() { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
}
