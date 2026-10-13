namespace Electron2D;

public sealed partial class SceneTree
{
    internal void GrabGUIClickFocus(Viewport viewport, Control control)
    {
        EnsureOwnerThread(); EnsureAcceptingWork();
        var state = GUIState(viewport);
        (state.ClickGrabber ??= new(null!)).SetTarget(control);
        if (state.ClickGrabQueued) return;
        state.ClickGrabAction ??= CreateClickGrabAction(state);
        state.ClickGrabQueued = true;
        try { Defer(state.ClickGrabAction); } catch { state.ClickGrabQueued = false; state.ClickGrabber.SetTarget(null!); throw; }
    }
    private Action CreateClickGrabAction(ViewportGUIState state) => () => TransferGUIClickFocus(state);
    private Action CreateClickPressAction(ViewportGUIState state) => () => DeliverClickCapturePresses(state);
    private bool ClickControlLive(Control control, Viewport viewport) =>
        !control.IsDisposed && control.IsInsideTree && ReferenceEquals(control.Tree, this) && ReferenceEquals(control.GetViewport(), viewport);

    private void TransferGUIClickFocus(ViewportGUIState state)
    {
        state.ClickGrabQueued = false;
        if (!state.ClickGrabber!.TryGetTarget(out var target)) return;
        state.ClickGrabber.SetTarget(null!);
        if (state.Viewport is not { IsDisposed: false, GUIDisableInput: false } viewport || !ReferenceEquals(viewport.Tree, this) || !ClickControlLive(target, viewport)) return;
        using var scope = SelectGUI(viewport);
        var previous = state.GuiMouseCapture;
        if (previous is null || ReferenceEquals(previous, target) || !ClickControlLive(previous, viewport)) return;
        var mask = state.GuiMouseCaptureMask;
        var point = state.GuiHoverPosition;
        _ = previous.MakeCanvasPositionLocal(point); _ = target.MakeCanvasPositionLocal(point);
        List<Exception>? errors = null;
        for (var button = 0; button < 3; button++)
            if ((mask & (1u << button)) != 0 && ClickControlLive(previous, viewport))
                try { SendClickCaptureEvent(previous, point, button, pressed: false); }
                catch (Exception error) { CollectException(ref errors, error); }
        if ((ReferenceEquals(state.GuiMouseCapture, previous) || state.GuiMouseCapture is null && !ClickControlLive(previous, viewport)) && ClickControlLive(target, viewport))
        {
            state.GuiMouseCapture = target; state.GuiMouseCaptureMask = mask;
            state.ClickPressTarget ??= new(null!); state.ClickPressTarget.SetTarget(target);
            state.ClickPressMask = mask & 7; state.ClickPressPosition = point;
            if (!state.ClickPressQueued && state.ClickPressMask != 0)
            {
                state.ClickPressAction ??= CreateClickPressAction(state);
                state.ClickPressQueued = true; Defer(state.ClickPressAction);
            }
        }
        ThrowCollected("Click capture release callbacks failed.", errors);
    }
    private void DeliverClickCapturePresses(ViewportGUIState state)
    {
        state.ClickPressQueued = false;
        var mask = state.ClickPressMask; var point = state.ClickPressPosition; state.ClickPressMask = 0;
        if (state.ClickPressTarget is not { } weak || !weak.TryGetTarget(out var target)) return;
        state.ClickPressTarget.SetTarget(null!);
        if (state.Viewport is not { IsDisposed: false, GUIDisableInput: false } viewport || !ReferenceEquals(viewport.Tree, this)) return;
        using var scope = SelectGUI(viewport);
        List<Exception>? errors = null;
        for (var button = 0; button < 3; button++)
            if ((mask & state.GuiMouseCaptureMask & (1u << button)) != 0 && ReferenceEquals(state.GuiMouseCapture, target) && ClickControlLive(target, viewport))
                try { SendClickCaptureEvent(target, point, button, pressed: true); }
                catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Click capture press callbacks failed.", errors);
    }
    private static void SendClickCaptureEvent(Control target, Vector2 point, int button, bool pressed)
    {
        if (!target.CanProcess()) return;
        var position = target.MakeCanvasPositionLocal(point);
        if (!position.IsFinite()) throw new InvalidOperationException("Click capture transform exceeds finite coordinates.");
        using var input = new InputEventMouseButton { Device = InputEvent.DeviceIdInternal, Position = position, ButtonIndex = (MouseButton)(button + 1), Pressed = pressed };
        target.DispatchGUIInput(input);
    }
}
