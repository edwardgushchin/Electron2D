namespace Electron2D;

/// <summary>A transparent popup fitting direct child controls inside a themed panel.</summary>
/// <remarks>The internal panel and its shadow are owned by this node. Theme resources remain borrowed.
/// The content rectangle is restored after hiding a popup with a shadow-expanded rectangle.</remarks>
public class PopupPanel : Popup
{
    private readonly Panel _panel;
    private Rect2i? _contentRect;
    private Vector2 _shadowStart, _shadowEnd;
    private bool _arranging;
    private Vector2i _expandedSize;
    /// <summary>Creates a hidden transparent panel popup with inherited texture sampling.</summary>
    public PopupPanel()
    {
        Transparent = true; TransparentBG = true;
        CanvasItemDefaultTextureFilter = DefaultCanvasItemTextureFilter.ParentNode;
        CanvasItemDefaultTextureRepeat = DefaultCanvasItemTextureRepeat.ParentNode;
        _panel = new Panel { Name = "_popup_panel", MouseFilter = MouseFilter.Ignore };
        AddChild(_panel, InternalMode.Front);
        SizeChanged += Resized;
        ThemeChanged += Arrange;
        VisibilityChanged += PanelVisibility;
        ChildEnteredTree += ChildChanged;
        ChildExitingTree += ChildLeaving;
    }
    private void ChildChanged(Node _, Node child) { if (child is Control control && child != _panel) { control.MinimumSizeChanged += ChildMinimum; Arrange(); } }
    private void ChildLeaving(Node _, Node child) { if (child is Control control) control.MinimumSizeChanged -= ChildMinimum; }
    private void ChildMinimum() { if (!IsDisposed) { UpdateEmbeddedContents(); Arrange(); } }
    private StyleBox? Style => GetThemeStyleBox("panel");
    private void PanelVisibility()
    {
        if (!Visible && _contentRect is { } rect) { _contentRect = null; _shadowStart = _shadowEnd = default; Position = rect.Position; Size = rect.Size; }
        Arrange();
    }
    private void Resized()
    { if (Visible && !_arranging && _contentRect is not null && Size != _expandedSize) _contentRect = null; Arrange(); }
    private void ShadowInsets(bool visible)
    {
        if (visible && Style is StyleBoxFlat flat) { var size = flat.ShadowSize; var offset = flat.ShadowOffset; _shadowStart = new(MathF.Max(0, size - offset.X), MathF.Max(0, size - offset.Y)); _shadowEnd = new(MathF.Max(0, size + offset.X), MathF.Max(0, size + offset.Y)); if (IsLayoutRTL()) (_shadowStart.X, _shadowEnd.X) = (_shadowEnd.X, _shadowStart.X); }
        else _shadowStart = _shadowEnd = default;
    }
    private void Arrange()
    {
        if (_arranging || IsDisposed || _panel is null || _panel.IsDisposed) return;
        _arranging = true;
        try
        {
            ShadowInsets(Visible);
            var style = Style;
            if (style is not null) _panel.AddThemeStyleBoxOverride("panel", style);
            _panel.Position = _shadowStart; _panel.Size = ((Vector2)Size - _shadowStart - _shadowEnd).Max(Vector2.Zero);
            var position = _shadowStart + (style?.GetOffset() ?? Vector2.Zero);
            var size = ((Vector2)Size - _shadowStart - _shadowEnd - (style?.GetMinimumSize() ?? Vector2.Zero)).Max(Vector2.Zero);
            for (var i = 0; i < GetChildCount(); i++) if (GetChild(i) is Control { TopLevel: false } control) { control.Position = position; control.Size = size; }
        }
        finally { _arranging = false; }
    }
    /// <inheritdoc />
    protected override Vector2 OnGetContentsMinimumSize()
    {
        var min = Vector2.Zero;
        for (var i = 0; i < GetChildCount(); i++) if (GetChild(i) is Control { TopLevel: false } control) min = min.Max(control.GetBoundMinimumSize());
        return min + (Style?.GetMinimumSize() ?? Vector2.Zero);
    }
    internal override void AdjustPopup()
    {
        base.AdjustPopup(); _contentRect = new(Position, Size);
        ShadowInsets(true); _expandedSize = Size + (Vector2i)(_shadowStart + _shadowEnd);
        _arranging = true;
        try { Position -= (Vector2i)_shadowStart; Size = _expandedSize; }
        finally { _arranging = false; }
        Arrange();
    }
    internal override void HandlePopupInput(InputEvent input)
    {
        if (PopupWindow && input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } button && !new Rect2(_shadowStart, (Vector2)Size - _shadowStart - _shadowEnd).HasPoint(button.Position)) RequestEmbeddedClose();
        base.HandlePopupInput(input);
    }
    /// <inheritdoc />
    protected override void OnNotification(int what) { base.OnNotification(what); if (what is NotificationThemeChanged or Control.NotificationLayoutDirectionChanged or Control.NotificationTranslationChanged) Arrange(); }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(PopupPanel) ? CreatePopupPanel : base.CreateSceneInstanceFactory();
    private static Node CreatePopupPanel() => new PopupPanel();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors())
        {
            if (property.Name is nameof(Transparent) or nameof(TransparentBG)) yield return new PropertyDescriptor<PopupPanel, bool>(property.Name, w => property.Name == nameof(Transparent) ? w.Transparent : w.TransparentBG, (w, v) => { if (property.Name == nameof(Transparent)) w.Transparent = v; else w.TransparentBG = v; }, _ => true, stored: true);
            else if (property.Name == nameof(CanvasItemDefaultTextureFilter)) yield return new PropertyDescriptor<PopupPanel, DefaultCanvasItemTextureFilter>(property.Name, w => w.CanvasItemDefaultTextureFilter, (w, v) => w.CanvasItemDefaultTextureFilter = v, _ => DefaultCanvasItemTextureFilter.ParentNode, stored: true);
            else if (property.Name == nameof(CanvasItemDefaultTextureRepeat)) yield return new PropertyDescriptor<PopupPanel, DefaultCanvasItemTextureRepeat>(property.Name, w => w.CanvasItemDefaultTextureRepeat, (w, v) => w.CanvasItemDefaultTextureRepeat = v, _ => DefaultCanvasItemTextureRepeat.ParentNode, stored: true);
            else yield return property;
        }
    }
}
