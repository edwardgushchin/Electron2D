namespace Electron2D;

public sealed partial class ProjectSettings
{
    /// <summary>Defines the remappable graph duplication action.</summary><value>Command or Control plus D.</value>
    public static ProjectSetting<InputActionSettings> InputUIGraphDuplicate { get; } = CreateDefaultKeyAction("ui_graph_duplicate", Key.D, KeyModifierMask.CommandOrControl);
    /// <summary>Defines the remappable graph deletion action.</summary><value>Delete.</value>
    public static ProjectSetting<InputActionSettings> InputUIGraphDelete { get; } = CreateDefaultKeyAction("ui_graph_delete", Key.Delete);
    /// <summary>Defines following the selected input port's connection.</summary><value>Alt plus Left on macOS; Command or Control plus Left elsewhere.</value>
    public static ProjectSetting<InputActionSettings> InputUIGraphFollowLeft { get; } = CreateDefaultKeyAction("ui_graph_follow_left", Key.Left, OperatingSystem.IsMacOS() ? KeyModifierMask.Alt : KeyModifierMask.CommandOrControl);
    /// <summary>Defines following the selected output port's connection.</summary><value>Alt plus Right on macOS; Command or Control plus Right elsewhere.</value>
    public static ProjectSetting<InputActionSettings> InputUIGraphFollowRight { get; } = CreateDefaultKeyAction("ui_graph_follow_right", Key.Right, OperatingSystem.IsMacOS() ? KeyModifierMask.Alt : KeyModifierMask.CommandOrControl);
}
