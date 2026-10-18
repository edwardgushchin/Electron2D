using Electron2D;

public static partial class BackendChecks
{
    public static void RunNodeTweens(PhysicsServer.Backend? backend, bool threaded)
    {
        if (backend is null) PhysicsServerManager.RegisterServer("NodeTweens", () => new Solver());
        PhysicsNodeTweenTests.Run(() => backend is { } selected ? new World(selected) : new World("NodeTweens"), backend?.ToString() ?? "registered", threaded);
    }
}
