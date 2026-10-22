namespace Electron2D;

/// <summary>Applies one binary floating-point operation to two graph inputs.</summary>
/// <remarks>Arithmetic executes in the imported shader, including its native NaN/infinity rules.</remarks>
public sealed class VisualShaderNodeFloatOp : VisualShaderNode
{
    /// <summary>The binary scalar operation.</summary>
    public enum OperatorEnum
    {
        /// <summary>Adds the inputs.</summary>
        Add = 0,
        /// <summary>Subtracts the second input from the first.</summary>
        Subtract = 1,
        /// <summary>Multiplies the inputs.</summary>
        Multiply = 2,
        /// <summary>Divides the first input by the second.</summary>
        Divide = 3,
        /// <summary>Computes x minus y times floor(x divided by y).</summary>
        Modulo = 4,
        /// <summary>Raises the first input to the second input's power.</summary>
        Power = 5,
        /// <summary>Selects the larger input.</summary>
        Max = 6,
        /// <summary>Selects the smaller input.</summary>
        Min = 7,
        /// <summary>Computes the two-argument arctangent of the first and second inputs.</summary>
        Atan2 = 8,
        /// <summary>Returns zero when the second input is below the first, otherwise one.</summary>
        Step = 9,
        /// <summary>The nonselectable operation-count sentinel.</summary>
        EnumSize = 10,
    }
    private OperatorEnum _operation;
    /// <summary>Creates an addition operation with two zero defaults.</summary>
    public VisualShaderNodeFloatOp() { InitializeDefault(0, 0f); InitializeDefault(1, 0f); }
    /// <summary>Gets or selects the binary scalar operation.</summary>
    /// <value>Add initially.</value>
    /// <exception cref="ArgumentOutOfRangeException">The operation is undefined.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public OperatorEnum Operator
    {
        get { ThrowIfDisposed(); return _operation; }
        set { ThrowIfDisposed(); if (!Enum.IsDefined(value) || value == OperatorEnum.EnumSize) throw new ArgumentOutOfRangeException(nameof(value)); if (_operation == value) return; _operation = value; EmitChanged(); }
    }
    internal override PortType[] Inputs => [PortType.Scalar, PortType.Scalar];
    internal override PortType[] Outputs => [PortType.Scalar];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => _operation switch
    {
        OperatorEnum.Add => $"({inputs[0]} + {inputs[1]})",
        OperatorEnum.Subtract => $"({inputs[0]} - {inputs[1]})",
        OperatorEnum.Multiply => $"({inputs[0]} * {inputs[1]})",
        OperatorEnum.Divide => $"({inputs[0]} / {inputs[1]})",
        OperatorEnum.Modulo => $"mod({inputs[0]}, {inputs[1]})",
        OperatorEnum.Power => $"pow({inputs[0]}, {inputs[1]})",
        OperatorEnum.Max => $"max({inputs[0]}, {inputs[1]})",
        OperatorEnum.Min => $"min({inputs[0]}, {inputs[1]})",
        OperatorEnum.Atan2 => $"atan({inputs[0]}, {inputs[1]})",
        OperatorEnum.Step => $"step({inputs[0]}, {inputs[1]})",
        _ => throw new InvalidOperationException("Invalid scalar operation.")
    };
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeFloatOp)target)._operation = _operation;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeFloatOp();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeFloatOp, OperatorEnum>(nameof(Operator), n => n.Operator, (n, v) => n.Operator = v, stored: true)]);
}
