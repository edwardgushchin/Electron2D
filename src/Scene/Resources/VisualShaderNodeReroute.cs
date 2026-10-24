namespace Electron2D;

/// <summary>Forwards one typed graph value through a connection chain or branch.</summary>
/// <remarks>Validated graph connections select the port role. Disconnecting retains that role and its
/// authored default. Sampled images retain their original symbol and image shape.</remarks>
public sealed class VisualShaderNodeReroute : VisualShaderNode
{
    private PortType _portType;
    /// <summary>Creates an unconnected scalar reroute with a zero default.</summary>
    public VisualShaderNodeReroute() => ReplaceDefinitionDefaults([VisualShaderValues.Entry(0, 0f)]);
    /// <summary>Gets the current input and output role selected by graph connections.</summary>
    /// <returns>Scalar initially; validated connections propagate their role through downstream reroutes.</returns>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public PortType GetPortType() { ThrowIfDisposed(); return _portType; }
    internal override PortType[] Inputs => [_portType];
    internal override PortType[] Outputs => [_portType];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => inputs[0];
    internal bool SetPortType(PortType type)
    {
        ThrowIfDisposed();
        if (!Enum.IsDefined(type) || type == PortType.Max) throw new ArgumentOutOfRangeException(nameof(type));
        if (_portType == type) return false;
        _portType = type;
        ReplaceDefinitionDefaults(type == PortType.Sampler ? [] : [VisualShaderValues.Entry(0, VisualShaderValues.Zero(type))]);
        return true;
    }
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeReroute)target)._portType = _portType;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeReroute();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        new PropertyDescriptor[] { new PropertyDescriptor<VisualShaderNodeReroute, PortType>("PortType", n => n.GetPortType(), (n, v) => { if (n.SetPortType(v)) n.EmitChanged(); }, stored: true) }
        .Concat(base.GetPropertyDescriptors());
}
