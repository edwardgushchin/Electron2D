using Electron2D;
using IOPath = System.IO.Path;

internal static partial class GraphChecks
{
    private static readonly string[] FloatOperations = ["add", "subtract", "multiply", "divide", "modulo", "power", "max", "min", "atan2", "step"];
    private static readonly string[] VectorOperations = ["add", "subtract", "multiply", "divide", "modulo", "power", "max", "min", "cross", "atan2", "reflect", "step"];
    private static void AuthorNumeric(string folder)
    {
        for (var operation = 0; operation < FloatOperations.Length; operation++)
        {
            using var graph = new VisualShader();
            using var node = new VisualShaderNodeFloatOp { Operation = (VisualShaderNodeFloatOp.Operator)operation };
            node.SetInputPortDefaultValue(0, .5f); node.SetInputPortDefaultValue(1, .25f);
            graph.AddNode(VisualShader.Type.Fragment, node, default, 2); graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 0);
            ResourceSaver.Save(graph, IOPath.Combine(folder, "float-" + FloatOperations[operation] + ".e2dres"), SaverFlags.BundleResources);
        }
        for (var width = 2; width <= 4; width++)
            for (var operation = 0; operation < VectorOperations.Length; operation++)
            {
                if (operation == 8 && width != 3) continue;
                using var graph = new VisualShader();
                using var node = new VisualShaderNodeVectorOp { OperationType = (VisualShaderNodeVectorBase.OpType)(width - 2), Operation = (VisualShaderNodeVectorOp.Operator)operation };
                if (width == 2) { node.SetInputPortDefaultValue(0, new Vector2(.5f, .25f)); node.SetInputPortDefaultValue(1, new Vector2(.25f, .5f)); }
                if (width == 3) { node.SetInputPortDefaultValue(0, new Vector3(.5f, .25f, .75f)); node.SetInputPortDefaultValue(1, new Vector3(.25f, .5f, .125f)); }
                if (width == 4) { node.SetInputPortDefaultValue(0, new Vector4(.5f, .25f, .75f, .125f)); node.SetInputPortDefaultValue(1, new Vector4(.25f, .5f, .125f, .75f)); }
                graph.AddNode(VisualShader.Type.Fragment, node, default, 2); graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 0);
                ResourceSaver.Save(graph, IOPath.Combine(folder, "vector" + width + "-" + VectorOperations[operation] + ".e2dres"), SaverFlags.BundleResources | SaverFlags.Compress);
            }
        VisualShaderNodeConstant[] constants =
        [
            new VisualShaderNodeFloatConstant { Constant = .375f }, new VisualShaderNodeIntConstant { Constant = 1 },
            new VisualShaderNodeUIntConstant { Constant = uint.MaxValue }, new VisualShaderNodeBooleanConstant { Constant = true },
            new VisualShaderNodeVec2Constant { Constant = new(.125f, .25f) }, new VisualShaderNodeVec3Constant { Constant = new(.125f, .25f, .5f) },
            new VisualShaderNodeVec4Constant { Constant = new(.125f, .25f, .5f, .75f) }, new VisualShaderNodeColorConstant { Constant = new(.125f, .25f, .5f, .75f) }
        ];
        try
        {
            foreach (var constant in constants)
            {
                using var graph = new VisualShader(); graph.AddNode(VisualShader.Type.Fragment, constant, default, 2);
                using var divide = new VisualShaderNodeFloatOp { Operation = VisualShaderNodeFloatOp.Operator.Divide };
                if (constant is VisualShaderNodeUIntConstant)
                {
                    divide.SetInputPortDefaultValue(1, (float)uint.MaxValue);
                    graph.AddNode(VisualShader.Type.Fragment, divide, default, 3);
                    graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 3, 0);
                    graph.ConnectNodes(VisualShader.Type.Fragment, 3, 0, 0, 0);
                }
                else graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 0);
                ResourceSaver.Save(graph, IOPath.Combine(folder, constant.GetType().Name + ".e2dres"), SaverFlags.BundleResources);
            }
        }
        finally { foreach (var constant in constants) constant.Dispose(); }
        using (var graph = new VisualShader())
        using (var position = new VisualShaderNodeInput { InputName = "vertex" })
        using (var offset = new VisualShaderNodeVectorOp { OperationType = VisualShaderNodeVectorBase.OpType.Vector2 })
        using (var color = new VisualShaderNodeColorConstant { Constant = new(.25f, .5f, .75f, 1) })
        {
            offset.SetInputPortDefaultValue(1, new Vector2(4, 0));
            graph.AddNode(VisualShader.Type.Vertex, position, default, 2); graph.AddNode(VisualShader.Type.Vertex, offset, default, 3);
            graph.ConnectNodes(VisualShader.Type.Vertex, 2, 0, 3, 0); graph.ConnectNodes(VisualShader.Type.Vertex, 3, 0, 0, 0);
            graph.AddNode(VisualShader.Type.Fragment, color, default, 2); graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 0);
            ResourceSaver.Save(graph, IOPath.Combine(folder, "vertex-shift.e2dres"), SaverFlags.BundleResources);
        }
    }
    private static Color NumericExpected(string name)
    {
        if (name.StartsWith("float-", StringComparison.Ordinal))
        { var value = Scalar(name[6..], .5f, .25f); return new(value, value, value, 1); }
        if (name.StartsWith("vector", StringComparison.Ordinal))
        {
            var width = name[6] - '0'; var operation = name[8..];
            var a = new Vector3(.5f, .25f, .75f); var b = new Vector3(.25f, .5f, .125f);
            Vector3 value;
            if (operation == "cross") value = new(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
            else if (operation == "reflect")
            {
                var dot = a.X * b.X + a.Y * b.Y + (width >= 3 ? a.Z * b.Z : 0) + (width == 4 ? .125f * .75f : 0);
                value = a - 2 * dot * b;
            }
            else value = new(Scalar(operation, a.X, b.X), Scalar(operation, a.Y, b.Y), Scalar(operation, a.Z, b.Z));
            if (width == 2) value.Z = 0;
            return new(value.X, value.Y, value.Z, 1);
        }
        return name switch
        {
            "VisualShaderNodeFloatConstant" => new(.375f, .375f, .375f, 1),
            "VisualShaderNodeIntConstant" or "VisualShaderNodeUIntConstant" or "VisualShaderNodeBooleanConstant" => Colors.White,
            "VisualShaderNodeVec2Constant" => new(.125f, .25f, 0, 1),
            "VisualShaderNodeVec3Constant" or "VisualShaderNodeVec4Constant" or "VisualShaderNodeColorConstant" => new(.125f, .25f, .5f, 1),
            _ => throw new InvalidOperationException("Missing numeric oracle " + name)
        };
    }
    private static float Scalar(string operation, float a, float b) => operation switch
    {
        "add" => a + b,
        "subtract" => a - b,
        "multiply" => a * b,
        "divide" => a / b,
        "modulo" => a - b * MathF.Floor(a / b),
        "power" => MathF.Pow(a, b),
        "max" => MathF.Max(a, b),
        "min" => MathF.Min(a, b),
        "atan2" => MathF.Atan2(a, b),
        "step" => b < a ? 0 : 1,
        _ => throw new InvalidOperationException("Unknown scalar oracle " + operation)
    };
}
