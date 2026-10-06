namespace Electron2D;

public partial class AcceptDialog
{
    private bool _arranging, _arrangeAgain;
    private StyleBox? _panelStyle;
    private readonly List<Control> _content = [];
    private void ChildEntered(Node _, Node child) { if (child is Control control && control != _panel && control != _buttons && control != _label) { control.MinimumSizeChanged += Arrange; Arrange(); } }
    private void ChildLeaving(Node _, Node child) { if (child is Control control && control != _label) control.MinimumSizeChanged -= Arrange; }
    private void ApplyDialogTheme()
    {
        var style = GetThemeStyleBox("panel"); if (style != _panelStyle) { _panelStyle = style; if (style != null) _panel.AddThemeStyleBoxOverride("panel", style); }
        var min = new Vector2(Math.Max(0, GetThemeConstant("buttons_min_width")), Math.Max(0, GetThemeConstant("buttons_min_height")));
        for (var i = 0; i < _buttons.GetChildCount(); i++) if (_buttons.GetChild(i) is Button { IsDisposed: false } button) button.CustomMinimumSize = min;
    }
    private void Arrange()
    {
        if (_disposing || IsDisposed || _panel == null || _ok == null) return;
        if (_arranging) { _arrangeAgain = true; return; }
        _arranging = true; List<Exception>? errors = null;
        try
        {
            for (var pass = 0; pass < 64; pass++)
            {
                _arrangeAgain = false; ApplyDialogTheme(); UpdateEmbeddedContents();
                var offset = _panelStyle?.GetOffset() ?? Vector2.Zero; var margins = _panelStyle?.GetMinimumSize() ?? Vector2.Zero; var rowHeight = _buttons.GetBoundMinimumSize().Y;
                _panel.Position = Vector2.Zero; _panel.Size = Size;
                _buttons.Position = new(offset.X, Size.Y - (margins.Y - offset.Y) - rowHeight); _buttons.Size = new(MathF.Max(0, Size.X - margins.X), rowHeight);
                _content.Clear(); for (var i = 0; i < GetChildCount(true); i++) if (GetChild(i, true) is Control { TopLevel: false } control && control != _panel && control != _buttons) _content.Add(control);
                var available = new Vector2(MathF.Max(0, Size.X - margins.X), MathF.Max(0, Size.Y - margins.Y - rowHeight - GetThemeConstant("buttons_separation")));
                for (var i = 0; i < _content.Count; i++) { var c = _content[i]; if (c.IsDisposed || c.Parent != this) continue; try { c.Position = offset; c.Size = available; } catch (Exception e) { CollectException(ref errors, e); } }
                if (!_arrangeAgain) break; if (pass == 63) throw new InvalidOperationException("Dialog layout did not settle.");
            }
        }
        finally { _content.Clear(); _arranging = false; }
        ThrowCollected("Dialog layout callbacks failed.", errors);
    }
    /// <inheritdoc />
    protected override Vector2 OnGetContentsMinimumSize()
    {
        if (_buttons == null) return Vector2.Zero;
        var content = Vector2.Zero;
        for (var i = 0; i < GetChildCount(true); i++) if (GetChild(i, true) is Control { TopLevel: false, IsDisposed: false } c && c != _panel && c != _buttons) content = content.Max(c.GetBoundMinimumSize());
        var row = _buttons.GetBoundMinimumSize(); return new Vector2(MathF.Max(content.X, row.X), content.Y + row.Y + GetThemeConstant("buttons_separation")) + (GetThemeStyleBox("panel")?.GetMinimumSize() ?? Vector2.Zero);
    }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        base.OnNotification(what);
        if (what == NotificationExitTree) UnwatchParent();
        if (what is NotificationReady or NotificationThemeChanged or Control.NotificationLayoutDirectionChanged or Control.NotificationTranslationChanged or NotificationChildOrderChanged) Arrange();
    }
}
