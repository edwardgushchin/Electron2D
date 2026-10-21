namespace Electron2D;

/// <summary>Applies a native signed 32-bit integer function to one graph input.</summary>
/// <remarks>Inputs and outputs retain integer precision until connected to another numeric port role.
/// Arithmetic executes in the imported shader; invalid native arithmetic domains retain their backend behavior.</remarks>
public sealed class VisualShaderNodeIntFunc : VisualShaderNode
{
    /// <summary>The selected integer function.</summary>
    public enum FunctionEnum
    {
        /// <summary>Returns the native signed absolute value.</summary>
        Abs = 0,
        /// <summary>Negates the signed input.</summary>
        Negate = 1,
        /// <summary>Returns negative one, zero or one according to the signed input.</summary>
        Sign = 2,
        /// <summary>Complements every input bit.</summary>
        BitwiseNot = 3,
        /// <summary>The nonselectable function-count sentinel.</summary>
        Max = 4,
    }
    private FunctionEnum _function = FunctionEnum.Sign;
    /// <summary>Creates a signed definition with explicit zero input defaults.</summary>
    public VisualShaderNodeIntFunc()
    { InitializeDefault(0, 0); }
    /// <summary>Gets or selects the signed integer function.</summary>
    /// <value>Sign initially. The count sentinel is not selectable.</value>
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
    internal override PortType[] Inputs => [PortType.ScalarInt];
    internal override PortType[] Outputs => [PortType.ScalarInt];
    internal override string Expression(int output, IReadOnlyList<string> inputs) => _function switch
    {
        FunctionEnum.Abs => $"abs({inputs[0]})",
        FunctionEnum.Negate => $"(-({inputs[0]}))",
        FunctionEnum.Sign => $"sign({inputs[0]})",
        FunctionEnum.BitwiseNot => $"(~({inputs[0]}))",
        _ => throw new InvalidOperationException("Invalid integer function.")
    };
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeIntFunc)target)._function = _function;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeIntFunc();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeIntFunc, FunctionEnum>(nameof(Function), n => n.Function, (n, v) => n.Function = v, stored: true)]);
}
