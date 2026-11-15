using Electron2D;

internal static partial class ControlTargetContracts
{
    internal static void RunNative(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var window = new Window { Size = new(128, 112), Title = "Control target contracts" };
        var container = new SubViewportContainer { Position = new(40, 20), Size = new(64, 64), Stretch = true, StretchShrink = 2 };
        var nested = new SubViewport { Size = new(32, 32) };
        var parent = new Control { Size = new(32, 32), MouseDefaultCursorShape = CursorShape.Help };
        var child = new NativePointer { Position = new(2, 2), Size = new(10, 10), MouseFilter = MouseFilter.Pass };
        window.AddChild(container); container.AddChild(nested); nested.AddChild(parent); parent.AddChild(child);
        var finished = false;
        long ownerBefore = 0, allBefore = 0;
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Requested renderer executes without fallback.");
            RenderingServer.RenderLoopEnabled = false;
            child.WarpMouse(new(3, 4));
        };
        void Cursor(CursorShape expected) => Check(Input.GetCurrentCursorShape() == expected,
            $"Native cursor: expected {expected}, actual {Input.GetCurrentCursorShape()}.");
        Check(Engine.Run((host, frame) =>
        {
            switch (frame)
            {
                case 3:
                    Check(child.Motions > 0 && child.Last.DistanceTo(new(3, 4)) < 1, "Native pointer reaches the scaled nested control in local coordinates.");
                    Check(child.LastCursor.DistanceTo(new(3, 4)) < 1, "Nested cursor override receives control-local coordinates.");
                    Cursor(CursorShape.Help);
                    using (var touch = new InputEventScreenTouch { Index = 7, Pressed = true, Position = new(50, 32) }) window.PushInput(touch, true);
                    using (var drag = new InputEventScreenDrag { Index = 7, Position = new(200, 200) }) window.PushInput(drag, true);
                    using (var touch = new InputEventScreenTouch { Index = 7, Position = new(200, 200) }) window.PushInput(touch, true);
                    Check(child.Touches == 3 && child.LastTouch == new Vector2(78, 88), "Embedded touch capture retains outside drag/release and localizes both viewport transforms.");
                    RenderingServer.ForceDraw(false);
                    using (var image = window.GetTexture().GetImage())
                    {
                        Check(image is not null && image.GetPixel(50, 32).G > .9f, "Actual nested pointer geometry produces green pixels.");
                        if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } folder)
                            image!.SavePNG(System.IO.Path.Combine(folder, $"control-target-{backend}.png"));
                    }
                    parent.MouseDefaultCursorShape = CursorShape.Cross; Cursor(CursorShape.Cross);
                    child.MouseDefaultCursorShape = CursorShape.Wait; Cursor(CursorShape.Wait);
                    child.MouseDefaultCursorShape = CursorShape.Arrow;
                    child.MouseFilter = MouseFilter.Stop; Cursor(CursorShape.Arrow);
                    child.MouseFilter = MouseFilter.Pass; Cursor(CursorShape.Cross);
                    child.ReenterCursor = true;
                    child.MouseDefaultCursorShape = CursorShape.Help;
                    child.ReenterCursor = false;
                    Check(child.CursorCalls == 1, "Cursor property changes inside cursor lookup cannot recursively query again.");
                    child.FailCursor = true;
                    try { child.MouseDefaultCursorShape = CursorShape.Wait; throw new Exception("Expected cursor callback failure."); }
                    catch (AggregateException) { }
                    child.FailCursor = false; child.MouseDefaultCursorShape = CursorShape.Arrow; Cursor(CursorShape.Cross);
                    child.TopLevel = true;
                    break;
                case 4: Cursor(CursorShape.Arrow); child.TopLevel = false; parent.Position = new(20, 0); break;
                case 5: Cursor(CursorShape.Arrow); parent.Position = Vector2.Zero; break;
                case 6: Cursor(CursorShape.Cross); parent.Visible = false; break;
                case 7: Cursor(CursorShape.Arrow); parent.Visible = true; break;
                case 8:
                    Cursor(CursorShape.Cross);
                    container.MouseTarget = true; Cursor(CursorShape.Arrow);
                    container.MouseTarget = false; Cursor(CursorShape.Cross);
                    child.Dispose();
                    break;
                case 9: Cursor(CursorShape.Cross); parent.Visible = false; break;
                case 10: Cursor(CursorShape.Arrow); parent.Visible = true; break;
                case 32: ownerBefore = GC.GetAllocatedBytesForCurrentThread(); allBefore = GC.GetTotalAllocatedBytes(true); break;
                case 160:
                case 320:
                    var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - ownerBefore;
                    var allBytes = GC.GetTotalAllocatedBytes(true) - allBefore;
                    Check(ownerBytes == 0 && allBytes == 0, $"Native prepared frames allocated {ownerBytes}/{allBytes} owner/all-thread bytes.");
                    Console.WriteLine($"Native hover {backend}: 128 {(frame == 160 ? "idle" : "active")} frames at {ownerBytes}/{allBytes} owner/all-thread bytes.");
                    if (frame == 320) { finished = true; host.Tree!.Quit(); }
                    break;
                case 192: ownerBefore = GC.GetAllocatedBytesForCurrentThread(); allBefore = GC.GetTotalAllocatedBytes(true); break;
            }
            if (frame is >= 161 and < 320) parent.Position = frame % 2 == 0 ? new(20, 0) : Vector2.Zero;
            if (frame > 340) throw new TimeoutException("Native target profile did not finish.");
        }, window) == 0 && finished, "Native host finishes and releases its scene.");
        Console.WriteLine($"Control target native {backend}: scaled input, pixels, nested cursor fallback/refresh, Stop/Pass, TopLevel, stationary move/hide/show and disposal passed.");
    }

    private sealed class NativePointer : Control
    {
        internal int Motions;
        internal int CursorCalls;
        internal bool ReenterCursor, FailCursor;
        internal Vector2 Last;
        internal Vector2 LastCursor, LastTouch;
        internal int Touches;
        protected override CursorShape OnGetCursorShape(Vector2 position)
        {
            LastCursor = position;
            if (FailCursor) throw new InvalidOperationException("Cursor callback failure.");
            if (ReenterCursor)
            {
                if (++CursorCalls > 20) throw new InvalidOperationException("Cursor lookup recursed.");
                MouseDefaultCursorShape = MouseDefaultCursorShape == CursorShape.Cross ? CursorShape.Help : CursorShape.Cross;
                return CursorShape.Arrow;
            }
            return base.OnGetCursorShape(position);
        }
        protected override void OnGUIInput(InputEvent input)
        {
            if (input is InputEventMouseMotion motion) { Motions++; Last = motion.Position; QueueRedraw(); }
            if (input is InputEventScreenTouch touch) { Touches++; LastTouch = touch.Position; }
            if (input is InputEventScreenDrag drag) { Touches++; LastTouch = drag.Position; }
        }
        protected override void OnDraw() => DrawRect(new(Vector2.Zero, Size), Motions == 0 ? Colors.Red : Colors.Green);
    }
}
