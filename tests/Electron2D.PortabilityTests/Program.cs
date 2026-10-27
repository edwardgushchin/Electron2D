if (args is ["--low-processor"]) { LowProcessorContract.Run(); return; }
if (args is ["--server-monitors"]) { ServerMonitorContract.Run(); return; }
if (args is ["--server-monitors-audio"]) { ServerMonitorContract.RunAudio(); return; }
if (args.Length == 1 && args[0] == "--device-sensors") DeviceSensorContract.Run();
else if (args.Length == 1 && args[0] == "--joy-motion") JoyMotionContract.Run();
else if (args.Length == 1 && args[0] == "--frame-monitors") FrameMonitorContract.Run();
else if (args.Length == 3 && args[0] == "--deferred-scene") DeferredSceneContract.Run(args[1], args[2]);
else ContractChecks.Run();
