using Electron2D;

internal static class PolygonTests
{
    internal static void Run()
    {
        using var polygon = new Polygon();
        Check(polygon is Entity && polygon.Vertices.Length == 0 && polygon.Color == Colors.White &&
            polygon.TextureScale == Vector2.One && polygon.InternalVertexCount == 0);
        var vertexProperty = polygon.GetPropertyList().Single(p => p.Name == nameof(Polygon.Vertices));
        Check(!vertexProperty.CanRevert(polygon), "Default empty arrays are already at their revert value.");
        Vector2[] square = [new(0, 0), new(20, 0), new(20, 20), new(0, 20)];
        polygon.Vertices = square; square[0] = new(100, 100);
        Check(vertexProperty.CanRevert(polygon), "Edited vertices can revert.");
        var snapshot = polygon.Vertices; snapshot[1] = new(100, 100);
        Check(polygon.Vertices[0] == Vector2.Zero && polygon.Vertices[1] == new Vector2(20, 0));
        polygon.Offset = new(3, 5); polygon.Color = Colors.Red;
        var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>();
        polygon.PrepareCanvas(); polygon.AppendCanvas(vertices, batches, Transform.Identity);
        Check(vertices.Count == 6 && batches.Count == 1 && vertices.All(v => v.Color == Colors.Red) &&
            vertices.Any(v => v.Position == new Vector2(3, 5)));

        polygon.VertexColors = [Colors.Red, Colors.Green, Colors.Blue, Colors.White];
        polygon.PrepareCanvas(); vertices.Clear(); batches.Clear(); polygon.AppendCanvas(vertices, batches, Transform.Identity);
        Check(vertices.Any(v => v.Color == Colors.Green) && vertices.Any(v => v.Color == Colors.Blue));
        polygon.InternalVertexCount = 1;
        polygon.PrepareCanvas(); vertices.Clear(); batches.Clear(); polygon.AppendCanvas(vertices, batches, Transform.Identity);
        Check(vertices.Count == 3 && vertices.All(v => v.Color == Colors.Red), "Incomplete colors fall back to uniform fill.");

        polygon.InternalVertexCount = 0;
        int[][] contours = [[0, 1, 2], [0, 2, 3]];
        polygon.Polygons = contours; contours[0][0] = 3;
        var copy = polygon.Polygons; copy[1][2] = 1;
        Check(polygon.Polygons[0][0] == 0 && polygon.Polygons[1][2] == 3);
        polygon.PrepareCanvas(); vertices.Clear(); batches.Clear(); polygon.AppendCanvas(vertices, batches, Transform.Identity);
        Check(vertices.Count == 6 && batches.Count > 0, "Indexed contours draw both triangles.");

        using var image = Image.CreateFromData(2, 2, false, Image.Format.Rgba8,
            [255, 0, 0, 255, 0, 255, 0, 255, 0, 0, 255, 255, 255, 255, 0, 255]);
        using var texture = ImageTexture.CreateFromImage(image);
        polygon.Texture = texture;
        polygon.UV = [Vector2.Zero, new(2, 0), new(2, 2), new(0, 2)];
        polygon.TextureScale = new(0.5f, 1f); polygon.TextureOffset = new(1, 0);
        polygon.PrepareCanvas(); vertices.Clear(); batches.Clear(); polygon.AppendCanvas(vertices, batches, Transform.Identity);
        Check(vertices.Any(v => v.UV == new Vector2(0.5f, 0)) && vertices.Any(v => v.UV == new Vector2(1, 1)) &&
            batches.All(b => ReferenceEquals(b.Texture, texture)), "Texture coordinates and borrowed texture enter retained batches.");

        using var scene = new PackedScene(); scene.Pack(polygon);
        var state = scene.GetState();
        var colorIndex = Enumerable.Range(0, state.GetNodePropertyCount(0)).Single(i => state.GetNodePropertyName(0, i) == nameof(Polygon.VertexColors));
        var contourIndex = Enumerable.Range(0, state.GetNodePropertyCount(0)).Single(i => state.GetNodePropertyName(0, i) == nameof(Polygon.Polygons));
        state.GetNodePropertyValue<Color[]>(0, colorIndex)[0] = Colors.Black;
        state.GetNodePropertyValue<int[][]>(0, contourIndex)[0][0] = 3;
        Check(state.GetNodePropertyValue<Color[]>(0, colorIndex)[0] == Colors.Red &&
            state.GetNodePropertyValue<int[][]>(0, contourIndex)[0][0] == 0, "Packed arrays are deeply isolated.");
        using var instance = (Polygon)scene.Instantiate();
        Check(instance.Vertices.SequenceEqual(polygon.Vertices) && instance.Polygons[0][0] == 0 &&
            instance.VertexColors[0] == Colors.Red && ReferenceEquals(instance.Texture, texture));

        polygon.Polygons = [[9, 1, 2]];
        Reject<ArgumentOutOfRangeException>(() => polygon.PrepareCanvas());
        Reject<ArgumentException>(() => polygon.Vertices = [new(float.NaN, 0)]);
        Reject<ArgumentException>(() => polygon.VertexColors = [new(float.NaN, 0, 0)]);
        Reject<ArgumentOutOfRangeException>(() => polygon.InternalVertexCount = -1);
        polygon.Dispose();
        Reject<ObjectDisposedException>(() => polygon.Color = Colors.Blue);
        Console.WriteLine("Polygon managed geometry, texture and packed state passed.");
    }

    private static void Check(bool value, string? message = null) { if (!value) throw new InvalidOperationException(message ?? "Polygon check failed."); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
