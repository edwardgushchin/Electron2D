namespace Electron2D;

public partial class OptionButton
{
    private readonly List<TextLayout> _itemLayouts = [];
    private Texture? Arrow() => GetThemeIcon("arrow");
    internal override void RefreshButtonIndicator() { var width = Arrow()?.GetWidth() ?? 0; SetButtonInternalMargins(IsLayoutRTL() ? width : 0, IsLayoutRTL() ? 0 : width); }
    internal override void PollButtonTextures(ref int count, ref bool changed) { base.PollButtonTextures(ref count, ref changed); PollButtonTexture(Arrow(), ref count, ref changed); }
    /// <inheritdoc />
    protected override Vector2 OnGetMinimumSize()
    {
        if (_popup is null) return base.OnGetMinimumSize(); var padding = LargestButtonStyleSize; Vector2 minimum;
        if (!_fitLongest) minimum = base.OnGetMinimumSize();
        else
        {
            minimum = padding; var font = GetThemeFont("font") ?? throw new InvalidOperationException("Options require a font."); var size = GetThemeFontSize("font_size"); var language = Language.Length == 0 ? TranslationServer.Culture.Name : Language;
            var direction = TextDirection == TextDirection.Inherited ? IsLayoutRTL() ? TextDirection.RTL : TextDirection.LTR : TextDirection;
            while (_itemLayouts.Count < _popup.ItemCount) _itemLayouts.Add(new());
            for (var i = 0; i < _popup.ItemCount; i++)
            {
                var text = TranslateItem(i, _popup.GetItemText(i)); var layout = _itemLayouts[i];
                layout.Build(font, new(text, size, -1, HorizontalAlignment.Left, -1, TextLineBreakFlags.Mandatory | AutowrapTrimFlags, TextJustificationFlags.Kashida | TextJustificationFlags.WordBound | TextJustificationFlags.SkipLastLine | TextJustificationFlags.DoNotSkipSingleLine, direction, TextOrientation.Horizontal, true), new TextLayoutOptions(Language: language, LineSpacing: GetThemeConstant("line_spacing")));
                var item = layout.Size; if (text.Length != 0) item.Y = MathF.Max(item.Y, font.GetHeight(size));
                if (_popup.GetItemIcon(i) is { } icon && !ExpandIcon) { var iconSize = FitButtonIcon(icon.GetSize()); item.Y = VerticalIconAlignment == VerticalAlignment.Center ? MathF.Max(item.Y, iconSize.Y) : item.Y + iconSize.Y; if (IconAlignment == HorizontalAlignment.Center) item.X = MathF.Max(item.X, iconSize.X); else item.X += iconSize.X + (text.Length == 0 ? 0 : ButtonSeparation); }
                minimum = minimum.Max(item + padding);
            }
        }
        if (Arrow() is { } arrow) { var content = minimum - padding; content.X += arrow.GetWidth() + ButtonSeparation; content.Y = MathF.Max(content.Y, arrow.GetHeight()); minimum = content + padding; }
        if (_fitLongest) minimum.X = MathF.Max(minimum.X, _popup.GetContentsMinimumSize().X); return minimum;
    }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        base.OnNotification(what); if (_popup is null || IsDisposed) return;
        if (what == NotificationDraw && Arrow() is { } arrow)
        {
            var color = Colors.White; if (GetThemeConstant("modulate_arrow") != 0) { var key = GetDrawMode() switch { DrawMode.Pressed => "font_pressed_color", DrawMode.Hover => "font_hover_color", DrawMode.HoverPressed => "font_hover_pressed_color", DrawMode.Disabled => "font_disabled_color", _ => HasFocus(true) ? "font_focus_color" : "font_color" }; color = GetThemeColor(key); }
            var x = IsLayoutRTL() ? GetThemeConstant("arrow_margin") : Size.X - arrow.GetWidth() - GetThemeConstant("arrow_margin"); var y = MathF.Truncate(MathF.Abs((Size.Y - arrow.GetHeight()) / 2)); DrawTextureRect(arrow, new(x, y, arrow.GetWidth(), arrow.GetHeight()), false, color);
        }
        else if (what is NotificationThemeChanged or NotificationLayoutDirectionChanged or NotificationTranslationChanged) { _popup.LayoutDirection = LayoutDirection; Reconcile(); UpdateMinimumSize(); }
        else if (what == NotificationVisibilityChanged && !IsVisibleInTree) _popup.Hide();
    }
}
