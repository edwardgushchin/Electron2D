using Android.Content;
using Android.Content.PM;
using Android.Util;
using Electron2D;
using System.Runtime.Versioning;
using AndroidEnvironment = Android.OS.Environment;
using EngineFile = Electron2D.FileAccess;
using IOPath = System.IO.Path;

namespace Electron2DAndroidTests;

internal static class SystemDirectoryChecks
{
    internal static void Run(Context context, bool expectUnavailableStorage)
    {
        var roles = new (OS.SystemDir Role, string? AndroidType)[]
        {
            (OS.SystemDir.Desktop, null),
            (OS.SystemDir.DCIM, "DCIM"),
            (OS.SystemDir.Documents, "Documents"),
            (OS.SystemDir.Downloads, "Download"),
            (OS.SystemDir.Movies, "Movies"),
            (OS.SystemDir.Music, "Music"),
            (OS.SystemDir.Pictures, "Pictures"),
            (OS.SystemDir.Ringtones, "Ringtones"),
            ((OS.SystemDir)(-1), null),
            ((OS.SystemDir)8, null)
        };

        var unavailable = 0;
        foreach (var (role, androidType) in roles)
        {
#pragma warning disable CS0618, CA1422
            using var shared = androidType is null
                ? AndroidEnvironment.ExternalStorageDirectory
                : AndroidEnvironment.GetExternalStoragePublicDirectory(androidType);
#pragma warning restore CS0618, CA1422
            if (shared is null)
            {
                ExpectUnavailable(role, sharedStorage: true);
            }
            else if (OS.GetSystemDir(role) != shared.AbsolutePath ||
                OS.GetSystemDir(role, sharedStorage: true) != shared.AbsolutePath)
            {
                throw new InvalidOperationException($"Wrong shared Android path for {role}.");
            }

            using var app = context.GetExternalFilesDir(androidType);
            if (app is null)
            {
                ExpectUnavailable(role, sharedStorage: false);
                unavailable++;
            }
            else if (OS.GetSystemDir(role, sharedStorage: false) != app.AbsolutePath)
            {
                throw new InvalidOperationException($"Wrong app-specific Android path for {role}.");
            }
        }

        if (expectUnavailableStorage)
        {
            if (unavailable != roles.Length)
                throw new InvalidOperationException("The unavailable-storage run requires Android to return null for every app-specific role.");

            Log.Info("Electron2DTests", $"CHECK Android directory paths with storage state {AndroidEnvironment.ExternalStorageState}");
            return;
        }
        if (unavailable != 0 || AndroidEnvironment.ExternalStorageState != AndroidEnvironment.MediaMounted)
            throw new InvalidOperationException("The normal run requires writable primary external storage.");

        var appPictures = OS.GetSystemDir(OS.SystemDir.Pictures, sharedStorage: false);
        var name = $"electron2d-{Guid.NewGuid():N}.txt";
        var appFile = IOPath.Combine(appPictures, name);
        try
        {
            using (var file = EngineFile.Open(appFile, FileAccessModeFlags.Write))
            {
                file.WriteString("Android external files");
            }

            if (EngineFile.GetFileAsString(appFile) != "Android external files")
                throw new InvalidOperationException("FileAccess did not round-trip the app-specific file.");
        }
        finally
        {
            File.Delete(appFile);
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(30))
            CheckSharedWriteDenied(context, name);

        Log.Info("Electron2DTests", "CHECK Android directory roles and app-specific FileAccess");
    }

    [SupportedOSPlatform("android30.0")]
    private static void CheckSharedWriteDenied(Context context, string name)
    {
        if (context.CheckSelfPermission("android.permission.WRITE_EXTERNAL_STORAGE") == Permission.Granted ||
            AndroidEnvironment.IsExternalStorageManager)
            throw new InvalidOperationException("The test app unexpectedly holds shared storage permission.");

        var path = IOPath.Combine(OS.GetSystemDir(OS.SystemDir.Desktop), name);
        try
        {
            using var file = EngineFile.Open(path, FileAccessModeFlags.Write);
            throw new InvalidOperationException("Scoped storage allowed writing directly to the shared root.");
        }
        catch (UnauthorizedAccessException)
        {
            Log.Info("Electron2DTests", "CHECK Android scoped storage denied a shared root write");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static void ExpectUnavailable(OS.SystemDir role, bool sharedStorage)
    {
        try
        {
            OS.GetSystemDir(role, sharedStorage);
        }
        catch (IOException)
        {
            return;
        }

        throw new InvalidOperationException($"Missing Android path for {role} must raise an IOException.");
    }
}
