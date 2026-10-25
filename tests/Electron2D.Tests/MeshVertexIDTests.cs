using System.Runtime.InteropServices;
using Electron2D;
using static TextureArrayTests;
using static Electron2D.MaterialLightingProgram;

internal static class MeshVertexIDTests
{
    private static MeshSurfaceData Data(Mesh.PrimitiveType primitive = Mesh.PrimitiveType.Triangles, bool wide = false)
    {
        var count = wide ? 65538 : 6; var a = wide ? 65536 : 4; var b = 2; var c = wide ? 65537 : 5;
        var vertices = new Vector2[count]; vertices[a] = new(0, 0); vertices[b] = new(24, 0); vertices[c] = new(0, 24); vertices[1] = new(24, 24);
        if (primitive is Mesh.PrimitiveType.Lines or Mesh.PrimitiveType.LineStrip) { vertices[a] = new(0, 12.5f); vertices[b] = new(24, 12.5f); vertices[c] = new(24, 24); }
        if (primitive == Mesh.PrimitiveType.Points) vertices[a] = new(12.5f, 12.5f);
        return new() { Vertices = vertices, UVs = Enumerable.Range(0, count).Select(i => new Vector2(i, 0)).ToArray(), Indices = primitive switch { Mesh.PrimitiveType.Points => [a], Mesh.PrimitiveType.Lines => [a, b], Mesh.PrimitiveType.LineStrip => [a, b, c], Mesh.PrimitiveType.TriangleStrip => [a, b, c, 1], _ => [a, b, c, b, 1, c] } };
    }
    private static byte[] Module(string language, string stage)
    {
        using var stream = typeof(MeshVertexIDTests).Assembly.GetManifestResourceStream($"Electron2D.Device.DeviceVertexID{language}.{stage}.spv")!;
        using var memory = new MemoryStream(); stream.CopyTo(memory); return memory.ToArray();
    }
    internal static byte[] Modern(byte[] code)
    {
        var module = new Electron2D.MaterialLightingProgram.Module(code); module.Header[1] = 0x10500;
        var entry = module.Instructions.Single(i => Op(i) == 15); var start = StringEnd(entry, 3);
        var globals = module.Instructions.Where(i => Op(i) == 59 && i[3] != 7).Select(i => i[2]);
        var replacement = entry.Concat(globals.Except(entry.Skip(start))).ToArray(); replacement[0] = (uint)(replacement.Length << 16) | 15;
        module.Instructions[module.Instructions.IndexOf(entry)] = replacement;
        return MemoryMarshal.AsBytes(module.Header.Concat(module.Instructions.SelectMany(i => i)).ToArray().AsSpan()).ToArray();
    }
    private static Shader Program(string language, bool modern = false) => Shader.CreateFromSPIRV(modern ? Modern(Module(language, "vert")) : Module(language, "vert"), Module(language, "frag"));
    internal static void Run()
    {
        CanvasVertexLayoutTests.Run();
        foreach (var primitive in Enum.GetValues<Mesh.PrimitiveType>())
        {
            using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(primitive, Data(primitive));
            var vertices = MeshLODTests.Draw(mesh, new Transform(.3f, new(20, 30)));
            Check(vertices.Count > 0 && vertices.All(v => v.VertexID == (uint)v.UV.X), $"Original indices survive {primitive} expansion and transforms.");
        }
        using var wide = new ArrayMesh(); wide.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, Data(wide: true));
        Check(MeshLODTests.Draw(wide, Transform.Identity).Select(v => v.VertexID).SequenceEqual(new uint[] { 65536, 2, 65537, 2, 1, 65537 }), "Vertex IDs retain their full 32-bit width.");
        using var sequential = new ArrayMesh(); sequential.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new() { Vertices = [Vector2.Zero, Vector2.Right, Vector2.Down], UVs = [Vector2.Zero, Vector2.Right, new(2, 0)] });
        Check(MeshLODTests.Draw(sequential, Transform.Identity).Select(v => v.VertexID).SequenceEqual(new uint[] { 0, 1, 2 }), "Sequential vertices use their source ordinals.");
        using var lod = MeshLODTests.Mesh(); var lodVertices = MeshLODTests.Draw(lod, new Transform(0, Vector2.Zero).Scaled(new(.1f, .1f)));
        Check(lodVertices.Select(v => v.VertexID).SequenceEqual(new uint[] { 0, 1, 2 }), "LOD indices remain source vertex identities.");
        var data = Data(); var triangles = new List<CanvasVertex>(); CanvasMesh.AppendTriangleData(triangles, data, data.Indices.Length, Transform.Identity, Colors.White, false, null);
        Check(triangles.All(v => v.VertexID == (uint)v.UV.X), "The canvas triangle-array sibling uses the same identities.");
        foreach (var language in new[] { "GLSL", "HLSL" }) foreach (var modern in new[] { false, true })
            {
                using var shader = Program(language, modern); var program = shader.GetProgram();
                for (var i = 0; i < 4; i++)
                {
                    var code = program.VertexCode((i & 1) != 0, (i & 2) != 0)!;
                    if (Environment.GetEnvironmentVariable("ELECTRON2D_VERTEX_DUMP") is { } folder) { Directory.CreateDirectory(folder); File.WriteAllBytes(System.IO.Path.Combine(folder, $"id-{language}-{(modern ? "15" : "10")}-{i}.spv"), code); }
                    Check(code.Length > Module(language, "vert").Length, "Each ordinary/instanced/lit variant wraps the authored builtin.");
                }
                var original = shader.GetSPIRV(RenderingDevice.ShaderStage.Vertex); Reject<ArgumentException>(() => shader.SetSPIRV(original, new byte[20]));
                Check(shader.GetSPIRV(RenderingDevice.ShaderStage.Vertex).SequenceEqual(original), "Rejected replacement preserves authored vertex identity.");
            }
        Console.WriteLine("Mesh vertex ID: all topologies, sequential/indexed/32-bit indices, LODs, triangle siblings and signed/unsigned shader variants passed.");
    }
    internal static void RunNative(string profile)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, profile == "compatibility" ? "compatibility" : "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var shader = Program(profile.Contains("hlsl") ? "HLSL" : "GLSL", profile.Contains("modern")); using var material = new ShaderMaterial { Shader = shader };
        if (profile == "compatibility")
        {
            using var rejectedWindow = new Window { Size = new(96, 96) }; rejectedWindow.AddChild(new Rectangle { Material = material });
            Reject<NotSupportedException>(() => Engine.Run(rejectedWindow));
            Console.WriteLine("Vertex ID shaders reject compatibility explicitly."); return;
        }
        using var rawMaterial = new ShaderMaterial { Shader = shader }; rawMaterial.SetShaderParameter("raw", 1f);
        using var window = new Window { Size = new(520, 144) };
        window.AddChild(new Rectangle { Position = new(4, 100) }); window.AddChild(new Rectangle { Position = new(44, 100), Material = rawMaterial });
        var samples = new List<Vector2i>(); var resources = new List<Resource>(); var column = 0;
        MeshInstance Add(Mesh mesh, Mesh.PrimitiveType primitive = Mesh.PrimitiveType.Triangles)
        {
            resources.Add(mesh); var origin = new Vector2(4 + column++ * 36, 4); var node = new MeshInstance { Mesh = mesh, Material = material, Position = origin }; window.AddChild(node);
            samples.Add(new((int)origin.X + (primitive == Mesh.PrimitiveType.Points ? 12 : 6), (int)origin.Y + (primitive is Mesh.PrimitiveType.Points or Mesh.PrimitiveType.Lines or Mesh.PrimitiveType.LineStrip ? 12 : 6))); return node;
        }
        foreach (var primitive in Enum.GetValues<Mesh.PrimitiveType>()) { var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(primitive, Data(primitive)); Add(mesh, primitive); }
        var wide = new ArrayMesh(); wide.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, Data(wide: true)); Add(wide);
        var basis = new ArrayMesh(); basis.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, Data()); Add(basis);
        using var tool = new SurfaceTool(); tool.CreateFrom(basis, 0); Add(tool.Commit());
        var immediate = new ImmediateMesh(); immediate.SurfaceBegin(Mesh.PrimitiveType.Triangles);
        var seq = new[] { Vector2.Zero, new Vector2(24, 0), new Vector2(0, 24) };
        for (var i = 0; i < 3; i++) { immediate.SurfaceSetUV(new(i, 0)); immediate.SurfaceAddVertex2D(seq[i]); }
        immediate.SurfaceEnd(); Add(immediate);
        var morph = new ArrayMesh(); morph.AddBlendShape("move"); morph.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, Data(), [new() { Vertices = Enumerable.Repeat(new Vector2(2, 0), 6).ToArray() }]); morph.BlendShapeMode = BlendShapeMode.Relative; Add(morph).SetBlendShapeValue(0, .5f);
        using var skeleton = new Skeleton(); var bone = new Bone { Rest = Transform.Identity }; bone.SetAutocalculateLengthAndAngle(false); bone.SetLength(24); skeleton.AddChild(bone); window.AddChild(skeleton);
        var skinData = Data(); skinData.Bones = new int[48]; skinData.Weights = new float[48]; for (var i = 0; i < 6; i++) skinData.Weights[i * 8 + 7] = 1;
        var skin = new ArrayMesh(); skin.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, skinData, flags: Mesh.ArrayFormat.Use8BoneWeights); var skinned = Add(skin);
        using var multi = new MultiMesh { Mesh = basis, InstanceCount = 2 }; multi.SetInstanceTransform2D(0, new(0, new(4, 48))); multi.SetInstanceTransform2D(1, new(0, new(40, 48))); window.AddChild(new MultiMeshInstance { MultiMesh = multi, Material = material }); samples.Add(new(10, 54)); samples.Add(new(46, 54));
        var group = new CanvasGroup { Position = new(84, 48) }; group.AddChild(new MeshInstance { Mesh = basis, Material = material }); window.AddChild(group); samples.Add(new(90, 54));
        var lodData = Data(); var lod = new ArrayMesh(); lod.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, lodData, lods: new Dictionary<float, int[]> { [4] = [4, 2, 5] }); var lodNode = Add(lod); lodNode.Scale = new(.5f, .5f); samples[^1] -= new Vector2i(3, 3);
        if (profile.Contains("lit")) { window.AddChild(new CanvasModulate { Color = new(.5f, .5f, .5f, 1) }); window.AddChild(new DirectionalLight { Color = Colors.White, Energy = .5f }); }
        var forward = new byte[] { 4, 0, 2, 0 }; var reverse = new byte[] { 2, 0, 4, 0 };
        var frame = 0; long before = 0, active = 0, idle = 0;
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == "gpu", "Actual GPU shader execution."); RenderingServer.SetDefaultClearColor(Colors.Black); skinned.AttachSkeleton(skeleton.GetSkeleton());
            window.Tree!.ProcessFrameStarted += _ => { before = GC.GetAllocatedBytesForCurrentThread(); if (frame is >= 16 and < 80) { basis.UpdateIndexRegion(0, 0, (frame & 1) == 0 ? forward : reverse); bone.Position = new((frame & 1), 0); multi.SetInstanceTransform2D(1, new(0, new(40 + (frame & 1), 48))); } };
            RenderingServer.FramePostDraw += () =>
            {
                if (frame is >= 16 and < 80) active += GC.GetAllocatedBytesForCurrentThread() - before;
                if (frame is >= 80 and < 144) idle += GC.GetAllocatedBytesForCurrentThread() - before;
                if (++frame == 144) { Check(active == 0 && idle == 0, $"Vertex ID frame allocations: active={active}, idle={idle}."); window.Tree!.Quit(); }
                if (frame > 2) return;
                using var image = window.GetTexture().GetImage()!;
                foreach (var point in samples) { var pixel = image.GetPixel(point.X, point.Y); Check(pixel.R > .95f && pixel.G > .95f && pixel.B > .95f, $"Original vertex ID at {point}: {pixel} ({profile})."); }
                var raw = image.GetPixel(50, 106); Check(raw.R > .18f && raw.R < .3f && raw.G < .02f, $"Ordinary geometry retains native batch vertex IDs: {raw}.");
                if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } folder) { Directory.CreateDirectory(folder); image.SavePNG(System.IO.Path.Combine(folder, $"vertex-id-{profile}-{frame}.png")); }
                if (frame == 1) shader.SetSPIRV(Module(profile.Contains("hlsl") ? "HLSL" : "GLSL", "vert"), Module(profile.Contains("hlsl") ? "HLSL" : "GLSL", "frag"));
            };
        };
        try { Engine.Run(window); }
        finally { window.Dispose(); foreach (var resource in resources) resource.Dispose(); }
        Check(frame == 144, "All prepared frames executed."); Console.WriteLine($"Vertex ID native {profile}: topology/32-bit/authoring/morph/eighth-slot skin/LOD/group/instances, reload and native fallback pixels; active={active}, idle={idle}.");
    }
    private sealed class Rectangle : Entity { protected override void OnDraw() => DrawRect(new(0, 0, 24, 24), Colors.White); }
}
