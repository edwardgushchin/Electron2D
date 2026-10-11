using Electron2D;

namespace Electron2D.Examples.SceneTiles;

internal static class SceneTileAssets
{
    internal static void Write(string directory)
    {
        Directory.CreateDirectory(directory);
        using var shape = new RectangleShape { Size = new(10, 10) };
        using var polygon = new NavigationPolygon { AgentRadius = 0 }; Vector2[] points = [new(-6, -6), new(6, -6), new(6, 6), new(-6, 6)]; polygon.SetVertices(points); polygon.AddPolygon([0, 1, 2, 3]); polygon.AddOutline(points);
        using var actor = new Entity { Name = "Actor" };
        var image = new Polygon { Name = "Image", Vertices = points, Color = new(.2f, .8f, .4f) }; actor.AddChild(image); image.Owner = actor;
        var marker = new Polygon { Name = "Marker", Vertices = [new(1, -4), new(4, -4), new(4, -1), new(1, -1)], Color = Colors.White }; actor.AddChild(marker); marker.Owner = actor;
        using var visual = new PackedScene(); visual.Pack(actor);
        var body = new StaticBody { Name = "Collider" }; actor.AddChild(body); body.Owner = actor;
        var collision = new CollisionShape { Name = "Shape", Shape = shape }; body.AddChild(collision); collision.Owner = actor;
        var region = new NavigationRegion { Name = "Walkable", NavigationPolygon = polygon }; actor.AddChild(region); region.Owner = actor;
        using var prefab = new PackedScene(); prefab.Pack(actor);
        using var collection = new TileSetScenesCollectionSource(); collection.CreateSceneTile(visual, 1); collection.CreateSceneTile(prefab, 2);
        using var set = new TileSet { TileSize = new(32, 32) }; set.AddSource(collection, 0);
        using var layer = new TileMapLayer { Name = "SceneTiles", TileSet = set, Position = new(16, 16) };
        for (var i = 0; i < 8; i++) layer.SetCell(new(i, 0), 0, Vector2i.Zero, 1 | (i % 2 != 0 ? TileSetAtlasSource.TransformFlipH : 0) | ((i & 2) != 0 ? TileSetAtlasSource.TransformFlipV : 0) | ((i & 4) != 0 ? TileSetAtlasSource.TransformTranspose : 0));
        for (var i = 0; i < 8; i++) layer.SetCell(new(i, 1), 0, Vector2i.Zero, 2);
        using var packed = new PackedScene(); packed.Pack(layer); ResourceSaver.Save(packed, System.IO.Path.Combine(directory, "scenes.e2dscene"));
    }
}
