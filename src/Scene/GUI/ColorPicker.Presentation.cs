namespace Electron2D;

public partial class ColorPicker
{
    private sealed class InsetContainer(ColorPicker owner) : Container
    {
        private Vector2 Inset => Vector2.One * owner.GetThemeConstant("margin");
        protected override Vector2 OnGetMinimumSize() => owner._content is { } content ? content.GetBoundMinimumSize() + Inset * 2 : Vector2.Zero;
        protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationSortChildren && owner._content != null) FitChildInRect(owner._content, new(Inset, (Size - Inset * 2).Max(Vector2.Zero))); }
    }
    private readonly (Color Color, bool Alpha, bool Constructor, string Text)[] _textCache = new (Color, bool, bool, string)[64];
    private int _textCacheCount, _textCacheNext;
    private readonly Button _presetFold, _recentFold;
    private string ColorText(bool constructor)
    {
        for (var i = 0; i < _textCacheCount; i++) if (_textCache[i].Color == _color && _textCache[i].Alpha == _editAlpha && _textCache[i].Constructor == constructor) return _textCache[i].Text;
        // ponytail: 64 color texts retain prepared states; arbitrary new colors format cold, add span-backed text when needed.
        var text = constructor ? FormattableString.Invariant($"Color({_color.R}, {_color.G}, {_color.B}, {_color.A})") : _color.ToHTML(_editAlpha && _color.A < 1);
        _textCache[_textCacheNext] = (_color, _editAlpha, constructor, text); _textCacheNext = (_textCacheNext + 1) % _textCache.Length; _textCacheCount = Math.Min(_textCacheCount + 1, _textCache.Length); return text;
    }
    private void RefreshIcons()
    {
        _pick.Icon = GetThemeIcon("screen_picker"); _copy.Icon = GetThemeIcon("color_copy"); _textType.Icon = _textLabel.Text == "Expr" ? GetThemeIcon("color_script") : null;
        _add.Icon = GetThemeIcon("add_preset"); _paletteMenu.Icon = GetThemeIcon("menu_option");
        _shapeMenu.Icon = GetThemeIcon(_shape == PickerShapeType.HSVWheel ? "shape_rect_wheel" : _shape is PickerShapeType.VHSCircle or PickerShapeType.OKHSLCircle ? "shape_circle" : "shape_rect");
        var shapePopup = _shapeMenu.GetPopup(); for (var i = 0; i < shapePopup.ItemCount; i++) { var shape = (PickerShapeType)shapePopup.GetItemID(i); shapePopup.SetItemIcon(i, GetThemeIcon(shape == PickerShapeType.HSVWheel ? "shape_rect_wheel" : shape is PickerShapeType.VHSCircle or PickerShapeType.OKHSLCircle ? "shape_circle" : "shape_rect")); }
        _presetFold.Icon = GetThemeIcon(_presetFold.ButtonPressed ? "expanded_arrow" : "folded_arrow"); _recentFold.Icon = GetThemeIcon(_recentFold.ButtonPressed ? "expanded_arrow" : "folded_arrow");
        foreach (var slider in _sliders) slider.AddThemeConstantOverride("center_grabber", GetThemeConstant("center_slider_grabbers"));
        if (GetChild(0, true) is InsetContainer inset) { inset.UpdateMinimumSize(); inset.QueueSort(); }
    }
}
