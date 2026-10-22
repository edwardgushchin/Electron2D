namespace Electron2D;

/// <summary>Declares a named Vector4 value for an imported canvas shader.</summary>
/// <remarks>Authored defaults, qualifiers and tooling metadata survive copies and archives. Changes require
/// reimport. Material, global and per-item values use their existing typed runtime APIs.</remarks>
public sealed class VisualShaderNodeVec4Parameter : VisualShaderNodeParameter
{
    private Vector4 _defaultValue = Vector4.Zero;
    private bool _defaultValueEnabled;
    /// <summary>Creates the definition with its initial stored value and the authored default disabled.</summary>
    public VisualShaderNodeVec4Parameter() { }
    /// <summary>Gets or replaces the copied authored default.</summary>
    /// <value>Initially Vector4.Zero. Floating components must be finite; disabling the default retains this value.</value>
    /// <exception cref="ArgumentException">A floating component is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public Vector4 DefaultValue { get { ThrowIfDisposed(); return _defaultValue; } set => SetDefinitionValue(ref _defaultValue, value, false); }
    /// <summary>Gets or changes whether import supplies the stored authored default.</summary>
    /// <value>False initially. Global values remain registry-owned and cannot use local authored defaults.</value>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public bool DefaultValueEnabled { get { ThrowIfDisposed(); return _defaultValueEnabled; } set => SetDefinitionValue(ref _defaultValueEnabled, value, false); }
    internal override PortType[] Outputs => [PortType.Vector4];
    internal override object ParameterDefault => _defaultValue;
    internal override bool HasParameterDefault => _defaultValueEnabled;
    internal override void CopyDefinitionTo(VisualShaderNode target)
    { base.CopyDefinitionTo(target); var copy = (VisualShaderNodeVec4Parameter)target; copy._defaultValue = _defaultValue; copy._defaultValueEnabled = _defaultValueEnabled; }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeVec4Parameter();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeVec4Parameter, Vector4>(nameof(DefaultValue), n => n.DefaultValue, (n,v) => n.DefaultValue = v, stored:true),
         new PropertyDescriptor<VisualShaderNodeVec4Parameter, bool>(nameof(DefaultValueEnabled), n => n.DefaultValueEnabled, (n,v) => n.DefaultValueEnabled = v, stored:true)]);
}
