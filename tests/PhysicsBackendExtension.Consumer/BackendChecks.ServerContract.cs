using Electron2D;

public static partial class BackendChecks
{
    public static void RunServerContract(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            if (backend is null) SelectionChecks();
            RunShapeResources(backend);
            RunShapeGeometry(backend, threaded);
            RunShapePolicy(backend, threaded);
            RunShapePairs(backend, threaded);
            RunBodyStates(backend, threaded);
            RunContactThreshold(backend, threaded);
            RunBodyParameters(backend, threaded);
            RunAxisVelocity(backend, threaded);
            RunForces(backend, threaded);
            RunMassReset(backend, threaded);
            RunExceptions(backend, threaded);
            RunAreaParameters(backend, threaded);
            RunAreaMonitors(backend, threaded);
            RunPicking(backend, threaded);
            RunJointParameters(backend, threaded);
            RunJointLifecycle(backend, threaded);
            RunWorldParameters(backend, threaded);
            RunLogicalQueries(backend, threaded);
            RunMotionExclusions(backend);
            RunMotionResultLifetime(backend, threaded);
            RunStatistics(backend, threaded);
            RunNotificationPhase(backend);
            SceneSpaceChecks(backend);
            RunSpaceCreation(backend, threaded);
            RunConstruction(backend);
            if (backend is null)
            {
                RunFactoryViews(threaded);
                RunDebugContacts(threaded);
                RunLifecycle();
            }
            Console.WriteLine($"Server contract {backend?.ToString() ?? "registered"}/{threaded}: shape/body/Area/joint/world/query/lifetime families passed.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
}
