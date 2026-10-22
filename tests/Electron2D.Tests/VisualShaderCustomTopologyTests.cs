using Electron2D;

internal static class VisualShaderCustomTopologyTests
{
    internal static void Run()
    {
        foreach (var stage in new[] { VisualShader.Type.Vertex, VisualShader.Type.Fragment })
        {
            using var graph = new VisualShader(); using var custom = new PassThrough();
            using var first = new VisualShaderNodeFloatConstant { Constant = .125f };
            using var last = new VisualShaderNodeFloatConstant { Constant = .875f };
            graph.AddNode(stage, custom, default, 2); graph.AddNode(stage, first, default, 3); graph.AddNode(stage, last, default, 4);
            graph.ConnectNodes(stage, 3, 0, 2, 0); graph.ConnectNodes(stage, 4, 0, 2, 0);
            graph.ConnectNodes(stage, 2, 0, 0, stage == VisualShader.Type.Vertex ? 2 : 0);
            var source = graph.GenerateSources(); var selected = stage == VisualShader.Type.Vertex ? source.Vertex : source.Fragment;
            Check(selected.Contains("n4p0", StringComparison.Ordinal) && !selected.Contains("n3p0", StringComparison.Ordinal) && custom.Calls == 1,
                "Only the last incoming edge reaches a custom block and its source callback runs once.");
            graph.DisconnectNodes(stage, 3, 0, 2, 0); graph.DisconnectNodes(stage, 4, 0, 2, 0);
            graph.ConnectNodesForced(stage, 2, 0, 2, 0); graph.ConnectNodes(stage, 4, 0, 2, 0);
            _ = graph.GenerateSources(); Check(custom.Calls == 2, "An overridden cyclic edge is not part of the executing dependency graph.");
            graph.DisconnectNodes(stage, 4, 0, 2, 0);
            try { _ = graph.GenerateSources(); throw new Exception("The selected self-cycle was accepted."); }
            catch (InvalidDataException) { }
            Check(custom.Calls == 2, "Selected cycles reject before source callbacks.");
        }
        using (var graph = new VisualShader())
        using (var custom = new PassThrough())
        using (var first = new VisualShaderNodeFloatConstant { Constant = .25f })
        using (var last = new VisualShaderNodeFloatConstant { Constant = .75f })
        {
            graph.AddNode(VisualShader.Type.Fragment, custom, default, 2);
            graph.AddNode(VisualShader.Type.Vertex, first, default, 3); graph.AddNode(VisualShader.Type.Vertex, last, default, 4);
            graph.ConnectNodes(VisualShader.Type.Vertex, 3, 0, 0, 2); graph.ConnectNodes(VisualShader.Type.Vertex, 4, 0, 0, 2);
            var code = graph.GenerateSources();
            Check(custom.Calls == 0 && code.Vertex.Contains("n4p0", StringComparison.Ordinal) && !code.Vertex.Contains("n3p0", StringComparison.Ordinal),
                "An unreachable custom provider does not break ordinary input precedence in another stage.");
        }
        Console.WriteLine("Custom topology: last input, overridden and selected cycles, both stages and disconnected providers passed.");
    }
    private static void Check(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
    private sealed class PassThrough : VisualShaderNodeCustom
    {
        internal int Calls;
        public override int GetInputPortCount() => 1;
        public override int GetOutputPortCount() => 1;
        public override string GetCode(IReadOnlyList<string> inputs, IReadOnlyList<string> outputs, Shader.Mode mode, VisualShader.Type stage)
        { Calls++; return outputs[0] + "=" + (inputs[0].Length == 0 ? "0.0" : inputs[0]) + ";"; }
    }
}
