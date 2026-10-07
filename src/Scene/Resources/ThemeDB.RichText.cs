namespace Electron2D;

public sealed partial class ThemeDB
{
    private void AddRichTextDefaults()
    {
        _defaultTheme.SetIcon("horizontal_rule", "RichTextLabel", CreateIcon("""<svg xmlns="http://www.w3.org/2000/svg" width="16" height="2"><path fill="#fff" d="M0 0h16v2H0z"/></svg>"""u8));
        _defaultTheme.SetFontSize("bold_font_size", "RichTextLabel", 16);
        _defaultTheme.SetFontSize("bold_italics_font_size", "RichTextLabel", 16);
        _defaultTheme.SetColor("default_color", "RichTextLabel", new(1f, 1f, 1f, 1f));
        _defaultTheme.SetColor("font_outline_color", "RichTextLabel", new(0f, 0f, 0f, 1f));
        _defaultTheme.SetColor("font_selected_color", "RichTextLabel", new(0f, 0f, 0f, 0f));
        _defaultTheme.SetColor("font_shadow_color", "RichTextLabel", new(0f, 0f, 0f, 0f));
        _defaultTheme.SetFontSize("italics_font_size", "RichTextLabel", 16);
        _defaultTheme.SetConstant("line_separation", "RichTextLabel", 0);
        _defaultTheme.SetFontSize("mono_font_size", "RichTextLabel", 16);
        _defaultTheme.SetFontSize("normal_font_size", "RichTextLabel", 16);
        _defaultTheme.SetConstant("outline_size", "RichTextLabel", 0);
        _defaultTheme.SetConstant("paragraph_separation", "RichTextLabel", 0);
        _defaultTheme.SetColor("selection_color", "RichTextLabel", new(0.1f, 0.1f, 1f, 0.8f));
        _defaultTheme.SetConstant("shadow_offset_x", "RichTextLabel", 1);
        _defaultTheme.SetConstant("shadow_offset_y", "RichTextLabel", 1);
        _defaultTheme.SetConstant("shadow_outline_size", "RichTextLabel", 1);
        _defaultTheme.SetConstant("strikethrough_alpha", "RichTextLabel", 50);
        _defaultTheme.SetColor("table_border", "RichTextLabel", new(0f, 0f, 0f, 0f));
        _defaultTheme.SetColor("table_even_row_bg", "RichTextLabel", new(0f, 0f, 0f, 0f));
        _defaultTheme.SetConstant("table_h_separation", "RichTextLabel", 3);
        _defaultTheme.SetColor("table_odd_row_bg", "RichTextLabel", new(0f, 0f, 0f, 0f));
        _defaultTheme.SetConstant("table_v_separation", "RichTextLabel", 3);
        _defaultTheme.SetConstant("text_highlight_h_padding", "RichTextLabel", 3);
        _defaultTheme.SetConstant("text_highlight_v_padding", "RichTextLabel", 3);
        _defaultTheme.SetConstant("underline_alpha", "RichTextLabel", 50);
        FontFile LoadFont(string name) { using var stream = typeof(ThemeDB).Assembly.GetManifestResourceStream("Electron2D.Fonts." + name) ?? throw new InvalidOperationException("A rich-text font is missing."); var bytes = new byte[checked((int)stream.Length)]; stream.ReadExactly(bytes); var font = new FontFile(bytes); _owned.Add(font); return font; }
        var fallback = LoadFont("Vazirmatn_Regular.woff2"); FontFile WithFallback(string name) { var font = LoadFont(name); font.Fallbacks = [fallback]; return font; }
        var normal = (FontFile)_font!.Duplicate(true); normal.Fallbacks = [fallback]; _owned.Add(normal); _defaultTheme.SetFont("normal_font", "RichTextLabel", normal);
        _defaultTheme.SetFont("bold_font", "RichTextLabel", WithFallback("OpenSans-Bold.ttf"));
        _defaultTheme.SetFont("italics_font", "RichTextLabel", WithFallback("OpenSans-SemiBoldItalic.ttf"));
        _defaultTheme.SetFont("bold_italics_font", "RichTextLabel", WithFallback("OpenSans-BoldItalic.ttf"));
        _defaultTheme.SetFont("mono_font", "RichTextLabel", WithFallback("JetBrainsMono_Regular.woff2"));
        _defaultTheme.SetStyleBox("normal", "RichTextLabel", CreateButtonStyle(new(.13f, .13f, .13f), 4, 4, 4, 4));
        var focus = new StyleBoxFlat { DrawCenter = false, BorderColor = new(.8f, .8f, 1) }; focus.SetBorderWidthAll(1); _owned.Add(focus); _defaultTheme.SetStyleBox("focus", "RichTextLabel", focus);
    }
}
