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
        Console.WriteLine("Host metadata parser exact-key, quoting and malformed-line boundaries passed.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
