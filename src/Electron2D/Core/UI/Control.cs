using System;

namespace Electron2D;

#region Control

/// <summary>
/// Базовый UI-узел (аналог Godot Control).
/// На текущем этапе события GUI направляются только в FocusedControl (без hit-test).
/// </summary>
public class Control(string name) : Node(name)
{
    #region Properties

    /// <summary>
    /// True, если именно этот <see cref="Control"/> сейчас в фокусе <see cref="SceneTree"/>.
    /// </summary>
    public bool HasFocus => SceneTree is not null && ReferenceEquals(SceneTree.FocusedControl, this);

    #endregion

    #region Public API

    /// <summary>
    /// Сделать этот <see cref="Control"/> сфокусированным (keyboard/gamepad/mouse уйдут сюда в GUI-фазе).
    /// </summary>
    public void GrabFocus()
    {
        var sceneTree = SceneTree;
        if (sceneTree is null)
            return;

        sceneTree.SetFocusedControl(this);
    }

    /// <summary>
    /// Снять фокус (если он был на этом <see cref="Control"/>).
    /// </summary>
    public void ReleaseFocus()
    {
        var sceneTree = SceneTree;
        if (sceneTree is null)
            return;

        if (ReferenceEquals(sceneTree.FocusedControl, this))
            sceneTree.SetFocusedControl(null);
    }

    #endregion

    #region Protected API

    /// <summary>
    /// Аналог accept_event() в Godot: помечает текущее <see cref="InputEvent"/> обработанным.
    /// </summary>
    protected void AcceptEvent() => SetInputHandled();

    /// <summary>
    /// Аналог _gui_input(event) в Godot.
    /// </summary>
    /// <param name="inputEvent">Входное событие.</param>
    /*protected virtual void HandleGUIInput(InputEvent inputEvent)
    {
    }*/

    #endregion

    #region Internal helpers

    //internal void InternalGUIInput(InputEvent inputEvent) => HandleGUIInput(inputEvent);

    #endregion
}

#endregion