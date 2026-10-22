namespace Electron2D;

/// <summary>Computes screen-space rates of change in an imported fragment graph.</summary>
/// <remarks>Derivatives use neighboring fragment invocations. Their native precision and behavior at
/// discontinuities are backend-defined. Authored input defaults are finite; graph edits require reimport.</remarks>
public sealed class VisualShaderNodeDerivativeFunc : VisualShaderNode
{
    /// <summary>The component-wise screen-space derivative operation.</summary>
    public enum FunctionEnum
    {
        /// <summary>Computes absolute X derivative plus absolute Y derivative.</summary>
        Sum = 0,
        /// <summary>Computes change along the framebuffer X direction.</summary>
        X = 1,
        /// <summary>Computes change along the framebuffer Y direction.</summary>
        Y = 2,
        /// <summary>The nonselectable function-count sentinel.</summary>
        Max = 3,
    }
    /// <summary>The neighboring invocation policy requested from the fragment backend.</summary>
    public enum PrecisionEnum
    {
        /// <summary>Uses the backend's default derivative policy.</summary>
        None = 0,
        /// <summary>Uses the coarse derivative operation.</summary>
        Coarse = 1,
        /// <summary>Uses the fine derivative operation.</summary>
        Fine = 2,
        /// <summary>The nonselectable precision-count sentinel.</summary>
        Max = 3,
    }
    private VisualShaderFloatOpType _operationType;
    private FunctionEnum _function;
    private PrecisionEnum _precision;
    /// <summary>Creates a scalar Sum operation with default precision and an explicit zero input.</summary>
    public VisualShaderNodeDerivativeFunc() => InitializeDefault(0, 0f);
    /// <summary>Gets or selects the floating component layout, converting the authored input atomically.</summary>
    /// <value>Scalar initially. Vector expansion repeats the previous last component. A changed layout
    /// recreates a missing default as zero, or (0,0,0,1) for four components.</value>
    /// <remarks>Repeated selection is quiet. The layout and default commit before observers run.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The layout is undefined or the count sentinel.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public VisualShaderFloatOpType OpType
    {
        get { ThrowIfDisposed(); return _operationType; }
        set
        {
            ThrowIfDisposed();
            if (!Enum.IsDefined(value) || value == VisualShaderFloatOpType.Max) throw new ArgumentOutOfRangeException(nameof(value));
            if (_operationType == value) return;
            ChangeDefaultTypes([value == VisualShaderFloatOpType.Vector4 ? new Vector4(0, 0, 0, 1) : VisualShaderValues.Zero(Kind(value))]);
            _operationType = value; EmitChanged();
        }
    }
    /// <summary>Gets or selects the derivative operation.</summary>
    /// <value>Sum initially. Repeated selection is quiet; input defaults are retained.</value>
    /// <exception cref="ArgumentOutOfRangeException">The function is undefined or the count sentinel.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public FunctionEnum Function
    {
        get { ThrowIfDisposed(); return _function; }
        set { ThrowIfDisposed(); if (!Enum.IsDefined(value) || value == FunctionEnum.Max) throw new ArgumentOutOfRangeException(nameof(value)); if (_function == value) return; _function = value; EmitChanged(); }
    }
    /// <summary>Gets or selects the native derivative precision.</summary>
    /// <value>None initially. Repeated selection is quiet; input defaults are retained.</value>
    /// <exception cref="ArgumentOutOfRangeException">The precision is undefined or the count sentinel.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public PrecisionEnum Precision
    {
        get { ThrowIfDisposed(); return _precision; }
        set { ThrowIfDisposed(); if (!Enum.IsDefined(value) || value == PrecisionEnum.Max) throw new ArgumentOutOfRangeException(nameof(value)); if (_precision == value) return; _precision = value; EmitChanged(); }
    }
    private static PortType Kind(VisualShaderFloatOpType type) => type switch
    { VisualShaderFloatOpType.Scalar => PortType.Scalar, VisualShaderFloatOpType.Vector2 => PortType.Vector2, VisualShaderFloatOpType.Vector3 => PortType.Vector3, _ => PortType.Vector4 };
    internal override PortType[] Inputs => [Kind(_operationType)];
    internal override PortType[] Outputs => Inputs;
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) =>
        $"{(_function == FunctionEnum.Sum ? "fwidth" : _function == FunctionEnum.X ? "dFdx" : "dFdy")}{(_precision == PrecisionEnum.Coarse ? "Coarse" : _precision == PrecisionEnum.Fine ? "Fine" : "")}({inputs[0]})";
    internal override void CopyDefinitionTo(VisualShaderNode target)
    { var copy = (VisualShaderNodeDerivativeFunc)target; copy._operationType = _operationType; copy._function = _function; copy._precision = _precision; }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeDerivativeFunc();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        new PropertyDescriptor[] {new PropertyDescriptor<VisualShaderNodeDerivativeFunc, VisualShaderFloatOpType>(nameof(OpType), n => n.OpType, (n,v) => n.OpType = v, stored:true),
         new PropertyDescriptor<VisualShaderNodeDerivativeFunc, FunctionEnum>(nameof(Function), n => n.Function, (n,v) => n.Function = v, stored:true),
         new PropertyDescriptor<VisualShaderNodeDerivativeFunc, PrecisionEnum>(nameof(Precision), n => n.Precision, (n,v) => n.Precision = v, stored:true) }.Concat(base.GetPropertyDescriptors());
}
