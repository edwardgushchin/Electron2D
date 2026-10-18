using Electron2D;
using static TextureArrayTests;

internal static partial class MeshBoundsTests
{
    internal static void Native(string profile)
    {
        var gpu = profile != "compatibility";
        ProjectSettings.Set(ProjectSettings.RenderingMethod, gpu ? "gpu" : "compatibility"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var shader = gpu ? BoundsShader(profile == "hlsl" ? "HLSL" : "GLSL") : null;
        using var material = shader is null ? null : new ShaderMaterial { Shader = shader };
        if (material is not null) { material.SetShaderParameter("offset", new Vector2(108, 0)); material.SetShaderParameter("gain", 1f); }
        MeshSurfaceData Data(bool skin = false)
        {
            var data = new MeshSurfaceData { Vertices = [new(gpu ? -100 : 8, 0), new(gpu ? -84 : 24, 0), new(gpu ? -84 : 24, 16), new(gpu ? -100 : 8, 16)], Colors = Enumerable.Repeat(Colors.Green, 4).ToArray(), Indices = [0, 1, 2, 0, 2, 3] };
            if (skin) { data.Bones = new int[16]; data.Weights = new float[16]; for (var i = 0; i < 4; i++) data.Weights[i * 4] = 1; }
            return data;
        }
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, Data());
        using var expanded = new ArrayMesh(); expanded.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, Data(), lods: new Dictionary<float, int[]> { [.25f] = [0, 1, 2] });
        using var morph = new ArrayMesh(); morph.AddBlendShape("shift"); morph.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, Data(), [new() { Vertices = Enumerable.Repeat(new Vector2(2, 0), 4).ToArray() }]);
        using var hardware = new MultiMesh { Mesh = mesh, InstanceCount = 2 }; hardware.SetInstanceTransform2D(0, Transform.Identity); hardware.SetInstanceTransform2D(1, new(0, new(32, 0)));
        using var fallback = new MultiMesh { Mesh = expanded, InstanceCount = 2 }; fallback.SetInstanceTransform2D(0, Transform.Identity); fallback.SetInstanceTransform2D(1, new(0, new(32, 0)));
        using var window = new Window { Size = new(128, 128) }; var frame = 0; RID owned = default, skeleton = default;
        var direct = new BoundsDraw { Material = material }; var instances = new BoundsDraw { Material = material, Multi = hardware, Position = new(0, 32) }; var expandedDraw = new BoundsDraw { Material = material, Multi = fallback, Position = new(0, 60) };
        var shaped = new MeshInstance { Mesh = morph, Material = material }; shaped.SetBlendShapeValue(0, .5f);
        var group = new CanvasGroup { Position = new(0, 4), FitMargin = 0, ClearMargin = 0 }; group.AddChild(direct); window.AddChild(group); window.AddChild(instances); window.AddChild(expandedDraw);
        var morphGroup = new CanvasGroup { Position = new(0, 88), FitMargin = 0, ClearMargin = 0 }; morphGroup.AddChild(shaped); window.AddChild(morphGroup);
        var inside = new Rect2(8, 0, 32, 20); var outside = new Rect2(-400, 0, 32, 20); var before = 0L; var active = 0L; var idle = 0L; var recorded = new int[3]; System.Collections.IDictionary? pipelines = null; var preparedPipelines = 0;
        void Set(Rect2 value) { RenderingServer.MeshSetCustomAABB(owned, value); mesh.CustomAABB = value; expanded.CustomAABB = value; morph.CustomAABB = value; }
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == (gpu ? "gpu" : "compatibility"), "Requested bounds backend executes."); RenderingServer.SetDefaultClearColor(Colors.Black);
            if (gpu)
            {
                const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                var backend = typeof(RenderingServer).GetField("_backend", flags)!.GetValue(RenderingServer.Service)!;
                pipelines = (System.Collections.IDictionary)typeof(GPUCanvasBackend).GetField("_pipelines", flags)!.GetValue(backend)!;
            }
            owned = RenderingServer.MeshCreate(); RenderingServer.MeshAddSurfaceFromArrays(owned, Mesh.PrimitiveType.Triangles, Data(skin: true)); direct.MeshRID = owned;
            skeleton = RenderingServer.SkeletonCreate(); RenderingServer.SkeletonAllocateData(skeleton, 1); RenderingServer.SkeletonBoneSetTransform2D(skeleton, 0, new(0, new(4, 0))); RenderingServer.CanvasItemAttachSkeleton(direct.GetCanvasItem(), skeleton);
            Check(RenderingServer.MeshGetCustomAABB(owned) == default && RenderingServer.MeshGetCustomAABB(mesh.GetRID()) == default, "Owned/borrowed defaults.");
            mesh.CustomAABB = inside; Check(RenderingServer.MeshGetCustomAABB(mesh.GetRID()) == inside, "Borrowed resource query sees the authored value."); mesh.CustomAABB = default;
            using var immediate = new ImmediateMesh(); Check(RenderingServer.MeshGetCustomAABB(immediate.GetRID()) == default, "Other borrowed mesh roles retain automatic bounds.");
            Reject<InvalidOperationException>(() => RenderingServer.MeshSetCustomAABB(mesh.GetRID(), inside)); Reject<ArgumentException>(() => RenderingServer.MeshSetCustomAABB(owned, new(0, 0, -1, 1)));
            Reject<ArgumentException>(() => RenderingServer.MeshGetCustomAABB(default)); Reject<ArgumentException>(() => RenderingServer.MeshSetCustomAABB(default, inside));
            Task.Run(() => { Reject<InvalidOperationException>(() => RenderingServer.MeshGetCustomAABB(owned)); Reject<InvalidOperationException>(() => RenderingServer.MeshSetCustomAABB(owned, inside)); }).GetAwaiter().GetResult();
            var retired = RenderingServer.MeshCreate(); RenderingServer.MeshSetCustomAABB(retired, inside); RenderingServer.FreeRID(retired); Reject<ArgumentException>(() => RenderingServer.MeshGetCustomAABB(retired));
            window.Tree!.ProcessFrameStarted += _ =>
            {
                before = GC.GetAllocatedBytesForCurrentThread();
                if (frame == 2) Set(outside); if (frame is 4 or 8) Set(inside); if (frame == 6) Set(default);
                if (frame >= 32 && frame < 96) Set((frame & 1) == 0 ? outside : inside);
                if (frame == 96) mesh.CustomAABB = outside;
            };
            RenderingServer.FramePostDraw += () =>
            {
                var bytes = GC.GetAllocatedBytesForCurrentThread() - before; if (frame >= 32 && frame < 96) active += bytes; else if (frame >= 96) idle += bytes;
                if (frame is 0 or 2 or 4 or 6 or 8)
                {
                    using var image = window.GetTexture().GetImage()!;
                    var visible = frame is 4 or 8 || !gpu && frame is 0 or 6; var expected = visible ? Colors.Green : Colors.Black;
                    foreach (var y in new[] { 8, 36, 64, 92 }) Pixel(image, 14, y, expected);
                    Pixel(image, 46, 36, expected); Pixel(image, 46, 64, expected);
                    var authored = RenderingServer.MeshGetCustomAABB(owned); Check(authored == (frame == 2 ? outside : frame is 4 or 8 ? inside : default), "Server bounds retain current policy.");
                    var bounds = RenderingServer.DebugCanvasItemGetRect(direct.GetCanvasItem()); if (frame is 2 or 4 or 8) Check(bounds == authored, "Manual bounds take precedence over the attached palette.");
                    if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } folder) { Directory.CreateDirectory(folder); image.SavePNG(System.IO.Path.Combine(folder, $"bounds-{profile}-{frame}.png")); }
                }
                if (frame == 24) { recorded[0] = direct.Calls; recorded[1] = instances.Calls; recorded[2] = expandedDraw.Calls; preparedPipelines = pipelines?.Count ?? 0; }
                if (frame >= 32) Check(direct.Calls == recorded[0] && instances.Calls == recorded[1] && expandedDraw.Calls == recorded[2], "Bounds changes preserve retained commands.");
                if (gpu && frame == 96) Check(pipelines!.Count == preparedPipelines, "One hidden frame retains prepared pipelines.");
                if (gpu && frame == 97) Check(pipelines!.Count == preparedPipelines - 1, "Two hidden frames retire the unused instance pipeline.");
                if (++frame == 160) window.Tree!.Quit();
            };
        };
        Check(Engine.Run(window) == 0 && frame == 160 && active == 0 && idle == 0, $"Bounds {profile}: 64 active/64 idle complete owner frames {active}/{idle} B.");
        Check(!RenderingMeshRegistry.Contains(owned) && !RenderingSkeletonRegistry.Contains(skeleton) && !mesh.IsDisposed, "Session retirement preserves borrowed resources.");
        Reject<InvalidOperationException>(() => RenderingServer.MeshGetCustomAABB(owned));
        Console.WriteLine($"Bounds {profile}: public ownership/thread guards, palette/group/morph and instance pixels, live reset, bounded pipeline retirement and 64 active/64 idle owner frames 0 B passed.");
    }
    private static Shader BoundsShader(string language) => Shader.CreateFromSPIRV(Module("CanvasUserVertex" + language + ".vert.spv"), Module("CanvasUserVertex" + language + ".frag.spv"));
    private static byte[] Module(string name) { using var stream = typeof(MeshBoundsTests).Assembly.GetManifestResourceStream("TestShaders." + name)!; using var bytes = new MemoryStream(); stream.CopyTo(bytes); return bytes.ToArray(); }
    private static void Pixel(Image image, int x, int y, Color expected) { var value = image.GetPixel(x, y); Check(Math.Abs(value.R - expected.R) < .05f && Math.Abs(value.G - expected.G) < .05f && Math.Abs(value.B - expected.B) < .05f, $"Bounds pixel {x}/{y}: {value}, expected {expected}."); }
    private sealed class BoundsDraw : Entity
    {
        internal RID MeshRID; internal MultiMesh? Multi; internal int Calls;
        protected override void OnDraw() { Calls++; if (Multi is { } multi) DrawMultiMesh(multi); else DrawMesh(MeshRID); }
    }
}
