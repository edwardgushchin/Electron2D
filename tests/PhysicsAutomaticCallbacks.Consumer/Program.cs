using Electron2D;

PhysicsAOTPrerequisiteTests.Run();

var backend = args.Length > 0 && args[0] == "gpu" ? PhysicsServer.Backend.GPU : PhysicsServer.Backend.CPU;
if (args.Contains("--local-scenes")) BackendChecks.RunLocalScenes(args.Length > 0 && args[0] == "registered" ? null : backend, args.Contains("threaded"));
else PhysicsAutomaticCallbackTests.Run(backend, args.Length > 1 && args[1] == "threaded");
