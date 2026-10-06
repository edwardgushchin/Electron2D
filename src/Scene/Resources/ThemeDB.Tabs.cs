namespace Electron2D;

public sealed partial class ThemeDB
{
    private void AddTabDefaults()
    {
        const string type = "TabBar";
        _defaultTheme.SetStyleBox("tab_selected", type, CreateButtonStyle(new(.18f, .18f, .18f), 8, 5, 8, 5));
        _defaultTheme.SetStyleBox("tab_unselected", type, CreateButtonStyle(new(.1f, .1f, .1f), 8, 5, 8, 5));
        _defaultTheme.SetStyleBox("tab_hovered", type, CreateButtonStyle(new(.14f, .14f, .14f), 8, 5, 8, 5));
        _defaultTheme.SetStyleBox("tab_disabled", type, CreateButtonStyle(new(.08f, .08f, .08f), 8, 5, 8, 5));
        var focus = CreateButtonStyle(new(1, 1, 1, .75f), 8, 5, 8, 5); focus.DrawCenter = false; focus.SetBorderWidthAll(2); _defaultTheme.SetStyleBox("tab_focus", type, focus);
        _defaultTheme.SetStyleBox("button_highlight", type, CreateButtonStyle(new(1, 1, 1, .12f), 2, 2, 2, 2));
        _defaultTheme.SetStyleBox("button_pressed", type, CreateButtonStyle(new(1, 1, 1, .25f), 2, 2, 2, 2));
        _defaultTheme.SetFont("font", type, null); _defaultTheme.SetFontSize("font_size", type, -1);
        _defaultTheme.SetConstant("h_separation", type, 4); _defaultTheme.SetConstant("tab_separation", type, 0);
        _defaultTheme.SetConstant("hover_switch_wait_msec", type, 500); _defaultTheme.SetConstant("icon_max_width", type, 0); _defaultTheme.SetConstant("outline_size", type, 0);
        _defaultTheme.SetColor("font_disabled_color", type, new(.875f, .875f, .875f, .5f));
        _defaultTheme.SetColor("font_hovered_color", type, new(.95f, .95f, .95f)); _defaultTheme.SetColor("font_selected_color", type, new(.95f, .95f, .95f));
        _defaultTheme.SetColor("font_unselected_color", type, new(.7f, .7f, .7f)); _defaultTheme.SetColor("font_outline_color", type, Colors.Black);
        foreach (var key in new[] { "icon_disabled_color", "icon_hovered_color", "icon_selected_color", "icon_unselected_color", "drop_mark_color" }) _defaultTheme.SetColor(key, type, Colors.White);
        var close = CreateIcon("""<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16"><path d="M4 4L12 12M12 4L4 12" fill="none" stroke="#ddd" stroke-width="2"/></svg>"""u8);
        var left = CreateIcon("""<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16"><path d="M10 3L5 8L10 13" fill="none" stroke="#ddd" stroke-width="2"/></svg>"""u8);
        var right = CreateIcon("""<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16"><path d="M6 3L11 8L6 13" fill="none" stroke="#ddd" stroke-width="2"/></svg>"""u8);
        var mark = CreateIcon("""<svg xmlns="http://www.w3.org/2000/svg" width="8" height="24"><path d="M1 1L7 1L4 5L4 23" fill="none" stroke="#fff" stroke-width="2"/></svg>"""u8);
        _defaultTheme.SetIcon("close", type, close); _defaultTheme.SetIcon("decrement", type, left); _defaultTheme.SetIcon("decrement_highlight", type, left);
        _defaultTheme.SetIcon("increment", type, right); _defaultTheme.SetIcon("increment_highlight", type, right); _defaultTheme.SetIcon("drop_mark", type, mark);
    }
}
