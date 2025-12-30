namespace Electron2D;

/// <summary>
/// Базовый UI-узел (аналог Godot Control).
/// На текущем этапе события GUI направляются только в FocusedControl (без hit-test).
/// </summary>
public class Control(string name) : Node(name)
{
    /// <summary>
    /// True если именно этот Control сейчас в фокусе SceneTree.
    /// </summary>
    public bool HasFocus => SceneTree is not null && ReferenceEquals(SceneTree.FocusedControl, this);

    /// <summary>
    /// Сделать этот Control сфокусированным (keyboard/gamepad/mouse уйдут сюда в GUI-фазе).
    /// </summary>
    public void GrabFocus()
    {
        var t = SceneTree;
        if (t is null) return;
        t.SetFocusedControl(this);
    }

    /// <summary>
    /// Снять фокус (если он был на этом Control).
    /// </summary>
    public void ReleaseFocus()
    {
        var t = SceneTree;
        if (t is null) return;
        if (ReferenceEquals(t.FocusedControl, this))
            t.SetFocusedControl(null);
    }

    /// <summary>
    /// Аналог accept_event() в Godot: помечает текущее InputEvent обработанным.
    /// </summary>
    protected void AcceptEvent() => SetInputHandled();

    /// <summary>
    /// Аналог _gui_input(event) в Godot.
    /// </summary>
    protected virtual void HandleGUIInput(InputEvent inputEvent) { }

    internal void InternalGUIInput(InputEvent inputEvent) => HandleGUIInput(inputEvent);
}