namespace Electron2D;

/// <summary>Declares a named typed value in an offline-compiled visual shader.</summary>
/// <remarks>Definitions are borrowed by graphs. Name edits and explicit slot assignments emit Changed after
/// commitment. AddNode makes names unique; import validates later edits, qualifiers and the complete symbol table.</remarks>
public abstract class VisualShaderNodeParameter : VisualShaderNode
{
    /// <summary>The storage owner of a graph parameter.</summary>
    public enum QualifierEnum
    {
        /// <summary>One value per shared material.</summary>
        None = 0,
        /// <summary>A renderer-session global value.</summary>
        Global = 1,
        /// <summary>One value per canvas item, with an automatically assigned slot.</summary>
        Instance = 2,
        /// <summary>One value per canvas item, with an explicit slot.</summary>
        InstanceIndex = 3,
        /// <summary>The nonselectable qualifier count.</summary>
        Max = 4,
    }
    private string _parameterName = "";
    private QualifierEnum _qualifier;
    private int _instanceIndex;
    /// <summary>Initializes a parameter definition.</summary>
    protected VisualShaderNodeParameter() { }
    /// <summary>Gets or assigns the authored symbol name.</summary>
    /// <value>Empty initially; AddNode assigns a valid unique name. Later edits must form a valid nonreserved symbol at import.</value>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public string ParameterName
    { get { ThrowIfDisposed(); return _parameterName; } set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); _parameterName = value; EmitChanged(); } }
    /// <summary>Gets or selects the value owner.</summary>
    /// <value>None initially. An unchanged selection is quiet. Matrix and sampled-image declarations reject per-item storage at import.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is undefined or the count sentinel.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public QualifierEnum Qualifier
    {
        get { ThrowIfDisposed(); return _qualifier; }
        set { ThrowIfDisposed(); if (!Enum.IsDefined(value) || value == QualifierEnum.Max) throw new ArgumentOutOfRangeException(nameof(value)); if (_qualifier == value) return; _qualifier = value; EmitChanged(); }
    }
    /// <summary>Gets or assigns the explicit per-item slot used by InstanceIndex.</summary>
    /// <value>Zero initially; valid slots are zero through fifteen. Other qualifiers retain this dormant value.</value>
    /// <exception cref="ArgumentOutOfRangeException">The slot is outside zero through fifteen.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public int InstanceIndex
    { get { ThrowIfDisposed(); return _instanceIndex; } set { ThrowIfDisposed(); if ((uint)value >= 16) throw new ArgumentOutOfRangeException(nameof(value)); _instanceIndex = value; EmitChanged(); } }
    internal abstract object? ParameterDefault { get; }
    internal abstract bool HasParameterDefault { get; }
    internal virtual string ParameterCaption => GetType().Name["VisualShaderNode".Length..];
    internal override PortType[] Inputs => [];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => ParameterName;
    internal override void CopyDefinitionTo(VisualShaderNode target)
    { var copy = (VisualShaderNodeParameter)target; copy._parameterName = _parameterName; copy._qualifier = _qualifier; copy._instanceIndex = _instanceIndex; }
    internal void SetDefinitionValue<T>(ref T field, T value, bool quiet) where T : unmanaged
    { ThrowIfDisposed(); VisualShaderValues.Validate(value); if (quiet && VisualShaderValues.SameConstant(field, value)) return; field = value; EmitChanged(); }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeParameter, string>(nameof(ParameterName), n => n.ParameterName, (n,v) => n.ParameterName = v, stored:true),
         new PropertyDescriptor<VisualShaderNodeParameter, QualifierEnum>(nameof(Qualifier), n => n.Qualifier, (n,v) => n.Qualifier = v, stored:true),
         new PropertyDescriptor<VisualShaderNodeParameter, int>(nameof(InstanceIndex), n => n.InstanceIndex, (n,v) => n.InstanceIndex = v, stored:true)]);
}
