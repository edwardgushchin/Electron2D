namespace Electron2D;

/// <summary>Selects between two numeric or Boolean graph inputs using a Boolean condition.</summary>
/// <remarks>Inputs are condition, true value and false value. Floating selections use native interpolation;
/// integer, Boolean and matrix selections retain exact values. Type changes preserve the condition's presence and value.</remarks>
public sealed class VisualShaderNodeSwitch : VisualShaderNode
{
    private VisualShaderValueType _type;
    /// <summary>Creates a floating definition with false condition, true value one and false value zero.</summary>
    public VisualShaderNodeSwitch()
    { InitializeDefault(0, false); InitializeDefault(1, 1f); InitializeDefault(2, 0f); }
    /// <summary>Gets or selects the result type, converting both value defaults atomically.</summary>
    /// <value>Float initially. A changed type recreates absent value defaults; the condition is unchanged.</value>
    /// <remarks>Vector expansion repeats the last component. Boolean conversions retain target initial values.
    /// Matrix layout changes use identity operands; conversions from matrices use the target defaults.
    /// Unchanged selection is quiet; Changed runs after the new type and defaults commit.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The selection is undefined or the count sentinel.</exception>
    /// <exception cref="OverflowException">An operand cannot be represented by the target integer type; state is unchanged.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public VisualShaderValueType OpType
    {
        get { ThrowIfDisposed(); return _type; }
        set
        {
            ThrowIfDisposed();
            if (!Enum.IsDefined(value) || value == VisualShaderValueType.Max) throw new ArgumentOutOfRangeException(nameof(value));
            if (_type == value) return;
            var kind = (PortType)value;
            var one = value switch
            {
                VisualShaderValueType.Float => (object)1f,
                VisualShaderValueType.Int => 1,
                VisualShaderValueType.UInt => 1u,
                VisualShaderValueType.Vector2 => new Vector2(1, 1),
                VisualShaderValueType.Vector3 => new Vector3(1, 1, 1),
                VisualShaderValueType.Vector4 => new Vector4(1, 1, 1, 1),
                VisualShaderValueType.Matrix => System.Numerics.Matrix4x4.Identity,
                _ => true
            };
            ChangeDefaultTypes([one, VisualShaderValues.Zero(kind)], 1); _type = value; EmitChanged();
        }
    }
    internal override PortType[] Inputs => [PortType.Boolean, (PortType)_type, (PortType)_type];
    internal override PortType[] Outputs => [(PortType)_type];
    internal override string Expression(int output, IReadOnlyList<string> inputs) => _type is VisualShaderValueType.Float or VisualShaderValueType.Vector2 or VisualShaderValueType.Vector3 or VisualShaderValueType.Vector4
        ? $"mix({inputs[2]}, {inputs[1]}, float({inputs[0]}))" : $"({inputs[0]} ? {inputs[1]} : {inputs[2]})";
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeSwitch)target)._type = _type;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeSwitch();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => new PropertyDescriptor[]
    { new PropertyDescriptor<VisualShaderNodeSwitch, VisualShaderValueType>(nameof(OpType), n => n.OpType, (n, v) => n.OpType = v, stored: true) }.Concat(base.GetPropertyDescriptors());
}
