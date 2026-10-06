namespace Electron2D;

public sealed partial class ThemeDB
{
    private void AddDialogDefaults()
    {
        var style = new StyleBoxFlat { BGColor = new(.13f, .13f, .13f), CornerDetail = 5 };
        style.SetContentMarginAll(8); style.SetCornerRadiusAll(3); _owned.Add(style);
        _defaultTheme.SetStyleBox("panel", "AcceptDialog", style);
        _defaultTheme.SetConstant("buttons_separation", "AcceptDialog", 10);
        _defaultTheme.SetConstant("buttons_min_width", "AcceptDialog", 0);
        _defaultTheme.SetConstant("buttons_min_height", "AcceptDialog", 0);
    }
}
