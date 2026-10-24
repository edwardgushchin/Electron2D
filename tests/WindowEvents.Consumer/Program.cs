using Electron2D;

try
{
    Preflight();
    ProjectSettings.Set(ProjectSettings.RenderingMethod, args.FirstOrDefault() ?? "gpu");
    ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
    Engine.MaxFPS = 60;
    using var root = new Window { Size = new(96, 80), Borderless = true };
    var first = new Window { Size = new(96, 80), Borderless = true };
    var second = new Window { Size = new(96, 80), Borderless = true };
    var controller = new Controller(first, second);
    first.AddChild(new Paint(Colors.Red)); second.AddChild(new Paint(Colors.Blue));
    root.AddChild(first); root.AddChild(second); root.AddChild(controller);
    Engine.Run(root);
    Check(controller.Complete && root.IsDisposed && first.IsDisposed && second.IsDisposed && controller.Publishers.All(p => p.IsDisposed), "Native run retires every requested publisher and scene window.");
    Check(!DisplayServer.IsAvailable && !RenderingServer.IsAvailable, "Native services retire.");
    Console.WriteLine("Public window events consumer: native identities, isolated input/scene order, real resize, rendered child pixels, zero-allocation dispatch and hide/reopen retirement passed.");
}
catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }

static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

static void Preflight()
{
    using var display = DisplayServer.Open("Public input preflight", new(64, 64), hidden: true);
    var publisher = DisplayServer.GetWindowEvents(); var receiver = new InputReceiver();
    using var tree = new SceneTree(receiver); using var key = new InputEventKey { Keycode = Key.Q, PhysicalKeycode = Key.Q };
    Action<InputEvent> observe = value => { Check(ReferenceEquals(value, key) && Input.IsPhysicalKeyPressed(Key.Q) == key.Pressed && receiver.Phase == 0, "Public publisher sees committed state before the scene."); receiver.Phase = 1; };
    publisher.Input += observe; Engine.Start(tree);
    try
    {
        for (var i = 0; i < 64; i++) { key.Pressed = !key.Pressed; Input.ParseInputEvent(key); }
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 64; i++) { key.Pressed = !key.Pressed; Input.ParseInputEvent(key); }
        var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
        Check(receiver.Calls == 128 && ownerBytes == 0 && allBytes == 0, $"Public prepared input: owner={ownerBytes}, all={allBytes} managed bytes.");
        Console.WriteLine("Public input preflight: 64 prepared dispatches, zero owner/all-thread managed bytes.");
    }
    finally { publisher.Input -= observe; Engine.Stop(); }
}

internal sealed class InputReceiver : Node
{
    internal int Phase, Calls;
    internal InputReceiver() { InputEnabled = true; }
    protected override void OnInput(InputEvent value)
    { if (Phase != 1) throw new InvalidOperationException("Public window publisher must run before the scene."); Phase = 0; Calls++; }
}

internal sealed class Paint(Color ink) : Entity
{
    protected override void OnDraw() => DrawRect(new(0, 0, 64, 64), ink);
}

internal sealed class Controller(Window first, Window second) : Node
{
    private DisplayWindowEvents _first = null!, _second = null!;
    private int _frame, _rects;
    private Vector2i _initialSize;
    private Rect2i _lastRect;
    private int _retiredID;
    private bool _subscribed;
    internal readonly List<DisplayWindowEvents> Publishers = [];
    internal bool Complete;
    protected override void OnReady()
    {
        OS.LowProcessorUsageMode = false;
        _first = DisplayServer.GetWindowEvents(first.GetWindowID()); _second = DisplayServer.GetWindowEvents(second.GetWindowID());
        Publishers.Add(DisplayServer.GetWindowEvents()); Publishers.Add(_first); Publishers.Add(_second);
        Check(_first.WindowID > 0 && _second.WindowID > _first.WindowID && ReferenceEquals(_first, DisplayServer.GetWindowEvents(_first.WindowID)), "Public native IDs select stable independent event sources.");
        _initialSize = first.Size;
        _first.RectChanged += rect => { _rects++; _lastRect = rect; };
        RenderingServer.FramePostDraw += Tick; _subscribed = true;
    }
    private void Tick()
    {
        _frame++;
        if (_frame == 2)
        {
            first.Size = new(112, 88);
        }
        if (_frame == 10)
        {
            Check(_rects > 0 && _lastRect.Size != _initialSize && _lastRect.Size == first.Size && DisplayServer.WindowGetSize(_first.WindowID) == _lastRect.Size, $"A real native resize reaches its scoped rectangle callback: callbacks={_rects}, native={DisplayServer.WindowGetSize(_first.WindowID)}, scene={first.Size}, frame={_frame}.");
            Pixel(first, Colors.Red); Pixel(second, Colors.Blue);
        }
        if (_frame == 12)
        {
            _retiredID = _first.WindowID; first.Hide();
            Check(_first.IsDisposed && !_second.IsDisposed, "Hiding the native child retires only its publisher.");
        }
        if (_frame == 14)
        {
            first.Show(); var replacement = DisplayServer.GetWindowEvents(first.GetWindowID());
            Check(replacement.WindowID > _retiredID && !ReferenceEquals(replacement, _first), "Reopening creates a new native ID and event publisher.");
            _first = replacement; Publishers.Add(replacement);
        }
        if (_frame == 18)
        { Pixel(first, Colors.Red); Pixel(second, Colors.Blue); Complete = true; Tree!.Quit(); }
    }
    private static void Pixel(Window window, Color expected)
    {
        using var image = window.GetTexture().GetImage()!; var actual = image.GetPixel(16, 16);
        Check(MathF.Abs(actual.R - expected.R) < .01f && MathF.Abs(actual.G - expected.G) < .01f && MathF.Abs(actual.B - expected.B) < .01f, "Each real renderer produces the expected child canvas pixels.");
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { if (_subscribed && RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Tick; }
        base.Dispose(disposing);
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
