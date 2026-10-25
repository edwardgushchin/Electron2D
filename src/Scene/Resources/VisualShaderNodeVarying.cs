namespace Electron2D;

/// <summary>Defines the name and numeric port role of a graph varying access.</summary>
/// <remarks>The graph owns declarations; this node borrows a declaration by name at import. Authoring is cold and single-threaded.</remarks>
public abstract class VisualShaderNodeVarying : VisualShaderNode
{
    private string _varyingName = "[None]";
    private VisualShaderVaryingType _varyingType;
    /// <summary>Initializes an unselected floating-point varying access.</summary>
    protected VisualShaderNodeVarying() { }
    /// <summary>Gets or assigns the selected graph declaration name.</summary>
    /// <value>[None] initially. An unselected getter returns typed zero or matrix identity; an unselected setter writes nothing.</value>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposed.</exception>
    public string VaryingName
    { get { ThrowIfDisposed(); return _varyingName; } set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); if (_varyingName == value) return; _varyingName = value; EmitChanged(); } }
    /// <summary>Gets or selects the numeric port role.</summary>
    /// <value>Float initially. Import requires the selected declaration to have the same role. A changed setter role resets its input default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The role is undefined or the count sentinel.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposed.</exception>
    public VisualShaderVaryingType VaryingType
    {
        get { ThrowIfDisposed(); return _varyingType; }
        set
        {
            ThrowIfDisposed(); if ((uint)value >= (uint)VisualShaderVaryingType.Max) throw new ArgumentOutOfRangeException(nameof(value));
            if (_varyingType == value) return; _varyingType = value;
            if (this is VisualShaderNodeVaryingSetter) ReplaceDefinitionDefaults([VisualShaderValues.Entry(0, VisualShaderValues.Zero(ValueType))]);
            EmitChanged();
        }
    }
    internal PortType ValueType => (PortType)_varyingType;
    internal override void CopyDefinitionTo(VisualShaderNode target)
    { var copy = (VisualShaderNodeVarying)target; copy._varyingName = _varyingName; copy._varyingType = _varyingType; }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => new PropertyDescriptor[] {
        new PropertyDescriptor<VisualShaderNodeVarying, string>(nameof(VaryingName), n => n.VaryingName, (n,v) => n.VaryingName = v, stored:true),
        new PropertyDescriptor<VisualShaderNodeVarying, VisualShaderVaryingType>(nameof(VaryingType), n => n.VaryingType, (n,v) => n.VaryingType = v, stored:true)
    }.Concat(base.GetPropertyDescriptors());
}
