using Electron2D;
using IOPath = System.IO.Path;

internal static partial class GraphChecks
{
    private static readonly Vector4 Components = new(.125f, .25f, .5f, .75f);
    private static void DataflowCPU()
    {
        using var compose = new VisualShaderNodeVectorCompose();
        Check(compose.GetInputPortDefaultValue<float>(3) is null, "Composition initially has three defaults and an absent reserved W.");
        compose.SetInputPortDefaultValue(2, .5f); compose.SetInputPortDefaultValue(3, .75f);
        compose.OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector2;
        var defaults = compose.GetDefaultInputValues(); compose.SetDefaultInputValues(defaults);
        Check(compose.GetInputPortDefaultValue<float>(2) == .5f && compose.GetInputPortDefaultValue<float>(3) == .75f, "Narrowing and complete-table snapshots retain dormant scalar defaults.");
        using var copy = (VisualShaderNodeVectorCompose)compose.Duplicate();
        copy.OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector4;
        Check(copy.GetInputPortDefaultValue<float>(2) == .5f && copy.GetInputPortDefaultValue<float>(3) == .75f, "Copied dormant components return when widened.");
        compose.ClearDefaultInputValues(); compose.OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector4;
        Check(compose.GetDefaultInputValues().Count == 4 && compose.GetDefaultInputValues().Cast<VisualShaderPortDefault<float>>().All(p => p.Value == 0), "A composition width change recreates missing active scalar defaults as zero.");
        Reject<ArgumentOutOfRangeException>(() => compose.SetInputPortDefaultValue(4, 1f));
        using var refract = new VisualShaderNodeVectorRefract(); refract.SetInputPortDefaultValue(2, .625f);
        refract.OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector4;
        Check(refract.GetInputPortDefaultValue<float>(2) == .625f, "Vector width changes preserve scalar eta.");
        refract.RemoveInputPortDefaultValue(2); refract.RemoveInputPortDefaultValue(0);
        refract.OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector2;
        refract.RemoveInputPortDefaultValue(0); refract.OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector4;
        Check(refract.GetInputPortDefaultValue<float>(2) is null && refract.GetInputPortDefaultValue<Vector4>(0) == new Vector4(0, 0, 0, 1), "Missing scalar eta stays absent while missing vector defaults use the selected fallback.");
        Reject<ArgumentException>(() => refract.SetInputPortDefaultValue(2, Vector4.Zero));
        using var dot = new VisualShaderNodeDotProduct(); dot.SetInputPortDefaultValue(0, new Vector3(.25f, .5f, .75f));
        using var dotCopy = (VisualShaderNodeDotProduct)dot.Duplicate();
        Check(dotCopy.GetInputPortDefaultValue<Vector3>(0) == new Vector3(.25f, .5f, .75f), "Fixed-width dot definitions copy their actual defaults.");
        using var graph = new VisualShader(); using var split = new VisualShaderNodeVectorDecompose { OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector4 };
        graph.AddNode(VisualShader.Type.Fragment, split, default, 2); graph.ConnectNodes(VisualShader.Type.Fragment, 2, 3, 0, 0);
        split.OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector2;
        Check(!graph.CanConnectNodes(VisualShader.Type.Fragment, 2, 3, 0, 0) && graph.IsNodeConnection(VisualShader.Type.Fragment, 2, 3, 0, 0), "Shape changes retain source edges while rejecting absent outputs.");
    }

