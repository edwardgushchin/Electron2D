namespace Electron2D;

/// <summary>Clamps each input component to its inclusive lower and upper bounds.</summary>
/// <remarks>Inputs are value, minimum and maximum. Bounds must be ordered; reversed bounds retain native shader domain behavior.</remarks>
public sealed class VisualShaderNodeClamp : VisualShaderNode
{
    /// <summary>The numeric component type processed by this operation.</summary>
    public enum OpTypeEnum
    {
        /// <summary>A floating scalar.</summary>
        Float = 0,
        /// <summary>A signed integer scalar.</summary>
        Int = 1,
        /// <summary>An unsigned integer scalar.</summary>
        UInt = 2,
        /// <summary>Two floating components.</summary>
        Vector2 = 3,
        /// <summary>Three floating components.</summary>
        Vector3 = 4,
        /// <summary>Four floating components.</summary>
        Vector4 = 5,
        /// <summary>The nonselectable type-count sentinel.</summary>
        Max = 6,
    }
    private OpTypeEnum _operationType;
    /// <summary>Creates a scalar definition with its initial input defaults.</summary>
    /// <remarks>Value and lower bound are explicit zero floats; the upper bound is one.</remarks>
    public VisualShaderNodeClamp()
    {
        InitializeDefault(0, 0f);
        InitializeDefault(1, 0f);
        InitializeDefault(2, 1f);
    }
    /// <summary>Gets or selects the numeric input layout, converting existing defaults atomically.</summary>
    /// <value>Float initially. A changed selection recreates absent defaults; vector expansion repeats the last component.</value>
    /// <remarks>An unchanged selection emits no event. The complete new layout and defaults commit before Changed observers run.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The selection is undefined or the count sentinel.</exception>
    /// <exception cref="OverflowException">An existing default cannot be represented by the selected integer type; no state changes.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public OpTypeEnum OpType
    {
        get { ThrowIfDisposed(); return _operationType; }
        set
        {
            ThrowIfDisposed();
            if (!Enum.IsDefined(value) || value == OpTypeEnum.Max) throw new ArgumentOutOfRangeException(nameof(value));
            if (_operationType == value) return;
            ChangeDefaultTypes(InputsFor(value).Select(kind => kind == PortType.Vector4
                ? new Vector4(0, 0, 0, 1) : VisualShaderValues.Zero(kind)).ToArray());
            _operationType = value; EmitChanged();
        }
    }
    private static PortType Kind(OpTypeEnum type) => type switch
    {
        OpTypeEnum.Float => PortType.Scalar,
        OpTypeEnum.Int => PortType.ScalarInt,
        OpTypeEnum.UInt => PortType.ScalarUInt,
        OpTypeEnum.Vector2 => PortType.Vector2,
        OpTypeEnum.Vector3 => PortType.Vector3,
        _ => PortType.Vector4
    };
    private static PortType[] InputsFor(OpTypeEnum type) => [Kind(type), Kind(type), Kind(type)];
    internal override PortType[] Inputs => InputsFor(_operationType);
    internal override PortType[] Outputs => [Kind(_operationType)];
    internal override string Expression(int output, IReadOnlyList<string> inputs) => $"clamp({inputs[0]}, {inputs[1]}, {inputs[2]})";
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeClamp)target)._operationType = _operationType;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeClamp();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        new PropertyDescriptor[] { new PropertyDescriptor<VisualShaderNodeClamp, OpTypeEnum>(nameof(OpType), n => n.OpType,
            (n, v) => n.OpType = v, stored: true) }.Concat(base.GetPropertyDescriptors());
}
