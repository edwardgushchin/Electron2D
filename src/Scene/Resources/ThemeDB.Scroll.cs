namespace Electron2D;

public sealed partial class ThemeDB
{
    private StyleBoxFlat CreateScrollStyle(Color color, float left, float top, float right, float bottom)
    {
        var style = new StyleBoxFlat { BGColor = color, CornerDetail = 6 };
        style.SetCornerRadiusAll(10);
        style.ContentMarginLeft = left; style.ContentMarginTop = top;
        style.ContentMarginRight = right; style.ContentMarginBottom = bottom;
        _owned.Add(style); return style;
    }

    private void AddScrollDefaults()
    {
        var horizontal = CreateScrollStyle(new(.1f, .1f, .1f, .6f), 0, 4, 0, 4);
        var vertical = CreateScrollStyle(new(.1f, .1f, .1f, .6f), 4, 0, 4, 0);
        var grabber = CreateScrollStyle(new(1, 1, 1, .4f), 4, 4, 4, 4);
        var highlighted = CreateScrollStyle(new(1, 1, 1, .75f), 4, 4, 4, 4);
        var pressed = CreateScrollStyle(new(.75f, .75f, .75f, .75f), 4, 4, 4, 4);
        var focus = _defaultTheme.GetStyleBox("focus", "Button")!;
        var emptyIcon = new ImageTexture(); _owned.Add(emptyIcon);
        foreach (var type in new[] { "HScrollBar", "VScrollBar" })
        {
            _defaultTheme.SetStyleBox("scroll", type, type == "HScrollBar" ? horizontal : vertical);
            _defaultTheme.SetStyleBox("scroll_focus", type, focus);
            _defaultTheme.SetStyleBox("grabber", type, grabber);
            _defaultTheme.SetStyleBox("grabber_highlight", type, highlighted);
            _defaultTheme.SetStyleBox("grabber_pressed", type, pressed);
            foreach (var icon in new[] { "increment", "increment_highlight", "increment_pressed", "decrement", "decrement_highlight", "decrement_pressed" })
                _defaultTheme.SetIcon(icon, type, emptyIcon);
        }

        var panel = new StyleBoxEmpty(); _owned.Add(panel);
        _defaultTheme.SetStyleBox("panel", "ScrollContainer", panel);
        var containerFocus = CreateButtonStyle(new(1, 1, 1, .75f));
        containerFocus.SetExpandMarginAll(4); containerFocus.SetBorderWidthAll(2);
        containerFocus.DrawCenter = false; containerFocus.BorderColor = new(1, 1, 1, .75f);
        _defaultTheme.SetStyleBox("focus", "ScrollContainer", containerFocus);
        _defaultTheme.SetIcon("scroll_hint_horizontal", "ScrollContainer", LoadButtonIcon("scroll_hint_horizontal"));
        _defaultTheme.SetIcon("scroll_hint_vertical", "ScrollContainer", LoadButtonIcon("scroll_hint_vertical"));
        _defaultTheme.SetColor("scroll_hint_horizontal_color", "ScrollContainer", Colors.Black);
        _defaultTheme.SetColor("scroll_hint_vertical_color", "ScrollContainer", Colors.Black);
    }
}
