namespace Electron2D;

public sealed partial class ThemeDB
{
    private void AddTextEditDefaults()
    {
        foreach (var name in new[] { "normal", "read_only", "focus" }) _defaultTheme.SetStyleBox(name, "TextEdit", _defaultTheme.GetStyleBox(name, "LineEdit"));
        _defaultTheme.SetFont("font", "TextEdit", null); _defaultTheme.SetFontSize("font_size", "TextEdit", -1);
        foreach (var name in new[] { "font_color", "font_uneditable_color", "font_placeholder_color", "font_selected_color", "font_outline_color", "caret_color", "selection_color" }) _defaultTheme.SetColor(name, "TextEdit", _defaultTheme.GetColor(name, "LineEdit"));
        _defaultTheme.SetColor("font_readonly_color", "TextEdit", new(.875f, .875f, .875f, .5f)); _defaultTheme.SetColor("font_selected_color", "TextEdit", new(0, 0, 0, 0)); _defaultTheme.SetColor("current_line_color", "TextEdit", new(1, 1, 1, .08f)); _defaultTheme.SetColor("search_result_color", "TextEdit", new(1, .7f, .2f, .6f)); _defaultTheme.SetColor("search_result_border_color", "TextEdit", new(1, .7f, .2f)); _defaultTheme.SetColor("word_highlighted_color", "TextEdit", new(1, 1, 1, .12f)); _defaultTheme.SetColor("caret_background_color", "TextEdit", Colors.Black);
        _defaultTheme.SetConstant("minimum_character_width", "TextEdit", 4); _defaultTheme.SetConstant("caret_width", "TextEdit", 1); _defaultTheme.SetConstant("wrap_offset", "TextEdit", 10); _defaultTheme.SetConstant("line_spacing", "TextEdit", 4); _defaultTheme.SetConstant("outline_size", "TextEdit", 0); _defaultTheme.SetConstant("minimap_char_size", "TextEdit", 2); _defaultTheme.SetConstant("minimap_char_spacing", "TextEdit", 1);
        _defaultTheme.SetIcon("space", "TextEdit", CreateIcon("""<svg xmlns="http://www.w3.org/2000/svg" width="8" height="8"><circle cx="4" cy="4" r="1" fill="#aaa"/></svg>"""u8));
        _defaultTheme.SetIcon("tab", "TextEdit", CreateIcon("""<svg xmlns="http://www.w3.org/2000/svg" width="12" height="8"><path d="M1 4h9m-3-3l3 3-3 3" stroke="#aaa" fill="none"/></svg>"""u8));
    }
}
