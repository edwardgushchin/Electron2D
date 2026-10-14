namespace Electron2D;

public sealed partial class ThemeDB
{
    private void AddVirtualJoystickDefaults()
    {
        var baseStyle = new StyleBoxFlat { BGColor = new(.175f, .175f, .175f), DrawCenter = false, BorderColor = new(.875f, .875f, .875f), CornerDetail = 20 };
        baseStyle.SetContentMarginAll(0); baseStyle.SetCornerRadiusAll(10000); baseStyle.SetBorderWidthAll(4);
        var tip = new StyleBoxFlat { BGColor = new(.175f, .175f, .175f), CornerDetail = 20 }; tip.SetContentMarginAll(0); tip.SetCornerRadiusAll(10000);
        _owned.Add(baseStyle); _owned.Add(tip);
        foreach (var name in new[] { "normal_joystick", "pressed_joystick" }) _defaultTheme.SetStyleBox(name, "VirtualJoystick", baseStyle);
        foreach (var name in new[] { "normal_tip", "pressed_tip" }) _defaultTheme.SetStyleBox(name, "VirtualJoystick", tip);
    }
}
