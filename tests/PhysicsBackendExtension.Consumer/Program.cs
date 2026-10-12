if (args.Contains("--joint-lifecycle")) BackendChecks.RunJointLifecycle(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null);
else if (args.Contains("--joint-parameters")) BackendChecks.RunJointParameters(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null);
else if (args.Contains("--exceptions")) BackendChecks.RunExceptions(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null);
else if (args.Contains("--forces")) BackendChecks.RunForces(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null);
else if (args.Contains("--spaces")) BackendChecks.RunSpaces(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null);
else if (args.Contains("--draw")) CustomDrawChecks.Run();
else if (args.Contains("--integration")) BackendChecks.RunIntegration(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null);
else if (args.Contains("--server-modes")) BackendChecks.RunServerModes(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null);
else BackendChecks.Run();
