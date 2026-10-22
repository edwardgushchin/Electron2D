namespace Electron2D;

/// <summary>Samples a centered rectangle whose input is the full width and height.</summary>
public sealed class VisualShaderNodeParticleBoxEmitter : VisualShaderNodeParticleEmitter
{
    /// <summary>Creates the initial emission shape and its explicit numeric defaults.</summary>
    public VisualShaderNodeParticleBoxEmitter() { InitializeDefault(0, Vector2.One); }
    internal override PortType[] Inputs => [PortType.Vector2];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => "e2d_box(e2d_seed, " + inputs[0] + ")";
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeParticleBoxEmitter();
}
