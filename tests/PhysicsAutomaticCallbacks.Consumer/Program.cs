using Electron2D;

var backend = args.Length > 0 && args[0] == "gpu" ? PhysicsServer.Backend.GPU : PhysicsServer.Backend.CPU;
PhysicsAutomaticCallbackTests.Run(backend, args.Length > 1 && args[1] == "threaded");
