try { DeviceResourceCacheChecks.Run(args.FirstOrDefault() ?? "gpu"); }
catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
