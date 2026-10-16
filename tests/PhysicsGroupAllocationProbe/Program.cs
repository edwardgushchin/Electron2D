using Electron2D;

if (args.Length >= 2 && args[0] == "--node-groups-child")
    BackendChecks.ReadNodeGroups(args[1], args.Contains("--gpu") ? PhysicsServer.Backend.GPU : args.Contains("--cpu") ? PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Length == 1 && args[0] == "contention") PhysicsGroupAllocationTests.ContentionControl();
else if (args.Length == 1 && args[0] == "tls") PhysicsGroupAllocationTests.ThreadStaticControl();
else if (args.Length == 1) PhysicsGroupAllocationTests.Run(args[0]);
else throw new ArgumentException("Specify tls, contention, cpu-serial, gpu-serial, gpu-separate, registered-serial or registered-separate.");
