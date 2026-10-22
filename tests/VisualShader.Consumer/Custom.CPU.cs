using Electron2D;
using P = Electron2D.VisualShaderNode.PortType;
using V = Electron2D.VisualShader;
using IOPath = System.IO.Path;

internal static partial class GraphChecks
{
    internal static void CustomCPU()
    {
        using (var empty = new EmptyCustom())
        {
            Check(empty.GetName() == "Unnamed" && empty.GetCategory() == "Addons" && empty.GetDescription() == "" && empty.GetReturnIconType() is null && !empty.IsHighEnd(), "Default compiled authoring metadata.");
            Check(empty.GetInputPortCount() == 0 && empty.GetOutputPortCount() == 0 && empty.GetPropertyCount() == 0 && empty.GetOptionIndex(-1) == 0 && empty.GetOptionIndex(int.MaxValue) == 0, "Empty schema and unknown selections.");
            Check(empty.GetDefaultInputPort(P.Matrix) == 0 && empty.GetFuncCode(Shader.Mode.CanvasItem, V.Type.Vertex) == "" && empty.GetGlobalCode(Shader.Mode.CanvasItem) == "" && empty.IsAvailable(Shader.Mode.CanvasItem, V.Type.Fragment), "Default source/context callbacks.");
            Reject<ArgumentOutOfRangeException>(() => empty.GetInputPortName(0)); Reject<ArgumentOutOfRangeException>(() => empty.GetOutputPortType(0));
            Reject<ArgumentOutOfRangeException>(() => empty.GetPropertyOptions(0)); Reject<ArgumentOutOfRangeException>(() => empty.GetDefaultInputPort(P.Max));
            Reject<ArgumentOutOfRangeException>(() => empty.GetFuncCode((Shader.Mode)999, V.Type.Vertex)); Reject<ArgumentOutOfRangeException>(() => empty.IsAvailable(Shader.Mode.CanvasItem, (V.Type)999));
        }
        foreach (var role in Enum.GetValues<P>().Where(p => p is not (P.Sampler or P.Max)))
        {
            using var node = new CustomNumericNode { Role = role, InputMode = 1 };
            Check(node.Constructed && node.GetDefaultInputValues().Count == 1 && node.GetInputPortName(0) == "in0" && node.GetOutputPortName(0) == "out0", "Lazy callback defaults and inherited port names.");
            using var copy = (CustomNumericNode)node.Duplicate(true); using var target = new CustomNumericNode(); target.CopyFromResource(node);
            Check(copy.Role == role && target.Role == role && Equals(CustomValue(copy), CustomValue(node)) && Equals(CustomValue(target), CustomValue(node)), "Compiled copy hooks retain schema and numeric defaults.");
            node.ClearDefaultInputValues(); Check(node.GetDefaultInputValues().Count == 0 && copy.GetDefaultInputValues().Count == 1, "Copied defaults remain independent.");
            node.RefreshDefinition(); Check(node.GetDefaultInputValues().Count == 1, "Refresh restores callback defaults.");
            if (role == P.Vector4) Check(node.GetDefaultInputValues()[0] is VisualShaderPortDefault<Vector4> && node.GetInputPortDefaultValue<Color>(0) == new Color(.125f, .375f, .625f, .875f), "Callback Color defaults use the same canonical vector representation.");
        }
        using var options = new CustomProcedureNode();
        using (var dynamicPorts = new CustomProcedureNode { Kind = 12 })
        {
            Check(dynamicPorts.GetInputPortCount() == 2 && dynamicPorts.GetInputPortDefaultValue<Color>(0) == new Color(.25f, .5f, .75f, 1), "Options initialize dependent port types and callback defaults.");
            dynamicPorts.SetInputPortDefaultValue(1, .875f); dynamicPorts.SetOptionIndex(0, 0);
            Check(dynamicPorts.GetInputPortCount() == 1 && dynamicPorts.GetInputPortDefaultValue<bool>(0) == true && dynamicPorts.GetDefaultInputValues().Count == 1, "Narrowing options replace numeric defaults for the complete active schema.");
            dynamicPorts.SetOptionIndex(0, 1); Check(dynamicPorts.GetInputPortDefaultValue<float>(1) == .5f, "Reactivation uses fresh callback defaults.");
        }
        Check(options.GetPropertyCount() == 2 && options.GetOptionIndex(0) == 1 && options.GetOptionIndex(1) == 0, "Nonzero initial option exposes its dependent property before schema discovery.");
        var changed = 0; var schema = 0; options.Changed += _ => changed++; options.PropertyListChanged += _ => schema++;
        options.SetOptionIndex(1, 2); var stale = options.GetPropertyList().Single(p => p.Name == "Option[1]:selection");
        options.SetOptionIndex(0, 0); Check(options.GetPropertyCount() == 1 && options.GetOptionIndex(1) == 2, "Inactive choices retain their values.");
        Reject<ArgumentOutOfRangeException>(() => stale.Revert(options)); options.SetOptionIndex(0, 1);
        Check(options.GetOptionIndex(1) == 2, "Reactivated choices recover retained selections."); stale.Revert(options); Check(options.GetOptionIndex(1) == 0, "Typed descriptor revert uses the current callback default.");
        var before = changed; options.SetOptionIndex(0, 1); Check(changed == before + 1 && schema == changed, "Equal options regenerate and publish both notification channels.");
        Reject<ArgumentOutOfRangeException>(() => options.SetOptionIndex(-1, 0)); Reject<ArgumentOutOfRangeException>(() => options.SetOptionIndex(0, 3));
        var stored = (PropertyDescriptor<VisualShaderNodeCustom, int[]>)options.GetPropertyList().Single(p => p.Name == "CustomOptions"); var snapshot = stored.GetValue(options);
        foreach (var bad in new[] { new[] { 0 }, new[] { -1, 0 }, new[] { 4096, 0 }, new[] { 0, 4096 }, new[] { 0, 1, 0, 2 } }) Reject<ArgumentException>(() => stored.SetValue(options, bad));
        Reject<InvalidOperationException>(() => stored.SetValue(options, [0, 3])); Check(snapshot.SequenceEqual(stored.GetValue(options)), "Malformed restored option tables preserve prior state.");
        stored.SetValue(options, [0, 0, 3, 2]); Check(options.GetOptionIndex(0) == 0 && options.GetOptionIndex(3) == 2, "Stored explicit zero and dormant selections survive restoration.");
        var folder = IOPath.Combine(IOPath.GetTempPath(), "electron2d-custom-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(folder); var file = IOPath.Combine(folder, "options.e2dres"); ResourceSaver.Save(options, file, SaverFlags.Compress | SaverFlags.SaveBigEndian);
            using var loaded = ResourceLoader.Load<CustomProcedureNode>(file, ResourceLoader.CacheMode.IgnoreDeep);
            Check(loaded.GetOptionIndex(0) == 0 && loaded.GetOptionIndex(3) == 2, "Fresh factory load preserves explicit zero instead of the nonzero callback default.");
            using var graph = new V(); using var numeric = new CustomNumericNode { Role = P.Vector4, InputMode = 1 };
            graph.AddNode(V.Type.Vertex, numeric, default, 2); graph.AddNode(V.Type.Fragment, numeric, default, 2);
            ResourceSaver.Save(graph, file, SaverFlags.BundleResources | SaverFlags.Compress | SaverFlags.SaveBigEndian);
            using var reloaded = ResourceLoader.Load<V>(file, ResourceLoader.CacheMode.IgnoreDeep);
            Check(ReferenceEquals(reloaded.GetNode(V.Type.Vertex, 2), reloaded.GetNode(V.Type.Fragment, 2)) && reloaded.GetNode(V.Type.Vertex, 2).GetInputPortDefaultValue<Color>(0) == new Color(.125f, .375f, .625f, .875f), "Fresh graph factories preserve aliases, schema order and defaults.");
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        using var fault = new FaultCustom(); fault.RefreshDefinition();
        var original = fault.GetDefaultInputValues();
        foreach (var bad in new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 })
        {
            fault.Fault = bad;
            try { fault.SetOptionIndex(0, 1); throw new Exception("Expected rejected callback " + bad); }
            catch (Exception error) when (error is InvalidOperationException or ArgumentException) { }
            fault.Fault = 0; Check(fault.GetOptionIndex(0) == 0 && fault.GetDefaultInputValues().SequenceEqual(original), "Failed callback leaves selection, cached ports and defaults unchanged.");
        }
        using var observer = new FaultCustom(); observer.RefreshDefinition(); var notified = 0;
        observer.Changed += _ => throw new InvalidOperationException("Changed observer"); observer.PropertyListChanged += _ => { notified++; throw new ArgumentException("Schema observer"); };
        Reject<AggregateException>(() => observer.SetOptionIndex(0, 1)); Check(observer.GetOptionIndex(0) == 1 && notified == 1, "Committed metadata publishes both channels despite observer failures.");
        options.Dispose(); Reject<ObjectDisposedException>(() => options.GetOptionIndex(0)); Reject<ObjectDisposedException>(() => options.SetOptionIndex(0, 0)); Reject<ObjectDisposedException>(() => options.RefreshDefinition());
        Console.WriteLine("Custom C# nodes: lazy discovery, all numeric defaults, metadata/options, dependent schema, retained/zero options, copies, archives/aliases, failed callbacks, budgets, reentry and observers passed.");
    }
    private sealed class EmptyCustom : VisualShaderNodeCustom
    {
        public override string GetCode(IReadOnlyList<string> inputVariables, IReadOnlyList<string> outputVariables, Shader.Mode mode, V.Type type) => "";
    }
    private static object? CustomValue(CustomNumericNode node) => node.Role switch
    {
        P.Scalar => node.GetInputPortDefaultValue<float>(0),
        P.ScalarInt => node.GetInputPortDefaultValue<int>(0),
        P.ScalarUInt => node.GetInputPortDefaultValue<uint>(0),
        P.Boolean => node.GetInputPortDefaultValue<bool>(0),
        P.Vector2 => node.GetInputPortDefaultValue<Vector2>(0),
        P.Vector3 => node.GetInputPortDefaultValue<Vector3>(0),
        P.Vector4 => node.GetInputPortDefaultValue<Vector4>(0),
        _ => node.GetInputPortDefaultValue<System.Numerics.Matrix4x4>(0)
    };
    private sealed class FaultCustom : VisualShaderNodeCustom
    {
        internal int Fault;
        public override int GetPropertyCount() => 1;
        public override string[] GetPropertyOptions(int index) => Fault == 1 ? new string[4097] : ["A", "B"];
        public override string GetName() => Fault == 2 ? null! : Fault == 3 ? "\ud800" : Fault == 4 ? new string('a', 8 * 1024 * 1024 + 1) : "Fault";
        public override int GetInputPortCount() => Fault == 5 ? 4097 : 1;
        public override P GetInputPortType(int port) => Fault == 6 ? P.Max : P.Scalar;
        public override VisualShaderPortDefault? GetInputPortDefaultValue(int port)
        {
            if (Fault == 7) return new VisualShaderPortDefault<float>(1, .25f);
            if (Fault == 8) return new VisualShaderPortDefault<int>(0, 2);
            if (Fault == 9) ClearDefaultInputValues();
            return new VisualShaderPortDefault<float>(0, .25f);
        }
        public override string GetCode(IReadOnlyList<string> inputVariables, IReadOnlyList<string> outputVariables, Shader.Mode mode, V.Type type) => "";
    }
}
