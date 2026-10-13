using SDL3;
using IOPath = System.IO.Path;

namespace Electron2D;

public sealed partial class OS
{
    /// <summary>Identifies a platform-provided user directory.</summary>
    public enum SystemDir
    {
        /// <summary>Desktop files.</summary>
        Desktop = 0,
        /// <summary>Camera images; desktop Linux/Windows use Pictures.</summary>
        DCIM = 1,
        /// <summary>User documents.</summary>
        Documents = 2,
        /// <summary>Downloaded files.</summary>
        Downloads = 3,
        /// <summary>User videos.</summary>
        Movies = 4,
        /// <summary>User music.</summary>
        Music = 5,
        /// <summary>User pictures.</summary>
        Pictures = 6,
        /// <summary>Ringtones; desktop Linux/Windows use Music.</summary>
        Ringtones = 7
    }

    /// <summary>Gets the global user configuration directory without creating it.</summary>
    /// <returns>The platform directory; Linux honors absolute XDG_CONFIG_HOME, otherwise HOME/.config.</returns>
    /// <exception cref="PlatformNotSupportedException">The current host lacks a directory adapter.</exception>
    public static string GetConfigDir()
    {
        DesktopDirectories();
        if (OperatingSystem.IsWindows()) return RequireDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.DoNotVerify));
        if (OperatingSystem.IsMacOS()) return IOPath.Combine(HomeDirectory(), "Library", "Application Support");
        return XDGDirectory("XDG_CONFIG_HOME", ".config");
    }

    /// <summary>Gets the global user data directory without creating it.</summary>
    /// <returns>The platform directory; Linux honors absolute XDG_DATA_HOME, otherwise HOME/.local/share.</returns>
    /// <remarks>This is distinct from the current project's user-data root.</remarks>
    public static string GetDataDir()
    {
        DesktopDirectories();
        return OperatingSystem.IsLinux() ? XDGDirectory("XDG_DATA_HOME", ".local/share") : GetConfigDir();
    }

    /// <summary>Gets the global user cache directory without creating it.</summary>
    /// <returns>The platform directory; Linux honors absolute XDG_CACHE_HOME, otherwise HOME/.cache.</returns>
    public static string GetCacheDir()
    {
        DesktopDirectories();
        if (OperatingSystem.IsWindows()) return RequireDirectory(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify));
        if (OperatingSystem.IsMacOS()) return IOPath.Combine(HomeDirectory(), "Library", "Caches");
        return XDGDirectory("XDG_CACHE_HOME", ".cache");
    }

    /// <summary>Gets the global temporary directory without creating it.</summary>
    /// <returns>The Linux global /tmp path, or the current host's temporary-directory path.</returns>
    public static string GetTempDir() => OperatingSystem.IsLinux() && !OperatingSystem.IsAndroid() ? "/tmp" : IOPath.GetTempPath();

    /// <summary>Gets the current project's absolute user-data directory without creating it.</summary>
    /// <returns>The same root used by ProjectSettings.GlobalizePath for user://.</returns>
    public static string GetUserDataDir() => ProjectSettings.UserDataRoot;

    /// <summary>Gets a platform-provided directory for user-managed files without creating it.</summary>
    /// <param name="dir">The directory role.</param>
    /// <param name="sharedStorage">Selects Android shared storage; desktop hosts have no separate shared-storage selector.</param>
    /// <returns>The native directory path, which need not exist yet.</returns>
    /// <remarks>Desktop Linux/Windows map DCIM to Pictures and Ringtones to Music. macOS has no corresponding roles.
    /// Mobile shared/app directory adapters are currently unavailable. Native directory policy is owned by SDL.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The directory role is unknown.</exception>
    /// <exception cref="PlatformNotSupportedException">The host or directory role is unsupported.</exception>
    /// <exception cref="IOException">The native directory service fails.</exception>
    public static string GetSystemDir(SystemDir dir, bool sharedStorage = true)
    {
        if ((uint)dir > (uint)SystemDir.Ringtones) throw new ArgumentOutOfRangeException(nameof(dir));
        DesktopDirectories();
        if (OperatingSystem.IsMacOS() && dir is SystemDir.DCIM or SystemDir.Ringtones) throw new PlatformNotSupportedException("This desktop has no camera-image or ringtone directory role.");
        var folder = dir switch
        {
            SystemDir.Desktop => SDL.Folder.Desktop,
            SystemDir.DCIM or SystemDir.Pictures => SDL.Folder.Pictures,
            SystemDir.Documents => SDL.Folder.Documents,
            SystemDir.Downloads => SDL.Folder.Downloads,
            SystemDir.Movies => SDL.Folder.Videos,
            _ => SDL.Folder.Music
        };
        var path = SDL.GetUserFolder(folder);
        if (string.IsNullOrEmpty(path)) throw new IOException("Cannot obtain the user directory: " + SDL.GetError());
        return IOPath.TrimEndingDirectorySeparator(path);
    }

    private static void DesktopDirectories()
    {
        if (!(OperatingSystem.IsLinux() && !OperatingSystem.IsAndroid() || OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()))
            throw new PlatformNotSupportedException("Global and user-managed directories require a desktop directory adapter.");
    }

    private static string RequireDirectory(string path)
    {
        if (string.IsNullOrEmpty(path)) throw new IOException("The host directory service returned no directory.");
        return path;
    }

    private static string HomeDirectory()
    {
        var home = Environment.GetEnvironmentVariable("HOME");
        if (!string.IsNullOrEmpty(home) && IOPath.IsPathFullyQualified(home)) return home;
        home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify);
        if (string.IsNullOrEmpty(home)) throw new IOException("The host has no user home directory.");
        return home;
    }

    private static string XDGDirectory(string variable, string suffix)
    {
        var value = GetEnvironment(variable);
        return value.Length > 0 && IOPath.IsPathFullyQualified(value) ? value : IOPath.Combine(HomeDirectory(), suffix);
    }
}
