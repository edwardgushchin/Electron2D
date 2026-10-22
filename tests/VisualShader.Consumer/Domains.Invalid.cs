using Electron2D;
using V = Electron2D.VisualShader;
using P = Electron2D.VisualShaderNode.PortType;
using Q = Electron2D.VisualShaderNodeParameter.QualifierEnum;

internal static partial class GraphChecks
{
    private static void AuthorInvalidDomains(string folder)
    {
        folder = System.IO.Path.Combine(folder, "invalid-domains"); Directory.CreateDirectory(folder);
        for (var kind = 0; kind < 8; kind++)
        {
            using var graph = new V(); var stage = kind == 6 ? V.Type.TextureBlit : kind == 7 ? V.Type.Fragment : V.Type.Process;
            graph.SetMode(kind == 6 ? Shader.Mode.TextureBlit : kind == 7 ? Shader.Mode.CanvasItem : Shader.Mode.Particles);
            using VisualShaderNode node = kind switch
            {
                0 => new VisualShaderNodeExpression(),
                1 => new VisualShaderNodeFloatParameter { ParameterName = "per_instance", Qualifier = Q.Instance },
                2 => new VisualShaderNodeTexture(),
                3 => new VisualShaderNodeDerivativeFunc(),
                4 => new VisualShaderNodeImageParameter { ParameterName = "screen_value", TextureSource = VisualShaderNodeTextureParameter.TextureSourceEnum.Screen },
                5 => new VisualShaderNodeExpression(),
                6 => new VisualShaderNodeFloatParameter { ParameterName = "SOURCE_TEXTURE0" },
                _ => new VisualShaderNodeParticleRandomness()
            };
            if (node is VisualShaderNodeExpression expression) { expression.AddOutputPort(0, P.Vector3, "value"); expression.Expression = kind == 0 ? "invalid source here;" : "discard; value=vec3(1);"; }
            graph.AddNode(stage, node, default, 2);
            if (kind == 4)
            {
                using var sample = new VisualShaderNodeTexture { Source = VisualShaderNodeTexture.SourceEnum.Port }; using var uv = new VisualShaderNodeVec2Constant();
                graph.AddNode(stage, sample, default, 3); graph.AddNode(stage, uv, default, 4); graph.ConnectNodes(stage, 2, 0, 3, 2); graph.ConnectNodes(stage, 4, 0, 3, 0); graph.ConnectNodes(stage, 3, 0, 0, 2);
                SaveDomain(graph, folder, "invalid-" + kind);
            }
            else { graph.ConnectNodes(stage, 2, 0, 0, stage == V.Type.Process ? 2 : 0); SaveDomain(graph, folder, "invalid-" + kind); }
        }
    }
}
