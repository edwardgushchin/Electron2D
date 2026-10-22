namespace Electron2D;

/// <summary>The reserved planar particle output operation of a visual shader lifecycle stage.</summary>
/// <remarks>Physical position and velocity use two components. Color/custom channels retain numeric components;
/// matrix inputs project their XY basis and origin. Start and Process angle inputs use radians about the planar normal.</remarks>
public sealed class VisualShaderNodeParticleOutput : VisualShaderNodeOutput
{
    /// <summary>Creates an unbound particle-start output definition.</summary>
    public VisualShaderNodeParticleOutput() { Stage = VisualShader.Type.Start; }
    internal override PortType[] Inputs => Stage switch
    {
        VisualShader.Type.Start => [PortType.Boolean, PortType.Vector2, PortType.Vector3, PortType.Scalar, PortType.Vector2, PortType.Scalar, PortType.Scalar],
        VisualShader.Type.Process => [PortType.Boolean, PortType.Vector2, PortType.Vector3, PortType.Scalar, PortType.Scalar, PortType.Scalar],
        VisualShader.Type.Collide => [PortType.Boolean, PortType.Vector2, PortType.Vector3, PortType.Scalar, PortType.Matrix],
        _ => [PortType.Vector3, PortType.Scalar, PortType.Vector2, PortType.Vector3, PortType.Scalar, PortType.Matrix]
    };
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeParticleOutput { Stage = Stage };
}
