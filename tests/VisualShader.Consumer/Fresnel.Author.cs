using Electron2D;
using System.Globalization;
using System.Text;
using V = Electron2D.VisualShader;
using P = Electron2D.VisualShaderNode.PortType;

internal static partial class GraphChecks
{
    private readonly record struct FresnelCase(Vector3 Normal, Vector3 View, bool Invert, float Power);
    private static readonly FresnelCase[] FresnelCases =
    [
        new(new(0, 0, 1), new(0, 0, .25f), false, 2),
        new(new(0, 0, 2), new(0, 0, 2), false, 1),
        new(new(0, 0, 1), new(0, 0, -1), true, 2),
        new(new(2, -1, .5f), new(.25f, .125f, .5f), false, .5f),
        new(new(2, -1, .5f), new(.25f, .125f, .5f), true, 3),
        new(Vector3.Zero, new(1, 2, 3), false, 2),
        new(new(0, 0, 1), new(0, 0, .5f), false, 0),
        new(new(0, 0, 1), new(0, 0, .5f), true, -2),
        new(new(1, 0, 0), new(-2, 0, 0), false, 1),
        new(new(0, 1, 0), new(0, 1, 0), true, 2)
    ];
    private static readonly FresnelCase FresnelAlternate = new(new(.5f, .25f, -.5f), new(.5f, 1, -.5f), true, .5f);
    private static float FresnelExpected(FresnelCase value)
    {
        var dot = Math.Clamp(value.Normal.X * value.View.X + value.Normal.Y * value.View.Y + value.Normal.Z * value.View.Z, 0, 1);
        return MathF.Pow(value.Invert ? dot : 1 - dot, value.Power);
    }
    private static string GLSLFloat(float value) => value.ToString("R", CultureInfo.InvariantCulture) + (value == MathF.Truncate(value) ? ".0" : "");
    internal static void AuthorFresnel(string folder)
    {
        Directory.CreateDirectory(folder); Directory.CreateDirectory(System.IO.Path.Combine(folder, "invalid"));
        foreach (var stage in ParticleStages.Concat([V.Type.Vertex, V.Type.Fragment, V.Type.TextureBlit])) SaveFresnelStage(folder, stage);
        SaveFresnelNormal(folder);
        using (var graph = new V())
        using (var value = new VisualShaderNodeFresnel())
        {
            graph.AddNode(V.Type.Vertex, value, default, 2); graph.ConnectNodes(V.Type.Vertex, 2, 0, 0, 2);
            SaveDomain(graph, System.IO.Path.Combine(folder, "invalid"), "invalid-fresnel-vertex");
        }
    }
    private static void SaveFresnelStage(string folder, V.Type stage)
    {
        using var graph = new V(); graph.SetMode(stage == V.Type.TextureBlit ? Shader.Mode.TextureBlit : stage is V.Type.Vertex or V.Type.Fragment ? Shader.Mode.CanvasItem : Shader.Mode.Particles);
        var owned = new List<Resource>(); var id = 2; var port = 0; var results = new List<string>(); var checks = new List<string>();
        T Own<T>(T value) where T : Resource { owned.Add(value); return value; }
        int Add(VisualShaderNode value) { var next = id++; graph.AddNode(stage, value, default, next); return next; }
        void Connect(VisualShaderNode value, int target, int input) => graph.ConnectNodes(stage, Add(value), 0, target, input);
        var observe = Own(new VisualShaderNodeExpression()); var observeID = Add(observe);
        string Input(int source) { var name = "sample" + port; observe.AddInputPort(port, P.Scalar, name); graph.ConnectNodes(stage, source, 0, observeID, port++); return name; }
        try
        {
            var shared = Own(new VisualShaderNodeFresnel());
            for (var sample = 0; sample < FresnelCases.Length; sample++)
            {
                var value = FresnelCases[sample]; var node = sample == 3 ? Own(new VisualShaderNodeFresnel()) : shared; var nodeID = Add(node);
                if (sample == 0)
                {
                    Connect(Own(new VisualShaderNodeVec3Parameter { ParameterName = "fresnel_normal", DefaultValueEnabled = true, DefaultValue = value.Normal }), nodeID, 0);
                    Connect(Own(new VisualShaderNodeVec3Parameter { ParameterName = "fresnel_view", DefaultValueEnabled = true, DefaultValue = value.View }), nodeID, 1);
                    Connect(Own(new VisualShaderNodeBooleanParameter { ParameterName = "fresnel_invert", DefaultValueEnabled = true, DefaultValue = value.Invert }), nodeID, 2);
                    Connect(Own(new VisualShaderNodeFloatParameter { ParameterName = "fresnel_power", DefaultValueEnabled = true, DefaultValue = value.Power }), nodeID, 3);
                }
                else if (sample == 3)
                { node.SetInputPortDefaultValue(0, value.Normal); node.SetInputPortDefaultValue(1, value.View); node.SetInputPortDefaultValue(2, value.Invert); node.SetInputPortDefaultValue(3, value.Power); }
                else
                {
                    Connect(Own(new VisualShaderNodeVec3Constant { Constant = value.Normal }), nodeID, 0);
                    Connect(Own(new VisualShaderNodeVec3Constant { Constant = value.View }), nodeID, 1);
                    Connect(Own(new VisualShaderNodeBooleanConstant { Constant = value.Invert }), nodeID, 2);
                    Connect(Own(new VisualShaderNodeFloatConstant { Constant = value.Power }), nodeID, 3);
                }
                var actual = Input(nodeID); results.Add(actual);
                var expected = sample == 0 ? Input(Add(Own(new VisualShaderNodeFloatParameter { ParameterName = "fresnel_expected", DefaultValueEnabled = true, DefaultValue = FresnelExpected(value) }))) : GLSLFloat(FresnelExpected(value));
                checks.Add($"abs({actual}-({expected})) < .0002");
            }
            var absent = Own(new VisualShaderNodeFresnel());
            if (stage == V.Type.Vertex) absent.SetInputPortDefaultValue(0, Vector3.Zero);
            var defaultResult = Input(Add(absent)); results.Add(defaultResult); checks.Add($"{defaultResult} == 1.0");
            var code = new StringBuilder("bool valid=true;\n"); foreach (var check in checks) code.Append("valid=valid && (").Append(check).AppendLine(");");
            observe.AddOutputPort(0, stage == V.Type.TextureBlit ? P.Vector4 : P.Vector3, "observed");
            if (stage == V.Type.TextureBlit)
            {
                code.Append("int pixel=int(UV.x*").Append(results.Count).AppendLine(".0);float raw=0.0;");
                for (var sample = 0; sample < results.Count; sample++) code.Append("if(pixel==").Append(sample).Append(")raw=").Append(results[sample]).AppendLine(";");
                code.AppendLine("observed=vec4(raw,valid?1.0:0.0,0,1);"); graph.ConnectNodes(stage, observeID, 0, 0, 0);
            }
            else
            { code.AppendLine("observed=valid?vec3(.25,.5,.75):vec3(1,0,0);"); graph.ConnectNodes(stage, observeID, 0, 0, stage == V.Type.Vertex ? 2 : stage == V.Type.Fragment ? 0 : ColorPort(stage)); }
            observe.Expression = code.ToString();
            SaveDomain(graph, folder, (stage == V.Type.TextureBlit ? "blit" : stage is V.Type.Vertex or V.Type.Fragment ? "canvas" : "particle") + "-fresnel-" + (int)stage);
        }
        finally { for (var i = owned.Count - 1; i >= 0; i--) owned[i].Dispose(); }
    }
    private static void SaveFresnelNormal(string folder)
    {
        using var graph = new V(); using var value = new VisualShaderNodeFresnel();
        using var view = new VisualShaderNodeVec3Parameter { ParameterName = "normal_view", DefaultValueEnabled = true, DefaultValue = new(.25f, -.125f, .5f) };
        using var power = new VisualShaderNodeFloatParameter { ParameterName = "normal_power", DefaultValueEnabled = true, DefaultValue = 2 };
        using var invert = new VisualShaderNodeBooleanParameter { ParameterName = "normal_invert", DefaultValueEnabled = true, DefaultValue = false };
        using var observe = new VisualShaderNodeExpression(); observe.AddInputPort(0, P.Scalar, "value"); observe.AddOutputPort(0, P.Vector3, "observed"); observe.Expression = "observed=vec3(value);";
        using var collision = new VisualShaderNodeGlobalExpression { Expression = "float e2d_normal=13.0;" };
        graph.AddNode(V.Type.Fragment, value, default, 2); graph.AddNode(V.Type.Fragment, view, default, 3); graph.AddNode(V.Type.Fragment, power, default, 4); graph.AddNode(V.Type.Fragment, invert, default, 5); graph.AddNode(V.Type.Fragment, observe, default, 6); graph.AddNode(V.Type.Fragment, collision, default, 7);
        graph.ConnectNodes(V.Type.Fragment, 3, 0, 2, 1); graph.ConnectNodes(V.Type.Fragment, 4, 0, 2, 3); graph.ConnectNodes(V.Type.Fragment, 5, 0, 2, 2);
        graph.ConnectNodes(V.Type.Fragment, 2, 0, 6, 0); graph.ConnectNodes(V.Type.Fragment, 6, 0, 0, 0);
        SaveDomain(graph, folder, "normal-fresnel");
    }
}
