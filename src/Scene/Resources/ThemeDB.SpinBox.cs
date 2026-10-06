namespace Electron2D;

public sealed partial class ThemeDB
{
    private void AddSpinBoxDefaults()
    {
        var up = CreateIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"16\" height=\"8\"><path d=\"M4 6L8 2L12 6\" fill=\"none\" stroke=\"white\" stroke-width=\"2\"/></svg>"u8);
        var down = CreateIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"16\" height=\"8\"><path d=\"M4 2L8 6L12 2\" fill=\"none\" stroke=\"white\" stroke-width=\"2\"/></svg>"u8);
        var empty = new StyleBoxEmpty(); _owned.Add(empty);
        foreach (var side in new[] { "up", "down" })
        {
            foreach (var state in new[] { "", "_hover", "_pressed", "_disabled" })
            {
                _defaultTheme.SetIcon(side + state, "SpinBox", side == "up" ? up : down);
                _defaultTheme.SetColor(side + state + "_icon_modulate", "SpinBox", state == "_disabled" ? new(.875f, .875f, .875f, .5f) : state is "_hover" or "_pressed" ? new(.95f, .95f, .95f) : new(.875f, .875f, .875f));
                _defaultTheme.SetStyleBox(side + "_background" + (state == "_hover" ? "_hovered" : state), "SpinBox", empty);
            }
        }
        _defaultTheme.SetStyleBox("field_and_buttons_separator", "SpinBox", empty); _defaultTheme.SetStyleBox("up_down_buttons_separator", "SpinBox", empty);
        _defaultTheme.SetConstant("buttons_width", "SpinBox", 16); _defaultTheme.SetConstant("field_and_buttons_separation", "SpinBox", 2); _defaultTheme.SetConstant("buttons_vertical_separation", "SpinBox", 0);
        _defaultTheme.SetTypeVariation("SpinBoxInnerLineEdit", "LineEdit");
    }
}
