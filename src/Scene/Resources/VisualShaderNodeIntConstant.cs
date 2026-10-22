namespace Electron2D;

/// <summary>Produces a copied int constant in an imported visual shader.</summary>
public sealed class VisualShaderNodeIntConstant : VisualShaderNodeConstant
{
    private int _constant = 0;
    /// <summary>Creates the node with its initial constant.</summary>
    public VisualShaderNodeIntConstant() { }
    /// <summary>Gets or replaces the authored constant.</summary>
    /// <value>Initially 0; numeric floating components must be finite.</value>
    /// <exception cref="ArgumentException">A numeric component is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public int Constant
    {
        get { ThrowIfDisposed(); return _constant; }
        set { ThrowIfDisposed(); VisualShaderValues.Validate(value); if (VisualShaderValues.SameConstant(_constant, value)) return; _constant = value; EmitChanged(); }
    }
    internal override PortType[] Outputs => [PortType.ScalarInt];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => VisualShaderValues.Literal(_constant);
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeIntConstant)target)._constant = _constant;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeIntConstant();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeIntConstant, int>(nameof(Constant), n => n.Constant, (n, v) => n.Constant = v, stored: true)]);
}
