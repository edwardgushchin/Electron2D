namespace Electron2D;

/// <summary>Produces a copied four-by-four numeric matrix constant in an imported visual shader.</summary>
public sealed class VisualShaderNodeMatrixConstant : VisualShaderNodeConstant
{
    private System.Numerics.Matrix4x4 _constant = System.Numerics.Matrix4x4.Identity;
    /// <summary>Creates the node with its initial constant.</summary>
    public VisualShaderNodeMatrixConstant() { }
    /// <summary>Gets or replaces the authored constant.</summary>
    /// <value>Initially identity; all sixteen numeric components must be finite.</value>
    /// <exception cref="ArgumentException">A numeric component is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public System.Numerics.Matrix4x4 Constant
    {
        get { ThrowIfDisposed(); return _constant; }
        set { ThrowIfDisposed(); VisualShaderValues.Validate(value); if (VisualShaderValues.SameConstant(_constant, value)) return; _constant = value; EmitChanged(); }
    }
    internal override PortType[] Outputs => [PortType.Matrix];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => VisualShaderValues.Literal(_constant);
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeMatrixConstant)target)._constant = _constant;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeMatrixConstant();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeMatrixConstant, System.Numerics.Matrix4x4>(nameof(Constant), n => n.Constant, (n, v) => n.Constant = v, stored: true)]);
}
