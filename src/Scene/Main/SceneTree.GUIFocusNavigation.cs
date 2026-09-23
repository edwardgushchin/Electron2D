namespace Electron2D;

public sealed partial class SceneTree
{
    private void NavigateGUIFocus(Viewport viewport, InputEvent inputEvent)
    {
        if (inputEvent is not InputEventJoypadMotion && !inputEvent.IsPressed()) return;
        var from = _guiFocus;
        if (from is null)
            for (var index = 0; index < viewport.ChildCount; index++)
                if (viewport.GetChild(index) is Control { TopLevel: false, IsVisibleInTree: true } control)
                { from = control; break; }
        if (from is null) return;

        var map = InputMap.Instance;
        var analog = inputEvent is InputEventJoypadMotion;
        bool Pressed(string action) => map.HasAction(action) && inputEvent.IsActionPressed(action, allowEcho: true, exactMatch: !analog) &&
            (!analog || Input.Instance.IsActionJustPressedByEvent(action, inputEvent));

        Control? next = null;
        var requested = false;
        if (Pressed("ui_focus_next")) { next = from.FindNextValidFocus(); requested = true; }
        else if (Pressed("ui_focus_prev")) { next = from.FindPrevValidFocus(); requested = true; }
        else if (Pressed("ui_up")) { next = from.FindValidFocusNeighbor(Side.Top); requested = true; }
        else if (Pressed("ui_left")) { next = from.FindValidFocusNeighbor(Side.Left); requested = true; }
        else if (Pressed("ui_right")) { next = from.FindValidFocusNeighbor(Side.Right); requested = true; }
        else if (Pressed("ui_down")) { next = from.FindValidFocusNeighbor(Side.Bottom); requested = true; }
        if (!requested) return;
        if (next is not null)
        {
            SetGUIFocus(next, hideFocus: false);
            SetInputAsHandled();
        }
        else if (_guiFocusHidden && _guiFocus is { } focused)
            SetGUIFocus(focused, hideFocus: false);
    }
}
