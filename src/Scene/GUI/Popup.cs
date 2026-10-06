namespace Electron2D;

/// <summary>A transient embedded window that hides after cancellation, outside clicks or parent focus.</summary>
/// <remarks>Attach beneath a viewport with GUIEmbedSubwindows enabled. Rendering and input belong to this window's
/// viewport; hide requests are deferred on its SceneTree. Independent native child windows remain unavailable.</remarks>
public class Popup : Window
{
    private bool _closePending;
    /// <summary>Creates a hidden, borderless, fixed-size transient popup with wrapped controls.</summary>
    public Popup()
    {
        Visible = false; Transient = true; WrapControls = true; Borderless = true; Unresizable = true;
        MinimizeDisabled = true; MaximizeDisabled = true; PopupWindow = true; PopupWMHint = true;
        CloseRequested += ClosePopup;
        VisibilityChanged += ChangedVisibility;
    }
    /// <summary>Occurs after this popup becomes hidden, before canvas visibility propagation completes.</summary>
    public event Action? PopupHide;
    private void ChangedVisibility() { _closePending = false; if (!Visible) PopupHide?.Invoke(); }
    private void ClosePopup()
    {
        if (_closePending || !Visible) return;
        _closePending = true;
        if (Tree is { } tree) tree.Defer(() => { if (!IsDisposed && _closePending) { _closePending = false; Hide(); } });
        else { _closePending = false; Hide(); }
    }
    internal virtual void HandlePopupInput(InputEvent input)
    { if (PopupWindow && input.IsActionPressed("ui_cancel", false, true)) ClosePopup(); }
    internal override void AdjustPopup()
    {
        var area = GetUsableParentRect(); var size = Size; var position = Position;
        position = new((int)Math.Clamp((long)position.X, area.Position.X, Math.Max((long)area.Position.X, (long)area.Position.X + area.Size.X - size.X)), (int)Math.Clamp((long)position.Y, area.Position.Y, Math.Max((long)area.Position.Y, (long)area.Position.Y + area.Size.Y - size.Y)));
        size = new(Math.Min(size.X, area.Size.X), Math.Min(size.Y, area.Size.Y));
        if (MaxSize.X > 0) size.X = Math.Min(size.X, MaxSize.X); if (MaxSize.Y > 0) size.Y = Math.Min(size.Y, MaxSize.Y);
        Size = size; Position = position;
    }
    /// <inheritdoc />
    protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationApplicationFocusOut && PopupWindow) ClosePopup(); }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(Popup) ? CreatePopup : base.CreateSceneInstanceFactory();
    private static Node CreatePopup() => new Popup();
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) PopupHide = null; base.Dispose(disposing); }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors())
        {
            var name = property.Name;
            if (name == nameof(Visible)) yield return new PropertyDescriptor<Popup, bool>(name, w => w.Visible, (w, v) => w.Visible = v, _ => false, stored: true);
            else if (name is nameof(Transient) or nameof(WrapControls) or nameof(Borderless) or nameof(Unresizable) or nameof(MinimizeDisabled) or nameof(MaximizeDisabled) or nameof(PopupWindow) or nameof(PopupWMHint))
                yield return new PropertyDescriptor<Popup, bool>(name, w => GetDefaultedProperty(w, name), (w, v) => SetDefaultedProperty(w, name, v), _ => true, stored: true);
            else yield return property;
        }
    }
    private static bool GetDefaultedProperty(Popup w, string name) => name switch
    { nameof(Transient) => w.Transient, nameof(WrapControls) => w.WrapControls, nameof(Borderless) => w.Borderless, nameof(Unresizable) => w.Unresizable, nameof(MinimizeDisabled) => w.MinimizeDisabled, nameof(MaximizeDisabled) => w.MaximizeDisabled, nameof(PopupWindow) => w.PopupWindow, _ => w.PopupWMHint };
    private static void SetDefaultedProperty(Popup w, string name, bool value)
    { switch (name) { case nameof(Transient): w.Transient = value; break; case nameof(WrapControls): w.WrapControls = value; break; case nameof(Borderless): w.Borderless = value; break; case nameof(Unresizable): w.Unresizable = value; break; case nameof(MinimizeDisabled): w.MinimizeDisabled = value; break; case nameof(MaximizeDisabled): w.MaximizeDisabled = value; break; case nameof(PopupWindow): w.PopupWindow = value; break; default: w.PopupWMHint = value; break; } }

}
