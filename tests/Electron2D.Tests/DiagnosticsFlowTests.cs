using Electron2D;
using System.Collections.Concurrent;
using EOs = Electron2D.OS;
using Path = System.IO.Path;

internal static class DiagnosticsFlowTests
{
    private sealed class Receiver : Logger
    {
        internal readonly ConcurrentQueue<string> Messages = new();
        internal readonly ConcurrentQueue<(string Code, string Function, ScriptBacktrace[] Traces)> Errors = new();
        internal bool ThrowErrors;
        protected override void OnLogMessage(string message, bool error) => Messages.Enqueue(message);
        protected override void OnLogError(string function, string file, int line, string code, string rationale, bool editorNotify, LoggerErrorType type, ReadOnlySpan<ScriptBacktrace> traces)
        {
            Errors.Enqueue((code, function, traces.ToArray()));
            if (ThrowErrors) throw new InvalidOperationException("receiver failure during engine cleanup");
        }
    }
    internal static void Run(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var receiver = new Receiver(); EOs.AddLogger(receiver);
        using var window = new Window { Size = new(40, 40) }; var canvas = new Entity(); window.AddChild(canvas);
        canvas.Draw += item => item.DrawRect(new(10, 10, 12, 12), Colors.Lime);
        using var target = window.GetTexture(); var frames = 0; var seen = false; long before = 0, allocated = -1;
        window.Ready += _ => { EOs.LogMessage("ready diagnostic"); Task.Run(() => EOs.LogMessage("worker diagnostic")).GetAwaiter().GetResult(); RenderingServer.FramePostDraw += Capture; };
        void Capture()
        {
            frames++;
            if (frames == 4)
            {
                using var image = target.GetImage()!; var p = image.GetPixel(15, 15);
                if (p.G < .98f || p.R > .02f || p.B > .02f) throw new InvalidOperationException("Diagnostic host did not render the authored pixel.");
                if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } folder) { Directory.CreateDirectory(folder); image.SavePNG(Path.Combine(folder, "diagnostics-" + backend + ".png")); }
                seen = true;
            }
            if (frames == 8) before = GC.GetAllocatedBytesForCurrentThread();
            if (frames == 72)
            {
                allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit();
            }
        }
        try { if (Engine.Run(window) != 0 || !seen || frames != 72 || allocated != 0) throw new InvalidOperationException("Diagnostic prepared full-frame allocation bytes=" + allocated); }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Capture; }
        if (!receiver.Messages.Contains("ready diagnostic") || !receiver.Messages.Contains("worker diagnostic")) throw new InvalidOperationException("Native host/worker messages were not delivered.");
        var initialErrors = receiver.Errors.Count;
        var failure = new InvalidOperationException("native window ready failure"); var badWindow = new Window { Size = new(40, 40) };
        badWindow.Ready += _ => throw failure; receiver.ThrowErrors = true;
        Exception? caught = null;
        try { Engine.Run(badWindow); } catch (Exception error) { caught = error; }
        if (caught is not AggregateException aggregate || !aggregate.Flatten().InnerExceptions.Any(error => ReferenceEquals(error, failure)) || receiver.Errors.Count != initialErrors + 1 || Engine.MainLoop is not null || RenderingServer.IsAvailable) throw new InvalidOperationException("Native failure identity, exactly-once delivery or cleanup changed.");
        var snapshot = receiver.Errors.Last();
        if (!snapshot.Code.Contains(nameof(AggregateException)) || snapshot.Function.Length == 0 || snapshot.Traces.Length != 1 || snapshot.Traces[0].IsEmpty()) throw new InvalidOperationException("Native failure omitted its C# metadata.");
        Console.WriteLine($"Diagnostics {backend}: retained native host/worker delivery, actual pixel, 64 warmed full frames ({allocated} B), exactly-once failure/throwing logger and cleanup passed.");
    }
}
