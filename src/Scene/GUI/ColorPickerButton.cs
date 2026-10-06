namespace Electron2D;

/// <summary>A themed color swatch button opening an owned color editor popup.</summary>
/// <remarks>The picker is created lazily. GetPicker/GetPopup return required borrowed nodes; preserve their
/// parent and lifetime. Programmatic color writes are silent. Escape restores the opening color and publishes it.</remarks>
public partial class ColorPickerButton : Button
{
    private Color _color = Colors.Black, _openingColor;
    private bool _editAlpha = true, _editIntensity = true, _disposing, _wasOpen, _opened, _creating;
    private OwnedPicker? _picker;
    private OwnedPopup? _popup;
    /// <summary>Creates a black color button with toggle mode and both alpha/intensity editing enabled.</summary>
    public ColorPickerButton() : this("") { }
    /// <summary>Creates a color button with source text.</summary><param name="text">Nonnull initial caption.</param>
    public ColorPickerButton(string text) : base(text) { ToggleMode = true; }
    /// <summary>Gets or sets the finite selected color without ColorChanged.</summary><value>Opaque black initially; HDR channels are preserved.</value>
    public Color Color { get { CheckColorButton(); return _color; } set { EnsureMutable(); if (!value.IsFinite()) throw new ArgumentException("Color channels must be finite.", nameof(value)); if (_color == value) return; _color = value; if (_picker != null) _picker.Color = value; QueueRedraw(); } }
    /// <summary>Gets or sets alpha channel visibility in the owned picker.</summary><value>True initially.</value>
    public bool EditAlpha { get { CheckColorButton(); return _editAlpha; } set { EnsureMutable(); _editAlpha = value; if (_picker != null) _picker.EditAlpha = value; } }
    /// <summary>Gets or sets intensity editing in the owned picker.</summary><value>True initially.</value>
    public bool EditIntensity { get { CheckColorButton(); return _editIntensity; } set { EnsureMutable(); _editIntensity = value; if (_picker != null) _picker.EditIntensity = value; } }
    /// <summary>Occurs after an editor color change or Escape restoration.</summary>
    public event Action<Color>? ColorChanged;
    /// <summary>Occurs once after the required picker and popup have been created.</summary>
    public event Action? PickerCreated;
    /// <summary>Occurs after an opened picker popup closes.</summary>
    public event Action? PopupClosed;
    /// <summary>Creates if necessary and returns the stable required borrowed color editor.</summary><returns>The button-owned picker.</returns>
    public ColorPicker GetPicker() { EnsureMutable(); CreatePicker(); return _picker!; }
    /// <summary>Creates if necessary and returns the stable required borrowed popup.</summary><returns>The button-owned popup.</returns>
    public PopupPanel GetPopup() { EnsureMutable(); CreatePicker(); return _popup!; }
    private void CheckColorButton() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); if (_picker?.IsDisposed == true || _popup?.IsDisposed == true) throw new InvalidOperationException("The required color editor has left its owner."); }
    private void CreatePicker()
    {
        if (_picker != null) return; if (_creating) throw new InvalidOperationException("Color editor creation cannot reenter."); _creating = true;
        try { _popup = new OwnedPopup(this) { Name = "_color_popup" }; AddChild(_popup, InternalMode.Front); _picker = new OwnedPicker(this) { Name = "Picker", Color = _color, EditAlpha = _editAlpha, EditIntensity = _editIntensity }; _popup.AddChild(_picker); _picker.SetOpeningColor(_color); _picker.ColorChanged += PickerChanged; _popup.AboutToPopup += BeforePopup; _popup.PopupHide += Closed; PickerCreated?.Invoke(); }
        finally { _creating = false; }
    }
    private void PickerChanged(Color color) { _color = color; QueueRedraw(); ColorChanged?.Invoke(color); }
    private void BeforePopup() { _openingColor = _color; _picker!.SetOpeningColor(_color); _opened = true; SetPressedNoSignal(true); }
    private void Closed()
    {
        if (!_opened || _disposing) return; _opened = false; List<Exception>? errors = null;
        try { if (_popup!.Canceled) { Color = _openingColor; ColorChanged?.Invoke(_color); } } catch (Exception error) { CollectException(ref errors, error); }
        try { if (!IsDisposed) SetPressedNoSignal(false); } catch (Exception error) { CollectException(ref errors, error); }
        try { if (!IsDisposed) PopupClosed?.Invoke(); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Color popup closing callbacks failed.", errors);
    }
    /// <inheritdoc />
    protected override void OnGUIInput(InputEvent input) { if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } || InputMap.HasAction("ui_accept") && input.IsActionPressed("ui_accept", allowEcho: false)) _wasOpen = _popup?.Visible == true; base.OnGUIInput(input); }
    /// <inheritdoc />
    protected override void OnPressed()
    {
        CreatePicker(); if (_wasOpen) { _wasOpen = false; _popup!.Hide(); return; }
        if (!IsInsideTree) return;
        var minimum = _popup!.GetContentsMinimumSize(); var transform = GetGlobalTransformWithCanvas(); var at = transform.Origin; var viewport = _popup.Embedder?.GetVisibleRect() ?? GetViewport()!.GetVisibleRect();
        at.X += (Size.X - minimum.X) / 2; var below = at.Y + Size.Y; at.Y = below + minimum.Y > viewport.End.Y && at.Y * 2 + Size.Y > viewport.End.Y ? at.Y - minimum.Y : below;
        _popup.Position = (Vector2i)at; _popup.Size = (Vector2i)minimum.Ceil(); _popup.Canceled = false; _popup.Popup(); _picker!.FocusEditor();
    }
    /// <inheritdoc />
    protected override void OnDraw()
    {
        var style = GetThemeStyleBox("normal"); var rect = new Rect2(style?.GetOffset() ?? Vector2.Zero, Size - (style?.GetMinimumSize() ?? Vector2.Zero));
        if (GetThemeIcon("bg") is { } bg) DrawTextureRect(bg, rect, true); DrawRect(rect, _color);
        if ((_color.R > 1 || _color.G > 1 || _color.B > 1) && GetThemeIcon("overbright_indicator", "ColorPicker") is { } indicator) DrawTexture(indicator, rect.Position);
    }
    /// <inheritdoc />
    protected override void OnNotification(int what) { base.OnNotification(what); if ((what == NotificationVisibilityChanged && !IsVisibleInTree || what == NotificationExitTree) && _popup?.Visible == true) _popup.Hide(); }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(ColorPickerButton) ? CreateColorPickerButton : base.CreateSceneInstanceFactory();
    private static Node CreateColorPickerButton() => new ColorPickerButton();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) { if (property.Name == nameof(ToggleMode)) yield return new PropertyDescriptor<ColorPickerButton, bool>(nameof(ToggleMode), p => p.ToggleMode, (p, v) => p.ToggleMode = v, _ => true, stored: true); else yield return property; }
        yield return new PropertyDescriptor<ColorPickerButton, Color>(nameof(Color), p => p.Color, (p, v) => p.Color = v, _ => Colors.Black, stored: true);
        yield return new PropertyDescriptor<ColorPickerButton, bool>(nameof(EditAlpha), p => p.EditAlpha, (p, v) => p.EditAlpha = v, _ => true, stored: true);
        yield return new PropertyDescriptor<ColorPickerButton, bool>(nameof(EditIntensity), p => p.EditIntensity, (p, v) => p.EditIntensity = v, _ => true, stored: true);
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { _disposing = true; ColorChanged = null; PickerCreated = PopupClosed = null; } base.Dispose(disposing); }
    private sealed class OwnedPicker(ColorPickerButton owner) : ColorPicker
    { protected override void ValidateDisposal() { if (!owner.IsDisposed && !owner._disposing) throw new InvalidOperationException("The required color editor belongs to its button."); base.ValidateDisposal(); } }
    private sealed class OwnedPopup(ColorPickerButton owner) : PopupPanel
    {
        internal bool Canceled;
        internal override void HandlePopupInput(InputEvent input) { if (InputMap.HasAction("ui_cancel") && input.IsActionPressed("ui_cancel")) Canceled = true; if (InputMap.HasAction("ui_accept") && input.IsActionPressed("ui_accept")) { List<Exception>? errors = null; try { owner._picker?.CommitFocusedField(); } catch (Exception error) { CollectException(ref errors, error); } try { Hide(); } catch (Exception error) { CollectException(ref errors, error); } ThrowCollected("Color popup acceptance callbacks failed.", errors); return; } base.HandlePopupInput(input); }
        protected override void ValidateDisposal() { if (!owner.IsDisposed && !owner._disposing) throw new InvalidOperationException("The required color popup belongs to its button."); base.ValidateDisposal(); }
    }
}
