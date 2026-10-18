if (args.Length == 1 && args[0] == "--frame-monitors") FrameMonitorContract.Run();
else if (args.Length == 3 && args[0] == "--deferred-scene") DeferredSceneContract.Run(args[1], args[2]);
else ContractChecks.Run();
