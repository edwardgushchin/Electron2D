using System.Runtime.InteropServices;
using Electron2D;
using static TextureArrayTests;

internal static partial class MultiMeshGPUTests
{
    private sealed class Background : Entity { protected override void OnDraw() => DrawRect(new(0, 0, 110, 74), Colors.White); }
    private static void Oracles()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var window = new Window { Size = new(128, 96) };
        var resources = new List<Resource>(); var pairs = new List<(SubViewport Reference, SubViewport GPU, MultiMesh Source, MultiMesh Target, string Name)>(); var skins = new List<MultiMeshInstance>();
        foreach (var name in new[] { "plain", "colors", "custom", "both", "LOD", "indirect LOD", "snap", "group child", "group owner", "mask", "HDR", "MSAA", "skin", "shader snap" })
        {
            var data = name.EndsWith("LOD", StringComparison.Ordinal) ? MeshLODTests.Data() : MeshTests.Quad(Colors.White);
            if (name == "skin") { data.Bones = new int[data.VertexCount * 4]; data.Weights = new float[data.VertexCount * 4]; for (var i = 0; i < data.VertexCount; i++) data.Weights[i * 4] = 1; }
            var mesh = new ArrayMesh(); resources.Add(mesh); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, data, lods: name.EndsWith("LOD", StringComparison.Ordinal) ? MeshLODTests.Levels() : null);
            ShaderMaterial? material = null;
            if (name is "custom" or "both" or "shader snap") { using var module = typeof(MultiMeshGPUTests).Assembly.GetManifestResourceStream("TestShaders.InstanceCustom.spv")!; using var bytes = new MemoryStream(); module.CopyTo(bytes); var shader = Shader.CreateFromSPIRV(bytes.ToArray()); resources.Add(shader); material = new() { Shader = shader }; resources.Add(material); }
            var source = new MultiMesh { Mesh = mesh, UseColors = name != "plain", UseCustomData = name is "custom" or "both" or "shader snap", InstanceCount = 2, CustomAABB = new(0, 0, 128, 96) }; resources.Add(source);
            source.SetInstanceTransform2D(0, name.EndsWith("LOD", StringComparison.Ordinal) ? new(new Vector2(.5f, 0), new Vector2(0, .5f), new(8, 8)) : new(0, new(8.6f, 8.3f))); source.SetInstanceTransform2D(1, new(new Vector2(-1, .125f), new Vector2(.25f, 1), new(96, 36)));
            if (source.UseColors) { source.SetInstanceColor(0, new(.8f, .4f, .2f, .75f)); source.SetInstanceColor(1, new(.2f, .6f, .8f, 1)); }
            if (source.UseCustomData) { source.SetInstanceCustomData(0, Colors.Blue); source.SetInstanceCustomData(1, Colors.Yellow); }
            var target = new MultiMesh { Mesh = mesh, UseColors = source.UseColors, UseCustomData = source.UseCustomData, InstanceCount = 2, CustomAABB = source.CustomAABB }; resources.Add(target);
            SubViewport View(MultiMesh instances)
            {
                var view = new SubViewport { Size = new(128, 96), RenderTargetUpdateMode = ViewportUpdateMode.Always, UseHDR2D = name == "HDR", MSAA2D = name == "MSAA" ? Viewport.MSAA.Samples4X : Viewport.MSAA.Disabled, SnapVerticesToPixel = name is "snap" or "shader snap" };
                var node = new MultiMeshInstance { MultiMesh = instances, Material = material, Modulate = new(.8f, .7f, .6f, .8f) };
                if (name == "group child") { var group = new CanvasGroup { SelfModulate = new(1, 1, 1, .6f), FitMargin = 0 }; group.AddChild(node); view.AddChild(group); }
                else if (name is "group owner" or "mask") { node.AddChild(new Background()); view.AddChild(node); }
                else view.AddChild(node);
                if (name == "skin") skins.Add(node);
                window.AddChild(view); return view;
            }
            pairs.Add((View(source), View(target), source, target, name));
        }
        var ownedInstances = new List<RID>(); var frames = 0; var palettes = new List<RID>();
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); var device = RenderingServer.GetRenderingDevice()!;
            foreach (var pair in pairs)
            {
                if (pair.Name == "indirect LOD")
                {
                    var rid = RenderingServer.MultiMeshCreate(); ownedInstances.Add(rid); RenderingServer.MultiMeshAllocateData(rid, 2, true, false, true); RenderingServer.MultiMeshSetMesh(rid, pair.Source.Mesh!.GetRID()); RenderingServer.MultiMeshSetCustomAABB(rid, pair.Source.CustomAABB); RenderingServer.MultiMeshSetBuffer(rid, pair.Source.Buffer);
                    var command = RenderingServer.MultiMeshGetCommandBufferRDRID(rid); uint[] words = [12, 2, 0, 0, 0]; device.BufferUpdate(command, 0, 20, MemoryMarshal.AsBytes(words.AsSpan()));
                    ((MultiMeshInstance)pair.GPU.GetChild(0)).MultiMesh = null; pair.GPU.AddChild(new Draw { Instances = rid, Modulate = new(.8f, .7f, .6f, .8f) });
                }
                else { var buffer = RenderingServer.MultiMeshGetBufferRDRID(pair.Target.GetRID()); var data = pair.Source.Buffer; device.BufferUpdate(buffer, 0, (uint)(data.Length * 4), MemoryMarshal.AsBytes(data.AsSpan())); }
                if (pair.Name is "group owner" or "mask") foreach (var view in new[] { pair.Reference, pair.GPU }) { var owner = (CanvasItem)view.GetChild(0); RenderingServer.CanvasItemSetCanvasGroupMode(owner.GetCanvasItem(), pair.Name == "mask" ? RenderingServer.CanvasGroupMode.ClipOnly : RenderingServer.CanvasGroupMode.Transparent, clearMargin: 0, fitEmpty: false, fitMargin: 0); }
            }
            foreach (var node in skins) { var rig = RenderingServer.SkeletonCreate(); palettes.Add(rig); RenderingServer.SkeletonAllocateData(rig, 1); RenderingServer.SkeletonBoneSetTransform2D(rig, 0, new(.2f, new(3, 2))); RenderingServer.CanvasItemAttachSkeleton(node.GetCanvasItem(), rig); }
            RenderingServer.FramePostDraw += () =>
            {
                if (++frames != 3) return;
                foreach (var pair in pairs)
                {
                    using var expected = pair.Reference.GetTexture().GetImage()!; using var actual = pair.GPU.GetTexture().GetImage()!; var occupied = 0;
                    for (var y = 0; y < 96; y++) for (var x = 0; x < 128; x++)
                        {
                            var a = expected.GetPixel(x, y); var b = actual.GetPixel(x, y); if (a.R + a.G + a.B > .01f) occupied++;
                            Check(MathF.Abs(a.R - b.R) < .012f && MathF.Abs(a.G - b.G) < .012f && MathF.Abs(a.B - b.B) < .012f && MathF.Abs(a.A - b.A) < .012f, $"GPU buffer {pair.Name} differs from authored replay at {x},{y}: {a} / {b}.");
                        }
                    Check(occupied > 40, pair.Name + " oracle rendered nonempty geometry.");
                    if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } path) { Directory.CreateDirectory(path); actual.SavePNG(System.IO.Path.Combine(path, "gpu-buffer-" + pair.Name.Replace(' ', '-') + ".png")); }
                }
                foreach (var rid in ownedInstances) RenderingServer.FreeRID(rid); foreach (var rig in palettes) RenderingServer.FreeRID(rig); window.Tree!.Quit();
            };
        };
        try { Check(Engine.Run(window) == 0 && frames == 3, "Independent authored/GPU-buffer canvas oracles completed."); }
        finally { for (var i = resources.Count - 1; i >= 0; i--) resources[i].Dispose(); }
        Console.WriteLine("GPU buffer canvas oracles: four schemas, reflected/sheared poses, LOD, snapping, groups/masks, HDR/MSAA, skin and custom data passed.");
    }
}
