if (args.Contains("--statistics")) BackendChecks.RunStatistics(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--joint-contacts")) BackendChecks.RunJointContacts(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : Electron2D.PhysicsServer.Backend.CPU, args.Contains("--threaded"));
else if (args.Contains("--coupled-joints")) BackendChecks.RunCoupledJoints(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : Electron2D.PhysicsServer.Backend.CPU, args.Contains("--threaded"));
else if (args.Contains("--motion-result-lifetime")) BackendChecks.RunMotionResultLifetime(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--pair-lifetime")) BackendChecks.RunPairLifetime(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--separate-thread")) BackendChecks.RunSeparateThread();
else if (args.Contains("--world-copies")) BackendChecks.RunWorldCopies(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--portable-roles")) BackendChecks.RunPortableRoles(args.Contains("--threaded"), args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null);
else if (args.Contains("--portable-contacts")) BackendChecks.RunPortableContacts(args.Contains("--threaded"), args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null);
else if (args.Contains("--portable-capture")) BackendChecks.RunPortableCapture(args.Contains("--threaded"), args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null);
else if (args.Contains("--portable-geometry")) BackendChecks.RunPortableGeometry(args.Contains("--threaded"), args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null);
else if (args.Contains("--portable-motion")) BackendChecks.RunPortableMotion(args.Contains("--threaded"), args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null);
else if (args.Contains("--spring-anchors")) BackendChecks.RunSpringAnchors(args.Contains("--threaded"), args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null);
else if (args.Contains("--portable-joints")) BackendChecks.RunPortableJoints(args.Contains("--threaded"), args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null);
else if (args.Contains("--mass-roles")) BackendChecks.RunMassRoles(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--curved-annulus")) BackendChecks.RunCurvedAnnulus(args.Contains("--threaded"));
else if (args.Contains("--angular-geometry")) BackendChecks.RunAngularGeometry(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : Electron2D.PhysicsServer.Backend.CPU, args.Contains("--threaded"));
else if (args.Contains("--angular-compound")) BackendChecks.RunAngularCompound(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--compound-native")) BackendChecks.RunCompoundNative(args.Contains("--gpu-renderer") ? "gpu" : "compatibility");
else if (args.Contains("--compound")) BackendChecks.RunCompound(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null);
else if (args.Contains("--project-selection")) BackendChecks.RunProjectSelection();
else if (args.Contains("--allocation-warmup")) BackendChecks.RunAllocationWarmup();
else if (args.Contains("--contact-frames")) BackendChecks.RunContactFrames(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null);
else if (args.Contains("--continuous-response")) BackendChecks.RunContinuousResponse(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null);
else if (args.Contains("--contact-response")) BackendChecks.RunContactResponse(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded") ? true : null);
else if (args.Contains("--sleep")) BackendChecks.RunSleep(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null);
else if (args.Contains("--history")) BackendChecks.RunHistory(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null);
else if (args.Contains("--joint-response")) BackendChecks.RunJointResponse(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null);
else if (args.Contains("--shape-owners")) BackendChecks.RunShapeOwners(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null);
else if (args.Contains("--picking")) BackendChecks.RunPicking(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null);
else if (args.Contains("--construction")) BackendChecks.RunConstruction(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null);
else if (args.Contains("--shape-resources")) BackendChecks.RunShapeResources(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null);
else if (args.Contains("--shape-geometry")) BackendChecks.RunShapeGeometry(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--shape-policy")) BackendChecks.RunShapePolicy(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null);
else if (args.Contains("--custom")) BackendChecks.RunCustom();
else if (args.Contains("--factory-views")) BackendChecks.RunFactoryViews();
else if (args.Contains("--debug-drawing")) BackendChecks.RunDebugDrawing();
else if (args.Contains("--debug-contacts")) BackendChecks.RunDebugContacts();
else if (args.Contains("--lifecycle")) BackendChecks.RunLifecycle();
else if (args.Contains("--parameter-reads")) BackendChecks.RunParameterReads();
else if (args.Contains("--shape-pairs")) BackendChecks.RunShapePairs(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null);
else if (args.Contains("--notification-phase")) BackendChecks.RunNotificationPhase(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null);
else if (args.Contains("--motion-exclusions")) BackendChecks.RunMotionExclusions(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null);
else if (args.Contains("--contact-threshold")) BackendChecks.RunContactThreshold(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null);
else if (args.Contains("--area-monitors")) BackendChecks.RunAreaMonitors(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null);
else if (args.Contains("--resources")) BackendChecks.RunResources(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null);
else if (args.Contains("--joint-lifecycle")) BackendChecks.RunJointLifecycle(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
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