    private static void SetVector(VisualShaderNode node, int port, int width, Vector4 value)
    {
        if (width == 2) node.SetInputPortDefaultValue(port, new Vector2(value.X, value.Y));
        else if (width == 3) node.SetInputPortDefaultValue(port, new Vector3(value.X, value.Y, value.Z));
        else node.SetInputPortDefaultValue(port, value);
    }
    private static void SaveFlow(VisualShader graph, string folder, string name) =>
        ResourceSaver.Save(graph, IOPath.Combine(folder, "flow-" + name + ".e2dres"), SaverFlags.BundleResources | SaverFlags.Compress | SaverFlags.SaveBigEndian);
    private static void AuthorDataflow(string folder)
    {
        for (var width = 2; width <= 4; width++)
        {
            var kind = (VisualShaderNodeVectorBase.OpTypeEnum)(width - 2);
            using var compose = new VisualShaderNodeVectorCompose { OpType = kind };
            for (var p = 0; p < 4; p++) compose.SetInputPortDefaultValue(p, Components[p]);
            using (var graph = new VisualShader())
            { graph.AddNode(VisualShader.Type.Fragment, compose, default, 2); graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 0); SaveFlow(graph, folder, "compose" + width); }
            for (var port = 0; port < width; port++)
            {
                using var graph = new VisualShader(); using var split = new VisualShaderNodeVectorDecompose { OpType = kind };
                graph.AddNode(VisualShader.Type.Fragment, compose, default, 2); graph.AddNode(VisualShader.Type.Fragment, split, default, 3);
                graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 3, 0); graph.ConnectNodes(VisualShader.Type.Fragment, 3, port, 0, 0);
                SaveFlow(graph, folder, $"split{width}-{port}");
            }
            using (var graph = new VisualShader())
            using (var split = new VisualShaderNodeVectorDecompose { OpType = kind })
            using (var join = new VisualShaderNodeVectorCompose { OpType = kind })
            {
                graph.AddNode(VisualShader.Type.Fragment, compose, default, 2); graph.AddNode(VisualShader.Type.Fragment, split, default, 3); graph.AddNode(VisualShader.Type.Fragment, join, default, 4);
                graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 3, 0);
                for (var port = 0; port < width; port++) graph.ConnectNodes(VisualShader.Type.Fragment, 3, width - 1 - port, 4, port);
                graph.ConnectNodes(VisualShader.Type.Fragment, 4, 0, 0, 0); SaveFlow(graph, folder, "roundtrip" + width);
            }
            using (var graph = new VisualShader())
            using (var length = new VisualShaderNodeVectorLen { OpType = kind })
            { SetVector(length, 0, width, new(.25f, .5f, .75f, 1)); graph.AddNode(VisualShader.Type.Fragment, length, default, 2); graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 0); SaveFlow(graph, folder, "length" + width); }
            using (var graph = new VisualShader())
            using (var distance = new VisualShaderNodeVectorDistance { OpType = kind })
            { SetVector(distance, 0, width, new(.25f, .5f, .75f, 1)); SetVector(distance, 1, width, new(-.25f, .25f, .5f, .5f)); graph.AddNode(VisualShader.Type.Fragment, distance, default, 2); graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 0); SaveFlow(graph, folder, "distance" + width); }
            foreach (var variant in new[] { "half", "zero", "tir", "critical" }) AuthorRefraction(folder, width, variant);
        }
        AuthorRefraction(folder, 3, "negative"); AuthorRefraction(folder, 4, "w"); AuthorRefraction(folder, 4, "cleared");
        using (var graph = new VisualShader())
        using (var dot = new VisualShaderNodeDotProduct())
        { dot.SetInputPortDefaultValue(0, new Vector3(.25f, .5f, .75f)); dot.SetInputPortDefaultValue(1, new Vector3(.5f, .25f, .125f)); graph.AddNode(VisualShader.Type.Fragment, dot, default, 2); graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 0); SaveFlow(graph, folder, "dot"); }
        using (var graph = new VisualShader())
        using (var compose = new VisualShaderNodeVectorCompose { OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector4 })
        using (var split = new VisualShaderNodeVectorDecompose { OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector4 })
        {
            compose.SetInputPortDefaultValue(0, .25f); compose.SetInputPortDefaultValue(1, .5f); compose.SetInputPortDefaultValue(2, .75f); compose.SetInputPortDefaultValue(3, .5f);
            graph.AddNode(VisualShader.Type.Fragment, compose, default, 2); graph.AddNode(VisualShader.Type.Fragment, split, default, 3);
            graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 3, 0); graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 0); graph.ConnectNodes(VisualShader.Type.Fragment, 3, 3, 0, 1); SaveFlow(graph, folder, "alpha");
            compose.OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector2; graph.DisconnectNodes(VisualShader.Type.Fragment, 2, 0, 3, 0); graph.DisconnectNodes(VisualShader.Type.Fragment, 3, 3, 0, 1); graph.RemoveNode(VisualShader.Type.Fragment, 3);
            SaveFlow(graph, folder, "hidden-compose");
        }
        using (var graph = new VisualShader())
        using (var split = new VisualShaderNodeVectorDecompose { OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector4 })
        {
            graph.AddNode(VisualShader.Type.Fragment, split, default, 2); graph.ConnectNodes(VisualShader.Type.Fragment, 2, 3, 0, 0); split.OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector2;
            var invalid = IOPath.Combine(folder, "invalid"); Directory.CreateDirectory(invalid); SaveFlow(graph, invalid, "missing-output");
        }
        AuthorFlowVertex(folder);
        using (var graph = new VisualShader())
        using (var uv = new VisualShaderNodeInput { InputName = "uv" })
        using (var distance = new VisualShaderNodeVectorDistance { OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector2 })
        {
            distance.SetInputPortDefaultValue(1, new Vector2(.5f, .5f));
            graph.AddNode(VisualShader.Type.Fragment, uv, default, 2); graph.AddNode(VisualShader.Type.Fragment, distance, default, 3);
            graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 3, 0); graph.ConnectNodes(VisualShader.Type.Fragment, 3, 0, 0, 0); SaveFlow(graph, folder, "radial");
        }
    }

    private static void AuthorRefraction(string folder, int width, string variant)
    {
        using var graph = new VisualShader(); using var refract = new VisualShaderNodeVectorRefract { OpType = (VisualShaderNodeVectorBase.OpTypeEnum)(width - 2) };
        var incident = variant == "critical" ? new Vector4(1, 0, 0, 0) : width == 4 ? new Vector4(.36f, -.8f, 0, .48f) : new Vector4(.6f, -.8f, 0, 0);
        SetVector(refract, 0, width, incident); SetVector(refract, 1, width, new(0, 1, 0, 0));
        refract.SetInputPortDefaultValue(2, Eta(variant));
        if (variant == "cleared") { refract.RemoveInputPortDefaultValue(2); refract.OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector2; refract.OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector4; SetVector(refract, 0, width, incident); SetVector(refract, 1, width, new(0, 1, 0, 0)); }
        graph.AddNode(VisualShader.Type.Fragment, refract, default, 2);
        using var split = new VisualShaderNodeVectorDecompose { OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector4 };
        if (variant == "w") { graph.AddNode(VisualShader.Type.Fragment, split, default, 3); graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 3, 0); graph.ConnectNodes(VisualShader.Type.Fragment, 3, 3, 0, 0); }
        else graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 0);
        SaveFlow(graph, folder, $"refract{width}-{variant}");
    }
    private static float Eta(string variant) => variant switch { "zero" or "cleared" => 0, "tir" => 2, "critical" => 1, "negative" => -.5f, _ => .5f };
    private static void AuthorFlowVertex(string folder)
    {
        using var graph = new VisualShader(); using var uv = new VisualShaderNodeInput { InputName = "uv" };
        using var split = new VisualShaderNodeVectorDecompose { OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector2 };
        using var join = new VisualShaderNodeVectorCompose { OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector2 };
        using var inverse = new VisualShaderNodeFloatFunc { Function = VisualShaderNodeFloatFunc.FunctionEnum.OneMinus };
        graph.AddNode(VisualShader.Type.Vertex, uv, default, 2); graph.AddNode(VisualShader.Type.Vertex, split, default, 3); graph.AddNode(VisualShader.Type.Vertex, join, default, 4); graph.AddNode(VisualShader.Type.Vertex, inverse, default, 5);
        graph.ConnectNodes(VisualShader.Type.Vertex, 2, 0, 3, 0); graph.ConnectNodes(VisualShader.Type.Vertex, 3, 0, 5, 0); graph.ConnectNodes(VisualShader.Type.Vertex, 3, 1, 4, 0); graph.ConnectNodes(VisualShader.Type.Vertex, 5, 0, 4, 1); graph.ConnectNodes(VisualShader.Type.Vertex, 4, 0, 0, 1);
        graph.AddNode(VisualShader.Type.Fragment, uv, default, 2); graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 0); SaveFlow(graph, folder, "vertex");
    }
    private static Color DataflowExpected(string name)
    {
        name = name[5..];
        if (name == "alpha") return new(.125f, .25f, .375f, 1);
        if (name == "hidden-compose") return new(.25f, .5f, 0, 1);
        if (name == "vertex") return new(8.5f / 16, 7.5f / 16, 0, 1);
        if (name == "dot") return new(.34375f, .34375f, .34375f, 1);
        if (name == "radial") { var x = MathF.Sqrt(2) / 32; return new(x, x, x, 1); }
        if (name.StartsWith("compose", StringComparison.Ordinal)) return new(Components.X, Components.Y, name[^1] == '2' ? 0 : Components.Z, 1);
        if (name.StartsWith("split", StringComparison.Ordinal)) { var x = Components[int.Parse(name.Split('-')[1])]; return new(x, x, x, 1); }
        if (name.StartsWith("roundtrip", StringComparison.Ordinal)) { var width = name[^1] - '0'; return new(Components[width - 1], Components[width - 2], width == 2 ? 0 : Components[width - 3], 1); }
        if (name.StartsWith("length", StringComparison.Ordinal) || name.StartsWith("distance", StringComparison.Ordinal))
        {
            var width = name[^1] - '0'; var values = name.StartsWith("length", StringComparison.Ordinal) ? new Vector4(.25f, .5f, .75f, 1) : new Vector4(.5f, .25f, .25f, .5f);
            var sum = 0f; for (var i = 0; i < width; i++) sum += values[i] * values[i]; var x = MathF.Sqrt(sum); return new(x, x, x, 1);
        }
        if (name.StartsWith("refract", StringComparison.Ordinal))
        {
            var width = name[7] - '0'; var variant = name.Split('-')[1]; var eta = Eta(variant);
            var incident = variant == "critical" ? new Vector4(1, 0, 0, 0) : width == 4 ? new Vector4(.36f, -.8f, 0, .48f) : new Vector4(.6f, -.8f, 0, 0);
            var dot = incident.Y; var k = 1 - eta * eta * (1 - dot * dot); var result = k < 0 ? Vector4.Zero : eta * incident - new Vector4(0, 1, 0, 0) * (eta * dot + MathF.Sqrt(k));
            return variant == "w" ? new(result.W, result.W, result.W, 1) : new(result.X, result.Y, result.Z, 1);
        }
        throw new InvalidOperationException("Missing dataflow oracle " + name);
    }
}
