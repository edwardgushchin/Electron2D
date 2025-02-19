using System.Diagnostics;
using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

public static class PlatformInfo
{
    /// <summary>
    /// Returns the amount of RAM configured in the system in MiB.
    /// </summary>
    public static int SystemRAM { get; private set; }
    
    public static string CPUName { get; private set; }
    
    /// <summary>
    /// Returns the total number of logical CPU cores. On CPUs that include technologies such as hyperthreading,
    /// the number of logical cores may be more than the number of physical cores.
    /// </summary>
    public static int CPUCores { get; private set; }
    
    public static Architecture CPUArchitecture { get; private set; }
    
    /// <summary>
    /// Returns the L1 cache line size of the CPU, in bytes.
    /// </summary>
    public static int CacheLineSize { get; private set; }
    
    /// <summary>
    /// Returns the type of the platform. If the correct platform name is not available, returns a string beginning
    /// with the text <see cref="PlatformType.Unknown"/>.
    /// </summary>
    public static PlatformType OSType { get; }

    public static OperatingSystem OSVersion { get; private set; }
    
    public static string MachineName { get; private set; }
    
    public static string UserName { get; private set; }
    
    public static string GPUName { get; private set; }
    
    public static string GPUDriver { get; private set; }
    
    public static string CurrentDirectory { get; private set; }
    
    
    /// <summary>
    /// Returns true if the game is running on a Windows operating system.
    /// </summary>
    public static bool IsWindows => OSType == PlatformType.Windows;

    /// <summary>
    /// Returns true if the game is running on a macOS operating system.
    /// </summary>
    public static bool IsMacOS => OSType == PlatformType.MacOS;

    /// <summary>
    /// Returns true if the game is running on a Linux-based operating system.
    /// </summary>
    public static bool IsLinux => OSType == PlatformType.Linux;

    /// <summary>
    /// Returns true if the game is running on an Android device.
    /// </summary>
    public static bool IsAndroid => OSType == PlatformType.Android;

    /// <summary>
    /// Returns true if the game is running on an iOS device.
    /// </summary>
    public static bool IsIOS => OSType == PlatformType.IOS;

    
    static PlatformInfo()
    {
        SystemRAM = SDL.GetSystemRAM();
        CPUCores = SDL.GetNumLogicalCPUCores();
        CacheLineSize = SDL.GetCPUCacheLineSize();
        OSVersion = Environment.OSVersion;
        MachineName = Environment.MachineName;
        UserName = Environment.UserName;
        CurrentDirectory = Environment.CurrentDirectory;
        CPUArchitecture = RuntimeInformation.OSArchitecture;
        OSType = SDL.GetPlatform() switch
        {
            "Windows" => PlatformType.Windows,
            "macOS" => PlatformType.MacOS,
            "Linux" => PlatformType.Linux,
            "iOS" => PlatformType.IOS,
            "Android" => PlatformType.Android,
            _ => PlatformType.Unknown
        };
        CPUName = GetCPUName();
        GPUName = GetGPUName();
        GPUDriver = SDL.GetGPUDriver(0) ?? "Unknown";
    }

    private static string GetCPUName()
    {
        if (IsWindows)
            return RunCommand("wmic cpu get Name").Split("\n")[1].Trim();

        if (IsLinux)
            return RunCommand("lscpu | grep 'Model name'").Split(":")[1].Trim();

        if (IsMacOS)
            return RunCommand("sysctl -n machdep.cpu.brand_string").Trim();

        if (IsAndroid)
        {
            var variant1 = RunCommand("cat /proc/cpuinfo | grep 'Hardware'");

            return variant1 != string.Empty
                ? variant1.Split(":")[1].Trim()
                : RunCommand("getprop ro.product.cpu.abilist").Trim();
        }

        if (IsIOS)
            return RunCommand("sysctl -n machdep.cpu.brand_string").Trim();
        
        return "Unknown";
    }
    
    private static string GetGPUName()
    {
        if (IsWindows)
            return RunCommand("wmic path win32_videocontroller get caption").Split("\n")[1].Trim();

        if (IsLinux)
            return RunCommand("lspci | grep -i 'VGA'").Split(":")[2].Trim();

        if (IsMacOS)
            return RunCommand("system_profiler SPDisplaysDataType | grep 'Chipset Model'").Split(":")[1].Trim();

        if (IsAndroid)
        {
            var variant1 = RunCommand("getprop ro.hardware.egl"); // Часто содержит имя GPU
            var variant2 = RunCommand("dumpsys SurfaceFlinger | grep GLES"); // Может дать доп. информацию

            return !string.IsNullOrWhiteSpace(variant1) ? variant1.Trim() : variant2.Trim();
        }

        if (IsIOS)
            return "Apple GPU"; // На iOS нет команды для получения названия GPU, можно возвращать тип

        return "Unknown";
    }
    

    private static string RunCommand(string command)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                psi.FileName = "cmd.exe";
                psi.Arguments = $"/C {command}";
            }

            using var process = Process.Start(psi);
            using var reader = process?.StandardOutput;
            return reader?.ReadToEnd() ?? "";
        }
        catch
        {
            return "Unknown";
        }
    }
}