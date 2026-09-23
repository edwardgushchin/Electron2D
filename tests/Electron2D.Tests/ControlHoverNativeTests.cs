using Electron2D;
using SDL3;

internal static class ControlHoverNativeTests
{
    internal static void Run()
    {
        var window = new Window { Title = "Control cursor check", Size = new(96, 64) };
        var control = new Control { Size = new(80, 50), MouseDefaultCursorShape = Control.CursorShape.Help };
        window.AddChild(control);
        window.AddChild(new Probe(control));
        Check(Engine.Instance.Run(window) == 0, "The native cursor scene exits normally.");
        Check(DisplayServer.Instance is null, "The native cursor scene releases its display.");
        Console.WriteLine("Native Control hover and cursor precedence passed.");
    }

    private sealed class Probe(Control control) : Node
    {
        protected override void OnReady()
        {
            ProcessEnabled = true;
            var windows = SDL.GetWindows(out var count);
            Check(count == 1 && windows is { Length: 1 }, "One native window is active.");
            var motion = new SDL.Event
            {
                Motion = new SDL.MouseMotionEvent
                {
                    Type = SDL.EventType.MouseMotion,
                    WindowID = SDL.GetWindowID(windows![0]),
                    X = 10,
                    Y = 10,
                },
            };
            Check(SDL.PushEvent(ref motion), "The backend accepts a pointer motion event.");
        }

        protected override void OnProcess(double delta)
        {
            Check(Input.Instance.GetCurrentCursorShape() == Input.CursorShape.Help,
                "The hovered control selects its native cursor.");
            Input.Instance.SetDefaultCursorShape(Input.CursorShape.IBeam);
            Check(Input.Instance.GetCurrentCursorShape() == Input.CursorShape.Help,
                "A hovered control overrides the changed viewport default.");
            control.MouseFilter = ControlMouseFilter.Ignore;
            Check(Input.Instance.GetCurrentCursorShape() == Input.CursorShape.IBeam,
                "Ignoring the control exposes the retained viewport default.");
            control.MouseFilter = ControlMouseFilter.Stop;
            Check(Input.Instance.GetCurrentCursorShape() == Input.CursorShape.Help,
                "Restoring hit testing refreshes the native cursor without pointer motion.");
            control.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
            Check(Input.Instance.GetCurrentCursorShape() == Input.CursorShape.PointingHand,
                "Changing a hovered control's cursor refreshes the native shape.");
            Input.Instance.SetDefaultCursorShape();
            Tree!.Quit();
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
