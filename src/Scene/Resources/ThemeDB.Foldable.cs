namespace Electron2D;

public sealed partial class ThemeDB
{
    private void AddFoldableDefaults()
    {
        var title = new StyleBoxFlat { BGColor = new(.175f, .175f, .175f), CornerDetail = 5 }; title.SetContentMarginAll(8); title.SetCornerRadiusAll(3);
        var hover = (StyleBoxFlat)title.Duplicate(); hover.BGColor = new(.21f, .21f, .21f);
        var panel = new StyleBoxFlat { BGColor = new(.1f, .1f, .1f, .6f), CornerDetail = 5 }; panel.SetContentMarginAll(8); panel.SetCornerRadiusAll(3);
        _owned.Add(title); _owned.Add(hover); _owned.Add(panel);
        foreach (var name in new[] { "title_panel", "title_collapsed_panel" }) _defaultTheme.SetStyleBox(name, "FoldableContainer", title);
        foreach (var name in new[] { "title_hover_panel", "title_collapsed_hover_panel" }) _defaultTheme.SetStyleBox(name, "FoldableContainer", hover);
        _defaultTheme.SetStyleBox("panel", "FoldableContainer", panel); _defaultTheme.SetStyleBox("focus", "FoldableContainer", _defaultTheme.GetStyleBox("focus", "Button"));
        foreach (var name in new[] { "expanded_arrow", "expanded_arrow_mirrored", "folded_arrow", "folded_arrow_mirrored" })
        {
            var points = name switch { "expanded_arrow" => "2,5 10,5 6,9", "expanded_arrow_mirrored" => "2,9 10,9 6,5", "folded_arrow" => "4,2 8,6 4,10", _ => "8,2 4,6 8,10" };
            var icon = CreateIcon(System.Text.Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"12\" height=\"12\"><polygon fill=\"#dddddd\" points=\"" + points + "\"/></svg>")); _defaultTheme.SetIcon(name, "FoldableContainer", icon);
        }
        _defaultTheme.SetFont("font", "FoldableContainer", null); _defaultTheme.SetFontSize("font_size", "FoldableContainer", -1);
        _defaultTheme.SetColor("font_color", "FoldableContainer", new(.875f, .875f, .875f)); _defaultTheme.SetColor("collapsed_font_color", "FoldableContainer", Colors.White);
        _defaultTheme.SetColor("hover_font_color", "FoldableContainer", new(.95f, .95f, .95f)); _defaultTheme.SetColor("font_outline_color", "FoldableContainer", Colors.White);
        _defaultTheme.SetConstant("outline_size", "FoldableContainer", 0); _defaultTheme.SetConstant("h_separation", "FoldableContainer", 2);
    }
}
