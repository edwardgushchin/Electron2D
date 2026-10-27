try { DeviceLimitChecks.Run(); }
catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
