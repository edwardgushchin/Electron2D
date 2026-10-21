namespace Electron2D;

/// <summary>A typed numeric operation in an offline-compiled visual shader graph.</summary>
/// <remarks>Nodes are borrowed by graphs. Authoring is cold, single-threaded work. Edits emit Changed and
/// affect the next import; existing compiled shader programs retain their immutable code.</remarks>
public abstract class VisualShaderNode : Resource
{
    /// <summary>The numeric value role carried by a graph port.</summary>
    public enum PortType
    {
        /// <summary>A single-precision scalar.</summary>
        Scalar = 0,
        /// <summary>A signed 32-bit scalar.</summary>
        ScalarInt = 1,
        /// <summary>An unsigned 32-bit scalar.</summary>
        ScalarUInt = 2,
        /// <summary>Two numeric floating components.</summary>
        Vector2 = 3,
        /// <summary>Three numeric floating components.</summary>
        Vector3 = 4,
        /// <summary>Four numeric floating components.</summary>
        Vector4 = 5,
        /// <summary>A Boolean scalar.</summary>
        Boolean = 6,
        /// <summary>The nonselectable upper bound of the complete logical port type table.</summary>
        Max = 9,
    }

    private readonly Dictionary<int, VisualShaderPortDefault> _defaults = [];
    private int[]? _fileDefaultPorts;
    internal abstract PortType[] Inputs { get; }
    internal abstract PortType[] Outputs { get; }
    internal abstract string Expression(int output, IReadOnlyList<string> inputs);
    internal abstract void CopyDefinitionTo(VisualShaderNode target);

    /// <summary>Initializes a numeric graph node.</summary>
    protected VisualShaderNode() { }

    /// <summary>Gets the default connection target used when adding this node from an existing port.</summary>
    /// <param name="type">The originating numeric port type.</param>
    /// <returns>Input zero, as specified by the base node connection policy.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The numeric port type is undefined.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public int GetDefaultInputPort(PortType type)
    { ThrowIfDisposed(); if (!Enum.IsDefined(type) || type == PortType.Max) throw new ArgumentOutOfRangeException(nameof(type)); return 0; }

    /// <summary>Copies a typed default into an input port.</summary>
    /// <typeparam name="T">The exact numeric input type; Color also represents a four-component port.</typeparam>
    /// <param name="port">The input index.</param>
    /// <param name="value">The finite numeric default.</param>
    /// <exception cref="ArgumentException">The value has the wrong type or nonfinite components.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The input does not exist.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public void SetInputPortDefaultValue<T>(int port, T value) where T : unmanaged
    {
        ThrowIfDisposed(); ValidatePort(port); VisualShaderValues.Require(Inputs[port], value);
        _defaults[port] = VisualShaderValues.Entry(port, value); EmitChanged();
    }

    /// <summary>Converts a previous numeric default to a newly selected port value type.</summary>
    /// <typeparam name="T">The target port value type.</typeparam>
    /// <typeparam name="TPrevious">The previous numeric value type.</typeparam>
    /// <param name="port">The input index.</param>
    /// <param name="value">The target role and fallback value.</param>
    /// <param name="previousValue">The old numeric value; expanding a vector repeats its last component. Boolean conversion retains the fallback.</param>
    /// <exception cref="ArgumentException">Either value is invalid or the target type does not match.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The input does not exist.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public void SetInputPortDefaultValue<T, TPrevious>(int port, T value, TPrevious previousValue)
        where T : unmanaged where TPrevious : unmanaged
    {
        ThrowIfDisposed(); ValidatePort(port); VisualShaderValues.Require(Inputs[port], value);
        VisualShaderValues.Validate(previousValue);
        SetInputPortDefaultValue(port, VisualShaderValues.ConvertDefault(value, previousValue));
    }

    /// <summary>Reads a copied authored input default.</summary>
    /// <typeparam name="T">The exact numeric port type, or Color for a four-component port.</typeparam>
    /// <param name="port">The input index.</param>
    /// <returns>The authored value, or null after its default entry has been removed.</returns>
    /// <exception cref="ArgumentException">The requested type does not match the input.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The input does not exist.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public T? GetInputPortDefaultValue<T>(int port) where T : unmanaged
    {
        ThrowIfDisposed(); ValidatePort(port); VisualShaderValues.Require(Inputs[port], default(T));
        return _defaults.TryGetValue(port, out var entry) ? VisualShaderValues.Cast<T>(entry.BoxedValue) : null;
    }

    /// <summary>Returns immutable typed copies of all explicitly authored input defaults.</summary>
    /// <returns>Entries in ascending input order using canonical port types. Absent defaults are omitted.</returns>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public IReadOnlyList<VisualShaderPortDefault> GetDefaultInputValues()
    { ThrowIfDisposed(); return Array.AsReadOnly(_defaults.OrderBy(p => p.Key).Select(p => p.Value).ToArray()); }

