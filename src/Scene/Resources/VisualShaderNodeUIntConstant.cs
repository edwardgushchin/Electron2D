namespace Electron2D;

/// <summary>Produces a copied uint constant in an imported visual shader.</summary>
public sealed class VisualShaderNodeUIntConstant : VisualShaderNodeConstant
{
    private uint _constant = 0u;
    /// <summary>Creates the node with its initial constant.</summary>
    public VisualShaderNodeUIntConstant() { }
    /// <summary>Gets or replaces the authored constant.</summary>
    /// <value>Initially 0u; numeric floating components must be finite.</value>
    /// <exception cref="ArgumentException">A numeric component is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public uint Constant
    {
        get { ThrowIfDisposed(); return _constant; }
        set { ThrowIfDisposed(); VisualShaderValues.Validate(value); if (VisualShaderValues.SameConstant(_constant, value)) return; _constant = value; EmitChanged(); }
    }
    internal override PortType[] Outputs => [PortType.ScalarUInt];
    internal override string Expression(int output, IReadOnlyList<string> inputs) => VisualShaderValues.Literal(_constant);
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeUIntConstant)target)._constant = _constant;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeUIntConstant();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeUIntConstant, uint>(nameof(Constant), n => n.Constant, (n, v) => n.Constant = v, stored: true)]);
}
