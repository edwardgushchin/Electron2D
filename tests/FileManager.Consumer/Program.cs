if (OperatingSystem.IsLinux() && args is ["--linux", var scenario, var directory, var file])
{
    LinuxFileManagerChecks.Run(scenario, directory, file);
    return;
}

if (args.Length != 2)
{
    throw new ArgumentException("Expected independent Shell observer and private output folder.");
}

FileManagerChecks.Run(args[0], args[1]);
