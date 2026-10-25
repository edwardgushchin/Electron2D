using Electron2D;
using V = Electron2D.VisualShader;
using P = Electron2D.VisualShaderNode.PortType;
using T = Electron2D.VisualShaderVaryingType;

internal static partial class GraphChecks
{
    internal static void AuthorLightGraphs(string folder)
    {
        Directory.CreateDirectory(folder);
        foreach (var name in new[] { "constant", "builtins", "fragment-varying", "vertex-varying" })
        {
            using var graph = new V(); using var expression = new VisualShaderNodeExpression();
            using var setter = new VisualShaderNodeVaryingSetter { VaryingName = "value", VaryingType = T.Vector4 };
            using var getter = new VisualShaderNodeVaryingGetter { VaryingName = "value", VaryingType = T.Vector4 };
            expression.AddOutputPort(0, P.Vector3, "rgb"); expression.AddOutputPort(1, P.Scalar, "alpha");
            expression.Expression = name == "builtins" ? "rgb=LIGHT_COLOR.rgb*LIGHT_ENERGY; alpha=LIGHT_COLOR.a;" : "rgb=vec3(.25,.5,.75); alpha=.5;";
            if (name.EndsWith("varying", StringComparison.Ordinal))
            {
                graph.AddVarying("value", name == "fragment-varying" ? V.VaryingMode.FragmentToLight : V.VaryingMode.VertexToFragmentLight, T.Vector4);
                graph.AddNode(name == "fragment-varying" ? V.Type.Fragment : V.Type.Vertex, setter, default, 2);
                setter.SetInputPortDefaultValue(0, new Vector4(.25f, .5f, .75f, .5f));
                graph.AddNode(V.Type.Light, getter, default, 2); expression.AddInputPort(0, P.Vector4, "value");
                expression.Expression = "rgb=value.rgb; alpha=value.a;";
                graph.AddNode(V.Type.Light, expression, default, 3); graph.ConnectNodes(V.Type.Light, 2, 0, 3, 0);
            }
            else graph.AddNode(V.Type.Light, expression, default, 3);
            graph.ConnectNodes(V.Type.Light, 3, 0, 0, 0); graph.ConnectNodes(V.Type.Light, 3, 1, 0, 1);
            SaveDomain(graph, folder, "light-" + name);
        }
        AuthorLightProbes(folder);
        AuthorInvalidLightGraphs(System.IO.Path.Combine(folder, "invalid"));
        Console.WriteLine("Authored per-light builtins, values, images, material scopes and both varying producer roles.");
    }

    private static void AuthorInvalidLightGraphs(string folder)
    {
        Directory.CreateDirectory(folder);
        foreach (var failure in new[] { "missing", "role", "vertex-read", "light-write", "cycle", "light-size", "fragment-light" })
        {
            using var graph = new V();
            using var getter = new VisualShaderNodeVaryingGetter { VaryingName = failure == "missing" ? "absent" : "value", VaryingType = failure == "role" ? T.Int : T.Float };
            using var setter = new VisualShaderNodeVaryingSetter { VaryingName = "value" };
            using var first = new VisualShaderNodeFloatOp(); using var second = new VisualShaderNodeFloatOp();
            using var expression = new VisualShaderNodeExpression();
            graph.AddVarying("value", V.VaryingMode.FragmentToLight, T.Float);
            if (failure == "light-write") graph.AddNode(V.Type.Light, setter, default, 2);
            else if (failure == "cycle")
            {
                graph.AddNode(V.Type.Light, first, default, 2); graph.AddNode(V.Type.Light, second, default, 3);
                graph.ConnectNodesForced(V.Type.Light, 2, 0, 3, 0); graph.ConnectNodesForced(V.Type.Light, 3, 0, 2, 0); graph.ConnectNodesForced(V.Type.Light, 2, 0, 0, 1);
            }
            else if (failure == "light-size")
            {
                expression.AddOutputPort(0, P.Vector3, "rgb"); expression.Expression = "rgb=vec3(POINT_SIZE);";
                graph.AddNode(V.Type.Light, expression, default, 2); graph.ConnectNodesForced(V.Type.Light, 2, 0, 0, 0);
            }
            else if (failure == "fragment-light")
            {
                expression.AddOutputPort(0, P.Vector3, "rgb"); expression.Expression = "rgb=LIGHT_COLOR.rgb;";
                graph.AddNode(V.Type.Fragment, expression, default, 2); graph.ConnectNodes(V.Type.Fragment, 2, 0, 0, 0);
                graph.GetNode(V.Type.Light, 0).SetInputPortDefaultValue(1, 1f);
            }
            else
            {
                var stage = failure == "vertex-read" ? V.Type.Vertex : V.Type.Light;
                graph.AddNode(stage, getter, default, 2); graph.ConnectNodesForced(stage, 2, 0, 0, stage == V.Type.Vertex ? 3 : 1);
            }
            SaveDomain(graph, folder, "light-" + failure);
        }
    }
}
