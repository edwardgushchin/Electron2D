namespace Electron2D;

/// <summary>Selects an equal, greater or lesser three-component value from two floating graph inputs.</summary>
/// <remarks>Inputs are a, b, tolerance, equal result, greater result and lesser result. Strict abs(a-b) &lt; tolerance
/// selects equal; otherwise a &lt; b selects lesser, and the remaining cases select greater.</remarks>
public sealed class VisualShaderNodeIf : VisualShaderNode
{
    /// <summary>Creates zero operands/results and an explicit 0.00001 tolerance.</summary>
    public VisualShaderNodeIf()
    {
        InitializeDefault(0, 0f); InitializeDefault(1, 0f); InitializeDefault(2, .00001f);
        InitializeDefault(3, Vector3.Zero); InitializeDefault(4, Vector3.Zero); InitializeDefault(5, Vector3.Zero);
    }
    internal override PortType[] Inputs => [PortType.Scalar, PortType.Scalar, PortType.Scalar, PortType.Vector3, PortType.Vector3, PortType.Vector3];
    internal override PortType[] Outputs => [PortType.Vector3];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) =>
        $"(abs({inputs[0]} - {inputs[1]}) < {inputs[2]} ? {inputs[3]} : ({inputs[0]} < {inputs[1]} ? {inputs[5]} : {inputs[4]}))";
    internal override void CopyDefinitionTo(VisualShaderNode target) { }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeIf();
}
