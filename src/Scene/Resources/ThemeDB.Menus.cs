namespace Electron2D;

public sealed partial class ThemeDB
{
    private void AddMenuDefaults()
    {
        const string bar = "MenuBar";
        foreach (var state in new[] { "normal", "disabled", "hover", "pressed", "hover_pressed" })
        {
            var color = state is "hover" or "hover_pressed" ? new Color(.25f, .25f, .25f) : state == "pressed" ? new Color(.15f, .15f, .15f) : new Color(.2f, .2f, .2f);
            var style = CreateButtonStyle(color, 6, 4, 6, 4);
            _defaultTheme.SetStyleBox(state, bar, style); _defaultTheme.SetStyleBox(state + "_mirrored", bar, style);
        }
        _defaultTheme.SetFont("font", bar, null); _defaultTheme.SetFontSize("font_size", bar, -1);
        _defaultTheme.SetConstant("h_separation", bar, 4); _defaultTheme.SetConstant("outline_size", bar, 0);
        _defaultTheme.SetColor("font_color", bar, new(.875f, .875f, .875f)); _defaultTheme.SetColor("font_disabled_color", bar, new(.875f, .875f, .875f, .5f));
        _defaultTheme.SetColor("font_focus_color", bar, new(.95f, .95f, .95f)); _defaultTheme.SetColor("font_hover_color", bar, new(.95f, .95f, .95f));
        _defaultTheme.SetColor("font_pressed_color", bar, Colors.White); _defaultTheme.SetColor("font_hover_pressed_color", bar, Colors.White); _defaultTheme.SetColor("font_outline_color", bar, Colors.Black);
        const string type = "PopupMenu";
        _defaultTheme.SetStyleBox("panel", type, CreateButtonStyle(new(.1f, .1f, .1f, .98f), 6, 6, 6, 6));
        _defaultTheme.SetStyleBox("hover", type, CreateButtonStyle(new(.3f, .3f, .3f, .8f), 0, 0, 0, 0));
        foreach (var key in new[] { "separator", "labeled_separator_left", "labeled_separator_right" })
        { var style = new StyleBoxLine { Color = new(.5f, .5f, .5f), Thickness = 1 }; _owned.Add(style); _defaultTheme.SetStyleBox(key, type, style); }
        foreach (var key in new[] { "font_color", "font_hover_color", "font_separator_color" }) _defaultTheme.SetColor(key, type, new(.875f, .875f, .875f));
        _defaultTheme.SetColor("font_disabled_color", type, new(.4f, .4f, .4f, .8f)); _defaultTheme.SetColor("font_accelerator_color", type, new(.7f, .7f, .7f, .8f));
        _defaultTheme.SetColor("font_outline_color", type, Colors.Black); _defaultTheme.SetColor("font_separator_outline_color", type, Colors.Black);
        foreach (var key in new[] { "font", "font_separator" }) _defaultTheme.SetFont(key, type, null);
        foreach (var key in new[] { "font_size", "font_separator_size" }) _defaultTheme.SetFontSize(key, type, -1);
        foreach (var pair in new (string, int)[] { ("gutter_compact", 1), ("h_separation", 4), ("v_separation", 4), ("icon_max_width", 0), ("indent", 10), ("item_start_padding", 2), ("item_end_padding", 2), ("outline_size", 0), ("separator_outline_size", 0), ("search_bar_separation", 4) }) _defaultTheme.SetConstant(pair.Item1, type, pair.Item2);
        var checkSource = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"14\" height=\"14\" viewBox=\"0 0 14 14\"><path d=\"M3 7L6 10L11 4\" fill=\"none\" stroke=\"#dddddd\" stroke-width=\"2\"/></svg>"; var check = CreateIcon(System.Text.Encoding.UTF8.GetBytes(checkSource)); var checkDisabled = CreateIcon(System.Text.Encoding.UTF8.GetBytes(checkSource.Replace("#dddddd", "#666666").Replace("#aaaaaa", "#555555")));
        var squareSource = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"14\" height=\"14\"><rect x=\"2\" y=\"2\" width=\"10\" height=\"10\" fill=\"none\" stroke=\"#aaaaaa\"/></svg>"; var square = CreateIcon(System.Text.Encoding.UTF8.GetBytes(squareSource)); var squareDisabled = CreateIcon(System.Text.Encoding.UTF8.GetBytes(squareSource.Replace("#dddddd", "#666666").Replace("#aaaaaa", "#555555")));
        var radioSource = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"14\" height=\"14\"><circle cx=\"7\" cy=\"7\" r=\"5\" fill=\"none\" stroke=\"#aaaaaa\"/><circle cx=\"7\" cy=\"7\" r=\"3\" fill=\"#dddddd\"/></svg>"; var radio = CreateIcon(System.Text.Encoding.UTF8.GetBytes(radioSource)); var radioDisabled = CreateIcon(System.Text.Encoding.UTF8.GetBytes(radioSource.Replace("#dddddd", "#666666").Replace("#aaaaaa", "#555555")));
        var circleSource = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"14\" height=\"14\"><circle cx=\"7\" cy=\"7\" r=\"5\" fill=\"none\" stroke=\"#aaaaaa\"/></svg>"; var circle = CreateIcon(System.Text.Encoding.UTF8.GetBytes(circleSource)); var circleDisabled = CreateIcon(System.Text.Encoding.UTF8.GetBytes(circleSource.Replace("#dddddd", "#666666").Replace("#aaaaaa", "#555555")));
        foreach (var disabled in new[] { "", "_disabled" }) { _defaultTheme.SetIcon("checked" + disabled, type, disabled.Length == 0 ? check : checkDisabled); _defaultTheme.SetIcon("unchecked" + disabled, type, disabled.Length == 0 ? square : squareDisabled); _defaultTheme.SetIcon("radio_checked" + disabled, type, disabled.Length == 0 ? radio : radioDisabled); _defaultTheme.SetIcon("radio_unchecked" + disabled, type, disabled.Length == 0 ? circle : circleDisabled); }
        var arrow = CreateIcon(System.Text.Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"8\" height=\"12\"><path d=\"M2 2L6 6L2 10\" fill=\"none\" stroke=\"#dddddd\" stroke-width=\"2\"/></svg>"));
        var reverse = CreateIcon(System.Text.Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"8\" height=\"12\"><path d=\"M6 2L2 6L6 10\" fill=\"none\" stroke=\"#dddddd\" stroke-width=\"2\"/></svg>"));
        _defaultTheme.SetIcon("submenu", type, arrow); _defaultTheme.SetIcon("submenu_mirrored", type, reverse); _defaultTheme.SetIcon("search", type, CreateIcon(System.Text.Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"14\" height=\"14\"><circle cx=\"6\" cy=\"6\" r=\"4\" fill=\"none\" stroke=\"#aaaaaa\"/><path d=\"M9 9L13 13\" stroke=\"#aaaaaa\" stroke-width=\"2\"/></svg>")));
    }
}
