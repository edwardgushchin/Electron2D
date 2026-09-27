namespace Electron2D;

public sealed partial class ThemeDB
{
    private StyleBoxFlat CreateButtonStyle(Color color, float left = 4, float top = 4, float right = 4, float bottom = 4)
    {
        var style = new StyleBoxFlat { BGColor = color, CornerDetail = 5 };
        style.SetCornerRadiusAll(3);
        style.ContentMarginLeft = left; style.ContentMarginTop = top; style.ContentMarginRight = right; style.ContentMarginBottom = bottom;
        _owned.Add(style); return style;
    }
    private ImageTexture LoadButtonIcon(string name)
    {
        using var stream = typeof(ThemeDB).Assembly.GetManifestResourceStream("Electron2D.Icons." + name + ".svg")
            ?? throw new InvalidOperationException("A built-in button icon is missing.");
        var bytes = new byte[checked((int)stream.Length)]; stream.ReadExactly(bytes); return CreateIcon(bytes);
    }
    private void AddButtonTextDefaults(string type)
    {
        _defaultTheme.SetFont("font", type, null); _defaultTheme.SetFontSize("font_size", type, -1);
        _defaultTheme.SetColor("font_color", type, new(.875f, .875f, .875f));
        _defaultTheme.SetColor("font_pressed_color", type, Colors.White);
        _defaultTheme.SetColor("font_hover_color", type, new(.95f, .95f, .95f));
        _defaultTheme.SetColor("font_focus_color", type, new(.95f, .95f, .95f));
        _defaultTheme.SetColor("font_hover_pressed_color", type, Colors.White);
        _defaultTheme.SetColor("font_disabled_color", type, new(.875f, .875f, .875f, .5f));
        _defaultTheme.SetColor("font_outline_color", type, Colors.Black);
        _defaultTheme.SetConstant("h_separation", type, 4); _defaultTheme.SetConstant("outline_size", type, 0);
    }
    private void AddButtonDefaults()
    {
        var normal = CreateButtonStyle(new(.1f, .1f, .1f, .6f));
        var hover = CreateButtonStyle(new(.225f, .225f, .225f, .6f));
        var pressed = CreateButtonStyle(new(0, 0, 0, .6f));
        var disabled = CreateButtonStyle(new(.1f, .1f, .1f, .3f));
        var focus = _defaultTheme.GetStyleBox("focus", "Label")!;
        _defaultTheme.SetStyleBox("normal", "Button", normal); _defaultTheme.SetStyleBox("hover", "Button", hover);
        _defaultTheme.SetStyleBox("pressed", "Button", pressed); _defaultTheme.SetStyleBox("disabled", "Button", disabled);
        _defaultTheme.SetStyleBox("focus", "Button", focus); AddButtonTextDefaults("Button");
        foreach (var key in new[] { "icon_normal_color", "icon_pressed_color", "icon_hover_color", "icon_hover_pressed_color", "icon_focus_color" })
            _defaultTheme.SetColor(key, "Button", Colors.White);
        _defaultTheme.SetColor("icon_disabled_color", "Button", new(1, 1, 1, .4f));
        _defaultTheme.SetConstant("icon_max_width", "Button", 0); _defaultTheme.SetConstant("align_to_largest_stylebox", "Button", 0);

        foreach (var type in new[] { "CheckBox", "CheckButton" })
        {
            var empty = new StyleBoxEmpty(); _owned.Add(empty);
            empty.SetContentMarginAll(4);
            if (type == "CheckButton") { empty.ContentMarginLeft = 6; empty.ContentMarginRight = 6; }
            foreach (var state in new[] { "normal", "pressed", "disabled", "hover", "hover_pressed" }) _defaultTheme.SetStyleBox(state, type, empty);
            _defaultTheme.SetStyleBox("focus", type, focus); AddButtonTextDefaults(type);
            _defaultTheme.SetConstant("check_v_offset", type, 0);
        }
        foreach (var key in new[] { "checked", "checked_disabled", "unchecked", "unchecked_disabled", "radio_checked", "radio_checked_disabled", "radio_unchecked", "radio_unchecked_disabled" })
            _defaultTheme.SetIcon(key, "CheckBox", LoadButtonIcon(key));
        _defaultTheme.SetColor("checkbox_checked_color", "CheckBox", Colors.White); _defaultTheme.SetColor("checkbox_unchecked_color", "CheckBox", Colors.White);
        foreach (var suffix in new[] { "", "_disabled", "_mirrored", "_disabled_mirrored" })
        {
            _defaultTheme.SetIcon("checked" + suffix, "CheckButton", LoadButtonIcon("toggle_on" + suffix));
            _defaultTheme.SetIcon("unchecked" + suffix, "CheckButton", LoadButtonIcon("toggle_off" + suffix));
        }
        _defaultTheme.SetColor("button_checked_color", "CheckButton", Colors.White); _defaultTheme.SetColor("button_unchecked_color", "CheckButton", Colors.White);

        _defaultTheme.SetTypeVariation("FlatButton", "Button");
        var flat = new StyleBoxEmpty(); flat.SetContentMarginAll(4); _owned.Add(flat);
        var flatPressed = CreateButtonStyle(new(0, 0, 0, .6f * .85f));
        foreach (var state in new[] { "normal", "hover", "disabled" }) _defaultTheme.SetStyleBox(state, "FlatButton", flat);
        _defaultTheme.SetStyleBox("pressed", "FlatButton", flatPressed);

        _defaultTheme.SetTypeVariation("TooltipPanel", "PanelContainer");
        _defaultTheme.SetStyleBox("panel", "TooltipPanel", CreateButtonStyle(new(0, 0, 0, .5f), 8, 2, 8, 2));
        _defaultTheme.SetTypeVariation("TooltipLabel", "Label");
        _defaultTheme.SetFont("font", "TooltipLabel", null); _defaultTheme.SetFontSize("font_size", "TooltipLabel", -1);
        _defaultTheme.SetColor("font_color", "TooltipLabel", new(.875f, .875f, .875f));
        _defaultTheme.SetColor("font_shadow_color", "TooltipLabel", new(0, 0, 0, 0));
        _defaultTheme.SetColor("font_outline_color", "TooltipLabel", Colors.Black);
        _defaultTheme.SetConstant("shadow_offset_x", "TooltipLabel", 1); _defaultTheme.SetConstant("shadow_offset_y", "TooltipLabel", 1);
        _defaultTheme.SetConstant("outline_size", "TooltipLabel", 0);
    }
}
