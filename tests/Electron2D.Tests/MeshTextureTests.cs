using System.Diagnostics;
using Electron2D;
using static TextureArrayTests;

internal static partial class MeshTextureTests
{
    private static ArrayMesh Triangle()
    {
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new() { Vertices = [new(0, 0), new(16, 0), new(0, 8)], UVs = [new(0, 0), new(1, 0), new(0, 1)] }, flags: Mesh.ArrayFormat.UseDynamicUpdate);
        return mesh;
    }
    private static ImageTexture Solid(Color color)
    {
        using var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); image.Fill(color); return ImageTexture.CreateFromImage(image);
    }
    private sealed class Probe(MeshTexture texture) : Entity
    {
        internal Action<Probe>? Action;
        internal int Draws;
        protected override void OnDraw() { Draws++; if (Action is { } action) action(this); else DrawTexture(texture, Vector2.Zero); }
        internal void Record() { QueueRedraw(); PrepareCanvas(); }
        internal void Replay(List<CanvasVertex> vertices, List<CanvasBatch> batches) => AppendCanvas(vertices, batches, Transform.Identity);
    }
    private sealed class RIDTexture(Texture backing, Action callback) : Texture
    {
        public override int GetWidth() => 1;
        public override int GetHeight() => 1;
        public override RID GetRID() { callback(); return backing.GetRID(); }
    }
    internal static void Managed()
    {
        using var view = new MeshTexture();
        Check(view.Mesh is null && view.BaseTexture is null && view.ImageSize == Vector2.Zero && view.GetSize() == Vector2.Zero && !view.ResourceLocalToScene, "Empty mesh view defaults.");
        Check(!view.HasAlpha && !view.HasMIPMaps && view.MIPMapCount == 0 && view.GetImage() is null && view.IsPixelOpaque(int.MinValue, int.MaxValue), "Drawing-only views have no raster metadata and retain opaque hit testing.");
        var rid = view.GetRID(); Check(rid.IsValid() && view.GetRID() == rid, "Stable borrowed logical view identity.");
        using var mesh = Triangle(); using var image = Solid(Colors.Red);
        var changes = 0; view.Changed += _ => changes++;
        view.Mesh = mesh; view.BaseTexture = image; view.ImageSize = new(16, 8); view.Mesh = mesh; view.BaseTexture = image; view.ImageSize = new(16, 8);
        Check(changes == 3, "Actual assignments commit and notify; equal assignments are silent.");
        foreach (var size in new[] { new Vector2(float.NaN, 8), new(float.PositiveInfinity, 8), new(16, float.NegativeInfinity), new(float.MaxValue, 0), new(2147483648f, 0) })
            Reject<ArgumentOutOfRangeException>(() => view.ImageSize = size);
        Check(view.ImageSize == new Vector2(16, 8), "Invalid dimensions preserve previous state.");
        using var atlas = new AtlasTexture { Atlas = view }; using var canvas = new CanvasTexture { DiffuseTexture = view };
        Reject<ArgumentException>(() => view.BaseTexture = view); Reject<ArgumentException>(() => view.BaseTexture = atlas); Reject<ArgumentException>(() => view.BaseTexture = canvas);
        Check(ReferenceEquals(view.BaseTexture, image), "Direct and mixed texture cycles preserve the source.");
        using var other = Triangle(); var before = changes; view.Mesh = other; mesh.ClearSurfaces(); Check(changes == before + 1, "Replaced mesh subscription is detached.");
        other.ClearSurfaces(); Check(changes == before + 2, "Current mesh edits notify the view."); view.Mesh = mesh; mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new() { Vertices = [new(0, 0), new(16, 0), new(0, 8)], UVs = [new(0, 0), new(1, 0), new(0, 1)] });
        using var second = Solid(Colors.Blue); view.BaseTexture = second; before = changes;
        using (var pixels = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8)) { pixels.Fill(Colors.Green); image.Update(pixels); Check(changes == before, "Replaced image subscription is detached."); second.Update(pixels); Check(changes == before + 1, "Current image edits notify the view."); }
        view.BaseTexture = image;
        Action<Resource> failure = _ => throw new ApplicationException("fixture"); view.Changed += failure;
        Reject<ApplicationException>(() => view.ImageSize = new(16.5f, 8.5f)); view.Changed -= failure;
        Check(view.GetSize() == new Vector2(16, 8) && view.ImageSize == new Vector2(16.5f, 8.5f), "Observer failure retains fractional size; integer queries truncate.");
        using var shallow = (MeshTexture)view.Duplicate(); using var deep = (MeshTexture)view.Duplicate(true); using var deepMesh = deep.Mesh!; using var deepImage = deep.BaseTexture!;
        Check(ReferenceEquals(shallow.Mesh, mesh) && ReferenceEquals(shallow.BaseTexture, image) && !ReferenceEquals(deep.Mesh, mesh) && !ReferenceEquals(deep.BaseTexture, image) && deep.ImageSize == view.ImageSize, "Shallow/deep copies retain exact type and resource policy.");
        using var copy = new MeshTexture { Mesh = other, BaseTexture = second }; var copied = 0; copy.Changed += _ => copied++; copy.CopyFromResource(view);
        Check(copied == 1 && ReferenceEquals(copy.Mesh, mesh) && ReferenceEquals(copy.BaseTexture, image) && copy.ImageSize == view.ImageSize, "CopyFromResource coalesces notifications and updates subscriptions.");
        Geometry(view);
        ReentrantAtlas(image);
        Archive(view);
        SceneGraph(view);
        using var empty = new MeshTexture(); using var node = new Probe(empty); using var tree = new SceneTree(node);
        node.Record(); Check(!node.HasCanvasCommands, "Missing resources draw nothing."); empty.Mesh = mesh; empty.BaseTexture = image;
        node.Record(); Check(node.HasCanvasCommands, "Point drawing is independent of zero logical image size.");
        node.Action = n => n.DrawTextureRect(empty, new(0, 0, 32, 16), false); node.Record(); Check(!node.HasCanvasCommands, "Zero logical rectangle axes skip undefined division.");
        Reject<InvalidOperationException>(() => view.Draw(node, default));
        Task.Run(() => Reject<InvalidOperationException>(() => node.Record())).GetAwaiter().GetResult();
        using var doomedMesh = Triangle(); using var doomedImage = Solid(Colors.White); empty.Mesh = doomedMesh; empty.BaseTexture = doomedImage;
        doomedMesh.Dispose(); doomedImage.Dispose(); Check(empty.Mesh is null && empty.BaseTexture is null && !empty.IsDisposed, "Borrowed disposal clears both edges.");
        Reject<ObjectDisposedException>(() => empty.Mesh = doomedMesh); Reject<ObjectDisposedException>(() => empty.BaseTexture = doomedImage);
        empty.Dispose(); Check(!mesh.IsDisposed && !image.IsDisposed, "View disposal preserves borrowed resources.");
        Reject<ObjectDisposedException>(() => empty.GetWidth()); Reject<ObjectDisposedException>(() => empty.BaseTexture = image);
        view.Dispose(); Reject<ArgumentException>(() => RenderingTextureRegistry.Resolve(rid));
        Console.WriteLine("MeshTexture managed defaults, geometry, graphs, archive, guards, notifications and borrowed lifecycle passed.");
    }
    private static void Geometry(MeshTexture view)
    {
        using var node = new Probe(view); using var tree = new SceneTree(node);
        var vertices = new List<CanvasVertex>(64); var batches = new List<CanvasBatch>(8);
        void Verify(Action<Probe> draw, Vector2 a, Vector2 b, Vector2 c)
        {
            node.Action = draw; node.Record(); vertices.Clear(); batches.Clear(); node.Replay(vertices, batches);
            Check(vertices.Count == 3 && vertices[0].Position == a && vertices[1].Position == b && vertices[2].Position == c, "Reference mesh-texture geometry: " + string.Join(",", vertices.Select(v => v.Position)));
            Check(batches.Count == 1 && ReferenceEquals(batches[0].Texture, view.BaseTexture), "The real mesh samples the borrowed base image.");
        }
        view.ImageSize = new(16, 8);
        Verify(n => view.Draw(n, new(3, 5)), new(3, 5), new(19, 5), new(3, 13));
        Verify(n => view.Draw(n, new(3, 5), transpose: true), new(3, 5), new(19, 5), new(3, 13));
        Verify(n => view.DrawRect(n, new(3, 5, 32, 24), false), new(3, 5), new(35, 5), new(3, 29));
        Verify(n => view.DrawRect(n, new(3, 5, 32, 24), true, transpose: true), new(3, 5), new(51, 5), new(3, 21));
        Verify(n => view.DrawRect(n, new(3, 5, -32, 24), false), new(19, 5), new(-13, 5), new(19, 29));
        Verify(n => view.DrawRect(n, new(3, 5, 32, -24), false), new(3, 13), new(35, 13), new(3, -11));
        Verify(n => view.DrawRect(n, new(3, 5, -32, -24), false, transpose: true), new(19, 13), new(-29, 13), new(19, -3));
        Verify(n => view.DrawRectRegion(n, new(3, 5, 32, 24), new(100, -200, 0, 0)), new(3, 5), new(35, 5), new(3, 29));
        view.ImageSize = new(16.5f, 8.5f); Verify(n => view.DrawRect(n, new(3, 5, 33, 17), false), new(3, 5), new(35, 5), new(3, 21));
        view.ImageSize = new(-16, 8); Verify(n => view.DrawRect(n, new(3, 5, 32, 24), false), new(3, 5), new(-29, 5), new(3, 29));
        view.ImageSize = new(float.Epsilon, 8); node.Action = n => view.DrawRect(n, new(0, 0, 32, 24), false);
        Reject<ArgumentException>(() => node.Record()); Check(!node.HasCanvasCommands, "Finite metadata cannot publish an overflowing scale.");
        view.ImageSize = new(16, 8);
        foreach (var draw in new Action<Probe>[] { n => view.Draw(n, new(float.NaN, 0)), n => view.DrawRect(n, new(0, 0, float.PositiveInfinity, 1), false), n => view.DrawRectRegion(n, new(0, 0, 1, 1), new(float.NaN, 0, 1, 1)), n => view.Draw(n, default, new(float.NaN, 1, 1, 1)) })
        { node.Action = draw; Reject<ArgumentException>(() => node.Record()); Check(!node.HasCanvasCommands, "Invalid drawing leaves no partial commands."); }
        node.Action = n => view.DrawRect(n, new(0, 0, 32, 24), false); node.Record(); node.Replay(vertices, batches);
        for (var pass = 0; pass < 2; pass++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 2000; i++) { node.Record(); vertices.Clear(); batches.Clear(); node.Replay(vertices, batches); _ = view.GetSize(); _ = view.GetRID(); }
            if (pass == 1) Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared matrix/geometry recording and replay allocate zero bytes.");
        }
    }
    private static void Archive(MeshTexture view)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "electron2d-mesh-texture-" + Guid.NewGuid().ToString("N") + ".e2dres");
        try
        {
            ResourceSaver.Save(view, path); var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true }; if (System.IO.Path.GetFileNameWithoutExtension(start.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(typeof(MeshTextureTests).Assembly.Location); start.Environment["ELECTRON2D_TEST_MESH_TEXTURE"] = "archive"; start.Environment["ELECTRON2D_MESH_TEXTURE_ARCHIVE"] = path;
            using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEnd(); var error = child.StandardError.ReadToEnd(); child.WaitForExit();
            Check(child.ExitCode == 0, "Fresh-process archive: " + output + error);
        }
        finally { File.Delete(path); }
    }
    private static void ReentrantAtlas(Texture backing)
    {
        using var mesh = Triangle(); using var source = new RIDTexture(backing, mesh.Dispose); using var atlas = new AtlasTexture { Atlas = source };
        using var view = new MeshTexture { Mesh = mesh, BaseTexture = atlas, ImageSize = new(16, 8) };
        using var node = new Probe(view); using var tree = new SceneTree(node);
        Reject<ObjectDisposedException>(() => node.Record()); Check(!node.HasCanvasCommands && view.Mesh is null, "Virtual source callbacks cannot publish disposed geometry.");
        using var instances = new MultiMesh(); using var second = new RIDTexture(backing, instances.Dispose); using var secondAtlas = new AtlasTexture { Atlas = second };
        node.Action = n => n.DrawMultiMesh(instances, secondAtlas);
        Reject<ObjectDisposedException>(() => node.Record()); Check(!node.HasCanvasCommands, "Virtual source callbacks cannot publish disposed instance storage.");
    }
    private static void SceneGraph(MeshTexture view)
    {
        view.ResourceLocalToScene = view.Mesh!.ResourceLocalToScene = view.BaseTexture!.ResourceLocalToScene = true;
        using var root = new Entity(); var left = new Sprite { Name = "first", Texture = view }; var right = new Sprite { Name = "second", Texture = view }; root.AddChild(left); root.AddChild(right); left.Owner = right.Owner = root;
        using var packed = new PackedScene(); packed.Pack(root);
        MeshTexture first; Mesh firstMesh; Texture firstImage;
        using (var a = packed.Instantiate()) using (var b = packed.Instantiate())
        {
            first = (MeshTexture)a.GetNode<Sprite>("first").Texture!; firstMesh = first.Mesh!; firstImage = first.BaseTexture!;
            Check(ReferenceEquals(first, a.GetNode<Sprite>("second").Texture) && !ReferenceEquals(first, b.GetNode<Sprite>("first").Texture) && !ReferenceEquals(firstMesh, view.Mesh) && !ReferenceEquals(firstImage, view.BaseTexture) && ReferenceEquals(first.GetLocalScene(), a), "Scene-local graphs preserve aliases within an instance and isolate instances.");
        }
        Check(first.IsDisposed && firstMesh.IsDisposed && firstImage.IsDisposed && !view.IsDisposed && !view.Mesh.IsDisposed && !view.BaseTexture.IsDisposed, "Scene instances own localized graphs and preserve sources.");
        view.ResourceLocalToScene = view.Mesh.ResourceLocalToScene = view.BaseTexture.ResourceLocalToScene = false;
    }
    internal static void Run(string profile)
    {
        if (profile == "archive")
        {
            using var view = ResourceLoader.Load<MeshTexture>(Environment.GetEnvironmentVariable("ELECTRON2D_MESH_TEXTURE_ARCHIVE")!, cacheMode: ResourceLoader.CacheMode.Ignore)!;
            Check(view.Mesh is ArrayMesh mesh && mesh.GetFaces().Length == 3 && view.BaseTexture is ImageTexture && view.ImageSize == new Vector2(16, 8), "Exact factories restore the mesh/image view graph.");
            view.Mesh!.Dispose(); view.BaseTexture!.Dispose(); return;
        }
        if (profile == "managed") { Managed(); return; }
        Native(profile);
    }
}
