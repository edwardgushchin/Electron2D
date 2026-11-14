using Electron2D;

if (args.Length != 1 || args[0] is not ("managed" or "gpu" or "compatibility"))
    throw new ArgumentException("Usage: MeshTopology.Consumer managed|gpu|compatibility");

MeshPrimitiveType[] topologies =
[
    MeshPrimitiveType.Points, MeshPrimitiveType.Lines, MeshPrimitiveType.LineStrip,
    MeshPrimitiveType.Triangles, MeshPrimitiveType.TriangleStrip
];
Check(Enum.GetValues<MeshPrimitiveType>().Select(value => (int)value).SequenceEqual(Enumerable.Range(0, 6)) &&
    topologies.Select(value => (int)value).SequenceEqual(Enumerable.Range(0, 5)) &&
    (int)MeshPrimitiveType.Max == 5, "Pinned primitive identities.");

var fixtures = System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures");
using var legacy = ResourceLoader.Load<ArrayMesh>(System.IO.Path.Combine(fixtures, "legacy-array.e2dres"), ResourceLoader.CacheMode.Ignore);
using var importer = ResourceLoader.Load<ImporterMesh>(System.IO.Path.Combine(fixtures, "legacy-importer.e2dres"), ResourceLoader.CacheMode.Ignore);
using var imported = importer.GetMesh()!;
using var copied = (ArrayMesh)legacy.Duplicate();
Check(legacy.GetSurfaceCount() == 5 && importer.GetSurfaceCount() == 5 && imported.GetSurfaceCount() == 5 &&
    copied.GetSurfaceCount() == 5, "Both prior archives contain every topology.");
using var authored = new ArrayMesh();
using var immediate = new ImmediateMesh();
using var tool = new SurfaceTool();
foreach (var topology in topologies)
{
    var slot = (int)topology;
    var expected = Geometry(topology);
    Check(importer.GetSurfacePrimitiveType(slot) == topology &&
        importer.GetSurfaceArrays(slot).Vertices.SequenceEqual(expected.Vertices) &&
        importer.GetSurfaceArrays(slot).Indices.SequenceEqual(expected.Indices), $"Legacy importer {topology}.");
    foreach (var mesh in new[] { legacy, copied, imported })
        Check(mesh.SurfaceGetPrimitiveType(slot) == topology &&
            mesh.SurfaceGetArrays(slot).Vertices.SequenceEqual(expected.Vertices) &&
            mesh.SurfaceGetArrays(slot).Indices.SequenceEqual(expected.Indices), $"Restored/copied {topology}.");

    authored.AddSurfaceFromArrays(topology, expected);
    expected.Vertices[0] = new(-100, -100);
    Check(authored.SurfaceGetArrays(slot).Vertices[0] == new Vector2(0, 0), "Resource owns copied vertices.");
}

tool.Begin(MeshPrimitiveType.Points);
tool.AddVertex(new(3, 4));
var resourceChanges = 0;
var importerChanges = 0;
authored.Changed += _ => resourceChanges++;
importer.Changed += _ => importerChanges++;
foreach (var invalid in new[] { (MeshPrimitiveType)(-1), MeshPrimitiveType.Max, (MeshPrimitiveType)6 })
{
    Reject(() => authored.AddSurfaceFromArrays(invalid, Geometry(MeshPrimitiveType.Points)));
    Reject(() => importer.AddSurface(invalid, Geometry(MeshPrimitiveType.Points)));
    Reject(() => tool.Begin(invalid));
    Reject(() => immediate.SurfaceBegin(invalid));
}
using var committed = tool.Commit();
immediate.SurfaceBegin(MeshPrimitiveType.Points);
immediate.SurfaceAddVertex2D(new(3, 4));
immediate.SurfaceEnd();
Check(authored.GetSurfaceCount() == 5 && importer.GetSurfaceCount() == 5 && resourceChanges == 0 && importerChanges == 0 &&
    committed.SurfaceGetPrimitiveType(0) == MeshPrimitiveType.Points &&
    committed.SurfaceGetArrays(0).Vertices.SequenceEqual(new Vector2[] { new(3, 4) }) &&
    immediate.SurfaceGetPrimitiveType(0) == MeshPrimitiveType.Points &&
    immediate.SurfaceGetArrays(0).Vertices.SequenceEqual(new Vector2[] { new(3, 4) }), "Invalid topology leaves resources and drafts intact.");
Console.WriteLine("Managed: six identities, five archive/copy topologies, three invalid values rejected without mutation.");
if (args[0] == "managed") return;

