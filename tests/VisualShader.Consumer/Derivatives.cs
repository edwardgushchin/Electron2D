using Electron2D;
using IOPath = System.IO.Path;
using D = Electron2D.VisualShaderNodeDerivativeFunc;
using F = Electron2D.VisualShaderFloatOpType;

internal static partial class GraphChecks
{
    private static readonly float[] DerivativeX = [.25f, -.5f, .125f, -.25f];
    private static readonly float[] DerivativeY = [.5f, .125f, -.25f, -.5f];
    private static void DerivativesCPU()
    {
        using var node = new D(); using var multiply = new VisualShaderNodeMultiplyAdd();
        multiply.OpType = node.OpType;
        Check(node.OpType == F.Scalar && node.Function == D.FunctionEnum.Sum && node.Precision == D.PrecisionEnum.None && node.GetInputPortDefaultValue<float>(0) == 0, "Derivative initial selectors and explicit default.");
        for (var from = 0; from < 4; from++) for (var to = 0; to < 4; to++)
            {
                node.OpType = (F)from;
                switch (from)
                {
                    case 0: node.SetInputPortDefaultValue(0, .25f); break;
                    case 1: node.SetInputPortDefaultValue(0, new Vector2(.25f, .5f)); break;
                    case 2: node.SetInputPortDefaultValue(0, new Vector3(.25f, .5f, .75f)); break;
                    default: node.SetInputPortDefaultValue(0, new Vector4(.25f, .5f, .75f, 1)); break;
                }
                node.OpType = (F)to;
                var value = node.GetDefaultInputValues().Single();
                float C(int c) => Math.Min(c, from) switch { 0 => .25f, 1 => .5f, 2 => .75f, _ => 1 };
                Check(to switch
                {
                    0 => ((VisualShaderPortDefault<float>)value).Value == C(0),
                    1 => ((VisualShaderPortDefault<Vector2>)value).Value == new Vector2(C(0), C(1)),
                    2 => ((VisualShaderPortDefault<Vector3>)value).Value == new Vector3(C(0), C(1), C(2)),
                    _ => ((VisualShaderPortDefault<Vector4>)value).Value == new Vector4(C(0), C(1), C(2), C(3))
                }, "All derivative layout transitions preserve values and repeat the last component.");
            }
        for (var to = 0; to < 4; to++)
        {
            node.OpType = (F)((to + 1) % 4); node.ClearDefaultInputValues(); node.OpType = (F)to;
            Check(node.GetDefaultInputValues().Count == 1 && (to != 3 || node.GetInputPortDefaultValue<Vector4>(0) == new Vector4(0, 0, 0, 1)), "A layout change recreates the absent default, including homogeneous W.");
        }
        var changes = 0; node.Changed += _ => changes++;
        node.OpType = node.OpType; node.Function = node.Function; node.Precision = node.Precision;
        Check(changes == 0, "Repeated derivative selections are quiet.");
        Reject<ArgumentOutOfRangeException>(() => node.OpType = F.Max);
        Reject<ArgumentOutOfRangeException>(() => node.OpType = (F)(-1));
        Reject<ArgumentOutOfRangeException>(() => node.Function = D.FunctionEnum.Max);
        Reject<ArgumentOutOfRangeException>(() => node.Function = (D.FunctionEnum)99);
        Reject<ArgumentOutOfRangeException>(() => node.Precision = D.PrecisionEnum.Max);
        Reject<ArgumentOutOfRangeException>(() => node.Precision = (D.PrecisionEnum)(-1));
        Reject<ArgumentException>(() => node.SetInputPortDefaultValue(0, new Vector4(0, float.NaN, 0, 1)));
        Check(changes == 0 && node.OpType == F.Vector4, "Invalid selectors and inputs preserve the full derivative definition.");
        node.Function = D.FunctionEnum.Y; node.Precision = D.PrecisionEnum.Fine;
        using var copy = (D)node.Duplicate(true); using var target = new D(); target.CopyFromResource(node);
        Check(copy.OpType == node.OpType && copy.Function == node.Function && copy.Precision == node.Precision && target.GetInputPortDefaultValue<Vector4>(0) == node.GetInputPortDefaultValue<Vector4>(0), "Copies preserve derivative selectors and typed defaults.");
        node.ClearDefaultInputValues(); using var empty = (D)node.Duplicate(); Check(empty.GetDefaultInputValues().Count == 0, "Derivative copies retain absent defaults.");
        var folder = IOPath.Combine(IOPath.GetTempPath(), "e2d-derivatives-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            for (var width = 0; width < 4; width++) for (var presence = 0; presence < 2; presence++)
                {
                    using var authored = new D { OpType = (F)width, Function = D.FunctionEnum.Y, Precision = D.PrecisionEnum.Fine };
                    if (presence == 0) authored.ClearDefaultInputValues();
                    else switch (width)
                        {
                            case 0: authored.SetInputPortDefaultValue(0, .25f); break;
                            case 1: authored.SetInputPortDefaultValue(0, new Vector2(.25f, .5f)); break;
                            case 2: authored.SetInputPortDefaultValue(0, new Vector3(.25f, .5f, .75f)); break;
                            default: authored.SetInputPortDefaultValue(0, new Vector4(.25f, .5f, .75f, 1)); break;
                        }
                    var file = IOPath.Combine(folder, "layout.e2dres");
                    ResourceSaver.Save(authored, file, SaverFlags.BundleResources | SaverFlags.Compress | SaverFlags.SaveBigEndian);
                    using var restored = ResourceLoader.Load<D>(file, ResourceLoader.CacheMode.IgnoreDeep);
                    var sameValue = presence == 0 || (width switch
                    {
                        0 => restored.GetInputPortDefaultValue<float>(0) == authored.GetInputPortDefaultValue<float>(0),
                        1 => restored.GetInputPortDefaultValue<Vector2>(0) == authored.GetInputPortDefaultValue<Vector2>(0),
                        2 => restored.GetInputPortDefaultValue<Vector3>(0) == authored.GetInputPortDefaultValue<Vector3>(0),
                        _ => restored.GetInputPortDefaultValue<Vector4>(0) == authored.GetInputPortDefaultValue<Vector4>(0)
                    });
                    Check(restored.OpType == authored.OpType && restored.Function == authored.Function && restored.Precision == authored.Precision && restored.GetDefaultInputValues().Count == presence && sameValue && (presence == 0 || restored.GetDefaultInputValues().Single().ValueType == authored.GetDefaultInputValues().Single().ValueType), "Fresh derivative layout/default restoration orders selectors before typed values and preserves absence.");
                }
        }
        finally { Directory.Delete(folder, true); }
        node.Changed += _ => throw new InvalidOperationException("Derivative observer");
        Reject<InvalidOperationException>(() => node.OpType = F.Vector2);
        Check(node.OpType == F.Vector2 && node.GetInputPortDefaultValue<Vector2>(0) == Vector2.Zero, "Layout and default commit before observer failure.");
        Reject<InvalidOperationException>(() => node.Function = D.FunctionEnum.X);
        Reject<InvalidOperationException>(() => node.Precision = D.PrecisionEnum.Coarse);
        Check(node.Function == D.FunctionEnum.X && node.Precision == D.PrecisionEnum.Coarse, "Derivative selectors commit before observers run.");
        node.Dispose(); Reject<ObjectDisposedException>(() => node.OpType = 0); Reject<ObjectDisposedException>(() => node.Function = 0); Reject<ObjectDisposedException>(() => node.Precision = 0);
    }
    internal static void AuthorDerivatives(string folder)
    {
        Directory.CreateDirectory(folder); Directory.CreateDirectory(IOPath.Combine(folder, "invalid"));
        for (var width = 0; width < 4; width++) for (var function = 0; function < 3; function++) for (var precision = 0; precision < 3; precision++)
                {
                    for (var source = 0; source < 3; source++) for (var component = 0; component < (source == 2 ? width + 1 : 1); component++)
                            SaveDerivative(folder, $"derivative-{width}-{function}-{precision}-{source}-{component}", width, function, precision, source, component);
                    SaveDerivative(IOPath.Combine(folder, "invalid"), $"derivative-vertex-{width}-{function}-{precision}", width, function, precision, 0, 0, vertex: true);
                }
        for (var precision = 0; precision < 3; precision++) SaveDerivative(folder, $"derivative-aa-{precision}", 0, 0, precision, 3, 0);
        using var graph = new VisualShader(); using var unused = new D { OpType = F.Vector4, Precision = D.PrecisionEnum.Fine };
        graph.AddNode(VisualShader.Type.Vertex, unused, default, 2);
        ResourceSaver.Save(graph, IOPath.Combine(folder, "derivative-unused-vertex.e2dres"), SaverFlags.BundleResources);
        Console.WriteLine("Authored 166 derivative programs and 36 invalid vertex-stage graphs.");
    }
    private static void SaveDerivative(string folder, string name, int width, int function, int precision, int source, int component, bool vertex = false)
    {
        using var graph = new VisualShader(); var owned = new List<VisualShaderNode>(); var stage = vertex ? VisualShader.Type.Vertex : VisualShader.Type.Fragment;
        int Add(VisualShaderNode node) { var id = owned.Count + 2; owned.Add(node); graph.AddNode(stage, node, default, id); return id; }
        void Link(int from, int to, int port, int output = 0) => graph.ConnectNodes(stage, from, output, to, port);
        void Save() => ResourceSaver.Save(graph, IOPath.Combine(folder, name + ".e2dres"), SaverFlags.BundleResources | (precision == 2 ? SaverFlags.Compress | SaverFlags.SaveBigEndian : 0));
        try
        {
            var definition = new D { OpType = (F)width, Function = (D.FunctionEnum)function, Precision = (D.PrecisionEnum)precision }; var derivative = Add(definition);
            graph.AddNode(stage, definition, default, 900);
            if (source == 0) definition.ClearDefaultInputValues();
            if (source == 1)
                switch (width)
                {
                    case 0: definition.SetInputPortDefaultValue(0, .25f); break;
                    case 1: definition.SetInputPortDefaultValue(0, new Vector2(.25f, .5f)); break;
                    case 2: definition.SetInputPortDefaultValue(0, new Vector3(.25f, .5f, .75f)); break;
                    default: definition.SetInputPortDefaultValue(0, new Vector4(.25f, .5f, .75f, 1)); break;
                }
            var selected = derivative;
            if (source >= 2)
            {
                var uv = Add(new VisualShaderNodeInput { InputName = "uv" }); var split = Add(new VisualShaderNodeVectorDecompose { OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector2 }); Link(uv, split, 0);
                if (source == 3)
                {
                    Link(split, derivative, 0);
                    var low = Add(new VisualShaderNodeMultiplyAdd()); var high = Add(new VisualShaderNodeMultiplyAdd());
                    ((VisualShaderNodeMultiplyAdd)graph.GetNode(stage, low)).SetInputPortDefaultValue(1, -.5f);
                    ((VisualShaderNodeMultiplyAdd)graph.GetNode(stage, low)).SetInputPortDefaultValue(2, .515625f);
                    ((VisualShaderNodeMultiplyAdd)graph.GetNode(stage, high)).SetInputPortDefaultValue(1, .5f);
                    ((VisualShaderNodeMultiplyAdd)graph.GetNode(stage, high)).SetInputPortDefaultValue(2, .515625f);
                    Link(derivative, low, 0); Link(derivative, high, 0);
                    var mask = Add(new VisualShaderNodeSmoothStep()); Link(low, mask, 0); Link(high, mask, 1); Link(split, mask, 2); Link(mask, 0, 0); Save(); return;
                }
                var compose = width == 0 ? -1 : Add(new VisualShaderNodeVectorCompose { OpType = (VisualShaderNodeVectorBase.OpTypeEnum)(width - 1) });
                for (var c = 0; c <= width; c++)
                {
                    var x = new VisualShaderNodeFloatOp { Operator = VisualShaderNodeFloatOp.OperatorEnum.Multiply }; x.SetInputPortDefaultValue(1, DerivativeX[c]); var xid = Add(x); Link(split, xid, 0);
                    var sum = new VisualShaderNodeMultiplyAdd(); sum.SetInputPortDefaultValue(1, DerivativeY[c]); var sumid = Add(sum); Link(split, sumid, 0, 1); Link(xid, sumid, 2);
                    if (width == 0) Link(sumid, derivative, 0); else Link(sumid, compose, c);
                }
                if (width > 0) Link(compose, derivative, 0);
            }
            if (width > 0)
            {
                var decompose = Add(new VisualShaderNodeVectorDecompose { OpType = (VisualShaderNodeVectorBase.OpTypeEnum)(width - 1) }); Link(derivative, decompose, 0); selected = decompose;
            }
            var scale = new VisualShaderNodeMultiplyAdd(); scale.SetInputPortDefaultValue(1, 8f); scale.SetInputPortDefaultValue(2, .5f); var scaled = Add(scale); Link(selected, scaled, 0, width > 0 ? component : 0); Link(scaled, 0, 0); Save();
        }
        finally
        {
            foreach (var node in owned) node.Dispose();
        }
    }
    private static Color DerivativeExpected(string name)
    {
        if (name == "derivative-unused-vertex") return Colors.White;
        if (name.StartsWith("derivative-aa-", StringComparison.Ordinal)) return new(.84375f, .84375f, .84375f, 1);
        var pieces = name.Split('-'); var function = int.Parse(pieces[2]); var source = int.Parse(pieces[4]); var c = int.Parse(pieces[5]);
        var value = source != 2 ? .5f : .5f + .5f * (function == 0 ? MathF.Abs(DerivativeX[c]) + MathF.Abs(DerivativeY[c]) : function == 1 ? DerivativeX[c] : DerivativeY[c]);
        return new(value, value, value, 1);
    }
    private static void VerifyDerivativeArchive(string name, VisualShader graph)
    {
        if (name == "derivative-unused-vertex")
        { Check(((D)graph.GetNode(VisualShader.Type.Vertex, 2)).Precision == D.PrecisionEnum.Fine, "Unreachable vertex definitions remain authorable but do not execute derivatives."); return; }
        var pieces = name.Split('-'); var aa = pieces[1] == "aa"; var width = aa ? 0 : int.Parse(pieces[1]); var function = aa ? 0 : int.Parse(pieces[2]); var precision = int.Parse(pieces[aa ? 2 : 3]);
        var definition = (D)graph.GetNode(VisualShader.Type.Fragment, 2);
        Check(definition.OpType == (F)width && definition.Function == (D.FunctionEnum)function && definition.Precision == (D.PrecisionEnum)precision && ReferenceEquals(definition, graph.GetNode(VisualShader.Type.Fragment, 900)), "Fresh derivative archives retain selectors and borrowed aliases.");
        if (aa || pieces[4] == "2")
        {
            var expected = precision switch { 0 => function switch { 0 => 209u, 1 => 207u, _ => 208u }, 1 => function switch { 0 => 215u, 1 => 213u, _ => 214u }, _ => function switch { 0 => 212u, 1 => 210u, _ => 211u } };
            var words = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, uint>(graph.GetSPIRV(RenderingDevice.ShaderStage.Fragment)); var found = false;
            for (var at = 5; at < words.Length; at += (int)(words[at] >> 16)) if ((words[at] & 0xffff) == expected) found = true;
            Check(found, "The requested default/fine/coarse derivative instruction survives import.");
        }
        else Check((pieces[4] == "0") == (definition.GetDefaultInputValues().Count == 0), "Derivative archive retains authored default presence.");
    }
}
