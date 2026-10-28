using Electron2D;
using static TextureArrayTests;

internal static class EngineBeforeFrameTests
{
    private const string ActionName = "tests_host_input";
    private sealed class Probe : Node
    {
        internal int ReadyCalls, ProcessCalls, PressedFrames, InputCalls;
        protected override void OnReady() => ReadyCalls++;
        protected override void OnProcess(double delta) { ProcessCalls++; if (Input.IsActionPressed(ActionName)) PressedFrames++; }
        protected override void OnInput(InputEvent input)
        {
            if (input is not InputEventAction { Action: ActionName }) return;
            Check(ReadyCalls == 1, "Host input is delivered after ordinary readiness."); InputCalls++;
        }
    }
    private sealed class Writer : MovieWriter
    {
        internal int Frames, Ends;
        protected override string[] OnGetSupportedExtensions() => ["framehook"];
        protected override bool OnHandlesFile(string path) => System.IO.Path.GetExtension(path) == ".framehook";
        protected override void OnWriteBegin(Vector2i size, int fps, string path) { }
        protected override void OnWriteFrame(Image image, ReadOnlySpan<int> pcm) => Frames++;
        protected override void OnWriteEnd() => Ends++;
    }
    internal static void Managed()
    {
        Func<Window, int> original = Engine.Run;
        Func<Window, Task<int>> originalAsync = Engine.RunAsync;
        Func<Window, string, int, int> recording = Engine.Run;
        Func<Window, string, int, Task<int>> recordingAsync = Engine.RunAsync;
        Func<Window, int> configured = Engine.RunMovie;
        Func<Window, Task<int>> configuredAsync = Engine.RunMovieAsync;
        using var root = new Window();
        Reject<ArgumentNullException>(() => Engine.Run((Action<Window, ulong>)null!, root));
        Reject<ArgumentNullException>(() => Engine.RunAsync((Action<Window, ulong>)null!, root));
        Reject<ArgumentNullException>(() => Engine.Run((Action<Window, ulong>)null!, root, "capture.framehook"));
        Reject<ArgumentNullException>(() => Engine.RunAsync((Action<Window, ulong>)null!, root, "capture.framehook"));
        Reject<ArgumentNullException>(() => Engine.RunMovie((Action<Window, ulong>)null!, root));
        Reject<ArgumentNullException>(() => Engine.RunMovieAsync((Action<Window, ulong>)null!, root));
        Reject<ArgumentException>(() => Engine.Run(root, null!));
        Check(!root.IsDisposed && Engine.MainLoop is null, "All callback precondition failures preserve the unowned root and idle engine.");
        GC.KeepAlive(new Delegate[] { original, originalAsync, recording, recordingAsync, configured, configuredAsync });
        Console.WriteLine("Host frame boundary: six original delegate shapes and six null-callback preconditions passed.");
    }
    internal static void Native(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        InputMap.AddAction(ActionName); Engine.MaxFPS = 60;
        var previousPath = ProjectSettings.Get(ProjectSettings.MovieWriterFile); var previousFPS = ProjectSettings.Get(ProjectSettings.MovieWriterFPS);
        try
        {
            ProjectSettings.Set(ProjectSettings.MovieWriterFile, "capture.framehook"); ProjectSettings.Set(ProjectSettings.MovieWriterFPS, 60);
            for (var kind = 0; kind < 6; kind++)
            {
                var root = new Window { Size = new(64, 64) }; var probe = new Probe(); root.AddChild(probe);
                using var writer = new Writer(); if (kind >= 2) MovieWriter.AddWriter(writer);
                using var press = new InputEventAction { Action = ActionName, Pressed = true };
                using var release = new InputEventAction { Action = ActionName, Pressed = false };
                ulong callbacks = 0;
                void Before(Window window, ulong next)
                {
                    callbacks++;
                    Check(ReferenceEquals(window, root) && ReferenceEquals(Engine.MainLoop, root.Tree) && probe.ReadyCalls == 1 && next == callbacks && probe.ProcessCalls == (int)next - 1, "The public boundary follows readiness and precedes every process step.");
                    if (next == 1)
                    {
                        Reject<InvalidOperationException>(() => Engine.AdvanceFrame(.1)); Reject<InvalidOperationException>(Engine.Stop);
                        Input.ParseInputEvent(press);
                    }
                    if (next == 4) Input.ParseInputEvent(release);
                    if (next == 7) root.Tree!.Quit();
                }
                var code = kind switch
                {
                    0 => Engine.Run(Before, root),
                    1 => Await(() => Engine.RunAsync(Before, root)),
                    2 => Engine.Run(Before, root, "capture.framehook", 60),
                    3 => Await(() => Engine.RunAsync(Before, root, "capture.framehook", 60)),
                    4 => Engine.RunMovie(Before, root),
                    _ => Await(() => Engine.RunMovieAsync(Before, root))
                };
                Check(code == 0 && callbacks == 7 && probe.ProcessCalls == 6 && probe.PressedFrames == 3 && probe.InputCalls == 2 && root.IsDisposed && Engine.MainLoop is null, "The quit boundary skips the pending step, preserves committed input and cleans up.");
                if (kind >= 2) Check(writer.Frames == 6 && writer.Ends == 1, "Fixed recording retains six real completed frames and balanced writer cleanup.");
            }
            for (var asynchronous = 0; asynchronous < 2; asynchronous++)
            {
                var root = new Window { Size = new(64, 64) };
                void Fail(Window _, ulong next) { Check(next == 1, "Every new run restarts host indices at one."); throw new ArithmeticException("before-frame fixture"); }
                Reject<ArithmeticException>(() => { if (asynchronous == 0) Engine.Run(Fail, root); else Await(() => Engine.RunAsync(Fail, root)); });
                Check(root.IsDisposed && Engine.MainLoop is null, "Throwing host callbacks release all owned native and scene state.");
            }
            Engine.MaxFPS = 60; var firstProcess = Engine.ProcessFrames;
            // Desktop decoration minima must not resize a prepared allocation-measurement target.
            var measuredRoot = new Window { Size = new(320, 96) }; long before = 0, allocated = 0; var samples = 0;
            Check(Engine.Run((window, next) =>
            {
                // Native configure events settle during paced warmup; all measured intervals remain uncapped.
                if (next == 25) Engine.MaxFPS = 0;
                var current = GC.GetAllocatedBytesForCurrentThread();
                if (next >= 26) { allocated += current - before; samples++; }
                before = current;
                if (next == 89) window.Tree!.Quit();
            }, measuredRoot) == 0 && samples == 64 && allocated == 0 && Engine.ProcessFrames - firstProcess == 88, $"24 warm + 64 complete host/input/scheduler/render intervals allocated {allocated} owner bytes.");
            Console.WriteLine($"Host frame boundary {backend}: all six overloads, first-frame state/delivery, 7 boundaries/6 steps, sync/async failure cleanup, restart and 64 warmed zero-byte intervals passed.");
        }
        finally { ProjectSettings.Set(ProjectSettings.MovieWriterFile, previousPath); ProjectSettings.Set(ProjectSettings.MovieWriterFPS, previousFPS); Input.ActionRelease(ActionName); }
    }
    private static int Await(Func<Task<int>> start)
    {
        var previous = SynchronizationContext.Current; var context = new MovieWriterTests.PumpContext(); SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            var task = start(); var clock = System.Diagnostics.Stopwatch.StartNew();
            while (!task.IsCompleted) { if (clock.Elapsed.TotalSeconds > 30) throw new TimeoutException("Native asynchronous frame boundary did not complete."); context.Pump(); }
            return task.GetAwaiter().GetResult();
        }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
    }
}
