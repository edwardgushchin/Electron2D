using Electron2D;

if (args.Contains("--resource-identity-child"))
{
    BackendChecks.ReadResourceIdentity(args[Array.IndexOf(args, "--resource-identity-child") + 1], args[0] == "gpu" ? PhysicsServer.Backend.GPU : args[0] == "cpu" ? PhysicsServer.Backend.CPU : null, args.Contains("threaded"));
    return;
}
PhysicsAOTPrerequisiteTests.Run();

var backend = args.Length > 0 && args[0] == "gpu" ? PhysicsServer.Backend.GPU : PhysicsServer.Backend.CPU;
if (args.Contains("--node-scheduling")) BackendChecks.RunNodeScheduling(args.Length > 0 && args[0] == "registered" ? null : backend, args.Contains("threaded"));
else if (args.Contains("--resource-identity")) BackendChecks.RunResourceIdentity(args.Length > 0 && args[0] == "registered" ? null : backend, args.Contains("threaded"));
else if (args.Contains("--local-scenes")) BackendChecks.RunLocalScenes(args.Length > 0 && args[0] == "registered" ? null : backend, args.Contains("threaded"));
else PhysicsAutomaticCallbackTests.Run(backend, args.Length > 1 && args[1] == "threaded");
