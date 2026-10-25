using Electron2D;
using V = Electron2D.VisualShader;
using P = Electron2D.VisualShaderNode.PortType;
using T = Electron2D.VisualShaderVaryingType;
using IOPath = System.IO.Path;

internal static partial class GraphChecks
{
    private static readonly string[] VaryingGraphs = Enumerable.Range(0, 8).SelectMany(i => new[] { "varying-" + i, "default-" + i, "none-" + i }).Concat(["capacity", "multiple"]).ToArray();
    private static string VaryingValue(T type) => type switch
    {
        T.Float => "UV.x*.5+.125",
        T.Int => "-17",
        T.UInt => "4294967291u",
        T.Vector2 => "UV*vec2(.5,.25)+vec2(.125,.25)",
        T.Vector3 => "vec3(UV*.5,.375)",
        T.Vector4 => "vec4(UV*.5,.375,.625)",
        T.Boolean => "true",
        _ => "mat4(vec4(UV,2,3),vec4(4,5,6,7),vec4(8,9,10,11),vec4(12,13,14,15))"
    };
    private static string AnimatedVarying(T type) => type switch { T.Int => "int(factor*100.0)-117", T.UInt => "4294967191u+uint(factor*100.0)", T.Boolean => "(factor>.75)", _ => "(" + VaryingValue(type) + ")*factor" };
    private static string VaryingEqual(T type, string a, string b) => type is T.Int or T.UInt or T.Boolean ? $"({a}=={b})" : type == T.Float ? $"abs({a}-({b}))<.0001" : type == T.Transform ? string.Join(" && ", Enumerable.Range(0, 4).Select(i => $"all(lessThan(abs({a}[{i}]-({b})[{i}]),vec4(.0001)))")) : $"all(lessThan(abs({a}-({b})),{(type == T.Vector2 ? "vec2" : type == T.Vector3 ? "vec3" : "vec4")}(.0001)))";
    internal static void AuthorVaryings(string folder)
    {
        Directory.CreateDirectory(folder); Directory.CreateDirectory(IOPath.Combine(folder, "invalid"));
        foreach (var type in Enum.GetValues<T>().Where(t => t != T.Max))
            for (var mode = 0; mode < 3; mode++) SaveVarying(folder, type, mode);
        SaveVarying(folder, T.Float, 3); SaveVarying(folder, T.Float, 4);
        foreach (var failure in new[] { "missing", "role", "stage", "capacity", "particle", "blit", "cycle" })
        {
            using var graph = new V(); var owned = new List<Resource>();
            void Add(VisualShaderNode node, V.Type stage, int id) { owned.Add(node); graph.AddNode(stage, node, default, id); }
            try
            {
                graph.AddVarying("value", default, T.Float);
                if (failure is "particle" or "blit") graph.SetMode(failure == "particle" ? Shader.Mode.Particles : Shader.Mode.TextureBlit);
                var stage = failure == "stage" ? V.Type.Fragment : failure == "particle" ? V.Type.Process : failure == "blit" ? V.Type.TextureBlit : V.Type.Vertex;
                Add(new VisualShaderNodeVaryingSetter { VaryingName = failure == "missing" ? "absent" : "value", VaryingType = failure == "role" ? T.Int : T.Float }, stage, 2);
                if (failure == "capacity")
                    for (var i = 0; i < 4; i++) { graph.AddVarying("extra" + i, default, T.Float); Add(new VisualShaderNodeVaryingSetter { VaryingName = "extra" + i }, stage, i + 3); }
                if (failure == "cycle")
                {
                    Add(new VisualShaderNodeFloatOp(), stage, 3); Add(new VisualShaderNodeFloatOp(), stage, 4);
                    graph.ConnectNodesForced(stage, 3, 0, 4, 0); graph.ConnectNodesForced(stage, 4, 0, 3, 0); graph.ConnectNodesForced(stage, 3, 0, 2, 0);
                }
                SaveDomain(graph, IOPath.Combine(folder, "invalid"), failure);
            }
            finally { foreach (var resource in owned) resource.Dispose(); }
        }
        AuthorVaryingSources(folder);
        Console.WriteLine("Authored 26 varying graphs, two paired source programs and nine invalid transports.");
    }
    private static void SaveVarying(string folder, T type, int mode)
    {
        using var graph = new V(); var owned = new List<Resource>();
        int Add(VisualShaderNode node, V.Type stage, int id) { owned.Add(node); graph.AddNode(stage, node, default, id); return id; }
        try
        {
            if (mode != 2) graph.AddVarying("value", default, type);
            var getter = new VisualShaderNodeVaryingGetter { VaryingName = mode == 2 ? "[None]" : "value", VaryingType = type };
            var getterID = Add(getter, V.Type.Fragment, 2);
            var expression = new VisualShaderNodeExpression(); expression.AddInputPort(0, (P)type, "transported"); expression.AddInputPort(1, P.Scalar, "factor"); expression.AddOutputPort(0, P.Vector3, "observed");
            var expected = mode is 1 or 2 ? type switch { T.Int => "0", T.UInt => "0u", T.Boolean => "false", T.Float => "0.0", T.Vector2 => "vec2(0)", T.Vector3 => "vec3(0)", T.Vector4 => "vec4(0)", _ => "mat4(1)" } : mode == 4 ? ".75" : AnimatedVarying(type);
            var valid = VaryingEqual(type, "transported", expected);
            expression.Expression = "observed=(" + valid + ")?vec3(UV*.5+.125,.625)*factor:vec3(1,0,0);";
            var expressionID = Add(expression, V.Type.Fragment, 3); var gain = new VisualShaderNodeFloatParameter { ParameterName = "gain", DefaultValueEnabled = true, DefaultValue = 1 }; Add(gain, V.Type.Fragment, 4);
            graph.ConnectNodes(V.Type.Fragment, getterID, 0, expressionID, 0); graph.ConnectNodes(V.Type.Fragment, 4, 0, expressionID, 1); graph.ConnectNodes(V.Type.Fragment, expressionID, 0, 0, 0);
            if (mode is 0 or 3 or 4)
            {
                var setterID = Add(new VisualShaderNodeVaryingSetter { VaryingName = "value", VaryingType = type }, V.Type.Vertex, 2);
                var produce = new VisualShaderNodeExpression(); produce.AddInputPort(0, P.Scalar, "factor"); produce.AddOutputPort(0, (P)type, "exported"); produce.Expression = "exported=" + (mode == 4 ? ".25" : AnimatedVarying(type)) + ";";
                Add(produce, V.Type.Vertex, 3); graph.ConnectNodes(V.Type.Vertex, 3, 0, setterID, 0);
                graph.AddNode(V.Type.Vertex, gain, default, 10); graph.ConnectNodes(V.Type.Vertex, 10, 0, 3, 0);
                if (mode == 4)
                { var last = new VisualShaderNodeVaryingSetter { VaryingName = "value" }; last.SetInputPortDefaultValue(0, .75f); Add(last, V.Type.Vertex, 4); }
            }
            if (mode == 3)
            {
                for (var i = 0; i < 3; i++)
                {
                    var extraName = "extra" + i; graph.AddVarying(extraName, default, T.Float); var setter = new VisualShaderNodeVaryingSetter { VaryingName = extraName }; setter.SetInputPortDefaultValue(0, .375f); Add(setter, V.Type.Vertex, i + 5);
                    Add(new VisualShaderNodeVaryingGetter { VaryingName = extraName }, V.Type.Fragment, i + 5); expression.AddInputPort(i + 2, P.Scalar, extraName); graph.ConnectNodes(V.Type.Fragment, i + 5, 0, expressionID, i + 2);
                    valid += " && " + extraName + "==.375";
                }
                expression.Expression = "observed=(" + valid + ")?vec3(UV*.5+.125,.625)*factor:vec3(1,0,0);";
            }
            for (var i = 0; i < 8; i++) graph.AddVarying("unused" + i, default, T.Transform);
            Add(new VisualShaderNodeGlobalExpression { Expression = "float e2d_varying_0=13.0;" }, V.Type.Vertex, 20);
            var name = mode == 3 ? "capacity" : mode == 4 ? "multiple" : (mode == 0 ? "varying-" : mode == 1 ? "default-" : "none-") + (int)type;
            SaveDomain(graph, folder, name);
            using var restored = ResourceLoader.Load<V>(IOPath.Combine(folder, name + ".e2dres"), ResourceLoader.CacheMode.IgnoreDeep);
            Check(restored.HasVarying("unused7") && (mode == 2 || restored.HasVarying("value")) && restored.GetNode(V.Type.Fragment, 2) is VisualShaderNodeVaryingGetter g && g.VaryingType == type, "Portable varying declarations, source nodes and numeric roles reconstruct.");
        }
        finally { foreach (var resource in owned) resource.Dispose(); }
    }
}
