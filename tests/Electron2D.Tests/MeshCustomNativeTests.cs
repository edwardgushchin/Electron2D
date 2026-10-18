using Electron2D;
using static TextureArrayTests;

internal static class MeshCustomNativeTests
{
    internal static void Run()
    {
        MeshCustomTests.Run(); foreach (var language in new[] { "GLSL", "HLSL" }) Retained(language); Retained("baseline", "compatibility");
        Console.WriteLine("Mesh custom: four channels/eight formats reach real ordinary/authored/custom/instanced/skinned/strip/line/point/group pixels.");
    }
    private static MeshSurfaceData Data(int first = 0, bool changed = false)
    {
        var data = MeshFrameTests.Data(); for (var channel = 0; channel < 4; channel++) data.SetCustom(channel, MeshCustomTests.Channel((MeshCustomFormat)(first + channel), new(changed ? 0 : first == 0 && channel == 1 ? -2 : 1, 0, 0, 1))); return data;
    }
    private static byte[] Patch(int first)
    {
        var data = Data(first, true); var bytes = new byte[MeshCustomCodec.AttributeStride(MeshCustomCodec.Channels(data) | Mesh.ArrayFormat.TexUV) * 4]; var stride = bytes.Length / 4;
        for (var vertex = 0; vertex < 4; vertex++) { var offset = vertex * stride + 8; for (var channel = 0; channel < 4; channel++) { var custom = data.Custom(channel)!; var width = MeshCustomCodec.Stride(custom.Format); custom.Data.AsSpan(vertex * width, width).CopyTo(bytes.AsSpan(offset)); offset += width; } }
        return bytes;
    }
    private static Shader Shader(string language)
    {
        using var input = typeof(MeshCustomNativeTests).Assembly.GetManifestResourceStream("Electron2D.Device.MeshCustom" + language + ".frag.spv")!; using var bytes = new MemoryStream(); input.CopyTo(bytes); return Electron2D.Shader.CreateFromSPIRV(bytes.GetBuffer().AsSpan(0, (int)bytes.Length));
    }
    private static void Retained(string language, string backend = "gpu")
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); using var shader = backend == "gpu" ? Shader(language) : null; using Material material = shader is null ? new CanvasItemMaterial() : new ShaderMaterial { Shader = shader };
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, Data()); mesh.SurfaceSetMaterial(0, material); using var floats = new ArrayMesh(); floats.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, Data(4)); floats.SurfaceSetMaterial(0, material);
        using var builder = new SurfaceTool(); builder.CreateFromArrays(Data()); builder.Index(); builder.GenerateNormals(); builder.GenerateTangents(); builder.SetMaterial(material); using var authored = builder.Commit(); using var custom = new Custom(material);
        using var skin = new ArrayMesh(); var skinnedData = Data(4); skinnedData.Bones = new int[32]; skinnedData.Weights = new float[32]; for (var i = 0; i < 4; i++) { skinnedData.Bones[i * 8 + 5] = 5; skinnedData.Weights[i * 8 + 5] = 1; }
        skin.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, skinnedData, flags: Mesh.ArrayFormat.Use8BoneWeights); skin.SurfaceSetMaterial(0, material);
        using var strip = new ArrayMesh(); var stripData = Data(); stripData.Indices = [0, 1, 3, 2]; strip.AddSurfaceFromArrays(Mesh.PrimitiveType.TriangleStrip, stripData); strip.SurfaceSetMaterial(0, material);
        using var lines = new ArrayMesh(); var lineData = Data(4); lineData.Indices = [0, 1, 2, 3]; lines.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, lineData); lines.SurfaceSetMaterial(0, material);
        using var points = new ArrayMesh(); var pointData = Data(); pointData.Indices = []; points.AddSurfaceFromArrays(Mesh.PrimitiveType.Points, pointData); points.SurfaceSetMaterial(0, material);
        using var instances = new MultiMesh { Mesh = floats, InstanceCount = 1 }; instances.SetInstanceTransform2D(0, Transform.Identity);
        using var window = new Window { Size = new(256, 64), Visible = true };
        window.AddChild(new MeshInstance { Name = "CustomPath1", Mesh = mesh, Position = new(4, 4) }); window.AddChild(new MeshInstance { Name = "CustomPath2", Mesh = floats, Position = new(40, 4) }); window.AddChild(new MeshInstance { Name = "CustomPath3", Mesh = authored, Position = new(76, 4) }); window.AddChild(new MeshInstance { Name = "CustomPath4", Mesh = custom, Position = new(112, 4) }); window.AddChild(new MultiMeshInstance { Name = "CustomPath5", MultiMesh = instances, Position = new(148, 4) }); var skinned = new MeshInstance { Name = "CustomPath6", Mesh = skin, Position = new(184, 4) }; window.AddChild(skinned);
        window.AddChild(new MeshInstance { Name = "CustomPath7", Mesh = strip, Position = new(220, 4) }); window.AddChild(new MeshInstance { Name = "CustomPath8", Mesh = lines, Position = new(4, 36.5f) }); window.AddChild(new MeshInstance { Name = "CustomPath9", Mesh = points, Position = new(40.5f, 36.5f) }); var group = new CanvasGroup { Name = "CustomPath10", Position = new(76, 36) }; group.AddChild(new MeshInstance { Name = "CustomPath11", Mesh = floats }); window.AddChild(group);
        if (backend == "gpu") window.AddChild(new DirectionalLight { Name = "CustomLight", Color = Colors.White, Energy = 1 });
        RID palette = default; var frame = 0; long before = 0, allocated = 0; var packed = Patch(0); var floating = Patch(4);
        void Edit() { mesh.SurfaceUpdateAttributeRegion(0, 0, packed); floats.SurfaceUpdateAttributeRegion(0, 0, floating); authored.SurfaceUpdateAttributeRegion(0, 0, packed); skin.SurfaceUpdateAttributeRegion(0, 0, floating); strip.SurfaceUpdateAttributeRegion(0, 0, packed); lines.SurfaceUpdateAttributeRegion(0, 0, floating); points.SurfaceUpdateAttributeRegion(0, 0, packed); }
        window.Ready += _ => { window.Tree!.ProcessFrameStarted += _ => { if (frame >= 24) before = GC.GetAllocatedBytesForCurrentThread(); }; RenderingServer.SetDefaultClearColor(Colors.Black); Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Requested custom backend without fallback."); Check(RenderingServer.MeshSurfaceGetFormatAttributeStride(mesh.SurfaceGetFormat(0), 4) == packed.Length / 4 && RenderingServer.MeshSurfaceGetFormatAttributeStride(floats.SurfaceGetFormat(0), 4) == floating.Length / 4, "Public attribute strides include all variable custom layouts."); palette = RenderingServer.SkeletonCreate(); RenderingServer.SkeletonAllocateData(palette, 8); RenderingServer.SkeletonBoneSetTransform2D(palette, 5, new Transform(0, new(2, 0))); RenderingServer.CanvasItemAttachSkeleton(skinned.GetCanvasItem(), palette); RenderingServer.FramePreDraw += Pre; RenderingServer.FramePostDraw += Post; };
        void Pre() { if (frame == 16) GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); if (frame >= 24 && frame < 88) { Edit(); RenderingServer.SkeletonBoneSetTransform2D(palette, 5, new Transform(0, new(2 + frame % 2, 0))); } }
        void Post()
        {
            if (frame >= 24) allocated += GC.GetAllocatedBytesForCurrentThread() - before;
            if (frame is 3 or 5)
            {
                using var pixels = window.GetTexture().GetImage()!; foreach (var point in new Vector2i[] { new(12, 12), new(48, 12), new(84, 12), new(120, 12), new(156, 12), new(194, 12), new(228, 12), new(12, 36), new(40, 36), new(84, 44) }) { var c = pixels.GetPixel(point.X, point.Y); Check(backend == "compatibility" ? c.R > .99f && c.G > .99f && c.B > .99f : frame == 3 ? c.G > .99f && c.R < .01f && c.B < .01f : c.R > .99f && c.G < .01f && c.B > .99f, "Four independent custom payloads/default components across retained mesh paths " + language + "."); }
                if (Environment.GetEnvironmentVariable("ELECTRON2D_MESH_CUSTOM_OUTPUT") is { } directory) { Directory.CreateDirectory(directory); pixels.SavePNG(System.IO.Path.Combine(directory, language + "-retained-" + frame + ".png")); }
            }
            if (frame == 3) { Edit(); custom.Change(); }
            if (++frame == 152) { RenderingServer.FramePreDraw -= Pre; RenderingServer.FramePostDraw -= Post; RenderingServer.FreeRID(palette); window.Tree!.Quit(); }
        }
        Engine.Run(window); Check(allocated == 0, language + " custom channels allocated " + allocated); Console.WriteLine(language + " custom payload: 64 active +64 idle frames, 0 managed bytes.");
    }
    private sealed class Custom(Material material) : Mesh
    {
        private MeshSurfaceData _data = Data(4);
        internal void Change() { _data = Data(4, true); EmitChanged(); }
        protected override int OnGetSurfaceCount() => 1;
        protected override MeshSurfaceData OnSurfaceGetArrays(int surfaceIndex) => _data;
        protected override int OnSurfaceGetArrayLen(int surfaceIndex) => 4;
        protected override int OnSurfaceGetArrayIndexLen(int surfaceIndex) => 6;
        protected override PrimitiveType OnSurfaceGetPrimitiveType(int surfaceIndex) => PrimitiveType.Triangles;
        protected override ArrayFormat OnSurfaceGetFormat(int surfaceIndex) => ArrayFormat.Vertex | ArrayFormat.Normal | ArrayFormat.Tangent | ArrayFormat.TexUV | ArrayFormat.Index | ArrayFormat.Use2DVertices | MeshCustomCodec.Channels(_data);
        protected override Material? OnSurfaceGetMaterial(int surfaceIndex) => material;
        protected override void OnSurfaceSetMaterial(int surfaceIndex, Material? value) { material = value!; EmitChanged(); }
    }
}
