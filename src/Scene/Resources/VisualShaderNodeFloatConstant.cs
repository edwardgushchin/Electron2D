namespace Electron2D;

/// <summary>Produces a copied float constant in an imported visual shader.</summary>
public sealed class VisualShaderNodeFloatConstant : VisualShaderNodeConstant
{
    private float _constant = 0f;
    /// <summary>Creates the node with its initial constant.</summary>
    public VisualShaderNodeFloatConstant() { }
    /// <summary>Gets or replaces the authored constant.</summary>
    /// <value>Initially 0f; numeric floating components must be finite.</value>
    /// <exception cref="ArgumentException">A numeric component is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public float Constant
    {
        get { ThrowIfDisposed(); return _constant; }
        set { ThrowIfDisposed(); VisualShaderValues.Validate(value); if (VisualShaderValues.SameConstant(_constant, value)) return; _constant = value; EmitChanged(); }
    }
    internal override PortType[] Outputs => [PortType.Scalar];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => VisualShaderValues.Literal(_constant);
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeFloatConstant)target)._constant = _constant;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeFloatConstant();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeFloatConstant, float>(nameof(Constant), n => n.Constant, (n, v) => n.Constant = v, stored: true)]);
}
