namespace Electron2D;

/// <summary>Multiplies three numeric components by an axis-angle rotation in a particle graph.</summary>
/// <remarks>This is numeric shader arithmetic. It does not add a spatial scene transform or particle state.</remarks>
public sealed class VisualShaderNodeParticleMultiplyByAxisAngle : VisualShaderNode
{
    private bool _degreesMode = true;
    /// <summary>Creates a degree-based operation with axis (1,0,0) and angle zero.</summary>
    public VisualShaderNodeParticleMultiplyByAxisAngle() { InitializeDefault(1, new Vector3(1, 0, 0)); InitializeDefault(2, 0f); }
    /// <summary>Gets or sets whether the angle input uses degrees.</summary>
    /// <value>True initially; false selects radians.</value>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public bool DegreesMode { get { ThrowIfDisposed(); return _degreesMode; } set { ThrowIfDisposed(); if (_degreesMode == value) return; _degreesMode = value; EmitChanged(); } }
    internal override PortType[] Inputs => [PortType.Vector3, PortType.Vector3, PortType.Scalar];
    internal override PortType[] Outputs => [PortType.Vector3];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => "(e2d_rotation(" + inputs[1] + ", " + (_degreesMode ? "radians(" + inputs[2] + ")" : inputs[2]) + ") * " + inputs[0] + ")";
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeParticleMultiplyByAxisAngle)target)._degreesMode = _degreesMode;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeParticleMultiplyByAxisAngle();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeParticleMultiplyByAxisAngle, bool>(nameof(DegreesMode), n => n.DegreesMode, (n, v) => n.DegreesMode = v, stored: true)]);
}
