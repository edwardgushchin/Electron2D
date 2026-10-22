namespace Electron2D;

/// <summary>Reads a named parameter declared in either canvas graph stage.</summary>
/// <remarks>Resolution is local to the containing graph, including when definitions are borrowed by several graphs.
/// Color references expose RGB and alpha separately; image references carry the containing declaration's sampler shape. The initial [None] selection produces scalar zero.
/// Renaming or removing a declaration may invalidate stored connections; import rejects them before replacing output.</remarks>
public sealed class VisualShaderNodeParameterRef : VisualShaderNode
{
    private string _parameterName = "[None]";
    /// <summary>Creates an unselected parameter reference.</summary>
    public VisualShaderNodeParameterRef() { }
    /// <summary>Gets or assigns the referenced case-sensitive parameter name.</summary>
    /// <value>[None] initially. Each assignment emits Changed after commitment.</value>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public string ParameterName
    { get { ThrowIfDisposed(); return _parameterName; } set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); _parameterName = value; EmitChanged(); } }
    internal override PortType[] Inputs => [];
    internal override PortType[] Outputs => [PortType.Scalar];
    internal override string Expression(int output, IReadOnlyList<string> inputs) => _parameterName == "[None]" ? "0.0" : _parameterName;
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeParameterRef)target)._parameterName = _parameterName;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeParameterRef();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeParameterRef, string>(nameof(ParameterName), n => n.ParameterName, (n, v) => n.ParameterName = v, stored: true)]);
}
