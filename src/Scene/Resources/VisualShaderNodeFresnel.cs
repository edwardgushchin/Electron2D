namespace Electron2D;

/// <summary>Raises a clamped normal/view dot-product falloff to a numeric power.</summary>
/// <remarks>Inputs are a three-component normal, three-component view, Boolean inversion and scalar power;
/// the output is scalar. Vectors are not normalized. Inversion selects the clamped dot product instead
/// of one minus that value. The constructor stores false and one for inversion and power, leaving
/// normal and view absent. An absent canvas-fragment normal reads the current command's normal texture;
/// an absent view and noncanvas normal are zero. A canvas vertex needs an explicit normal.
/// Shader power domain and precision remain native. Graph edits require reimport.</remarks>
public sealed class VisualShaderNodeFresnel : VisualShaderNode
{
    /// <summary>Creates absent normal/view inputs, false inversion and power one.</summary>
    public VisualShaderNodeFresnel() { InitializeDefault(2, false); InitializeDefault(3, 1f); }
    internal override PortType[] Inputs => [PortType.Vector3, PortType.Vector3, PortType.Boolean, PortType.Scalar];
    internal override PortType[] Outputs => [PortType.Scalar];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs)
    {
        var dot = $"clamp(dot({inputs[0]}, {inputs[1]}), 0.0, 1.0)";
        return $"pow(({inputs[2]}) ? {dot} : (1.0 - {dot}), {inputs[3]})";
    }
    internal override void CopyDefinitionTo(VisualShaderNode target) { }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeFresnel();
}
