if (args is ["unavailable"]) { NativeMenuChecks.Unavailable(); return; }
if (args.Length != 2) throw new ArgumentException("Expected independent native oracle and output folder.");
NativeMenuChecks.Run(args[0], args[1]);
