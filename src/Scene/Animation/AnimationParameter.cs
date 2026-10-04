namespace Electron2D;

/// <summary>Immutable animation-domain parameter metadata with no untyped value accessor.</summary>
/// <remarks>Values belong to individual AnimationTree instances. Definitions may be shared by graph resources.</remarks>
public abstract class AnimationParameter
{
    /// <summary>Initializes a named parameter definition.</summary>
    /// <param name="name">The nonblank local parameter name.</param>
    /// <param name="valueType">The exact value type.</param>
    /// <param name="readOnly">Whether external tree callers may write this parameter.</param>
    /// <exception cref="ArgumentException">The parameter name is null or blank.</exception>
    /// <exception cref="ArgumentNullException">The value type is null.</exception>
    protected AnimationParameter(string name, Type valueType, bool readOnly)
    { ArgumentException.ThrowIfNullOrWhiteSpace(name); ArgumentNullException.ThrowIfNull(valueType); Name = name; ValueType = valueType; IsReadOnly = readOnly; }
    /// <summary>Gets the exact local name.</summary>
    /// <value>The typed value described in the summary.</value>
    public string Name { get; }
    /// <summary>Gets the exact parameter value type.</summary>
    /// <value>The typed value described in the summary.</value>
    public Type ValueType { get; }
    /// <summary>Gets whether external callers may write the parameter.</summary>
    /// <value>The typed value described in the summary.</value>
    public bool IsReadOnly { get; }
    internal abstract AnimationParameterSlot CreateSlot(AnimationNode node);
}
/// <summary>A typed key and default value for per-tree animation-node memory.</summary>
/// <typeparam name="TValue">The exact value type; mutable custom references remain caller-owned.</typeparam>
public sealed class AnimationParameter<TValue> : AnimationParameter
{
    private readonly TValue _defaultValue;
    /// <summary>Defines a typed animation parameter.</summary>
    /// <param name="name">The local nonblank name.</param>
    /// <param name="defaultValue">The initial value; array containers are copied for each tree.</param>
    /// <param name="readOnly">Whether external tree callers may write the parameter.</param>
    /// <exception cref="ArgumentException">The parameter name is null or blank.</exception>
    public AnimationParameter(string name, TValue defaultValue, bool readOnly = false) : base(name, typeof(TValue), readOnly) { _defaultValue = defaultValue; }
    /// <summary>Returns the default value, with an independent array container when applicable.</summary>
    /// <returns>The typed default; custom mutable references and resources remain borrowed.</returns>
    public TValue GetDefaultValue() => _defaultValue is Array array ? (TValue)(object)array.Clone() : _defaultValue;
    internal override AnimationParameterSlot CreateSlot(AnimationNode node) => new AnimationParameterSlot<TValue>(this, node.ParameterDefault(this));
}
internal abstract class AnimationParameterSlot(AnimationParameter parameter) { internal readonly AnimationParameter Parameter = parameter; }
internal sealed class AnimationParameterSlot<T>(AnimationParameter<T> parameter, T value) : AnimationParameterSlot(parameter) { internal T Value = value; }
