namespace Electron2D;

/// <summary>Multiplies numeric matrix and vector data in either order with an explicit homogeneous component.</summary>
/// <remarks>Matrix4x4 is numeric shader data. Multiplication retains native precision and the selected operand order.</remarks>
public sealed class VisualShaderNodeMatrixVecMult : VisualShaderNode
{
    /// <summary>The selected native numeric operation.</summary>
    public enum OperatorEnum
    {
        /// <summary>Multiplies the matrix by the column (x,y,z,1) and returns xyz.</summary>
        MatrixTimesVector = 0,
        /// <summary>Multiplies the row (x,y,z,1) by the matrix and returns xyz.</summary>
        VectorTimesMatrix = 1,
        /// <summary>Multiplies the matrix by the column (x,y,z,0) and returns xyz.</summary>
        MatrixTimesDirection = 2,
        /// <summary>Multiplies the row (x,y,z,0) by the matrix and returns xyz.</summary>
        DirectionTimesMatrix = 3,
        /// <summary>The nonselectable operator-count sentinel.</summary>
        Max = 4,
    }
    private OperatorEnum _selection;
    /// <summary>Creates the definition with explicit identity matrix and zero vector defaults.</summary>
    public VisualShaderNodeMatrixVecMult() { InitializeDefault(0, System.Numerics.Matrix4x4.Identity); InitializeDefault(1, Vector3.Zero); }
    /// <summary>Gets or selects the native operation without changing stored defaults.</summary>
    /// <value>MatrixTimesVector initially.</value>
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
    internal override PortType[] Inputs => [PortType.Matrix, PortType.Vector3];
    internal override PortType[] Outputs => [PortType.Vector3];
    internal override string Expression(int output, IReadOnlyList<string> inputs) => _selection switch
    {
        OperatorEnum.MatrixTimesVector => $"({inputs[0]} * vec4({inputs[1]}, 1.0)).xyz",
        OperatorEnum.VectorTimesMatrix => $"(vec4({inputs[1]}, 1.0) * {inputs[0]}).xyz",
        OperatorEnum.MatrixTimesDirection => $"({inputs[0]} * vec4({inputs[1]}, 0.0)).xyz",
        _ => $"(vec4({inputs[1]}, 0.0) * {inputs[0]}).xyz"
    };
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeMatrixVecMult)target)._selection = _selection;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeMatrixVecMult();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeMatrixVecMult, OperatorEnum>(nameof(Operator), n => n.Operator, (n, v) => n.Operator = v, stored: true)]);
}
