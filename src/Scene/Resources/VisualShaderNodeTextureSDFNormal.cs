namespace Electron2D;

/// <summary>Samples the gradient direction of the canvas signed-distance field.</summary>
/// <remarks>The vector input is a framebuffer pixel position; absence means (0,0). The vector output normalizes central differences at texture UV offsets of 0.001. Flat gradients retain native normalization semantics.
/// Uses the renderer's pixel-space field, including its viewport oversize/resolution and occluder policies.
/// Only reachable fragment-stage nodes import; definition edits require reimport.</remarks>
public sealed class VisualShaderNodeTextureSDFNormal : VisualShaderNode
{
    /// <summary>Creates a definition with no authored input defaults.</summary>
    public VisualShaderNodeTextureSDFNormal() { }
    internal override PortType[] Inputs => [PortType.Vector2];
    internal override PortType[] Outputs => [PortType.Vector2];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => "e2d_sdf_normal(" + inputs[0] + ")";
    internal override void CopyDefinitionTo(VisualShaderNode target) { }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeTextureSDFNormal();
}