var method = args[0];
ProjectSettings.Set(ProjectSettings.RenderingMethod, method);
var root = new Window { Size = new(150, 104) };
var rendered = false;
root.Ready += _ =>
{
    Check(RenderingServer.GetCurrentRenderingMethod() == method, "Requested renderer did not fall back.");
    Console.WriteLine($"Renderer: {method}/{RenderingServer.GetCurrentRenderingDriverName()}, {RenderingServer.GetVideoAdapterName()}");
    RenderingServer.SetDefaultClearColor(Colors.Black);
    foreach (var topology in topologies)
    {
        using var single = new ArrayMesh();
        var data = copied.SurfaceGetArrays((int)topology);
        single.AddSurfaceFromArrays(topology, data);
        var packed = RenderingServer.MeshGetSurface(single.GetRID(), 0);
        var rid = RenderingServer.MeshCreateFromSurfaces([packed]);
        var arraysRID = RenderingServer.MeshCreate();
        RenderingServer.MeshAddSurfaceFromArrays(arraysRID, topology, data);
        foreach (var candidate in new[] { rid, arraysRID })
        {
            Check(RenderingServer.MeshSurfaceGetPrimitiveType(candidate, 0) == topology &&
                RenderingServer.MeshSurfaceGetArrays(candidate, 0).Indices.SequenceEqual(data.Indices) &&
                RenderingServer.MeshGetSurface(candidate, 0).Primitive == topology, $"RID query {topology}.");
            foreach (var invalid in new[] { (MeshPrimitiveType)(-1), MeshPrimitiveType.Max, (MeshPrimitiveType)6 })
            {
                packed.Primitive = invalid;
                Reject(() => RenderingServer.MeshCreateFromSurfaces([packed]));
                Reject(() => RenderingServer.MeshAddSurface(candidate, packed));
                Reject(() => RenderingServer.MeshAddSurfaceFromArrays(candidate, invalid, data));
            }
            Check(RenderingServer.MeshGetSurfaceCount(candidate) == 1 &&
                RenderingServer.MeshSurfaceGetPrimitiveType(candidate, 0) == topology &&
                RenderingServer.MeshSurfaceGetArrays(candidate, 0).Vertices.SequenceEqual(data.Vertices),
                "Failed RID appends preserve the existing surface.");
        }
        var x = 8 + 28 * (int)topology;
        root.AddChild(new DrawTopology((ArrayMesh)single.Duplicate()) { Position = new(x, 8) });
        root.AddChild(new DrawTopology(rid) { Position = new(x, 40) });
        root.AddChild(new DrawTopology(arraysRID) { Position = new(x, 72) });
    }
    RenderingServer.FramePostDraw += ReadPixels;
};
Engine.Run(root);
Check(rendered, "Frame readback did not run.");

void ReadPixels()
{
    RenderingServer.FramePostDraw -= ReadPixels;
    using var image = root.GetTexture().GetImage() ?? throw new InvalidOperationException("Missing viewport readback.");
    foreach (var topology in topologies)
    {
        var x = 8 + 28 * (int)topology;
        var counts = new List<int>();
        foreach (var y in new[] { 8, 40, 72 })
        {
            var count = 0;
            for (var row = y - 2; row <= y + 18; row++)
                for (var col = x - 2; col <= x + 18; col++)
                    if (image.GetPixel(col, row).R > .5f) count++;
            var expected = topology switch
            {
                MeshPrimitiveType.Points => count == 4,
                MeshPrimitiveType.Lines => count is >= 30 and <= 36,
                MeshPrimitiveType.LineStrip => count is >= 44 and <= 55,
                MeshPrimitiveType.Triangles => count is >= 110 and <= 145,
                MeshPrimitiveType.TriangleStrip => count is >= 245 and <= 260,
                _ => false
            };
            Check(expected, $"{method}/{topology} at row {y}: {count} lit pixels.");
            var bridge = false;
            for (var row = y + 7; row <= y + 9; row++)
                for (var col = x + 7; col <= x + 9; col++)
                    bridge |= image.GetPixel(col, row).R > .5f;
            if (topology is MeshPrimitiveType.Points or MeshPrimitiveType.Lines or MeshPrimitiveType.LineStrip)
                Check(bridge == (topology == MeshPrimitiveType.LineStrip), $"Unexpected diagonal coverage for {topology}.");
            Check(image.GetPixel(x + 21, y + 8).R < .05f, $"Unexpected pixels beyond {topology}.");
            counts.Add(count);
        }
        Check(counts.Distinct().Count() == 1, $"Resource and RID pixel counts differ for {topology}.");
        Console.WriteLine($"{method} {topology}: {string.Join(", ", counts)} lit pixels (resource, packed RID, array RID).");
    }
    rendered = true;
    root.Tree!.Quit();
}

static MeshSurfaceData Geometry(MeshPrimitiveType topology) => new()
{
    Vertices = [new(0, 0), new(16, 0), new(0, 16), new(16, 16)],
    Indices = topology == MeshPrimitiveType.Triangles ? [0, 1, 2] : [0, 1, 2, 3]
};

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static void Reject(Action action)
{
    try { action(); }
    catch (ArgumentOutOfRangeException) { return; }
    throw new InvalidOperationException("Expected the invalid topology to be rejected.");
}

sealed class DrawTopology : Entity
{
    private readonly ArrayMesh? _resource;
    private readonly RID _mesh;
    public DrawTopology(ArrayMesh resource) => _resource = resource;
    public DrawTopology(RID mesh) => _mesh = mesh;
    protected override void OnDraw()
    {
        if (_resource is not null) DrawMesh(_resource);
        else DrawMesh(_mesh);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) _resource?.Dispose();
        base.Dispose(disposing);
    }
}
