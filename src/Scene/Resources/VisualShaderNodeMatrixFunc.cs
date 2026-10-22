namespace Electron2D;

/// <summary>Applies native inverse or transpose to a numeric matrix input.</summary>
/// <remarks>Matrix4x4 is numeric shader data. Arithmetic retains native precision and undefined-domain rules;
/// inverse requires a nonsingular input for portable results.</remarks>
public sealed class VisualShaderNodeMatrixFunc : VisualShaderNode
{
    /// <summary>The selected native numeric operation.</summary>
    public enum FunctionEnum
    {
        /// <summary>Computes the native matrix inverse.</summary>
        Inverse = 0,
        /// <summary>Swaps rows and columns.</summary>
        Transpose = 1,
        /// <summary>The nonselectable function-count sentinel.</summary>
        Max = 2,
    }
    private FunctionEnum _selection;
    /// <summary>Creates the definition with an explicit identity matrix default.</summary>
    public VisualShaderNodeMatrixFunc() { InitializeDefault(0, System.Numerics.Matrix4x4.Identity); }
    /// <summary>Gets or selects the native operation without changing stored defaults.</summary>
    /// <value>Inverse initially.</value>
    /// <remarks>An unchanged selection is quiet. The new value commits before Changed observers execute.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is undefined or the count sentinel.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public FunctionEnum Function
    {
        get { ThrowIfDisposed(); return _selection; }
        set
        {
            ThrowIfDisposed();
            if (!Enum.IsDefined(value) || value == FunctionEnum.Max) throw new ArgumentOutOfRangeException(nameof(value));
            if (_selection == value) return; _selection = value; EmitChanged();
        }
    }
    internal override PortType[] Inputs => [PortType.Matrix];
    internal override PortType[] Outputs => [PortType.Matrix];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => $"{(_selection == FunctionEnum.Inverse ? "inverse" : "transpose")}({inputs[0]})";
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeMatrixFunc)target)._selection = _selection;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeMatrixFunc();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeMatrixFunc, FunctionEnum>(nameof(Function), n => n.Function, (n, v) => n.Function = v, stored: true)]);
}
