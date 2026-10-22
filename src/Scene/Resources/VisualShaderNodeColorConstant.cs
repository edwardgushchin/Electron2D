namespace Electron2D;

/// <summary>Produces a copied Color constant in an imported visual shader.</summary>
public sealed class VisualShaderNodeColorConstant : VisualShaderNodeConstant
{
    private Color _constant = Colors.White;
    /// <summary>Creates the node with its initial constant.</summary>
    public VisualShaderNodeColorConstant() { }
    /// <summary>Gets or replaces the authored constant.</summary>
    /// <value>Initially Colors.White; numeric floating components must be finite.</value>
    /// <exception cref="ArgumentException">A numeric component is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public Color Constant
    {
        get { ThrowIfDisposed(); return _constant; }
        set { ThrowIfDisposed(); VisualShaderValues.Validate(value); if (VisualShaderValues.SameConstant(_constant, value)) return; _constant = value; EmitChanged(); }
    }
    internal override PortType[] Outputs => [PortType.Vector4];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => VisualShaderValues.Literal(_constant);
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeColorConstant)target)._constant = _constant;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeColorConstant();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeColorConstant, Color>(nameof(Constant), n => n.Constant, (n, v) => n.Constant = v, stored: true)]);
}
