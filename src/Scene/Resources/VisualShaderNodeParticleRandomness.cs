namespace Electron2D;

/// <summary>Generates a seeded scalar or component-wise numeric vector range in a particle graph.</summary>
/// <remarks>An unconnected seed uses the lifecycle's sequential stream. A connected seed is a writable generated
/// value whose state advances for each component. Equal and reversed bounds retain arithmetic range semantics.</remarks>
public sealed class VisualShaderNodeParticleRandomness : VisualShaderNode
{
    private VisualShaderFloatOpType _opType;
    /// <summary>Creates a scalar range with explicit bounds minus one and one.</summary>
    public VisualShaderNodeParticleRandomness() { InitializeDefault(1, -1f); InitializeDefault(2, 1f); }
    /// <summary>Gets or changes the range width, converting both bounds to the selected numeric role.</summary>
    /// <value>Scalar initially. Missing bounds are recreated as zero when the width changes.</value>
    /// <exception cref="ArgumentOutOfRangeException">The width is undefined or Max.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public VisualShaderFloatOpType OpType
    {
        get { ThrowIfDisposed(); return _opType; }
        set
        {
            ThrowIfDisposed(); if (value < 0 || value >= VisualShaderFloatOpType.Max) throw new ArgumentOutOfRangeException(nameof(value));
            if (_opType == value) return;
            var zero = VisualShaderValues.Zero(Kind(value)); ChangeDefaultTypes([zero, zero], 1); _opType = value; EmitChanged();
        }
    }
    private static PortType Kind(VisualShaderFloatOpType op) => op switch { VisualShaderFloatOpType.Scalar => PortType.Scalar, VisualShaderFloatOpType.Vector2 => PortType.Vector2, VisualShaderFloatOpType.Vector3 => PortType.Vector3, _ => PortType.Vector4 };
    internal override PortType[] Inputs => [PortType.ScalarUInt, Kind(_opType), Kind(_opType)];
    internal override PortType[] Outputs => [Kind(_opType)];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => "e2d_range(" + string.Join(", ", inputs) + ")";
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeParticleRandomness)target)._opType = _opType;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeParticleRandomness();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => new PropertyDescriptor[]
        { new PropertyDescriptor<VisualShaderNodeParticleRandomness, VisualShaderFloatOpType>(nameof(OpType), n => n.OpType, (n, v) => n.OpType = v, stored: true) }.Concat(base.GetPropertyDescriptors());
}
