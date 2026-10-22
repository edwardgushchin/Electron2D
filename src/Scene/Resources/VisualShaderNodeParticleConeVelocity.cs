namespace Electron2D;

/// <summary>Generates a seeded three-component numeric direction with independent angular spread.</summary>
/// <remarks>The direction is numeric data; physical particle outputs retain only their planar components.</remarks>
public sealed class VisualShaderNodeParticleConeVelocity : VisualShaderNode
{
    /// <summary>Creates direction (1,0,0) and a 45-degree spread.</summary>
    public VisualShaderNodeParticleConeVelocity() { InitializeDefault(0, new Vector3(1, 0, 0)); InitializeDefault(1, 45f); }
    internal override PortType[] Inputs => [PortType.Vector3, PortType.Scalar];
    internal override PortType[] Outputs => [PortType.Vector3];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => "e2d_cone(e2d_seed, " + inputs[0] + ", " + inputs[1] + ")";
    internal override void CopyDefinitionTo(VisualShaderNode target) { }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeParticleConeVelocity();
}
