namespace Electron2D.Examples.BakedRoutes;

internal sealed class RouteScene : Entity
{
    internal NavigationRegion Region { get; }
    internal int Frames { get; private set; }
    internal int Completions { get; private set; }
    internal Vector2[] Route { get; private set; } = [];
    internal RouteScene(string file)
    {
        using var scene = ResourceLoader.Load<PackedScene>(file);
        Region = (NavigationRegion)scene.Instantiate(); AddChild(Region);
    }
    protected override void OnReady()
    {
        ProcessEnabled = true; Region.BakeFinished += () => Completions++;
        Region.BakeNavigationPolygon();
    }
    protected override void OnProcess(double delta)
    {
        Frames++;
        if (!Region.IsBaking() && Completions != 0 && Route.Length == 0) Route = NavigationServer.MapGetPath(Region.GetNavigationMap(), new(10, 32), new(54, 32), true);
        QueueRedraw();
    }
    protected override void OnDraw()
    {
        DrawRect(new(4, 4, 56, 56), new(.08f, .08f, .08f));
        if (Route.Length > 1) DrawPolyline(Route, Colors.Green, 2);
        DrawRect(new(4 + Frames % 56, 61, 2, 2), Colors.White);
    }
}
