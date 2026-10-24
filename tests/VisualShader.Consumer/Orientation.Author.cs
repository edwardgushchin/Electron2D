using Electron2D;
using System.Text;
using M4 = System.Numerics.Matrix4x4;
using N3 = System.Numerics.Vector3;
using V = Electron2D.VisualShader;
using P = Electron2D.VisualShaderNode.PortType;

internal static partial class GraphChecks
{
    private readonly record struct AxisCase(Vector3 Value, float Angle, Vector3 Axis);
    private static readonly AxisCase[] AxisCases =
    [
        new(new(1, 0, 0), MathF.PI / 2, new(0, 0, 2)),
        new(new(.25f, .5f, .75f), MathF.PI / 2, new(0, 0, -2)),
        new(new(.125f, .25f, .5f), MathF.PI, new(1, 0, 0)),
        new(new(.5f, .25f, .125f), -MathF.PI / 2, new(0, 2, 0)),
        new(new(-.25f, .5f, 1), .73f, new(1, 2, 3)),
        new(new(2, -1, .25f), -1.1f, new(-1, .5f, 2)),
        new(new(4, -3, .25f), 0, new(0, 1, 0)),
        new(new(.125f, .25f, .5f), 2 * MathF.PI, new(0, 0, 1)),
        new(Vector3.Zero, MathF.PI / 3, new(0, 0, 1))
    ];
    private static readonly AxisCase AlternateAxis = new(new(.25f, -.5f, 1), -.6f, new(-.5f, 2, 1));
    private static readonly float[] AlternateAxisMatrix = MatrixColumns(AxisMatrix(AlternateAxis));
    private static M4 AxisMatrix(AxisCase value) => M4.CreateFromAxisAngle(N3.Normalize(new(value.Axis.X, value.Axis.Y, value.Axis.Z)), value.Angle);
    private static Vector3 AxisExpected(AxisCase value)
    { var result = N3.Transform(new(value.Value.X, value.Value.Y, value.Value.Z), AxisMatrix(value)); return new(result.X, result.Y, result.Z); }
    internal static void AuthorOrientation(string folder)
    {
        Directory.CreateDirectory(folder);
        foreach (var stage in ParticleStages.Concat([V.Type.Vertex, V.Type.Fragment, V.Type.TextureBlit])) SaveOrientation(folder, stage);
        using (var graph = new V())
        using (var node = new VisualShaderNodeRotationByAxis())
        {
            graph.SetMode(Shader.Mode.Particles); node.SetInputPortDefaultValue(1, MathF.PI / 2); node.SetInputPortDefaultValue(2, new Vector3(0, 0, 2));
            graph.AddNode(V.Type.StartCustom, node, default, 2); graph.ConnectNodes(V.Type.StartCustom, 2, 1, 0, 5);
            graph.GetNode(V.Type.StartCustom, 0).SetInputPortDefaultValue(3, new Vector3(.25f, .5f, .75f));
            SaveDomain(graph, folder, "particle-orientation-transform");
        }
        SaveOrientationVertex(folder);
        Console.WriteLine("Authored eight orientation programs plus actual particle-matrix and canvas-vertex consumers.");
    }
    private static void SaveOrientation(string folder, V.Type stage)
    {
        using var graph = new V(); graph.SetMode(stage == V.Type.TextureBlit ? Shader.Mode.TextureBlit : stage is V.Type.Vertex or V.Type.Fragment ? Shader.Mode.CanvasItem : Shader.Mode.Particles);
        var owned = new List<Resource>(); var id = 2; var port = 0; var checks = new List<string>(); var rows = new List<(string Vector, string Matrix, string Fourth)>();
        T Own<T>(T value) where T : Resource { owned.Add(value); return value; }
        int Add(VisualShaderNode value) { var next = id++; graph.AddNode(stage, value, default, next); return next; }
        var observe = Own(new VisualShaderNodeExpression()); var observeID = Add(observe);
        string Input(P type, int source, int output = 0)
        { var name = "observed" + port; observe.AddInputPort(port, type, name); graph.ConnectNodes(stage, source, output, observeID, port++); return name; }
        string MatrixCheck(string actual, string expected) => string.Join(" && ", Enumerable.Range(0, 4).Select(c => $"all(lessThanEqual(abs({actual}[{c}]-{expected}[{c}]),vec4(.0002)))"));
        var shared = Own(new VisualShaderNodeRotationByAxis());
        try
        {
            Add(Own(new VisualShaderNodeGlobalExpression { Expression = "mat4 e2d_axis_rotation(vec3 axis,float angle){return mat4(15.0);}" }));
            for (var sample = 0; sample < AxisCases.Length; sample++)
            {
                var value = AxisCases[sample]; var node = sample == 2 ? Own(new VisualShaderNodeRotationByAxis()) : shared; var nodeID = Add(node);
                if (sample == 0)
                {
                    Connect(Own(new VisualShaderNodeVec3Parameter { ParameterName = "orientation_vector", DefaultValueEnabled = true, DefaultValue = value.Value }), nodeID, 0);
                    Connect(Own(new VisualShaderNodeFloatParameter { ParameterName = "orientation_angle", DefaultValueEnabled = true, DefaultValue = value.Angle }), nodeID, 1);
                    Connect(Own(new VisualShaderNodeVec3Parameter { ParameterName = "orientation_axis", DefaultValueEnabled = true, DefaultValue = value.Axis }), nodeID, 2);
                }
                else if (sample == 2) { node.SetInputPortDefaultValue(0, value.Value); node.SetInputPortDefaultValue(1, value.Angle); node.SetInputPortDefaultValue(2, value.Axis); }
                else
                {
                    Connect(Own(new VisualShaderNodeVec3Constant { Constant = value.Value }), nodeID, 0);
                    Connect(Own(new VisualShaderNodeFloatConstant { Constant = value.Angle }), nodeID, 1);
                    Connect(Own(new VisualShaderNodeVec3Constant { Constant = value.Axis }), nodeID, 2);
                }
                var vector = Input(P.Vector3, nodeID); var matrix = Input(P.Matrix, nodeID, 1);
                var expectedVector = sample == 0 ? Input(P.Vector3, Add(Own(new VisualShaderNodeVec3Parameter { ParameterName = "orientation_expected_vector", DefaultValueEnabled = true, DefaultValue = AxisExpected(value) }))) : Input(P.Vector3, Add(Own(new VisualShaderNodeVec3Constant { Constant = AxisExpected(value) })));
                var expectedMatrix = sample == 0 ? Input(P.Matrix, Add(Own(new VisualShaderNodeMatrixParameter { ParameterName = "orientation_expected_matrix", DefaultValueEnabled = true, DefaultValue = AxisMatrix(value) }))) : Input(P.Matrix, Add(Own(new VisualShaderNodeMatrixConstant { Constant = AxisMatrix(value) })));
                checks.Add($"all(lessThanEqual(abs({vector}-{expectedVector}),vec3(.0002))) && " + MatrixCheck(matrix, expectedMatrix)); rows.Add((vector, matrix, "0.0"));
            }
            for (var width = 2; width <= 4; width++)
            {
                var face = Own(new VisualShaderNodeFaceForward { OpType = (VisualShaderNodeVectorBase.OpTypeEnum)(width - 2) });
                for (var sign = -1; sign <= 1; sign++)
                {
                    var nodeID = Add(face); var normal = new Vector4(.125f, .25f, .375f, .5f); var incident = new Vector4(.5f, -.25f, 0, 0); var reference = sign == 0 ? new Vector4(-.25f, -.5f, 0, 0) : incident * sign;
                    Connect(Own(new VisualShaderNodeVec4Constant { Constant = normal }), nodeID, 0); Connect(Own(new VisualShaderNodeVec4Constant { Constant = incident }), nodeID, 1); Connect(Own(new VisualShaderNodeVec4Constant { Constant = reference }), nodeID, 2);
                    var type = width == 2 ? P.Vector2 : width == 3 ? P.Vector3 : P.Vector4; var output = Input(type, nodeID); var expected = sign < 0 ? normal : -normal;
                    var expectedID = Add(Own(new VisualShaderNodeVec4Constant { Constant = expected })); var wanted = Input(type, expectedID);
                    checks.Add($"all(equal({output},{wanted}))"); rows.Add((width == 2 ? $"vec3({output},0.0)" : width == 3 ? output : output + ".xyz", "mat4(1.0)", width == 4 ? output + ".w" : "0.0"));
                }
            }
            var faceDefault = Add(Own(new VisualShaderNodeFaceForward())); var zero = Input(P.Vector3, faceDefault); checks.Add($"all(equal({zero},vec3(0.0)))");
            var code = new StringBuilder("bool valid=true;\n");
            for (var i = 0; i < checks.Count; i++) code.Append("valid=valid && (").Append(checks[i]).AppendLine(");");
            if (stage == V.Type.TextureBlit)
            {
                observe.AddOutputPort(0, P.Vector4, "vector_value"); observe.AddOutputPort(1, P.Vector4, "matrix_value"); observe.AddOutputPort(2, P.Vector4, "last_value");
                code.Append("int pixel=int(UV.x*").Append(rows.Count * 4).AppendLine(".0); int item=pixel/4; int column=pixel%4; vector_value=vec4(0,0,0,1);matrix_value=vec4(0,0,0,1);last_value=vec4(0,0,0,1);");
                for (var i = 0; i < rows.Count; i++) { var row = rows[i]; code.Append("if(item==").Append(i).Append("){mat4 value_matrix=").Append(row.Matrix).Append(";vector_value=vec4(").Append(row.Vector).Append(",1);matrix_value=vec4(value_matrix[column].xyz,1);last_value=vec4(").Append(row.Fourth).AppendLine(",value_matrix[column].w,valid?1.0:0.0,1);}"); }
                for (var output = 0; output < 3; output++) graph.ConnectNodes(stage, observeID, output, 0, output);
            }
            else
            { observe.AddOutputPort(0, P.Vector3, "color_value"); code.AppendLine("color_value=valid?vec3(.25,.5,.75):vec3(1,0,0);"); graph.ConnectNodes(stage, observeID, 0, 0, stage == V.Type.Vertex ? 2 : stage == V.Type.Fragment ? 0 : ColorPort(stage)); }
            observe.Expression = code.ToString();
            SaveDomain(graph, folder, (stage == V.Type.TextureBlit ? "blit" : stage is V.Type.Vertex or V.Type.Fragment ? "canvas" : "particle") + "-orientation-" + (int)stage);
        }
        finally { for (var i = owned.Count - 1; i >= 0; i--) owned[i].Dispose(); }
        void Connect(VisualShaderNode value, int target, int input) => graph.ConnectNodes(stage, Add(value), 0, target, input);
    }
    private static void SaveOrientationVertex(string folder)
    {
        using var graph = new V(); using var input = new VisualShaderNodeInput { InputName = "uv" }; using var rotation = new VisualShaderNodeRotationByAxis(); using var offset = new VisualShaderNodeVectorOp();
        using var scale = new VisualShaderNodeVectorOp { Operator = VisualShaderNodeVectorOp.OperatorEnum.Multiply }; scale.SetInputPortDefaultValue(1, new Vector3(16, 16, 0));
        using var origin = new VisualShaderNodeVec3Parameter { ParameterName = "orientation_origin" };
        using var color = new VisualShaderNodeExpression(); color.AddOutputPort(0, P.Vector3, "value"); color.Expression = "value=vec3(UV,.25);";
        rotation.SetInputPortDefaultValue(1, MathF.PI / 2); rotation.SetInputPortDefaultValue(2, new Vector3(0, 0, 1)); offset.SetInputPortDefaultValue(1, new Vector3(16, 0, 0));
        graph.AddNode(V.Type.Vertex, input, default, 2); graph.AddNode(V.Type.Vertex, rotation, default, 3); graph.AddNode(V.Type.Vertex, offset, default, 4); graph.AddNode(V.Type.Vertex, color, default, 5); graph.AddNode(V.Type.Vertex, scale, default, 6); graph.AddNode(V.Type.Vertex, origin, default, 7);
        graph.ConnectNodes(V.Type.Vertex, 2, 0, 6, 0); graph.ConnectNodes(V.Type.Vertex, 6, 0, 3, 0); graph.ConnectNodes(V.Type.Vertex, 7, 0, 4, 1); graph.ConnectNodes(V.Type.Vertex, 3, 0, 4, 0); graph.ConnectNodes(V.Type.Vertex, 4, 0, 0, 0); graph.ConnectNodes(V.Type.Vertex, 5, 0, 0, 2); SaveDomain(graph, folder, "canvas-orientation-vertex");
    }
}
