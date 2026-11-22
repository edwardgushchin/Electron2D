using Android.Content;
using Android.OS.Storage;
using Android.Util;
using Electron2D;
using System.Runtime.Versioning;
using IOPath = System.IO.Path;

namespace Electron2DAndroidTests;

internal static class DriveChecks
{
    internal static void Run(Context context, bool expectUnavailableStorage, bool expectExcludedVolume)
    {
        var sdk = (int)Android.OS.Build.VERSION.SdkInt;
        if (!OperatingSystem.IsAndroidVersionAtLeast(24))
        {
            Expect<PlatformNotSupportedException>(() => DirAccess.GetDriveCount());
            Log.Info("Electron2DTests", $"CHECK Android API {sdk}: drive count is unsupported");
            return;
        }

        CheckVolumes(context, sdk, expectUnavailableStorage, expectExcludedVolume);
    }

    [SupportedOSPlatform("android24.0")]
    private static void CheckVolumes(Context context, int sdk, bool expectUnavailableStorage, bool expectExcludedVolume)
    {
        var storage = (StorageManager)context.GetSystemService(Context.StorageService)!;
        var volumes = storage.StorageVolumes;
        var mounted = volumes.Where(volume => volume.State is "mounted" or "mounted_ro").ToArray();
        var excluded = volumes.Count - mounted.Length;
        if (DirAccess.GetDriveCount() != mounted.Length)
        {
            throw new InvalidOperationException("Drive count disagrees with Android storage states.");
        }

        if ((expectUnavailableStorage || expectExcludedVolume) && excluded == 0)
        {
            throw new InvalidOperationException("The filter check requires an unmounted system volume.");
        }

        if (mounted.Any(volume => volume.IsPrimary) == expectUnavailableStorage)
        {
            throw new InvalidOperationException("The primary volume did not have the expected mount state.");
        }

        for (var index = 0; index < mounted.Length; index++)
        {
            if (DirAccess.GetDriveLabel(index) != string.Empty)
            {
                throw new InvalidOperationException("Android drive labels must be empty.");
            }
        }
        Expect<ArgumentOutOfRangeException>(() => DirAccess.GetDriveLabel(mounted.Length));

        var internalPath = context.FilesDir!.AbsolutePath;
        using var internalDirectory = DirAccess.Open(internalPath);
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            CheckPaths(context, mounted, internalDirectory, expectUnavailableStorage);
        }
        else
        {
            Expect<PlatformNotSupportedException>(() => DirAccess.GetDriveName(0));
            Expect<PlatformNotSupportedException>(() => internalDirectory.GetCurrentDrive());
        }

        Log.Info("Electron2DTests", $"CHECK Android API {sdk}: {mounted.Length} mounted volumes, {excluded} excluded");
    }

    [SupportedOSPlatform("android30.0")]
    private static void CheckPaths(Context context, StorageVolume[] mounted, DirAccess internalDirectory,
        bool expectUnavailableStorage)
    {
        var roots = mounted.Select(volume =>
        {
            using var directory = volume.Directory ?? throw new InvalidOperationException("Missing system volume root.");
            return directory.AbsolutePath;
        }).ToArray();
        for (var index = 0; index < roots.Length; index++)
        {
            if (DirAccess.GetDriveName(index) != roots[index])
            {
                throw new InvalidOperationException($"Wrong volume root at index {index}.");
            }
        }

        Expect<ArgumentOutOfRangeException>(() => DirAccess.GetDriveName(-1));
        Expect<ArgumentOutOfRangeException>(() => DirAccess.GetDriveName(roots.Length));
        if (internalDirectory.GetCurrentDrive() != -1)
        {
            throw new InvalidOperationException("App-internal storage unexpectedly belongs to a shared volume.");
        }

        if (expectUnavailableStorage)
        {
            return;
        }

        using var appDirectory = context.GetExternalFilesDir(null)
            ?? throw new InvalidOperationException("The primary app-specific directory is unavailable.");
        var primaryIndex = Array.FindIndex(mounted, volume => volume.IsPrimary);
        using var externalDirectory = DirAccess.Open(appDirectory.AbsolutePath);
        if (externalDirectory.GetCurrentDrive() != primaryIndex)
        {
            throw new InvalidOperationException("The app-specific external path did not match the primary volume.");
        }

        var projectRoot = ProjectSettings.ProjectRoot;
        var userRoot = ProjectSettings.UserDataRoot;
        try
        {
            ProjectSettings.ConfigurePaths(appDirectory.AbsolutePath, internalDirectory.GetCurrentDir());
            using var resources = DirAccess.Open("res://");
            using var userData = DirAccess.Open("user://");
            if (resources.GetCurrentDir() != "res://" || resources.GetCurrentDrive() != primaryIndex ||
                userData.GetCurrentDir() != "user://" || userData.GetCurrentDrive() != -1)
            {
                throw new InvalidOperationException("Drive lookup changed or misidentified a virtual directory.");
            }

            Expect<UnauthorizedAccessException>(() => resources.ChangeDir(".."));
            Expect<UnauthorizedAccessException>(() => userData.ChangeDir(".."));
        }
        finally
        {
            ProjectSettings.ConfigurePaths(projectRoot, userRoot);
        }
    }

    private static void Expect<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }

        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
