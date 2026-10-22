namespace Electron2D;

/// <summary>Returns xyz from each of the four numeric matrix columns.</summary>
/// <remarks>Inputs and results are numeric shader data. Editing requires reimport before drawing changes.</remarks>
public sealed class VisualShaderNodeMatrixDecompose : VisualShaderNode
{
    /// <summary>Creates the definition with an explicit identity matrix default.</summary>
    public VisualShaderNodeMatrixDecompose() { InitializeDefault(0, System.Numerics.Matrix4x4.Identity); }
    internal override PortType[] Inputs => [PortType.Matrix];
    internal override PortType[] Outputs => [PortType.Vector3, PortType.Vector3, PortType.Vector3, PortType.Vector3];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => $"({inputs[0]})[{output}].xyz";
    internal override void CopyDefinitionTo(VisualShaderNode target) { }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeMatrixDecompose();
}
