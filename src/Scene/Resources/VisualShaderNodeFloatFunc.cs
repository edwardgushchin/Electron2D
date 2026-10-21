namespace Electron2D;

/// <summary>Applies one unary scalar function in an imported canvas shader graph.</summary>
/// <remarks>Input constants/defaults must be finite; computed results retain native shader domain,
/// NaN/infinity and rounding rules. Graph authoring is cold and requires reimport before drawing changes.</remarks>
public sealed class VisualShaderNodeFloatFunc : VisualShaderNode
{
    /// <summary>The available unary scalar functions with stable numeric identities.</summary>
    public enum FunctionEnum
    {
        /// <summary>Computes sine.</summary>
        Sin = 0,
        /// <summary>Computes cosine.</summary>
        Cos = 1,
        /// <summary>Computes tangent.</summary>
        Tan = 2,
        /// <summary>Computes inverse sine.</summary>
        Asin = 3,
        /// <summary>Computes inverse cosine.</summary>
        Acos = 4,
        /// <summary>Computes inverse tangent.</summary>
        Atan = 5,
        /// <summary>Computes hyperbolic sine.</summary>
        Sinh = 6,
        /// <summary>Computes hyperbolic cosine.</summary>
        Cosh = 7,
        /// <summary>Computes hyperbolic tangent.</summary>
        Tanh = 8,
        /// <summary>Computes the natural logarithm.</summary>
        Log = 9,
        /// <summary>Computes the natural exponential.</summary>
        Exp = 10,
        /// <summary>Computes the square root.</summary>
        Sqrt = 11,
        /// <summary>Computes absolute value.</summary>
        Abs = 12,
        /// <summary>Returns negative one, zero or one.</summary>
        Sign = 13,
        /// <summary>Rounds toward negative infinity.</summary>
        Floor = 14,
        /// <summary>Rounds to a nearest integer; tie direction follows the shader backend.</summary>
        Round = 15,
        /// <summary>Rounds toward positive infinity.</summary>
        Ceil = 16,
        /// <summary>Subtracts floor from the input.</summary>
        Fract = 17,
        /// <summary>Restricts components to the zero-to-one interval.</summary>
        Saturate = 18,
        /// <summary>Negates the input.</summary>
        Negate = 19,
        /// <summary>Computes inverse hyperbolic cosine.</summary>
        Acosh = 20,
        /// <summary>Computes inverse hyperbolic sine.</summary>
        Asinh = 21,
        /// <summary>Computes inverse hyperbolic tangent.</summary>
        Atanh = 22,
        /// <summary>Converts radians to degrees.</summary>
        Degrees = 23,
        /// <summary>Raises two to the input power.</summary>
        Exp2 = 24,
        /// <summary>Computes the reciprocal square root.</summary>
        InverseSqrt = 25,
        /// <summary>Computes the base-two logarithm.</summary>
        Log2 = 26,
        /// <summary>Converts degrees to radians.</summary>
        Radians = 27,
        /// <summary>Divides one by the input.</summary>
        Reciprocal = 28,
        /// <summary>Rounds to a nearest integer with ties to even.</summary>
        RoundEven = 29,
        /// <summary>Rounds toward zero.</summary>
        Trunc = 30,
        /// <summary>Subtracts the input from one.</summary>
        OneMinus = 31,
        /// <summary>The nonselectable function-count sentinel.</summary>
        Max = 32,
    }
    private FunctionEnum _function;
    /// <summary>Creates a sin function with a zero scalar default.</summary>
    public VisualShaderNodeFloatFunc() => InitializeDefault(0, 0f);
    /// <summary>Gets or selects the unary function.</summary>
    /// <value>Sin initially; Max is nonselectable.</value>
    /// <remarks>An unchanged selection emits no Changed event.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The function is undefined or the Max sentinel.</exception>
    /// <exception cref="ObjectDisposedException">This definition is disposed.</exception>
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
    internal override PortType[] Inputs => [PortType.Scalar];
    internal override PortType[] Outputs => [PortType.Scalar];
    internal override string Expression(int output, IReadOnlyList<string> inputs) => VisualShaderFunctions.Expression(_function.ToString(), inputs[0]);
    internal override void CopyDefinitionTo(VisualShaderNode target)
    {
        ((VisualShaderNodeFloatFunc)target)._function = _function;
    }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeFloatFunc();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeFloatFunc, FunctionEnum>(nameof(Function), n => n.Function, (n, v) => n.Function = v, stored: true)]);
}
