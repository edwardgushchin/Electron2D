namespace Electron2D;

/// <summary>Produces a copied Vector3 constant in an imported visual shader.</summary>
public sealed class VisualShaderNodeVec3Constant : VisualShaderNodeConstant
{
    private Vector3 _constant = Vector3.Zero;
    /// <summary>Creates the node with its initial constant.</summary>
    public VisualShaderNodeVec3Constant() { }
    /// <summary>Gets or replaces the authored constant.</summary>
    /// <value>Initially Vector3.Zero; numeric floating components must be finite.</value>
    /// <exception cref="ArgumentException">A numeric component is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public Vector3 Constant
    {
        get { ThrowIfDisposed(); return _constant; }
        set { ThrowIfDisposed(); VisualShaderValues.Validate(value); if (VisualShaderValues.SameConstant(_constant, value)) return; _constant = value; EmitChanged(); }
    }
    internal override PortType[] Outputs => [PortType.Vector3];
    internal override string Expression(int output, IReadOnlyList<string> inputs) => VisualShaderValues.Literal(_constant);
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeVec3Constant)target)._constant = _constant;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeVec3Constant();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeVec3Constant, Vector3>(nameof(Constant), n => n.Constant, (n, v) => n.Constant = v, stored: true)]);
}
