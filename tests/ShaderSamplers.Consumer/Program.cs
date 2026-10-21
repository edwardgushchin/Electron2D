try { ShaderSamplerChecks.Run(args); }
catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
