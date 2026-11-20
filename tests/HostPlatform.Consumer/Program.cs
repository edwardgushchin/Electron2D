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
