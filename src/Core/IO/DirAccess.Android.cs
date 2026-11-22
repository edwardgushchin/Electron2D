#if ANDROID
using Android.App;
using Android.Content;
using Android.OS.Storage;
using AndroidEnvironment = Android.OS.Environment;

namespace Electron2D;

public sealed partial class DirAccess
{
    private static StorageVolume[] GetMountedAndroidVolumes()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(24))
        {
            throw new PlatformNotSupportedException("Storage-volume enumeration requires Android API 24 or later.");
        }

        var storage = Application.Context.GetSystemService(Context.StorageService) as StorageManager
            ?? throw new IOException("Android storage service is unavailable.");
        var mounted = new List<StorageVolume>();
        foreach (var volume in storage.StorageVolumes)
        {
            var state = volume.State;
            if (state == AndroidEnvironment.MediaMounted || state == AndroidEnvironment.MediaMountedReadOnly)
            {
                mounted.Add(volume);
            }
        }

        return mounted.ToArray();
    }

    private static DriveEntry[] GetAndroidDriveEntries()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            throw new PlatformNotSupportedException("Storage-volume root paths require Android API 30 or later.");
        }

        var volumes = GetMountedAndroidVolumes();
        var entries = new DriveEntry[volumes.Length];
        for (var index = 0; index < volumes.Length; index++)
        {
            using var directory = volumes[index].Directory
                ?? throw new IOException("A mounted Android volume no longer has a root path.");
            var path = directory.AbsolutePath;
            entries[index] = new DriveEntry(path, string.Empty, path);
        }

        return entries;
    }
}
#endif
