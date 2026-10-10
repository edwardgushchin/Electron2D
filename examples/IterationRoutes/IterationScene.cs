namespace Electron2D.Examples.IterationRoutes;

internal sealed class IterationScene : Entity
{
    internal NavigationRegion Region { get; }
    private readonly NavigationPolygon _ring;
    internal Vector2[] Route { get; private set; } = [];
    internal ulong InitialIteration { get; private set; }
    internal bool Updated { get; private set; }
    internal IterationScene(string directory) { using var scene = ResourceLoader.Load<PackedScene>(System.IO.Path.Combine(directory, "room.e2dscene")); Region = (NavigationRegion)scene.Instantiate(); AddChild(Region); _ring = ResourceLoader.Load<NavigationPolygon>(System.IO.Path.Combine(directory, "ring.e2dres")); }
    protected override void OnReady()
    {
        ProcessEnabled = true; NavigationServer.Synchronize(); var map = Region.GetNavigationMap(); InitialIteration = NavigationServer.MapGetIterationID(map); Route = NavigationServer.MapGetPath(map, new(8, 32), new(56, 32), true); Region.NavigationPolygon = _ring;
    }
    protected override void OnProcess(double delta)
    {
        if (!Updated && NavigationServer.MapGetIterationID(Region.GetNavigationMap()) > InitialIteration) { Updated = true; Route = NavigationServer.MapGetPath(Region.GetNavigationMap(), new(8, 32), new(56, 32), true); QueueRedraw(); }
    }
    protected override void OnDraw() { DrawRect(new(20, 20, 24, 24), Colors.Red); if (Route.Length > 1) DrawPolyline(Route, Colors.Green, 2); }
    protected override void Dispose(bool disposing) { try { base.Dispose(disposing); } finally { if (disposing) _ring.Dispose(); } }
}
