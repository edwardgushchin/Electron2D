using Electron2D;
using V = Electron2D.VisualShader;
using P = Electron2D.VisualShaderNode.PortType;
using Q = Electron2D.VisualShaderNodeParameter.QualifierEnum;
using M4 = System.Numerics.Matrix4x4;

internal static partial class GraphChecks
{
    private static void AuthorDomainIntegration(string folder)
    {
        foreach (var stage in ParticleStages.Append(V.Type.TextureBlit))
        {
            var prefix = stage == V.Type.TextureBlit ? "blit" : "particle";
            V NewGraph() { var graph = new V(); graph.SetMode(stage == V.Type.TextureBlit ? Shader.Mode.TextureBlit : Shader.Mode.Particles); return graph; }
            VisualShaderNodeExpression? Connect(V graph, int id)
            {
                if (stage != V.Type.TextureBlit) { graph.ConnectNodes(stage, id, 0, 0, ColorPort(stage)); return null; }
                var rgba = new VisualShaderNodeExpression(); rgba.AddInputPort(0, P.Vector3, "value"); rgba.AddOutputPort(0, P.Vector4, "color_value"); rgba.Expression = "color_value=vec4(value,1.0);";
                graph.AddNode(stage, rgba, default, 20); graph.ConnectNodes(stage, id, 0, 20, 0); graph.ConnectNodes(stage, 20, 0, 0, 0); return rgba;
            }
            for (var role = 0; role < 8; role++)
                for (var custom = 0; custom < 2; custom++)
                {
                    using var graph = NewGraph(); using var parameter = ExpressionParameter((P)role); ParameterEnabled(parameter, true); parameter.ParameterName = "domain_value";
                    using var node = new CustomNumericNode { Role = (P)role, InputMode = 2 }; using var observe = new VisualShaderNodeExpression();
                    observe.AddInputPort(0, (P)role, "value"); observe.AddOutputPort(0, P.Vector3, "observed"); observe.Expression = "observed=" + ExpressionObservation((P)role) + ";";
                    graph.AddNode(stage, parameter, default, 2); graph.AddNode(stage, observe, default, 4);
                    if (custom != 0) { graph.AddNode(stage, node, default, 3); graph.ConnectNodes(stage, 2, 0, 3, 0); graph.ConnectNodes(stage, 3, 0, 4, 0); }
                    else graph.ConnectNodes(stage, 2, 0, 4, 0);
                    using var boundary = Connect(graph, 4); SaveDomain(graph, folder, $"{prefix}-parameter-{(int)stage}-{role}-{custom}");
                }
            for (var kind = 0; kind < 3; kind++)
            {
                using var graph = NewGraph(); using var node = new CustomProcedureNode { Kind = kind == 0 ? 0 : kind == 1 ? 6 : stage == V.Type.TextureBlit ? 8 : 10 };
                graph.AddNode(stage, node, default, 2); using var boundary = Connect(graph, 2); SaveDomain(graph, folder, $"{prefix}-custom-{(int)stage}-{kind}");
            }
            for (var array = 0; array < 2; array++)
            {
                using var graph = NewGraph(); using var pixels = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); pixels.Fill(new(64f / 255, 128f / 255, 192f / 255, 1));
                using var image = ImageTexture.CreateFromImage(pixels); using var layers = new TextureArray(); layers.CreateFromImages([pixels]);
                using VisualShaderNode sample = array == 0 ? new VisualShaderNodeTexture { Texture = image } : new VisualShaderNodeTextureArray { TextureArray = layers };
                using VisualShaderNode uv = array == 0 ? new VisualShaderNodeVec2Constant { Constant = new(.5f, .5f) } : new VisualShaderNodeVec3Constant { Constant = new(.5f, .5f, 0) };
                graph.AddNode(stage, uv, default, 3); graph.AddNode(stage, sample, default, 2); graph.ConnectNodes(stage, 3, 0, 2, 0); using var boundary = Connect(graph, 2); SaveDomain(graph, folder, $"{prefix}-image-{(int)stage}-{array}");
            }
            using (var graph = NewGraph())
            using (var global = new VisualShaderNodeVec3Parameter { ParameterName = "domain_global", Qualifier = Q.Global })
            {
                graph.AddNode(stage, global, default, 2); using var boundary = Connect(graph, 2); SaveDomain(graph, folder, $"{prefix}-global-{(int)stage}");
            }
            using (var graph = NewGraph())
            using (var global = new VisualShaderNodeGlobalExpression { Expression = "vec3 domain_shared(){return vec3(.25,.5,.75);}" })
            using (var local = new VisualShaderNodeExpression())
            {
                local.AddOutputPort(0, P.Vector3, "observed"); local.Expression = "observed=domain_shared()+vec3(TIME*0.0);";
                graph.AddNode(stage, global, default, 2); graph.AddNode(stage, local, default, 3); using var boundary = Connect(graph, 3); SaveDomain(graph, folder, $"{prefix}-shared-{(int)stage}");
            }
        }
        foreach (var stage in new[] { V.Type.Start, V.Type.Process, V.Type.StartCustom, V.Type.ProcessCustom })
        {
            using var graph = new V(); graph.SetMode(Shader.Mode.Particles); using var restart = new VisualShaderNodeInput { InputName = "restart" };
            graph.AddNode(stage, restart, default, 2); graph.ConnectNodes(stage, 2, 0, 0, ColorPort(stage)); SaveDomain(graph, folder, "particle-restart-" + (int)stage);
        }
        for (var kind = 0; kind < 3; kind++)
        {
            using var graph = new V(); graph.SetMode(Shader.Mode.Particles); using var input = new VisualShaderNodeInput { InputName = kind == 0 ? "collision_normal" : "collision_depth" };
            graph.AddNode(V.Type.Collide, input, default, 2); graph.ConnectNodes(V.Type.Collide, 2, 0, 0, 2);
            graph.GetNode(V.Type.Process, 0).SetInputPortDefaultValue(2, new Vector3(.25f, .5f, .75f));
            graph.GetNode(V.Type.ProcessCustom, 0).SetInputPortDefaultValue(0, new Vector3(.2f, .3f, .4f));
            SaveDomain(graph, folder, "particle-collision-" + kind);
        }
        for (var kind = 0; kind < 3; kind++)
        {
            using var graph = new V(); graph.SetMode(Shader.Mode.Particles);
            var stage = kind == 0 ? V.Type.Start : kind == 1 ? V.Type.Process : V.Type.ProcessCustom;
            var root = graph.GetNode(stage, 0);
            if (kind == 2) root.SetInputPortDefaultValue(5, new M4(2, 4, 9, 21, 3, 5, 8, 22, 7, 6, 1, 23, 0, 0, 0, 1));
            else { root.SetInputPortDefaultValue(kind == 0 ? 5 : 4, 2f); root.SetInputPortDefaultValue(kind == 0 ? 6 : 5, MathF.PI / 2); }
            SaveDomain(graph, folder, "particle-transform-" + kind);
        }
        for (var kind = 0; kind < 2; kind++)
        {
            using var graph = new V(); graph.SetMode(Shader.Mode.Particles); using var emit = new VisualShaderNodeParticleEmit();
            emit.SetInputPortDefaultValue(0, kind != 0); emit.SetInputPortDefaultValue(1, new M4(1, 0, 0, 7, 0, 1, 0, 8, 0, 0, 1, 9, 0, 0, 0, 1)); emit.SetInputPortDefaultValue(2, new Vector2(3, 4)); emit.SetInputPortDefaultValue(3, new Vector3(.25f, .5f, .75f)); emit.SetInputPortDefaultValue(5, new Vector3(.6f, .7f, .8f)); emit.SetInputPortDefaultValue(6, .9f);
            graph.AddNode(V.Type.Start, emit, default, 2); SaveDomain(graph, folder, "particle-payload-" + kind);
        }
    }
}
