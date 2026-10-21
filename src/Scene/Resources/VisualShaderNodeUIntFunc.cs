namespace Electron2D;

/// <summary>Applies a native unsigned 32-bit integer function to one graph input.</summary>
/// <remarks>Inputs and outputs retain integer precision until connected to another numeric port role.
/// Arithmetic executes in the imported shader; invalid native arithmetic domains retain their backend behavior.</remarks>
public sealed class VisualShaderNodeUIntFunc : VisualShaderNode
{
    /// <summary>The selected integer function.</summary>
    public enum FunctionEnum
    {
        /// <summary>Negates the unsigned input modulo its 32-bit representation.</summary>
        Negate = 0,
        /// <summary>Complements every input bit.</summary>
        BitwiseNot = 1,
        /// <summary>The nonselectable function-count sentinel.</summary>
        Max = 2,
    }
    private FunctionEnum _function = FunctionEnum.Negate;
    /// <summary>Creates a unsigned definition with explicit zero input defaults.</summary>
    public VisualShaderNodeUIntFunc()
    { InitializeDefault(0, 0u); }
    /// <summary>Gets or selects the unsigned integer function.</summary>
    /// <value>Negate initially. The count sentinel is not selectable.</value>
    /// <remarks>An unchanged selection emits no event. The new selection commits before Changed observers execute.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The selection is undefined or the count sentinel.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public FunctionEnum Function
    {
        get { ThrowIfDisposed(); return _function; }
        set
        {
            ThrowIfDisposed();
            if (!Enum.IsDefined(value) || value == FunctionEnum.Max) throw new ArgumentOutOfRangeException(nameof(value));
            if (_function == value) return;
            _function = value; EmitChanged();
        }
    }
    internal override PortType[] Inputs => [PortType.ScalarUInt];
    internal override PortType[] Outputs => [PortType.ScalarUInt];
    internal override string Expression(int output, IReadOnlyList<string> inputs) => _function switch
    {
        FunctionEnum.Negate => $"(-({inputs[0]}))",
        FunctionEnum.BitwiseNot => $"(~({inputs[0]}))",
        _ => throw new InvalidOperationException("Invalid integer function.")
    };
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeUIntFunc)target)._function = _function;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeUIntFunc();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeUIntFunc, FunctionEnum>(nameof(Function), n => n.Function, (n, v) => n.Function = v, stored: true)]);
}
