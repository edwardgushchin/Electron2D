using Electron2D;
using IOPath = System.IO.Path;

internal static partial class GraphChecks
{
    private static readonly string[] IntegerKinds = ["si", "ui", "sf", "uf"];
    private static bool UnsignedInteger(string kind) => kind[0] == 'u';
    private static bool IntegerFunction(string kind) => kind[1] == 'f';
    private static int IntegerSelections(string kind) => IntegerFunction(kind) ? UnsignedInteger(kind) ? 2 : 4 : 12;
    private static VisualShaderNode NewInteger(string kind) => kind switch
    { "si" => new VisualShaderNodeIntOp(), "ui" => new VisualShaderNodeUIntOp(), "sf" => new VisualShaderNodeIntFunc(), _ => new VisualShaderNodeUIntFunc() };
    private static void SelectInteger(VisualShaderNode node, int selection)
    {
        switch (node)
        {
            case VisualShaderNodeIntOp n: n.Operator = (VisualShaderIntegerOperator)selection; break;
            case VisualShaderNodeUIntOp n: n.Operator = (VisualShaderIntegerOperator)selection; break;
            case VisualShaderNodeIntFunc n: n.Function = (VisualShaderNodeIntFunc.FunctionEnum)selection; break;
            case VisualShaderNodeUIntFunc n: n.Function = (VisualShaderNodeUIntFunc.FunctionEnum)selection; break;
        }
    }
    private static int SelectedInteger(VisualShaderNode node) => node switch
    { VisualShaderNodeIntOp n => (int)n.Operator, VisualShaderNodeUIntOp n => (int)n.Operator, VisualShaderNodeIntFunc n => (int)n.Function, VisualShaderNodeUIntFunc n => (int)n.Function, _ => -1 };
    private static void IntegerDefault(VisualShaderNode node, bool unsigned, int port, uint bits)
    { if (unsigned) node.SetInputPortDefaultValue(port, bits); else node.SetInputPortDefaultValue(port, unchecked((int)bits)); }
    private static uint? IntegerDefaultBits(VisualShaderNode node, bool unsigned, int port) => unsigned
        ? node.GetInputPortDefaultValue<uint>(port) : node.GetInputPortDefaultValue<int>(port) is { } value ? unchecked((uint)value) : null;
    private static void IntegersCPU()
    {
        foreach (var kind in IntegerKinds)
        {
            using var initial = NewInteger(kind); var ports = IntegerFunction(kind) ? 1 : 2;
            Check(SelectedInteger(initial) == (kind == "sf" ? 2 : 0) && initial.GetDefaultInputValues().Count == ports, "Initial integer selectors and explicit zero defaults.");
            Reject<ArgumentException>(() => { if (UnsignedInteger(kind)) initial.SetInputPortDefaultValue(0, 0); else initial.SetInputPortDefaultValue(0, 0u); });
            Reject<ArgumentOutOfRangeException>(() => initial.RemoveInputPortDefaultValue(ports));
            for (var selection = 0; selection < IntegerSelections(kind); selection++)
            {
                using var node = NewInteger(kind); var changes = 0; node.Changed += _ => changes++;
                var before = SelectedInteger(node); SelectInteger(node, selection); SelectInteger(node, selection);
                Check(changes == (before == selection ? 0 : 1), "One committed integer selector change; repeated selection is quiet.");
                for (var port = 0; port < ports; port++) IntegerDefault(node, UnsignedInteger(kind), port, 0x89abcdefu + (uint)port);
                var defaults = node.GetDefaultInputValues();
                Reject<ArgumentOutOfRangeException>(() => SelectInteger(node, -1));
                Reject<ArgumentOutOfRangeException>(() => SelectInteger(node, IntegerSelections(kind)));
                Check(SelectedInteger(node) == selection && defaults.SequenceEqual(node.GetDefaultInputValues()), "Invalid selectors preserve the complete integer definition.");
                using var copy = (VisualShaderNode)node.Duplicate(true); using var assigned = NewInteger(kind); assigned.CopyFromResource(node);
                Check(SelectedInteger(copy) == selection && SelectedInteger(assigned) == selection && IntegerDefaultBits(copy, UnsignedInteger(kind), 0) == 0x89abcdefu && IntegerDefaultBits(assigned, UnsignedInteger(kind), 0) == 0x89abcdefu, "Integer selectors and all bits survive duplicate and copy.");
                node.ClearDefaultInputValues();
                using var emptyCopy = (VisualShaderNode)node.Duplicate();
                Check(emptyCopy.GetDefaultInputValues().Count == 0, "Integer copies preserve absent defaults.");
                SelectInteger(node, (selection + 1) % IntegerSelections(kind));
                Check(node.GetDefaultInputValues().Count == 0, "Integer operation changes preserve default absence.");
            }
            using var observed = NewInteger(kind); observed.Changed += _ => throw new InvalidOperationException("Integer observer");
            var next = (SelectedInteger(observed) + 1) % IntegerSelections(kind);
            Reject<InvalidOperationException>(() => SelectInteger(observed, next)); Check(SelectedInteger(observed) == next, "Observer errors occur after integer selection commits.");
            observed.Dispose(); Reject<ObjectDisposedException>(() => SelectInteger(observed, 0));
        }
        using var maximum = new VisualShaderNodeIntOp { Operator = VisualShaderIntegerOperator.Max };
        Check(maximum.Operator == VisualShaderIntegerOperator.Max, "Max is an actual binary operation; EnumSize is its sentinel.");
    }
    private static (uint A, uint B) IntegerInputs(string kind, int selection, int variant)
    {
        var unsigned = UnsignedInteger(kind);
        if (IntegerFunction(kind))
        {
            uint value = unsigned ? selection == 0 ? variant switch { 0 => 0, 1 => 1, _ => uint.MaxValue } : variant switch { 0 => 0, 1 => uint.MaxValue, _ => 0x89abcdef }
                : selection == 2 ? variant switch { 0 => 0x80000000, 1 => 0, _ => int.MaxValue } : variant switch { 0 => unchecked((uint)-123456789), 1 => 0, _ => int.MaxValue };
            return (value, 0);
        }
        var op = (VisualShaderIntegerOperator)selection;
        if (op is VisualShaderIntegerOperator.BitwiseLeftShift or VisualShaderIntegerOperator.BitwiseRightShift)
            return variant switch { 0 => (0x12345678, 0), 1 => (0xfedcba98, 7), _ => (op == VisualShaderIntegerOperator.BitwiseLeftShift ? 1u : 0x80000001, 31) };
        if (variant == 2) return op switch
        {
            VisualShaderIntegerOperator.Add => (unsigned ? uint.MaxValue : int.MaxValue, 1),
            VisualShaderIntegerOperator.Subtract => (unsigned ? 0u : 0x80000000, 1),
            VisualShaderIntegerOperator.Multiply => (unsigned ? uint.MaxValue : 0x40000001, unsigned ? 2u : 4u),
            VisualShaderIntegerOperator.Divide => (unsigned ? uint.MaxValue : 0x80000000, 1),
            VisualShaderIntegerOperator.Modulo => (unsigned ? uint.MaxValue : int.MaxValue, 256),
            VisualShaderIntegerOperator.Max or VisualShaderIntegerOperator.Min => (unsigned ? 0u : 0x80000000, unsigned ? uint.MaxValue : int.MaxValue),
            VisualShaderIntegerOperator.BitwiseAnd => (0x89abcdef, 0xf0f00ff0),
            VisualShaderIntegerOperator.BitwiseOr => (0x0f0f0f0f, 0xf0f0f0f0),
            _ => (uint.MaxValue, uint.MaxValue)
        };
        var a = variant == 0 ? 123456789u : unsigned ? 0xfedcba98 : unchecked((uint)-123456789);
        if (op == VisualShaderIntegerOperator.Modulo && !unsigned) a = 123456789;
        return (a, op is VisualShaderIntegerOperator.BitwiseAnd or VisualShaderIntegerOperator.BitwiseOr or VisualShaderIntegerOperator.BitwiseXor ? 0x0f0ff0f0u : 37u);
    }
    private static uint IntegerResult(string kind, int selection, uint bits, uint right)
    {
        if (IntegerFunction(kind)) return kind == "uf" ? selection == 0 ? unchecked(0u - bits) : ~bits
            : selection switch { 0 => unchecked((uint)Math.Abs((int)bits)), 1 => unchecked((uint)-(int)bits), 2 => unchecked((uint)Math.Sign((int)bits)), _ => ~bits };
        var op = (VisualShaderIntegerOperator)selection;
        if (UnsignedInteger(kind)) return op switch
        {
            VisualShaderIntegerOperator.Add => unchecked(bits + right),
            VisualShaderIntegerOperator.Subtract => unchecked(bits - right),
            VisualShaderIntegerOperator.Multiply => unchecked(bits * right),
            VisualShaderIntegerOperator.Divide => bits / right,
            VisualShaderIntegerOperator.Modulo => bits % right,
            VisualShaderIntegerOperator.Max => Math.Max(bits, right),
            VisualShaderIntegerOperator.Min => Math.Min(bits, right),
            VisualShaderIntegerOperator.BitwiseAnd => bits & right,
            VisualShaderIntegerOperator.BitwiseOr => bits | right,
            VisualShaderIntegerOperator.BitwiseXor => bits ^ right,
            VisualShaderIntegerOperator.BitwiseLeftShift => bits << (int)right,
            _ => bits >> (int)right
        };
        var a = unchecked((int)bits); var b = unchecked((int)right);
        return unchecked((uint)(op switch
        {
            VisualShaderIntegerOperator.Add => unchecked(a + b),
            VisualShaderIntegerOperator.Subtract => unchecked(a - b),
            VisualShaderIntegerOperator.Multiply => unchecked(a * b),
            VisualShaderIntegerOperator.Divide => a / b,
            VisualShaderIntegerOperator.Modulo => a % b,
            VisualShaderIntegerOperator.Max => Math.Max(a, b),
            VisualShaderIntegerOperator.Min => Math.Min(a, b),
            VisualShaderIntegerOperator.BitwiseAnd => a & b,
            VisualShaderIntegerOperator.BitwiseOr => a | b,
            VisualShaderIntegerOperator.BitwiseXor => a ^ b,
            VisualShaderIntegerOperator.BitwiseLeftShift => a << b,
            _ => a >> b
        }));
    }
    private static void AuthorIntegers(string folder)
    {
        foreach (var kind in IntegerKinds)
        {
            for (var selection = 0; selection < IntegerSelections(kind); selection++)
            {
                for (var variant = 0; variant < 3; variant++)
                    foreach (var high in new[] { false, true }) AuthorInteger(folder, kind, selection, variant, high, false, false, false);
                foreach (var high in new[] { false, true }) AuthorInteger(folder, kind, selection, 1, high, true, false, false);
            }
            foreach (var high in new[] { false, true })
            {
                AuthorInteger(folder, kind, kind == "sf" ? 2 : 0, 0, high, false, true, false);
                AuthorInteger(folder, kind, IntegerFunction(kind) ? kind == "sf" ? 3 : 1 : 8, 0, high, false, false, true);
            }
        }
        foreach (var high in new[] { false, true }) AuthorIntegerProcedural(folder, high);
        var boundary = IOPath.Combine(folder, "native-boundaries"); Directory.CreateDirectory(boundary);
        foreach (var item in IntegerBoundaries())
        {
            using var graph = new VisualShader(); using var node = NewInteger(item.Kind); SelectInteger(node, item.Selection);
            IntegerDefault(node, UnsignedInteger(item.Kind), 0, item.A);
            if (!IntegerFunction(item.Kind)) IntegerDefault(node, UnsignedInteger(item.Kind), 1, item.B);
            graph.AddNode(VisualShader.Type.Fragment, node, default, 2); graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 0);
            SaveInteger(graph, boundary, "boundary-" + item.Name);
        }
    }
    private static void AuthorInteger(string folder, string kind, int selection, int variant, bool high, bool vertex, bool initial, bool cleared)
    {
        using var graph = new VisualShader(); using var node = NewInteger(kind); var owned = new List<VisualShaderNode>();
        var stage = vertex ? VisualShader.Type.Vertex : VisualShader.Type.Fragment;
        try
        {
            if (!initial)
            {
                SelectInteger(node, selection);
                var input = IntegerInputs(kind, selection, variant); IntegerDefault(node, UnsignedInteger(kind), 0, input.A);
                if (!IntegerFunction(kind)) IntegerDefault(node, UnsignedInteger(kind), 1, input.B);
                if (cleared) node.ClearDefaultInputValues();
            }
            graph.AddNode(stage, node, default, 2); EncodeInteger(graph, stage, 2, high, owned);
            var name = initial ? "init-" + kind : cleared ? "cleared-" + kind : $"{kind}-{selection}-{variant}";
            SaveInteger(graph, folder, name + (high ? "-hi" : "-lo") + (vertex ? "-vertex" : ""));
        }
        finally { foreach (var item in owned) item.Dispose(); }
    }
    private static void EncodeInteger(VisualShader graph, VisualShader.Type stage, int input, bool high, List<VisualShaderNode> owned)
    {
        var next = 200; var channels = new int[3];
        for (var c = 0; c < (high ? 1 : 3); c++)
        {
            var shift = new VisualShaderNodeUIntOp { Operator = VisualShaderIntegerOperator.BitwiseRightShift };
            shift.SetInputPortDefaultValue(1, (uint)(high ? 24 : c * 8));
            var mask = new VisualShaderNodeUIntOp { Operator = VisualShaderIntegerOperator.BitwiseAnd }; mask.SetInputPortDefaultValue(1, 255u);
            var scale = new VisualShaderNodeFloatOp { Operator = VisualShaderNodeFloatOp.OperatorEnum.Divide }; scale.SetInputPortDefaultValue(1, 255f);
            owned.AddRange([shift, mask, scale]); var id = next; next += 3;
            graph.AddNode(stage, shift, default, id); graph.AddNode(stage, mask, default, id + 1); graph.AddNode(stage, scale, default, id + 2);
            graph.ConnectNodes(stage, input, 0, id, 0); graph.ConnectNodes(stage, id, 0, id + 1, 0); graph.ConnectNodes(stage, id + 1, 0, id + 2, 0); channels[c] = id + 2;
        }
        if (high) graph.ConnectNodes(stage, channels[0], 0, 0, stage == VisualShader.Type.Vertex ? 2 : 0);
        else
        {
            var join = new VisualShaderNodeVectorCompose(); owned.Add(join); graph.AddNode(stage, join, default, next);
            for (var c = 0; c < 3; c++) graph.ConnectNodes(stage, channels[c], 0, next, c);
            graph.ConnectNodes(stage, next, 0, 0, stage == VisualShader.Type.Vertex ? 2 : 0);
        }
    }
    private static void AuthorIntegerProcedural(string folder, bool high)
    {
        using var graph = new VisualShader(); var owned = new List<VisualShaderNode>();
        try
        {
            var uv = new VisualShaderNodeInput { InputName = "uv" }; var split = new VisualShaderNodeVectorDecompose { OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector2 };
            var scale = new VisualShaderNodeFloatOp { Operator = VisualShaderNodeFloatOp.OperatorEnum.Multiply }; scale.SetInputPortDefaultValue(1, 64f);
            var multiply = new VisualShaderNodeUIntOp { Operator = VisualShaderIntegerOperator.Multiply }; multiply.SetInputPortDefaultValue(1, 1664525u);
            var add = new VisualShaderNodeUIntOp { Operator = VisualShaderIntegerOperator.Add }; add.SetInputPortDefaultValue(1, 1013904223u);
            var shift = new VisualShaderNodeUIntOp { Operator = VisualShaderIntegerOperator.BitwiseRightShift }; shift.SetInputPortDefaultValue(1, 16u);
            var xor = new VisualShaderNodeUIntOp { Operator = VisualShaderIntegerOperator.BitwiseXor };
            owned.AddRange([uv, split, scale, multiply, add, shift, xor]);
            for (var i = 0; i < owned.Count; i++) graph.AddNode(VisualShader.Type.Fragment, owned[i], default, i + 2);
            for (var i = 2; i < 7; i++) graph.ConnectNodes(VisualShader.Type.Fragment, i, 0, i + 1, 0);
            graph.ConnectNodes(VisualShader.Type.Fragment, 6, 0, 8, 0); graph.ConnectNodes(VisualShader.Type.Fragment, 7, 0, 8, 1);
            EncodeInteger(graph, VisualShader.Type.Fragment, 8, high, owned); SaveInteger(graph, folder, "procedural" + (high ? "-hi" : "-lo"));
        }
        finally { foreach (var item in owned) item.Dispose(); }
    }
    private static void SaveInteger(VisualShader graph, string folder, string name) => ResourceSaver.Save(graph, IOPath.Combine(folder, "integer-" + name + ".e2dres"), SaverFlags.BundleResources | SaverFlags.Compress | SaverFlags.SaveBigEndian);
    private static (string Name, string Kind, int Selection, uint A, uint B)[] IntegerBoundaries() =>
    [
        ("signed-div-zero", "si", 3, 7, 0), ("unsigned-div-zero", "ui", 3, 7, 0),
        ("signed-mod-zero", "si", 4, 7, 0), ("unsigned-mod-zero", "ui", 4, 7, 0),
        ("signed-div-overflow", "si", 3, 0x80000000, uint.MaxValue),
        ("signed-mod-negative", "si", 4, unchecked((uint)-7), 3),
        ("signed-shift-wide", "si", 10, 1, 32), ("unsigned-shift-wide", "ui", 10, 1, 32),
        ("signed-shift-negative", "si", 11, 1, uint.MaxValue), ("signed-abs-min", "sf", 0, 0x80000000, 0)
    ];
    private static Color IntegerExpected(string name)
    {
        var parts = name.Split('-'); uint value;
        if (parts[1] == "procedural") { value = unchecked(34u * 1664525u + 1013904223u); value ^= value >> 16; }
        else if (parts[1] == "init") value = 0;
        else if (parts[1] == "cleared") value = IntegerFunction(parts[2]) ? uint.MaxValue : 0;
        else { var selection = int.Parse(parts[2]); var input = IntegerInputs(parts[1], selection, int.Parse(parts[3])); value = IntegerResult(parts[1], selection, input.A, input.B); }
        if (parts.Contains("hi")) { var high = (value >> 24) / 255f; return new(high, high, high, 1); }
        return new((value & 255) / 255f, ((value >> 8) & 255) / 255f, ((value >> 16) & 255) / 255f, 1);
    }
    private static void VerifyIntegerArchive(string name, VisualShader graph)
    {
        var parts = name.Split('-');
        if (parts[0] != "integer" || parts[1] == "procedural") return;
        var boundary = parts[1] == "boundary"; var special = parts[1] is "init" or "cleared";
        var item = boundary ? IntegerBoundaries().Single(x => name == "integer-boundary-" + x.Name) : default;
        var kind = boundary ? item.Kind : special ? parts[2] : parts[1];
        var node = graph.GetNode(parts.Contains("vertex") ? VisualShader.Type.Vertex : VisualShader.Type.Fragment, 2);
        var selection = boundary ? item.Selection : parts[1] == "init" ? kind == "sf" ? 2 : 0 : parts[1] == "cleared" ? IntegerFunction(kind) ? kind == "sf" ? 3 : 1 : 8 : int.Parse(parts[2]);
        Check(SelectedInteger(node) == selection, "Fresh integer archive retains the exact selector.");
        if (parts[1] == "cleared") { Check(node.GetDefaultInputValues().Count == 0, "Fresh integer archive preserves absent defaults."); return; }
        var values = boundary ? (item.A, item.B) : parts[1] == "init" ? (0u, 0u) : IntegerInputs(kind, selection, int.Parse(parts[3]));
        Check(IntegerDefaultBits(node, UnsignedInteger(kind), 0) == values.Item1, "Fresh integer archive retains every input bit.");
        if (!IntegerFunction(kind)) Check(IntegerDefaultBits(node, UnsignedInteger(kind), 1) == values.Item2, "Fresh integer archive retains the second input exactly.");
    }
}
