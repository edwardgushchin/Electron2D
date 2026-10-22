namespace Electron2D;

/// <summary>Declares a named int value for an imported canvas shader.</summary>
/// <remarks>Authored defaults, qualifiers and tooling metadata survive copies and archives. Changes require
/// reimport. Material, global and per-item values use their existing typed runtime APIs.</remarks>
public sealed class VisualShaderNodeIntParameter : VisualShaderNodeParameter
{
    private int _defaultValue = 0;
    private bool _defaultValueEnabled;
    /// <summary>Creates the definition with its initial stored value and the authored default disabled.</summary>
    public VisualShaderNodeIntParameter() { }
    /// <summary>Gets or replaces the copied authored default.</summary>
    /// <value>Initially 0. Disabling the default retains this value.</value>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public int DefaultValue { get { ThrowIfDisposed(); return _defaultValue; } set => SetDefinitionValue(ref _defaultValue, value, true); }
    /// <summary>Gets or changes whether import supplies the stored authored default.</summary>
    /// <value>False initially. Global values remain registry-owned and cannot use local authored defaults.</value>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public bool DefaultValueEnabled { get { ThrowIfDisposed(); return _defaultValueEnabled; } set => SetDefinitionValue(ref _defaultValueEnabled, value, true); }
    /// <summary>The numeric property hint exposed to tooling.</summary>
    public enum HintEnum
    {
        /// <summary>No tooling hint.</summary>
        None = 0,
        /// <summary>An advisory range.</summary>
        Range = 1,
        /// <summary>An advisory range with an explicit step.</summary>
        RangeStep = 2,
        /// <summary>Named integer choices.</summary>
        Enum = 3,
        /// <summary>The nonselectable hint count.</summary>
        Max = 4,
    }
    private HintEnum _hint;
    private int _min, _max = 100, _step = 1;
    /// <summary>Gets or selects advisory numeric metadata; this does not clamp runtime values.</summary>
    /// <value>None initially. An unchanged selection is quiet.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is undefined or the count sentinel.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public HintEnum Hint
    { get { ThrowIfDisposed(); return _hint; } set { ThrowIfDisposed(); if (!Enum.IsDefined(value) || value == HintEnum.Max) throw new ArgumentOutOfRangeException(nameof(value)); if (_hint == value) return; _hint = value; EmitChanged(); } }
    /// <summary>Gets or changes the advisory min value.</summary>
    /// <value>Initially zero; retained independently of the selected hint.</value>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public int Min { get { ThrowIfDisposed(); return _min; } set => SetDefinitionValue(ref _min, value, true); }
    /// <summary>Gets or changes the advisory max value.</summary>
    /// <value>Initially 100; retained independently of the selected hint.</value>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public int Max { get { ThrowIfDisposed(); return _max; } set => SetDefinitionValue(ref _max, value, true); }
    /// <summary>Gets or changes the advisory step value.</summary>
    /// <value>Initially one; retained independently of the selected hint.</value>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public int Step { get { ThrowIfDisposed(); return _step; } set => SetDefinitionValue(ref _step, value, true); }
    private string[] _enumNames = [];
    /// <summary>Gets an independent copy or replaces the named integer choices.</summary>
    /// <value>Empty initially. Order and explicit label/value syntax are retained.</value>
    /// <exception cref="ArgumentNullException">The array or one of its labels is null.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public string[] EnumNames
    { get { ThrowIfDisposed(); return (string[])_enumNames.Clone(); } set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); if (value.Any(v => v is null)) throw new ArgumentNullException(nameof(value)); _enumNames = (string[])value.Clone(); EmitChanged(); } }
    internal override PortType[] Outputs => [PortType.ScalarInt];
    internal override object ParameterDefault => _defaultValue;
    internal override bool HasParameterDefault => _defaultValueEnabled;
    internal override void CopyDefinitionTo(VisualShaderNode target)
    { base.CopyDefinitionTo(target); var copy = (VisualShaderNodeIntParameter)target; copy._defaultValue = _defaultValue; copy._defaultValueEnabled = _defaultValueEnabled; copy._hint = _hint; copy._min = _min; copy._max = _max; copy._step = _step; copy._enumNames = (string[])_enumNames.Clone(); }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeIntParameter();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeIntParameter, int>(nameof(DefaultValue), n => n.DefaultValue, (n,v) => n.DefaultValue = v, stored:true),
         new PropertyDescriptor<VisualShaderNodeIntParameter, bool>(nameof(DefaultValueEnabled), n => n.DefaultValueEnabled, (n,v) => n.DefaultValueEnabled = v, stored:true),
         new PropertyDescriptor<VisualShaderNodeIntParameter, HintEnum>(nameof(Hint), n => n.Hint, (n,v) => n.Hint = v, stored:true),
         new PropertyDescriptor<VisualShaderNodeIntParameter, int>(nameof(Min), n => n.Min, (n,v) => n.Min = v, stored:true),
         new PropertyDescriptor<VisualShaderNodeIntParameter, int>(nameof(Max), n => n.Max, (n,v) => n.Max = v, stored:true),
         new PropertyDescriptor<VisualShaderNodeIntParameter, int>(nameof(Step), n => n.Step, (n,v) => n.Step = v, stored:true),
         new PropertyDescriptor<VisualShaderNodeIntParameter, string[]>(nameof(EnumNames), n => n.EnumNames, (n,v) => n.EnumNames = v, stored:true)]);
}
