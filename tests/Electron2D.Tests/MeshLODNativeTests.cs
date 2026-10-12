using Electron2D;
using static TextureArrayTests;

internal static class MeshLODNativeTests
{
    internal static void Run()
    {
        MeshLODTests.Run(); foreach (var language in new[] { "GLSL", "HLSL" }) Retained(language); Retained("baseline", "compatibility");
        Console.WriteLine("LOD native pixels: scale-selected geometry, shared per-instance levels, morph/skin/group/server drawing passed.");
    }
    private static void Retained(string language, string backend = "gpu")
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var shader = backend == "gpu" ? Shader(language) : null; using var material = shader is null ? null : new ShaderMaterial { Shader = shader };
        using var mesh = MeshLODTests.Mesh(); using var shaped = MeshLODTests.Mesh(true); mesh.SurfaceSetMaterial(0, material); shaped.SurfaceSetMaterial(0, material);
        var data = MeshLODTests.Data(); data.Bones = new int[32]; data.Weights = new float[32]; for (var i = 0; i < 8; i++) data.Weights[i * 4] = 1;
        using var skin = new ArrayMesh(); skin.AddSurfaceFromArrays(Electron2D.Mesh.PrimitiveType.Triangles, data, lods: MeshLODTests.Levels()); skin.SurfaceSetMaterial(0, material);
        using var multi = new MultiMesh { Mesh = mesh, InstanceCount = 2 }; multi.SetInstanceTransform2D(0, new(new Vector2(.5f, 0), new Vector2(0, .5f), Vector2.Zero)); multi.SetInstanceTransform2D(1, new(0, new(48, 0)));
        using var window = new Window { Size = new(320, 160), MinSize = new(320, 160), MaxSize = new(320, 160), Visible = true };
        var node = new MeshInstance { Mesh = mesh, Name = "Mesh", Position = new(4, 4) }; var morph = new MeshInstance { Mesh = shaped, Name = "Morph", Position = new(100, 4), Scale = new(.5f, .5f) }; morph.SetBlendShapeValue(0, 1); var skinned = new MeshInstance { Mesh = skin, Name = "Skin", Position = new(140, 4), Scale = new(.5f, .5f) }; var instances = new MultiMeshInstance { MultiMesh = multi, Name = "Instances", Position = new(4, 56) };
        foreach (var item in new CanvasItem[] { node, morph, skinned, instances }) window.AddChild(item);
        var group = new CanvasGroup { Name = "Group", Position = new(180, 4) }; group.AddChild(new MeshInstance { Mesh = mesh, Scale = new(.5f, .5f) }); window.AddChild(group);
        var frame = 0; var deltas = new long[152]; var preDeltas = new long[152]; long before = 0, allocated = 0; RID palette = default, owned = default;
        window.Ready += _ =>
        {
            window.Tree!.ProcessFrameStarted += _ => { if (frame >= 24) before = GC.GetAllocatedBytesForCurrentThread(); }; RenderingServer.SetDefaultClearColor(Colors.Black); Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Requested LOD backend without fallback.");
            palette = RenderingServer.SkeletonCreate(); RenderingServer.SkeletonAllocateData(palette, 1); RenderingServer.SkeletonBoneSetTransform2D(palette, 0, new(0, new(4, 0))); RenderingServer.CanvasItemAttachSkeleton(skinned.GetCanvasItem(), palette);
            owned = RenderingServer.MeshCreate(); RenderingServer.MeshAddSurfaceFromArrays(owned, Electron2D.Mesh.PrimitiveType.Triangles, MeshLODTests.Data(), lods: MeshLODTests.Levels()); RenderingServer.MeshSurfaceSetMaterial(owned, 0, material); Check(RenderingServer.MeshSurfaceGetLODs(owned, 0)[2].Length == 6 && RenderingServer.MeshSurfaceGetLODs(mesh.GetRID(), 0)[4].Length == 3, "Owned and borrowed LOD queries."); var copy = RenderingServer.MeshSurfaceGetLODs(owned, 0); copy[2][0] = 7; Check(RenderingServer.MeshSurfaceGetLODs(owned, 0)[2][0] == 0, "Service queries copy indices."); window.AddChild(new Owned(owned) { Name = "Owned", Position = new(220, 4), Scale = new(.5f, .5f) }); RenderingServer.FramePreDraw += Pre; RenderingServer.FramePostDraw += Post;
        };
        void Pre()
        {
            var preBefore = GC.GetAllocatedBytesForCurrentThread();
            if (frame == 16) GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
            // Warm every active LOD/capacity combination before the measured bracket.
            if (frame >= 6 && frame < 88) { var scale = frame % 2 == 0 ? .5f : 1; node.Scale = new(scale, scale); multi.SetInstanceTransform2D(0, new(new Vector2(scale, 0), new Vector2(0, scale), Vector2.Zero)); morph.SetBlendShapeValue(0, frame % 2); }
            preDeltas[frame] = GC.GetAllocatedBytesForCurrentThread() - preBefore;
        }
        void Post()
        {
            if (frame >= 24) { deltas[frame] = GC.GetAllocatedBytesForCurrentThread() - before; allocated += deltas[frame]; }
            if (frame is 3 or 5)
            {
                using var pixels = window.GetTexture().GetImage()!;
                void At(int x, int y, bool drawn) { var color = pixels.GetPixel(x, y); Check(drawn ? backend == "compatibility" ? color.R > .99f && color.G > .99f && color.B > .99f : color.G > .99f && color.R > .99f && color.B < .01f : color.R < .01f && color.G < .01f && color.B < .01f, "LOD pixel " + language + " " + x + "," + y + " frame=" + frame + " " + color); }
                At(52, 12, frame == 3); At(8, 12, true); At(126, 12, false); At(108, 12, true); At(142, 12, false); At(146, 12, true); At(166, 12, false); At(184, 12, true); At(206, 12, false); At(224, 12, true); At(246, 12, false); At(28, 64, false); At(108, 64, true);
                if (Environment.GetEnvironmentVariable("ELECTRON2D_MESH_LOD_OUTPUT") is { } output) { Directory.CreateDirectory(output); pixels.SavePNG(System.IO.Path.Combine(output, language + "-retained-" + frame + ".png")); }
            }
            if (frame == 3) node.Scale = new(.5f, .5f);
            if (++frame == 152) { RenderingServer.FramePreDraw -= Pre; RenderingServer.FramePostDraw -= Post; RenderingServer.FreeRID(palette); RenderingServer.FreeRID(owned); window.Tree!.Quit(); }
        }
        Engine.Run(window); if (allocated != 0) for (var i = 24; i < deltas.Length; i++) if (deltas[i] != 0) Console.WriteLine(language + " allocation frame=" + i + " total=" + deltas[i] + " pre=" + preDeltas[i]); Check(allocated == 0, language + " LOD full frames allocated " + allocated); Console.WriteLine(language + " LOD: 64 active +64 idle complete frames, 0 managed bytes.");
    }
    private static Shader Shader(string language)
    {
        using var input = typeof(MeshLODNativeTests).Assembly.GetManifestResourceStream("Electron2D.Device.MeshFrame" + language + ".frag.spv")!; using var bytes = new MemoryStream(); input.CopyTo(bytes); return Electron2D.Shader.CreateFromSPIRV(bytes.GetBuffer().AsSpan(0, (int)bytes.Length));
    }
    private sealed class Owned(RID mesh) : Entity { protected override void OnDraw() => DrawMesh(mesh); }
}
