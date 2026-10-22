namespace Electron2D;

/// <summary>The shared planar position role of particle emission-shape graph operations.</summary>
/// <remarks>Emitter positions use two components. Dimensional mode selection has no separate planar behavior.</remarks>
public abstract class VisualShaderNodeParticleEmitter : VisualShaderNode
{
    /// <summary>Initializes a planar emission-shape operation.</summary>
    protected VisualShaderNodeParticleEmitter() { }
    internal override PortType[] Outputs => [PortType.Vector2];
    internal override void CopyDefinitionTo(VisualShaderNode target) { }
}
