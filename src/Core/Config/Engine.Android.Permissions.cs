#if ANDROID
using Android.App;
using Android.Content.PM;
using Android.OS;
using System.Runtime.Versioning;

namespace Electron2D;

internal static partial class AndroidWindowHost
{
    [SupportedOSPlatform("android23.0")]
    internal static bool RequestPermission(string name)
    {
        var context = Application.Context;
        var packages = context.PackageManager!;
        using var package = packages.GetPackageInfo(context.PackageName!, PackageInfoFlags.Permissions);
        if (package?.RequestedPermissions?.Contains(name, StringComparer.Ordinal) != true)
        {
            return false;
        }

        if (context.CheckSelfPermission(name) == Permission.Granted)
        {
            return true;
        }

        if (!IsDangerous(packages, name))
        {
            return false;
        }

        QueuePermissionRequest([name]);
        return false;
    }

    [SupportedOSPlatform("android23.0")]
    internal static bool RequestPermissions()
    {
        var missing = GetDangerousPermissions(Permission.Denied);
        if (missing.Length == 0)
        {
            return true;
        }

        QueuePermissionRequest(missing);
        return false;
    }

    [SupportedOSPlatform("android23.0")]
    private static void QueuePermissionRequest(string[] names)
    {
        AndroidGameActivity? activity;
        MainLoop? loop;
        lock (Gate)
        {
            activity = _activity?.TryGetTarget(out var current) == true ? current : null;
            loop = activity is null ? null : GetPermissionLoop(activity);
        }
        if (activity is not null && loop is not null)
        {
            activity.QueuePermissionRequest(loop, names);
        }
    }

    [SupportedOSPlatform("android23.0")]
    internal static string[] GetGrantedPermissions() => GetDangerousPermissions(Permission.Granted);

    [SupportedOSPlatform("android23.0")]
    private static string[] GetDangerousPermissions(Permission state)
    {
        var context = Application.Context;
        var packages = context.PackageManager!;
        using var package = packages.GetPackageInfo(context.PackageName!, PackageInfoFlags.Permissions);
        return package?.RequestedPermissions?
            .Where(name => context.CheckSelfPermission(name) == state && IsDangerous(packages, name))
            .Distinct(StringComparer.Ordinal)
            .ToArray() ?? [];
    }

    private static bool IsDangerous(PackageManager packages, string name)
    {
        try
        {
            using var permission = packages.GetPermissionInfo(name, 0);
            if (permission is null)
            {
                return false;
            }
            if (OperatingSystem.IsAndroidVersionAtLeast(28))
            {
                return permission.Protection == Protection.Dangerous;
            }

#pragma warning disable CS0618
            return (permission.ProtectionLevel & Protection.MaskBase) == Protection.Dangerous;
#pragma warning restore CS0618
        }
        catch (PackageManager.NameNotFoundException)
        {
            return false;
        }
    }

    internal static MainLoop? GetPermissionLoop(AndroidGameActivity activity)
    {
        lock (Gate)
        {
            var loop = _window?.Tree;
            return _started && !activity.IsFinishing && !activity.IsDestroyed &&
                _activity?.TryGetTarget(out var current) == true && ReferenceEquals(activity, current) &&
                ReferenceEquals(loop, Engine.Service.MainLoopCore) ? loop : null;
        }
    }
}

internal sealed partial class AndroidGameActivity
{
    [SupportedOSPlatform("android29.0")]
    private const string BackgroundLocation = Android.Manifest.Permission.AccessBackgroundLocation;
    [SupportedOSPlatform("android33.0")]
    private const string BackgroundBodySensors = Android.Manifest.Permission.BodySensorsBackground;

    // Reserve a request-code range separate from SDL's requests.
    private const int FirstPermissionCode = 0x45320000;
    private const int LastPermissionCode = 0x4532FFFF;
    private int _permissionCode = LastPermissionCode;
    private readonly Queue<(MainLoop Loop, string[] Names)> _permissionRequests = new();
    private readonly object _permissionGate = new();
    private (MainLoop Loop, string[] Names, bool[] Granted)? _activePermission;
    private bool _permissionActivityResumed;
    private bool _permissionActivityDestroyed;

