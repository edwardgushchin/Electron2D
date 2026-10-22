namespace Electron2D;

/// <summary>Computes distance between two numeric graph vectors at the selected width.</summary>
/// <remarks>Arithmetic executes in the imported shader; source edits require reimport. Numeric components do not introduce spatial scene types.</remarks>
public sealed class VisualShaderNodeVectorDistance : VisualShaderNodeVectorBase
{
    /// <summary>Creates the definition with zero numeric input defaults.</summary>
    public VisualShaderNodeVectorDistance()
    {
        InitializeDefault(0, Vector3.Zero);
        InitializeDefault(1, Vector3.Zero);
    }
    internal override PortType[] Inputs => [VectorKind, VectorKind];
    internal override PortType[] Outputs => [PortType.Scalar];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => $"distance({inputs[0]}, {inputs[1]})";
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeVectorDistance();
}
