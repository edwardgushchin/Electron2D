namespace Electron2D;

/// <summary>Produces a copied Vector2 constant in an imported visual shader.</summary>
public sealed class VisualShaderNodeVec2Constant : VisualShaderNodeConstant
{
    private Vector2 _constant = Vector2.Zero;
    /// <summary>Creates the node with its initial constant.</summary>
    public VisualShaderNodeVec2Constant() { }
    /// <summary>Gets or replaces the authored constant.</summary>
    /// <value>Initially Vector2.Zero; numeric floating components must be finite.</value>
    /// <exception cref="ArgumentException">A numeric component is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public Vector2 Constant
    {
        get { ThrowIfDisposed(); return _constant; }
        set { ThrowIfDisposed(); VisualShaderValues.Validate(value); if (VisualShaderValues.SameConstant(_constant, value)) return; _constant = value; EmitChanged(); }
    }
    internal override PortType[] Outputs => [PortType.Vector2];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => VisualShaderValues.Literal(_constant);
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeVec2Constant)target)._constant = _constant;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeVec2Constant();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeVec2Constant, Vector2>(nameof(Constant), n => n.Constant, (n, v) => n.Constant = v, stored: true)]);
}
