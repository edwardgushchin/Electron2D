namespace Electron2D;

/// <summary>Produces a copied Vector4 constant in an imported visual shader.</summary>
public sealed class VisualShaderNodeVec4Constant : VisualShaderNodeConstant
{
    private Vector4 _constant = new(0, 0, 0, 1);
    /// <summary>Creates the node with its initial constant.</summary>
    public VisualShaderNodeVec4Constant() { }
    /// <summary>Gets or replaces the authored constant.</summary>
    /// <value>Initially (0, 0, 0, 1); numeric floating components must be finite.</value>
    /// <exception cref="ArgumentException">A numeric component is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public Vector4 Constant
    {
        get { ThrowIfDisposed(); return _constant; }
        set { ThrowIfDisposed(); VisualShaderValues.Validate(value); if (VisualShaderValues.SameConstant(_constant, value)) return; _constant = value; EmitChanged(); }
    }
    internal override PortType[] Outputs => [PortType.Vector4];
    internal override string Expression(int output, IReadOnlyList<string> inputs) => VisualShaderValues.Literal(_constant);
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeVec4Constant)target)._constant = _constant;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeVec4Constant();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeVec4Constant, Vector4>(nameof(Constant), n => n.Constant, (n, v) => n.Constant = v, stored: true)]);
}
