namespace Electron2D;

public sealed partial class ThemeDB
{
    private void AddWindowDefaults()
    {
        const string type = "Window";
        foreach (var (key, color) in new[] { ("embedded_border", new Color(.13f, .13f, .13f)), ("embedded_unfocused_border", new Color(.1f, .1f, .1f)) })
        {
            var style = CreateButtonStyle(color, 10, 28, 10, 8); style.ExpandMarginLeft = 8; style.ExpandMarginTop = 32; style.ExpandMarginRight = 8; style.ExpandMarginBottom = 6;
            _defaultTheme.SetStyleBox(key, type, style);
        }
        _defaultTheme.SetFont("title_font", type, null); _defaultTheme.SetFontSize("title_font_size", type, -1);
        _defaultTheme.SetColor("title_color", type, new(.875f, .875f, .875f)); _defaultTheme.SetColor("title_outline_modulate", type, Colors.Black);
        _defaultTheme.SetConstant("title_height", type, 36); _defaultTheme.SetConstant("resize_margin", type, 4); _defaultTheme.SetConstant("title_outline_size", type, 0);
        _defaultTheme.SetConstant("close_h_offset", type, 18); _defaultTheme.SetConstant("close_v_offset", type, 24);
        var close = CreateIcon("""<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16"><path d="M4 4L12 12M12 4L4 12" fill="none" stroke="#ddd" stroke-width="2"/></svg>"""u8);
        var pressed = CreateIcon("""<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16"><path d="M4 4L12 12M12 4L4 12" fill="none" stroke="#fff" stroke-width="2"/></svg>"""u8);
        _defaultTheme.SetIcon("close", type, close); _defaultTheme.SetIcon("close_pressed", type, pressed);
    }
}
