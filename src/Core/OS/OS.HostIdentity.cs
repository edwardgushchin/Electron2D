using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Microsoft.Win32;

namespace Electron2D;

public sealed partial class OS
{
    private const string GenericDevice = "GenericDevice";

    private static string ReadHostVersionAlias()
    {
        if (OperatingSystem.IsWindows())
        {
            var version = Service._windowsVersion.Value;
            if (version.Major == 0) return string.Empty;
            var brand = WindowsReleaseName(version.Major, version.Build, version.ProductType == 1);
            return FormattableString.Invariant($"{brand} (build {version.Build})");
        }
        if (OperatingSystem.IsMacOS())
        {
            var version = Environment.OSVersion.Version;
            return $"{MacOSReleaseName(version.Major, version.Minor)} ({GetVersion()})";
        }
        if (OperatingSystem.IsAndroid())
        {
            var release = ReadAndroidProperty("ro.build.version.release_or_codename");
            if (release.Length == 0) release = ReadAndroidProperty("ro.build.version.release");
            var sdk = ReadAndroidProperty("ro.build.version.sdk");
            var build = ReadAndroidProperty("ro.build.version.incremental");
            return $"{release} (SDK {sdk} build {build})";
        }
        return GetVersion();
    }

    internal static string WindowsReleaseName(uint major, uint build, bool workstation)
    {
        if (major != 10) return "Unknown";
        if (workstation) return build >= 20348 ? "11" : "10";
        return build switch
        {
            >= 26100 => "Server 2025",
            >= 20348 => "Server 2022",
            >= 17763 => "Server 2019",
            _ => "Server 2016"
        };
    }

    internal static string MacOSReleaseName(int major, int minor) => (major, minor) switch
    {
        (26, _) => "Tahoe",
        (15, _) => "Sequoia",
        (14, _) => "Sonoma",
        (13, _) => "Ventura",
        (12, _) => "Monterey",
        (11, _) or (10, 16) => "Big Sur",
        (10, 15) => "Catalina",
        (10, 14) => "Mojave",
        (10, 13) => "High Sierra",
        _ => "Unknown"
    };

    private static string ReadDeviceModel()
    {
        var model = string.Empty;
        if (OperatingSystem.IsWindows()) model = ReadWindowsDeviceModel();
        else if (OperatingSystem.IsAndroid()) model = ReadAndroidProperty("ro.product.model");
        else if (OperatingSystem.IsMacOS()) model = ReadAppleDeviceModel("hw.model");
        else if (OperatingSystem.IsIOS() || OperatingSystem.IsTvOS()) model = ReadAppleDeviceModel("hw.machine");
        return model.Length == 0 ? GenericDevice : model;
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static string ReadWindowsDeviceModel()
    {
        try
        {
            using var bios = Registry.LocalMachine.OpenSubKey(@"HARDWARE\Description\System\BIOS");
            return SelectWindowsDeviceModel(bios?.GetValue("SystemProductName") as string, bios?.GetValue("BaseBoardProduct") as string);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or SecurityException)
        {
            return GenericDevice;
        }
    }

    internal static string SelectWindowsDeviceModel(string? product, string? board)
    {
        product = product?.Trim();
        if (!string.IsNullOrEmpty(product) && !product.Equals("System Product Name", StringComparison.OrdinalIgnoreCase))
            return product;
        board = board?.Trim();
        return !string.IsNullOrEmpty(board) && !board.Equals("Base Board Product", StringComparison.OrdinalIgnoreCase)
            ? board : GenericDevice;
    }

    private static unsafe string ReadAppleDeviceModel(string key)
    {
        var buffer = stackalloc byte[256];
        nuint size = 256;
        if (QueryAppleSystemValue(key, buffer, ref size, 0, 0) != 0 || size is 0 or > 256)
            return string.Empty;
        var bytes = new ReadOnlySpan<byte>(buffer, (int)size);
        var end = bytes.IndexOf((byte)0);
        return end < 0 ? string.Empty : Encoding.UTF8.GetString(bytes[..end]);
    }

    private static (string Name, string Version) ReadAndroidDistribution()
    {
        ReadOnlySpan<(string Key, string Name)> releases =
        [
            ("ro.havoc.version", "Havoc OS"),
            ("org.pex.version", "Pixel Extended"),
            ("org.pixelexperience.version", "Pixel Experience"),
            ("ro.potato.version", "POSP"),
            ("ro.xtended.version", "Project-Xtended"),
            ("org.evolution.version", "Evolution X"),
            ("ro.corvus.version", "Corvus-Q"),
            ("ro.pa.version", "Paranoid Android"),
            ("ro.crdroid.version", "crDroid Android"),
            ("ro.syberia.version", "Syberia Project"),
            ("ro.arrow.version", "ArrowOS"),
            ("ro.lineage.version", "LineageOS"),
            ("ro.modversion", "Android Custom ROM")
        ];
        foreach (var (key, name) in releases)
        {
            var version = ReadAndroidProperty(key);
            if (version.Length != 0) return (name, version);
        }
        var sdk = ReadAndroidProperty("ro.build.version.sdk");
        if (sdk.Length == 0) return ("Android", string.Empty);
        var build = ReadAndroidProperty("ro.build.version.incremental");
        return ("Android", build.Length == 0 ? sdk : $"{sdk}.{build}");
    }

    private static unsafe string ReadAndroidProperty(string key)
    {
        // Android PROP_VALUE_MAX includes the terminator.
        const int capacity = 92;
        var buffer = stackalloc byte[capacity];
        var length = QueryAndroidSystemProperty(key, buffer);
        return length is > 0 and < capacity ? Encoding.UTF8.GetString(new ReadOnlySpan<byte>(buffer, length)) : string.Empty;
    }

    private static unsafe WindowsVersion ReadWindowsVersion()
    {
        var version = new WindowsVersion { Size = (uint)sizeof(WindowsVersion) };
        return QueryWindowsVersion(ref version) == 0 ? version : default;
    }

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct WindowsVersion
    {
        internal uint Size, Major, Minor, Build, Platform;
        internal fixed ushort ServicePack[128];
        internal ushort ServicePackMajor, ServicePackMinor, SuiteMask;
        internal byte ProductType, Reserved;
    }

    [LibraryImport("ntdll.dll", EntryPoint = "RtlGetVersion")]
    private static partial int QueryWindowsVersion(ref WindowsVersion version);

    [LibraryImport("libc", EntryPoint = "__system_property_get", StringMarshalling = StringMarshalling.Custom,
        StringMarshallingCustomType = typeof(NativeUTF8StringMarshaller))]
    private static unsafe partial int QueryAndroidSystemProperty(string key, byte* value);
}