    /// <summary>Atomically replaces the complete explicit input-default table.</summary>
    /// <param name="values">Unique, correctly typed entries for existing inputs; an empty collection clears the table.</param>
    /// <exception cref="ArgumentNullException">The collection or an entry is null.</exception>
    /// <exception cref="ArgumentException">An entry is duplicated, mistyped or invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An input does not exist.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public void SetDefaultInputValues(IReadOnlyList<VisualShaderPortDefault> values)
    {
        ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(values);
        if (values.Count > Inputs.Length) throw new ArgumentException("Too many input defaults.", nameof(values));
        var replacement = new Dictionary<int, VisualShaderPortDefault>();
        foreach (var value in values)
        {
            ArgumentNullException.ThrowIfNull(value); ValidatePort(value.Port);
            VisualShaderValues.Require(Inputs[value.Port], value.BoxedValue);
            if (!replacement.TryAdd(value.Port, VisualShaderValues.Entry(value.Port, value.BoxedValue))) throw new ArgumentException("Duplicate input default.", nameof(values));
        }
        ThrowIfDisposed(); _defaults.Clear(); foreach (var pair in replacement) _defaults.Add(pair.Key, pair.Value); EmitChanged();
    }

    /// <summary>Removes one explicit input default, restoring the operation's initial value.</summary>
    /// <param name="port">The input index.</param>
    /// <exception cref="ArgumentOutOfRangeException">The input does not exist.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public void RemoveInputPortDefaultValue(int port)
    { ThrowIfDisposed(); ValidatePort(port); if (_defaults.Remove(port)) EmitChanged(); }

    /// <summary>Clears all explicit input defaults.</summary>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public void ClearDefaultInputValues()
    { ThrowIfDisposed(); if (_defaults.Count != 0) { _defaults.Clear(); EmitChanged(); } }

    internal void ChangeDefaultWidth(PortType type)
    {
        var values = new Dictionary<int, VisualShaderPortDefault>();
        for (var port = 0; port < Inputs.Length; port++)
        {
            var fallback = type == PortType.Vector4 ? (object)new Vector4(0, 0, 0, 1) : VisualShaderValues.Zero(type);
            var value = _defaults.TryGetValue(port, out var previous) ? VisualShaderValues.ConvertDefaultValue(fallback, previous.BoxedValue) : fallback;
            values.Add(port, VisualShaderValues.Entry(port, value));
        }
        _defaults.Clear(); foreach (var pair in values) _defaults.Add(pair.Key, pair.Value);
    }
    internal virtual object InitialDefault(int port) => VisualShaderValues.Zero(Inputs[port]);
    internal void InitializeDefault<T>(int port, T value) where T : unmanaged => _defaults[port] = VisualShaderValues.Entry(port, value);
    internal string InputDefaultExpression(int port) => VisualShaderValues.Literal(
        _defaults.TryGetValue(port, out var value) ? value.BoxedValue : InitialDefault(port));
    private void ValidatePort(int port)
    { if ((uint)port >= (uint)Inputs.Length) throw new ArgumentOutOfRangeException(nameof(port)); }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var descriptor in base.GetPropertyDescriptors()) yield return descriptor;
        yield return new PropertyDescriptor<VisualShaderNode, int[]>("DefaultPorts", n => n._defaults.Keys.Order().ToArray(),
            (n, ports) => n.RestoreDefaultPorts(ports), stored: true);
        for (var index = 0; index < Inputs.Length; index++)
        {
            var port = index;
            yield return Inputs[index] switch
            {
                PortType.Scalar => DefaultDescriptor<float>(port),
                PortType.ScalarInt => DefaultDescriptor<int>(port),
                PortType.ScalarUInt => DefaultDescriptor<uint>(port),
                PortType.Boolean => DefaultDescriptor<bool>(port),
                PortType.Vector2 => DefaultDescriptor<Vector2>(port),
                PortType.Vector3 => DefaultDescriptor<Vector3>(port),
                PortType.Vector4 => DefaultDescriptor<Vector4>(port),
                _ => throw new InvalidOperationException("Invalid input type.")
            };
        }
    }

    private PropertyDescriptor DefaultDescriptor<T>(int port) where T : unmanaged =>
        new PropertyDescriptor<VisualShaderNode, T>($"InputDefault[{port}]", n => n.GetInputPortDefaultValue<T>(port) ?? default,
            (n, v) => n.SetInputPortDefaultValue(port, v), stored: true)
        { StoreCondition = n => n._defaults.ContainsKey(port) };

    private void RestoreDefaultPorts(int[] ports)
    {
        ArgumentNullException.ThrowIfNull(ports);
        if (ports.Length > Inputs.Length || ports.Distinct().Count() != ports.Length || ports.Any(p => p < 0 || p >= Inputs.Length))
            throw new InvalidDataException("Invalid input-default table.");
        _defaults.Clear(); _fileDefaultPorts = (int[])ports.Clone();
    }
    internal override void ValidateFileState()
    {
        base.ValidateFileState();
        if (_fileDefaultPorts is not null && !_defaults.Keys.Order().SequenceEqual(_fileDefaultPorts.Order()))
            throw new InvalidDataException("The visual shader default table is incomplete.");
        _fileDefaultPorts = null;
    }

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var copy = (VisualShaderNode)target; copy._defaults.Clear();
        foreach (var pair in _defaults) copy._defaults.Add(pair.Key, pair.Value);
        CopyDefinitionTo(copy);
    }
}
