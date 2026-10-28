namespace Electron2D;

internal static class LinuxPortalThemeSupport
{
    internal static bool Query() => LinuxPortalDBus.QueryVersion("org.freedesktop.portal.Settings") >= 1;
}
