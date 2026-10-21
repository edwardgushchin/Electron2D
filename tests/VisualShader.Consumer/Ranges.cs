using Electron2D;
using IOPath = System.IO.Path;

internal static partial class GraphChecks
{
    private static readonly string[] RangeNames = ["Clamp", "Mix", "Step", "SmoothStep", "MultiplyAdd", "Remap"];
    private static int RangeModes(string name) => name == "Clamp" ? 6 : name == "MultiplyAdd" ? 4 : 7;
    private static int RangePorts(string name) => name == "Step" ? 2 : name == "Remap" ? 5 : 3;
    private static VisualShaderNode CreateRange(string name) => name switch
    {
        "Clamp" => new VisualShaderNodeClamp(),
        "Mix" => new VisualShaderNodeMix(),
        "Step" => new VisualShaderNodeStep(),
        "SmoothStep" => new VisualShaderNodeSmoothStep(),
        "MultiplyAdd" => new VisualShaderNodeMultiplyAdd(),
        _ => new VisualShaderNodeRemap()
    };
    private static void SelectRange(VisualShaderNode node, int type)
    {
        switch (node)
        {
            case VisualShaderNodeClamp n: n.OpType = (VisualShaderNodeClamp.OpTypeEnum)type; break;
            case VisualShaderNodeMultiplyAdd n: n.OpType = (VisualShaderNodeMultiplyAdd.OpTypeEnum)type; break;
            case VisualShaderNodeMix n: n.OpType = (VisualShaderNumericOpType)type; break;
            case VisualShaderNodeStep n: n.OpType = (VisualShaderNumericOpType)type; break;
            case VisualShaderNodeSmoothStep n: n.OpType = (VisualShaderNumericOpType)type; break;
            case VisualShaderNodeRemap n: n.OpType = (VisualShaderNumericOpType)type; break;
        }
    }
    private static int SelectedRange(VisualShaderNode node) => node switch
    {
        VisualShaderNodeClamp n => (int)n.OpType,
        VisualShaderNodeMultiplyAdd n => (int)n.OpType,
        VisualShaderNodeMix n => (int)n.OpType,
        VisualShaderNodeStep n => (int)n.OpType,
        VisualShaderNodeSmoothStep n => (int)n.OpType,
        VisualShaderNodeRemap n => (int)n.OpType,
        _ => -1
    };
    private static int RangeWidth(string name, int mode) => name switch
    { "Clamp" => mode < 3 ? 1 : mode - 1, "MultiplyAdd" => mode + 1, _ => mode == 0 ? 1 : (mode + 3) / 2 };
    private static int RangePortWidth(string name, int mode, int port)
    {
        var mixed = name is not ("Clamp" or "MultiplyAdd") && mode is 2 or 4 or 6;
        var control = name switch { "Mix" => port == 2, "Step" => port == 0, "SmoothStep" => port < 2, "Remap" => port > 0, _ => false };
        return mixed && control ? 1 : RangeWidth(name, mode);
    }
    private static void SetRangeDefault(VisualShaderNode node, string name, int mode, int port, Vector4 value)
    {
        if (name == "Clamp" && mode == 1) node.SetInputPortDefaultValue(port, (int)value.X);
        else if (name == "Clamp" && mode == 2) node.SetInputPortDefaultValue(port, (uint)value.X);
        else if (RangePortWidth(name, mode, port) == 1) node.SetInputPortDefaultValue(port, value.X);
        else SetVector(node, port, RangePortWidth(name, mode, port), value);
    }
    private static void RangesCPU()
    {
        foreach (var name in RangeNames)
        {
            using var initial = CreateRange(name);
            Check(initial.GetDefaultInputValues().Count == (name == "Remap" ? 4 : RangePorts(name)), name + " initial default presence.");
            for (var from = 0; from < RangeModes(name); from++)
                for (var to = 0; to < RangeModes(name); to++)
                {
                    using var node = CreateRange(name); SelectRange(node, from);
                    for (var port = 0; port < RangePorts(name); port++) SetRangeDefault(node, name, from, port, new(.25f, .5f, .75f, 1));
                    var changes = 0; node.Changed += _ => changes++;
                    SelectRange(node, to);
                    Check(changes == (from == to ? 0 : 1), "Selection publishes exactly one complete state.");
                    foreach (var entry in node.GetDefaultInputValues())
                    {
                        var expected = name == "Clamp" && to == 1 ? typeof(int) : name == "Clamp" && to == 2 ? typeof(uint) : RangePortWidth(name, to, entry.Port) switch
                        { 1 => typeof(float), 2 => typeof(Vector2), 3 => typeof(Vector3), _ => typeof(Vector4) };
                        Check(entry.ValueType == expected, name + " converted input type.");
                    }
                    var valid = node.GetDefaultInputValues();
                    Reject<ArgumentOutOfRangeException>(() => SelectRange(node, -1));
                    Reject<ArgumentOutOfRangeException>(() => SelectRange(node, RangeModes(name)));
                    Check(valid.SequenceEqual(node.GetDefaultInputValues()) && SelectedRange(node) == to, "Invalid selectors preserve all state.");
                    using var copy = (VisualShaderNode)node.Duplicate();
                    using var copied = CreateRange(name); copied.CopyFromResource(node);
                    Check(SelectedRange(copy) == to && SelectedRange(copied) == to && copy.GetDefaultInputValues().Count == RangePorts(name), "Copy and duplicate preserve selected layouts.");
                    node.ClearDefaultInputValues(); SelectRange(node, (to + 1) % RangeModes(name));
                    Check(node.GetDefaultInputValues().Count == RangePorts(name), "Changed layouts recreate every absent default.");
                }
            using var disposed = CreateRange(name); disposed.Dispose();
            Reject<ObjectDisposedException>(() => SelectRange(disposed, 0));
        }
        using var clamp = new VisualShaderNodeClamp { OpType = VisualShaderNodeClamp.OpTypeEnum.Int };
        clamp.SetInputPortDefaultValue(0, 123456789); clamp.OpType = VisualShaderNodeClamp.OpTypeEnum.UInt;
        Check(clamp.GetInputPortDefaultValue<uint>(0) == 123456789u, "Integer conversion preserves all integer bits without a float intermediate.");
        clamp.SetInputPortDefaultValue(2, uint.MaxValue); var snapshot = clamp.GetDefaultInputValues();
        Reject<OverflowException>(() => clamp.OpType = VisualShaderNodeClamp.OpTypeEnum.Int);
        Check(clamp.OpType == VisualShaderNodeClamp.OpTypeEnum.UInt && snapshot.SequenceEqual(clamp.GetDefaultInputValues()), "A later conversion overflow cannot partially update earlier defaults.");
        using var mix = new VisualShaderNodeMix(); mix.ClearDefaultInputValues(); mix.OpType = VisualShaderNumericOpType.Vector4;
        Check(mix.GetInputPortDefaultValue<Vector4>(2) == new Vector4(0, 0, 0, 1), "Missing quaternion-role controls retain their selected fallback.");
        mix.SetInputPortDefaultValue(2, new Vector4(.25f, .5f, .75f, 1)); mix.OpType = VisualShaderNumericOpType.Vector4Scalar;
        Check(mix.GetInputPortDefaultValue<float>(2) == .25f, "Vector controls narrow by their first component.");
        mix.Changed += _ => throw new InvalidOperationException("Range observer");
        Reject<InvalidOperationException>(() => mix.OpType = VisualShaderNumericOpType.Vector2Scalar);
        Check(mix.OpType == VisualShaderNumericOpType.Vector2Scalar && mix.GetInputPortDefaultValue<Vector2>(0) is not null, "Observer errors occur after the coherent layout commits.");
        using var multiply = new VisualShaderNodeMultiplyAdd(); multiply.ClearDefaultInputValues(); multiply.OpType = VisualShaderNodeMultiplyAdd.OpTypeEnum.Vector4;
        Check(multiply.GetInputPortDefaultValue<Vector4>(0) == Vector4.Zero && multiply.GetInputPortDefaultValue<Vector4>(1) == Vector4.One, "MultiplyAdd recreates zero/one/zero vector defaults without homogeneous W.");
    }
    private static Vector4 RangeInput(string name, int mode, int port, string variant)
    {
        var width = RangePortWidth(name, mode, port); var values = new float[4];
        for (var component = 0; component < 4; component++)
        {
            var c = width == 1 ? 0 : component;
            var edge = .125f * (c + 1);
            values[component] = name switch
            {
                "Clamp" when mode == 1 => port == 0 ? variant == "outside" ? int.MinValue : 3 : port == 1 ? -1 : 2,
                "Clamp" when mode == 2 => port == 0 ? variant == "outside" ? 0 : 4 : port == 1 ? 1 : 3,
                "Clamp" => port == 1 ? .125f * c : port == 2 ? .5f + .125f * c : variant == "edge" ? .125f * c : variant == "outside" ? -1 : .375f * c - .125f,
                "Mix" => port == 0 ? edge : port == 1 ? 1 - edge : variant == "edge" ? c % 2 : variant == "outside" ? 2 : .25f + .125f * c,
                "Step" => port == 0 ? edge : variant == "edge" ? RangeInput(name, mode, 0, variant)[component] : variant == "outside" ? -1 : .125f * c,
                "SmoothStep" => port == 0 ? .125f * c : port == 1 ? .5f + .125f * c : variant == "edge" ? RangeInput(name, mode, c % 2, variant)[component] : variant == "outside" ? (c % 2 == 0 ? -1 : 2) : .125f * (c + 1),
                "MultiplyAdd" => port == 0 ? variant == "outside" ? -1 : edge : port == 1 ? 2 : variant == "edge" ? -.25f : .125f,
                "Remap" => variant == "outside" ? port switch { 0 => 1.5f + .125f * c, 1 => 1, 2 => 0, 3 => 1, _ => -1 } : port switch { 0 => variant == "edge" ? 0 : edge, 1 => 0, 2 => 1, 3 => .125f * c, _ => 1 - .125f * c },
                _ => throw new InvalidOperationException(name)
            };
        }
        return new(values[0], values[1], values[2], values[3]);
    }
    private static void AuthorRanges(string folder)
    {
        foreach (var name in RangeNames)
            for (var mode = 0; mode < RangeModes(name); mode++)
            {
                foreach (var variant in new[] { "normal", "edge", "outside" }) AuthorRange(folder, name, mode, variant, false);
                if (RangeWidth(name, mode) == 4) AuthorRange(folder, name, mode, "w", false);
            }
        foreach (var name in RangeNames)
        {
            AuthorRange(folder, name, RangeModes(name) - 1, "normal", true);
            using var defaults = new VisualShader(); using var node = CreateRange(name);
            defaults.AddNode(VisualShader.Type.Fragment, node, default, 2); defaults.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 0);
            SaveRange(defaults, folder, "defaults-" + name);
        }
        using (var fused = new VisualShader())
        using (var operation = new VisualShaderNodeMultiplyAdd())
        using (var amplify = new VisualShaderNodeFloatOp { Operator = VisualShaderNodeFloatOp.OperatorEnum.Multiply })
        {
            operation.SetInputPortDefaultValue(0, MathF.BitIncrement(1)); operation.SetInputPortDefaultValue(1, 1 - MathF.ScaleB(1, -23)); operation.SetInputPortDefaultValue(2, -1f);
            amplify.SetInputPortDefaultValue(1, MathF.ScaleB(1, 46));
            fused.AddNode(VisualShader.Type.Fragment, operation, default, 2); fused.AddNode(VisualShader.Type.Fragment, amplify, default, 3);
            fused.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 3, 0); fused.ConnectNodes(VisualShader.Type.Fragment, 3, 0, 0, 0); SaveRange(fused, folder, "fused-witness");
        }
        AuthorRangeProcedural(folder);
        using var graph = new VisualShader(); using var remap = new VisualShaderNodeRemap();
        remap.SetInputPortDefaultValue(0, 1f); remap.SetInputPortDefaultValue(2, 0f);
        graph.AddNode(VisualShader.Type.Fragment, remap, default, 2); graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 0);
        var boundary = IOPath.Combine(folder, "native-boundaries"); Directory.CreateDirectory(boundary);
        SaveRange(graph, boundary, "zero-input-range");
    }
    private static void AuthorRange(string folder, string name, int mode, string variant, bool vertex)
    {
        using var graph = new VisualShader(); using var node = CreateRange(name); SelectRange(node, mode);
        for (var port = 0; port < RangePorts(name); port++) SetRangeDefault(node, name, mode, port, RangeInput(name, mode, port, variant));
        var stage = vertex ? VisualShader.Type.Vertex : VisualShader.Type.Fragment;
        graph.AddNode(stage, node, default, 2);
        using var split = new VisualShaderNodeVectorDecompose { OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector4 };
        if (variant == "w") { graph.AddNode(stage, split, default, 3); graph.ConnectNodes(stage, 2, 0, 3, 0); graph.ConnectNodes(stage, 3, 3, 0, vertex ? 2 : 0); }
        else graph.ConnectNodes(stage, 2, 0, 0, vertex ? 2 : 0);
        SaveRange(graph, folder, $"{name}-{mode}-{variant}" + (vertex ? "-vertex" : ""));
    }
    private static void SaveRange(VisualShader graph, string folder, string name) =>
        ResourceSaver.Save(graph, IOPath.Combine(folder, "range-" + name + ".e2dres"), SaverFlags.BundleResources | SaverFlags.Compress | SaverFlags.SaveBigEndian);
    private static Color RangeExpected(string name)
    {
        if (name.StartsWith("range-defaults-", StringComparison.Ordinal))
        { var value = name.EndsWith("Step", StringComparison.Ordinal) && !name.EndsWith("SmoothStep", StringComparison.Ordinal) ? 1 : name.EndsWith("Mix", StringComparison.Ordinal) || name.EndsWith("SmoothStep", StringComparison.Ordinal) ? .5f : 0; return new(value, value, value, 1); }
        if (name == "range-fused-witness") return new(-1, -1, -1, 1);
        if (name == "range-procedural")
        {
            var d = MathF.Sqrt(2) * .03125f; var t = Math.Clamp(d / .1f, 0, 1); t = t * t * (3 - 2 * t);
            return new(.125f * (1 - t) + .875f * t, .25f * (1 - t) + .75f * t, .5f, 1);
        }
        var parts = name.Split('-'); var operation = parts[1]; var mode = int.Parse(parts[2]); var variant = parts[3];
        var input = Enumerable.Range(0, RangePorts(operation)).Select(p => RangeInput(operation, mode, p, variant)).ToArray();
        var result = new float[4];
        for (var c = 0; c < 4; c++)
        {
            var a = input[0][c]; var b = input[1][c]; var third = input.Length > 2 ? input[2][c] : 0;
            var t = operation == "SmoothStep" ? Math.Clamp((third - a) / (b - a), 0, 1) : 0;
            result[c] = operation switch
            {
                "Clamp" => Math.Clamp(a, b, third),
                "Mix" => a * (1 - third) + b * third,
                "Step" => b < a ? 0 : 1,
                "SmoothStep" => t * t * (3 - 2 * t),
                "MultiplyAdd" => MathF.FusedMultiplyAdd(a, b, third),
                "Remap" => input[3][c] + (input[4][c] - input[3][c]) * ((a - b) / (third - b)),
                _ => throw new InvalidOperationException()
            };
        }
        if (variant == "w") return new(result[3], result[3], result[3], 1);
        return RangeWidth(operation, mode) switch { 1 => new(result[0], result[0], result[0], 1), 2 => new(result[0], result[1], 0, 1), _ => new(result[0], result[1], result[2], 1) };
    }
    private static void AuthorRangeProcedural(string folder)
    {
        using var graph = new VisualShader(); using var uv = new VisualShaderNodeInput { InputName = "uv" };
        using var distance = new VisualShaderNodeVectorDistance { OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector2 };
        using var smooth = new VisualShaderNodeSmoothStep(); using var mix = new VisualShaderNodeMix { OpType = VisualShaderNumericOpType.Vector3Scalar };
        distance.SetInputPortDefaultValue(1, new Vector2(.5f, .5f)); smooth.SetInputPortDefaultValue(1, .1f);
        mix.SetInputPortDefaultValue(0, new Vector3(.125f, .25f, .5f)); mix.SetInputPortDefaultValue(1, new Vector3(.875f, .75f, .5f));
        graph.AddNode(VisualShader.Type.Fragment, uv, default, 2); graph.AddNode(VisualShader.Type.Fragment, distance, default, 3);
        graph.AddNode(VisualShader.Type.Fragment, smooth, default, 4); graph.AddNode(VisualShader.Type.Fragment, mix, default, 5);
        graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 3, 0); graph.ConnectNodes(VisualShader.Type.Fragment, 3, 0, 4, 2);
        graph.ConnectNodes(VisualShader.Type.Fragment, 4, 0, 5, 2); graph.ConnectNodes(VisualShader.Type.Fragment, 5, 0, 0, 0); SaveRange(graph, folder, "procedural");
    }
    private static void VerifyRangeArchive(string fileName, VisualShader graph)
    {
        ReadRanges(graph);
        var parts = fileName.Split('-');
        if (parts.Length < 4 || parts[0] != "range" || !RangeNames.Contains(parts[1])) return;
        var name = parts[1]; var mode = int.Parse(parts[2]); var variant = parts[3];
        var node = graph.GetNode(fileName.EndsWith("-vertex", StringComparison.Ordinal) ? VisualShader.Type.Vertex : VisualShader.Type.Fragment, 2);
        Check(SelectedRange(node) == mode, "Fresh archive retains the exact range selector.");
        for (var port = 0; port < RangePorts(name); port++)
        {
            var value = RangeInput(name, mode, port, variant);
            var valid = name == "Clamp" && mode == 1 ? node.GetInputPortDefaultValue<int>(port) == (int)value.X
                : name == "Clamp" && mode == 2 ? node.GetInputPortDefaultValue<uint>(port) == (uint)value.X
                : RangePortWidth(name, mode, port) switch
                {
                    1 => node.GetInputPortDefaultValue<float>(port) == value.X,
                    2 => node.GetInputPortDefaultValue<Vector2>(port) == new Vector2(value.X, value.Y),
                    3 => node.GetInputPortDefaultValue<Vector3>(port) == new Vector3(value.X, value.Y, value.Z),
                    _ => node.GetInputPortDefaultValue<Vector4>(port) == value
                };
            Check(valid, "Fresh archive retains the exact typed range input.");
        }
    }
    private static void ReadRanges(VisualShader graph)
    {
        foreach (var stage in new[] { VisualShader.Type.Vertex, VisualShader.Type.Fragment })
            foreach (var id in graph.GetNodeList(stage))
            {
                var node = graph.GetNode(stage, id);
                if (node is VisualShaderNodeClamp or VisualShaderNodeMix or VisualShaderNodeStep or VisualShaderNodeSmoothStep or VisualShaderNodeMultiplyAdd or VisualShaderNodeRemap)
                    Check(SelectedRange(node) >= 0 && SelectedRange(node) < RangeModes(node.GetType().Name[16..]), "Fresh range resource retains a valid selected layout.");
            }
    }
}
