using Electron2D;

internal static class HostPlatformTests
{
    internal static void Run()
    {
        var fields = OS.ParseHostRelease("#header\r\nPRETTY_NAME=Wrong\nNAME=\"Exact distro\"\r\nVERSION_ID=1\nVERSION='Release = stable'\nOTHER=ignored\nBROKEN\n=invalid\n");
        Check(fields["NAME"] == "Exact distro" && fields["VERSION"] == "Release = stable", "Metadata selects exact keys and preserves equals inside quoted values.");
        Check(!fields.ContainsKey("") && !fields.ContainsKey("BROKEN"), "Malformed lines do not become metadata.");
        Check(OS.ParseHostRelease("NAME=old\nNAME=\"new\"\n")["NAME"] == "new", "Later complete definitions replace earlier ones.");
        Check(OS.ParseHostRelease("NAME=\n")["NAME"] == "", "Empty metadata retains the native fallback trigger.");
        if (OperatingSystem.IsLinux() && !OperatingSystem.IsAndroid()) Check(OS.ReadLinuxKernelVersion() == File.ReadAllText("/proc/sys/kernel/version").TrimEnd('\n'), "Native kernel version agrees with the independent proc view.");
        foreach (var (build, workstation, name) in new (uint, bool, string)[]
        {
            (19045, true, "10"), (22000, true, "11"), (26100, true, "11"),
            (14393, false, "Server 2016"), (17763, false, "Server 2019"),
            (20348, false, "Server 2022"), (26099, false, "Server 2022"), (26100, false, "Server 2025")
        })
            Check(OS.WindowsReleaseName(10, build, workstation) == name, "Windows client and server release boundaries.");
        Check(OS.WindowsReleaseName(6, 9600, true) == "Unknown", "Unrecognized Windows major version.");
        Check(OS.MacOSReleaseName(10, 16) == "Big Sur" && OS.MacOSReleaseName(11, 0) == "Big Sur" &&
            OS.MacOSReleaseName(15, 0) == "Sequoia" && OS.MacOSReleaseName(26, 0) == "Tahoe" &&
            OS.MacOSReleaseName(27, 0) == "Unknown", "Apple release branding includes legacy and unknown versions.");
        Check(OS.SelectWindowsDeviceModel("  Model  ", "Board") == "Model" &&
            OS.SelectWindowsDeviceModel("system PRODUCT name", "  Board ") == "Board" &&
            OS.SelectWindowsDeviceModel(null, "Base board product") == "GenericDevice",
            "Firmware placeholders fall through in product, board, generic order.");
        Console.WriteLine("Host metadata parser exact-key, quoting and malformed-line boundaries passed.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
