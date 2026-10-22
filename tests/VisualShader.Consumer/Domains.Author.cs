using Electron2D;
using IOPath = System.IO.Path;
using V = Electron2D.VisualShader;

internal static partial class GraphChecks
{
    internal static void DomainCPU()
    {
        using (var failed = new V())
        {
            var changes = 0; failed.PropertyListChanged += _ => throw new ArgumentException("schema observer"); failed.Changed += _ => { changes++; throw new InvalidOperationException("change observer"); };
            Reject<AggregateException>(() => failed.SetMode(Shader.Mode.Particles)); Check(failed.GetMode() == Shader.Mode.Particles && changes == 1, "Committed mode changes publish both failing observer channels.");
        }
        using (var mesh = new ArrayMesh())
        using (var emitter = new VisualShaderNodeParticleMeshEmitter { Mesh = mesh })
        {
            var changes = 0; emitter.Changed += _ => changes++; mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Points, new() { Vertices = [Vector2.Zero] });
            Check(changes == 1, "A borrowed mesh change invalidates its graph definition.");
            using var shallow = (VisualShaderNodeParticleMeshEmitter)emitter.Duplicate(); using var deep = (VisualShaderNodeParticleMeshEmitter)emitter.Duplicate(true);
            Check(ReferenceEquals(shallow.Mesh, mesh) && !ReferenceEquals(deep.Mesh, mesh), "Mesh duplication follows the resource graph ownership policy.");
            emitter.Mesh = null; mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Points, new() { Vertices = [Vector2.One] }); Check(changes == 2, "Rebinding detaches old mesh notifications.");
            Reject<ArgumentOutOfRangeException>(() => emitter.SurfaceIndex = -1);
        }
        using var graph = new V(); Shader shader = graph;
        Check(graph.GetMode() == Shader.Mode.CanvasItem, "Canvas is the initial authored mode.");
        using var input = new VisualShaderNodeInput { InputName = "uv" };
        using var first = new VisualShaderNodeVectorLen(); using var second = new VisualShaderNodeFloatOp();
        graph.AddNode(V.Type.Vertex, input, default, 2); graph.AddNode(V.Type.Vertex, first, default, 3); graph.AddNode(V.Type.Vertex, second, default, 4);
        graph.ConnectNodes(V.Type.Vertex, 2, 0, 3, 0); graph.ConnectNodes(V.Type.Vertex, 3, 0, 4, 0); graph.ConnectNodes(V.Type.Vertex, 4, 0, 0, 3);
        var events = 0; graph.Changed += _ => events++;
        graph.SetMode(Shader.Mode.Particles);
        Check(shader.GetMode() == Shader.Mode.Particles && events == 1, "Base Shader references observe authored mode and exactly one Changed.");
        Check(graph.GetNodeConnections(V.Type.Vertex).SequenceEqual(new[] { new VisualShaderConnection(3, 0, 4, 0) }), "Mode changes remove only boundary links in inactive stages.");
        graph.SetMode(Shader.Mode.Particles); Check(events == 1, "Equal mode is unchanged.");
        Check(graph.GetNode(V.Type.Start, 0) is VisualShaderNodeParticleOutput && graph.GetNode(V.Type.TextureBlit, 0) is VisualShaderNodeOutput, "Domain roots preserve their concrete roles.");
        using var clone = (V)graph.Duplicate(); Check(clone.GetMode() == graph.GetMode() && !ReferenceEquals(clone.GetNode(V.Type.Start, 0), graph.GetNode(V.Type.Start, 0)) && ReferenceEquals(clone.GetNode(V.Type.Vertex, 3), first), "Shallow duplicate owns fresh roots and borrows ordinary aliases.");
        foreach (var mode in new[] { Shader.Mode.CanvasItem, Shader.Mode.Particles, Shader.Mode.TextureBlit })
        {
            graph.SetMode(mode);
            var file = IOPath.Combine(IOPath.GetTempPath(), "e2d-domain-" + Guid.NewGuid().ToString("N") + ".e2dres");
            try
            {
                ResourceSaver.Save(graph, file, SaverFlags.BundleResources | SaverFlags.Compress | SaverFlags.SaveBigEndian);
                using var restored = ResourceLoader.Load<V>(file, ResourceLoader.CacheMode.IgnoreDeep);
                Check(restored.GetMode() == mode && restored.GetNodeConnections(V.Type.Vertex).Length == 1, "Mode, inactive source and links survive a fresh resource graph.");
                Check(restored.GetNode(V.Type.Start, 0) is VisualShaderNodeParticleOutput, "Stored roots retain the particle subtype.");
            }
            finally { System.IO.File.Delete(file); }
        }
        Reject<ArgumentOutOfRangeException>(() => graph.SetMode((Shader.Mode)0));
        Reject<ArgumentOutOfRangeException>(() => graph.GetNode((V.Type)2, 0));
        using var random = new VisualShaderNodeParticleRandomness();
        Check(random.GetInputPortDefaultValue<float>(1) == -1 && random.GetInputPortDefaultValue<float>(2) == 1, "Random range defaults are explicit.");
        random.OpType = VisualShaderFloatOpType.Vector4;
        Check(random.GetInputPortDefaultValue<Vector4>(1) == new Vector4(-1, -1, -1, -1), "Random range width converts every bound component.");
        random.ClearDefaultInputValues(); random.OpType = VisualShaderFloatOpType.Vector2;
        Check(random.GetInputPortDefaultValue<Vector2>(1) == Vector2.Zero && random.GetInputPortDefaultValue<Vector2>(2) == Vector2.Zero, "A missing changed-width bound is recreated as zero.");
        Reject<ArgumentOutOfRangeException>(() => random.OpType = VisualShaderFloatOpType.Max);
        using var emit = new VisualShaderNodeParticleEmit(); Check((int)emit.Flags == 31 && emit.GetInputPortDefaultValue<bool>(0) == true, "Subemission defaults enable the condition and all fields.");
        Reject<ArgumentOutOfRangeException>(() => emit.Flags = (ParticlesEmitFlags)32);
        Console.WriteLine("Domain authoring: modes, inactive links, independent roots, copied aliases, archives, widths and guards passed.");
    }
    internal static void AuthorDomains(string folder)
    {
        System.IO.Directory.CreateDirectory(folder);
        using (var graph = new V())
        {
            graph.SetMode(Shader.Mode.Particles);
            graph.GetNode(V.Type.Start, 0).SetInputPortDefaultValue(4, new Vector2(16, 16));
            graph.GetNode(V.Type.Start, 0).SetInputPortDefaultValue(2, new Vector3(.25f, .5f, .75f));
            SaveDomain(graph, folder, "particle-basic");
        }
        foreach (var kind in new[] { 0, 1, 2 })
        {
            using var graph = new V(); graph.SetMode(Shader.Mode.Particles);
            using VisualShaderNodeParticleEmitter node = kind switch { 0 => new VisualShaderNodeParticleSphereEmitter(), 1 => new VisualShaderNodeParticleBoxEmitter(), _ => new VisualShaderNodeParticleRingEmitter() };
            graph.AddNode(V.Type.Start, node, default, 2); graph.ConnectNodes(V.Type.Start, 2, 0, 0, 4); SaveDomain(graph, folder, "particle-shape-" + kind);
        }
        for (var width = 0; width < 4; width++)
        {
            using var graph = new V(); graph.SetMode(Shader.Mode.Particles);
            using var node = new VisualShaderNodeParticleRandomness { OpType = (VisualShaderFloatOpType)width };
            graph.AddNode(V.Type.StartCustom, node, default, 2); graph.ConnectNodes(V.Type.StartCustom, 2, 0, 0, 0); SaveDomain(graph, folder, "particle-random-" + width);
        }
        for (var outputs = 1; outputs <= 4; outputs++)
        {
            using var graph = new V(); graph.SetMode(Shader.Mode.TextureBlit);
            for (var i = 0; i < outputs; i++) graph.GetNode(V.Type.TextureBlit, 0).SetInputPortDefaultValue(i, new Vector4((i + 1) / 4f, .5f, .75f, 1));
            SaveDomain(graph, folder, "blit-" + outputs);
        }
        AuthorDomainCorpus(folder); AuthorInvalidDomains(folder);
        Console.WriteLine("Authored particle and writable-texture domain corpus: " + System.IO.Directory.GetFiles(folder, "*.e2dres").Length + " programs.");
    }
    private static void SaveDomain(V graph, string folder, string name) => ResourceSaver.Save(graph, IOPath.Combine(folder, name + ".e2dres"), SaverFlags.BundleResources | SaverFlags.Compress);
}
