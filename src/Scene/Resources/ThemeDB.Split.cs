namespace Electron2D;

public sealed partial class ThemeDB
{
    private void AddSplitDefaults()
    {
        var horizontal = LoadButtonIcon("hsplitter"); var vertical = LoadButtonIcon("vsplitter");
        var touchHorizontal = LoadButtonIcon("h_dragger"); var touchVertical = LoadButtonIcon("v_dragger");
        var empty = new StyleBoxEmpty(); _owned.Add(empty);
        foreach (var type in new[] { "SplitContainer", "HSplitContainer", "VSplitContainer" })
        {
            _defaultTheme.SetConstant("separation", type, 12); _defaultTheme.SetConstant("minimum_grab_thickness", type, 6); _defaultTheme.SetConstant("autohide", type, 1);
            _defaultTheme.SetStyleBox("split_bar_background", type, empty);
        }
        _defaultTheme.SetColor("touch_dragger_color", "SplitContainer", new(1, 1, 1, .3f));
        _defaultTheme.SetColor("touch_dragger_hover_color", "SplitContainer", new(1, 1, 1, .6f));
        _defaultTheme.SetColor("touch_dragger_pressed_color", "SplitContainer", Colors.White);
        _defaultTheme.SetIcon("h_grabber", "SplitContainer", horizontal); _defaultTheme.SetIcon("v_grabber", "SplitContainer", vertical);
        _defaultTheme.SetIcon("h_touch_dragger", "SplitContainer", touchHorizontal); _defaultTheme.SetIcon("v_touch_dragger", "SplitContainer", touchVertical);
        _defaultTheme.SetIcon("grabber", "HSplitContainer", horizontal); _defaultTheme.SetIcon("grabber", "VSplitContainer", vertical);
        _defaultTheme.SetIcon("touch_dragger", "HSplitContainer", touchHorizontal); _defaultTheme.SetIcon("touch_dragger", "VSplitContainer", touchVertical);
    }
}
