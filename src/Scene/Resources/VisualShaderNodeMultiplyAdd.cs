namespace Electron2D;

/// <summary>Multiplies two numeric inputs and adds a third using fused shader arithmetic.</summary>
/// <remarks>Inputs are a, b and c. Arithmetic executes in the imported shader; rounding follows the native fused operation.</remarks>
public sealed class VisualShaderNodeMultiplyAdd : VisualShaderNode
{
    /// <summary>The numeric component type processed by this operation.</summary>
    public enum OpTypeEnum
    {
        /// <summary>A floating scalar.</summary>
        Scalar = 0,
        /// <summary>Two floating components.</summary>
        Vector2 = 1,
        /// <summary>Three floating components.</summary>
        Vector3 = 2,
        /// <summary>Four floating components.</summary>
        Vector4 = 3,
        /// <summary>The nonselectable type-count sentinel.</summary>
        Max = 4,
    }
    private OpTypeEnum _operationType;
    /// <summary>Creates a scalar definition with its initial input defaults.</summary>
    /// <remarks>The explicit scalar defaults are zero, one and zero.</remarks>
    public VisualShaderNodeMultiplyAdd()
    {
        InitializeDefault(0, 0f);
        InitializeDefault(1, 1f);
        InitializeDefault(2, 0f);
    }
    /// <summary>Gets or selects the numeric input layout, converting existing defaults atomically.</summary>
    /// <value>Scalar initially. A changed selection recreates absent defaults; vector expansion repeats the last component.</value>
    /// <remarks>An unchanged selection emits no event. The complete new layout and defaults commit before Changed observers run.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The selection is undefined or the count sentinel.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public OpTypeEnum OpType
    {
        get { ThrowIfDisposed(); return _operationType; }
        set
        {
            ThrowIfDisposed();
            if (!Enum.IsDefined(value) || value == OpTypeEnum.Max) throw new ArgumentOutOfRangeException(nameof(value));
            if (_operationType == value) return;
            ChangeDefaultTypes(InputsFor(value).Select((kind, port) => port == 1
                ? VisualShaderValues.ConvertDefaultValue(VisualShaderValues.Zero(kind), 1f)
                : VisualShaderValues.Zero(kind)).ToArray());
            _operationType = value; EmitChanged();
        }
    }
    private static PortType Kind(OpTypeEnum type) => type switch
    { OpTypeEnum.Scalar => PortType.Scalar, OpTypeEnum.Vector2 => PortType.Vector2, OpTypeEnum.Vector3 => PortType.Vector3, _ => PortType.Vector4 };
    private static PortType[] InputsFor(OpTypeEnum type) => [Kind(type), Kind(type), Kind(type)];
    internal override PortType[] Inputs => InputsFor(_operationType);
    internal override PortType[] Outputs => [Kind(_operationType)];
    internal override string Expression(int output, IReadOnlyList<string> inputs) => $"fma({inputs[0]}, {inputs[1]}, {inputs[2]})";
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeMultiplyAdd)target)._operationType = _operationType;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeMultiplyAdd();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        new PropertyDescriptor[] { new PropertyDescriptor<VisualShaderNodeMultiplyAdd, OpTypeEnum>(nameof(OpType), n => n.OpType,
            (n, v) => n.OpType = v, stored: true) }.Concat(base.GetPropertyDescriptors());
}
