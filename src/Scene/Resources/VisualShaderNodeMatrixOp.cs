namespace Electron2D;

/// <summary>Applies native arithmetic to two four-by-four numeric matrix inputs.</summary>
/// <remarks>Matrix4x4 is numeric shader data. Arithmetic retains native precision and undefined-domain rules;
/// component division requires nonzero divisors for portable results.</remarks>
public sealed class VisualShaderNodeMatrixOp : VisualShaderNode
{
    /// <summary>The selected native numeric operation.</summary>
    public enum OperatorEnum
    {
        /// <summary>Multiplies the first matrix by the second.</summary>
        Multiply = 0,
        /// <summary>Multiplies the second matrix by the first.</summary>
        MultiplyReversed = 1,
        /// <summary>Multiplies matching components.</summary>
        MultiplyComponents = 2,
        /// <summary>Multiplies matching components in reversed operand order.</summary>
        MultiplyComponentsReversed = 3,
        /// <summary>Adds matching components.</summary>
        Add = 4,
        /// <summary>Subtracts the second matrix from the first.</summary>
        Subtract = 5,
        /// <summary>Subtracts the first matrix from the second.</summary>
        SubtractReversed = 6,
        /// <summary>Divides matching components of the first matrix by the second.</summary>
        Divide = 7,
        /// <summary>Divides matching components of the second matrix by the first.</summary>
        DivideReversed = 8,
        /// <summary>The nonselectable operator-count sentinel.</summary>
        Max = 9,
    }
    private OperatorEnum _selection;
    /// <summary>Creates the definition with two explicit identity matrix defaults.</summary>
    public VisualShaderNodeMatrixOp() { InitializeDefault(0, System.Numerics.Matrix4x4.Identity); InitializeDefault(1, System.Numerics.Matrix4x4.Identity); }
    /// <summary>Gets or selects the native operation without changing stored defaults.</summary>
    /// <value>Multiply initially.</value>
    /// <remarks>An unchanged selection is quiet. The new value commits before Changed observers execute.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is undefined or the count sentinel.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public OperatorEnum Operator
    {
        get { ThrowIfDisposed(); return _selection; }
        set
        {
            ThrowIfDisposed();
            if (!Enum.IsDefined(value) || value == OperatorEnum.Max) throw new ArgumentOutOfRangeException(nameof(value));
            if (_selection == value) return; _selection = value; EmitChanged();
        }
    }
    internal override PortType[] Inputs => [PortType.Matrix, PortType.Matrix];
    internal override PortType[] Outputs => [PortType.Matrix];
    internal override string Expression(int output, IReadOnlyList<string> inputs) => _selection switch
    {
        OperatorEnum.Multiply => $"({inputs[0]} * {inputs[1]})",
        OperatorEnum.MultiplyReversed => $"({inputs[1]} * {inputs[0]})",
        OperatorEnum.MultiplyComponents => $"matrixCompMult({inputs[0]}, {inputs[1]})",
        OperatorEnum.MultiplyComponentsReversed => $"matrixCompMult({inputs[1]}, {inputs[0]})",
        OperatorEnum.Add => $"({inputs[0]} + {inputs[1]})",
        OperatorEnum.Subtract => $"({inputs[0]} - {inputs[1]})",
        OperatorEnum.SubtractReversed => $"({inputs[1]} - {inputs[0]})",
        OperatorEnum.Divide => $"({inputs[0]} / {inputs[1]})",
        _ => $"({inputs[1]} / {inputs[0]})"
    };
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeMatrixOp)target)._selection = _selection;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeMatrixOp();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeMatrixOp, OperatorEnum>(nameof(Operator), n => n.Operator, (n, v) => n.Operator = v, stored: true)]);
}
