namespace Electron2D.Examples.BakedRoutes;

internal static class RouteAssets
{
    internal static void Write(string directory)
    {
        Directory.CreateDirectory(directory);
        using var polygon = new NavigationPolygon { AgentRadius = 2, ParsedGeometry = NavigationPolygon.ParsedGeometryType.StaticColliders };
        polygon.AddOutline([new(4, 4), new(60, 4), new(60, 60), new(4, 60)]);
        using var root = new NavigationRegion { Name = "BakedRoom", NavigationPolygon = polygon };
        var body = new StaticBody { Name = "WallBody", Position = new(32, 32) }; root.AddChild(body); body.Owner = root;
        using var shape = new RectangleShape { Size = new(12, 24) }; var collision = new CollisionShape { Name = "WallShape", Shape = shape }; body.AddChild(collision); collision.Owner = root;
        var wall = new Polygon { Name = "Wall", Vertices = [new(26, 20), new(38, 20), new(38, 44), new(26, 44)], Color = Colors.Red }; root.AddChild(wall); wall.Owner = root;
        using var scene = new PackedScene(); scene.Pack(root); ResourceSaver.Save(scene, System.IO.Path.Combine(directory, "room.e2dscene"));
    }
}
