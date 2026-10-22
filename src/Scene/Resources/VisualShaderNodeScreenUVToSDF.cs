namespace Electron2D;

/// <summary>Converts normalized screen UV to framebuffer pixel coordinates in a fragment graph.</summary>
/// <remarks>An unconnected vector input uses the current fragment screen UV even when an authored numeric default is stored. A connected input is multiplied by framebuffer size without clamping.
/// Conversion uses framebuffer dimensions and does not request field generation.
/// Only reachable fragment-stage nodes import; definition edits require reimport.</remarks>
public sealed class VisualShaderNodeScreenUVToSDF : VisualShaderNode
{
    /// <summary>Creates a definition with no authored input defaults.</summary>
    public VisualShaderNodeScreenUVToSDF() { }
    internal override PortType[] Inputs => [PortType.Vector2];
    internal override PortType[] Outputs => [PortType.Vector2];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => "(" + inputs[0] + " / e2d_values.SCREEN_PIXEL_SIZE)";
    internal override void CopyDefinitionTo(VisualShaderNode target) { }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeScreenUVToSDF();
}
