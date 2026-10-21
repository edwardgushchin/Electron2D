namespace Electron2D;

/// <summary>Splits a numeric vector into scalar X, Y, Z and W outputs at the selected width.</summary>
/// <remarks>Output indices follow component order. Width changes retain stored connections; import
/// rejects connections to outputs no longer present until the caller repairs them.</remarks>
public sealed class VisualShaderNodeVectorDecompose : VisualShaderNodeVectorBase
{
    /// <summary>Creates a three-component split with one zero vector input default.</summary>
    public VisualShaderNodeVectorDecompose() => InitializeDefault(0, Vector3.Zero);
    internal override PortType[] Inputs => [VectorKind];
    internal override PortType[] Outputs => Enumerable.Repeat(PortType.Scalar, (int)OpType + 2).ToArray();
    internal override string Expression(int output, IReadOnlyList<string> inputs) => $"({inputs[0]}).{"xyzw"[output]}";
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeVectorDecompose();
}
