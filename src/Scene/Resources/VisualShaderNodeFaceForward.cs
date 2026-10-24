namespace Electron2D;

/// <summary>Orients a numeric vector using the sign of the incident/reference dot product.</summary>
/// <remarks>Returns the first input when the dot product of inputs one and two is negative;
/// otherwise returns its negation, including at zero. Inputs are not normalized.
/// The inherited component-count selector supports two, three and four components.</remarks>
public sealed class VisualShaderNodeFaceForward : VisualShaderNodeVectorBase
{
    /// <summary>Creates three zero three-component input defaults.</summary>
    public VisualShaderNodeFaceForward()
    { InitializeDefault(0, Vector3.Zero); InitializeDefault(1, Vector3.Zero); InitializeDefault(2, Vector3.Zero); }
    internal override PortType[] Inputs => [VectorKind, VectorKind, VectorKind];
    internal override PortType[] Outputs => [VectorKind];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => $"faceforward({inputs[0]}, {inputs[1]}, {inputs[2]})";
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeFaceForward();
}
