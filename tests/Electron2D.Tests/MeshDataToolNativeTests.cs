using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Electron2D;
using static TextureArrayTests;

internal static class MeshDataToolNativeTests
{
    internal static void Run()
    {
        MeshDataToolTests.Run(); foreach (var language in new[] { "GLSL", "HLSL" }) { Device(language); Retained("gpu", language); }
        Retained("compatibility", "baseline");
        Console.WriteLine("MeshDataTool indexed device, edited retained UV2/skin/instance pixels and warmed active/idle frames passed.");
    }
    private static void Device(string language)
    {
        using var source = new ArrayMesh(); source.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, SurfaceToolTests.Grid(8)); using var tool = new MeshDataTool(); tool.CreateFromSurface(source, 0);
        for (var i = 0; i < tool.GetVertexCount(); i++) { tool.SetVertexColor(i, Colors.Green); tool.SetVertex(i, tool.GetVertex(i) * .5f); }
        using var output = new ArrayMesh(); tool.CommitToSurface(output); var data = output.SurfaceGetArrays(0);
        using var device = RenderingServer.CreateLocalRenderingDevice(); using var module = RenderingDeviceGraphicsTests.Load("DeviceSurfaceTool" + language); var shader = device.ShaderCreateFromSPIRV(module);
        using var position = new RDVertexAttribute { Binding = 0, Location = 0, Stride = 24, Format = RenderingDevice.DataFormat.R32G32SFLOAT }; using var color = new RDVertexAttribute { Binding = 0, Location = 1, Offset = 8, Stride = 24, Format = RenderingDevice.DataFormat.R32G32B32A32SFLOAT }; var format = device.VertexFormatCreate([position, color]);
        var values = new float[data.Vertices.Length * 6]; for (var i = 0; i < data.Vertices.Length; i++) { values[i * 6] = data.Vertices[i].X; values[i * 6 + 1] = data.Vertices[i].Y; values[i * 6 + 2] = data.Colors[i].R; values[i * 6 + 3] = data.Colors[i].G; values[i * 6 + 4] = data.Colors[i].B; values[i * 6 + 5] = data.Colors[i].A; }
        var vertices = device.VertexBufferCreate((uint)(values.Length * 4), MemoryMarshal.AsBytes(values.AsSpan())); var va = device.VertexArrayCreate((uint)data.Vertices.Length, format, [vertices]); var index = device.IndexBufferCreate((uint)data.Indices.Length, RenderingDevice.IndexBufferFormat.UInt32, MemoryMarshal.AsBytes(data.Indices.AsSpan())); var ia = device.IndexArrayCreate(index, 0, (uint)data.Indices.Length);
        using var textureFormat = new RDTextureFormat { Width = 64, Height = 64, Format = RenderingDevice.DataFormat.R8G8B8A8UNORM, UsageBits = RenderingDevice.TextureUsageBits.ColorAttachment | RenderingDevice.TextureUsageBits.CanCopyFrom | RenderingDevice.TextureUsageBits.Sampling }; using var view = new RDTextureView(); var image = device.TextureCreate(textureFormat, view); var target = device.FramebufferCreate([image]); using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), format, RenderingDevice.RenderPrimitive.Triangles, raster, blend); var pixels = new byte[64 * 64 * 4]; Color[] clear = [Colors.Black];
        void Draw() { var list = device.DrawListBegin(target, RenderingDevice.DrawFlags.ClearColorAll, clear); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindVertexArray(list, va); device.DrawListBindIndexArray(list, ia); device.DrawListDraw(list, true, 1); device.DrawListEnd(); device.TextureGetData(image, 0, pixels); }
        Draw(); Check(pixels[(32 * 64 + 32) * 4] == 0 && pixels[(32 * 64 + 32) * 4 + 1] == 255 && pixels[(32 * 64 + 8) * 4 + 1] == 0, "Edited color and half-size vertex geometry actually reaches indexed " + language + " drawing.");
        for (var i = 0; i < 24; i++) Draw(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) Draw(); Check(before == GC.GetAllocatedBytesForCurrentThread(), "Warmed topology device replay/readback allocates zero managed bytes.");
        var directory = Environment.GetEnvironmentVariable("ELECTRON2D_MESH_DATA_TOOL_OUTPUT"); if (directory is not null) { Directory.CreateDirectory(directory); using var screenshot = Image.CreateFromData(64, 64, false, Image.Format.Rgba8, pixels); screenshot.SavePNG(System.IO.Path.Combine(directory, language + "-indexed.png")); }
        Console.WriteLine(language + " topology indexed replay: 64 frames/readbacks, 0 managed bytes.");
    }
    private static Shader Shader(string language)
    {
        using var input = typeof(MeshDataToolNativeTests).Assembly.GetManifestResourceStream("Electron2D.Device.MeshUV2" + language + ".frag.spv")!; using var bytes = new MemoryStream(); input.CopyTo(bytes); return Electron2D.Shader.CreateFromSPIRV(bytes.ToArray());
    }
    private static void Retained(string backend, string language)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); using var shader = backend == "gpu" ? Shader(language) : null; using var material = shader is not null ? new ShaderMaterial { Shader = shader } : null;
        using var source = SurfaceToolTests.Mesh(true); using var tool = new MeshDataTool(); tool.CreateFromSurface(source, 0); tool.SetMaterial(material); for (var i = 0; i < tool.GetVertexCount(); i++) { tool.SetVertex(i, tool.GetVertex(i) + new Vector2(2, 0)); tool.SetVertexUV2(i, new(.5f, 0)); }
        using var mesh = new ArrayMesh(); tool.CommitToSurface(mesh);
        using var instances = new MultiMesh { Mesh = mesh, UseCustomData = true, InstanceCount = 1 }; instances.SetInstanceTransform2D(0, Transform.Identity); instances.SetInstanceCustomData(0, Colors.Blue); using var window = new Window { Size = new(160, 80) }; var node = new MeshInstance { Mesh = mesh, Position = new(8, 8) }; window.AddChild(node); var multi = new MultiMeshInstance { MultiMesh = instances, Position = new(40, 8) }; window.AddChild(multi);
        RID palette = default; var frame = 0; long before = 0, allocated = 0; var patch = new byte[80]; for (var i = 0; i < 4; i++) { patch[i * 20] = patch[i * 20 + 1] = patch[i * 20 + 2] = patch[i * 20 + 3] = 255; BinaryPrimitives.WriteSingleLittleEndian(patch.AsSpan(i * 20 + 4), .25f); BinaryPrimitives.WriteSingleLittleEndian(patch.AsSpan(i * 20 + 12), .25f); }
        window.Ready += _ => { RenderingServer.SetDefaultClearColor(Colors.Black); Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Requested topology backend initialized without fallback."); palette = RenderingServer.SkeletonCreate(); RenderingServer.SkeletonAllocateData(palette, 6); RenderingServer.SkeletonBoneSetTransform2D(palette, 5, new(0, new(2, 0))); RenderingServer.CanvasItemAttachSkeleton(node.GetCanvasItem(), palette); RenderingServer.CanvasItemAttachSkeleton(multi.GetCanvasItem(), palette); window.Tree!.ProcessFrameStarted += _ => { if (frame >= 24) before = GC.GetAllocatedBytesForCurrentThread(); }; RenderingServer.FramePreDraw += Pre; RenderingServer.FramePostDraw += Post; };
        void Pre() { if (frame == 16) GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); if (frame >= 24 && frame < 88) { tool.SetVertexUV2(0, new(frame % 2 * .25f, 0)); tool.SetVertex(0, new(2 + frame % 2, 0)); mesh.SurfaceUpdateAttributeRegion(0, 0, patch); RenderingServer.SkeletonBoneSetTransform2D(palette, 5, new(0, new(2 + frame % 2, 0))); } }
        void Post()
        {
            if (frame >= 24) allocated += GC.GetAllocatedBytesForCurrentThread() - before;
            if (frame is 3 or 5)
            {
                using var pixels = window.GetTexture().GetImage()!; var expected = backend == "gpu" ? (frame == 3 ? .5f : .25f) : 1f;
                void Pixel(int x, float blue) { var c = pixels.GetPixel(x, 12); Check(Math.Abs(c.R - expected) < .012f && Math.Abs(c.G - (backend == "gpu" ? .25f : 1)) < .012f && Math.Abs(c.B - (backend == "gpu" ? blue : 1)) < .012f, backend + "/" + language + " edited topology pixel " + x + ": " + c); }
                Pixel(16, 0); Pixel(48, 1); Check(pixels.GetPixel(10, 12).R == 0, "Copied vertex offset plus eighth influence moves the actual silhouette.");
                var directory = Environment.GetEnvironmentVariable("ELECTRON2D_MESH_DATA_TOOL_OUTPUT"); if (directory is not null) { Directory.CreateDirectory(directory); pixels.SavePNG(System.IO.Path.Combine(directory, backend + "-" + language + "-" + frame + ".png")); }
            }
            if (frame == 3) { for (var i = 0; i < tool.GetVertexCount(); i++) tool.SetVertexUV2(i, new(.25f, 0)); mesh.ClearSurfaces(); tool.CommitToSurface(mesh); }
            if (++frame == 152) { RenderingServer.FramePreDraw -= Pre; RenderingServer.FramePostDraw -= Post; RenderingServer.FreeRID(palette); window.Tree!.Quit(); }
        }
        Engine.Run(window); Check(allocated == 0, backend + "/" + language + " topology edits/replay allocated " + allocated); Console.WriteLine(backend + "/" + language + " topology: 64 active +64 idle frames, 0 managed bytes.");
    }
}
