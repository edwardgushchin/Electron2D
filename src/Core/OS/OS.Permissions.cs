namespace Electron2D;

public sealed partial class OS
{
    /// <summary>Requests a single runtime permission from the active Android game Activity.</summary>
    /// <param name="name">The exact, case-sensitive permission name declared in the application manifest.</param>
    /// <returns>True if the declared permission is already granted. False otherwise, including while a request is pending.</returns>
    /// <remarks>Android API 23 or later can present a system prompt for a declared dangerous permission.
    /// Listen to <see cref="MainLoop.OnRequestPermissionsResult"/> before requesting it.
    /// Duplicate pending requests share a result. Invalid or undeclared names and unsupported hosts return false without a prompt.</remarks>
    public static bool RequestPermission(string name) => Service.RequestPermissionCore(name);

    /// <summary>Returns the application's currently granted dangerous permissions on Android API 23 or later.</summary>
    /// <returns>A snapshot of full permission names, or an empty array on unsupported hosts.</returns>
    /// <remarks>This query does not require an active window and never presents a prompt.</remarks>
    public static string[] GetGrantedPermissions() => Service.GetGrantedPermissionsCore();

    private bool RequestPermissionCore(string name)
    {
#if ANDROID
        if (OperatingSystem.IsAndroidVersionAtLeast(23) && !string.IsNullOrEmpty(name))
        {
            return AndroidWindowHost.RequestPermission(name);
        }
#endif
        return false;
    }

    private string[] GetGrantedPermissionsCore()
    {
#if ANDROID
        if (OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            return AndroidWindowHost.GetGrantedPermissions();
        }
#endif
        return [];
    }
}
