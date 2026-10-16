if (args.Contains("--signal-blocking")) SignalBlockingPhysicsTests.Run(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : Electron2D.PhysicsServer.Backend.CPU, args.Contains("--threaded"));
else if (args.Contains("--contact-coupling")) BackendChecks.RunContactCoupling(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : Electron2D.PhysicsServer.Backend.CPU, args.Contains("--threaded"));
else if (args.Contains("--node-processing-child")) BackendChecks.ReadNodeProcessing(args[Array.IndexOf(args, "--node-processing-child") + 1], args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--node-membership-child")) BackendChecks.ReadNodeMembership(args[Array.IndexOf(args, "--node-membership-child") + 1], args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--node-processing")) BackendChecks.RunNodeProcessing(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--node-membership")) BackendChecks.RunNodeMembership(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--node-lifecycle-child")) BackendChecks.ReadNodeLifecycle(args[Array.IndexOf(args, "--node-lifecycle-child") + 1], args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--node-lifecycle")) BackendChecks.RunNodeLifecycle(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--node-groups-child")) BackendChecks.ReadNodeGroups(args[Array.IndexOf(args, "--node-groups-child") + 1], args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--node-groups")) BackendChecks.RunNodeGroups(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--process-modes")) BackendChecks.RunProcessModes(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--shared-free")) BackendChecks.RunSharedFree(args.Contains("--gpu"), args.Contains("--threaded"), args.Contains("--reverse"));
else if (args.Contains("--shared-data")) BackendChecks.RunSharedData(args.Contains("--gpu"), args.Contains("--threaded"), args.Contains("--reverse"));
else if (args.Contains("--procedural-data")) BackendChecks.RunProceduralData(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--filter-transactions")) BackendChecks.RunFilterTransactions(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--area-callback-transactions")) BackendChecks.RunAreaCallbackTransactions(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--shape-edits")) BackendChecks.RunShapeEdits(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--viewport-ownership-child")) BackendChecks.ReadViewportOwnership(args[Array.IndexOf(args, "--viewport-ownership-child") + 1],
    args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--viewport-ownership")) BackendChecks.RunViewportOwnership(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--exclusive-windows")) BackendChecks.RunExclusiveWindows(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--node-names-child")) BackendChecks.ReadNodeNames(args[Array.IndexOf(args, "--node-names-child") + 1]);
else if (args.Contains("--node-names")) BackendChecks.RunNodeNames(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--node-orphans")) BackendChecks.RunNodeOrphans(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--anchor-capacity")) BackendChecks.RunAnchorCapacity(args.Contains("--gpu"), args.Contains("--threaded"));
else if (args.Contains("--history-storage")) BackendChecks.RunHistoryStorage(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--point-containment")) PhysicsPointContainmentTests.Run(args.Contains("--gpu"));
else if (args.Contains("--transient-contacts")) PhysicsTransientContactTests.Run(args.Contains("--gpu"));
else if (args.Contains("--dense-contacts")) PhysicsDenseContactTests.Run(args.Contains("--gpu"));
else if (args.Contains("--node-paths")) BackendChecks.RunNodePaths(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--node-search")) BackendChecks.RunNodeSearch(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU :
    args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--process-groups-child")) BackendChecks.ReadGroupSettings(args[Array.IndexOf(args, "--process-groups-child") + 1]);
else if (args.Contains("--resource-path-ids-child")) BackendChecks.ReadResourcePathIDs(args[Array.IndexOf(args, "--resource-path-ids-child") + 1], args[0] == "gpu" ? Electron2D.PhysicsServer.Backend.GPU : args[0] == "cpu" ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("threaded"));
else if (args.Contains("--resource-path-ids")) BackendChecks.RunResourcePathIDs(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--resource-identity-child")) BackendChecks.ReadResourceIdentity(args[Array.IndexOf(args, "--resource-identity-child") + 1], args[0] == "gpu" ? Electron2D.PhysicsServer.Backend.GPU : args[0] == "cpu" ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("threaded"));
else if (args.Contains("--process-groups")) BackendChecks.RunProcessGroups(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--thread-messages")) BackendChecks.RunThreadMessages(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--node-scheduling")) BackendChecks.RunNodeScheduling(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--resource-identity")) BackendChecks.RunResourceIdentity(args[0] == "gpu" ? Electron2D.PhysicsServer.Backend.GPU : args[0] == "cpu" ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("threaded"));
else if (args.Contains("--local-scenes")) BackendChecks.RunLocalScenes(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--contact-policy")) BackendChecks.RunContactPolicy(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--priority-geometry")) BackendChecks.RunPriorityGeometry(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--directed-geometry") || args.Contains("--directed-angular")) BackendChecks.RunDirectedGeometry(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"), args.Contains("--directed-angular"));
else if (args.Contains("--boundary-geometry")) BackendChecks.RunBoundaryGeometry(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--concave-geometry")) BackendChecks.RunConcaveGeometry(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--convex-geometry")) BackendChecks.RunConvexGeometry(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--segment-geometry")) BackendChecks.RunSegmentGeometry(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--capsule-geometry")) BackendChecks.RunCapsuleGeometry(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--rectangle-geometry")) BackendChecks.RunRectangleGeometry(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--statistics")) BackendChecks.RunStatistics(args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : args.Contains("--cpu") ? Electron2D.PhysicsServer.Backend.CPU : null, args.Contains("--threaded"));
else if (args.Contains("--motion-roles")) BackendChecks.RunMotionRoles(args.Contains("--threaded"));
else if (args.Contains("--joint-contacts")) BackendChecks.RunJointContacts(args.Contains("--registered") ? null : args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : Electron2D.PhysicsServer.Backend.CPU, args.Contains("--threaded"));
else if (args.Contains("--coupled-joints")) BackendChecks.RunCoupledJoints(args.Contains("--registered") ? null : args.Contains("--gpu") ? Electron2D.PhysicsServer.Backend.GPU : Electron2D.PhysicsServer.Backend.CPU, args.Contains("--threaded"));
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
