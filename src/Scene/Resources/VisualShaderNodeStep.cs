namespace Electron2D;

/// <summary>Returns zero below a threshold and one at or above it.</summary>
/// <remarks>Inputs are edge and value. Scalar-control layouts broadcast the scalar edge across every data component.</remarks>
public sealed class VisualShaderNodeStep : VisualShaderNode
{
    private VisualShaderNumericOpType _operationType;
    /// <summary>Creates a scalar definition with its initial input defaults.</summary>
    /// <remarks>Both edge and value are explicit zero floats.</remarks>
    public VisualShaderNodeStep()
    {
        InitializeDefault(0, 0f);
        InitializeDefault(1, 0f);
    }
    /// <summary>Gets or selects the numeric input layout, converting existing defaults atomically.</summary>
    /// <value>Scalar initially. A changed selection recreates absent defaults; vector expansion repeats the last component.</value>
    /// <remarks>An unchanged selection emits no event. The complete new layout and defaults commit before Changed observers run.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The selection is undefined or the count sentinel.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public VisualShaderNumericOpType OpType
    {
        get { ThrowIfDisposed(); return _operationType; }
        set
        {
            ThrowIfDisposed();
            if (!Enum.IsDefined(value) || value == VisualShaderNumericOpType.Max) throw new ArgumentOutOfRangeException(nameof(value));
            if (_operationType == value) return;
            ChangeDefaultTypes(InputsFor(value).Select(kind => kind == PortType.Vector4
                ? new Vector4(0, 0, 0, 1) : VisualShaderValues.Zero(kind)).ToArray());
            _operationType = value; EmitChanged();
        }
    }
    private static PortType Kind(VisualShaderNumericOpType type) => VisualShaderValues.RangeKind(type);
    private static PortType ControlKind(VisualShaderNumericOpType type) => VisualShaderValues.HasScalarControls(type) ? PortType.Scalar : Kind(type);
    private static PortType[] InputsFor(VisualShaderNumericOpType type) => [ControlKind(type), Kind(type)];
    internal override PortType[] Inputs => InputsFor(_operationType);
    internal override PortType[] Outputs => [Kind(_operationType)];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => $"step({inputs[0]}, {inputs[1]})";
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeStep)target)._operationType = _operationType;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeStep();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        new PropertyDescriptor[] { new PropertyDescriptor<VisualShaderNodeStep, VisualShaderNumericOpType>(nameof(OpType), n => n.OpType,
            (n, v) => n.OpType = v, stored: true) }.Concat(base.GetPropertyDescriptors());
}
