namespace Electron2D;

/// <summary>Computes the length of a numeric graph vector at the selected width.</summary>
/// <remarks>Arithmetic executes in the imported shader; source edits require reimport. Numeric components do not introduce spatial scene types.</remarks>
public sealed class VisualShaderNodeVectorLen : VisualShaderNodeVectorBase
{
    /// <summary>Creates the definition with zero numeric input defaults.</summary>
    public VisualShaderNodeVectorLen()
    {
        InitializeDefault(0, Vector3.Zero);
    }
    internal override PortType[] Inputs => [VectorKind];
    internal override PortType[] Outputs => [PortType.Scalar];
    internal override string Expression(int output, IReadOnlyList<string> inputs) => $"length({inputs[0]})";
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeVectorLen();
}
