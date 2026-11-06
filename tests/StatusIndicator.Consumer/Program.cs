if (args is ["unavailable"]) { StatusIndicatorChecks.Unavailable(); return; }
if (args.Length != 2) throw new ArgumentException("Expected independent native oracle and output folder.");
StatusIndicatorChecks.Run(args[0], args[1]);
