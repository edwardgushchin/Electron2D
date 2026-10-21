using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace Electron2D;

public sealed partial class OS
{
    private readonly Lazy<Dictionary<string, string>> _hostRelease = new(() => ParseHostRelease(ReadHostText("/etc/os-release", 65536)));
    private readonly Lazy<string> _processorName = new(ReadProcessorName);
    private readonly Lazy<string> _hostVersion = new(ReadHostVersion);

    /// <summary>Gets the platform family hosting this process.</summary>
    /// <returns>Windows, Linux, macOS, Android, iOS, tvOS, Web or the managed runtime's platform description.</returns>
    public static string GetName()
    {
        if (OperatingSystem.IsAndroid()) return "Android";
        if (OperatingSystem.IsIOS()) return "iOS";
        if (OperatingSystem.IsTvOS()) return "tvOS";
        if (OperatingSystem.IsBrowser()) return "Web";
        if (OperatingSystem.IsWindows()) return "Windows";
        if (OperatingSystem.IsMacOS()) return "macOS";
        if (OperatingSystem.IsLinux()) return "Linux";
        return RuntimeInformation.OSDescription;
    }
    /// <summary>Gets the operating-system distribution name.</summary>
    /// <returns>The Linux os-release NAME, or the platform family when a distribution name is unavailable.</returns>
    /// <remarks>Linux metadata is read once, outside a real-time interval. Missing optional files use the family name.</remarks>
    public static string GetDistributionName() => OperatingSystem.IsLinux() && !OperatingSystem.IsAndroid() && Service._hostRelease.Value.TryGetValue("NAME", out var name) && name.Length != 0 ? name : GetName();
    /// <summary>Gets the host operating-system version description.</summary>
    /// <returns>The Linux os-release VERSION or kernel build description; other profiles use the managed OS version description.</returns>
    /// <remarks>First use may read system metadata. This is independent of the engine and managed runtime versions.</remarks>
    /// <exception cref="IOException">The native kernel identity query fails.</exception>
    public static string GetVersion() => Service._hostVersion.Value;
    /// <summary>Gets the number of logical CPU cores reported by the platform.</summary>
    /// <returns>The native logical-core count, or zero when the platform cannot identify it.</returns>
    /// <remarks>This describes the host rather than a recommended worker count or a guarantee of process CPU capacity.</remarks>
    public static int GetProcessorCount() => SDL3.SDL.GetNumLogicalCPUCores();
    /// <summary>Gets the CPU model name when the host exposes one.</summary>
    /// <returns>The platform model string, or an empty string when unavailable.</returns>
    /// <remarks>First use may read CPU identification or system files. No graphics device is opened.</remarks>
    public static string GetProcessorName() => Service._processorName.Value;
    /// <summary>Gets the current operating-system locale identifier.</summary>
    /// <returns>On Linux/FreeBSD, LANG before its first encoding suffix, or en when absent; otherwise the native preferred language and optional country.</returns>
    /// <remarks>This cold environment query does not change TranslationServer.Culture or its catalog fallback.</remarks>
    public static string GetLocale()
    {
        if (OperatingSystem.IsLinux() && !OperatingSystem.IsAndroid() || OperatingSystem.IsFreeBSD())
        {
            lock (Service._environmentGate)
            {
                var value = Environment.GetEnvironmentVariable("LANG");
                if (value is null) return "en";
                var dot = value.IndexOf('.'); return dot < 0 ? value : value[..dot];
            }
        }
        lock (Service._environmentGate)
        {
            var locales = ReadNativeLocales(out var count);
            try
            {
                if (locales == 0 || count <= 0) return "en";
                var locale = Marshal.ReadIntPtr(locales); if (locale == 0) return "en";
                var language = Marshal.PtrToStringUTF8(Marshal.ReadIntPtr(locale)) ?? "en";
                var country = Marshal.PtrToStringUTF8(Marshal.ReadIntPtr(locale, IntPtr.Size));
                return string.IsNullOrEmpty(country) ? language : language + "_" + country;
            }
            finally { SDL3.SDL.Free(locales); }
        }
    }
    /// <summary>Gets the language portion of the current operating-system locale.</summary>
    /// <returns>The first language component, preserving C and other nonstandard system identifiers.</returns>
    public static string GetLocaleLanguage()
    {
        var locale = GetLocale(); var end = locale.AsSpan().IndexOfAny('_', '-', '@'); return end < 0 ? locale : locale[..end];
    }
    /// <summary>Reports whether this engine assembly was built with debug support.</summary>
    /// <returns>True for a Debug engine build; independent of a debugger attachment or the game build.</returns>
    public static bool IsDebugBuild()
    {
#if DEBUG
        return true;
#else
        return false;
#endif
    }
    /// <summary>Reports whether the current application runs in a platform sandbox.</summary>
    /// <returns>On Linux, whether Flatpak, the complete Snap environment or a host container-manager marker is present.</returns>
    /// <remarks>Environment checks are live. A generic container is not automatically an application sandbox.</remarks>
    /// <exception cref="PlatformNotSupportedException">The profile needs its native application-sandbox adapter.</exception>
    public static bool IsSandboxed()
    {
        if (!OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) throw new PlatformNotSupportedException("Application sandbox detection requires this platform's adapter.");
        if (System.IO.File.Exists("/.flatpak-info") || System.IO.File.Exists("/run/host/container-manager")) return true;
        lock (Service._environmentGate) return Environment.GetEnvironmentVariable("SNAP") is not null && Environment.GetEnvironmentVariable("SNAP_NAME") is not null && Environment.GetEnvironmentVariable("SNAP_REVISION") is not null;
    }

