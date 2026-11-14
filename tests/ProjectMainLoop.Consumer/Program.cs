using System.Diagnostics;
using System.Runtime.CompilerServices;
using Electron2D;
using Path = System.IO.Path;

var backend = args[0];
if (args.Length > 1) Check(!RuntimeFeature.IsDynamicCodeSupported, "Expected a NativeAOT process.");
ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
ProjectSettings.Set(ProjectSettings.MainScene, "res://missing.e2dscene");
ProjectSettings.Set(ProjectSettings.MainLoopType, nameof(ProjectLoop));
Engine.MaxFPS = 60;

Reject<ArgumentNullException>(() => Engine.RegisterMainLoop<ProjectLoop>(null!));
Reject<ArgumentException>(() => Engine.RegisterMainLoop(CreatePlain));
Reject<ArgumentException>(() => Engine.RegisterMainLoop<AbstractLoop>(Create));
Reject<ArgumentException>(() => Engine.RegisterMainLoop(CreateGeneric));
Reject<ArgumentException>(() => Engine.RegisterMainLoop(new CapturingFactory().Create));
Func<ProjectLoop> multicast = Create;
multicast += Create;
Reject<ArgumentException>(() => Engine.RegisterMainLoop(multicast));

using (var unregistered = NewWindow())
{
    Reject<InvalidOperationException>(() => Engine.RunProject(unregistered));
    Retired(unregistered);
}
Engine.RegisterMainLoop(Create);
Reject<InvalidOperationException>(() => Engine.RegisterMainLoop(Create));
Reject<InvalidOperationException>(() => Engine.RegisterMainLoop(CreateOther));
ProjectSettings.Set(ProjectSettings.MainLoopType, "projectloop");
using (var wrongCase = NewWindow())
{
    Reject<InvalidOperationException>(() => Engine.RunProject(wrongCase));
    Retired(wrongCase);
}
ProjectSettings.Set(ProjectSettings.MainLoopType, nameof(ProjectLoop));

using var borrowed = new ProjectLoop();
using (var window = NewWindow())
{
    ProjectLoop.Borrowed = borrowed;
    try { Reject<InvalidOperationException>(() => Engine.RunProject(window)); }
    finally { ProjectLoop.Borrowed = null; }
    Check(!borrowed.IsDisposed, "The startup factory does not acquire a pre-existing loop.");
    Retired(window);
}
using (var window = NewWindow())
{
    ProjectLoop.ReturnWrongType = true;
    try { Reject<InvalidOperationException>(() => Engine.RunProject(window)); }
    finally { ProjectLoop.ReturnWrongType = false; }
    Check(ProjectLoop.Last is DerivedLoop { IsDisposed: true }, "An invalid fresh result is retired.");
    Retired(window);
}
foreach (var stage in new[] { "initialize", "process", "finalize" })
{
    using var window = NewWindow();
    ProjectLoop.Fail = stage;
    try { Reject<ArithmeticException>(() => Engine.RunProject(window)); }
    finally { ProjectLoop.Fail = null; }
    var loop = ProjectLoop.Last;
    Check(loop is { IsDisposed: true } && loop.Finalized == (stage == "initialize" ? 0 : 1),
        $"The failed {stage} loop retires with the owned lifecycle (disposed: {loop?.IsDisposed}, finalized: {loop?.Finalized}).");
    Retired(window);
}

foreach (var asynchronous in new[] { false, true })
{
    using var window = NewWindow();
    using var texture = window.GetTexture();
    ProjectLoop.Texture = texture;
    try { Check(Run(window, null, asynchronous) == 0, "The configured loop stops normally."); }
    finally { ProjectLoop.Texture = null; }
    Check(ProjectLoop.Last is { IsDisposed: true, Finalized: 1, SawPixels: true }, "A fresh loop drew and retired.");
    Retired(window);

    using var directory = DirAccess.CreateTemp();
    using var recordingWindow = NewWindow();
    var movie = Path.Combine(directory.GetCurrentDir(), "loop.png");
    Check(Run(recordingWindow, movie, asynchronous) == 0, "The configured recording stops normally.");
    using var frame = Image.LoadFromFile(Path.Combine(directory.GetCurrentDir(), "loop00000002.png"));
    Check(frame.GetPixel(5, 5).IsEqualApprox(Colors.Red) &&
        new FileInfo(Path.Combine(directory.GetCurrentDir(), "loop.wav")).Length > 44,
        "The recorded frame and finalized audio contain real output.");
    Retired(recordingWindow);
}

Console.WriteLine($"Configured compiled loop passed ({backend}, dynamic code: {RuntimeFeature.IsDynamicCodeSupported}).");

