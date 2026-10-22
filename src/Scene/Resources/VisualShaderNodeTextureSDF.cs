namespace Electron2D;

/// <summary>Samples the signed nearest-contour field in an imported fragment graph.</summary>
/// <remarks>The vector input is a framebuffer pixel position; absence means (0,0). The scalar result is a signed pixel distance, negative inside a participating closed contour. Open contours have no interior.
/// Uses the renderer's pixel-space field, including its viewport oversize/resolution and occluder policies.
/// Only reachable fragment-stage nodes import; definition edits require reimport.</remarks>
public sealed class VisualShaderNodeTextureSDF : VisualShaderNode
{
    /// <summary>Creates a definition with no authored input defaults.</summary>
    public VisualShaderNodeTextureSDF() { }
    internal override PortType[] Inputs => [PortType.Vector2];
    internal override PortType[] Outputs => [PortType.Scalar];
    internal override string Expression(int output, IReadOnlyList<string> inputs) => "e2d_sdf(" + inputs[0] + ")";
    internal override void CopyDefinitionTo(VisualShaderNode target) { }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeTextureSDF();
}
