namespace Electron2D;

/// <summary>Casts a segment through the canvas signed-distance field.</summary>
/// <remarks>Vector inputs are from/to framebuffer pixel positions, absent as zero. Outputs are signed travelled distance, hit and endpoint. Each step adds the sampled distance before testing a 0.01-pixel hit threshold; the result is capped at segment length. Starting inside may return a negative distance and a backwards endpoint. A zero-length segment returns zero, false and its start.
/// Uses the renderer's pixel-space field, including its viewport oversize/resolution and occluder policies.
/// Only reachable fragment-stage nodes import; definition edits require reimport.</remarks>
public sealed class VisualShaderNodeSDFRaymarch : VisualShaderNode
{
    /// <summary>Creates a definition with no authored input defaults.</summary>
    public VisualShaderNodeSDFRaymarch() { }
    internal override PortType[] Inputs => [PortType.Vector2, PortType.Vector2];
    internal override PortType[] Outputs => [PortType.Scalar, PortType.Boolean, PortType.Vector2];
    internal override string Expression(int output, IReadOnlyList<string> inputs) => throw new InvalidOperationException("Raymarch outputs require shared graph lowering.");
    internal override void CopyDefinitionTo(VisualShaderNode target) { }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeSDFRaymarch();
}
