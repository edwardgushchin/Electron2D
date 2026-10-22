namespace Electron2D;

/// <summary>Produces a copied bool constant in an imported visual shader.</summary>
public sealed class VisualShaderNodeBooleanConstant : VisualShaderNodeConstant
{
    private bool _constant = false;
    /// <summary>Creates the node with its initial constant.</summary>
    public VisualShaderNodeBooleanConstant() { }
    /// <summary>Gets or replaces the authored constant.</summary>
    /// <value>Initially false; numeric floating components must be finite.</value>
    /// <exception cref="ArgumentException">A numeric component is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public bool Constant
    {
        get { ThrowIfDisposed(); return _constant; }
        set { ThrowIfDisposed(); VisualShaderValues.Validate(value); if (VisualShaderValues.SameConstant(_constant, value)) return; _constant = value; EmitChanged(); }
    }
    internal override PortType[] Outputs => [PortType.Boolean];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => VisualShaderValues.Literal(_constant);
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeBooleanConstant)target)._constant = _constant;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeBooleanConstant();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeBooleanConstant, bool>(nameof(Constant), n => n.Constant, (n, v) => n.Constant = v, stored: true)]);
}
