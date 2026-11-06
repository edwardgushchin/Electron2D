if (args is ["disabled"]) { TTSChecks.Disabled(); return; }
if (args.Length != 2) throw new ArgumentException("Expected the native inspector directory and output directory.");
TTSChecks.Run(args[0], args[1]);
