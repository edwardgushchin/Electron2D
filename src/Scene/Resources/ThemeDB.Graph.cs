namespace Electron2D;

public sealed partial class ThemeDB
{
    private void AddGraphDefaults()
    {
        _defaultTheme.SetTypeVariation("GraphNodeTitleLabel", "Label"); _defaultTheme.SetTypeVariation("GraphFrameTitleLabel", "Label");
        var resizer = CreateIcon("""<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16"><path stroke="#fff" stroke-width="2" d="M4 14L14 4M9 14L14 9"/></svg>"""u8);
        var port = CreateIcon("""<svg xmlns="http://www.w3.org/2000/svg" width="12" height="12"><circle cx="6" cy="6" r="5" fill="#fff"/></svg>"""u8);
        foreach (var type in new[] { "GraphElement", "GraphNode", "GraphFrame" }) _defaultTheme.SetIcon("resizer", type, resizer);
        _defaultTheme.SetIcon("port", "GraphNode", port); _defaultTheme.SetConstant("port_h_offset", "GraphNode", 0); _defaultTheme.SetConstant("separation", "GraphNode", 2);
        foreach (var type in new[] { "GraphNode", "GraphFrame" })
        {
            var panel = CreateButtonStyle(new(.15f, .17f, .2f, type == "GraphFrame" ? .5f : 1), 8, 8, 8, 8);
            var selected = CreateButtonStyle(new(.18f, .22f, .28f, type == "GraphFrame" ? .5f : 1), 8, 8, 8, 8); selected.BorderColor = new(.55f, .75f, 1); selected.SetBorderWidthAll(2);
            _defaultTheme.SetStyleBox("panel", type, panel); _defaultTheme.SetStyleBox("panel_selected", type, selected);
            _defaultTheme.SetStyleBox("titlebar", type, CreateButtonStyle(new(.2f, .24f, .3f), 8, 4, 8, 4));
            _defaultTheme.SetStyleBox("titlebar_selected", type, CreateButtonStyle(new(.25f, .35f, .5f), 8, 4, 8, 4));
            _defaultTheme.SetColor("resizer_color", type, new(.875f, .875f, .875f));
        }
        var focus = CreateButtonStyle(new(0, 0, 0, 0), 0, 0, 0, 0); focus.DrawCenter = false; focus.BorderColor = new(.75f, .8f, 1); focus.SetBorderWidthAll(1);
        _defaultTheme.SetStyleBox("panel_focus", "GraphNode", focus); _defaultTheme.SetStyleBox("panel_focus", "GraphEdit", focus);
        _defaultTheme.SetStyleBox("slot", "GraphNode", CreateButtonStyle(new(0, 0, 0, 0), 0, 0, 0, 0));
        _defaultTheme.SetStyleBox("slot_selected", "GraphNode", CreateButtonStyle(new(.4f, .5f, .7f, .3f), 0, 0, 0, 0));
        _defaultTheme.SetStyleBox("panel", "GraphEdit", CreateButtonStyle(new(.08f, .09f, .11f), 0, 0, 0, 0));
        _defaultTheme.SetStyleBox("menu_panel", "GraphEdit", CreateButtonStyle(new(.12f, .14f, .17f), 4, 4, 4, 4));
        _defaultTheme.SetColor("activity", "GraphEdit", Colors.White);
        _defaultTheme.SetColor("connection_hover_tint_color", "GraphEdit", new(0, 0, 0, .3f));
        _defaultTheme.SetColor("connection_rim_color", "GraphEdit", new(.1f, .1f, .1f, .6f));
        _defaultTheme.SetColor("connection_valid_target_tint_color", "GraphEdit", new(1, 1, 1, .4f));
        _defaultTheme.SetColor("grid_major", "GraphEdit", new(1, 1, 1, .2f)); _defaultTheme.SetColor("grid_minor", "GraphEdit", new(1, 1, 1, .05f));
        _defaultTheme.SetColor("selection_fill", "GraphEdit", new(1, 1, 1, .3f)); _defaultTheme.SetColor("selection_stroke", "GraphEdit", new(1, 1, 1, .8f));
        _defaultTheme.SetConstant("connection_hover_thickness", "GraphEdit", 0); _defaultTheme.SetConstant("port_hotzone_inner_extent", "GraphEdit", 22); _defaultTheme.SetConstant("port_hotzone_outer_extent", "GraphEdit", 26);
        _defaultTheme.SetIcon("zoom_in", "GraphEdit", CreateIcon("""<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16"><path stroke="#fff" stroke-width="2" d="M8 2v12M2 8h12"/></svg>"""u8));
        _defaultTheme.SetIcon("zoom_out", "GraphEdit", CreateIcon("""<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16"><path stroke="#fff" stroke-width="2" d="M2 8h12"/></svg>"""u8));
        _defaultTheme.SetIcon("zoom_reset", "GraphEdit", CreateIcon("""<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16"><circle cx="8" cy="8" r="5" stroke="#fff" fill="none"/></svg>"""u8));
        _defaultTheme.SetIcon("snapping_toggle", "GraphEdit", CreateIcon("""<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16"><path d="M3 2v7a5 5 0 0010 0V2h-3v7a2 2 0 01-4 0V2z" fill="#fff"/></svg>"""u8));
        _defaultTheme.SetIcon("grid_toggle", "GraphEdit", CreateIcon("""<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16"><path stroke="#fff" d="M1 5h14M1 10h14M5 1v14M10 1v14"/></svg>"""u8));
        _defaultTheme.SetIcon("minimap_toggle", "GraphEdit", CreateIcon("""<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16"><path stroke="#fff" fill="none" d="M1 1h14v14H1zM8 8h7v7H8z"/></svg>"""u8));
        _defaultTheme.SetIcon("layout", "GraphEdit", CreateIcon("""<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16"><path stroke="#fff" fill="none" d="M1 5h4v6H1zM11 1h4v6h-4zM11 9h4v6h-4zM5 8h3V4h3M8 8v4h3"/></svg>"""u8));
    }
}
