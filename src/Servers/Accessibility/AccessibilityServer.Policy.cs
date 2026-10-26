namespace Electron2D;

public sealed partial class AccessibilityServer
{
    private AccessibilitySupport _support = AccessibilitySupport.Auto;
    private bool _automatic;
    internal static bool IsActive => Runtime._native?.Available == true && (Runtime._automatic || Runtime._elements.Count != 0);
    internal static void BeginDisplay()
    {
        var r = Runtime;
        r._support = ProjectSettings.GetWithOverride(ProjectSettings.AccessibilitySupport);
        r._automatic = r._support == AccessibilitySupport.Always || r._support == AccessibilitySupport.Auto && ATSPIServer.DesktopEnabled();
    }
    internal static void StartDisplayOutput() { if (AutomaticOutput) _ = GetWindowRoot(DisplayServer.MainWindowId); }
    internal static bool AutomaticOutput => Runtime._automatic && IsSupported();
}
