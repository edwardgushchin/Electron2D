namespace Electron2D.Examples.IterationRoutes;

internal static class IterationAssets
{
    internal static void Write(string directory)
    {
        Directory.CreateDirectory(directory); using var polygon = new NavigationPolygon(); polygon.SetVertices([new(4, 4), new(60, 4), new(60, 60), new(4, 60)]); polygon.AddPolygon([0, 1, 2, 3]);
        using var root = new NavigationRegion { Name = "Room", NavigationPolygon = polygon }; using var scene = new PackedScene(); scene.Pack(root); ResourceSaver.Save(scene, System.IO.Path.Combine(directory, "room.e2dscene"));
        using var ring = new NavigationPolygon(); var vertices = new Vector2[16]; float[] coordinates = [4, 20, 44, 60];
        for (var y = 0; y < 4; y++) for (var x = 0; x < 4; x++) vertices[y * 4 + x] = new(coordinates[x], coordinates[y]); ring.SetVertices(vertices);
        for (var y = 0; y < 3; y++) for (var x = 0; x < 3; x++) if (x != 1 || y != 1) { var i = y * 4 + x; ring.AddPolygon([i, i + 1, i + 5, i + 4]); }
        ResourceSaver.Save(ring, System.IO.Path.Combine(directory, "ring.e2dres"));
    }
}
