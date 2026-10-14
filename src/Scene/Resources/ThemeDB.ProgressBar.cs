namespace Electron2D;

public sealed partial class ThemeDB
{
    private void AddProgressBarDefaults()
    {
        var background = new StyleBoxFlat { BGColor = new(.1f, .1f, .1f, .3f), CornerDetail = 6 };
        var fill = new StyleBoxFlat { BGColor = new(1, 1, 1, .4f), CornerDetail = 6 };
        background.SetContentMarginAll(2); background.SetCornerRadiusAll(6);
        fill.SetContentMarginAll(2); fill.SetCornerRadiusAll(6);
        _owned.Add(background); _owned.Add(fill);
        _defaultTheme.SetStyleBox("background", "ProgressBar", background); _defaultTheme.SetStyleBox("fill", "ProgressBar", fill);
        _defaultTheme.SetFont("font", "ProgressBar", null); _defaultTheme.SetFontSize("font_size", "ProgressBar", -1);
        _defaultTheme.SetColor("font_color", "ProgressBar", new(.95f, .95f, .95f));
        _defaultTheme.SetColor("font_outline_color", "ProgressBar", new(0, 0, 0)); _defaultTheme.SetConstant("outline_size", "ProgressBar", 0);
    }
}
