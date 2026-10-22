using Electron2D;
using P = Electron2D.VisualShaderNode.PortType;
using V = Electron2D.VisualShader;
using E = Electron2D.VisualShaderNodeExpression;
using IOPath = System.IO.Path;

internal static partial class GraphChecks
{
    internal static void AuthorCustom(string folder)
    {
        Directory.CreateDirectory(folder); Directory.CreateDirectory(IOPath.Combine(folder, "invalid-custom"));
        foreach (var stage in new[] { V.Type.Vertex, V.Type.Fragment })
        {
            for (var role = 0; role < 8; role++) for (var mode = 0; mode < 3; mode++) SaveCustomNumeric(folder, stage, (P)role, mode);
            for (var kind = 0; kind <= 13; kind++) if (kind != 4 || stage == V.Type.Fragment) SaveCustomProcedure(folder, stage, kind);
        }
        foreach (var kind in new[] { 100, 101, 102, 103, 104, 105, 106, 107, 109, 110 }) SaveCustomProcedure(IOPath.Combine(folder, "invalid-custom"), V.Type.Vertex, kind);
        Console.WriteLine("Authored 75 custom programs and ten invalid custom source workflows.");
    }
    private static void SaveCustomNumeric(string folder, V.Type stage, P role, int mode)
    {
        using var graph = new V(); using var node = new CustomNumericNode { Role = role, InputMode = mode }; using var observe = new E(); using var parameter = ExpressionParameter(role);
        observe.AddInputPort(0, role, "value"); observe.AddOutputPort(0, P.Vector3, "observed"); observe.Expression = "observed=" + ExpressionObservation(role) + ";";
        graph.AddNode(stage, node, default, 2); graph.AddNode(stage, node, default, 900); graph.AddNode(stage, observe, default, 3);
        graph.ConnectNodes(stage, 2, 0, 3, 0); graph.ConnectNodes(stage, 3, 0, 0, stage == V.Type.Vertex ? 2 : 0);
        if (mode == 2) { parameter.ParameterName = "custom_value"; ParameterEnabled(parameter, true); graph.AddNode(stage, parameter, default, 4); graph.ConnectNodes(stage, 4, 0, 2, 0); }
        ResourceSaver.Save(graph, IOPath.Combine(folder, $"custom-numeric-{(int)role}-{mode}-{(int)stage}.e2dres"), SaverFlags.BundleResources | SaverFlags.Compress | SaverFlags.SaveBigEndian);
    }
    private static void SaveCustomProcedure(string folder, V.Type stage, int kind)
    {
        using var graph = new V(); using var node = new CustomProcedureNode { Kind = kind == 13 ? 0 : kind }; using var texture = new VisualShaderNodeImageParameter { ParameterName = "custom_image" };
        graph.AddNode(stage, node, default, 2); graph.ConnectNodes(stage, 2, 0, 0, stage == V.Type.Vertex ? 2 : 0);
        if (kind == 1) graph.ConnectNodes(stage, 2, 1, 0, stage == V.Type.Vertex ? 3 : 1);
        if (kind == 5) { graph.AddNode(stage, texture, default, 3); graph.ConnectNodes(stage, 3, 0, 2, 0); }
        if (kind == 12) node.SetOptionIndex(0, 0);
        if (kind == 13)
        {
            using var other = new CustomOtherNode(); using var observe = new E();
            observe.AddInputPort(0, P.Vector3, "first"); observe.AddInputPort(1, P.Vector3, "second"); observe.AddOutputPort(0, P.Vector3, "colorValue"); observe.Expression = "colorValue=(first+second)*0.5;";
            graph.AddNode(stage, other, default, 3); graph.AddNode(stage, observe, default, 4); graph.DisconnectNodes(stage, 2, 0, 0, stage == V.Type.Vertex ? 2 : 0);
            graph.ConnectNodes(stage, 2, 0, 4, 0); graph.ConnectNodes(stage, 3, 0, 4, 1); graph.ConnectNodes(stage, 4, 0, 0, stage == V.Type.Vertex ? 2 : 0);
            ResourceSaver.Save(graph, IOPath.Combine(folder, $"custom-procedural-{kind}-{(int)stage}.e2dres"), SaverFlags.BundleResources); return;
        }
        if (kind == 0)
        {
            node.SetOptionIndex(0, 0);
            using var second = new CustomProcedureNode();
            var other = stage == V.Type.Vertex ? V.Type.Fragment : V.Type.Vertex;
            graph.AddNode(other, second, default, 2); graph.ConnectNodes(other, 2, 0, 0, other == V.Type.Vertex ? 2 : 0);
            ResourceSaver.Save(graph, IOPath.Combine(folder, $"custom-procedural-{kind}-{(int)stage}.e2dres"), SaverFlags.BundleResources); return;
        }
        ResourceSaver.Save(graph, IOPath.Combine(folder, $"custom-procedural-{kind}-{(int)stage}.e2dres"), SaverFlags.BundleResources);
    }
}
