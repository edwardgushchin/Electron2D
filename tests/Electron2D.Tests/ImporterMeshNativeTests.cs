using Electron2D;
using static TextureArrayTests;

internal static class ImporterMeshNativeTests
{
    internal static void Run()
    {
        ImporterMeshTests.Run(); foreach (var language in new[] { "GLSL", "HLSL" }) Retained(language); Retained("baseline", "compatibility"); Console.WriteLine("Generated importer levels, raw compilation and merge execute actual GLSL/HLSL/compatibility pixels and zero warmed complete frames.");
    }
    private static void Retained(string language, string backend = "gpu")
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var shader = backend == "gpu" ? Shader(language) : null; using var material = shader is null ? null : new ShaderMaterial { Shader = shader };
        using var r = ImporterMeshTests.Producer(); r.GenerateLODs(60); using var mesh = r.GetMesh()!; mesh.SurfaceSetMaterial(0, material);
        using var shaped = ImporterMeshTests.Producer(true); shaped.BlendShapeMode = BlendShapeMode.Relative; shaped.GenerateLODs(60); using var morphMesh = shaped.GetMesh()!; morphMesh.SurfaceSetMaterial(0, material);
        var data = ImporterMeshTests.Grid(); data.Bones = new int[data.Vertices.Length * 8]; data.Weights = new float[data.Bones.Length]; for (var i = 0; i < data.Vertices.Length; i++) { data.Bones[i * 8 + 7] = 7; data.Weights[i * 8 + 7] = 1; }
        using var skinSource = new ImporterMesh(); skinSource.AddSurface(Mesh.PrimitiveType.Triangles, data, flags: Mesh.ArrayFormat.CompressAttributes | Mesh.ArrayFormat.Use8BoneWeights); skinSource.GenerateLODs(60, Enumerable.Repeat(Transform.Identity, 8).ToArray()); using var skin = skinSource.GetMesh()!; skin.SurfaceSetMaterial(0, material);
        using var joined = ImporterMesh.MergeImporterMeshes([r, r], [Transform.Identity, new(new Vector2(-1, 0), new Vector2(0, 1), new(72, 0))]); using var merge = joined.GetMesh()!; for (var i = 0; i < merge.GetSurfaceCount(); i++) merge.SurfaceSetMaterial(i, material);
        using var instances = new MultiMesh { Mesh = mesh, InstanceCount = 2 }; instances.SetInstanceTransform2D(0, Transform.Identity); instances.SetInstanceTransform2D(1, new(new Vector2(.5f, 0), new Vector2(0, .5f), new(40, 0)));
        using var window = new Window { Size = new(256, 128), MinSize = new(256, 128), MaxSize = new(256, 128), Visible = true };
        var morph = new MeshInstance { Mesh = morphMesh, Name = "Morph", Position = new(48, 4) }; morph.SetBlendShapeValue(0, .5f); var skinned = new MeshInstance { Mesh = skin, Name = "Skin", Position = new(92, 4) };
        window.AddChild(new MeshInstance { Name = "Base", Mesh = mesh, Position = new(4, 4) }); window.AddChild(morph); window.AddChild(skinned); var group = new CanvasGroup { Name = "Group", Position = new(136, 4) }; group.AddChild(new MeshInstance { Mesh = mesh }); window.AddChild(group); window.AddChild(new MultiMeshInstance { Name = "Instances", MultiMesh = instances, Position = new(4, 48) }); window.AddChild(new MeshInstance { Name = "Merge", Mesh = merge, Position = new(80, 48) });
        var frame = 0; long before = 0, allocated = 0; RID palette = default, owned = default;
        window.Ready += _ =>
        {
            window.Tree!.ProcessFrameStarted += _ => { if (frame >= 24) before = GC.GetAllocatedBytesForCurrentThread(); }; RenderingServer.SetDefaultClearColor(Colors.Black); Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Importer backend without fallback.");
            palette = RenderingServer.SkeletonCreate(); RenderingServer.SkeletonAllocateData(palette, 8); RenderingServer.SkeletonBoneSetTransform2D(palette, 7, new(0, new(4, 0))); RenderingServer.CanvasItemAttachSkeleton(skinned.GetCanvasItem(), palette);
            owned = RenderingServer.MeshCreate(); RenderingServer.MeshAddSurfaceFromArrays(owned, Mesh.PrimitiveType.Triangles, mesh.SurfaceGetArrays(0), Mesh.ArrayFormat.CompressAttributes, lods: mesh.SurfaceGetLODs(0)); RenderingServer.MeshSurfaceSetMaterial(owned, 0, material); window.AddChild(new Owned(owned) { Name = "Owned", Position = new(184, 4) });
            Check(MeshLODTests.Draw(mesh, Transform.Identity).Count < mesh.SurfaceGetArrayIndexLen(0), "Generated levels replace full geometry in actual retained preparation."); RenderingServer.FramePreDraw += Pre; RenderingServer.FramePostDraw += Post;
        };
        void Pre()
        {
            if (frame == 16) GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
            if (frame >= 6 && frame < 88) { morph.SetBlendShapeValue(0, frame % 2 == 0 ? .5f : 1); RenderingServer.SkeletonBoneSetTransform2D(palette, 7, new(0, new(4 + frame % 2, 0))); if (!ReferenceEquals(r.GetMesh(), mesh)) throw new InvalidOperationException("Cached mesh identity."); }
        }
        void Post()
        {
            if (frame >= 24) allocated += GC.GetAllocatedBytesForCurrentThread() - before;
            if (frame == 3)
            {
                using var pixels = window.GetTexture().GetImage()!; foreach (var p in new Vector2i[] { new(12, 12), new(58, 12), new(104, 12), new(144, 12), new(192, 12), new(12, 56), new(48, 52), new(88, 56), new(128, 56) }) { var c = pixels.GetPixel(p.X, p.Y); Check(backend == "compatibility" ? c.R > .99f && c.G > .99f && c.B > .99f : p.X == 128 && p.Y == 56 ? c.R < .01f && c.G < .01f && c.B > .99f : c.R < .01f && c.G > .99f && c.B < .01f, "Importer numeric attributes and generated/merged/morph/skin/group/instance/owned pixels: " + language + " " + p + " " + c); }
                foreach (var p in new Vector2i[] { new(0, 0), new(38, 12), new(170, 12), new(116, 56), new(154, 56) }) { var c = pixels.GetPixel(p.X, p.Y); Check(c.R < .01f && c.G < .01f && c.B < .01f, "Generation/merge retains silhouette and gaps."); }
                if (Environment.GetEnvironmentVariable("ELECTRON2D_IMPORTER_OUTPUT") is { } output) { Directory.CreateDirectory(output); pixels.SavePNG(System.IO.Path.Combine(output, language + ".png")); }
            }
            if (++frame == 152) { RenderingServer.FramePreDraw -= Pre; RenderingServer.FramePostDraw -= Post; RenderingServer.FreeRID(palette); RenderingServer.FreeRID(owned); window.Tree!.Quit(); }
        }
        Engine.Run(window); Check(allocated == 0, language + " importer complete frames allocated " + allocated); Console.WriteLine(language + ": 64 active +64 idle importer frames, 0 managed bytes.");
    }
    private static Shader Shader(string language)
    {
        using var input = typeof(ImporterMeshNativeTests).Assembly.GetManifestResourceStream("Electron2D.Device.MeshImporter" + language + ".frag.spv")!; using var bytes = new MemoryStream(); input.CopyTo(bytes); return Electron2D.Shader.CreateFromSPIRV(bytes.GetBuffer().AsSpan(0, (int)bytes.Length));
    }
    private sealed class Owned(RID mesh) : Entity { protected override void OnDraw() => DrawMesh(mesh); }
}
