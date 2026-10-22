namespace Electron2D;

/// <summary>Blends two numeric RGB triples in an imported canvas shader graph.</summary>
/// <remarks>Outputs retain signed/HDR values. Overlay, SoftLight and HardLight select each component's
/// branch from the first input at 0.5. Dodge and Burn keep native division-by-zero semantics.</remarks>
public sealed class VisualShaderNodeColorOp : VisualShaderNode
{
    /// <summary>The component-wise color operation.</summary>
    public enum OperatorEnum
    {
        /// <summary>Computes one minus the product of the complementary inputs.</summary>
        Screen = 0,
        /// <summary>Computes absolute component differences.</summary>
        Difference = 1,
        /// <summary>Selects the smaller components.</summary>
        Darken = 2,
        /// <summary>Selects the larger components.</summary>
        Lighten = 3,
        /// <summary>Uses doubled product below 0.5 in the first input and complementary product otherwise.</summary>
        Overlay = 4,
        /// <summary>Divides the first input by one minus the second.</summary>
        Dodge = 5,
        /// <summary>Subtracts from one the complement of the first input divided by the second.</summary>
        Burn = 6,
        /// <summary>Applies the half-offset piecewise blend selected by the first input.</summary>
        SoftLight = 7,
        /// <summary>Applies the doubled piecewise blend selected by the first input.</summary>
        HardLight = 8,
        /// <summary>The nonselectable operator count.</summary>
        Max = 9,
    }
    private OperatorEnum _operator;
    /// <summary>Creates Screen with two zero Vector3 defaults.</summary>
    public VisualShaderNodeColorOp() { InitializeDefault(0, Vector3.Zero); InitializeDefault(1, Vector3.Zero); }
    /// <summary>Gets or selects the color operation.</summary>
    /// <value>Screen initially. Input values and default absence survive selection changes.</value>
    /// <remarks>An unchanged selection is quiet; edits require reimport before drawing changes.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The selector is undefined or Max.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public OperatorEnum Operator
    {
        get { ThrowIfDisposed(); return _operator; }
        set { ThrowIfDisposed(); if (!Enum.IsDefined(value) || value == OperatorEnum.Max) throw new ArgumentOutOfRangeException(nameof(value)); if (_operator == value) return; _operator = value; EmitChanged(); }
    }
    internal override PortType[] Inputs => [PortType.Vector3, PortType.Vector3];
    internal override PortType[] Outputs => [PortType.Vector3];
    internal override string Expression(int output, IReadOnlyList<string> inputs)
    {
        var a = "(" + inputs[0] + ")"; var b = "(" + inputs[1] + ")";
        string Piece(string component)
        {
            var x = a + "." + component; var y = b + "." + component;
            var low = _operator == OperatorEnum.SoftLight ? x + " * (" + y + " + .5)" : x + " * (2.0 * " + y + ")";
            var high = _operator switch
            {
                OperatorEnum.SoftLight => "1.0 - (1.0 - " + x + ") * (1.0 - (" + y + " - .5))",
                OperatorEnum.HardLight => "1.0 - (1.0 - " + x + ") * (1.0 - 2.0 * (" + y + " - .5))",
                _ => "1.0 - 2.0 * (1.0 - " + y + ") * (1.0 - " + x + ")"
            };
            return "(" + x + " < .5 ? " + low + " : " + high + ")";
        }
        return _operator switch
        {
            OperatorEnum.Screen => "vec3(1.0) - (vec3(1.0) - " + a + ") * (vec3(1.0) - " + b + ")",
            OperatorEnum.Difference => "abs(" + a + " - " + b + ")",
            OperatorEnum.Darken => "min(" + a + ", " + b + ")",
            OperatorEnum.Lighten => "max(" + a + ", " + b + ")",
            OperatorEnum.Dodge => a + " / (vec3(1.0) - " + b + ")",
            OperatorEnum.Burn => "vec3(1.0) - (vec3(1.0) - " + a + ") / " + b,
            _ => "vec3(" + Piece("r") + ", " + Piece("g") + ", " + Piece("b") + ")"
        };
    }
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeColorOp)target)._operator = _operator;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeColorOp();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeColorOp, OperatorEnum>(nameof(Operator), n => n.Operator, (n, v) => n.Operator = v, stored: true)]);
}
