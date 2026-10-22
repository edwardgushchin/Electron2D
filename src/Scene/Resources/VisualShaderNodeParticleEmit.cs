namespace Electron2D;

/// <summary>Emits a subparticle through the retained bounded GPU queue when its condition is true.</summary>
/// <remarks>This side-effect operation executes even without a connection to the reserved output.
/// Missing field inputs use the current particle channels; queue saturation drops excess records.</remarks>
public sealed class VisualShaderNodeParticleEmit : VisualShaderNode
{
    private ParticlesEmitFlags _flags = (ParticlesEmitFlags)31;
    /// <summary>Creates an emission operation supplying all five fields.</summary>
    public VisualShaderNodeParticleEmit() { InitializeDefault(0, true); }
    /// <summary>Gets or replaces the supplied-field mask.</summary>
    /// <value>All five flags initially; zero supplies no fields.</value>
    /// <exception cref="ArgumentOutOfRangeException">The mask contains undefined bits.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public ParticlesEmitFlags Flags { get { ThrowIfDisposed(); return _flags; } set { ThrowIfDisposed(); if (((int)value & ~31) != 0) throw new ArgumentOutOfRangeException(nameof(value)); if (_flags == value) return; _flags = value; EmitChanged(); } }
    internal override PortType[] Inputs => [PortType.Boolean, PortType.Matrix, PortType.Vector2, PortType.Vector3, PortType.Scalar, PortType.Vector3, PortType.Scalar];
    internal override PortType[] Outputs => [];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => throw new InvalidOperationException("A side-effect emitter has no output value.");
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeParticleEmit)target)._flags = _flags;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeParticleEmit();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeParticleEmit, ParticlesEmitFlags>(nameof(Flags), n => n.Flags, (n, v) => n.Flags = v, stored: true)]);
}
