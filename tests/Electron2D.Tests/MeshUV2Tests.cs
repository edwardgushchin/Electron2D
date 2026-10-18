using System.Buffers.Binary;
using System.Diagnostics;
using Electron2D;
using static TextureArrayTests;

internal static class MeshUV2Tests
{
    private static MeshSurfaceData Data() => new() { Vertices = [new(0, 0), new(16, 0), new(16, 16), new(0, 16)], Colors = [Colors.White, Colors.White, Colors.White, Colors.White], UVs = [new(.25f, 0), new(.25f, 0), new(.25f, 0), new(.25f, 0)], UV2s = [Vector2.One, Vector2.One, Vector2.One, Vector2.One], Indices = [0, 1, 2, 0, 2, 3] };
    internal static void Run()
    {
        Managed(); Storage(); foreach (var language in new[] { "GLSL", "HLSL" }) { Render(language); Lighting(language); }
        Compatibility();
        Console.WriteLine("Mesh secondary UV: typed copies, packed updates, legacy/new archives, builder capture, ordinary/custom/instance/skin/light/group pixels and warmed zero allocation passed.");
    }
    private static void Managed()
    {
        using var mesh = new ArrayMesh(); var data = Data(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, data); data.UV2s[0] = Vector2.Zero; Check(mesh.SurfaceGetArrays(0).UV2s[0] == Vector2.One && (mesh.SurfaceGetFormat(0) & Mesh.ArrayFormat.TexUV2) != 0, "Secondary imports copy independent data and infer their format."); var query = mesh.SurfaceGetArrays(0); query.UV2s[0] = Vector2.Zero; Check(mesh.SurfaceGetArrays(0).UV2s[0] == Vector2.One, "Queries copy the secondary channel.");
        foreach (var bad in new[] { new Vector2[] { Vector2.Zero }, new[] { new Vector2(float.NaN, 0), Vector2.Zero, Vector2.Zero, Vector2.Zero } }) { var broken = Data(); broken.UV2s = bad; Reject<ArgumentException>(() => mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, broken)); }
        var missing = Data(); missing.UV2s = []; Reject<ArgumentException>(() => mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, missing, flags: Mesh.ArrayFormat.TexUV2)); var nulls = Data(); nulls.UV2s = null!; Reject<ArgumentNullException>(() => mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, nulls));
        var patch = new byte[40]; for (var i = 0; i < 2; i++) { patch[i * 20] = 255; patch[i * 20 + 3] = 255; BinaryPrimitives.WriteSingleLittleEndian(patch.AsSpan(i * 20 + 4), .5f); BinaryPrimitives.WriteSingleLittleEndian(patch.AsSpan(i * 20 + 12), .75f); }
        BinaryPrimitives.WriteSingleLittleEndian(patch.AsSpan(32), float.NaN); Reject<ArgumentException>(() => mesh.SurfaceUpdateAttributeRegion(0, 0, patch)); Check(mesh.SurfaceGetArrays(0).UV2s[0] == Vector2.One && mesh.SurfaceGetArrays(0).Colors[0] == Colors.White && mesh.SurfaceGetArrays(0).UVs[0].X == .25f, "Invalid later secondary coordinates roll back earlier primary/color records."); BinaryPrimitives.WriteSingleLittleEndian(patch.AsSpan(32), .75f); mesh.SurfaceUpdateAttributeRegion(0, 0, patch); Check(mesh.SurfaceGetArrays(0).UV2s[1].X == .75f && mesh.SurfaceGetArrays(0).UVs[0].X == .5f && mesh.SurfaceGetArrays(0).Colors[0] == Colors.Red, "Both coordinate channels execute their packed offsets.");
        byte[] scalar = new byte[4]; BinaryPrimitives.WriteSingleLittleEndian(scalar, -.5f); mesh.SurfaceUpdateAttributeRegion(0, 13, scalar.AsSpan(1, 3)); Check(mesh.SurfaceGetArrays(0).UV2s[0].X == -.5f, "Partial byte update preserves untouched bytes in the secondary channel."); mesh.SurfaceUpdateAttributeRegion(0, 80, []); Reject<ArgumentOutOfRangeException>(() => mesh.SurfaceUpdateAttributeRegion(0, int.MaxValue, scalar));
        using var secondaryOnly = new ArrayMesh(); var only = Data(); only.Colors = []; only.UVs = []; secondaryOnly.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, only); secondaryOnly.SurfaceUpdateAttributeRegion(0, 0, scalar); Check(secondaryOnly.SurfaceGetArrays(0).UV2s[0].X == -.5f && secondaryOnly.SurfaceGetArrays(0).UVs.Length == 0, "Secondary-only packed storage has no implied primary channel.");
        using var duplicate = (ArrayMesh)mesh.Duplicate(); Check(duplicate.SurfaceGetArrays(0).UV2s.SequenceEqual(mesh.SurfaceGetArrays(0).UV2s), "Duplication carries the independent channel.");
        var descriptor = (PropertyDescriptor<ArrayMesh, byte[]>)mesh.GetPropertyList().Single(p => p.Name == "_surface_data"); var bytes = descriptor.GetValue(mesh); Check(BinaryPrimitives.ReadUInt32LittleEndian(bytes) == 0x38534d41, "Extended mesh archives write AMS8."); using var restored = new ArrayMesh(); descriptor.SetValue(restored, bytes); Check(restored.SurfaceGetArrays(0).UV2s.SequenceEqual(mesh.SurfaceGetArrays(0).UV2s), "Extended archive round trip preserves secondary coordinates."); var badArchive = (byte[])bytes.Clone(); BinaryPrimitives.WriteInt32LittleEndian(badArchive.AsSpan(40), int.MaxValue); Reject<InvalidDataException>(() => descriptor.SetValue(restored, badArchive)); Check(restored.GetSurfaceCount() == 1, "Malformed secondary archive counts preserve prior storage.");
        using var stream = new MemoryStream(); using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true)) { writer.Write(0x31534d41u); writer.Write(1); writer.Write((int)Mesh.PrimitiveType.Points); writer.Write((ulong)(Mesh.ArrayFormat.Vertex | Mesh.ArrayFormat.Use2DVertices)); writer.Write(0); writer.Write(1); writer.Write(0); writer.Write(0); writer.Write(0); writer.Write(0); writer.Write(3f); writer.Write(4f); }
        descriptor.SetValue(restored, stream.ToArray()); Check(restored.SurfaceGetArrays(0).UV2s.Length == 0 && restored.SurfaceGetArrays(0).Vertices[0] == new Vector2(3, 4), "Legacy AMS1 remains readable without a secondary channel.");
        using var immediate = new ImmediateMesh(); Reject<InvalidOperationException>(() => immediate.SurfaceSetUV2(Vector2.Zero)); immediate.SurfaceBegin(Mesh.PrimitiveType.Triangles); immediate.SurfaceAddVertex2D(Vector2.Zero); Reject<ArgumentException>(() => immediate.SurfaceSetUV2(new(float.NaN, 0))); immediate.SurfaceSetUV2(new(.25f, .5f)); immediate.SurfaceAddVertex2D(new(1, 0)); immediate.SurfaceSetUV2(Vector2.One); Reject<ArgumentException>(immediate.SurfaceEnd); immediate.SurfaceAddVertex2D(new(0, 1)); immediate.SurfaceEnd(); Check(immediate.SurfaceGetArrays(0).UV2s.SequenceEqual(new[] { new Vector2(.25f, .5f), new Vector2(.25f, .5f), Vector2.One }), "Immediate first activation backfills and later writes affect future vertices across retry."); using var copied = (ImmediateMesh)immediate.Duplicate(); Check(copied.SurfaceGetArrays(0).UV2s[2] == Vector2.One, "Immediate duplication preserves completed secondary data."); immediate.SurfaceBegin(Mesh.PrimitiveType.Points); immediate.SurfaceAddVertex2D(Vector2.Zero); immediate.SurfaceEnd(); Check(immediate.SurfaceGetArrays(1).UV2s.Length == 0, "New drafts reset secondary activation."); immediate.Dispose(); Reject<ObjectDisposedException>(() => immediate.SurfaceSetUV2(Vector2.Zero));
        using var archivedImmediate = new ImmediateMesh(); archivedImmediate.SurfaceBegin(Mesh.PrimitiveType.Points); archivedImmediate.SurfaceSetUV2(Vector2.One); archivedImmediate.SurfaceAddVertex2D(Vector2.Zero); archivedImmediate.SurfaceEnd(); var immediateDescriptor = (PropertyDescriptor<ImmediateMesh, byte[]>)archivedImmediate.GetPropertyList().Single(p => p.Name == "_surface_data"); var immediateBytes = immediateDescriptor.GetValue(archivedImmediate); archivedImmediate.SurfaceBegin(Mesh.PrimitiveType.Points); archivedImmediate.SurfaceAddVertex2D(Vector2.One); Reject<InvalidDataException>(() => immediateDescriptor.SetValue(archivedImmediate, new byte[8])); archivedImmediate.SurfaceEnd(); Check(archivedImmediate.GetSurfaceCount() == 2, "Invalid file restoration preserves immediate storage and active draft."); immediateDescriptor.SetValue(archivedImmediate, immediateBytes); Check(archivedImmediate.GetSurfaceCount() == 1 && archivedImmediate.SurfaceGetArrays(0).UV2s[0] == Vector2.One, "Successful restoration atomically replaces completed immediate surfaces."); archivedImmediate.SurfaceBegin(Mesh.PrimitiveType.Points); immediateDescriptor.SetValue(archivedImmediate, immediateBytes); Reject<InvalidOperationException>(archivedImmediate.SurfaceEnd);
        var vertices = new List<CanvasVertex>(4096); var batches = new List<CanvasBatch>(16); var recorder = new Draw(mesh); using var root = new Node(); root.AddChild(recorder); using var tree = new SceneTree(root); recorder.Record(); void Replay() { vertices.Clear(); batches.Clear(); recorder.AppendCanvas(vertices, batches, Transform.Identity); }
        Replay(); Check(vertices[0].UV2 == mesh.SurfaceGetArrays(0).UV2s[0], "Retained geometry carries the actual independent coordinate."); GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); for (var i = 0; i < 16; i++) Replay(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) { mesh.SurfaceUpdateAttributeRegion(0, 12, scalar); Replay(); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared secondary updates/replay allocate zero managed bytes."); recorder.Dispose();
    }
    private sealed class Draw(Mesh mesh) : Entity { internal void Record() { QueueRedraw(); PrepareCanvas(); } protected override void OnDraw() => DrawMesh(mesh, null); }
    private sealed class Custom(MeshSurfaceData data) : Mesh
    {
        protected override int OnGetSurfaceCount() => 1;
        protected override MeshSurfaceData OnSurfaceGetArrays(int surfaceIndex) => data;
        protected override int OnSurfaceGetArrayLen(int surfaceIndex) => data.Vertices.Length;
        protected override int OnSurfaceGetArrayIndexLen(int surfaceIndex) => data.Indices.Length;
        protected override PrimitiveType OnSurfaceGetPrimitiveType(int surfaceIndex) => PrimitiveType.Triangles;
        protected override ArrayFormat OnSurfaceGetFormat(int surfaceIndex) => ArrayFormat.Vertex | ArrayFormat.Use2DVertices | ArrayFormat.TexUV | ArrayFormat.TexUV2 | ArrayFormat.Color | ArrayFormat.Index;
        protected override Material? OnSurfaceGetMaterial(int surfaceIndex) => null;
        protected override void OnSurfaceSetMaterial(int surfaceIndex, Material? material) { }
    }
    private static Shader Shader(string language)
    { using var input = typeof(MeshUV2Tests).Assembly.GetManifestResourceStream("Electron2D.Device.MeshUV2" + language + ".frag.spv")!; using var stream = new MemoryStream(); input.CopyTo(stream); return Electron2D.Shader.CreateFromSPIRV(stream.ToArray()); }
    private static void Render(string language)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); using var shader = Shader(language); using var material = new ShaderMaterial { Shader = shader };
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, Data()); using var custom = new Custom(Data()); using var immediate = new ImmediateMesh(); immediate.SurfaceBegin(Mesh.PrimitiveType.Triangles);
        foreach (var index in Data().Indices) { immediate.SurfaceSetUV(new(.25f, 0)); immediate.SurfaceSetUV2(Vector2.One); immediate.SurfaceAddVertex2D(Data().Vertices[index]); }
        immediate.SurfaceEnd();
        using var instances = new MultiMesh { Mesh = mesh, UseCustomData = true, InstanceCount = 2 }; instances.SetInstanceTransform2D(0, Transform.Identity); instances.SetInstanceTransform2D(1, new(0, new(28, 0))); instances.SetInstanceCustomData(0, Colors.Blue); instances.SetInstanceCustomData(1, Colors.Black);
        using var skinned = new ArrayMesh(); var skin = Data(); skin.Bones = new int[16]; skin.Weights = new float[16]; for (var i = 0; i < 4; i++) skin.Weights[i * 4] = 1; skinned.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, skin);
        using var expanded = new MultiMesh { Mesh = skinned, UseCustomData = true, InstanceCount = 1 }; expanded.SetInstanceTransform2D(0, Transform.Identity); expanded.SetInstanceCustomData(0, Colors.Blue); var expandedNode = new MultiMeshInstance { Name = "Expanded", MultiMesh = expanded, Material = material, Position = new(92, 40) };
        using var absent = new ArrayMesh(); var absentData = Data(); absentData.UV2s = []; absent.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, absentData);
        using var window = new Window { Size = new(288, 72) }; window.AddChild(expandedNode); window.AddChild(new MeshInstance { Name = "Absent", Mesh = absent, Material = material, Position = new(120, 40) }); var normal = new MeshInstance { Name = "Normal", Mesh = mesh, Material = material, Position = new(8, 8) }; window.AddChild(normal); window.AddChild(new MeshInstance { Name = "Immediate", Mesh = immediate, Material = material, Position = new(36, 8) }); window.AddChild(new MeshInstance { Name = "Custom", Mesh = custom, Material = material, Position = new(64, 8) }); window.AddChild(new MultiMeshInstance { MultiMesh = instances, Material = material, Position = new(92, 8) }); var skinNode = new MeshInstance { Name = "Skin", Mesh = skinned, Material = material, Position = new(148, 8) }; window.AddChild(skinNode);
        var group = new CanvasGroup { Position = new(176, 0), SelfModulate = new(1, 1, 1, .5f) }; group.AddChild(new MeshInstance { Name = "GroupMesh", Mesh = mesh, Material = material, Position = new(8, 8) }); window.AddChild(group);
        using var points = new ArrayMesh(); points.AddSurfaceFromArrays(Mesh.PrimitiveType.Points, new() { Vertices = [new(.5f, .5f)], UVs = [new(.25f, 0)], UV2s = [Vector2.One] }); window.AddChild(new MeshInstance { Name = "Points", Mesh = points, Material = material, Position = new(8, 40) });
        using var line = new ArrayMesh(); line.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, new() { Vertices = [new(.5f, .5f), new(14.5f, .5f)], UVs = [new(.25f, 0), new(.25f, 0)], UV2s = [Vector2.One, Vector2.One] }); window.AddChild(new MeshInstance { Name = "Line", Mesh = line, Material = material, Position = new(36, 40) });
        using var strip = new ArrayMesh(); var stripData = Data(); stripData.Vertices = [new(0, 0), new(16, 0), new(0, 16), new(16, 16)]; stripData.Indices = []; strip.AddSurfaceFromArrays(Mesh.PrimitiveType.TriangleStrip, stripData); window.AddChild(new MeshInstance { Name = "Strip", Mesh = strip, Material = material, Position = new(64, 40) });
        RID owned = default, palette = default; var frame = 0; long before = 0, allocated = 0; var patch = new byte[80]; for (var i = 0; i < 4; i++) { patch[i * 20] = patch[i * 20 + 1] = patch[i * 20 + 2] = patch[i * 20 + 3] = 255; BinaryPrimitives.WriteSingleLittleEndian(patch.AsSpan(i * 20 + 4), .25f); BinaryPrimitives.WriteSingleLittleEndian(patch.AsSpan(i * 20 + 12), .5f); }
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == "gpu", "UV2 initializes actual GPU without fallback."); RenderingServer.SetDefaultClearColor(Colors.Black); palette = RenderingServer.SkeletonCreate(); RenderingServer.SkeletonAllocateData(palette, 1); RenderingServer.SkeletonBoneSetTransform2D(palette, 0, Transform.Identity); RenderingServer.CanvasItemAttachSkeleton(skinNode.GetCanvasItem(), palette); RenderingServer.CanvasItemAttachSkeleton(expandedNode.GetCanvasItem(), palette);
            owned = RenderingServer.MeshCreate(); RenderingServer.MeshAddSurfaceFromArrays(owned, Mesh.PrimitiveType.Triangles, Data()); window.AddChild(new Owned(owned) { Position = new(212, 8), Material = material }); Check(RenderingServer.MeshSurfaceGetFormatAttributeStride(RenderingServer.MeshSurfaceGetFormat(owned, 0), 4) == 20 && RenderingServer.MeshSurfaceGetFormatAttributeStride(Mesh.ArrayFormat.TexUV2, 4) == 8, "Owned format stride projects all independent channels.");
            window.Tree!.ProcessFrameStarted += _ => { if (frame >= 24) before = GC.GetAllocatedBytesForCurrentThread(); }; RenderingServer.FramePreDraw += Pre; RenderingServer.FramePostDraw += Post;
        };
        void Pre()
        {
            if (frame == 16) GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
            if (frame >= 24 && frame < 88) mesh.SurfaceUpdateAttributeRegion(0, 0, patch);
        }
        void Post()
        {
            if (frame >= 24) allocated += GC.GetAllocatedBytesForCurrentThread() - before;
            if (frame is 3 or 5)
            {
                using var image = window.GetTexture().GetImage()!; var red = frame == 3 ? 1f : .5f;
                void Pixel(int x, int y, float r, float g, float b) { var p = image.GetPixel(x, y); Check(Math.Abs(p.R - r) < .012f && Math.Abs(p.G - g) < .012f && Math.Abs(p.B - b) < .012f, language + " UV2 pixel " + x + "," + y + ": " + p); }
                Pixel(12, 12, red, .25f, 0); Pixel(40, 12, 1, .25f, 0); Pixel(68, 12, 1, .25f, 0); Pixel(96, 12, red, .25f, 1); Pixel(124, 12, red, .25f, 0); Pixel(152, 12, 1, .25f, 0); Pixel(188, 12, red * .5f, .125f, 0); Pixel(216, 12, red, .25f, 0); Pixel(8, 40, 1, .25f, 0); Pixel(40, 40, 1, .25f, 0); Pixel(68, 44, 1, .25f, 0); Pixel(96, 44, 1, .25f, 1); Pixel(124, 44, 0, .25f, 0);
                var directory = Environment.GetEnvironmentVariable("ELECTRON2D_MESH_UV2_OUTPUT"); if (directory is not null) { Directory.CreateDirectory(directory); image.SavePNG(System.IO.Path.Combine(directory, language + "-uv2-" + frame + ".png")); }
            }
            if (frame == 3) { mesh.SurfaceUpdateAttributeRegion(0, 0, patch); RenderingServer.MeshSurfaceUpdateAttributeRegion(owned, 0, 0, patch); }
            if (++frame == 152) { RenderingServer.FramePreDraw -= Pre; RenderingServer.FramePostDraw -= Post; RenderingServer.FreeRID(owned); RenderingServer.FreeRID(palette); window.Tree!.Quit(); }
        }
        Engine.Run(window); Check(allocated == 0, language + " prepared secondary coordinate active/idle render frames allocate zero managed bytes: " + allocated); Console.WriteLine(language + " secondary UV: 64 active + 64 idle frames, " + allocated + " managed bytes.");
    }
    private sealed class Owned(RID mesh) : Entity { protected override void OnDraw() => DrawMesh(mesh); }

    private static void Storage()
    {
        using var root = new Node(); using var material = new CanvasItemMaterial(); using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, Data()); mesh.SurfaceSetMaterial(0, material); using var immediate = new ImmediateMesh(); immediate.SurfaceBegin(Mesh.PrimitiveType.Points, material); immediate.SurfaceSetUV2(new(.5f, .75f)); immediate.SurfaceAddVertex2D(Vector2.Zero); immediate.SurfaceEnd(); immediate.SurfaceBegin(Mesh.PrimitiveType.Triangles); immediate.SurfaceAddVertex2D(Vector2.Zero); root.AddChild(new MeshInstance { Name = "Array", Mesh = mesh }); root.AddChild(new MeshInstance { Name = "Immediate", Mesh = immediate }); foreach (var child in root.GetChildren()) child.Owner = root; using var scene = new PackedScene(); scene.Pack(root); var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-uv2-" + Guid.NewGuid().ToString("N") + ".e2dscene");
        try
        {
            ResourceSaver.Save(scene, path); var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(typeof(MeshUV2Tests).Assembly.Location); start.Environment.Remove("ELECTRON2D_TEST_MESH_UV2"); start.Environment["ELECTRON2D_TEST_MESH_UV2_CHILD"] = path; using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync(); if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Mesh UV2 child."); }
            Check(child.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh mesh UV2 passed"), error.GetAwaiter().GetResult());
        }
        finally { File.Delete(path); }
    }
    internal static void RunChild(string path)
    {
        using var scene = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); using var root = scene.Instantiate(); var array = (ArrayMesh)((MeshInstance)root.GetNode("Array")).Mesh!; var immediate = (ImmediateMesh)((MeshInstance)root.GetNode("Immediate")).Mesh!; Check(array.SurfaceGetArrays(0).UV2s.SequenceEqual(Data().UV2s) && immediate.SurfaceGetArrays(0).UV2s[0] == new Vector2(.5f, .75f), "Fresh typed array and immediate archive factories preserve secondary channels."); Check(ReferenceEquals(array.SurfaceGetMaterial(0), immediate.SurfaceGetMaterial(0)), "Fresh immediate archive retains material graph aliases."); Reject<InvalidOperationException>(immediate.SurfaceEnd); using var tree = new SceneTree(root); var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>(); foreach (var name in new[] { "Array", "Immediate" }) { var node = (MeshInstance)root.GetNode(name); node.PrepareCanvas(); node.AppendCanvas(vertices, batches, Transform.Identity); }
        Check(vertices[0].UV2 == Vector2.One && vertices[^1].UV2 == new Vector2(.5f, .75f), "Fresh saved channels execute retained drawing."); Console.WriteLine("Fresh mesh UV2 passed.");
    }
    private static void Lighting(string language)
    {
        using var shader = Shader(language); using var material = new ShaderMaterial { Shader = shader }; using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, Data()); using var instances = new MultiMesh { Mesh = mesh, UseCustomData = true, InstanceCount = 1 }; instances.SetInstanceTransform2D(0, Transform.Identity); instances.SetInstanceCustomData(0, Colors.Blue); using var window = new Window { Size = new(96, 64) }; window.AddChild(new MeshInstance { Mesh = mesh, Material = material, Position = new(8, 8) }); window.AddChild(new MultiMeshInstance { MultiMesh = instances, Material = material, Position = new(36, 8) }); window.AddChild(new DirectionalLight { Color = Colors.White, Energy = .25f }); var frame = 0;
        window.Ready += _ => { RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.CanvasSetModulate(window.FindWorld()!.Canvas, new(.1f, .1f, .1f)); RenderingServer.FramePostDraw += Post; };
        void Post() { using var pixels = window.GetTexture().GetImage()!; var plain = pixels.GetPixel(12, 12); var instanced = pixels.GetPixel(40, 12); Check(Math.Abs(plain.R - .35f) < .012f && Math.Abs(plain.G - .0875f) < .012f && plain.B == 0 && Math.Abs(instanced.B - .35f) < .012f, language + " UV2 and instance data survive lighting composition: " + plain + "/" + instanced); if (++frame == 2) { RenderingServer.FramePostDraw -= Post; window.Tree!.Quit(); } }
        Engine.Run(window);
    }
    private static void Compatibility()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "compatibility"); using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, Data()); using var window = new Window { Size = new(64, 64) }; window.AddChild(new MeshInstance { Mesh = mesh, Position = new(8, 8) }); window.Ready += _ => { RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += Post; }; void Post() { using var image = window.GetTexture().GetImage()!; Check(image.GetPixel(12, 12) == Colors.White, "Compatibility keeps primary drawing with secondary storage."); RenderingServer.FramePostDraw -= Post; window.Tree!.Quit(); }
        Engine.Run(window);
        using var shader = Shader("GLSL"); using var material = new ShaderMaterial { Shader = shader }; using var rejected = new Window { Size = new(64, 64) }; rejected.AddChild(new MeshInstance { Mesh = mesh, Material = material }); Reject<NotSupportedException>(() => Engine.Run(rejected)); Check(rejected.IsDisposed && RenderingServer.Service is null, "Compatibility explicitly rejects programmable UV2 and tears down."); ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu");
    }
}
