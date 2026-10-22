using System.Globalization;

namespace Electron2D;

/// <summary>Defines a compiled C# visual shader node for the offline canvas importer.</summary>
/// <remarks>Register each concrete resource factory in the authoring/import/runtime hosts.
/// Callbacks provide typed ports, defaults, metadata and GLSL source; no delegate or assembly is loaded from an archive.
/// Definition discovery is lazy, after the derived constructor. Successful option edits rebuild callback defaults.
/// Derived stored schema fields precede base descriptors, and derived resources retain the ordinary explicit copy hooks.
/// Source callbacks run during import, never during material drawing. Editor UI remains a separate consumer.</remarks>
public abstract class VisualShaderNodeCustom : VisualShaderNode
{
    private const int MaximumEntries = 4096;
    private sealed record Option(string Name, string[] Choices, int DefaultIndex);
    private Dictionary<int, int> _selections = [];
    private Option[] _properties = [];
    private PortType[]? _inputs, _outputs;
    private bool _building, _preserveDefaults, _generating;

    /// <summary>Initializes an undiscovered custom definition without invoking derived callbacks.</summary>
    protected VisualShaderNodeCustom() { }

    /// <summary>Returns the programmatic authoring category path.</summary>
    /// <returns>Addons unless overridden.</returns>
    public virtual string GetCategory() { ThrowIfDisposed(); return "Addons"; }
    /// <summary>Produces one GLSL statement block using opaque input and output symbols.</summary>
    /// <param name="inputVariables">Read-only symbols; an absent input/default is an empty string.</param>
    /// <param name="outputVariables">Read-only numeric output symbols, initialized by the importer.</param>
    /// <param name="mode">The executing shader domain.</param>
    /// <param name="type">The executing graph stage.</param>
    /// <returns>Bounded strict UTF-8 GLSL; empty or literal null source emits no statements.</returns>
    /// <remarks>Use the supplied symbols without declaring replacements for them. They are opaque compiler names.
    /// The last authored connection to an input supplies its value, matching the ordinary graph connection policy.
    /// Loops, branches, return and fragment discard follow the ordinary expression scope contract.</remarks>
    public abstract string GetCode(IReadOnlyList<string> inputVariables, IReadOnlyList<string> outputVariables, Shader.Mode mode, VisualShader.Type type);
    /// <inheritdoc />
    public override int GetDefaultInputPort(PortType type)
    { var port = base.GetDefaultInputPort(type); EnsureDefinition(); return port; }
    /// <summary>Returns the authoring description.</summary>
    /// <returns>Empty unless overridden.</returns>
    public virtual string GetDescription() { ThrowIfDisposed(); return ""; }
    /// <summary>Produces one statement block at the beginning of each active graph stage function.</summary>
    /// <param name="mode">The shader domain.</param>
    /// <param name="type">The target graph stage, including other stages when this provider is reachable.</param>
    /// <returns>Empty initially; source is included once per concrete provider type and stage.</returns>
    /// <remarks>Nonempty particle-stage setup activates that stage even when its reserved output has no assignments.
    /// Return empty source for stages that should remain inactive.</remarks>
    public virtual string GetFuncCode(Shader.Mode mode, VisualShader.Type type)
    { RequireContext(mode, type); return ""; }
    /// <summary>Produces shared GLSL declarations for the active shader module.</summary>
    /// <param name="mode">The shader domain.</param>
    /// <returns>Empty initially; source is included once per concrete reachable provider type.</returns>
    /// <remarks>Use ELECTRON2D_SHADER_STAGE for native stage-specific declarations. Distinct provider types own their own symbol names.</remarks>
    public virtual string GetGlobalCode(Shader.Mode mode)
    { ThrowIfDisposed(); if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode)); return ""; }
    /// <summary>Returns the active input count.</summary>
    /// <returns>Zero unless overridden; discovery accepts at most 4096.</returns>
    public virtual int GetInputPortCount() { ThrowIfDisposed(); return 0; }
    /// <summary>Provides a copied, strongly typed default for one input.</summary>
    /// <param name="port">An existing input index.</param>
    /// <returns>A matching immutable entry for this index, or null for explicit absence. Samplers have no numeric default.</returns>
    /// <remarks>Discovery and option refresh consume these defaults. Subsequent ordinary typed default setters remain available.</remarks>
    public virtual VisualShaderPortDefault? GetInputPortDefaultValue(int port)
    { RequireIndex(port, GetInputPortCount()); return null; }
    /// <summary>Returns the input label supplied to programmatic authoring tools.</summary>
    /// <param name="port">An existing input index.</param>
    /// <returns>in followed by the invariant port number unless overridden.</returns>
    public virtual string GetInputPortName(int port)
    { RequireIndex(port, GetInputPortCount()); return "in" + port.ToString(CultureInfo.InvariantCulture); }
    /// <summary>Returns one input's value role.</summary>
    /// <param name="port">An existing input index.</param>
    /// <returns>Scalar unless overridden.</returns>
    public virtual PortType GetInputPortType(int port)
    { RequireIndex(port, GetInputPortCount()); return PortType.Scalar; }
    /// <summary>Returns the authoring caption.</summary>
    /// <returns>Unnamed unless overridden.</returns>
    public virtual string GetName() { ThrowIfDisposed(); return "Unnamed"; }
    /// <summary>Returns the active output count.</summary>
    /// <returns>Zero unless overridden; discovery accepts at most 4096.</returns>
    public virtual int GetOutputPortCount() { ThrowIfDisposed(); return 0; }
    /// <summary>Returns the output label supplied to authoring tools.</summary>
    /// <param name="port">An existing output index.</param>
    /// <returns>out followed by the invariant port number unless overridden.</returns>
    public virtual string GetOutputPortName(int port)
    { RequireIndex(port, GetOutputPortCount()); return "out" + port.ToString(CultureInfo.InvariantCulture); }
    /// <summary>Returns one output's value role.</summary>
    /// <param name="port">An existing output index.</param>
    /// <returns>Scalar unless overridden. Local sampler outputs reject during import.</returns>
    public virtual PortType GetOutputPortType(int port)
    { RequireIndex(port, GetOutputPortCount()); return PortType.Scalar; }
    /// <summary>Returns the active dropdown-property count.</summary>
    /// <returns>Zero unless overridden; discovery accepts at most 4096.</returns>
    public virtual int GetPropertyCount() { ThrowIfDisposed(); return 0; }
    /// <summary>Returns the initial selected index of one dropdown property.</summary>
    /// <param name="index">An active property index.</param>
    /// <returns>Zero unless overridden.</returns>
    public virtual int GetPropertyDefaultIndex(int index)
    { RequireIndex(index, GetPropertyCount()); return 0; }
    /// <summary>Returns a dropdown property's authoring label.</summary>
    /// <param name="index">An active property index.</param>
    /// <returns>prop unless overridden; descriptor identities also include the numeric property index.</returns>
    public virtual string GetPropertyName(int index)
    { RequireIndex(index, GetPropertyCount()); return "prop"; }
    /// <summary>Returns ordered dropdown labels.</summary>
    /// <param name="index">An active property index.</param>
    /// <returns>A fresh one-element Default list unless overridden.</returns>
    /// <remarks>Discovery copies the array. An empty list retains logical selection zero. The aggregate choice budget is 4096.</remarks>
    public virtual string[] GetPropertyOptions(int index)
    { RequireIndex(index, GetPropertyCount()); return ["Default"]; }
    /// <summary>Returns the optional authoring result-icon role.</summary>
    /// <returns>Null for no icon unless overridden; a concrete role excludes Max.</returns>
    public virtual PortType? GetReturnIconType() { ThrowIfDisposed(); return null; }
    /// <summary>Reports whether authoring tools should offer this provider in a context.</summary>
    /// <param name="mode">The shader domain.</param>
    /// <param name="type">The graph stage.</param>
    /// <returns>True unless overridden.</returns>
    /// <remarks>This is discovery metadata. Existing authored nodes still undergo ordinary source/stage validation at import.</remarks>
    public virtual bool IsAvailable(Shader.Mode mode, VisualShader.Type type)
    { RequireContext(mode, type); return true; }
    /// <summary>Reports the author's advisory advanced-renderer mark.</summary>
    /// <returns>False unless overridden.</returns>
    /// <remarks>The actual renderer validates imported native capabilities independently.</remarks>
    public virtual bool IsHighEnd() { ThrowIfDisposed(); return false; }

    /// <summary>Reads an active or retained dropdown selection.</summary>
    /// <param name="option">A property index; unknown indices return zero.</param>
    /// <returns>The retained selection, including explicit zero and temporarily inactive properties.</returns>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public int GetOptionIndex(int option)
    { ThrowIfDisposed(); if (!_building) EnsureDefinition(); return _selections.GetValueOrDefault(option); }
    /// <summary>Changes a dropdown selection and regenerates ports and callback defaults atomically.</summary>
    /// <param name="option">An active property index.</param>
    /// <param name="index">A valid choice; an empty choice list accepts logical zero.</param>
    /// <remarks>Equal selections still regenerate and publish Changed and PropertyListChanged. Callback failure preserves engine configuration;
    /// arbitrary side effects in application callbacks are not rolled back. Invalidated graph edges remain caller-owned.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The property or selection is invalid.</exception>
    /// <exception cref="InvalidOperationException">Definition discovery reenters configuration mutation or provider metadata is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public void SetOptionIndex(int option, int index)
    {
        BeforeDefaultMutation(); EnsureDefinition(); RequireIndex(option, _properties.Length);
        if ((uint)index >= (uint)Math.Max(1, _properties[option].Choices.Length)) throw new ArgumentOutOfRangeException(nameof(index));
        var selected = new Dictionary<int, int>(_selections) { [option] = index }; UpdateDefinition(selected, false); PublishDefinitionChange();
    }
    /// <summary>Regenerates metadata, ports and defaults after a compiled provider's own schema fields change.</summary>
    /// <remarks>Preserves selected and temporarily inactive options; resets the numeric default table from callbacks.
    /// Call this from schema-changing application setters. No virtual callback runs from the base constructor.</remarks>
    /// <exception cref="InvalidOperationException">Provider metadata is invalid or configuration mutation is reentrant.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public void RefreshDefinition()
    { BeforeDefaultMutation(); UpdateDefinition(new(_selections), false); PublishDefinitionChange(); }

    internal override PortType[] Inputs { get { EnsureDefinition(); return _inputs!; } }
    internal override PortType[] Outputs { get { EnsureDefinition(); return _outputs!; } }
    internal override void BeforeDefaultMutation()
    { ThrowIfDisposed(); if (_building || _generating) throw new InvalidOperationException("Custom callbacks cannot mutate engine configuration reentrantly."); }
    private void EnsureDefinition()
    {
        ThrowIfDisposed();
        if (_inputs is not null && _outputs is not null) return;
        if (_building) throw new InvalidOperationException("The custom port schema is not yet available during this callback.");
        UpdateDefinition(new(_selections), _preserveDefaults);
    }
    private void RequireContext(Shader.Mode mode, VisualShader.Type type)
    {
        ThrowIfDisposed(); if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));
    }
    private void RequireIndex(int index, int count)
    { ThrowIfDisposed(); if ((uint)index >= (uint)count) throw new ArgumentOutOfRangeException(nameof(index)); }
    private int Count(int count)
    { ThrowIfDisposed(); if ((uint)count > MaximumEntries) throw new InvalidOperationException("Custom definition count exceeds its budget."); return count; }
    private PortType Role(PortType type)
    { ThrowIfDisposed(); if (!Enum.IsDefined(type) || type == PortType.Max) throw new InvalidOperationException("Custom ports require a concrete value role."); return type; }
    private string Text(string? text, ref int bytes)
    {
        ThrowIfDisposed(); if (text is null) throw new InvalidOperationException("Custom callbacks must return nonnull text.");
        var size = ShaderInclude.SourceEncoding.GetByteCount(text);
        if (size > ShaderInclude.MaximumSourceBytes - bytes) throw new InvalidOperationException("Custom metadata exceeds its byte budget.");
        bytes += size; return text;
    }
    private void UpdateDefinition(Dictionary<int, int> selected, bool preserveDefaults)
    {
        ThrowIfDisposed(); if (_building) throw new InvalidOperationException("Custom definition discovery cannot reenter itself.");
        var oldSelections = _selections; var oldInputs = _inputs; var oldOutputs = _outputs; var oldProperties = _properties;
        _building = true; _selections = selected;
        try
        {
            for (var i = 0; i < Count(GetPropertyCount()); i++)
                if (!selected.ContainsKey(i))
                {
                    var initial = GetPropertyDefaultIndex(i);
                    var choices = GetPropertyOptions(i) ?? throw new InvalidOperationException("Custom choices cannot be null.");
                    if (choices.Length > MaximumEntries || (uint)initial >= (uint)Math.Max(1, choices.Length))
                        throw new InvalidOperationException("Custom default selection is invalid.");
                    selected.Add(i, initial);
                }
            var bytes = 0; _ = Text(GetName(), ref bytes); _ = Text(GetDescription(), ref bytes); _ = Text(GetCategory(), ref bytes);
            if (GetReturnIconType() is { } icon) _ = Role(icon); _ = IsHighEnd();
            var properties = new Option[Count(GetPropertyCount())]; var choicesTotal = 0;
            for (var i = 0; i < properties.Length; i++)
            {
                var name = Text(GetPropertyName(i), ref bytes); var choices = GetPropertyOptions(i) ?? throw new InvalidOperationException("Custom choices cannot be null.");
                if (choices.Length > MaximumEntries - choicesTotal) throw new InvalidOperationException("Custom dropdown choices exceed their budget.");
                choicesTotal += choices.Length; choices = (string[])choices.Clone(); foreach (var choice in choices) _ = Text(choice, ref bytes);
                var initial = GetPropertyDefaultIndex(i);
                if ((uint)initial >= (uint)Math.Max(1, choices.Length)) throw new InvalidOperationException("Custom default selection is invalid.");
                selected.TryAdd(i, initial);
                if ((uint)selected[i] >= (uint)Math.Max(1, choices.Length)) throw new InvalidOperationException("Custom selection is incompatible with its current choices.");
                properties[i] = new(name, choices, initial);
            }
            var inputs = new PortType[Count(GetInputPortCount())]; var outputs = new PortType[Count(GetOutputPortCount())];
            for (var i = 0; i < inputs.Length; i++) { inputs[i] = Role(GetInputPortType(i)); _ = Text(GetInputPortName(i), ref bytes); }
            for (var i = 0; i < outputs.Length; i++) { outputs[i] = Role(GetOutputPortType(i)); _ = Text(GetOutputPortName(i), ref bytes); }
            _inputs = inputs; _outputs = outputs;
            var defaults = preserveDefaults ? SnapshotDefinitionDefaults().ToList() : new List<VisualShaderPortDefault>();
            if (!preserveDefaults)
                for (var i = 0; i < inputs.Length; i++) if (GetInputPortDefaultValue(i) is { } value)
                    { if (value.Port != i) throw new InvalidOperationException("Custom default entries must identify their requested input."); defaults.Add(value); }
            foreach (var value in defaults)
            {
                if ((uint)value.Port >= (uint)inputs.Length) throw new InvalidOperationException("Custom copied defaults no longer match their port schema.");
                VisualShaderValues.Require(inputs[value.Port], value.BoxedValue);
            }
            ThrowIfDisposed(); ReplaceDefinitionDefaults(defaults); _properties = properties; _preserveDefaults = false;
        }
        catch { _selections = oldSelections; _inputs = oldInputs; _outputs = oldOutputs; _properties = oldProperties; throw; }
        finally { _building = false; }
    }
    private void PublishDefinitionChange()
    {
        Exception? error = null;
        try { EmitChanged(); } catch (Exception caught) { error = caught; }
        try { if (!IsDisposed) NotifyPropertyListChanged(); }
        catch (Exception caught) { if (error is not null) throw new AggregateException(error, caught); throw; }
        if (error is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }
    private int DefaultOptionIndex(int option)
    { EnsureDefinition(); RequireIndex(option, _properties.Length); return _properties[option].DefaultIndex; }
    private int[] StoredOptions() => _selections.OrderBy(p => p.Key).SelectMany(p => new[] { p.Key, p.Value }).ToArray();
    private void RestoreOptions(int[] values)
    {
        BeforeDefaultMutation(); ArgumentNullException.ThrowIfNull(values);
        if (values.Length > MaximumEntries * 2 || values.Length % 2 != 0) throw new ArgumentException("Invalid custom option table.", nameof(values));
        var selected = new Dictionary<int, int>();
        for (var i = 0; i < values.Length; i += 2)
            if ((uint)values[i] >= MaximumEntries || (uint)values[i + 1] >= MaximumEntries || !selected.TryAdd(values[i], values[i + 1]))
                throw new ArgumentException("Custom option indices must be distinct bounded nonnegative pairs.", nameof(values));
        UpdateDefinition(selected, false); PublishDefinitionChange();
    }
    internal string ValidatedCode(string? source)
    {
        ThrowIfDisposed(); if (source is null) throw new InvalidOperationException("Custom source callbacks must return a string.");
        if (ShaderInclude.SourceEncoding.GetByteCount(source) > ShaderInclude.MaximumSourceBytes) throw new InvalidOperationException("Custom source exceeds its byte budget.");
        return source == "null" ? "" : source;
    }
    internal string EvaluateCode(Func<string> callback)
    {
        EnsureDefinition();
        if (_generating) throw new InvalidOperationException("Custom source callbacks cannot reenter source generation.");
        _generating = true;
        try { return ValidatedCode(callback()); }
        finally { _generating = false; }
    }
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => throw new InvalidOperationException("Custom nodes require shared offline statement lowering.");
    internal override void CopyDefinitionTo(VisualShaderNode target)
    {
        var copy = (VisualShaderNodeCustom)target; copy._selections = new(_selections); copy._properties = [];
        copy._inputs = null; copy._outputs = null; copy._preserveDefaults = true;
    }
    /// <inheritdoc />
    protected override void OnResetState()
    { BeforeDefaultMutation(); base.OnResetState(); _inputs = null; _outputs = null; _properties = []; _preserveDefaults = true; }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        if (_building) throw new InvalidOperationException("Custom metadata discovery cannot reenter its property list.");
        EnsureDefinition();
        yield return new PropertyDescriptor<VisualShaderNodeCustom, int[]>("CustomOptions", n => n.StoredOptions(), (n, v) => n.RestoreOptions(v), stored: true);
        for (var i = 0; i < _properties.Length; i++)
        {
            var index = i; var property = _properties[i];
            yield return new PropertyDescriptor<VisualShaderNodeCustom, int>("Option[" + i.ToString(CultureInfo.InvariantCulture) + "]:" + property.Name,
                n => n.GetOptionIndex(index), (n, v) => n.SetOptionIndex(index, v), n => n.DefaultOptionIndex(index))
            { Hint = PropertyHint.Enum, HintString = string.Join(",", property.Choices) };
        }
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
    }
    internal override void ValidateFileState() { EnsureDefinition(); base.ValidateFileState(); }
}
