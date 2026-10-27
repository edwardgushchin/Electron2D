if (args is ["--render-memory", var renderMode, "gpu"]) { RenderMemoryContract.Run(renderMode, Electron2D.PhysicsServer.Backend.GPU); return; }
if (args is ["--render-memory", var mode]) { if (mode == "managed") RenderMemoryContract.RunManaged(); else if (mode == "local") RenderMemoryContract.RunLocal(); else RenderMemoryContract.Run(mode, Electron2D.PhysicsServer.Backend.CPU); return; }
if (args is ["--canvas-pipelines", var renderer]) { if (renderer == "managed") CanvasPipelineContract.RunManaged(); else CanvasPipelineContract.Run(renderer); return; }
if (args is ["--deferred-memory"]) { DeferredMemoryContract.Run(); return; }
if (args is ["--low-processor"]) { LowProcessorContract.Run(); return; }
if (args is ["--server-monitors"]) { ServerMonitorContract.Run(); return; }
if (args is ["--server-monitors-audio"]) { ServerMonitorContract.RunAudio(); return; }
if (args.Length == 1 && args[0] == "--device-sensors") DeviceSensorContract.Run();
else if (args.Length == 1 && args[0] == "--joy-motion") JoyMotionContract.Run();
else if (args.Length == 1 && args[0] == "--frame-monitors") FrameMonitorContract.Run();
else if (args.Length == 3 && args[0] == "--deferred-scene") DeferredSceneContract.Run(args[1], args[2]);
else ContractChecks.Run();
