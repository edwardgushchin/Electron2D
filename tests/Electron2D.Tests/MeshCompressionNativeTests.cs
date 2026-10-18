using System.Buffers.Binary;
using Electron2D;
using static TextureArrayTests;

internal static class MeshCompressionNativeTests
{
    internal static void Run()
    {
        MeshCompressionTests.Run(); foreach (var language in new[] { "GLSL", "HLSL" }) Retained(language); Retained("baseline", "compatibility");
        Console.WriteLine("Compressed mesh pixels and 64 active/64 idle complete frames passed on GLSL, HLSL and compatibility.");
    }
    private static void Retained(string language, string backend = "gpu")
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var shader = backend == "gpu" ? Shader(language) : null; using var material = shader is null ? null : new ShaderMaterial { Shader = shader };
        using var mesh = MeshCompressionTests.Mesh(); using var shaped = MeshCompressionTests.Mesh(true); mesh.SurfaceSetMaterial(0, material); shaped.SurfaceSetMaterial(0, material);
        var skinData = mesh.SurfaceGetArrays(0); skinData.Bones = new int[32]; skinData.Weights = new float[32]; for (var i = 0; i < 4; i++) { skinData.Bones[i * 8 + 7] = 7; skinData.Weights[i * 8 + 7] = 1; }
        using var skin = new ArrayMesh(); skin.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, skinData, flags: Mesh.ArrayFormat.CompressAttributes | Mesh.ArrayFormat.Use8BoneWeights); skin.SurfaceSetMaterial(0, material);
        using var multi = new MultiMesh { Mesh = mesh, InstanceCount = 2 }; multi.SetInstanceTransform2D(0, Transform.Identity); multi.SetInstanceTransform2D(1, new(new Vector2(.5f, 0), new Vector2(0, .5f), new(20, 0)));
        using var custom = new MeshCompressionTests.Custom();
        using var window = new Window { Size = new(256, 64), Visible = true };
        var node = new MeshInstance { Name = "Base", Mesh = mesh, Position = new(4, 4) }; var morph = new MeshInstance { Name = "Morph", Mesh = shaped, Position = new(40, 4) }; morph.SetBlendShapeValue(0, .5f); var skinned = new MeshInstance { Name = "Skin", Mesh = skin, Position = new(80, 4) };
        window.AddChild(new MeshInstance { Name = "Custom", Mesh = custom, Material = material, Position = new(232, 4) }); window.AddChild(node); window.AddChild(morph); window.AddChild(skinned); window.AddChild(new MultiMeshInstance { Name = "Instances", MultiMesh = multi, Position = new(116, 4) });
        var group = new CanvasGroup { Name = "Group", Position = new(160, 4) }; group.AddChild(new MeshInstance { Mesh = mesh }); window.AddChild(group);
        var frame = 0; long before = 0, allocated = 0; RID palette = default, owned = default; var patch = new byte[2];
        window.Ready += _ =>
        {
            window.Tree!.ProcessFrameStarted += _ => { if (frame >= 24) before = GC.GetAllocatedBytesForCurrentThread(); }; RenderingServer.SetDefaultClearColor(Colors.Black); Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Compression backend without fallback.");
            palette = RenderingServer.SkeletonCreate(); RenderingServer.SkeletonAllocateData(palette, 8); RenderingServer.SkeletonBoneSetTransform2D(palette, 7, new(0, new(4, 0))); RenderingServer.CanvasItemAttachSkeleton(skinned.GetCanvasItem(), palette);
            owned = RenderingServer.MeshCreate(); RenderingServer.MeshAddSurfaceFromArrays(owned, Mesh.PrimitiveType.Triangles, mesh.SurfaceGetArrays(0), Mesh.ArrayFormat.CompressAttributes, lods: mesh.SurfaceGetLODs(0)); RenderingServer.MeshSurfaceSetMaterial(owned, 0, material); window.AddChild(new Owned(owned) { Name = "Owned", Position = new(200, 4) });
            var format = mesh.SurfaceGetFormat(0); Check(RenderingServer.MeshSurfaceGetFormatVertexStride(format, 4) == 8 && RenderingServer.MeshSurfaceGetFormatNormalTangentStride(format, 4) == 6 && RenderingServer.MeshSurfaceGetFormatAttributeStride(format, 4) == 40, "Public compressed position/frame/attribute strides.");
            RenderingServer.FramePreDraw += Pre; RenderingServer.FramePostDraw += Post;
        };
        void Pre()
        {
            if (frame == 16) GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
            if (frame >= 6 && frame < 88) { BinaryPrimitives.WriteUInt16LittleEndian(patch, (ushort)(frame % 2 == 0 ? 32767 : 65535)); mesh.SurfaceUpdateAttributeRegion(0, 4, patch); RenderingServer.MeshSurfaceUpdateAttributeRegion(owned, 0, 4, patch); morph.SetBlendShapeValue(0, frame % 2 == 0 ? .5f : 1); }
        }
        void Post()
        {
            if (frame >= 24) allocated += GC.GetAllocatedBytesForCurrentThread() - before;
            if (frame == 3)
            {
                using var pixels = window.GetTexture().GetImage()!; foreach (var p in new Vector2i[] { new(16, 12), new(56, 12), new(96, 12), new(128, 12), new(142, 6), new(172, 12), new(212, 12), new(244, 12) }) { var c = pixels.GetPixel(p.X, p.Y); Check(backend == "compatibility" ? c.R > .99f && c.G > .99f && c.B > .99f : c.R < .01f && c.G > .99f && c.B < .01f, "Compressed channels/LOD/morph/eighth-slot skin/instances/group/owned pixels: " + language + " " + p + " " + c); }
                if (Environment.GetEnvironmentVariable("ELECTRON2D_MESH_COMPRESSION_OUTPUT") is { } output) { Directory.CreateDirectory(output); pixels.SavePNG(System.IO.Path.Combine(output, language + ".png")); }
            }
            if (++frame == 152) { RenderingServer.FramePreDraw -= Pre; RenderingServer.FramePostDraw -= Post; RenderingServer.FreeRID(palette); RenderingServer.FreeRID(owned); window.Tree!.Quit(); }
        }
        Engine.Run(window); Check(allocated == 0, language + " compressed complete frames allocated " + allocated); Console.WriteLine(language + ": 64 active +64 idle compressed frames, 0 managed bytes.");
    }
    private static Shader Shader(string language)
    {
        using var input = typeof(MeshCompressionNativeTests).Assembly.GetManifestResourceStream("Electron2D.Device.MeshCompression" + language + ".frag.spv")!; using var bytes = new MemoryStream(); input.CopyTo(bytes); return Electron2D.Shader.CreateFromSPIRV(bytes.GetBuffer().AsSpan(0, (int)bytes.Length));
    }
    private sealed class Owned(RID mesh) : Entity { protected override void OnDraw() => DrawMesh(mesh); }
}
