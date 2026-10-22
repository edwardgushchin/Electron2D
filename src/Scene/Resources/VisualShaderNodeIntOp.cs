namespace Electron2D;

/// <summary>Applies a native signed 32-bit integer operation to two graph inputs.</summary>
/// <remarks>Inputs and outputs retain integer precision until connected to another numeric port role.
/// Arithmetic executes in the imported shader; invalid native arithmetic domains retain their backend behavior.</remarks>
public sealed class VisualShaderNodeIntOp : VisualShaderNode
{
    private VisualShaderIntegerOperator _operation;
    /// <summary>Creates a signed definition with explicit zero input defaults.</summary>
    public VisualShaderNodeIntOp()
    { InitializeDefault(0, 0); InitializeDefault(1, 0); }
    /// <summary>Gets or selects the signed integer operator.</summary>
    /// <value>Add initially. The count sentinel is not selectable.</value>
    /// <remarks>An unchanged selection emits no event. The new selection commits before Changed observers execute.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The selection is undefined or the count sentinel.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public VisualShaderIntegerOperator Operator
    {
        get { ThrowIfDisposed(); return _operation; }
        set
        {
            ThrowIfDisposed();
            if (!Enum.IsDefined(value) || value == VisualShaderIntegerOperator.EnumSize) throw new ArgumentOutOfRangeException(nameof(value));
            if (_operation == value) return;
            _operation = value; EmitChanged();
        }
    }
    internal override PortType[] Inputs => [PortType.ScalarInt, PortType.ScalarInt];
    internal override PortType[] Outputs => [PortType.ScalarInt];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => VisualShaderFunctions.IntegerOperation(_operation, inputs);
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeIntOp)target)._operation = _operation;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeIntOp();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeIntOp, VisualShaderIntegerOperator>(nameof(Operator), n => n.Operator, (n, v) => n.Operator = v, stored: true)]);
}
