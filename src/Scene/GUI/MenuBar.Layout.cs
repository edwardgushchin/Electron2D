namespace Electron2D;

public partial class MenuBar
{
    private bool _dirty = true, _building;
    private int _revision;
    private float _layoutWidth = -1;
    private Font _font = null!;
    private int _fontSize, _outline, _separation;
    private Vector2 _minimum;
    private void InvalidateMenus() { if (IsDisposed || _disposing) return; _revision++; _dirty = true; QueueRedraw(); UpdateMinimumSize(); }
    private void MeasureMenus()
    {
        if (_building || !_dirty && _layoutWidth == Size.X) return;
        _building = true;
        try
        {
            var revision = _revision;
            _font = GetThemeFont("font") ?? throw new InvalidOperationException("Menu bars require a live font.");
            _fontSize = GetThemeFontSize("font_size"); _outline = GetThemeConstant("outline_size"); _separation = GetThemeConstant("h_separation");
            var style = GetThemeStyleBox("normal")!; var margins = style.GetMinimumSize(); var width = 0f; var height = 0f; var visible = 0;
            var language = _language; if (language.Length == 0) language = TranslationServer.GetOrAddDomain(TranslationDomain).LocaleOverride; if (language.Length == 0) language = TranslationServer.Culture.Name;
            var direction = _textDirection == TextDirection.Inherited ? IsLayoutRTL() ? TextDirection.RTL : TextDirection.LTR : _textDirection;
            foreach (var item in _menus)
            {
                var key = new TextLayoutKey(Atr(item.Title ?? DefaultTitle(item)), _fontSize, -1, HorizontalAlignment.Left, 1, TextLineBreakFlags.None, TextJustificationFlags.None, direction, TextOrientation.Horizontal, false);
                item.Layout.Build(_font, key, new TextLayoutOptions(Language: language));
                if (item.Hidden) { item.Rect = default; continue; }
                var size = item.Layout.Size + margins;
                if (visible++ > 0) width += _separation;
                item.Rect = new(IsLayoutRTL() ? Size.X - width - size.X : width, 0, size.X, size.Y);
                width += size.X; height = Math.Max(height, size.Y);
            }
            _minimum = new(width, height); _layoutWidth = Size.X; _dirty = revision != _revision;
        }
        finally { _building = false; }
    }
    private int MenuAt(Vector2 point, bool enabledOnly = true)
    {
        MeasureMenus(); for (var i = 0; i < _menus.Count; i++) if (!_menus[i].Hidden && (!enabledOnly || !_menus[i].Disabled) && _menus[i].Rect.HasPoint(point)) return i;
        return -1;
    }
    /// <inheritdoc />
    protected override Vector2 OnGetMinimumSize() { MeasureMenus(); return _minimum; }
    /// <inheritdoc />
    protected override string OnGetTooltip(Vector2 atPosition) { var index = MenuAt(atPosition, enabledOnly: false); return index < 0 ? "" : _menus[index].Tooltip; }
    private void DrawMenus()
    {
        MeasureMenus(); var rtl = IsLayoutRTL();
        for (var i = 0; i < _menus.Count; i++)
        {
            var item = _menus[i]; if (item.Hidden) continue;
            var hovered = i == _hover; var pressed = item == _active;
            var state = item.Disabled ? "disabled" : hovered && pressed && HasThemeStyleBox("hover_pressed") ? "hover_pressed" : pressed ? "pressed" : hovered ? "hover" : "normal";
            var mirrored = state switch { "disabled" => "disabled_mirrored", "hover_pressed" => "hover_pressed_mirrored", "pressed" => "pressed_mirrored", "hover" => "hover_mirrored", _ => "normal_mirrored" };
            var styleName = rtl && HasThemeStyleBox(mirrored) ? mirrored : state;
            var style = GetThemeStyleBox(styleName)!; if (!_flat) DrawStyleBox(style, item.Rect);
            var colorName = item.Disabled ? "font_disabled_color" : hovered && pressed ? "font_hover_pressed_color" : pressed ? "font_pressed_color" : hovered ? "font_hover_color" : HasFocus(true) ? "font_focus_color" : "font_color";
            var color = GetThemeColor(colorName); var outline = GetThemeColor("font_outline_color");
            var baseline = item.Rect.Position + new Vector2(style.GetMargin(Side.Left), style.GetMargin(Side.Top) + item.Layout.FirstAscent);
            if (_outline > 0 && outline.A > 0) item.Layout.Draw(this, baseline, outline, _outline, outlinePass: true);
            item.Layout.Draw(this, baseline, color);
        }
    }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        List<Exception>? errors = null;
        try { base.OnNotification(what); } catch (Exception e) { CollectException(ref errors, e); }
        if (!IsDisposed && !_disposing)
            try
            {
                if (what == NotificationChildOrderChanged) SynchronizeMenus();
                else if (what is NotificationThemeChanged or NotificationTranslationChanged or NotificationLayoutDirectionChanged or NotificationEnterTree) InvalidateMenus();
                else if (what == NotificationResized) { _dirty = true; QueueRedraw(); }
                else if (what == NotificationDraw) DrawMenus();
                else if (what is NotificationFocusEnter or NotificationFocusExit) QueueRedraw();
                else if (what == NotificationMouseExit) { _hover = -1; _selected = -1; QueueRedraw(); }
                else if (what == NotificationInternalProcess) HoverMenu();
                else if (what == NotificationExitTree || what == NotificationVisibilityChanged && !IsVisibleInTree) CloseMenus();
            }
            catch (Exception e) { CollectException(ref errors, e); }
        ThrowCollected("Menu bar notification callbacks failed.", errors);
    }
}
