namespace Electron2D;

/// <summary>Declares a named uint value for an imported canvas shader.</summary>
/// <remarks>Authored defaults, qualifiers and tooling metadata survive copies and archives. Changes require
/// reimport. Material, global and per-item values use their existing typed runtime APIs.</remarks>
public sealed class VisualShaderNodeUIntParameter : VisualShaderNodeParameter
{
    private uint _defaultValue = 0u;
    private bool _defaultValueEnabled;
    /// <summary>Creates the definition with its initial stored value and the authored default disabled.</summary>
    public VisualShaderNodeUIntParameter() { }
    /// <summary>Gets or replaces the copied authored default.</summary>
    /// <value>Initially 0u. Disabling the default retains this value.</value>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public uint DefaultValue { get { ThrowIfDisposed(); return _defaultValue; } set => SetDefinitionValue(ref _defaultValue, value, true); }
    /// <summary>Gets or changes whether import supplies the stored authored default.</summary>
    /// <value>False initially. Global values remain registry-owned and cannot use local authored defaults.</value>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public bool DefaultValueEnabled { get { ThrowIfDisposed(); return _defaultValueEnabled; } set => SetDefinitionValue(ref _defaultValueEnabled, value, true); }
    internal override PortType[] Outputs => [PortType.ScalarUInt];
    internal override object ParameterDefault => _defaultValue;
    internal override bool HasParameterDefault => _defaultValueEnabled;
    internal override void CopyDefinitionTo(VisualShaderNode target)
    { base.CopyDefinitionTo(target); var copy = (VisualShaderNodeUIntParameter)target; copy._defaultValue = _defaultValue; copy._defaultValueEnabled = _defaultValueEnabled; }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeUIntParameter();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeUIntParameter, uint>(nameof(DefaultValue), n => n.DefaultValue, (n,v) => n.DefaultValue = v, stored:true),
         new PropertyDescriptor<VisualShaderNodeUIntParameter, bool>(nameof(DefaultValueEnabled), n => n.DefaultValueEnabled, (n,v) => n.DefaultValueEnabled = v, stored:true)]);
}
