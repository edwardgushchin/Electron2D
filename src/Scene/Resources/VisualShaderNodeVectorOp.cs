namespace Electron2D;

/// <summary>Applies one binary component-wise vector operation to two graph inputs.</summary>
/// <remarks>Arithmetic executes in the imported shader, including its native NaN/infinity rules.</remarks>
public sealed class VisualShaderNodeVectorOp : VisualShaderNodeVectorBase
{
    /// <summary>The binary vector operation.</summary>
    public enum Operator
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
        /// <summary>Computes the three-component cross product.</summary>
        Cross = 8,
        /// <summary>Reflects the incident first vector around the second vector.</summary>
        Reflect = 10,
        /// <summary>Computes the two-argument arctangent of the first and second inputs.</summary>
        Atan2 = 9,
        /// <summary>Returns zero when the second input is below the first, otherwise one.</summary>
        Step = 11,
        /// <summary>The nonselectable operation-count sentinel.</summary>
        EnumSize = 12,
    }
    private Operator _operation;
    /// <summary>Creates an addition operation with two zero Vector3 defaults.</summary>
    public VisualShaderNodeVectorOp() { InitializeDefault(0, Vector3.Zero); InitializeDefault(1, Vector3.Zero); }
    /// <summary>Gets or selects the binary vector operation.</summary>
    /// <value>Add initially.</value>
    /// <exception cref="ArgumentOutOfRangeException">The operation is undefined.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public Operator Operation
    {
        get { ThrowIfDisposed(); return _operation; }
        set { ThrowIfDisposed(); if (!Enum.IsDefined(value) || value == Operator.EnumSize) throw new ArgumentOutOfRangeException(nameof(value)); if (value == Operator.Cross && OperationType != OpType.Vector3) throw new NotSupportedException("A cross product requires three components."); if (_operation == value) return; _operation = value; EmitChanged(); }
    }
    internal override PortType[] Inputs => [VectorKind, VectorKind];
    internal override PortType[] Outputs => [VectorKind];
    internal override string Expression(int output, IReadOnlyList<string> inputs) => _operation switch
    {
        Operator.Add => $"({inputs[0]} + {inputs[1]})",
        Operator.Subtract => $"({inputs[0]} - {inputs[1]})",
        Operator.Multiply => $"({inputs[0]} * {inputs[1]})",
        Operator.Divide => $"({inputs[0]} / {inputs[1]})",
        Operator.Modulo => $"mod({inputs[0]}, {inputs[1]})",
        Operator.Power => $"pow({inputs[0]}, {inputs[1]})",
        Operator.Max => $"max({inputs[0]}, {inputs[1]})",
        Operator.Min => $"min({inputs[0]}, {inputs[1]})",
        Operator.Cross => $"cross({inputs[0]}, {inputs[1]})",
        Operator.Reflect => $"reflect({inputs[0]}, {inputs[1]})",
        Operator.Atan2 => $"atan({inputs[0]}, {inputs[1]})",
        Operator.Step => $"step({inputs[0]}, {inputs[1]})",
        _ => throw new InvalidOperationException("Invalid scalar operation.")
    };
    internal override void CopyDefinitionTo(VisualShaderNode target) { base.CopyDefinitionTo(target); ((VisualShaderNodeVectorOp)target)._operation = _operation; }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeVectorOp();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeVectorOp, Operator>(nameof(Operation), n => n.Operation, (n, v) => n.Operation = v, stored: true)]);
}
