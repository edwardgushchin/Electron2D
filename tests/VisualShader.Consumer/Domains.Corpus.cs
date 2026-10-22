using Electron2D;
using V = Electron2D.VisualShader;
using P = Electron2D.VisualShaderNode.PortType;

internal static partial class GraphChecks
{
    private static readonly V.Type[] ParticleStages = [V.Type.Start, V.Type.Process, V.Type.Collide, V.Type.StartCustom, V.Type.ProcessCustom];
    private static int ColorPort(V.Type stage) => stage is V.Type.StartCustom or V.Type.ProcessCustom ? 3 : 2;
    private static void AuthorDomainCorpus(string folder)
    {
        AuthorDomainIntegration(folder);
        foreach (var stage in ParticleStages)
        {
            for (var kind = 0; kind < 7; kind++)
            {
                using var graph = new V(); graph.SetMode(Shader.Mode.Particles);
                using VisualShaderNode node = kind switch
                {
                    0 => new VisualShaderNodeParticleConeVelocity(),
                    1 or 2 => new VisualShaderNodeParticleMultiplyByAxisAngle { DegreesMode = kind == 1 },
                    3 or 4 or 5 => new VisualShaderNodeParticleAccelerator { Mode = (VisualShaderNodeParticleAccelerator.ModeEnum)(kind - 3) },
                    _ => new VisualShaderNodeParticleRandomness()
                };
                if (kind == 0) node.SetInputPortDefaultValue(1, 0f);
                if (kind is 1 or 2) { node.SetInputPortDefaultValue(0, new Vector3(1, 0, 0)); node.SetInputPortDefaultValue(1, new Vector3(0, 0, 1)); node.SetInputPortDefaultValue(2, kind == 1 ? 90f : MathF.PI / 2); }
                if (kind == 5) node.SetInputPortDefaultValue(2, new Vector3(0, 0, 1));
                if (kind == 6) { node.SetInputPortDefaultValue(0, 0u); node.SetInputPortDefaultValue(1, .25f); node.SetInputPortDefaultValue(2, .25f); }
                graph.AddNode(stage, node, default, 2); graph.ConnectNodes(stage, 2, 0, 0, ColorPort(stage));
                SaveDomain(graph, folder, "particle-math-" + (int)stage + "-" + kind);
            }
            foreach (var name in new[] { "active", "color", "custom", "delta", "emission_transform", "index", "lifetime", "number", "random_seed", "time", "transform", "velocity" })
            {
                using var graph = new V(); graph.SetMode(Shader.Mode.Particles); using var input = new VisualShaderNodeInput { InputName = name };
                graph.AddNode(stage, input, default, 2);
                if (name is "transform" or "emission_transform")
                {
                    using var determinant = new VisualShaderNodeDeterminant(); graph.AddNode(stage, determinant, default, 3);
                    graph.ConnectNodes(stage, 2, 0, 3, 0); graph.ConnectNodes(stage, 3, 0, 0, ColorPort(stage));
                    SaveDomain(graph, folder, "particle-input-" + (int)stage + "-" + name);
                }
                else { graph.ConnectNodes(stage, 2, 0, 0, ColorPort(stage)); SaveDomain(graph, folder, "particle-input-" + (int)stage + "-" + name); }
            }
            using (var graph = new V())
            using (var expression = new VisualShaderNodeExpression())
            {
                graph.SetMode(Shader.Mode.Particles); expression.AddOutputPort(0, P.Vector3, "value"); expression.Expression = "value=vec3(.25,.5,.75); return;";
                graph.AddNode(stage, expression, default, 2); graph.ConnectNodes(stage, 2, 0, 0, ColorPort(stage)); SaveDomain(graph, folder, "particle-expression-" + (int)stage);
            }
        }
        for (var flags = 0; flags < 32; flags++)
        {
            using var graph = new V(); graph.SetMode(Shader.Mode.Particles); using var emit = new VisualShaderNodeParticleEmit { Flags = (ParticlesEmitFlags)flags };
            graph.AddNode(V.Type.Start, emit, default, 2); SaveDomain(graph, folder, "particle-emit-" + flags);
        }
        for (var kind = 0; kind < 5; kind++)
        {
            using var graph = new V(); graph.SetMode(Shader.Mode.Particles); using var mesh = new ArrayMesh();
            if (kind != 0)
            {
                var count = kind == 4 ? 4097 : 1;
                mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Points, new() { Vertices = Enumerable.Repeat(new Vector2(2, 0), count).ToArray(), Normals = Enumerable.Repeat(new Vector3(.2f, .3f, 1), count).ToArray(), Colors = Enumerable.Repeat(new Color(.25f, .5f, .75f, 1), count).ToArray(), UVs = Enumerable.Repeat(new Vector2(.1f, .2f), count).ToArray(), UV2s = Enumerable.Repeat(new Vector2(.6f, .7f), count).ToArray() });
                mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Points, new() { Vertices = [new(3, 4)] });
            }
            using var emitter = new VisualShaderNodeParticleMeshEmitter { Mesh = kind == 0 ? null : mesh, UseAllSurfaces = kind == 3, SurfaceIndex = kind == 2 ? 100 : 0 };
            graph.AddNode(V.Type.Start, emitter, default, 2); graph.ConnectNodes(V.Type.Start, 2, 0, 0, 4); graph.ConnectNodes(V.Type.Start, 2, 2, 0, 2); graph.ConnectNodes(V.Type.Start, 2, 3, 0, 3);
            graph.AddNode(V.Type.StartCustom, emitter, default, 2); graph.ConnectNodes(V.Type.StartCustom, 2, 1, 0, 2); graph.ConnectNodes(V.Type.StartCustom, 2, 4, 0, 0);
            using var decompose = new VisualShaderNodeVectorDecompose { OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector2 };
            graph.AddNode(V.Type.StartCustom, decompose, default, 3); graph.ConnectNodes(V.Type.StartCustom, 2, 5, 3, 0); graph.ConnectNodes(V.Type.StartCustom, 3, 0, 0, 1);
            SaveDomain(graph, folder, "particle-mesh-" + kind);
        }
        using (var graph = new V())
        using (var number = new VisualShaderNodeInput { InputName = "number" })
        {
            graph.SetMode(Shader.Mode.Particles); graph.AddNode(V.Type.Start, number, default, 2); graph.ConnectNodes(V.Type.Start, 2, 0, 0, 2);
            graph.GetNode(V.Type.Process, 0).SetInputPortDefaultValue(0, false); SaveDomain(graph, folder, "particle-number");
        }
        using (var graph = new V())
        {
            graph.SetMode(Shader.Mode.TextureBlit); SaveDomain(graph, folder, "blit-empty");
        }
        for (var index = 0; index < 4; index++)
        {
            using var graph = new V(); graph.SetMode(Shader.Mode.TextureBlit);
            using var input = new VisualShaderNodeInput { InputName = "source_texture" + index }; using var sampler = new VisualShaderNodeTexture { Source = VisualShaderNodeTexture.SourceEnum.Port };
            graph.AddNode(V.Type.TextureBlit, input, default, 2); graph.AddNode(V.Type.TextureBlit, sampler, default, 3); graph.ConnectNodes(V.Type.TextureBlit, 2, 0, 3, 2); graph.ConnectNodes(V.Type.TextureBlit, 3, 0, 0, 0);
            SaveDomain(graph, folder, "blit-source-" + index);
        }
    }
}
