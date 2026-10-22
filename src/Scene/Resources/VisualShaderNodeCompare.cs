namespace Electron2D;

/// <summary>Compares two numeric graph inputs and produces a Boolean scalar.</summary>
/// <remarks>Floating equality uses a strict absolute tolerance. Vector comparisons reduce exact component results
/// with All or Any. Boolean and matrix ordering functions produce false. The tolerance default remains stored while inactive.</remarks>
public sealed class VisualShaderNodeCompare : VisualShaderNode
{
    /// <summary>The comparison performed on each input component.</summary>
    public enum FunctionEnum
    {
        /// <summary>Tests equality, using tolerance only for floating scalars.</summary>
        Equal = 0,
        /// <summary>Negates scalar tolerance equality or tests exact component inequality.</summary>
        NotEqual = 1,
        /// <summary>Tests whether the first value is larger.</summary>
        GreaterThan = 2,
        /// <summary>Tests whether the first value is at least the second.</summary>
        GreaterThanEqual = 3,
        /// <summary>Tests whether the first value is smaller.</summary>
        LessThan = 4,
        /// <summary>Tests whether the first value is at most the second.</summary>
        LessThanEqual = 5,
        /// <summary>The nonselectable function-count sentinel.</summary>
        Max = 6,
    }
    /// <summary>The reduction of a vector's component comparisons.</summary>
    public enum ConditionEnum
    {
        /// <summary>Requires every component comparison to succeed.</summary>
        All = 0,
        /// <summary>Requires at least one component comparison to succeed.</summary>
        Any = 1,
        /// <summary>The nonselectable condition-count sentinel.</summary>
        Max = 2,
    }
    private VisualShaderValueType _type;
    private FunctionEnum _function;
    private ConditionEnum _condition;
    /// <summary>Creates a floating equality definition with zero operands and a 0.00001 tolerance.</summary>
    public VisualShaderNodeCompare()
    { InitializeDefault(0, 0f); InitializeDefault(1, 0f); InitializeDefault(2, .00001f); }
    /// <summary>Gets or selects the compared value type, converting both operand defaults atomically.</summary>
    /// <value>Float initially. A changed type recreates absent operands and preserves the dormant tolerance.</value>
    /// <remarks>Vector expansion repeats the last component. Boolean conversions use the target's initial default.
    /// Matrix layout changes use identity operands; conversions from matrices use the target defaults.
    /// Repeated selection is quiet; Changed runs after the new type and defaults commit.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The selection is undefined or the count sentinel.</exception>
    /// <exception cref="OverflowException">An operand cannot be represented by the target integer type; state is unchanged.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public VisualShaderValueType Type
    {
        get { ThrowIfDisposed(); return _type; }
        set
        {
            ThrowIfDisposed();
            if (!Enum.IsDefined(value) || value == VisualShaderValueType.Max) throw new ArgumentOutOfRangeException(nameof(value));
            if (_type == value) return;
            var zero = value == VisualShaderValueType.Vector4 ? new Vector4(0, 0, 0, 1) : VisualShaderValues.Zero((PortType)value);
            ChangeDefaultTypes([zero, zero]); _type = value; EmitChanged();
        }
    }
    /// <summary>Gets or selects the comparison function without changing stored defaults.</summary>
    /// <value>Equal initially. Only Float Equal/NotEqual expose the third tolerance input.</value>
    /// <remarks>Invalid values leave state unchanged. Unchanged selection is quiet; observers run after commit.</remarks>
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
    /// <summary>Gets or selects the vector reduction, retained independently of the input type.</summary>
    /// <value>All initially; scalar comparisons ignore this selection.</value>
    /// <remarks>Invalid values leave state unchanged. Unchanged selection is quiet; observers run after commit.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The condition is undefined or the count sentinel.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public ConditionEnum Condition
    {
        get { ThrowIfDisposed(); return _condition; }
        set
        {
            ThrowIfDisposed();
            if (!Enum.IsDefined(value) || value == ConditionEnum.Max) throw new ArgumentOutOfRangeException(nameof(value));
            if (_condition == value) return; _condition = value; EmitChanged();
        }
    }
    internal override PortType[] DefaultInputs => [(PortType)_type, (PortType)_type, PortType.Scalar];
    internal override PortType[] Inputs => _type == VisualShaderValueType.Float && _function is FunctionEnum.Equal or FunctionEnum.NotEqual
        ? DefaultInputs : [(PortType)_type, (PortType)_type];
    internal override PortType[] Outputs => [PortType.Boolean];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs)
    {
        if (_type == VisualShaderValueType.Float && _function is FunctionEnum.Equal or FunctionEnum.NotEqual)
        {
            var equal = $"(abs({inputs[0]} - {inputs[1]}) < {inputs[2]})";
            return _function == FunctionEnum.Equal ? equal : $"(!{equal})";
        }
        if (_type is VisualShaderValueType.Boolean or VisualShaderValueType.Matrix && _function > FunctionEnum.NotEqual) return "false";
        if (_type is VisualShaderValueType.Vector2 or VisualShaderValueType.Vector3 or VisualShaderValueType.Vector4)
        {
            var function = _function switch { FunctionEnum.Equal => "equal", FunctionEnum.NotEqual => "notEqual", FunctionEnum.GreaterThan => "greaterThan", FunctionEnum.GreaterThanEqual => "greaterThanEqual", FunctionEnum.LessThan => "lessThan", _ => "lessThanEqual" };
            return $"{(_condition == ConditionEnum.All ? "all" : "any")}({function}({inputs[0]}, {inputs[1]}))";
        }
        var op = _function switch { FunctionEnum.Equal => "==", FunctionEnum.NotEqual => "!=", FunctionEnum.GreaterThan => ">", FunctionEnum.GreaterThanEqual => ">=", FunctionEnum.LessThan => "<", _ => "<=" };
        return $"({inputs[0]} {op} {inputs[1]})";
    }
    internal override void CopyDefinitionTo(VisualShaderNode target)
    { var copy = (VisualShaderNodeCompare)target; copy._type = _type; copy._function = _function; copy._condition = _condition; }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeCompare();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => new PropertyDescriptor[]
    {
        new PropertyDescriptor<VisualShaderNodeCompare, VisualShaderValueType>(nameof(Type), n => n.Type, (n, v) => n.Type = v, stored: true),
        new PropertyDescriptor<VisualShaderNodeCompare, FunctionEnum>(nameof(Function), n => n.Function, (n, v) => n.Function = v, stored: true),
        new PropertyDescriptor<VisualShaderNodeCompare, ConditionEnum>(nameof(Condition), n => n.Condition, (n, v) => n.Condition = v, stored: true),
    }.Concat(base.GetPropertyDescriptors());
}
