using Electron2D;

if (args.Contains("--process-groups-child")) { BackendChecks.ReadGroupSettings(args[Array.IndexOf(args, "--process-groups-child") + 1]); return; }
if (args.Contains("--resource-identity-child"))
{
    BackendChecks.ReadResourceIdentity(args[Array.IndexOf(args, "--resource-identity-child") + 1], args[0] == "gpu" ? PhysicsServer.Backend.GPU : args[0] == "cpu" ? PhysicsServer.Backend.CPU : null, args.Contains("threaded"));
    return;
}
PhysicsAOTPrerequisiteTests.Run();

var backend = args.Length > 0 && args[0] == "gpu" ? PhysicsServer.Backend.GPU : PhysicsServer.Backend.CPU;
if (args.Contains("--process-groups")) BackendChecks.RunProcessGroups(args.Length > 0 && args[0] == "registered" ? null : backend, args.Contains("threaded"));
else if (args.Contains("--portable-contacts")) BackendChecks.RunPortableContacts(args.Contains("threaded"), args.Length > 0 && args[0] == "registered" ? null : backend);
else if (args.Contains("--portable-capture")) BackendChecks.RunPortableCapture(args.Contains("threaded"), args.Length > 0 && args[0] == "registered" ? null : backend);
else if (args.Contains("--portable-geometry")) BackendChecks.RunPortableGeometry(args.Contains("threaded"), args.Length > 0 && args[0] == "registered" ? null : backend);
else if (args.Contains("--portable-motion")) BackendChecks.RunPortableMotion(args.Contains("threaded"), args.Length > 0 && args[0] == "registered" ? null : backend);
else if (args.Contains("--portable-joints")) BackendChecks.RunPortableJoints(args.Contains("threaded"), args.Length > 0 && args[0] == "registered" ? null : backend);
else if (args.Contains("--spring-anchors")) BackendChecks.RunSpringAnchors(args.Contains("threaded"), args.Length > 0 && args[0] == "registered" ? null : backend);
else if (args.Contains("--sleep")) BackendChecks.RunSleep(args.Length > 0 && args[0] == "registered" ? null : backend);
else if (args.Contains("--motion-roles")) BackendChecks.RunMotionRoles(args.Contains("threaded"));
else if (args.Contains("--joint-contacts")) BackendChecks.RunJointContacts(args.Length > 0 && args[0] == "registered" ? null : backend, args.Contains("threaded"));
else if (args.Contains("--coupled-joints")) BackendChecks.RunCoupledJoints(args.Length > 0 && args[0] == "registered" ? null : backend, args.Contains("threaded"));
else if (args.Contains("--thread-messages")) BackendChecks.RunThreadMessages(args.Length > 0 && args[0] == "registered" ? null : backend, args.Contains("threaded"));
else if (args.Contains("--node-scheduling")) BackendChecks.RunNodeScheduling(args.Length > 0 && args[0] == "registered" ? null : backend, args.Contains("threaded"));
else if (args.Contains("--resource-identity")) BackendChecks.RunResourceIdentity(args.Length > 0 && args[0] == "registered" ? null : backend, args.Contains("threaded"));
else if (args.Contains("--local-scenes")) BackendChecks.RunLocalScenes(args.Length > 0 && args[0] == "registered" ? null : backend, args.Contains("threaded"));
else PhysicsAutomaticCallbackTests.Run(backend, args.Length > 1 && args[1] == "threaded");
