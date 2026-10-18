using Electron2D;

public static partial class BackendChecks
{
    public static void RunTopLevel(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        if (backend is null) PhysicsServerManager.RegisterServer("TopLevel", () => new Solver());
        CanvasTopLevelPhysicsTests.Run(() => backend is { } selected ? new World(selected) : new World("TopLevel"), backend?.ToString() ?? "registered", threaded);
    }
}
