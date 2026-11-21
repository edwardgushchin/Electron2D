if (args is ["unique-id"])
{
    if (OperatingSystem.IsLinux() && !OperatingSystem.IsAndroid())
    {
        var id = Electron2D.OS.GetUniqueID();
        if (File.ReadLines("/proc/self/maps").Any(line => line.Contains("libSDL", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("The installation ID query loaded SDL.");
        }

        Console.Write(id);
        return 0;
    }

    try
    {
        Electron2D.OS.GetUniqueID();
    }
    catch (PlatformNotSupportedException)
    {
        Console.WriteLine("The unsupported installation ID query reported PlatformNotSupportedException.");
        return 0;
    }

    throw new InvalidOperationException("The unsupported installation ID query succeeded.");
}

if (OperatingSystem.IsWindows())
{
    if (args is ["sandbox"])
    {
        WindowsSandboxChecks.Run();
        return 0;
    }

    if (args is ["sandbox-child"])
    {
        return WindowsSandboxChecks.Probe();
    }
}

HostPlatformChecks.Run(args);
return 0;
