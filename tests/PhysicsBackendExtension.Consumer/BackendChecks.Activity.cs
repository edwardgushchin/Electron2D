using Electron2D;

public static partial class BackendChecks
{
    public static void RunActivity(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        const string registered = "ActivityContract";
        if (backend is null) PhysicsServerManager.RegisterServer(registered, () => new Solver());
        try { PhysicsActivityTests.Run(backend?.ToString() ?? registered); }
        finally { PhysicsServer.SetActive(true); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
}
