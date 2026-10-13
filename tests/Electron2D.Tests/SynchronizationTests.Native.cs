using Electron2D;
using Mutex = Electron2D.Mutex;
using Semaphore = Electron2D.Semaphore;

internal static partial class SynchronizationTests
{
    private sealed class Picture : Entity
    {
        internal Color Color = Colors.Black;
        protected override void OnDraw() => DrawRect(new(8, 8, 16, 12), Color);
    }
    internal static void RunNative(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2);
        using var gate = new Mutex(); using var work = new Semaphore();
        var picture = new Picture(); var window = new Window { Size = new(64, 48) }; window.AddChild(picture);
        var value = 0; var published = false; var rendered = false; long task = -1; var owner = Environment.CurrentManagedThreadId;
        Action publish = () =>
        {
            Check(Environment.CurrentManagedThreadId == owner, "Worker result publishes on scene owner.");
            gate.Lock(); try { Check(value == 37, "Shared data is protected by the public mutex."); } finally { gate.Unlock(); }
            picture.Color = Colors.Red; picture.QueueRedraw(); published = true;
        };
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            var tree = window.Tree!;
            task = WorkerThreadPool.AddTask(() =>
            {
                Check(Environment.CurrentManagedThreadId != owner, "Computation executes on real worker.");
                work.Wait(); gate.Lock(); try { value = 37; } finally { gate.Unlock(); }
                tree.Defer(publish);
            });
            work.Post(); var frames = 0;
            RenderingServer.FramePostDraw += () =>
            {
                if (published)
                {
                    using var image = RenderingServer.Service!.Readback(); var pixel = image.GetPixel(12, 12);
                    Check(pixel.R > .95f && pixel.G < .05f && pixel.B < .05f, "Synchronized worker result reaches actual native red pixels.");
                    WorkerThreadPool.WaitForTaskCompletion(task); task = -1; rendered = true; window.Tree!.Quit();
                }
                Check(++frames < 180, "Worker scene publication reaches a frame.");
            };
        };
        try { Check(Engine.Run(window) == 0 && rendered && window.IsDisposed && !RenderingServer.IsAvailable, "Synchronized native scene cleans up."); }
        finally { if (task != -1) { work.Post(); WorkerThreadPool.WaitForTaskCompletion(task); } }
        Console.WriteLine("Public Mutex/Semaphore worker-to-scene " + backend + " pixels and teardown passed.");
    }
}
