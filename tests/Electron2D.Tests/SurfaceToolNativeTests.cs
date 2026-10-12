using System.Runtime.InteropServices;
using Electron2D;
using static TextureArrayTests;

internal static class SurfaceToolNativeTests
{
    internal static void Run()
    {
        SurfaceToolTests.Run(); foreach (var language in new[] { "GLSL", "HLSL" }) { Device(language); Retained("gpu", language); }
        Retained("compatibility", "baseline");
        Console.WriteLine("SurfaceTool native indexed GLSL/HLSL output, retained GPU/compatibility topologies, UV2/eight-influence/instance pixels and warmed zero allocations passed.");
    }
    private static void Device(string language)
    {
        using var tool = new SurfaceTool(); tool.CreateFromArrays(SurfaceToolTests.Grid(12)); var original = tool.CommitToArrays(); tool.OptimizeIndicesForCache(); var optimized = tool.CommitToArrays(); Check(!original.Indices.SequenceEqual(optimized.Indices), "Native indexed consumer receives actual cache reordering.");
        using var device = RenderingServer.CreateLocalRenderingDevice(); using var module = RenderingDeviceGraphicsTests.Load("DeviceSurfaceTool" + language); var shader = device.ShaderCreateFromSPIRV(module);
        using var position = new RDVertexAttribute { Binding = 0, Location = 0, Stride = 24, Format = RenderingDevice.DataFormat.R32G32SFLOAT }; using var color = new RDVertexAttribute { Binding = 0, Location = 1, Offset = 8, Stride = 24, Format = RenderingDevice.DataFormat.R32G32B32A32SFLOAT }; var format = device.VertexFormatCreate([position, color]);
        var values = new float[optimized.Vertices.Length * 6]; for (var i = 0; i < optimized.Vertices.Length; i++) { var p = optimized.Vertices[i]; var c = optimized.Colors[i]; values[i * 6] = p.X; values[i * 6 + 1] = p.Y; values[i * 6 + 2] = c.R; values[i * 6 + 3] = c.G; values[i * 6 + 4] = c.B; values[i * 6 + 5] = c.A; }
        var vertices = device.VertexBufferCreate((uint)(values.Length * 4), MemoryMarshal.AsBytes(values.AsSpan())); var vertexArray = device.VertexArrayCreate((uint)optimized.Vertices.Length, format, [vertices]);
        RID Indices(int[] data) { var buffer = device.IndexBufferCreate((uint)data.Length, RenderingDevice.IndexBufferFormat.UInt32, MemoryMarshal.AsBytes(data.AsSpan())); return device.IndexArrayCreate(buffer, 0, (uint)data.Length); }
        var beforeIndices = Indices(original.Indices); var afterIndices = Indices(optimized.Indices);
        using var textureFormat = new RDTextureFormat { Width = 64, Height = 64, Format = RenderingDevice.DataFormat.R8G8B8A8UNORM, UsageBits = RenderingDevice.TextureUsageBits.ColorAttachment | RenderingDevice.TextureUsageBits.CanCopyFrom | RenderingDevice.TextureUsageBits.Sampling }; using var view = new RDTextureView(); var output = device.TextureCreate(textureFormat, view); var target = device.FramebufferCreate([output]);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), format, RenderingDevice.RenderPrimitive.Triangles, raster, blend); byte[] pixels = new byte[64 * 64 * 4]; Color[] clear = [Colors.Black];
        void Draw(RID indices) { var list = device.DrawListBegin(target, RenderingDevice.DrawFlags.ClearColorAll, clear); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindVertexArray(list, vertexArray); device.DrawListBindIndexArray(list, indices); device.DrawListDraw(list, true, 1); device.DrawListEnd(); device.TextureGetData(output, 0, pixels); }
        Draw(beforeIndices); var reference = (byte[])pixels.Clone(); Draw(afterIndices); Check(pixels.SequenceEqual(reference) && pixels[(32 * 64 + 32) * 4] == 255 && pixels[(32 * 64 + 32) * 4 + 1] == 0 && pixels[0] == 0, language + " exact indexed pixels survive cache optimization.");
        for (var i = 0; i < 24; i++) Draw(afterIndices); GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); var allocated = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) Draw(i % 2 == 0 ? beforeIndices : afterIndices); Check(GC.GetAllocatedBytesForCurrentThread() == allocated, language + " warmed indexed device replay/readback allocates zero managed bytes.");
        Save(pixels, 64, language + "-indexed"); Console.WriteLine(language + " SurfaceTool indexed device: 64 alternating frames/readbacks, 0 managed bytes.");
    }
    private static Shader Shader(string language)
    {
        using var input = typeof(SurfaceToolNativeTests).Assembly.GetManifestResourceStream("Electron2D.Device.MeshUV2" + language + ".frag.spv")!; using var stream = new MemoryStream(); input.CopyTo(stream); return Electron2D.Shader.CreateFromSPIRV(stream.ToArray());
    }
    private static void Retained(string backend, string language)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); using var shader = backend == "gpu" ? Shader(language) : null; using var material = shader is not null ? new ShaderMaterial { Shader = shader } : null;
        using var plain = SurfaceToolTests.Mesh(); using var skin = SurfaceToolTests.Mesh(true); using var instances = new MultiMesh { Mesh = plain, UseCustomData = true, InstanceCount = 2 }; instances.SetInstanceTransform2D(0, Transform.Identity); instances.SetInstanceTransform2D(1, new(0, new(28, 0))); instances.SetInstanceCustomData(0, Colors.Blue);
        using var window = new Window { Size = new(192, 88) }; var normal = new MeshInstance { Name = "Normal", Mesh = plain, Material = material, Position = new(8, 8) }; var skinned = new MeshInstance { Name = "Skin", Mesh = skin, Material = material, Position = new(40, 8) }; window.AddChild(normal); window.AddChild(skinned); window.AddChild(new MultiMeshInstance { MultiMesh = instances, Material = material, Position = new(80, 8) });
        var resources = new List<ArrayMesh>(); var x = 8;
        foreach (var topology in Enum.GetValues<Mesh.PrimitiveType>())
        {
            using var builder = new SurfaceTool(); builder.Begin(topology); builder.SetColor(Colors.White); builder.SetUV(new(.25f, 0)); builder.SetUV2(new(.75f, 0));
            Vector2[] vertices = topology switch { Mesh.PrimitiveType.Points => [new(.5f, .5f)], Mesh.PrimitiveType.Lines => [new(.5f, .5f), new(14.5f, .5f)], Mesh.PrimitiveType.LineStrip => [new(.5f, .5f), new(14.5f, .5f), new(14.5f, 14.5f)], Mesh.PrimitiveType.Triangles => [Vector2.Zero, new(16, 0), new(0, 16)], _ => [Vector2.Zero, new(16, 0), new(0, 16), new(16, 16)] }; foreach (var vertex in vertices) builder.AddVertex(vertex); builder.Index(); var mesh = builder.Commit(); resources.Add(mesh); window.AddChild(new MeshInstance { Name = topology.ToString(), Mesh = mesh, Material = material, Position = new(x, 40) }); x += 32;
        }
        var frame = 0; long before = 0, allocated = 0; RID palette = default;
        window.Ready += _ => { Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Requested SurfaceTool renderer starts without fallback."); RenderingServer.SetDefaultClearColor(Colors.Black); palette = RenderingServer.SkeletonCreate(); RenderingServer.SkeletonAllocateData(palette, 6); RenderingServer.SkeletonBoneSetTransform2D(palette, 5, new(0, new(2, 0))); RenderingServer.CanvasItemAttachSkeleton(skinned.GetCanvasItem(), palette); window.Tree!.ProcessFrameStarted += _ => { if (frame >= 24) before = GC.GetAllocatedBytesForCurrentThread(); }; RenderingServer.FramePreDraw += Pre; RenderingServer.FramePostDraw += Post; };
        void Pre() { if (frame == 16) GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); if (frame >= 24 && frame < 88) { normal.Position = new(8 + frame % 2, 8); instances.SetInstanceTransform2D(0, new(0, new(frame % 2, 0))); RenderingServer.SkeletonBoneSetTransform2D(palette, 5, new(0, new(2 + frame % 2, 0))); } }
        void Post()
        {
            if (frame >= 24) allocated += GC.GetAllocatedBytesForCurrentThread() - before;
            if (frame == 4)
            {
                using var pixels = window.GetTexture().GetImage()!; var r = backend == "gpu" ? .75f : 1; var g = backend == "gpu" ? .25f : 1;
                void Pixel(int px, int py, float blue = 0) { var c = pixels.GetPixel(px, py); Check(Math.Abs(c.R - r) < .012f && Math.Abs(c.G - g) < .012f && Math.Abs(c.B - (backend == "gpu" ? blue : 1)) < .012f, backend + "/" + language + " SurfaceTool pixel " + px + "," + py + ": " + c); }
                Pixel(12, 12); Pixel(44, 12); Pixel(84, 12, 1); Pixel(112, 12); Pixel(8, 40); Pixel(44, 40); Pixel(76, 40); Pixel(112, 44); Pixel(140, 44);
                var directory = Environment.GetEnvironmentVariable("ELECTRON2D_SURFACE_TOOL_OUTPUT"); if (directory is not null) { Directory.CreateDirectory(directory); pixels.SavePNG(System.IO.Path.Combine(directory, backend + "-" + language + ".png")); }
            }
            if (++frame == 152) { RenderingServer.FramePreDraw -= Pre; RenderingServer.FramePostDraw -= Post; RenderingServer.FreeRID(palette); window.Tree!.Quit(); }
        }
        try { Engine.Run(window); Check(allocated == 0, backend + "/" + language + " SurfaceTool replay allocated " + allocated); Console.WriteLine(backend + "/" + language + " SurfaceTool: 64 active + 64 idle frames, 0 managed bytes."); }
        finally { foreach (var mesh in resources) mesh.Dispose(); }
    }
    private static void Save(byte[] pixels, int size, string name)
    {
        var directory = Environment.GetEnvironmentVariable("ELECTRON2D_SURFACE_TOOL_OUTPUT"); if (directory is null) return; Directory.CreateDirectory(directory); using var image = Image.CreateFromData(size, size, false, Image.Format.Rgba8, pixels); image.SavePNG(System.IO.Path.Combine(directory, name + ".png"));
    }
}
