namespace Electron2D;

/// <summary>Converts framebuffer pixel coordinates to normalized screen UV in a fragment graph.</summary>
/// <remarks>The vector input is a framebuffer pixel position, absent as (0,0). The vector output divides each component by the framebuffer size; offscreen coordinates remain unclamped.
/// Conversion uses framebuffer dimensions and does not request field generation.
/// Only reachable fragment-stage nodes import; definition edits require reimport.</remarks>
public sealed class VisualShaderNodeSDFToScreenUV : VisualShaderNode
{
    /// <summary>Creates a definition with no authored input defaults.</summary>
    public VisualShaderNodeSDFToScreenUV() { }
    internal override PortType[] Inputs => [PortType.Vector2];
    internal override PortType[] Outputs => [PortType.Vector2];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => "(" + inputs[0] + " * e2d_values.SCREEN_PIXEL_SIZE)";
    internal override void CopyDefinitionTo(VisualShaderNode target) { }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeSDFToScreenUV();
}
