using SDL3;

namespace Electron2D;

public static class PlatformInfo
{
    /// <summary>
    /// Returns the amount of RAM configured in the system in MiB.
    /// </summary>
    public static int SystemRAM { get; private set; }
    
    /// <summary>
    /// Returns the total number of logical CPU cores. On CPUs that include technologies such as hyperthreading,
    /// the number of logical cores may be more than the number of physical cores.
    /// </summary>
    public static int CPUCores { get; private set; }
    
    /// <summary>
    /// Returns the L1 cache line size of the CPU, in bytes.
    /// </summary>
    public static int CacheLineSize { get; private set; }
    
    /// <summary>
    /// Returns the type of the platform. If the correct platform name is not available, returns a string beginning
    /// with the text <see cref="PlatformType.Unknown"/>.
    /// </summary>
    public static PlatformType Type { get; private set; }
    
    /// <summary>
    /// Returns true if the game is running on a Windows operating system.
    /// </summary>
    public static bool IsWindows => Type == PlatformType.Windows;

    /// <summary>
    /// Returns true if the game is running on a macOS operating system.
    /// </summary>
    public static bool IsMacOS => Type == PlatformType.MacOS;

    /// <summary>
    /// Returns true if the game is running on a Linux-based operating system.
    /// </summary>
    public static bool IsLinux => Type == PlatformType.Linux;

    /// <summary>
    /// Returns true if the game is running on an Android device.
    /// </summary>
    public static bool IsAndroid => Type == PlatformType.Android;

    /// <summary>
    /// Returns true if the game is running on an iOS device.
    /// </summary>
    public static bool IsIOS => Type == PlatformType.IOS;

    
    static PlatformInfo()
    {
        SystemRAM = SDL.GetSystemRAM();
        CPUCores = SDL.GetNumLogicalCPUCores();
        CacheLineSize = SDL.GetCPUCacheLineSize();

        Type = SDL.GetPlatform() switch
        {
            "Windows" => PlatformType.Windows,
            "macOS" => PlatformType.MacOS,
            "Linux" => PlatformType.Linux,
            "iOS" => PlatformType.IOS,
            "Android" => PlatformType.Android,
            _ => PlatformType.Unknown
        };
    }
}