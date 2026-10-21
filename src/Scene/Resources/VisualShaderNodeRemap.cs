namespace Electron2D;

/// <summary>Maps a numeric value from one interval to another without clamping.</summary>
/// <remarks>Inputs are value, input minimum, input maximum, output minimum and output maximum. Scalar-control layouts broadcast all four bounds. Reversed intervals remain valid; zero input width retains native division behavior.</remarks>
public sealed class VisualShaderNodeRemap : VisualShaderNode
{
    private VisualShaderNumericOpType _operationType;
    /// <summary>Creates a scalar definition with its initial input defaults.</summary>
    /// <remarks>The value default is absent. Input and output bounds each have explicit zero and one float defaults.</remarks>
    public VisualShaderNodeRemap()
    {
        InitializeDefault(1, 0f);
        InitializeDefault(2, 1f);
        InitializeDefault(3, 0f);
        InitializeDefault(4, 1f);
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
            ChangeDefaultTypes(InputsFor(value).Select((kind, port) => port is 2 or 4
                ? VisualShaderValues.ConvertDefaultValue(VisualShaderValues.Zero(kind), 1f)
                : kind == PortType.Vector4 ? new Vector4(0, 0, 0, 1) : VisualShaderValues.Zero(kind)).ToArray());
            _operationType = value; EmitChanged();
        }
    }
    private static PortType Kind(VisualShaderNumericOpType type) => VisualShaderValues.RangeKind(type);
    private static PortType ControlKind(VisualShaderNumericOpType type) => VisualShaderValues.HasScalarControls(type) ? PortType.Scalar : Kind(type);
    private static PortType[] InputsFor(VisualShaderNumericOpType type) => [Kind(type), ControlKind(type), ControlKind(type), ControlKind(type), ControlKind(type)];
    internal override PortType[] Inputs => InputsFor(_operationType);
    internal override PortType[] Outputs => [Kind(_operationType)];
    internal override string Expression(int output, IReadOnlyList<string> inputs) => $"({inputs[3]} + ({inputs[4]} - {inputs[3]}) * (({inputs[0]} - {inputs[1]}) / ({inputs[2]} - {inputs[1]})))";
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeRemap)target)._operationType = _operationType;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeRemap();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        new PropertyDescriptor[] { new PropertyDescriptor<VisualShaderNodeRemap, VisualShaderNumericOpType>(nameof(OpType), n => n.OpType,
            (n, v) => n.OpType = v, stored: true) }.Concat(base.GetPropertyDescriptors());
}