static ProjectLoop Create() => ProjectLoop.Borrowed ?? (ProjectLoop.ReturnWrongType ? new DerivedLoop() : new ProjectLoop());
static PlainLoop CreatePlain() => new();
static GenericLoop<int> CreateGeneric() => new();
static Other.ProjectLoop CreateOther() => new();
static Window NewWindow()
{
    var window = new Window { Size = new(64, 48), ContentScaleMode = ContentScaleMode.Viewport, ContentScaleSize = new(64, 48) };
    ProjectLoop.WindowRID = window.GetViewportRID();
    window.Ready += static _ => throw new InvalidOperationException("A custom loop does not activate a scene tree.");
    return window;
}

static int Run(Window window, string? movie, bool asynchronous)
{
    if (!asynchronous) return movie is null ? Engine.RunProject(window) : Engine.RunProject(window, movie, 60);
    var previous = SynchronizationContext.Current;
    using var context = new PumpContext();
    SynchronizationContext.SetSynchronizationContext(context);
    try
    {
        var task = movie is null ? Engine.RunProjectAsync(window) : Engine.RunProjectAsync(window, movie, 60);
        var start = Stopwatch.GetTimestamp();
        while (!task.IsCompleted)
        {
            if (Stopwatch.GetElapsedTime(start) > TimeSpan.FromSeconds(15)) throw new TimeoutException();
            context.Pump();
        }
        return task.GetAwaiter().GetResult();
    }
    finally { SynchronizationContext.SetSynchronizationContext(previous); }
}

static void Retired(Window window) => Check(window.IsDisposed && Engine.MainLoop is null &&
    !DisplayServer.IsAvailable && !RenderingServer.IsAvailable && Engine.GetWriteMoviePath() == "", "Owned host resources retire.");

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static void Reject<T>(Action operation) where T : Exception
{
    try { operation(); }
    catch (T) { return; }
    throw new InvalidOperationException("Expected " + typeof(T).Name);
}

[GlobalClass]
internal class ProjectLoop : AbstractLoop
{
    internal static ProjectLoop? Last, Borrowed;
    internal static bool ReturnWrongType;
    internal static string? Fail;
    internal static ViewportTexture? Texture;
    internal static RID WindowRID;
    internal bool SawPixels;
    internal int Finalized;
    private RID _canvas, _item;
    private int _frames;

    internal ProjectLoop() => Last = this;

    protected override void OnInitialize()
    {
        if (!ReferenceEquals(this, Engine.MainLoop) || GetScript() is not null)
            throw new InvalidOperationException("Expected the registered source-free loop to be published.");
        if (Fail == "initialize") throw new ArithmeticException();
        _canvas = RenderingServer.CanvasCreate();
        _item = RenderingServer.CanvasItemCreate();
        RenderingServer.ViewportAttachCanvas(WindowRID, _canvas);
        RenderingServer.CanvasItemSetParent(_item, _canvas);
        RenderingServer.CanvasItemAddRect(_item, new(2, 2, 12, 12), Colors.Red);
    }

    protected override bool OnProcess(double delta)
    {
        if (Fail == "process") throw new ArithmeticException();
        if (_frames > 0 && Texture is { } texture)
        {
            using var image = texture.GetImage();
            SawPixels |= image?.GetPixel(5, 5).IsEqualApprox(Colors.Red) == true;
        }
        return ++_frames == 3;
    }

    protected override void OnFinalize()
    {
        Finalized++;
        RenderingServer.FreeRID(_item);
        RenderingServer.FreeRID(_canvas);
        if (Fail == "finalize") throw new ArithmeticException();
    }
}

[GlobalClass]
internal abstract class AbstractLoop : MainLoop;
internal sealed class PlainLoop : MainLoop;
internal sealed class DerivedLoop : ProjectLoop;
[GlobalClass]
internal sealed class GenericLoop<T> : MainLoop;

internal sealed class CapturingFactory
{
    internal ProjectLoop Create() => new();
}

internal sealed class PumpContext : SynchronizationContext, IDisposable
{
    private readonly System.Collections.Concurrent.BlockingCollection<(SendOrPostCallback Callback, object? State)> _pending = new();
    public override void Post(SendOrPostCallback callback, object? state) => _pending.Add((callback, state));
    internal void Pump() { if (_pending.TryTake(out var item, 10)) item.Callback(item.State); }
    public void Dispose() => _pending.Dispose();
}

namespace Other
{
    [GlobalClass]
    internal sealed class ProjectLoop : MainLoop;
}
