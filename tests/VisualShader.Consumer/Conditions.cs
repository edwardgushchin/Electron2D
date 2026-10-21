using Electron2D;
using IOPath = System.IO.Path;

internal static partial class GraphChecks
{
    private static object SelectionValue(int type, bool first) => type switch
    {
        0 => first ? .75f : .25f,
        1 => first ? unchecked((int)0x89abcdef) : 0x76543210,
        2 => first ? 0x89abcdefu : 0x76543210u,
        3 => first ? new Vector2(.75f, .25f) : new Vector2(.25f, .75f),
        4 => first ? new Vector3(.75f, .25f, .5f) : new Vector3(.25f, .75f, .5f),
        5 => first ? new Vector4(.75f, .25f, .5f, .875f) : new Vector4(.25f, .75f, .5f, .125f),
        _ => first
    };
    private static void SetConditionDefault(VisualShaderNode node, int port, object value)
    {
        switch (value)
        {
            case float v: node.SetInputPortDefaultValue(port, v); break;
            case int v: node.SetInputPortDefaultValue(port, v); break;
            case uint v: node.SetInputPortDefaultValue(port, v); break;
            case bool v: node.SetInputPortDefaultValue(port, v); break;
            case Vector2 v: node.SetInputPortDefaultValue(port, v); break;
            case Vector3 v: node.SetInputPortDefaultValue(port, v); break;
            case Vector4 v: node.SetInputPortDefaultValue(port, v); break;
        }
    }
    private static object? ConditionDefault(VisualShaderNode node, int type, int port) => type switch
    {
        0 => node.GetInputPortDefaultValue<float>(port),
        1 => node.GetInputPortDefaultValue<int>(port),
        2 => node.GetInputPortDefaultValue<uint>(port),
        3 => node.GetInputPortDefaultValue<Vector2>(port),
        4 => node.GetInputPortDefaultValue<Vector3>(port),
        5 => node.GetInputPortDefaultValue<Vector4>(port),
        _ => node.GetInputPortDefaultValue<bool>(port)
    };
    private static void ConditionsCPU()
    {
        for (var from = 0; from < 7; from++)
            for (var to = 0; to < 7; to++)
            {
                using var compare = new VisualShaderNodeCompare { Type = (VisualShaderValueType)from };
                using var select = new VisualShaderNodeSwitch { OpType = (VisualShaderValueType)from };
                object previous = from switch { 0 => .25f, 1 => 2, 2 => 2u, 3 => new Vector2(.25f, .5f), 4 => new Vector3(.25f, .5f, .75f), 5 => new Vector4(.25f, .5f, .75f, 1), _ => true };
                SetConditionDefault(compare, 0, previous); SetConditionDefault(select, 1, previous);
                compare.SetInputPortDefaultValue(2, .125f); select.SetInputPortDefaultValue(0, true);
                var changes = 0; compare.Changed += _ => changes++;
                compare.Type = (VisualShaderValueType)to; compare.Type = (VisualShaderValueType)to;
                select.OpType = (VisualShaderValueType)to;
                Check(changes == (from == to ? 0 : 1) && compare.GetInputPortDefaultValue<float>(2) == .125f && select.GetInputPortDefaultValue<bool>(0) == true, "Type changes preserve dormant tolerance and the unchanged condition.");
                Check(ConditionDefault(compare, to, 0) is not null && ConditionDefault(select, to, 1) is not null, "Both converted operands retain their selected types.");
                var before = compare.GetDefaultInputValues();
                foreach (var invalid in new[] { -1, 8, 99 })
                {
                    Reject<ArgumentOutOfRangeException>(() => compare.Type = (VisualShaderValueType)invalid);
                    Reject<ArgumentOutOfRangeException>(() => select.OpType = (VisualShaderValueType)invalid);
                }
                Check(before.SequenceEqual(compare.GetDefaultInputValues()), "Invalid selectors preserve every stored default.");
                using var copy = (VisualShaderNodeCompare)compare.Duplicate(true); using var assigned = new VisualShaderNodeCompare(); assigned.CopyFromResource(compare);
                Check(copy.Type == compare.Type && assigned.Type == compare.Type && Equals(ConditionDefault(copy, to, 0), ConditionDefault(compare, to, 0)) && copy.GetInputPortDefaultValue<float>(2) == .125f, "Compare duplicate/copy retains the complete typed definition.");
                using var selectCopy = (VisualShaderNodeSwitch)select.Duplicate(true); using var selectAssigned = new VisualShaderNodeSwitch(); selectAssigned.CopyFromResource(select);
                Check(selectCopy.OpType == select.OpType && selectAssigned.OpType == select.OpType && selectCopy.GetInputPortDefaultValue<bool>(0) == true, "Switch duplicate/copy retains its condition and layout.");
                compare.ClearDefaultInputValues(); select.ClearDefaultInputValues();
                compare.Type = (VisualShaderValueType)((to + 1) % 7); select.OpType = (VisualShaderValueType)((to + 1) % 7);
                Check(compare.GetDefaultInputValues().Count == 2 && compare.GetInputPortDefaultValue<float>(2) is null && select.GetDefaultInputValues().Count == 2 && select.GetInputPortDefaultValue<bool>(0) is null, "Layout changes recreate only operand defaults and preserve unrelated absence.");
            }
        using var overflow = new VisualShaderNodeCompare { Type = VisualShaderValueType.UInt };
        overflow.SetInputPortDefaultValue(0, 123u); overflow.SetInputPortDefaultValue(1, uint.MaxValue); var snapshot = overflow.GetDefaultInputValues();
        Reject<OverflowException>(() => overflow.Type = VisualShaderValueType.Int);
        Check(overflow.Type == VisualShaderValueType.UInt && snapshot.SequenceEqual(overflow.GetDefaultInputValues()), "A later compare conversion failure cannot commit an earlier operand.");
        using var negative = new VisualShaderNodeSwitch(); negative.SetInputPortDefaultValue(0, true); negative.SetInputPortDefaultValue(2, -1f); snapshot = negative.GetDefaultInputValues();
        Reject<OverflowException>(() => negative.OpType = VisualShaderValueType.UInt);
        Check(negative.OpType == VisualShaderValueType.Float && snapshot.SequenceEqual(negative.GetDefaultInputValues()), "A later switch overflow preserves the condition and both operands.");
        using var dormant = new VisualShaderNodeCompare(); dormant.SetInputPortDefaultValue(2, -.125f); var changesCount = 0; dormant.Changed += _ => changesCount++;
        foreach (var function in Enumerable.Range(0, 6))
        {
            dormant.Function = (VisualShaderNodeCompare.FunctionEnum)function; var count = changesCount; dormant.Function = (VisualShaderNodeCompare.FunctionEnum)function;
            Check(changesCount == count && dormant.GetInputPortDefaultValue<float>(2) == -.125f, "Function changes retain the inactive tolerance; repeated selection is quiet.");
        }
        foreach (var condition in Enumerable.Range(0, 2)) dormant.Condition = (VisualShaderNodeCompare.ConditionEnum)condition;
        Reject<ArgumentOutOfRangeException>(() => dormant.Condition = VisualShaderNodeCompare.ConditionEnum.Max);
        Reject<ArgumentOutOfRangeException>(() => dormant.Function = VisualShaderNodeCompare.FunctionEnum.Max);
        using var graph = new VisualShader(); using var scalar = new VisualShaderNodeFloatConstant();
        graph.AddNode(VisualShader.Type.Fragment, dormant, default, 2); graph.AddNode(VisualShader.Type.Fragment, scalar, default, 3);
        Check(!graph.CanConnectNodes(VisualShader.Type.Fragment, 3, 0, 2, 2), "Non-equality comparisons hide the tolerance edge.");
        dormant.Function = VisualShaderNodeCompare.FunctionEnum.Equal;
        Check(graph.CanConnectNodes(VisualShader.Type.Fragment, 3, 0, 2, 2), "Floating equality exposes its tolerance edge.");
        dormant.Changed += _ => throw new InvalidOperationException("Compare observer");
        Reject<InvalidOperationException>(() => dormant.Condition = VisualShaderNodeCompare.ConditionEnum.All);
        Check(dormant.Condition == VisualShaderNodeCompare.ConditionEnum.All, "Compare state commits before a failing observer.");
        using var selectObserver = new VisualShaderNodeSwitch(); selectObserver.Changed += _ => throw new InvalidOperationException("Switch observer");
        Reject<InvalidOperationException>(() => selectObserver.OpType = VisualShaderValueType.Boolean);
        Check(selectObserver.OpType == VisualShaderValueType.Boolean && selectObserver.GetInputPortDefaultValue<bool>(1) == true, "Switch defaults commit before a failing observer.");
        using var classify = new VisualShaderNodeIs(); var isChanges = 0; classify.Changed += _ => isChanges++;
        classify.Function = VisualShaderNodeIs.FunctionEnum.IsNaN; classify.Function = VisualShaderNodeIs.FunctionEnum.IsNaN;
        Check(isChanges == 1, "Classification selection suppresses repeated changes.");
        Reject<ArgumentOutOfRangeException>(() => classify.Function = VisualShaderNodeIs.FunctionEnum.Max);
        using var isCopy = (VisualShaderNodeIs)classify.Duplicate(); Check(isCopy.Function == classify.Function, "Classification selector duplicates.");
        using var initialIf = new VisualShaderNodeIf(); Check(initialIf.GetDefaultInputValues().Count == 6 && initialIf.GetInputPortDefaultValue<float>(2) == .00001f, "If initializes all six typed defaults.");
        using var ifCopy = (VisualShaderNodeIf)initialIf.Duplicate(true); Check(ifCopy.GetInputPortDefaultValue<Vector3>(5) == Vector3.Zero, "If duplicates every result input.");
        Reject<ArgumentException>(() => initialIf.SetInputPortDefaultValue(0, float.NaN));
        dormant.Dispose(); selectObserver.Dispose(); classify.Dispose(); initialIf.Dispose();
        Reject<ObjectDisposedException>(() => dormant.Function = VisualShaderNodeCompare.FunctionEnum.Equal);
        Reject<ObjectDisposedException>(() => selectObserver.OpType = VisualShaderValueType.Float);
        Reject<ObjectDisposedException>(() => classify.Function = VisualShaderNodeIs.FunctionEnum.IsInf);
        Reject<ObjectDisposedException>(() => initialIf.ClearDefaultInputValues());
    }
    private static object[] CompareInputs(int type, int variant)
    {
        if (type == 0) return variant switch { 0 => [.25f, .3125f, .125f], 1 => [.25f, .375f, .125f], 2 => [.75f, .25f, 0f], 3 => [0f, 0f, 0f], _ => [0f, 0f, -.125f] };
        if (type == 1) return variant switch { 0 => [-7, 7, .125f], 1 => [16777217, 16777216, .125f], 2 => [int.MinValue, int.MinValue, .125f], 3 => [int.MaxValue, int.MinValue, .125f], _ => [int.MinValue, int.MaxValue, .125f] };
        if (type == 2) return variant switch { 0 => [0u, 1u, .125f], 1 => [uint.MaxValue, uint.MaxValue - 1, .125f], 2 => [uint.MaxValue, uint.MaxValue, .125f], 3 => [0u, uint.MaxValue, .125f], _ => [uint.MaxValue, 0u, .125f] };
        if (type == 6) return variant switch { 0 => [false, false, .125f], 1 => [true, false, .125f], 3 => [false, true, .125f], _ => [true, true, .125f] };
        var a = new Vector4(.25f, .5f, .75f, .125f); var b = variant == 0 ? a : variant == 1 ? new Vector4(.5f, .25f, .75f, .25f) : a;
        if (variant == 2) { if (type == 3) b.Y = .875f; else if (type == 4) b.Z = .875f; else b.W = .875f; }
        if (variant is 3 or 4) { a = variant == 3 ? new(.125f, .25f, .375f, .5f) : new(.5f, .625f, .75f, .875f); b = variant == 3 ? new(.5f, .625f, .75f, .875f) : new(.125f, .25f, .375f, .5f); }
        return type == 3 ? [new Vector2(a.X, a.Y), new Vector2(b.X, b.Y), .125f] : type == 4 ? [new Vector3(a.X, a.Y, a.Z), new Vector3(b.X, b.Y, b.Z), .125f] : [a, b, .125f];
    }
    private static double[] CompareComponents(object value) => value switch
    {
        float v => [v],
        int v => [v],
        uint v => [v],
        bool v => [v ? 1 : 0],
        Vector2 v => [v.X, v.Y],
        Vector3 v => [v.X, v.Y, v.Z],
        Vector4 v => [v.X, v.Y, v.Z, v.W],
        _ => throw new ArgumentException()
    };
    private static bool CompareResult(int type, int function, int condition, int variant)
    {
        var input = CompareInputs(type, variant); var a = CompareComponents(input[0]); var b = CompareComponents(input[1]);
        if (type == 0 && function < 2) { var equal = Math.Abs(a[0] - b[0]) < (float)input[2]; return function == 0 ? equal : !equal; }
        if (type == 6 && function > 1) return false;
        var results = a.Select((value, index) => function switch { 0 => value == b[index], 1 => value != b[index], 2 => value > b[index], 3 => value >= b[index], 4 => value < b[index], _ => value <= b[index] });
        return condition == 0 ? results.All(x => x) : results.Any(x => x);
    }
    private static readonly (float A, float B, float T)[] IfInputs = [(0, 0, .00001f), (1, 0, .00001f), (0, 1, .00001f), (.25f, .375f, .125f), (.25f, .3125f, .125f), (0, 0, 0), (0, 0, -.125f)];
    private static void AuthorConditions(string folder)
    {
        for (var type = 0; type < 7; type++)
        {
            for (var function = 0; function < 6; function++)
                for (var condition = 0; condition < 2; condition++)
                {
                    for (var variant = 0; variant < 5; variant++) AuthorCompare(folder, type, function, condition, variant, false);
                    AuthorCompare(folder, type, function, condition, 1, true);
                }
            for (var truth = 0; truth < 2; truth++)
            {
                AuthorSwitch(folder, type, truth != 0, false, false);
                AuthorSwitch(folder, type, truth != 0, false, true);
                if (type is 1 or 2 or 5) AuthorSwitch(folder, type, truth != 0, true, false);
            }
        }
        for (var index = 0; index < IfInputs.Length; index++) foreach (var vertex in new[] { false, true })
            {
                using var graph = new VisualShader(); using var node = new VisualShaderNodeIf(); var input = IfInputs[index];
                node.SetInputPortDefaultValue(0, input.A); node.SetInputPortDefaultValue(1, input.B); node.SetInputPortDefaultValue(2, input.T);
                node.SetInputPortDefaultValue(3, new Vector3(1, 0, 0)); node.SetInputPortDefaultValue(4, new Vector3(0, 1, 0)); node.SetInputPortDefaultValue(5, new Vector3(0, 0, 1));
                var stage = vertex ? VisualShader.Type.Vertex : VisualShader.Type.Fragment; graph.AddNode(stage, node, default, 2); graph.ConnectNodes(stage, 2, 0, 0, vertex ? 2 : 0);
                SaveCondition(graph, folder, "if-" + index + (vertex ? "-vertex" : ""));
            }
        for (var function = 0; function < 2; function++)
            for (var source = 0; source < 5; source++) foreach (var vertex in new[] { false, true }) AuthorClassification(folder, function, source, vertex);
        foreach (var name in new[] { "compare-init", "compare-empty", "switch-init", "switch-empty", "if-init", "if-empty", "is-init", "is-empty" })
        {
            using var graph = new VisualShader(); using VisualShaderNode node = name.Split('-')[0] switch { "compare" => new VisualShaderNodeCompare(), "switch" => new VisualShaderNodeSwitch(), "if" => new VisualShaderNodeIf(), _ => new VisualShaderNodeIs() };
            if (name.EndsWith("empty", StringComparison.Ordinal)) node.ClearDefaultInputValues();
            graph.AddNode(VisualShader.Type.Fragment, node, default, 2); graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 0); SaveCondition(graph, folder, name);
        }
        AuthorConditionProcedure(folder);
        var invalid = IOPath.Combine(folder, "invalid-conditions"); Directory.CreateDirectory(invalid);
        using var stale = new VisualShader(); using var comparison = new VisualShaderNodeCompare(); using var tolerance = new VisualShaderNodeFloatConstant();
        stale.AddNode(VisualShader.Type.Fragment, comparison, default, 2); stale.AddNode(VisualShader.Type.Fragment, tolerance, default, 3);
        stale.ConnectNodes(VisualShader.Type.Fragment, 3, 0, 2, 2); stale.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 0);
        comparison.Function = VisualShaderNodeCompare.FunctionEnum.GreaterThan; SaveCondition(stale, invalid, "hidden-tolerance");
    }
    private static void AuthorCompare(string folder, int type, int function, int condition, int variant, bool vertex)
    {
        using var graph = new VisualShader(); using var node = new VisualShaderNodeCompare { Type = (VisualShaderValueType)type, Function = (VisualShaderNodeCompare.FunctionEnum)function, Condition = (VisualShaderNodeCompare.ConditionEnum)condition };
        var input = CompareInputs(type, variant); for (var port = 0; port < 3; port++) SetConditionDefault(node, port, input[port]);
        var stage = vertex ? VisualShader.Type.Vertex : VisualShader.Type.Fragment; graph.AddNode(stage, node, default, 2); graph.ConnectNodes(stage, 2, 0, 0, vertex ? 2 : 0);
        SaveCondition(graph, folder, $"c-{type}-{function}-{condition}-{variant}" + (vertex ? "-vertex" : ""));
    }
    private static void AuthorSwitch(string folder, int type, bool truth, bool extra, bool vertex)
    {
        using var graph = new VisualShader(); using var node = new VisualShaderNodeSwitch { OpType = (VisualShaderValueType)type }; var owned = new List<VisualShaderNode>();
        try
        {
            node.SetInputPortDefaultValue(0, truth); SetConditionDefault(node, 1, SelectionValue(type, true)); SetConditionDefault(node, 2, SelectionValue(type, false));
            var stage = vertex ? VisualShader.Type.Vertex : VisualShader.Type.Fragment; graph.AddNode(stage, node, default, 2);
            if (type is 1 or 2) EncodeInteger(graph, stage, 2, extra, owned);
            else if (type == 5 && extra)
            {
                var split = new VisualShaderNodeVectorDecompose { OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector4 }; owned.Add(split); graph.AddNode(stage, split, default, 3);
                graph.ConnectNodes(stage, 2, 0, 3, 0); graph.ConnectNodes(stage, 3, 3, 0, vertex ? 2 : 0);
            }
            else graph.ConnectNodes(stage, 2, 0, 0, vertex ? 2 : 0);
            SaveCondition(graph, folder, $"s-{type}-{(truth ? 1 : 0)}-{(extra ? 1 : 0)}" + (vertex ? "-vertex" : ""));
        }
        finally { foreach (var item in owned) item.Dispose(); }
    }
    private static void AuthorClassification(string folder, int function, int source, bool vertex)
    {
        using var graph = new VisualShader(); using var node = new VisualShaderNodeIs { Function = (VisualShaderNodeIs.FunctionEnum)function }; var stage = vertex ? VisualShader.Type.Vertex : VisualShader.Type.Fragment;
        using var producer = new VisualShaderNodeFloatOp { Operator = source == 1 ? VisualShaderNodeFloatOp.OperatorEnum.Multiply : VisualShaderNodeFloatOp.OperatorEnum.Divide };
        if (source == 1) { producer.SetInputPortDefaultValue(0, float.MaxValue); producer.SetInputPortDefaultValue(1, 2f); }
        else { producer.SetInputPortDefaultValue(0, source == 2 ? 1f : source == 3 ? -1f : 0f); producer.SetInputPortDefaultValue(1, source == 0 ? 1f : 0f); }
        graph.AddNode(stage, node, default, 2); graph.AddNode(stage, producer, default, 3); graph.ConnectNodes(stage, 3, 0, 2, 0); graph.ConnectNodes(stage, 2, 0, 0, vertex ? 2 : 0);
        SaveCondition(graph, folder, $"is-{function}-{source}" + (vertex ? "-vertex" : ""));
    }
    private static void AuthorConditionProcedure(string folder)
    {
        using var graph = new VisualShader(); using var uv = new VisualShaderNodeInput { InputName = "uv" }; using var split = new VisualShaderNodeVectorDecompose { OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector2 };
        using var compare = new VisualShaderNodeCompare { Function = VisualShaderNodeCompare.FunctionEnum.GreaterThan }; compare.SetInputPortDefaultValue(1, .5f);
        using var select = new VisualShaderNodeSwitch { OpType = VisualShaderValueType.Vector3 }; select.SetInputPortDefaultValue(1, new Vector3(.25f, .5f, .75f)); select.SetInputPortDefaultValue(2, Vector3.Zero);
        graph.AddNode(VisualShader.Type.Fragment, uv, default, 2); graph.AddNode(VisualShader.Type.Fragment, split, default, 3); graph.AddNode(VisualShader.Type.Fragment, compare, default, 4); graph.AddNode(VisualShader.Type.Fragment, select, default, 5);
        for (var id = 2; id < 5; id++) graph.ConnectNodes(VisualShader.Type.Fragment, id, 0, id + 1, 0);
        graph.ConnectNodes(VisualShader.Type.Fragment, 5, 0, 0, 0); SaveCondition(graph, folder, "procedural");
    }
    private static void SaveCondition(VisualShader graph, string folder, string name) => ResourceSaver.Save(graph, IOPath.Combine(folder, "condition-" + name + ".e2dres"), SaverFlags.BundleResources | SaverFlags.Compress | SaverFlags.SaveBigEndian);
    private static Color ConditionExpected(string name)
    {
        var parts = name.Split('-'); if (parts[1] == "procedural") return new(.25f, .5f, .75f, 1);
        if (parts[1] == "c") return CompareResult(int.Parse(parts[2]), int.Parse(parts[3]), int.Parse(parts[4]), int.Parse(parts[5])) ? Colors.White : Colors.Black;
        if (parts[1] == "s")
        {
            var type = int.Parse(parts[2]); var value = SelectionValue(type, parts[3] == "1");
            if (type is 1 or 2)
            {
                var bits = value is int signed ? unchecked((uint)signed) : (uint)value;
                if (parts[4] == "1") { var high = (bits >> 24) / 255f; return new(high, high, high, 1); }
                return new((bits & 255) / 255f, ((bits >> 8) & 255) / 255f, ((bits >> 16) & 255) / 255f, 1);
            }
            var c = CompareComponents(value);
            if (type == 5 && parts[4] == "1") return new((float)c[3], (float)c[3], (float)c[3], 1);
            return c.Length < 3 ? new((float)c[0], c.Length == 2 ? (float)c[1] : (float)c[0], c.Length == 2 ? 0 : (float)c[0], 1) : new((float)c[0], (float)c[1], (float)c[2], 1);
        }
        if (parts[1] == "if" && int.TryParse(parts[2], out var index)) { var v = IfInputs[index]; return MathF.Abs(v.A - v.B) < v.T ? Colors.Red : v.A < v.B ? Colors.Blue : Colors.Green; }
        if (parts[1] == "is" && int.TryParse(parts[2], out var function)) { var source = int.Parse(parts[3]); return (function == 0 ? source is 1 or 2 or 3 : source == 4) ? Colors.White : Colors.Black; }
        return parts[1] == "compare" && parts[2] == "init" ? Colors.White : Colors.Black;
    }
    private static void VerifyConditionArchive(string name, VisualShader graph)
    {
        var parts = name.Split('-'); var stage = parts.Contains("vertex") ? VisualShader.Type.Vertex : VisualShader.Type.Fragment;
        if (parts[1] == "c")
        {
            var type = int.Parse(parts[2]); var node = (VisualShaderNodeCompare)graph.GetNode(stage, 2); var input = CompareInputs(type, int.Parse(parts[5]));
            Check((int)node.Type == type && (int)node.Function == int.Parse(parts[3]) && (int)node.Condition == int.Parse(parts[4]), "Fresh compare archives retain all selectors.");
            Check(Equals(ConditionDefault(node, type, 0), input[0]) && Equals(ConditionDefault(node, type, 1), input[1]) && node.GetInputPortDefaultValue<float>(2) == (float)input[2], "Fresh compare archives retain exact typed operands and dormant tolerance.");
        }
        else if (parts[1] == "s")
        {
            var type = int.Parse(parts[2]); var node = (VisualShaderNodeSwitch)graph.GetNode(stage, 2);
            Check((int)node.OpType == type && node.GetInputPortDefaultValue<bool>(0) == (parts[3] == "1") && Equals(ConditionDefault(node, type, 1), SelectionValue(type, true)) && Equals(ConditionDefault(node, type, 2), SelectionValue(type, false)), "Fresh switch archives retain exact typed conditions and all operand bits.");
        }
        else if (parts[1] == "is" && int.TryParse(parts[2], out var function)) Check((int)((VisualShaderNodeIs)graph.GetNode(stage, 2)).Function == function, "Fresh classification archives retain the selector.");
        else if (parts.Length == 3 && parts[2] == "empty") Check(graph.GetNode(stage, 2).GetDefaultInputValues().Count == 0, "Fresh condition archives preserve complete default absence.");
    }
}
