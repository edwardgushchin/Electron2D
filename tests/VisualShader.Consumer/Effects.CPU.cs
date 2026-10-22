using Electron2D;
using CF = Electron2D.VisualShaderNodeColorFunc;
using CO = Electron2D.VisualShaderNodeColorOp;
using UF = Electron2D.VisualShaderNodeUVFunc;

internal static partial class GraphChecks
{
    private static void EffectsCPU()
    {
        using var uv = new UF(); using var polar = new VisualShaderNodeUVPolarCoord(); using var color = new CF(); using var blend = new CO();
        Check(uv.Function == UF.FunctionEnum.Panning && uv.GetInputPortDefaultValue<Vector2>(0) is null && uv.GetInputPortDefaultValue<Vector2>(1) == Vector2.One && uv.GetInputPortDefaultValue<Vector2>(2) == Vector2.Zero, "UV initial defaults and implicit input.");
        Check(polar.GetInputPortDefaultValue<Vector2>(0) is null && polar.GetInputPortDefaultValue<Vector2>(1) == new Vector2(.5f, .5f) && polar.GetInputPortDefaultValue<float>(2) == 1 && polar.GetInputPortDefaultValue<float>(3) == 1, "Polar initial defaults.");
        var changed = 0; uv.Changed += _ => changed++;
        uv.Function = UF.FunctionEnum.Panning; Check(changed == 0, "Unchanged UV operation is quiet.");
        uv.Function = UF.FunctionEnum.Scaling;
        Check(changed == 1 && uv.GetInputPortDefaultValue<Vector2>(2) == Vector2.Zero, "Changing to Scaling preserves the existing zero offset as pivot.");
        uv.SetInputPortDefaultValue(2, new Vector2(-.25f, .75f)); uv.Function = UF.FunctionEnum.Panning; uv.Function = UF.FunctionEnum.Scaling;
        Check(uv.GetInputPortDefaultValue<Vector2>(2) == new Vector2(-.25f, .75f), "Custom offset/pivot survives both operations.");
        uv.RemoveInputPortDefaultValue(2); uv.Function = UF.FunctionEnum.Scaling;
        Check(uv.GetInputPortDefaultValue<Vector2>(2) is null, "An unchanged selection preserves absence.");
        uv.Function = UF.FunctionEnum.Panning; Check(uv.GetInputPortDefaultValue<Vector2>(2) == Vector2.Zero, "Panning recreates an absent offset as zero.");
        uv.RemoveInputPortDefaultValue(2); uv.Function = UF.FunctionEnum.Scaling;
        Check(uv.GetInputPortDefaultValue<Vector2>(2) == new Vector2(.5f, .5f), "Scaling recreates an absent pivot at the center.");
        uv.SetInputPortDefaultValue(0, new Vector2(8, -3)); uv.RemoveInputPortDefaultValue(1);
        using (var copied = (UF)uv.Duplicate())
        {
            Check(copied.Function == uv.Function && copied.GetInputPortDefaultValue<Vector2>(0) == new Vector2(8, -3) && copied.GetInputPortDefaultValue<Vector2>(1) is null, "UV definition copy preserves dormant UV and absent scale.");
            copied.SetInputPortDefaultValue(0, Vector2.Zero); Check(uv.GetInputPortDefaultValue<Vector2>(0) == new Vector2(8, -3), "Copied defaults are independent.");
        }
        var input = new Vector3(-.25f, .5f, 1.5f); color.SetInputPortDefaultValue(0, input);
        foreach (var from in Enum.GetValues<CF.FunctionEnum>().Where(v => v != CF.FunctionEnum.Max))
            foreach (var to in Enum.GetValues<CF.FunctionEnum>().Where(v => v != CF.FunctionEnum.Max))
            { color.Function = from; color.Function = to; Check(color.GetInputPortDefaultValue<Vector3>(0) == input, "Color function changes preserve signed/HDR defaults."); }
        foreach (var from in Enum.GetValues<CO.OperatorEnum>().Where(v => v != CO.OperatorEnum.Max))
            foreach (var to in Enum.GetValues<CO.OperatorEnum>().Where(v => v != CO.OperatorEnum.Max))
            { blend.Operator = from; blend.SetInputPortDefaultValue(0, input); blend.RemoveInputPortDefaultValue(1); blend.Operator = to; Check(blend.GetInputPortDefaultValue<Vector3>(0) == input && blend.GetInputPortDefaultValue<Vector3>(1) is null, "Color operator changes retain values and absence."); }
        foreach (var node in new VisualShaderNode[] { uv, polar, color, blend })
        {
            Reject<ArgumentException>(() => node.SetInputPortDefaultValue(0, new Vector2(float.NaN, 0)));
            using var copy = (VisualShaderNode)node.Duplicate(true); using var target = (VisualShaderNode)node.Duplicate(); target.CopyFromResource(node);
            Check(copy.GetDefaultInputValues().Count == node.GetDefaultInputValues().Count && target.GetDefaultInputValues().Count == node.GetDefaultInputValues().Count, "Node duplicates and CopyFromResource retain complete defaults.");
        }
        Reject<ArgumentOutOfRangeException>(() => uv.Function = UF.FunctionEnum.Max); Reject<ArgumentOutOfRangeException>(() => uv.Function = (UF.FunctionEnum)(-1));
        Reject<ArgumentOutOfRangeException>(() => color.Function = CF.FunctionEnum.Max); Reject<ArgumentOutOfRangeException>(() => color.Function = (CF.FunctionEnum)99);
        Reject<ArgumentOutOfRangeException>(() => blend.Operator = CO.OperatorEnum.Max); Reject<ArgumentOutOfRangeException>(() => blend.Operator = (CO.OperatorEnum)99);
        Reject<ArgumentException>(() => polar.SetInputPortDefaultValue(2, float.PositiveInfinity)); Reject<ArgumentOutOfRangeException>(() => polar.SetInputPortDefaultValue(4, 1f));
        using var graph = new VisualShader(); using var initial = new VisualShaderNodeFloatConstant();
        graph.AddNode(VisualShader.Type.Fragment, initial, default, 2);
        foreach (var name in new[] { nameof(VisualShaderNodeUVFunc), nameof(VisualShaderNodeUVPolarCoord), nameof(VisualShaderNodeColorFunc), nameof(VisualShaderNodeColorOp) })
        {
            graph.ReplaceNode(VisualShader.Type.Fragment, 2, name);
            Check(graph.GetNode(VisualShader.Type.Fragment, 2).GetType().Name == name, "All image-effect definitions use registered factories.");
        }
        uv.RemoveInputPortDefaultValue(2);
        uv.Changed += _ => throw new InvalidOperationException("UV observer");
        Reject<InvalidOperationException>(() => uv.Function = UF.FunctionEnum.Panning);
        Check(uv.Function == UF.FunctionEnum.Panning && uv.GetInputPortDefaultValue<Vector2>(2) == Vector2.Zero, "Function and recreated default commit together before observer failure.");
        using var disposed = new CF(); disposed.Dispose(); Reject<ObjectDisposedException>(() => disposed.Function = CF.FunctionEnum.Sepia);
        Console.WriteLine("UV and color graph defaults, complete selector transitions, aliases, factories, errors and copies passed.");
    }
}