    [LibraryImport(NativeLibraries.SDLLibrary, EntryPoint = "SDL_GetPreferredLocales")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial nint ReadNativeLocales(out int count);

    private static string ReadHostText(string path, int limit)
    {
        try
        {
            using var stream = new FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var bytes = new byte[limit]; var count = stream.ReadAtLeast(bytes, limit, throwOnEndOfStream: false);
            return Encoding.UTF8.GetString(bytes.AsSpan(0, count));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return string.Empty; }
    }
    internal static Dictionary<string, string> ParseHostRelease(string text)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in text.Split('\n'))
        {
            var value = line.AsSpan().Trim(); if (value.IsEmpty || value[0] == '#') continue;
            var equals = value.IndexOf('='); if (equals <= 0) continue;
            var key = value[..equals].Trim(); value = value[(equals + 1)..].Trim();
            if (value.Length >= 2 && value[0] is '\'' or '"' && value[^1] == value[0]) value = value[1..^1];
            values[key.ToString()] = value.ToString();
        }
        return values;
    }
    private static string ReadHostVersion()
    {
        if (OperatingSystem.IsLinux() && !OperatingSystem.IsAndroid())
        {
            if (Service._hostRelease.Value.TryGetValue("VERSION", out var version) && version.Length != 0) return version;
            return ReadLinuxKernelVersion();
        }
        // shortcut: exact foreign product-version descriptions need their native adapters and platform execution.
        return Environment.OSVersion.VersionString;
    }
    [LibraryImport("libc", EntryPoint = "uname", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static unsafe partial int QueryKernelIdentity(byte* buffer);

    internal static unsafe string ReadLinuxKernelVersion()
    {
        if (!OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) throw new PlatformNotSupportedException("Linux kernel identity requires the Linux ABI.");
        // Linux utsname contains six 65-byte fields, including their terminators.
        var fields = stackalloc byte[6 * 65];
        if (QueryKernelIdentity(fields) != 0) throw new IOException("The kernel identity query failed.", new System.ComponentModel.Win32Exception(Marshal.GetLastPInvokeError()));
        var version = new ReadOnlySpan<byte>(fields + 3 * 65, 65); var end = version.IndexOf((byte)0);
        if (end < 0) throw new InvalidDataException("Kernel identity is not terminated.");
        return Encoding.UTF8.GetString(version[..end]);
    }

    private static string ReadProcessorName()
    {
        if (X86Base.IsSupported)
        {
            var last = unchecked((uint)X86Base.CpuId(unchecked((int)0x80000000), 0).Eax);
            if (last >= 0x80000004)
            {
                Span<int> words = stackalloc int[12];
                for (var index = 0; index < 3; index++)
                {
                    var row = X86Base.CpuId(unchecked((int)(0x80000002u + index)), 0);
                    words[index * 4] = row.Eax; words[index * 4 + 1] = row.Ebx; words[index * 4 + 2] = row.Ecx; words[index * 4 + 3] = row.Edx;
                }
                return Encoding.ASCII.GetString(MemoryMarshal.AsBytes(words)).Trim('\0', ' ');
            }
        }
        if (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid())
            foreach (var line in ReadHostText("/proc/cpuinfo", 65536).Split('\n'))
            {
                var split = line.IndexOf(':');
                if (split > 0 && (line.AsSpan(0, split).Trim().Equals("model name", StringComparison.OrdinalIgnoreCase) || line.AsSpan(0, split).Trim().Equals("Hardware", StringComparison.OrdinalIgnoreCase))) return line[(split + 1)..].Trim();
            }
        // shortcut: non-x86 Apple/Windows CPU model providers need their native platform adapters.
        return string.Empty;
    }
}
