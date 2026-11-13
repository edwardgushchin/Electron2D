using System.Runtime.CompilerServices;
using Electron2D;
using SDL3;

[GlobalClass]
internal class ProjectLoopProbe : MainLoop
{
    internal static ProjectLoopProbe? Last;
    internal static string? Fail;
    private static Window _window = null!;
    private static ViewportTexture? _texture;
    private static bool _close;
    private static int _owner;
    internal readonly Script? Source;
    internal int Drawn, Physics, Finalized;
    private int _process;

    internal static readonly string SourcePath = GetSourcePath();
    private static string GetSourcePath([CallerFilePath] string path = "") => path;

    internal ProjectLoopProbe()
    {
        Source = GetScript();
        Last = this;
    }

    internal static void Reset(Window window, ViewportTexture? texture = null, bool close = false)
    {
        (_window, _texture, _close, _owner) = (window, texture, close, Environment.CurrentManagedThreadId);
        Fail = null;
        Last = null;
    }

    protected override void OnInitialize()
    {
        CheckOwner();
        if (Fail == "initialize") throw new ArithmeticException(Fail);
        var canvas = RenderingServer.CanvasCreate();
        var item = RenderingServer.CanvasItemCreate();
        RenderingServer.CanvasItemSetParent(item, canvas);
        RenderingServer.ViewportAttachCanvas(_window.GetViewportRID(), canvas);
        RenderingServer.CanvasItemAddRect(item, new(2, 2, 12, 12), Colors.Red);
        if (_texture is not null)
        {
            var offscreen = RenderingServer.ViewportCreate();
            RenderingServer.ViewportSetSize(offscreen, 16, 16);
            RenderingServer.ViewportSetActive(offscreen, true);
            RenderingServer.ViewportSetUpdateMode(offscreen, ViewportUpdateMode.Always);
            RenderingServer.ViewportAttachCanvas(offscreen, canvas);
            RenderingServer.ForceDraw(swapBuffers: false);
            using (var pixels = RenderingServer.Texture2DGet(RenderingServer.ViewportGetTexture(offscreen)))
                ProjectMainLoopTests.Check(pixels?.GetPixel(5, 5).IsEqualApprox(Colors.Red) == true, "The custom loop draws an offscreen viewport.");
            RenderingServer.FreeRID(offscreen);
            CheckDrawAllocations(item);
        }
        RenderingServer.FramePostDraw += () =>
        {
            CheckOwner();
            if (Drawn == 0)
            {
                ProjectMainLoopTests.Reject<InvalidOperationException>(FinalizeLoop);
                ProjectMainLoopTests.Reject<InvalidOperationException>(Dispose);
            }
            if (_texture is not null)
            {
                using var image = _texture.GetImage();
                ProjectMainLoopTests.Check(image?.GetPixel(5, 5).IsEqualApprox(Colors.Red) == true, "The custom loop submits real canvas pixels.");
            }
            Drawn++;
        };
        RenderingServer.ForceDraw(swapBuffers: false);
    }

    private static void CheckDrawAllocations(RID item)
    {
        static void Draw(RID item, bool change)
        {
            if (change)
            {
                RenderingServer.CanvasItemClear(item);
                RenderingServer.CanvasItemAddRect(item, new(2, 2, 12, 12), Colors.Red);
            }
            RenderingServer.ForceDraw(swapBuffers: false);
        }

        foreach (var change in new[] { true, false })
        {
            for (var i = 0; i < 24; i++) Draw(item, change);
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++) Draw(item, change);
            var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            ProjectMainLoopTests.Check(bytes == 0, $"Custom canvas draw allocated {bytes} bytes with changes={change}.");
        }
    }

    protected override bool OnPhysicsProcess(double delta)
    {
        CheckOwner();
        Physics++;
        return false;
    }

    protected override bool OnProcess(double delta)
    {
        CheckOwner();
        if (Fail == "process") throw new ArithmeticException(Fail);
        if (++_process == 1) Thread.Sleep(32);
        if (_process != 6) return false;
        if (!_close) return true;
        var close = new SDL.Event { Type = (uint)SDL.EventType.Quit };
        ProjectMainLoopTests.Check(SDL.PushEvent(ref close), "Queue the real SDL event pump's quit event.");
        return false;
    }

    protected override void OnFinalize()
    {
        CheckOwner();
        ProjectMainLoopTests.Check(!_window.IsDisposed && DisplayServer.IsAvailable && RenderingServer.IsAvailable &&
            (Source is null || !Source.IsDisposed), "Native services and source outlive loop finalization.");
        Finalized++;
        if (Fail == "finalize") throw new ArithmeticException(Fail);
    }

    private void CheckOwner() => ProjectMainLoopTests.Check(ReferenceEquals(Engine.MainLoop, this) &&
        _window.Tree is null && Environment.CurrentManagedThreadId == _owner, "The custom loop is published on the native owner thread.");
}
