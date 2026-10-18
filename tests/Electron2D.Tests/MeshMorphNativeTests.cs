using System.Buffers.Binary;
using Electron2D;
using static TextureArrayTests;

internal static class MeshMorphNativeTests
{
    internal static void Run()
    {
        MeshMorphTests.Run(); foreach (var language in new[] { "GLSL", "HLSL" }) Retained(language); Retained("baseline", "compatibility");
        Console.WriteLine("Morph rendering: independent instance positions, mode changes, raw extracted shader frames, eight-slot skin, groups and owned shape API pixels passed.");
    }
    private static Shader Shader(string language)
    {
        using var input = typeof(MeshMorphNativeTests).Assembly.GetManifestResourceStream("Electron2D.Device.MeshFrame" + language + ".frag.spv")!; using var bytes = new MemoryStream(); input.CopyTo(bytes); return Electron2D.Shader.CreateFromSPIRV(bytes.GetBuffer().AsSpan(0, (int)bytes.Length));
    }
    private static void Retained(string language, string backend = "gpu")
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); using var shader = backend == "gpu" ? Shader(language) : null; using Material material = shader is null ? new CanvasItemMaterial() : new ShaderMaterial { Shader = shader };
        using var relative = MeshMorphTests.Mesh(); using var normalized = MeshMorphTests.Mesh(true); using var skin = MeshMorphTests.Mesh(true, true); foreach (var mesh in new[] { relative, normalized, skin }) mesh.SurfaceSetMaterial(0, material);
        using var tool = new SurfaceTool(); tool.CreateFromBlendShape(normalized, 0, "move"); using var pose = tool.Commit();
        using var window = new Window { Size = new(288, 96), Visible = true };
        var a = new MeshInstance { Name = "A", Mesh = relative, Position = new(4, 4) }; var b = new MeshInstance { Name = "B", Mesh = relative, Position = new(48, 4) }; var n = new MeshInstance { Name = "Normalized", Mesh = normalized, Position = new(92, 4) }; var baseline = new MeshInstance { Name = "Base", Mesh = relative, Position = new(136, 4) }; var skinned = new MeshInstance { Name = "Skin", Mesh = skin, Position = new(180, 4) }; var extracted = new MeshInstance { Name = "Extracted", Mesh = pose, Position = new(228, 4) };
        a.SetBlendShapeValue(0, .5f); b.SetBlendShapeValue(0, 1); n.SetBlendShapeValue(0, .5f); skinned.SetBlendShapeValue(0, 1); foreach (var node in new[] { a, b, n, baseline, skinned, extracted }) window.AddChild(node);
        var group = new CanvasGroup { Name = "Group", Position = new(4, 52) }; var grouped = new MeshInstance { Name = "GroupMesh", Mesh = relative }; grouped.SetBlendShapeValue(0, 1); group.AddChild(grouped); window.AddChild(group);
        RID palette = default, owned = default; var frame = 0; long before = 0, allocated = 0; var patch = new byte[8]; BinaryPrimitives.WriteSingleLittleEndian(patch, 0); BinaryPrimitives.WriteSingleLittleEndian(patch.AsSpan(4), 0);
        window.Ready += _ =>
        {
            window.Tree!.ProcessFrameStarted += _ => { if (frame >= 24) before = GC.GetAllocatedBytesForCurrentThread(); }; RenderingServer.SetDefaultClearColor(Colors.Black); Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Requested morph backend without fallback."); palette = RenderingServer.SkeletonCreate(); RenderingServer.SkeletonAllocateData(palette, 8); RenderingServer.SkeletonBoneSetTransform2D(palette, 5, new Transform(0, new(2, 0))); RenderingServer.CanvasItemAttachSkeleton(skinned.GetCanvasItem(), palette);
            owned = RenderingServer.MeshCreate(1); var data = MeshCustomTests.Data(); RenderingServer.MeshAddSurfaceFromArrays(owned, Electron2D.Mesh.PrimitiveType.Triangles, data, blendShapes: [MeshMorphTests.Pose(data, new(8, 0))]); Check(RenderingServer.MeshGetBlendShapeCount(owned) == 1 && RenderingServer.MeshSurfaceGetBlendShapeArrays(owned, 0)[0].Vertices[0].X == 8, "Owned typed shape producer/getters execute."); RenderingServer.MeshSetBlendShapeMode(owned, BlendShapeMode.Normalized); Check(RenderingServer.MeshGetBlendShapeMode(owned) == BlendShapeMode.Normalized && RenderingServer.MeshGetBlendShapeCount(normalized.GetRID()) == 1, "Owned mode mutation and borrowed queries execute."); Reject<InvalidOperationException>(() => RenderingServer.MeshSetBlendShapeMode(normalized.GetRID(), BlendShapeMode.Relative)); Reject<ArgumentOutOfRangeException>(() => RenderingServer.MeshCreate(-1)); var copy = RenderingServer.MeshSurfaceGetBlendShapeArrays(owned, 0); copy[0].Vertices[0] = Vector2.Zero; Check(RenderingServer.MeshSurfaceGetBlendShapeArrays(owned, 0)[0].Vertices[0].X == 8, "Owned pose queries copy arrays."); window.AddChild(new Owned(owned) { Name = "Owned", Position = new(52, 52), Material = material }); RenderingServer.FramePreDraw += Pre; RenderingServer.FramePostDraw += Post;
        };
        void Pre()
        {
            if (frame == 16) GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
            if (frame >= 24 && frame < 88) { var value = frame % 2 == 0 ? .5f : 1; a.SetBlendShapeValue(0, value); b.SetBlendShapeValue(0, 1 - value); n.SetBlendShapeValue(0, value); skinned.SetBlendShapeValue(0, value); grouped.SetBlendShapeValue(0, value); relative.SurfaceUpdateVertexRegion(0, 0, patch); normalized.SurfaceUpdateVertexRegion(0, 0, patch); RenderingServer.SkeletonBoneSetTransform2D(palette, 5, new Transform(0, new(2 + frame % 2, 0))); }
        }
        void Post()
        {
            if (frame >= 24) allocated += GC.GetAllocatedBytesForCurrentThread() - before;
            if (frame is 3 or 5)
            {
                using var pixels = window.GetTexture().GetImage()!; if (Environment.GetEnvironmentVariable("ELECTRON2D_MESH_MORPH_OUTPUT") is { } early) { Directory.CreateDirectory(early); pixels.SavePNG(System.IO.Path.Combine(early, language + "-retained-" + frame + ".png")); }
                void At(int x, int y, bool drawn, bool targetFrame = false) { var c = pixels.GetPixel(x, y); Check(!drawn ? c.R < .01f && c.G < .01f && c.B < .01f : backend == "compatibility" ? c.R > .99f && c.G > .99f && c.B > .99f : targetFrame ? c.R > .99f && c.G < .01f && c.B > .99f : c.G > .99f && c.R < .01f && c.B < .01f, "Morph position/frame pixel " + language + " " + x + "," + y + " frame=" + frame + " color=" + c); }
                At(frame == 3 ? 6 : 10, 12, false); At(frame == 3 ? 10 : 14, 12, true); At(frame == 3 ? 52 : 48, 12, frame == 5); At(60, 12, true); At(100, 12, true); At(138, 12, true); At(184, 12, false); At(192, 12, true); At(240, 12, true, true); At(8, 60, false); At(16, 60, true); At(56, 60, true); At(136, 12, true); At(116, 12, frame == 5);
            }
            if (frame == 3) { a.SetBlendShapeValue(0, 1); b.SetBlendShapeValue(0, 0); normalized.BlendShapeMode = BlendShapeMode.Relative; }
            if (++frame == 152) { RenderingServer.FramePreDraw -= Pre; RenderingServer.FramePostDraw -= Post; RenderingServer.FreeRID(palette); RenderingServer.FreeRID(owned); window.Tree!.Quit(); }
        }
        Engine.Run(window); Check(allocated == 0, language + " morph frames allocated " + allocated); Console.WriteLine(language + " morph payload: 64 active +64 idle complete frames, 0 managed bytes.");
    }
    private sealed class Owned(RID mesh) : Entity { protected override void OnDraw() => DrawMesh(mesh); }
}
