namespace Electron2D;

/// <summary>Samples a circle using a seeded normalized-square direction and a linear radius range.</summary>
public sealed class VisualShaderNodeParticleSphereEmitter : VisualShaderNodeParticleEmitter
{
    /// <summary>Creates the initial emission shape and its explicit numeric defaults.</summary>
    public VisualShaderNodeParticleSphereEmitter() { InitializeDefault(0, 10f); InitializeDefault(1, 0f); }
    internal override PortType[] Inputs => [PortType.Scalar, PortType.Scalar];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => "e2d_circle(e2d_seed, " + inputs[0] + ", " + inputs[1] + ")";
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeParticleSphereEmitter();
}
