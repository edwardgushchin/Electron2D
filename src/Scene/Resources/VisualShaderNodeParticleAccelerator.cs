namespace Electron2D;

/// <summary>Calculates seeded linear, radial or tangential particle acceleration components.</summary>
/// <remarks>Three-component arithmetic lifts physical planar velocity/position with a zero third component.</remarks>
public sealed class VisualShaderNodeParticleAccelerator : VisualShaderNode
{
    /// <summary>The acceleration direction policy.</summary>
    public enum ModeEnum
    {
        /// <summary>Normalize the current velocity.</summary>
        Linear = 0,
        /// <summary>Normalize displacement from the emission origin.</summary>
        Radial = 1,
        /// <summary>Normalize the cross product of radial displacement and the supplied numeric axis.</summary>
        Tangential = 2,
        /// <summary>The nonselectable mode bound.</summary>
        Max = 3,
    }
    private ModeEnum _mode;
    /// <summary>Creates a linear operation with unit component amounts, zero randomness and axis (0,-9.8,0).</summary>
    public VisualShaderNodeParticleAccelerator() { InitializeDefault(0, Vector3.One); InitializeDefault(1, 0f); InitializeDefault(2, new Vector3(0, -9.8f, 0)); }
    /// <summary>Gets or changes the acceleration direction policy.</summary>
    /// <value>Linear initially.</value>
    /// <exception cref="ArgumentOutOfRangeException">The mode is undefined or Max.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public ModeEnum Mode { get { ThrowIfDisposed(); return _mode; } set { ThrowIfDisposed(); if (value < 0 || value >= ModeEnum.Max) throw new ArgumentOutOfRangeException(nameof(value)); if (_mode == value) return; _mode = value; EmitChanged(); } }
    internal override PortType[] Inputs => [PortType.Vector3, PortType.Scalar, PortType.Vector3];
    internal override PortType[] Outputs => [PortType.Vector3];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => "e2d_accelerate(e2d_seed, " + (int)_mode + ", VELOCITY, TRANSFORM[3].xyz - EMISSION_TRANSFORM[3].xyz, " + string.Join(", ", inputs) + ")";
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeParticleAccelerator)target)._mode = _mode;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeParticleAccelerator();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeParticleAccelerator, ModeEnum>(nameof(Mode), n => n.Mode, (n, v) => n.Mode = v, stored: true)]);
}
