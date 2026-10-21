using Electron2D;
using IOPath = System.IO.Path;

internal static partial class GraphChecks
{
    private static void UnaryCPU()
    {
        using var scalar = new VisualShaderNodeFloatFunc();
        using var vector = new VisualShaderNodeVectorFunc();
        Check(scalar.Function == VisualShaderNodeFloatFunc.FunctionEnum.Sin && scalar.GetInputPortDefaultValue<float>(0) == 0, "Scalar function initial state.");
        Check(vector.Function == VisualShaderNodeVectorFunc.FunctionEnum.Normalize && vector.OpType == VisualShaderNodeVectorBase.OpTypeEnum.Vector3, "Vector function initial state.");
        var changes = 0; scalar.Changed += _ => changes++;
        scalar.Function = VisualShaderNodeFloatFunc.FunctionEnum.Sin;
        Reject<ArgumentOutOfRangeException>(() => scalar.Function = VisualShaderNodeFloatFunc.FunctionEnum.Max);
        Reject<ArgumentOutOfRangeException>(() => scalar.Function = (VisualShaderNodeFloatFunc.FunctionEnum)(-1));
        Check(changes == 0 && scalar.Function == VisualShaderNodeFloatFunc.FunctionEnum.Sin, "Invalid/unchanged function selections preserve state and events.");
        scalar.Function = VisualShaderNodeFloatFunc.FunctionEnum.RoundEven;
        Check(changes == 1, "A committed function change emits once.");
        vector.Function = VisualShaderNodeVectorFunc.FunctionEnum.Log2;
        vector.SetInputPortDefaultValue(0, new Vector3(.125f, .5f, .75f));
        vector.OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector4;
        using var copy = (VisualShaderNodeVectorFunc)vector.Duplicate();
        Check(copy.Function == vector.Function && copy.OpType == vector.OpType && copy.GetInputPortDefaultValue<Vector4>(0) == new Vector4(.125f, .5f, .75f, .75f), "Function copies retain width, selector and exact defaults.");
        Reject<ArgumentOutOfRangeException>(() => vector.Function = VisualShaderNodeVectorFunc.FunctionEnum.Max);
        Reject<ArgumentException>(() => vector.SetInputPortDefaultValue(0, new Vector4(float.NaN, 0, 0, 0)));
        using var operation = new VisualShaderNodeVectorOp();
        Check(operation.GetPropertyList().Any(p => p.Name == "Operator") && operation.GetPropertyList().Any(p => p.Name == "OpType") && !operation.GetPropertyList().Any(p => p.Name is "Operation" or "OperationType"), "The complete selector/storage family preserves owning property names.");
        using var graph = new VisualShader();
        graph.AddNode(VisualShader.Type.Fragment, scalar, default, 2);
        graph.ReplaceNode(VisualShader.Type.Fragment, 2, "VisualShaderNodeVectorFunc");
        Check(graph.GetNode(VisualShader.Type.Fragment, 2) is VisualShaderNodeVectorFunc, "Function factories participate in ordinary replacement.");
        copy.Dispose(); Reject<ObjectDisposedException>(() => copy.Function = VisualShaderNodeVectorFunc.FunctionEnum.Sin);
    }

