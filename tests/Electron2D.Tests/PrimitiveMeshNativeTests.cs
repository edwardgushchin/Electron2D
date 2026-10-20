using Electron2D;
using Electron2D.Examples.PrimitiveMeshes;
using static TextureArrayTests;

internal static partial class PrimitiveMeshTests
{
    private sealed class Draw(Mesh mesh, bool rid = false, MultiMesh? multi = null) : Entity
    {
        protected override void OnDraw()
        {
            if (multi is not null) DrawMultiMesh(multi);
            else if (rid) RenderingServer.CanvasItemAddMesh(GetCanvasItem(), mesh.GetRID(), Transform.Identity, Colors.White);
            else DrawMesh(mesh);
        }
    }
    private static Shader LoadPrimitiveShader(string name)
    {
        using var input = typeof(PrimitiveMeshTests).Assembly.GetManifestResourceStream("Electron2D.Device." + name + ".frag.spv")!;
        using var memory = new MemoryStream(); input.CopyTo(memory); return Shader.CreateFromSPIRV(memory.ToArray());
    }
    internal static void Native(string backend)
    {
        var method = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fallback = ProjectSettings.Get(ProjectSettings.RenderingFallback); var fps = Engine.MaxFPS;
        try
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 0;
            using var mesh = new DiskMesh { Radius = 12, AddUV2 = true };
            using var directMesh = new DiskMesh { Radius = 10 }; using var ridMesh = new DiskMesh { Radius = 10 }; using var multiMesh = new DiskMesh { Radius = 8 };
            using var multi = new MultiMesh { Mesh = multiMesh, InstanceCount = 2 }; multi.SetInstanceTransform2D(0, Transform.Identity); multi.SetInstanceTransform2D(1, new(0, new(24, 0)));
            using var root = new Window { Size = new(160, 96), Borderless = true };
            var instance = new MeshInstance { Mesh = mesh, Position = new(24, 24), Modulate = Colors.Red }; var instanceGroup = new CanvasGroup { FitMargin = 0, ClearMargin = 0 }; instanceGroup.AddChild(instance); root.AddChild(instanceGroup);
            var group = new CanvasGroup { Position = new(64, 24), FitMargin = 0, ClearMargin = 0 }; group.AddChild(new Draw(directMesh) { Modulate = Colors.Green }); root.AddChild(group);
            root.AddChild(new Draw(ridMesh, true) { Position = new(104, 24), Modulate = Colors.Blue });
            root.AddChild(new Draw(multiMesh, multi: multi) { Position = new(24, 64), Modulate = Colors.White });
            root.Ready += _ =>
            {
                RenderingServer.SetDefaultClearColor(Colors.Black); Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Requested backend is active without fallback.");
                RenderingServer.ForceDraw(); RenderingServer.ForceDraw();
                using (var first = RenderingServer.Service!.Readback())
                {
                    Check(first.GetPixel(24, 24).R > .9f && first.GetPixel(64, 24).G > .9f && first.GetPixel(104, 24).B > .9f && first.GetPixel(24, 64).R > .9f && first.GetPixel(48, 64).R > .9f, "Cold generators feed MeshInstance, DrawMesh/group, borrowed RID and MultiMesh pixels.");
                    Check(first.GetPixel(40, 24).R < .01f, "Initial radius excludes the later ring.");
                }
                mesh.Radius = 20; mesh.Segments = 64; RenderingServer.ForceDraw(); using (var enlarged = RenderingServer.Service.Readback()) Check(enlarged.GetPixel(40, 24).R > .9f, "Live parameter update invalidates retained geometry and bounds.");
                if (backend == "gpu")
                {
                    var language = Environment.GetEnvironmentVariable("ELECTRON2D_PRIMITIVE_MESH_SHADER") ?? "GLSL";
                    using var uvShader = LoadPrimitiveShader("MeshUV2" + language); using var frameShader = LoadPrimitiveShader("MeshFrame" + language);
                    using var uvMaterial = new ShaderMaterial { Shader = uvShader }; using var frameMaterial = new ShaderMaterial { Shader = frameShader };
                    mesh.UV2Padding = 512; mesh.Material = uvMaterial; RenderingServer.ForceDraw();
                    using (var uv = RenderingServer.Service.Readback()) Check(Math.Abs(uv.GetPixel(24, 24).R - .25625f) < .025f && Math.Abs(uv.GetPixel(24, 24).G - .5125f) < .025f, "Generated secondary UV padding reaches authored shader independently of primary UV.");
                    mesh.Material = frameMaterial; RenderingServer.ForceDraw(); using (var normal = RenderingServer.Service.Readback()) Check(normal.GetPixel(24, 24).R > .99f, "Generated normal reaches authored shader.");
                    mesh.FlipFaces = true; RenderingServer.ForceDraw(); using (var normal = RenderingServer.Service.Readback()) Check(normal.GetPixel(24, 24).R < .01f, "Flipped generated normals reach authored shader.");
                    mesh.FlipFaces = false; mesh.Material = null;
                }
                using var hidden = new CanvasItemMaterial { BlendMode = BlendMode.Add }; mesh.Material = hidden; RenderingServer.ForceDraw();
                Check(RenderingServer.MeshGetCustomAABB(mesh.GetRID()) == default, "Generated borrowed RID visibility defaults.");
                mesh.CustomAABB = new(-400, -400, 10, 10); RenderingServer.ForceDraw(); using (var culled = RenderingServer.Service.Readback()) Check(culled.GetPixel(24, 24).R < .01f && culled.GetPixel(64, 24).G > .9f, "Authored visibility culls generated mesh without changing unrelated output.");
                Check(RenderingServer.MeshGetCustomAABB(mesh.GetRID()) == mesh.CustomAABB && mesh.GetAABB().Size.X == 40, "Server sees manual bounds while source geometry remains unchanged.");
                Reject<InvalidOperationException>(() => RenderingServer.MeshSetCustomAABB(mesh.GetRID(), default));
                mesh.CustomAABB = default; mesh.FlipFaces = true; RenderingServer.ForceDraw(); using (var flipped = RenderingServer.Service.Readback()) Check(flipped.GetPixel(24, 24).R > .9f, "Planar two-sided drawing retains flipped triangle coverage.");
                multiMesh.CustomAABB = new(-400, -400, 10, 10); RenderingServer.ForceDraw(); using (var culled = RenderingServer.Service.Readback()) Check(culled.GetPixel(24, 64).R < .01f, "Child generated bounds drive collection culling."); multiMesh.CustomAABB = default;
                directMesh.Radius = 14; RenderingServer.ForceDraw(); using (var resizedGroup = RenderingServer.Service.Readback()) Check(resizedGroup.GetPixel(76, 24).G > .9f, "CanvasGroup fit refreshes after generation.");
                for (var i = 0; i < 32; i++) { instance.Position = new(24 + (i & 1), 24); RenderingServer.ForceDraw(); }
                var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) { instance.Position = new(24 + (i & 1), 24); RenderingServer.ForceDraw(); }
                Check(GC.GetAllocatedBytesForCurrentThread() == before, "64 prepared active frames allocate zero managed owner bytes.");
                before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) RenderingServer.ForceDraw(); Check(GC.GetAllocatedBytesForCurrentThread() == before, "64 prepared idle frames allocate zero managed owner bytes.");
                if (Environment.GetEnvironmentVariable("ELECTRON2D_PRIMITIVE_MESH_OUTPUT") is { } output) { Directory.CreateDirectory(output); using var image = RenderingServer.Service.Readback(); image.SavePNG(System.IO.Path.Combine(output, backend + ".png")); }
                root.Tree!.Quit();
            };
            Check(Engine.Run(root) == 0 && root.IsDisposed, "Generated native session retires cleanly.");
        }
        finally { Engine.MaxFPS = fps; ProjectSettings.Set(ProjectSettings.RenderingMethod, method); ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback); }
        Console.WriteLine(backend + ": primitive native lazy generation/update/group/RID/instance/culling pixels and 64 active/64 idle zero managed owner frames passed.");
    }
}
