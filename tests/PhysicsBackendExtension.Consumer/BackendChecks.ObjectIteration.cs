using Electron2D;

public static partial class BackendChecks
{
    public static void RunObjectIteration(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        if (backend is null) PhysicsServerManager.RegisterServer("ObjectIteration", () => new Solver());
        ObjectIterationTests.Run(() => backend is { } selected ? new World(selected) : new World("ObjectIteration"), backend?.ToString() ?? "registered", threaded);
    }
}