    [SupportedOSPlatform("android23.0")]
    internal void QueuePermissionRequest(MainLoop loop, string[] names)
    {
        lock (_permissionGate)
        {
            if (_permissionActivityDestroyed)
            {
                return;
            }

            var pending = names.Where(name =>
                !(_activePermission is { } active && ReferenceEquals(active.Loop, loop) &&
                    active.Names.Contains(name, StringComparer.Ordinal)) &&
                !_permissionRequests.Any(request => ReferenceEquals(request.Loop, loop) &&
                    request.Names.Contains(name, StringComparer.Ordinal))).ToList();
            if (pending.Count == 0)
            {
                return;
            }

            var target = ApplicationInfo?.TargetSdkVersion;
            var background = new List<string>(2);
            // Android rejects foreground and background requests for these groups when submitted together.
            if (OperatingSystem.IsAndroidVersionAtLeast(30) && target >= BuildVersionCodes.R &&
                pending.Remove(BackgroundLocation))
            {
                background.Add(BackgroundLocation);
            }
            if (OperatingSystem.IsAndroidVersionAtLeast(33) && target >= BuildVersionCodes.Tiramisu &&
                pending.Remove(BackgroundBodySensors))
            {
                background.Add(BackgroundBodySensors);
            }
            if (pending.Count > 0)
            {
                _permissionRequests.Enqueue((loop, pending.ToArray()));
            }
            foreach (var name in background)
            {
                _permissionRequests.Enqueue((loop, [name]));
            }
        }
        SchedulePermissionRequest();
    }

    [SupportedOSPlatform("android23.0")]
    private void SchedulePermissionRequest()
    {
        // Always post, including from a result callback, so Activity can finish dispatching it.
        using var handler = new Handler(Looper.MainLooper!);
        handler.Post(StartPermissionRequest);
    }

    [SupportedOSPlatform("android23.0")]
    private void StartPermissionRequest()
    {
        var loop = AndroidWindowHost.GetPermissionLoop(this);
        lock (_permissionGate)
        {
            while (_permissionActivityResumed && _activePermission is null && _permissionRequests.TryDequeue(out var request))
            {
                if (!ReferenceEquals(loop, request.Loop))
                {
                    continue;
                }
                var granted = new bool[request.Names.Length];
                var missing = new List<string>(request.Names.Length);
                for (var i = 0; i < request.Names.Length; i++)
                {
                    var name = request.Names[i];
                    granted[i] = CheckSelfPermission(name) == Permission.Granted;
                    if (!granted[i])
                    {
                        missing.Add(name);
                    }
                }
                if (missing.Count == 0)
                {
                    request.Loop.PostPermissionResults(request.Names, granted);
                    continue;
                }

                // Android requires the declared location pair even after an earlier coarse denial.
                const string coarse = Android.Manifest.Permission.AccessCoarseLocation;
                const string fine = Android.Manifest.Permission.AccessFineLocation;
                if (OperatingSystem.IsAndroidVersionAtLeast(31) && missing.Contains(fine) &&
                    !missing.Contains(coarse))
                {
                    using var package = PackageManager!.GetPackageInfo(PackageName!, PackageInfoFlags.Permissions);
                    if (package?.RequestedPermissions?.Contains(coarse, StringComparer.Ordinal) == true)
                    {
                        // The companion is only for the system call. The original request owns its result.
                        missing.Add(coarse);
                    }
                }

                _permissionCode = _permissionCode == LastPermissionCode ? FirstPermissionCode : _permissionCode + 1;
                _activePermission = (request.Loop, request.Names, granted);
                try
                {
                    RequestPermissions(missing.ToArray(), _permissionCode);
                }
                catch (Java.Lang.IllegalStateException)
                {
                    _activePermission = null;
                    request.Loop.PostPermissionResults(request.Names, granted);
                }
            }
        }
    }

    [SupportedOSPlatform("android23.0")]
    private bool HandlePermissionResult(int code, string[] permissions, Permission[] grants)
    {
        if (code < FirstPermissionCode || code > LastPermissionCode)
        {
            return false;
        }

        var loop = AndroidWindowHost.GetPermissionLoop(this);
        lock (_permissionGate)
        {
            if (code != _permissionCode || _activePermission is not { } request)
            {
                return true;
            }

            _activePermission = null;
            if (ReferenceEquals(loop, request.Loop))
            {
                for (var i = 0; i < request.Names.Length; i++)
                {
                    var resultIndex = Array.IndexOf(permissions, request.Names[i]);
                    if (resultIndex >= 0 && resultIndex < grants.Length)
                    {
                        request.Granted[i] |= grants[resultIndex] == Permission.Granted;
                    }
                }
                request.Loop.PostPermissionResults(request.Names, request.Granted);
            }
        }
        SchedulePermissionRequest();
        return true;
    }

    protected override void OnResume()
    {
        base.OnResume();
        _permissionActivityResumed = true;
        if (OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            SchedulePermissionRequest();
        }
    }

    protected override void OnPause()
    {
        _permissionActivityResumed = false;
        base.OnPause();
    }

    private void ClosePermissionRequests()
    {
        lock (_permissionGate)
        {
            _permissionActivityDestroyed = true;
            _permissionRequests.Clear();
            _activePermission = null;
        }
    }
}
#endif
