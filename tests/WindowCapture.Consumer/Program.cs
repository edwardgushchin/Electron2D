if (args is ["unavailable"]) { WindowCaptureChecks.Unavailable(); return; }
if (args.Length != 2) throw new ArgumentException("Expected renderer and independent native capture probe.");
WindowCaptureChecks.Run(args[0], args[1]);
