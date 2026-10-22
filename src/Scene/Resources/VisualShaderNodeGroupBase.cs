using System.Globalization;

namespace Electron2D;

/// <summary>Owns ordered, named, typed input and output ports for source graph expressions.</summary>
/// <remarks>Port IDs are contiguous indices. Insert/remove renumbers the following ports; graph edges keep
/// their stored indices and callers repair affected connections. Numeric defaults move with inputs,
/// convert on type changes and retain explicit absence. Sampler ports have no numeric defaults.
/// Mutations validate before committing and publish Changed and the stored-property schema afterwards.</remarks>
public abstract class VisualShaderNodeGroupBase : VisualShaderNodeResizableBase
{
    internal readonly record struct GroupPort(PortType Type, string Name);
    internal const int MaximumPorts = 4096;
    private GroupPort[] _inputPorts = [], _outputPorts = [];
    internal IReadOnlyList<GroupPort> InputPorts => _inputPorts;
    internal IReadOnlyList<GroupPort> OutputPorts => _outputPorts;
    /// <summary>Initializes empty port lists.</summary>
    protected VisualShaderNodeGroupBase() { }
    internal override PortType[] Inputs => _inputPorts.Select(p => p.Type).ToArray();
    internal override PortType[] Outputs => _outputPorts.Select(p => p.Type).ToArray();
    /// <summary>Returns the number of input ports.</summary>
    /// <returns>The next free input index.</returns>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public int GetInputPortCount() { ThrowIfDisposed(); return _inputPorts.Length; }
    /// <summary>Returns the number of output ports.</summary>
    /// <returns>The next free output index.</returns>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public int GetOutputPortCount() { ThrowIfDisposed(); return _outputPorts.Length; }
    /// <summary>Returns the next appendable input ID.</summary>
    /// <returns>The current input count.</returns>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public int GetFreeInputPortID() => GetInputPortCount();
    /// <summary>Returns the next appendable output ID.</summary>
    /// <returns>The current output count.</returns>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public int GetFreeOutputPortID() => GetOutputPortCount();
    /// <summary>Reports whether an input index exists.</summary>
    /// <param name="id">The input index.</param>
    /// <returns>False for negative and out-of-range indices.</returns>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public bool HasInputPort(int id) { ThrowIfDisposed(); return (uint)id < (uint)_inputPorts.Length; }
    /// <summary>Reports whether an output index exists.</summary>
    /// <param name="id">The output index.</param>
    /// <returns>False for negative and out-of-range indices.</returns>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public bool HasOutputPort(int id) { ThrowIfDisposed(); return (uint)id < (uint)_outputPorts.Length; }
    /// <summary>Checks an unused ASCII identifier against both port lists.</summary>
    /// <param name="name">The proposed case-sensitive name.</param>
    /// <returns>True for a unique identifier beginning with a letter or underscore, followed by letters, digits or underscores; at most 128 characters.</returns>
    /// <remarks>Shader keyword/builtin conflicts are diagnosed during source compilation.</remarks>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public bool IsValidPortName(string name)
    { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(name); return Identifier(name) && !_inputPorts.Concat(_outputPorts).Any(p => p.Name == name); }
    private static bool Identifier(string name) => name.Length is > 0 and <= 128 && (char.IsAsciiLetter(name[0]) || name[0] == '_') && name.All(c => char.IsAsciiLetterOrDigit(c) || c == '_');
    private GroupPort[] Ports(bool input) => input ? _inputPorts : _outputPorts;
    private void Commit(bool input, GroupPort[] replacement, int[] sources)
    {
        if (input) RemapDefaults(replacement.Select((p, i) => (sources[i], p.Type)).ToArray());
        if (input) _inputPorts = replacement; else _outputPorts = replacement;
        Exception? error = null;
        try { EmitChanged(); } catch (Exception caught) { error = caught; }
        try { if (!IsDisposed) NotifyPropertyListChanged(); }
        catch (Exception caught) { if (error is not null) throw new AggregateException(error, caught); throw; }
        if (error is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }
    private void RequireName(string name, bool input, int except = -1)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (!Identifier(name) || Ports(!input).Any(p => p.Name == name) || Ports(input).Where((_, i) => i != except).Any(p => p.Name == name))
            throw new ArgumentException("Port names require distinct ASCII identifiers across both lists.", nameof(name));
    }
    private static void RequireType(PortType type)
    { if (!Enum.IsDefined(type) || type == PortType.Max) throw new ArgumentOutOfRangeException(nameof(type)); }
    private GroupPort[] At(bool input, int id)
    { ThrowIfDisposed(); var ports = Ports(input); if ((uint)id >= (uint)ports.Length) throw new ArgumentOutOfRangeException(nameof(id)); return ports; }
    private void AddPort(bool input, int id, PortType type, string name)
    {
        ThrowIfDisposed(); if (id < 0) throw new ArgumentOutOfRangeException(nameof(id)); RequireType(type); RequireName(name, input);
        var ports = Ports(input); if (ports.Length == MaximumPorts) throw new InvalidOperationException("Expression port capacity exceeded.");
        id = Math.Min(id, ports.Length); var replacement = ports.ToList(); replacement.Insert(id, new(type, name));
        Commit(input, replacement.ToArray(), Enumerable.Range(0, replacement.Count).Select(i => i < id ? i : i == id ? -1 : i - 1).ToArray());
    }
    private void RemovePort(bool input, int id)
    { var ports = At(input, id); Commit(input, ports.Where((_, i) => i != id).ToArray(), Enumerable.Range(0, ports.Length).Where(i => i != id).ToArray()); }
    private void RenamePort(bool input, int id, string name)
    {
        var ports = At(input, id); ArgumentNullException.ThrowIfNull(name); if (ports[id].Name == name) return;
        RequireName(name, input, id); var replacement = (GroupPort[])ports.Clone(); replacement[id] = replacement[id] with { Name = name };
        Commit(input, replacement, Enumerable.Range(0, ports.Length).ToArray());
    }
    private void SetPortType(bool input, int id, PortType type)
    {
        var ports = At(input, id); RequireType(type); if (ports[id].Type == type) return;
        var replacement = (GroupPort[])ports.Clone(); replacement[id] = replacement[id] with { Type = type };
        Commit(input, replacement, Enumerable.Range(0, ports.Length).ToArray());
    }
    private void ClearPorts(bool input)
    { ThrowIfDisposed(); if (Ports(input).Length != 0) Commit(input, [], []); }
    private string PortDescription(bool input)
    { ThrowIfDisposed(); return string.Concat(Ports(input).Select((p, i) => i.ToString(CultureInfo.InvariantCulture) + "," + ((int)p.Type).ToString(CultureInfo.InvariantCulture) + "," + p.Name + ";")); }
    private void SetPorts(bool input, string description)
    {
        ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(description);
        if (description.Length > MaximumPorts * 160) throw new ArgumentException("Port description exceeds its budget.", nameof(description));
        var rows = description.Split(';', StringSplitOptions.RemoveEmptyEntries); if (rows.Length > MaximumPorts) throw new ArgumentException("Too many ports.", nameof(description));
        var replacement = new GroupPort[rows.Length]; var names = Ports(!input).Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        for (var i = 0; i < rows.Length; i++)
        {
            var fields = rows[i].Split(',');
            if (fields.Length != 3 || !int.TryParse(fields[0], NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id != i ||
                !int.TryParse(fields[1], NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value < 0 || value >= (int)PortType.Max || !Identifier(fields[2]) || !names.Add(fields[2]))
                throw new ArgumentException("Port descriptions require contiguous id,type,name rows with unique valid names.", nameof(description));
            replacement[i] = new((PortType)value, fields[2]);
        }
        if (Ports(input).SequenceEqual(replacement)) return;
        Commit(input, replacement, Enumerable.Range(0, replacement.Length).Select(i => i < Ports(input).Length ? i : -1).ToArray());
    }

    /// <summary>Inserts one input port and renumbers subsequent ports.</summary>
    /// <param name="id">A nonnegative insertion index; indices at or beyond the count append.</param>
    /// <param name="type">The exact value role, excluding Max.</param>
    /// <param name="name">A unique ASCII name across both port lists.</param>
    /// <remarks>Connection indices remain stored in borrowing graphs. Default conversion can throw before mutation; floating defaults must remain finite.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">An index or value role is invalid.</exception>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ArgumentException">The name is invalid or already used.</exception>
    /// <exception cref="InvalidOperationException">The 4096-port bound is reached.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public void AddInputPort(int id, PortType type, string name) => AddPort(true, id, type, name);

    /// <summary>Removes one input port and renumbers subsequent ports.</summary>
    /// <param name="id">An existing zero-based port index.</param>
    /// <remarks>Connection indices remain stored in borrowing graphs. Default conversion can throw before mutation; floating defaults must remain finite.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">An index or value role is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public void RemoveInputPort(int id) => RemovePort(true, id);

    /// <summary>Renames one input port without changing its index or value role.</summary>
    /// <param name="id">An existing zero-based port index.</param>
    /// <param name="name">A unique ASCII name; selecting the same name is quiet.</param>
    /// <remarks>Connection indices remain stored in borrowing graphs. Default conversion can throw before mutation; floating defaults must remain finite.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">An index or value role is invalid.</exception>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ArgumentException">The name is invalid or already used.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public void SetInputPortName(int id, string name) => RenamePort(true, id, name);

    /// <summary>Changes one input port value role atomically.</summary>
    /// <param name="id">An existing zero-based port index.</param>
    /// <param name="type">The exact value role, excluding Max; selecting the same role is quiet.</param>
    /// <remarks>Connection indices remain stored in borrowing graphs. Default conversion can throw before mutation; floating defaults must remain finite.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">An index or value role is invalid.</exception>
    /// <exception cref="OverflowException">An existing default cannot convert to the selected integer type.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public void SetInputPortType(int id, PortType type) => SetPortType(true, id, type);

    /// <summary>Clears all input ports and their numeric defaults.</summary>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public void ClearInputPorts() => ClearPorts(true);

    /// <summary>Returns the canonical ordered input port description.</summary>
    /// <returns>Rows formatted as id,type,name; using numeric PortType identities, with no spaces added.</returns>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public string GetInputs() => PortDescription(true);

    /// <summary>Atomically replaces the complete input port description.</summary>
    /// <param name="inputs">Semicolon-separated id,type,name rows with contiguous IDs starting at zero.</param>
    /// <remarks>A final semicolon is optional. Get returns canonical rows. Existing defaults retain their indexed values and convert to new roles; absent defaults remain absent. Malformed replacements preserve the prior state.</remarks>
    /// <exception cref="ArgumentNullException">The description is null.</exception>
    /// <exception cref="ArgumentException">Names, IDs, roles or the 4096-port budget are invalid.</exception>
    /// <exception cref="OverflowException">An existing default cannot convert to the selected integer type.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public void SetInputs(string inputs) => SetPorts(true, inputs);

    /// <summary>Inserts one output port and renumbers subsequent ports.</summary>
    /// <param name="id">A nonnegative insertion index; indices at or beyond the count append.</param>
    /// <param name="type">The exact value role, excluding Max.</param>
    /// <param name="name">A unique ASCII name across both port lists.</param>
    /// <remarks>Connection indices remain stored in borrowing graphs. Default conversion can throw before mutation; floating defaults must remain finite.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">An index or value role is invalid.</exception>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ArgumentException">The name is invalid or already used.</exception>
    /// <exception cref="InvalidOperationException">The 4096-port bound is reached.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public void AddOutputPort(int id, PortType type, string name) => AddPort(false, id, type, name);

    /// <summary>Removes one output port and renumbers subsequent ports.</summary>
    /// <param name="id">An existing zero-based port index.</param>
    /// <remarks>Connection indices remain stored in borrowing graphs. Default conversion can throw before mutation; floating defaults must remain finite.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">An index or value role is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public void RemoveOutputPort(int id) => RemovePort(false, id);

    /// <summary>Renames one output port without changing its index or value role.</summary>
    /// <param name="id">An existing zero-based port index.</param>
    /// <param name="name">A unique ASCII name; selecting the same name is quiet.</param>
    /// <remarks>Connection indices remain stored in borrowing graphs. Default conversion can throw before mutation; floating defaults must remain finite.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">An index or value role is invalid.</exception>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ArgumentException">The name is invalid or already used.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public void SetOutputPortName(int id, string name) => RenamePort(false, id, name);

    /// <summary>Changes one output port value role atomically.</summary>
    /// <param name="id">An existing zero-based port index.</param>
    /// <param name="type">The exact value role, excluding Max; selecting the same role is quiet.</param>
    /// <remarks>Connection indices remain stored in borrowing graphs. Default conversion can throw before mutation; floating defaults must remain finite.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">An index or value role is invalid.</exception>
    /// <exception cref="OverflowException">An existing default cannot convert to the selected integer type.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public void SetOutputPortType(int id, PortType type) => SetPortType(false, id, type);

    /// <summary>Clears all output ports.</summary>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public void ClearOutputPorts() => ClearPorts(false);

    /// <summary>Returns the canonical ordered output port description.</summary>
    /// <returns>Rows formatted as id,type,name; using numeric PortType identities, with no spaces added.</returns>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public string GetOutputs() => PortDescription(false);

    /// <summary>Atomically replaces the complete output port description.</summary>
    /// <param name="outputs">Semicolon-separated id,type,name rows with contiguous IDs starting at zero.</param>
    /// <remarks>A final semicolon is optional. Get returns canonical rows. Existing defaults retain their indexed values and convert to new roles; absent defaults remain absent. Malformed replacements preserve the prior state.</remarks>
    /// <exception cref="ArgumentNullException">The description is null.</exception>
    /// <exception cref="ArgumentException">Names, IDs, roles or the 4096-port budget are invalid.</exception>
    /// <exception cref="OverflowException">An existing default cannot convert to the selected integer type.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public void SetOutputs(string outputs) => SetPorts(false, outputs);

    internal override void CopyDefinitionTo(VisualShaderNode target)
    {
        base.CopyDefinitionTo(target); var copy = (VisualShaderNodeGroupBase)target;
        copy._inputPorts = (GroupPort[])_inputPorts.Clone(); copy._outputPorts = (GroupPort[])_outputPorts.Clone();
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        new PropertyDescriptor[] {
            new PropertyDescriptor<VisualShaderNodeGroupBase,string>("Inputs", n => n.GetInputs(), (n,v) => n.SetInputs(v), stored:true),
            new PropertyDescriptor<VisualShaderNodeGroupBase,string>("Outputs", n => n.GetOutputs(), (n,v) => n.SetOutputs(v), stored:true)
        }.Concat(base.GetPropertyDescriptors());
}
