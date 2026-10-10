namespace Electron2D.Examples.TileRoutes;

internal sealed class TileRouteScene : Entity
{
    internal TileMapLayer Layer { get; }
    internal Vector2[] Route { get; private set; } = [];
    internal TileRouteScene(string file) { using var scene = ResourceLoader.Load<PackedScene>(file); Layer = (TileMapLayer)scene.Instantiate(); AddChild(Layer); }
    protected override void OnReady() { ProcessEnabled = true; }
    protected override void OnProcess(double delta)
    {
        if (Route.Length == 0 && NavigationServer.MapGetIterationID(Layer.GetNavigationMap()) != 0) { Route = NavigationServer.MapGetPath(Layer.GetNavigationMap(), new(8, 8), new(56, 56), true); QueueRedraw(); }
    }
    protected override void OnDraw() { if (Route.Length > 1) DrawPolyline(Route, Colors.Cyan, 2); }
}
