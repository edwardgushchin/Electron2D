namespace Electron2D;

public sealed partial class ThemeDB
{
    private void AddItemListDefaults()
    {
        const string type = "ItemList";
        _defaultTheme.SetStyleBox("panel", type, CreateButtonStyle(new(.1f, .1f, .1f, .6f)));
        var focus = _defaultTheme.GetStyleBox("focus", "Button")!;
        _defaultTheme.SetStyleBox("focus", type, focus);
        _defaultTheme.SetStyleBox("cursor", type, focus);
        _defaultTheme.SetStyleBox("cursor_unfocused", type, focus);
        _defaultTheme.SetStyleBox("hovered", type, CreateButtonStyle(new(1, 1, 1, .07f)));
        var hoveredSelected = CreateButtonStyle(new(1, 1, 1, .4f));
        _defaultTheme.SetStyleBox("hovered_selected", type, hoveredSelected);
        _defaultTheme.SetStyleBox("hovered_selected_focus", type, hoveredSelected);
        var selected = CreateButtonStyle(new(1, 1, 1, .3f));
        _defaultTheme.SetStyleBox("selected", type, selected);
        _defaultTheme.SetStyleBox("selected_focus", type, selected);
        _defaultTheme.SetConstant("h_separation", type, 4);
        _defaultTheme.SetConstant("v_separation", type, 4);
        _defaultTheme.SetConstant("icon_margin", type, 4);
        _defaultTheme.SetConstant("line_separation", type, 2);
        _defaultTheme.SetConstant("outline_size", type, 0);
        _defaultTheme.SetFont("font", type, null);
        _defaultTheme.SetFontSize("font_size", type, -1);
        _defaultTheme.SetColor("font_color", type, new(.65f, .65f, .65f));
        _defaultTheme.SetColor("font_hovered_color", type, new(.95f, .95f, .95f));
        _defaultTheme.SetColor("font_hovered_selected_color", type, Colors.White);
        _defaultTheme.SetColor("font_selected_color", type, Colors.White);
        _defaultTheme.SetColor("font_outline_color", type, Colors.Black);
        _defaultTheme.SetColor("guide_color", type, new(.7f, .7f, .7f, .25f));
        _defaultTheme.SetColor("scroll_hint_color", type, Colors.Black);
        _defaultTheme.SetIcon("scroll_hint", type, _defaultTheme.GetIcon("scroll_hint_vertical", "ScrollContainer")!);
    }
}
