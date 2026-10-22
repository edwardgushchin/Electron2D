namespace Electron2D;

/// <summary>Applies one unary vector function in an imported canvas shader graph.</summary>
/// <remarks>Input constants/defaults must be finite; computed results retain native shader domain,
/// NaN/infinity and rounding rules. Graph authoring is cold and requires reimport before drawing changes.</remarks>
public sealed class VisualShaderNodeVectorFunc : VisualShaderNodeVectorBase
{
    /// <summary>The available unary vector functions with stable numeric identities.</summary>
    public enum FunctionEnum
    {
        /// <summary>Divides the vector by its length.</summary>
        Normalize = 0,
        /// <summary>Restricts components to the zero-to-one interval.</summary>
        Saturate = 1,
        /// <summary>Negates the input.</summary>
        Negate = 2,
        /// <summary>Divides one by the input.</summary>
        Reciprocal = 3,
        /// <summary>Computes absolute value.</summary>
        Abs = 4,
        /// <summary>Computes inverse cosine.</summary>
        Acos = 5,
        /// <summary>Computes inverse hyperbolic cosine.</summary>
        Acosh = 6,
        /// <summary>Computes inverse sine.</summary>
        Asin = 7,
        /// <summary>Computes inverse hyperbolic sine.</summary>
        Asinh = 8,
        /// <summary>Computes inverse tangent.</summary>
        Atan = 9,
        /// <summary>Computes inverse hyperbolic tangent.</summary>
        Atanh = 10,
        /// <summary>Rounds toward positive infinity.</summary>
        Ceil = 11,
        /// <summary>Computes cosine.</summary>
        Cos = 12,
        /// <summary>Computes hyperbolic cosine.</summary>
        Cosh = 13,
        /// <summary>Converts radians to degrees.</summary>
        Degrees = 14,
        /// <summary>Computes the natural exponential.</summary>
        Exp = 15,
        /// <summary>Raises two to the input power.</summary>
        Exp2 = 16,
        /// <summary>Rounds toward negative infinity.</summary>
        Floor = 17,
        /// <summary>Subtracts floor from the input.</summary>
        Fract = 18,
        /// <summary>Computes the reciprocal square root.</summary>
        InverseSqrt = 19,
        /// <summary>Computes the natural logarithm.</summary>
        Log = 20,
        /// <summary>Computes the base-two logarithm.</summary>
        Log2 = 21,
        /// <summary>Converts degrees to radians.</summary>
        Radians = 22,
        /// <summary>Rounds to a nearest integer; tie direction follows the shader backend.</summary>
        Round = 23,
        /// <summary>Rounds to a nearest integer with ties to even.</summary>
        RoundEven = 24,
        /// <summary>Returns negative one, zero or one.</summary>
        Sign = 25,
        /// <summary>Computes sine.</summary>
        Sin = 26,
        /// <summary>Computes hyperbolic sine.</summary>
        Sinh = 27,
        /// <summary>Computes the square root.</summary>
        Sqrt = 28,
        /// <summary>Computes tangent.</summary>
        Tan = 29,
        /// <summary>Computes hyperbolic tangent.</summary>
        Tanh = 30,
        /// <summary>Rounds toward zero.</summary>
        Trunc = 31,
        /// <summary>Subtracts the input from one.</summary>
        OneMinus = 32,
        /// <summary>The nonselectable function-count sentinel.</summary>
        Max = 33,
    }
    private FunctionEnum _function;
    /// <summary>Creates a normalize function with a zero vector default.</summary>
    public VisualShaderNodeVectorFunc() => InitializeDefault(0, Vector3.Zero);
    /// <summary>Gets or selects the unary function.</summary>
    /// <value>Normalize initially; Max is nonselectable.</value>
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
    internal override PortType[] Inputs => [VectorKind];
    internal override PortType[] Outputs => [VectorKind];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => VisualShaderFunctions.GenerateExpression(_function.ToString(), inputs[0]);
    internal override void CopyDefinitionTo(VisualShaderNode target)
    {
        base.CopyDefinitionTo(target);
        ((VisualShaderNodeVectorFunc)target)._function = _function;
    }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeVectorFunc();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeVectorFunc, FunctionEnum>(nameof(Function), n => n.Function, (n, v) => n.Function = v, stored: true)]);
}
