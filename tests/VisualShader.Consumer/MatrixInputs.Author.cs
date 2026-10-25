using Electron2D;
using V = Electron2D.VisualShader;

internal static partial class GraphChecks
{
    internal static void MatrixInputsCPU()
    {
        using var input = new VisualShaderNodeInput();
        var changes = 0; input.InputTypeChanged += () => changes++;
        foreach (var name in new[] { "model_matrix", "canvas_matrix", "screen_matrix" })
        {
            input.InputName = name;
            Check(input.GetInputRealName() == name.ToUpperInvariant(), "Matrix inputs retain their renderer-owned symbols.");
            using var graph = new V(); using var operation = new VisualShaderNodeMatrixFunc();
            foreach (var stage in new[] { V.Type.Vertex, V.Type.Fragment })
            { graph.AddNode(stage, input, default, 2); graph.AddNode(stage, operation, default, 3); }
            Check(graph.CanConnectNodes(V.Type.Vertex, 2, 0, 3, 0) && !graph.CanConnectNodes(V.Type.Fragment, 2, 0, 3, 0),
                "One definition produces a matrix in canvas vertex and scalar zero outside that stage.");
            using var copy = (V)graph.DuplicateDeep();
            Check(ReferenceEquals(copy.GetNode(V.Type.Vertex, 2), copy.GetNode(V.Type.Fragment, 2)) &&
                  ((VisualShaderNodeInput)copy.GetNode(V.Type.Vertex, 2)).InputName == name, "Graph copies retain matrix names and shared borrower identities.");
        }
        using (var graph = new V())
        using (var operation = new VisualShaderNodeMatrixFunc())
        {
            graph.SetMode(Shader.Mode.Particles); graph.AddNode(V.Type.Process, input, default, 2); graph.AddNode(V.Type.Process, operation, default, 3);
            Check(!graph.CanConnectNodes(V.Type.Process, 2, 0, 3, 0), "Canvas matrix names remain scalar zero in particle lifecycle graphs.");
        }
        Check(changes == 1, "The matrix family changes the input role only once.");
        input.InputName = "[None]"; Check(changes == 2, "Leaving the matrix family reports the scalar role.");
        Console.WriteLine("Canvas matrix input CPU roles, events, aliases and copies passed.");
    }

    internal static void AuthorMatrixInputs(string folder)
    {
        Directory.CreateDirectory(folder); AuthorMatrixSources(folder);
        using var graph = new V(); using var expression = new VisualShaderNodeExpression();
        var definitions = new List<VisualShaderNodeInput>();
        foreach (var pair in new[] { ("model_matrix", "model"), ("canvas_matrix", "canvas"), ("screen_matrix", "screen") })
        {
            var input = new VisualShaderNodeInput { InputName = pair.Item1 }; definitions.Add(input);
            expression.AddInputPort(expression.GetInputPortCount(), VisualShaderNode.PortType.Matrix, pair.Item2);
            var id = 2 + expression.GetInputPortCount(); graph.AddNode(V.Type.Vertex, input, default, id);
        }
        expression.AddInputPort(3, VisualShaderNode.PortType.Vector2, "uv");
        expression.AddOutputPort(0, VisualShaderNode.PortType.Vector3, "observed");
        expression.AddOutputPort(1, VisualShaderNode.PortType.Vector2, "position");
        using var row = new VisualShaderNodeFloatParameter { ParameterName = "row" };
        graph.AddNode(V.Type.Vertex, row, default, 8); expression.AddInputPort(4, VisualShaderNode.PortType.Scalar, "line");
        expression.Expression = "int column = int(uv.x) / 4; int row = int(uv.x) % 4; float scale = column == 3 && row < 2 ? 0.001953125 : 0.125; float screenScale = column == row && row < 2 ? 32.0 : 0.125; observed = vec3(model[column][row] * scale, canvas[column][row] * scale, screen[column][row] * screenScale) + vec3(0.5); position = vec2(4.0 + uv.x * 4.0, line + uv.y * 8.0 + float(INSTANCE_ID) * 12.0);";
        using var uv = new VisualShaderNodeInput { InputName = "uv" };
        graph.AddNode(V.Type.Vertex, uv, default, 6); graph.AddNode(V.Type.Vertex, expression, default, 7);
        for (var i = 0; i < 3; i++) graph.ConnectNodes(V.Type.Vertex, 3 + i, 0, 7, i);
        graph.ConnectNodes(V.Type.Vertex, 6, 0, 7, 3); graph.ConnectNodes(V.Type.Vertex, 7, 0, 0, 2);
        graph.ConnectNodes(V.Type.Vertex, 8, 0, 7, 4); graph.ConnectNodes(V.Type.Vertex, 7, 1, 0, 0);
        SaveDomain(graph, folder, "matrix-nodes");
        expression.Expression = "int column = int(uv.x) / 4; int row = int(uv.x) % 4; float scale = column == 3 && row < 2 ? 0.001953125 : 0.125; float screenScale = column == row && row < 2 ? 32.0 : 0.125; observed = vec3(MODEL_MATRIX[column][row] * scale, CANVAS_MATRIX[column][row] * scale, SCREEN_MATRIX[column][row] * screenScale) + vec3(0.5); position = vec2(4.0 + uv.x * 4.0, line + uv.y * 8.0 + float(INSTANCE_ID) * 12.0);";
        for (var i = 0; i < 3; i++) graph.DisconnectNodes(V.Type.Vertex, 3 + i, 0, 7, i);
        SaveDomain(graph, folder, "matrix-expression");
        expression.Expression = expression.Expression.Replace("float(INSTANCE_ID) * 12.0", "0.0", StringComparison.Ordinal);
        SaveDomain(graph, folder, "matrix-no-id");
        foreach (var definition in definitions) definition.Dispose();
        Console.WriteLine("Authored matrix nodes and expression component probes.");
    }
}
