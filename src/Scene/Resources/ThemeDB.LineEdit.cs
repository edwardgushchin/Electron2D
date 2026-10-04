namespace Electron2D;

public sealed partial class ThemeDB
{
    private void AddLineEditDefaults()
    {
        _defaultTheme.SetStyleBox("normal", "LineEdit", CreateButtonStyle(new(.1f, .1f, .1f, .6f)));
        _defaultTheme.SetStyleBox("read_only", "LineEdit", CreateButtonStyle(new(.1f, .1f, .1f, .3f)));
        _defaultTheme.SetStyleBox("focus", "LineEdit", _defaultTheme.GetStyleBox("focus", "Label"));
        _defaultTheme.SetFont("font", "LineEdit", null); _defaultTheme.SetFontSize("font_size", "LineEdit", -1);
        _defaultTheme.SetColor("font_color", "LineEdit", new(.875f, .875f, .875f));
        _defaultTheme.SetColor("font_uneditable_color", "LineEdit", new(.875f, .875f, .875f, .5f));
        _defaultTheme.SetColor("font_placeholder_color", "LineEdit", new(.875f, .875f, .875f, .6f));
        _defaultTheme.SetColor("font_selected_color", "LineEdit", Colors.White); _defaultTheme.SetColor("font_outline_color", "LineEdit", Colors.Black);
        _defaultTheme.SetColor("caret_color", "LineEdit", new(.875f, .875f, .875f));
        _defaultTheme.SetColor("selection_color", "LineEdit", new(.5f, .5f, .5f));
        foreach (var name in new[] { "right_icon_modulate", "clear_button_color", "clear_button_color_pressed" }) _defaultTheme.SetColor(name, "LineEdit", Colors.White);
        _defaultTheme.SetConstant("minimum_character_width", "LineEdit", 4); _defaultTheme.SetConstant("caret_width", "LineEdit", 1);
        _defaultTheme.SetConstant("h_separation", "LineEdit", 4); _defaultTheme.SetConstant("outline_size", "LineEdit", 0);
        _defaultTheme.SetIcon("clear", "LineEdit", CreateIcon("""<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16"><path d="M4 4l8 8m0-8l-8 8" stroke="#fff" stroke-width="2"/></svg>"""u8));
    }
}