    private static void AuthorFunctions(string folder)
    {
        foreach (var function in Enum.GetValues<VisualShaderNodeFloatFunc.FunctionEnum>())
        {
            if (function == VisualShaderNodeFloatFunc.FunctionEnum.Max) continue;
            AuthorScalarFunction(folder, function, FunctionInput(function.ToString()).X);
        }
        foreach (var function in Enum.GetValues<VisualShaderNodeVectorFunc.FunctionEnum>())
            for (var width = 2; width <= 4; width++)
            {
                if (function == VisualShaderNodeVectorFunc.FunctionEnum.Max) continue;
                using var graph = new VisualShader();
                using var node = new VisualShaderNodeVectorFunc { OpType = (VisualShaderNodeVectorBase.OpTypeEnum)(width - 2), Function = function };
                var input = FunctionInput(function.ToString());
                if (width == 2) node.SetInputPortDefaultValue(0, new Vector2(input.X, input.Y));
                if (width == 3) node.SetInputPortDefaultValue(0, new Vector3(input.X, input.Y, input.Z));
                if (width == 4) node.SetInputPortDefaultValue(0, input);
                graph.AddNode(VisualShader.Type.Fragment, node, default, 2); graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 0);
                ResourceSaver.Save(graph, IOPath.Combine(folder, $"unary-vector{width}-{function}.e2dres"), SaverFlags.BundleResources | SaverFlags.Compress);
                if (width == 4)
                {
                    using var observeW = new VisualShaderNodeVectorFunc { OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector4 };
                    graph.DisconnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 0);
                    graph.AddNode(VisualShader.Type.Fragment, observeW, default, 3);
                    graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 3, 0); graph.ConnectNodes(VisualShader.Type.Fragment, 3, 0, 0, 0);
                    ResourceSaver.Save(graph, IOPath.Combine(folder, $"unary-vector4-{function}-w.e2dres"), SaverFlags.BundleResources | SaverFlags.Compress);
                }
            }
        AuthorScalarFunction(folder, VisualShaderNodeFloatFunc.FunctionEnum.Saturate, 1.25f, "-high");
        AuthorScalarFunction(folder, VisualShaderNodeFloatFunc.FunctionEnum.Saturate, .25f, "-mid");
        AuthorScalarFunction(folder, VisualShaderNodeFloatFunc.FunctionEnum.Sign, 0, "-zero");
        AuthorScalarFunction(folder, VisualShaderNodeFloatFunc.FunctionEnum.Reciprocal, -.5f, "-negative");
        using (var graph = new VisualShader())
        using (var uv = new VisualShaderNodeInput { InputName = "uv" })
        using (var function = new VisualShaderNodeVectorFunc { OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector2, Function = VisualShaderNodeVectorFunc.FunctionEnum.Sin })
        {
            graph.AddNode(VisualShader.Type.Vertex, uv, default, 2); graph.AddNode(VisualShader.Type.Vertex, function, default, 3);
            graph.ConnectNodes(VisualShader.Type.Vertex, 2, 0, 3, 0); graph.ConnectNodes(VisualShader.Type.Vertex, 3, 0, 0, 1);
            graph.AddNode(VisualShader.Type.Fragment, uv, default, 2); graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 0);
            ResourceSaver.Save(graph, IOPath.Combine(folder, "unary-vertex-vector-sin.e2dres"), SaverFlags.BundleResources);
        }
        AuthorScalarFunction(folder, VisualShaderNodeFloatFunc.FunctionEnum.Sin, .5f, vertex: true);
    }

    private static void AuthorScalarFunction(string folder, VisualShaderNodeFloatFunc.FunctionEnum function, float input, string suffix = "", bool vertex = false)
    {
        using var graph = new VisualShader();
        using var node = new VisualShaderNodeFloatFunc { Function = function };
        node.SetInputPortDefaultValue(0, input);
        var stage = vertex ? VisualShader.Type.Vertex : VisualShader.Type.Fragment;
        graph.AddNode(stage, node, default, 2); graph.ConnectNodes(stage, 2, 0, 0, vertex ? 2 : 0);
        ResourceSaver.Save(graph, IOPath.Combine(folder, vertex ? "unary-vertex-scalar-sin.e2dres" : $"unary-scalar-{function}{suffix}.e2dres"), SaverFlags.BundleResources);
    }

    private static Vector4 FunctionInput(string function) => function switch
    {
        "Acosh" => new(1.25f, 1.5f, 1.75f, 2),
        "Round" => new(-1.75f, -.625f, .75f, 1.75f),
        "RoundEven" => new(-2.5f, -1.5f, .5f, 1.5f),
        "Floor" or "Ceil" or "Trunc" or "Fract" => new(-1.25f, -.5f, .25f, 1.75f),
        "Saturate" => new(-.25f, .25f, 1.25f, 2),
        "Sign" => new(-.25f, 0, .5f, -1),
        "Abs" or "Normalize" => new(-.125f, .25f, -.5f, .75f),
        _ => new(.125f, .25f, .5f, .75f)
    };

    private static Color UnaryExpected(string name)
    {
        if (name == "unary-vertex-vector-sin") { var x = MathF.Sin(1) * (8.5f / 16); return new(x, x, 0, 1); }
        if (name == "unary-vertex-scalar-sin") { var x = MathF.Sin(.5f); return new(x, x, x, 1); }
        var parts = name.Split('-'); var function = parts[2]; var input = FunctionInput(function);
        if (parts[1] == "scalar")
        {
            var x = parts.Length < 4 ? input.X : parts[3] switch { "high" => 1.25f, "mid" => .25f, "zero" => 0, "negative" => -.5f, _ => throw new InvalidOperationException(name) };
            var value = Unary(function, x); return new(value, value, value, 1);
        }
        var width = parts[1][6] - '0';
        if (width == 4 && parts.Length == 4 && parts[3] == "w")
        {
            var result = function == "Normalize" ? input : new Vector4(Unary(function, input.X), Unary(function, input.Y), Unary(function, input.Z), Unary(function, input.W));
            var length = MathF.Sqrt(result.X * result.X + result.Y * result.Y + result.Z * result.Z + result.W * result.W);
            return new(result.X / length, result.Y / length, result.Z / length, 1);
        }
        if (function == "Normalize")
        {
            var length = MathF.Sqrt(input.X * input.X + input.Y * input.Y + (width >= 3 ? input.Z * input.Z : 0) + (width == 4 ? input.W * input.W : 0));
            return new(input.X / length, input.Y / length, width == 2 ? 0 : input.Z / length, 1);
        }
        return new(Unary(function, input.X), Unary(function, input.Y), width == 2 ? 0 : Unary(function, input.Z), 1);
    }

    private static float Unary(string function, float x) => function switch
    {
        "Sin" => MathF.Sin(x),
        "Cos" => MathF.Cos(x),
        "Tan" => MathF.Tan(x),
        "Asin" => MathF.Asin(x),
        "Acos" => MathF.Acos(x),
        "Atan" => MathF.Atan(x),
        "Sinh" => MathF.Sinh(x),
        "Cosh" => MathF.Cosh(x),
        "Tanh" => MathF.Tanh(x),
        "Acosh" => MathF.Acosh(x),
        "Asinh" => MathF.Asinh(x),
        "Atanh" => MathF.Atanh(x),
        "Abs" => MathF.Abs(x),
        "Sign" => MathF.Sign(x),
        "Negate" => -x,
        "Reciprocal" => 1 / x,
        "OneMinus" => 1 - x,
        "Floor" => MathF.Floor(x),
        "Ceil" => MathF.Ceiling(x),
        "Round" => MathF.Round(x, MidpointRounding.AwayFromZero),
        "RoundEven" => MathF.Round(x, MidpointRounding.ToEven),
        "Trunc" => MathF.Truncate(x),
        "Fract" => x - MathF.Floor(x),
        "Exp" => MathF.Exp(x),
        "Exp2" => MathF.Pow(2, x),
        "Log" => MathF.Log(x),
        "Log2" => MathF.Log2(x),
        "Sqrt" => MathF.Sqrt(x),
        "InverseSqrt" => 1 / MathF.Sqrt(x),
        "Degrees" => x * (180 / MathF.PI),
        "Radians" => x * (MathF.PI / 180),
        "Saturate" => Math.Clamp(x, 0, 1),
        _ => throw new InvalidOperationException("Missing independent function oracle " + function)
    };
}
