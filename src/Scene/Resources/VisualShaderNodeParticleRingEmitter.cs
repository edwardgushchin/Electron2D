namespace Electron2D;

/// <summary>Samples a planar ring using an angular distribution and a linear radius range.</summary>
public sealed class VisualShaderNodeParticleRingEmitter : VisualShaderNodeParticleEmitter
{
    /// <summary>Creates the initial emission shape and its explicit numeric defaults.</summary>
    public VisualShaderNodeParticleRingEmitter() { InitializeDefault(0, 10f); InitializeDefault(1, 0f); }
    internal override PortType[] Inputs => [PortType.Scalar, PortType.Scalar];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => "e2d_ring(e2d_seed, " + inputs[0] + ", " + inputs[1] + ")";
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeParticleRingEmitter();
}
