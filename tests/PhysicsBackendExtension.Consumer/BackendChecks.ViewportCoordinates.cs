using Electron2D;

public static partial class BackendChecks
{
    public static void RunViewportCoordinates(PhysicsServer.Backend? backend, bool threaded, bool native)
    {
        if (backend is null) PhysicsServerManager.RegisterServer("ViewportCoordinates", () => new Solver());
        CanvasViewportCoordinateTests.Run(() => backend is { } selected ? new World(selected) : new World("ViewportCoordinates"), backend?.ToString() ?? "registered", threaded, native);
    }
}
