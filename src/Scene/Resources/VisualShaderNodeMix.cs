namespace Electron2D;

/// <summary>Interpolates between two numeric inputs using a weight.</summary>
/// <remarks>Inputs are a, b and weight. Scalar-control layouts use one weight for all components. Weights outside zero through one extrapolate.</remarks>
public sealed class VisualShaderNodeMix : VisualShaderNode
{
    private VisualShaderNumericOpType _operationType;
    /// <summary>Creates a scalar definition with its initial input defaults.</summary>
    /// <remarks>The endpoints are explicit zero and one floats, with weight one half.</remarks>
    public VisualShaderNodeMix()
    {
        InitializeDefault(0, 0f);
        InitializeDefault(1, 1f);
        InitializeDefault(2, .5f);
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
    private static PortType[] InputsFor(VisualShaderNumericOpType type) => [Kind(type), Kind(type), ControlKind(type)];
    internal override PortType[] Inputs => InputsFor(_operationType);
    internal override PortType[] Outputs => [Kind(_operationType)];
    internal override string Expression(int output, IReadOnlyList<string> inputs) => $"mix({inputs[0]}, {inputs[1]}, {inputs[2]})";
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeMix)target)._operationType = _operationType;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeMix();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        new PropertyDescriptor[] { new PropertyDescriptor<VisualShaderNodeMix, VisualShaderNumericOpType>(nameof(OpType), n => n.OpType,
            (n, v) => n.OpType = v, stored: true) }.Concat(base.GetPropertyDescriptors());
}
