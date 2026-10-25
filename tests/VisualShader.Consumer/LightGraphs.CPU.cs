using Electron2D;
using V = Electron2D.VisualShader;
using P = Electron2D.VisualShaderNode.PortType;
using T = Electron2D.VisualShaderVaryingType;

internal static partial class GraphChecks
{
    internal static void LightGraphsCPU()
    {
        var light = V.Type.Light;
        using var graph = new V();
        var root = graph.GetNode(light, 0);
        Check(root is VisualShaderNodeOutput, "The light stage owns its reserved output.");
        root.SetInputPortDefaultValue(0, Vector3.One); root.SetInputPortDefaultValue(1, .75f); root.SetInputPortDefaultValue(2, Vector3.One);
        Reject<ArgumentException>(() => root.SetInputPortDefaultValue(0, .5f));
        Reject<ArgumentException>(() => root.SetInputPortDefaultValue(1, Vector3.One));
        Reject<ArgumentOutOfRangeException>(() => root.SetInputPortDefaultValue(3, .5f));
        graph.AddVarying("from_fragment", V.VaryingMode.FragmentToLight, T.Vector4);
        using var setter = new VisualShaderNodeVaryingSetter { VaryingName = "from_fragment", VaryingType = T.Vector4 };
        using var getter = new VisualShaderNodeVaryingGetter { VaryingName = "from_fragment", VaryingType = T.Vector4 };
        graph.AddNode(V.Type.Fragment, setter, default, 2); graph.AddNode(light, getter, default, 2);
        using var input = new VisualShaderNodeInput();
        var changes = 0; input.InputTypeChanged += () => changes++;
        foreach (var pair in new[]
        {
            ("light", P.Vector4), ("light_color", P.Vector4), ("light_direction", P.Vector3),
            ("light_energy", P.Scalar), ("light_is_directional", P.Boolean), ("light_position", P.Vector3),
            ("light_vertex", P.Vector3), ("normal", P.Vector3), ("shadow", P.Vector4), ("specular_shininess", P.Vector4)
        })
        {
            input.InputName = pair.Item1;
            var symbol = pair.Item1 == "shadow" ? "SHADOW_MODULATE" : pair.Item1.ToUpperInvariant();
            Check(input.GetInputRealName() == symbol, "Light source symbol: " + pair.Item1);
        }
        Check(changes == 6, "Light input notifications follow value-role transitions.");
        graph.AddNode(light, input, default, 3); graph.ConnectNodes(light, 3, 0, 0, 0);
        root.SetInputPortDefaultValue(1, .75f);
        using var shallow = (V)graph.Duplicate(); using var deep = (V)graph.DuplicateDeep();
        foreach (var copy in new[] { shallow, deep })
            Check(copy.HasVarying("from_fragment") && copy.GetNode(light, 0).GetInputPortDefaultValue<float>(1) == .75f &&
                  copy.GetNodeConnections(light).Length == 1, "Light graph copies retain declarations, typed defaults and connections.");
        graph.SetMode(Shader.Mode.Particles); graph.SetMode(Shader.Mode.CanvasItem);
        Check(graph.HasVarying("from_fragment") && graph.GetNodeList(light).SequenceEqual(new[] { 0, 2, 3 }), "Mode changes retain inactive light source definitions.");
        Console.WriteLine("LIGHT GRAPHS CPU PASS: stage/output roles, input names/events and retained copied source state.");
    }
}
