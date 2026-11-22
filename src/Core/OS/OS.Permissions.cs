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

    /// <summary>Requests the application's missing dangerous permissions from the active Android game Activity.</summary>
    /// <returns>True if every declared dangerous permission is already granted, including when there are none.
    /// False while permissions are missing or on unsupported hosts.</returns>
    /// <remarks>Supported on Android API 23 or later. Subscribe to <see cref="MainLoop.OnRequestPermissionsResult"/>
    /// before calling. Pending requests for the same permission share one result with <see cref="RequestPermission"/>.
    /// A request requires an active game Activity. On Android 11 or later, apps targeting API 30 or later
    /// request background location separately, after other permissions in the batch. Android 13 or later
    /// uses the same ordering for background body sensors in apps targeting API 33 or later. On Android 12
    /// or later, the system request for precise location includes declared approximate location, including
    /// after an earlier denial. A companion permission added to the system call produces no extra result.
    /// Android decides which prompts to display.</remarks>
    public static bool RequestPermissions() => Service.RequestPermissionsCore();

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

    private bool RequestPermissionsCore()
    {
#if ANDROID
        if (OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            return AndroidWindowHost.RequestPermissions();
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
