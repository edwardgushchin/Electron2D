using Electron2D;
using E = Electron2D.VisualShaderNodeExpression;
using P = Electron2D.VisualShaderNode.PortType;

internal static partial class GraphChecks
{
    private static void ExpressionsCPU()
    {
        using var node = new E(); var changed = 0; var schema = 0;
        node.Changed += _ => changed++; node.PropertyListChanged += _ => schema++;
        Check(node.Size == Vector2.Zero && node.Expression == "" && node.GetInputPortCount() == 0 && node.GetOutputPortCount() == 0, "Expression initial state.");
        node.Size = new(-10, 40); Check(changed == 0 && node.Size == new Vector2(-10, 40), "Layout size retains metadata without shader change events.");
        node.Expression = ""; node.Expression = ""; Check(changed == 2, "Equal source assignments still publish Changed.");
        Reject<ArgumentNullException>(() => node.Expression = null!); Reject<ArgumentException>(() => node.Expression = "\ud800");
        Reject<ArgumentException>(() => node.Expression = new string('a', 8 * 1024 * 1024 + 1)); Reject<ArgumentException>(() => node.Size = new(float.PositiveInfinity, 0));
        node.AddInputPort(0, P.Vector2, "uv"); node.SetInputPortDefaultValue(0, new Vector2(2, 3)); node.AddInputPort(0, P.Scalar, "gain");
        Check(node.GetInputs() == "0,0,gain;1,3,uv;" && node.GetInputPortDefaultValue<Vector2>(1) == new Vector2(2, 3) && node.GetInputPortDefaultValue<float>(0) is null, "Insert shifts typed defaults and creates absence.");
        node.AddInputPort(int.MaxValue, P.Boolean, "flag"); Check(node.GetFreeInputPortID() == 3 && node.HasInputPort(2) && !node.HasInputPort(-1) && !node.HasOutputPort(0), "Out-of-range nonnegative IDs append and free IDs follow count.");
        node.AddOutputPort(0, P.Vector3, "result"); Check(node.GetOutputs() == "0,4,result;" && node.GetFreeOutputPortID() == 1, "Output list and count.");
        foreach (var bad in new[] { "", "1a", "has space", "has,comma", "a;b", new string('a', 129), "uv", "result" }) Check(!node.IsValidPortName(bad), "Port name rejection.");
        Check(node.IsValidPortName("_unused0"), "ASCII underscores and digits."); Reject<ArgumentNullException>(() => node.IsValidPortName(null!));
        var oldInputs = node.GetInputs(); var oldDefaults = node.GetDefaultInputValues();
        foreach (var bad in new[] { "1,0,x;", "0,9,x;", "0,-1,x;", "0,0,result;", "0,0,x;1,0,x;", "0,0,;", "0,0,x,extra;" }) Reject<ArgumentException>(() => node.SetInputs(bad));
        Check(node.GetInputs() == oldInputs && node.GetDefaultInputValues().SequenceEqual(oldDefaults), "Invalid bulk replacement is atomic.");
        Reject<ArgumentException>(() => node.SetOutputPortName(0, "uv")); Reject<ArgumentOutOfRangeException>(() => node.SetInputPortType(0, P.Max)); Reject<ArgumentOutOfRangeException>(() => node.AddOutputPort(-1, P.Scalar, "x"));
        var before = changed; node.SetInputPortName(0, "gain"); node.SetInputPortType(0, P.Scalar); node.SetInputs(node.GetInputs()); Check(changed == before, "Unchanged layout edits are quiet.");
        node.SetInputPortName(0, "amount"); node.SetOutputPortName(0, "color"); node.SetInputPortType(1, P.Vector4);
        Check(node.GetInputPortDefaultValue<Vector4>(1) == new Vector4(2, 3, 3, 3), "Type expansion preserves previous components.");
        node.RemoveInputPort(0); Check(node.GetInputPortDefaultValue<Vector4>(0) == new Vector4(2, 3, 3, 3) && node.GetInputs().StartsWith("0,5,uv;", StringComparison.Ordinal), "Removal renumbers ports and defaults.");
        node.SetInputPortType(0, P.Sampler); Check(node.GetDefaultInputValues().Count == 0, "Sampler conversion drops numeric defaults."); node.SetInputPortType(0, P.Scalar); Check(node.GetInputPortDefaultValue<float>(0) is null, "Sampler absence stays absent after returning to numeric data.");
        node.SetInputPortDefaultValue(0, float.MaxValue); oldInputs = node.GetInputs(); Reject<OverflowException>(() => node.SetInputPortType(0, P.ScalarInt)); Check(node.GetInputs() == oldInputs && node.GetInputPortDefaultValue<float>(0) == float.MaxValue, "Overflow preserves type and default.");
        node.ClearInputPorts(); node.ClearOutputPorts(); Check(node.GetInputs() == "" && node.GetOutputs() == "" && node.GetDefaultInputValues().Count == 0, "Clear resets complete descriptions and defaults.");
        node.SetInputs("0,0,amount;1,7,matrix"); node.SetOutputs("0,4,color"); node.SetInputPortDefaultValue(0, .25f); node.Expression = "color=vec3(amount);";
        using var copy = (E)node.Duplicate(true); using var target = new E(); target.AddInputPort(0, P.Scalar, "different"); target.CopyFromResource(node);
        Check(copy.GetInputs() == node.GetInputs() && target.GetOutputs() == node.GetOutputs() && target.Size == node.Size && target.Expression == node.Expression, "Definition copies include layout, source, port schema and defaults.");
        copy.RemoveInputPort(0); Check(node.GetInputPortCount() == 2 && node.GetInputPortDefaultValue<float>(0) == .25f, "Copied schema/defaults remain independent.");
        node.RemoveOutputPort(0); node.AddOutputPort(0, P.Scalar, "value"); node.SetOutputPortType(0, P.Boolean); Check(node.GetOutputs() == "0,6,value;", "Output removal and typed replacement.");
        foreach (var type in Enum.GetValues<P>().Where(t => t != P.Max))
        {
            using var group = new E(); group.AddInputPort(0, type, "inputValue"); group.AddOutputPort(0, type, "outputValue");
            Check(group.GetInputs() == $"0,{(int)type},inputValue;" && group.GetOutputs() == $"0,{(int)type},outputValue;", "Complete port role descriptions.");
        }
        Check(schema > 0, "Port edits publish stored-property schema changes.");
        using var failed = new E(); var notifications = 0; failed.Changed += _ => throw new InvalidOperationException("port observer"); failed.PropertyListChanged += _ => notifications++;
        Reject<InvalidOperationException>(() => failed.AddInputPort(0, P.Scalar, "value")); Check(failed.GetInputPortCount() == 1 && notifications == 1, "Port/default commit and schema publication survive Changed observer failure.");
        using var graph = new VisualShader(); using var initial = new VisualShaderNodeFloatConstant(); graph.AddNode(VisualShader.Type.Fragment, initial, default, 2);
        foreach (var name in new[] { nameof(VisualShaderNodeExpression), nameof(VisualShaderNodeGlobalExpression) })
        { var registered = name; graph.ReplaceNode(VisualShader.Type.Fragment, 2, registered); Check(graph.GetNode(VisualShader.Type.Fragment, 2).GetType().Name == registered, "Expression registered factories."); }
        target.Dispose(); Reject<ObjectDisposedException>(() => target.GetInputs()); Reject<ObjectDisposedException>(() => target.AddInputPort(0, P.Scalar, "x")); Reject<ObjectDisposedException>(() => target.Expression = "");
        Console.WriteLine("Dynamic expression ports: all roles, insertion/removal, canonical schema, defaults/conversion/overflow, copies, notifications, source/size limits and factories passed.");
    }
}
