namespace Electron2D;

/// <summary>Displays a titled graph grouping frame with logical attachments and automatic content bounds.</summary>
/// <remarks>GraphEdit owns attachment relationships. The interior passes pointer hits through;
/// the titlebar and DragMargin edges remain draggable. Autoshrink disables manual resizing.</remarks>
public class GraphFrame : GraphElement
{
    private readonly GraphTitlebar _titlebar;
    private bool _autoshrink = true, _tintEnabled;
    private int _margin = 40, _dragMargin = 16;
    private Color _tint = new(.3f, .3f, .3f, .75f);
    private StyleBox? _tintedPanel, _tintedSelected;
    /// <summary>Creates an empty automatically shrinking frame with a centered title.</summary>
    public GraphFrame() { MouseFilter = MouseFilter.Stop; _titlebar = new(this, true); }
    /// <summary>Gets or sets the displayed title.</summary>
    public string Title { get { CheckGraph(); return _titlebar.Label.Text; } set { MutableGraph(); ArgumentNullException.ThrowIfNull(value); _titlebar.Label.Text = value; UpdateMinimumSize(); QueueSort(); QueueRedraw(); } }
    /// <summary>Gets or sets automatic bounds around attached elements.</summary>
    public bool AutoshrinkEnabled { get { CheckGraph(); return _autoshrink; } set { MutableGraph(); if (_autoshrink == value) return; _autoshrink = value; QueueRedraw(); AutoshrinkChanged?.Invoke(); } }
    /// <summary>Gets or sets nonnegative padding around automatic attachment bounds.</summary>
    public int AutoshrinkMargin { get { CheckGraph(); return _margin; } set { MutableGraph(); if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); if (_margin == value) return; _margin = value; AutoshrinkChanged?.Invoke(); } }
    /// <summary>Gets or sets the nonnegative draggable edge width.</summary>
    public int DragMargin { get { CheckGraph(); return _dragMargin; } set { MutableGraph(); if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); _dragMargin = value; } }
    /// <summary>Gets or sets optional panel tint.</summary>
    public Color TintColor { get { CheckGraph(); return _tint; } set { MutableGraph(); if (!float.IsFinite(value.R) || !float.IsFinite(value.G) || !float.IsFinite(value.B) || !float.IsFinite(value.A)) throw new ArgumentOutOfRangeException(nameof(value)); _tint = value; RefreshTint(); QueueRedraw(); } }
    /// <summary>Gets or sets whether panel tint overrides the themed panel colors.</summary>
    public bool TintColorEnabled { get { CheckGraph(); return _tintEnabled; } set { MutableGraph(); _tintEnabled = value; RefreshTint(); QueueRedraw(); } }
    /// <summary>Reports changed automatic sizing configuration.</summary>
    public event Action? AutoshrinkChanged;
    /// <summary>Returns the borrowed owned titlebar for additional application controls.</summary><returns>The horizontal titlebar.</returns>
    public HBoxContainer GetTitlebarHBox() { CheckGraph(); return _titlebar; }
    internal override bool CanResize => Resizable && !AutoshrinkEnabled;
    internal float GraphTitleHeight => _titlebar is null ? 0 : _titlebar.GetBoundMinimumSize().Y + (GetThemeStyleBox("titlebar")?.GetMinimumSize().Y ?? 0);
    private void RefreshTint()
    {
        _tintedPanel?.Dispose(); _tintedSelected?.Dispose(); _tintedPanel = _tintedSelected = null;
        if (!_tintEnabled) return;
        StyleBox? Tint(string name, bool selected)
        {
            var source = GetThemeStyleBox(name); if (source is null) return null;
            var style = (StyleBox)source.Duplicate(true);
            if (style is StyleBoxFlat flat) { flat.BGColor = _tint; if (!selected) flat.BorderColor = _tint.Lightened(.3f); }
            else if (style is StyleBoxTexture texture) texture.ModulateColor = _tint;
            return style;
        }
        _tintedPanel = Tint("panel", false); _tintedSelected = Tint("panel_selected", true);
    }
    /// <inheritdoc />
    protected override bool HasPoint(Vector2 point)
    {
        if (!base.HasPoint(point)) return false;
        return point.Y < GraphTitleHeight || new Rect2(Size - ResizeHandleSize, ResizeHandleSize).HasPoint(point) || !new Rect2(Vector2.Zero, Size).Grow(-DragMargin).HasPoint(point);
    }
    private Vector2 Measure(bool desired)
    {
        var margins = GetThemeStyleBox("panel")?.GetMinimumSize() ?? Vector2.Zero;
        var title = (_titlebar is null ? Vector2.Zero : desired ? _titlebar.GetBoundDesiredSize() : _titlebar.GetBoundMinimumSize()) + (GetThemeStyleBox("titlebar")?.GetMinimumSize() ?? Vector2.Zero);
        var body = Vector2.Zero;
        for (var i = 0; i < GetChildCount(); i++) if (GetChild(i) is Control child && Sortable(child, true)) body = body.Max(desired ? child.GetBoundDesiredSize() : child.GetBoundMinimumSize());
        return new(Math.Max(title.X, body.X + margins.X), title.Y + body.Y + margins.Y);
    }
    /// <inheritdoc />
    protected override Vector2 OnGetMinimumSize() => Measure(false);
    internal override Vector2 GetDesiredSize() => Measure(true);
    internal override void ArrangeGraphChildren()
    {
        if (_titlebar is null) return;
        var title = GetThemeStyleBox("titlebar"); var titleMargins = title?.GetMinimumSize() ?? Vector2.Zero;
        FitChildInRect(_titlebar, new(title?.GetOffset() ?? Vector2.Zero, new(Math.Max(0, Size.X - titleMargins.X), Math.Max(0, GraphTitleHeight - titleMargins.Y))));
        var panel = GetThemeStyleBox("panel"); var margins = panel?.GetMinimumSize() ?? Vector2.Zero; var offset = (panel?.GetOffset() ?? Vector2.Zero) + new Vector2(0, GraphTitleHeight);
        for (var i = 0; i < GetChildCount(); i++) if (GetChild(i) is Control child && Sortable(child)) FitChildInRect(child, new(offset, (Size - margins - new Vector2(0, GraphTitleHeight)).Max(Vector2.Zero)));
    }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        base.OnNotification(what);
        if (what == NotificationThemeChanged) RefreshTint();
        if (what != NotificationDraw || _titlebar is null) return;
        GraphDrawing = true; try
        {
            var body = new Rect2(new(0, GraphTitleHeight), new(Size.X, Math.Max(0, Size.Y - GraphTitleHeight)));
            var panel = (_tintEnabled ? Selected ? _tintedSelected : _tintedPanel : null) ?? GetThemeStyleBox(Selected ? "panel_selected" : "panel");
            if (panel != null) DrawStyleBox(panel, body);
            if (GetThemeStyleBox(Selected ? "titlebar_selected" : "titlebar") is { } title) DrawStyleBox(title, new(Vector2.Zero, new(Size.X, GraphTitleHeight)));
            if (CanResize && GetThemeIcon("resizer") is { } icon) DrawTexture(icon, Size - icon.GetSize(), GetThemeColor("resizer_color"));
        }
        finally { GraphDrawing = false; }
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var p in base.GetPropertyDescriptors()) { if (p.Name == nameof(MouseFilter)) yield return new PropertyDescriptor<GraphFrame, MouseFilter>(p.Name, n => n.MouseFilter, (n, v) => n.MouseFilter = v, _ => MouseFilter.Stop, stored: true); else yield return p; }
        yield return new PropertyDescriptor<GraphFrame, string>(nameof(Title), n => n.Title, (n, v) => n.Title = v, _ => "", stored: true);
        yield return new PropertyDescriptor<GraphFrame, bool>(nameof(AutoshrinkEnabled), n => n.AutoshrinkEnabled, (n, v) => n.AutoshrinkEnabled = v, _ => true, stored: true);
        yield return new PropertyDescriptor<GraphFrame, int>(nameof(AutoshrinkMargin), n => n.AutoshrinkMargin, (n, v) => n.AutoshrinkMargin = v, _ => 40, stored: true);
        yield return new PropertyDescriptor<GraphFrame, int>(nameof(DragMargin), n => n.DragMargin, (n, v) => n.DragMargin = v, _ => 16, stored: true);
        yield return new PropertyDescriptor<GraphFrame, Color>(nameof(TintColor), n => n.TintColor, (n, v) => n.TintColor = v, _ => new(.3f, .3f, .3f, .75f), stored: true);
        yield return new PropertyDescriptor<GraphFrame, bool>(nameof(TintColorEnabled), n => n.TintColorEnabled, (n, v) => n.TintColorEnabled = v, _ => false, stored: true);
    }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(GraphFrame) ? CreateGraphFrame : base.CreateSceneInstanceFactory();
    private static Node CreateGraphFrame() => new GraphFrame();
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { AutoshrinkChanged = null; _tintedPanel?.Dispose(); _tintedSelected?.Dispose(); } base.Dispose(disposing); }
}
