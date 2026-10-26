using Electron2D;

NotificationChecks.Run(args.Contains("--gpu") ? PhysicsServer.Backend.GPU : PhysicsServer.Backend.CPU, args.Contains("--threaded"));
