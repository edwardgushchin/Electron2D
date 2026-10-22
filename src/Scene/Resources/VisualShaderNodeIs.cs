namespace Electron2D;

/// <summary>Tests a floating graph result for infinity or NaN.</summary>
/// <remarks>Authored defaults remain finite. Nonfinite computed inputs retain native shader classification rules.</remarks>
public sealed class VisualShaderNodeIs : VisualShaderNode
{
    /// <summary>The floating classification performed by this definition.</summary>
    public enum FunctionEnum
    {
        /// <summary>Tests positive or negative infinity.</summary>
        IsInf = 0,
        /// <summary>Tests not-a-number values.</summary>
        IsNaN = 1,
        /// <summary>The nonselectable function-count sentinel.</summary>
        Max = 2,
    }
    private FunctionEnum _function;
    /// <summary>Creates an infinity test with an explicit zero input.</summary>
    public VisualShaderNodeIs() => InitializeDefault(0, 0f);
    /// <summary>Gets or selects the classification function without changing defaults.</summary>
    /// <value>IsInf initially.</value>
    /// <remarks>Invalid values leave state unchanged. Repeated selection is quiet; observers run after commit.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The function is undefined or the count sentinel.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public FunctionEnum Function
    {
        get { ThrowIfDisposed(); return _function; }
        set
        {
            ThrowIfDisposed();
            if (!Enum.IsDefined(value) || value == FunctionEnum.Max) throw new ArgumentOutOfRangeException(nameof(value));
            if (_function == value) return; _function = value; EmitChanged();
        }
    }
    internal override PortType[] Inputs => [PortType.Scalar];
    internal override PortType[] Outputs => [PortType.Boolean];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => $"{(_function == FunctionEnum.IsInf ? "isinf" : "isnan")}({inputs[0]})";
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeIs)target)._function = _function;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeIs();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeIs, FunctionEnum>(nameof(Function), n => n.Function, (n, v) => n.Function = v, stored: true)]);
}
