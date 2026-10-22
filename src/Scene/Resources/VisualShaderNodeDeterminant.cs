namespace Electron2D;

/// <summary>Computes the native determinant of a four-by-four numeric matrix.</summary>
/// <remarks>Inputs and results are numeric shader data. Editing requires reimport before drawing changes.</remarks>
public sealed class VisualShaderNodeDeterminant : VisualShaderNode
{
    /// <summary>Creates the definition with an explicit identity matrix default.</summary>
    public VisualShaderNodeDeterminant() { InitializeDefault(0, System.Numerics.Matrix4x4.Identity); }
    internal override PortType[] Inputs => [PortType.Matrix];
    internal override PortType[] Outputs => [PortType.Scalar];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => $"determinant({inputs[0]})";
    internal override void CopyDefinitionTo(VisualShaderNode target) { }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeDeterminant();
}
