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

        AndroidGameActivity? activity;
        MainLoop? loop;
        lock (Gate)
        {
            activity = _activity?.TryGetTarget(out var current) == true ? current : null;
            loop = activity is null ? null : GetPermissionLoop(activity);
        }
        if (activity is not null && loop is not null)
        {
            activity.QueuePermissionRequest(loop, name);
        }
        return false;
    }

    [SupportedOSPlatform("android23.0")]
    internal static string[] GetGrantedPermissions()
    {
        var context = Application.Context;
        var packages = context.PackageManager!;
        using var package = packages.GetPackageInfo(context.PackageName!, PackageInfoFlags.Permissions);
        return package?.RequestedPermissions?
            .Where(name => context.CheckSelfPermission(name) == Permission.Granted && IsDangerous(packages, name))
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
    // Reserve a request-code range separate from SDL's requests.
    private const int FirstPermissionCode = 0x45320000;
    private const int LastPermissionCode = 0x4532FFFF;
    private int _permissionCode = LastPermissionCode;
    private readonly Queue<(MainLoop Loop, string Name)> _permissionRequests = new();
    private readonly object _permissionGate = new();
    private (MainLoop Loop, string Name)? _activePermission;
    private bool _permissionActivityResumed;
    private bool _permissionActivityDestroyed;

    [SupportedOSPlatform("android23.0")]
    internal void QueuePermissionRequest(MainLoop loop, string name)
    {
        lock (_permissionGate)
        {
            var request = (loop, name);
            if (_permissionActivityDestroyed || _activePermission == request || _permissionRequests.Contains(request))
            {
                return;
            }
            _permissionRequests.Enqueue(request);
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
                if (CheckSelfPermission(request.Name) == Permission.Granted)
                {
                    request.Loop.PostPermissionResults([request.Name], [true]);
                    continue;
                }

                _permissionCode = _permissionCode == LastPermissionCode ? FirstPermissionCode : _permissionCode + 1;
                _activePermission = request;
                try
                {
                    RequestPermissions([request.Name], _permissionCode);
                }
                catch (Java.Lang.IllegalStateException)
                {
                    _activePermission = null;
                    request.Loop.PostPermissionResults([request.Name], [false]);
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
                var granted = permissions.Length == 1 && permissions[0] == request.Name &&
                    grants.Length == 1 && grants[0] == Permission.Granted;
                request.Loop.PostPermissionResults([request.Name], [granted]);
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
