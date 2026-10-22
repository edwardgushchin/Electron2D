namespace Electron2D;

/// <summary>Converts UV coordinates to wrapped radius and angle in an imported canvas graph.</summary>
/// <remarks>Inputs are UV, center, zoom and angular repeat. Unconnected UV uses the current stage UV;
/// stored numeric UV defaults remain source data. Results wrap into [0,1) using floor-based modulo.
/// The angle at the exact center retains native shader semantics. Edits require reimport.</remarks>
public sealed class VisualShaderNodeUVPolarCoord : VisualShaderNode
{
    /// <summary>Creates implicit UV with center (0.5,0.5), unit zoom and unit angular repeat.</summary>
    public VisualShaderNodeUVPolarCoord() { InitializeDefault(1, new Vector2(.5f, .5f)); InitializeDefault(2, 1f); InitializeDefault(3, 1f); }
    internal override PortType[] Inputs => [PortType.Vector2, PortType.Vector2, PortType.Scalar, PortType.Scalar];
    internal override PortType[] Outputs => [PortType.Vector2];
    internal override string Expression(int output, IReadOnlyList<string> inputs)
    {
        var direction = "(" + inputs[0] + " - " + inputs[1] + ")";
        return "mod(vec2(length(" + direction + ") * 2.0 * " + inputs[2] + ", atan(" + direction + ".y, " + direction + ".x) * (1.0 / 6.283185307179586) * " + inputs[3] + "), vec2(1.0))";
    }
    internal override void CopyDefinitionTo(VisualShaderNode target) { }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeUVPolarCoord();
}
