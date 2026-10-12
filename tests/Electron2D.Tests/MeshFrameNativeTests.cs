using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Electron2D;
using static TextureArrayTests;

internal static class MeshFrameNativeTests
{
    internal static void Run()
    {
        MeshFrameTests.Run(); foreach (var language in new[] { "GLSL", "HLSL" }) { Device(language); Retained(language); }
        Retained("baseline", "compatibility");
        Console.WriteLine("Mesh frames: indexed device and ordinary/immediate/custom/hardware-instance/skin shader pixels passed.");
    }
    private static void Save(Image pixels, string name)
    {
        var directory = Environment.GetEnvironmentVariable("ELECTRON2D_MESH_FRAME_OUTPUT"); if (directory is not null) { Directory.CreateDirectory(directory); pixels.SavePNG(System.IO.Path.Combine(directory, name + ".png")); }
    }
    private static void Device(string language)
    {
        var data = MeshFrameTests.Data(); for (var i = 0; i < data.Vertices.Length; i++) data.Vertices[i] = data.Vertices[i] / 16 - Vector2.One * .5f;
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, data); data = mesh.SurfaceGetArrays(0);
        using var device = RenderingServer.CreateLocalRenderingDevice(); using var module = RenderingDeviceGraphicsTests.Load("DeviceMeshFrame" + language); var shader = device.ShaderCreateFromSPIRV(module);
        using var position = new RDVertexAttribute { Binding = 0, Location = 0, Stride = 36, Format = RenderingDevice.DataFormat.R32G32SFLOAT }; using var normal = new RDVertexAttribute { Binding = 0, Location = 1, Offset = 8, Stride = 36, Format = RenderingDevice.DataFormat.R32G32B32SFLOAT }; using var tangent = new RDVertexAttribute { Binding = 0, Location = 2, Offset = 20, Stride = 36, Format = RenderingDevice.DataFormat.R32G32B32A32SFLOAT }; var format = device.VertexFormatCreate([position, normal, tangent]);
        var values = new float[data.Vertices.Length * 9]; for (var i = 0; i < data.Vertices.Length; i++) { values[i * 9] = data.Vertices[i].X; values[i * 9 + 1] = data.Vertices[i].Y; values[i * 9 + 2] = data.Normals[i].X; values[i * 9 + 3] = data.Normals[i].Y; values[i * 9 + 4] = data.Normals[i].Z; values[i * 9 + 5] = data.Tangents[i].X; values[i * 9 + 6] = data.Tangents[i].Y; values[i * 9 + 7] = data.Tangents[i].Z; values[i * 9 + 8] = data.Tangents[i].W; }
        var buffer = device.VertexBufferCreate((uint)(values.Length * 4), MemoryMarshal.AsBytes(values.AsSpan())); var vertices = device.VertexArrayCreate((uint)data.Vertices.Length, format, [buffer]); var index = device.IndexBufferCreate((uint)data.Indices.Length, RenderingDevice.IndexBufferFormat.UInt32, MemoryMarshal.AsBytes(data.Indices.AsSpan())); var indices = device.IndexArrayCreate(index, 0, (uint)data.Indices.Length);
        using var textureFormat = new RDTextureFormat { Width = 64, Height = 64, Format = RenderingDevice.DataFormat.R8G8B8A8UNORM, UsageBits = RenderingDevice.TextureUsageBits.ColorAttachment | RenderingDevice.TextureUsageBits.CanCopyFrom | RenderingDevice.TextureUsageBits.Sampling }; using var view = new RDTextureView(); var image = device.TextureCreate(textureFormat, view); var target = device.FramebufferCreate([image]); using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), format, RenderingDevice.RenderPrimitive.Triangles, raster, blend); var pixels = new byte[64 * 64 * 4]; Color[] clear = [Colors.Black];
        void Draw() { var list = device.DrawListBegin(target, RenderingDevice.DrawFlags.ClearColorAll, clear); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindVertexArray(list, vertices); device.DrawListBindIndexArray(list, indices); device.DrawListDraw(list, true, 1); device.DrawListEnd(); device.TextureGetData(image, 0, pixels); }
        Draw(); Check(pixels[(32 * 64 + 32) * 4] == 0 && pixels[(32 * 64 + 32) * 4 + 1] == 255 && pixels[(32 * 64 + 8) * 4 + 1] == 0, "Packed frame data reaches actual indexed " + language + " pixels.");
        for (var i = 0; i < 24; i++) Draw(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) Draw(); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed indexed normal/tangent replay allocates zero managed bytes."); using var screenshot = Image.CreateFromData(64, 64, false, Image.Format.Rgba8, pixels); Save(screenshot, language + "-indexed");
        Console.WriteLine(language + " indexed frame payload: 64 draws/readbacks, 0 managed bytes.");
    }
    private static Shader Shader(string language)
    {
        using var input = typeof(MeshFrameNativeTests).Assembly.GetManifestResourceStream("Electron2D.Device.MeshFrame" + language + ".frag.spv")!; using var bytes = new MemoryStream(); input.CopyTo(bytes); return Electron2D.Shader.CreateFromSPIRV(bytes.GetBuffer().AsSpan(0, (int)bytes.Length));
    }
    private static void Immediate(ImmediateMesh mesh, bool changed)
    {
        mesh.ClearSurfaces(); mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles); mesh.SurfaceSetNormal(new(0, 0, changed ? 1 : -1)); mesh.SurfaceSetTangent(new(changed ? -1 : 1, 0, 0, changed ? -1 : 1)); var data = MeshFrameTests.Data(); foreach (var index in data.Indices) mesh.SurfaceAddVertex2D(data.Vertices[index]); mesh.SurfaceEnd();
    }
    private static void Retained(string language, string backend = "gpu")
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); using var shader = backend == "gpu" ? Shader(language) : null; using Material material = shader is null ? new CanvasItemMaterial() : new ShaderMaterial { Shader = shader };
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, MeshFrameTests.Data()); mesh.SurfaceSetMaterial(0, material); using var immediate = new ImmediateMesh(); Immediate(immediate, false); immediate.SurfaceSetMaterial(0, material); using var custom = new Custom(material); using var skin = new ArrayMesh(); var skinData = MeshFrameTests.Data(); skinData.Bones = new int[32]; skinData.Weights = new float[32]; for (var i = 0; i < 4; i++) { skinData.Bones[i * 8 + 5] = 5; skinData.Weights[i * 8 + 5] = 1; }
        skin.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, skinData, flags: Mesh.ArrayFormat.Use8BoneWeights); skin.SurfaceSetMaterial(0, material);
        using var strip = new ArrayMesh(); var stripData = MeshFrameTests.Data(); stripData.Indices = [0, 1, 3, 2]; strip.AddSurfaceFromArrays(Mesh.PrimitiveType.TriangleStrip, stripData); strip.SurfaceSetMaterial(0, material);
        using var lines = new ArrayMesh(); var lineData = MeshFrameTests.Data(); lineData.Indices = [0, 1, 2, 3]; lines.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, lineData); lines.SurfaceSetMaterial(0, material);
        using var points = new ArrayMesh(); var pointData = MeshFrameTests.Data(); pointData.Indices = []; points.AddSurfaceFromArrays(Mesh.PrimitiveType.Points, pointData); points.SurfaceSetMaterial(0, material);
        using var builder = new SurfaceTool(); builder.CreateFromArrays(MeshFrameTests.Data()); builder.GenerateNormals(); builder.GenerateTangents(); builder.SetMaterial(material); using var authored = builder.Commit();
        using var instances = new MultiMesh { Mesh = mesh, InstanceCount = 1 }; instances.SetInstanceTransform2D(0, Transform.Identity);
        using var window = new Window { Size = new(256, 64), MinSize = new(256, 64), MaxSize = new(256, 64), Visible = true };
        window.AddChild(new MeshInstance { Name = "Array", Mesh = mesh, Position = new(4, 4) }); window.AddChild(new MeshInstance { Name = "Immediate", Mesh = immediate, Position = new(40, 4) }); window.AddChild(new MeshInstance { Name = "Authored", Mesh = authored, Position = new(76, 4) }); window.AddChild(new MeshInstance { Name = "Custom", Mesh = custom, Position = new(112, 4) }); window.AddChild(new MultiMeshInstance { Name = "Instances", MultiMesh = instances, Position = new(148, 4) }); var skinned = new MeshInstance { Name = "Skinned", Mesh = skin, Position = new(184, 4) }; window.AddChild(skinned);
        window.AddChild(new MeshInstance { Name = "Strip", Mesh = strip, Position = new(220, 4) }); window.AddChild(new MeshInstance { Name = "Lines", Mesh = lines, Position = new(4, 36.5f) }); window.AddChild(new MeshInstance { Name = "Points", Mesh = points, Position = new(40.5f, 36.5f) }); var group = new CanvasGroup { Name = "Group", Position = new(76, 36) }; group.AddChild(new MeshInstance { Name = "GroupMesh", Mesh = mesh }); window.AddChild(group);
        RID palette = default; var frame = 0; long before = 0, allocated = 0;
        var patch = new byte[32]; for (var i = 0; i < 4; i++) { BinaryPrimitives.WriteUInt32LittleEndian(patch.AsSpan(i * 8), 0x7fff7fffu); BinaryPrimitives.WriteUInt32LittleEndian(patch.AsSpan(i * 8 + 4), 0x3fff0000u); }
        window.Ready += _ => { window.Tree!.ProcessFrameStarted += _ => { if (frame >= 24) before = GC.GetAllocatedBytesForCurrentThread(); }; RenderingServer.SetDefaultClearColor(Colors.Black); Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Requested frame GPU without fallback."); Check(RenderingServer.MeshSurfaceGetFormatNormalTangentStride(mesh.SurfaceGetFormat(0), 4) == 8 && RenderingServer.MeshSurfaceGetFormatNormalTangentStride(Mesh.ArrayFormat.Normal, 4) == 4, "Shared public normal/tangent stride executes."); palette = RenderingServer.SkeletonCreate(); RenderingServer.SkeletonAllocateData(palette, 8); RenderingServer.SkeletonBoneSetTransform2D(palette, 5, new Transform(0, new(2, 0))); RenderingServer.CanvasItemAttachSkeleton(skinned.GetCanvasItem(), palette); RenderingServer.FramePreDraw += Pre; RenderingServer.FramePostDraw += Post; };
        void Pre() { if (frame == 16) GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); if (frame >= 24 && frame < 88) { mesh.SurfaceUpdateVertexRegion(0, 32, patch); authored.SurfaceUpdateVertexRegion(0, 32, patch); skin.SurfaceUpdateVertexRegion(0, 32, patch); strip.SurfaceUpdateVertexRegion(0, 32, patch); lines.SurfaceUpdateVertexRegion(0, 32, patch); points.SurfaceUpdateVertexRegion(0, 32, patch); RenderingServer.SkeletonBoneSetTransform2D(palette, 5, new Transform(0, new(2 + frame % 2, 0))); } }
        void Post()
        {
            if (frame >= 24) allocated += GC.GetAllocatedBytesForCurrentThread() - before;
            if (frame is 3 or 5)
            {
                using var pixels = window.GetTexture().GetImage()!; foreach (var point in new Vector2i[] { new(12, 12), new(48, 12), new(84, 12), new(120, 12), new(156, 12), new(194, 12), new(228, 12), new(12, 36), new(40, 36), new(84, 44) }) { var c = pixels.GetPixel(point.X, point.Y); Check(backend == "compatibility" ? c.R > .99f && c.G > .99f && c.B > .99f : frame == 3 ? c.G > .99f && c.R < .01f && c.B < .01f : c.R > .99f && c.G < .01f && c.B > .99f, "Actual local normal/tangent/orientation across mesh producers, instances, skin, strips, points/lines and group " + language + " mesh."); }
                Save(pixels, language + "-retained-" + frame);
            }
            if (frame == 3) { mesh.SurfaceUpdateVertexRegion(0, 32, patch); authored.SurfaceUpdateVertexRegion(0, 32, patch); skin.SurfaceUpdateVertexRegion(0, 32, patch); strip.SurfaceUpdateVertexRegion(0, 32, patch); lines.SurfaceUpdateVertexRegion(0, 32, patch); points.SurfaceUpdateVertexRegion(0, 32, patch); Immediate(immediate, true); immediate.SurfaceSetMaterial(0, material); custom.Change(); }
            if (++frame == 152) { RenderingServer.FramePreDraw -= Pre; RenderingServer.FramePostDraw -= Post; RenderingServer.FreeRID(palette); window.Tree!.Quit(); }
        }
        Engine.Run(window); Check(allocated == 0, language + " retained normal/tangent payload allocated " + allocated); Console.WriteLine(language + " retained frame payload: 64 active +64 idle frames, 0 managed bytes.");
    }
    private sealed class Custom(Material material) : Mesh
    {
        private readonly MeshSurfaceData _data = MeshFrameTests.Data();
        internal void Change() { Array.Fill(_data.Normals, new(0, 0, 1)); Array.Fill(_data.Tangents, new(-1, 0, 0, -1)); EmitChanged(); }
        protected override int OnGetSurfaceCount() => 1;
        protected override MeshSurfaceData OnSurfaceGetArrays(int surfaceIndex) => _data;
        protected override int OnSurfaceGetArrayLen(int surfaceIndex) => 4;
        protected override int OnSurfaceGetArrayIndexLen(int surfaceIndex) => 6;
        protected override PrimitiveType OnSurfaceGetPrimitiveType(int surfaceIndex) => PrimitiveType.Triangles;
        protected override ArrayFormat OnSurfaceGetFormat(int surfaceIndex) => ArrayFormat.Vertex | ArrayFormat.Normal | ArrayFormat.Tangent | ArrayFormat.TexUV | ArrayFormat.Index | ArrayFormat.Use2DVertices;
        protected override Material? OnSurfaceGetMaterial(int surfaceIndex) => material;
        protected override void OnSurfaceSetMaterial(int surfaceIndex, Material? value) { material = value!; EmitChanged(); }
    }
}
