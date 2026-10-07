using System.Runtime.CompilerServices;

namespace Electron2D;

/// <summary>Describes a typed property exposed to Electron2D tooling.</summary>
/// <remarks>
/// This is the non-generic discovery surface used in property lists. It contains immutable metadata and revert
/// operations but deliberately exposes no Variant-like untyped getter or setter.
/// </remarks>
public abstract class PropertyDescriptor
{
    /// <summary>Initializes immutable metadata for a tooling property.</summary>
    /// <param name="name">The nonblank property name.</param>
    /// <param name="ownerType">
    /// The runtime owner type accepted by this descriptor. The base constructor checks only for null; custom derived
    /// descriptors are responsible for supplying an <see cref="ElectronObject"/>-compatible type.
    /// </param>
    /// <param name="valueType">The exact type of the property value.</param>
    /// <param name="isReadOnly">Whether the descriptor has no setter.</param>
    /// <param name="isStored">Whether packed scenes should store this property when it belongs to a node.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or consists only of whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="name"/>, <paramref name="ownerType"/>, or <paramref name="valueType"/> is <see langword="null"/>.</exception>
    protected PropertyDescriptor(string name, Type ownerType, Type valueType, bool isReadOnly, bool isStored = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(ownerType);
        ArgumentNullException.ThrowIfNull(valueType);

        Name = name;
        OwnerType = ownerType;
        ValueType = valueType;
        IsReadOnly = isReadOnly;
        IsStored = isStored;
    }

    /// <summary>Gets the property name.</summary>
    /// <value>A nonblank name that is immutable for the descriptor's lifetime.</value>
    public string Name { get; }

    /// <summary>Gets the runtime owner type accepted by this descriptor.</summary>
    /// <value>
    /// The required owner type. <see cref="PropertyDescriptor{TOwner,TValue}"/> always supplies an
    /// <see cref="ElectronObject"/> subtype; the protected base constructor does not enforce that constraint.
    /// </value>
    public Type OwnerType { get; }

    /// <summary>Gets the property's exact value type.</summary>
    /// <value>The value type supplied when the descriptor was constructed.</value>
    public Type ValueType { get; }

    /// <summary>Gets whether the property has no setter.</summary>
    /// <value><see langword="true"/> for a read-only descriptor; otherwise <see langword="false"/>.</value>
    public bool IsReadOnly { get; }

    /// <summary>Gets whether packed scenes should store this property when its owner is a node.</summary>
    /// <value><see langword="true"/> only for an explicitly storage-enabled writable descriptor.</value>
    public bool IsStored { get; }
    internal bool AlwaysDuplicateResource { get; init; }

    /// <summary>Determines whether a compatible live owner's value currently differs from its revert value.</summary>
    /// <param name="owner">The owner whose property is inspected.</param>
    /// <returns><see langword="true"/> when the property can currently be reverted; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="owner"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="owner"/> is not assignable to <see cref="OwnerType"/>.</exception>
    /// <exception cref="ObjectDisposedException">Disposal of <paramref name="owner"/> has started.</exception>
    /// <exception cref="Exception">A configured getter or revert-value delegate throws.</exception>
    public abstract bool CanRevert(ElectronObject owner);

    /// <summary>Restores a compatible live owner's property to its current revert value.</summary>
    /// <param name="owner">The owner whose property is restored.</param>
    /// <exception cref="ArgumentNullException"><paramref name="owner"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="owner"/> is not assignable to <see cref="OwnerType"/>.</exception>
    /// <exception cref="InvalidOperationException">The descriptor has no writable revert value.</exception>
    /// <exception cref="ObjectDisposedException">Disposal of <paramref name="owner"/> has started.</exception>
    /// <exception cref="Exception">A configured getter, revert-value, validator, or setter delegate throws.</exception>
    public abstract void Revert(ElectronObject owner);

    internal virtual StoredPropertyValue CaptureStoredValue(ElectronObject owner) =>
        throw new NotSupportedException($"Property descriptor '{Name}' does not support scene storage.");

    internal virtual void RestoreStoredValue(
        ElectronObject owner,
        StoredPropertyValue value,
        Func<Resource, Resource> resolveResource) =>
        throw new NotSupportedException($"Property descriptor '{Name}' does not support scene storage.");

    internal virtual void WriteFileValue(ElectronObject owner, StoredPropertyValue value, ResourceArchiveWrite context, StreamPeerBuffer stream) => throw new NotSupportedException("Descriptor has no file schema.");
    internal virtual StoredPropertyValue ReadFileValue(ResourceArchiveRead context, StreamPeerBuffer stream) => throw new NotSupportedException("Descriptor has no file schema.");

    internal void EnsureCompatible(ElectronObject owner)
    {
        ArgumentNullException.ThrowIfNull(owner);

        if (!OwnerType.IsInstanceOfType(owner))
            throw new ArgumentException($"Property '{Name}' requires an owner assignable to {OwnerType.Name}.", nameof(owner));
    }
}

/// <summary>Provides strongly typed access, validation, and revert behavior for a tooling property.</summary>
/// <typeparam name="TOwner">The <see cref="ElectronObject"/> subtype that owns the property.</typeparam>
/// <typeparam name="TValue">The property's value type.</typeparam>
/// <remarks>
/// Delegate execution is synchronous on the caller's thread. The descriptor is immutable, but access to an owner
/// follows that owner's threading rules. Packed-scene storage accepts unmanaged values, strings, resources,
/// node references on node owners, and the explicit vector, color, float, string and polygon-index array profiles. Node references
/// are stored as relative paths and resolved after scene construction. Stored arrays are cloned
/// during capture and restoration, and their revert comparison uses element values.
/// </remarks>
public sealed class PropertyDescriptor<TOwner, TValue> : PropertyDescriptor
    where TOwner : ElectronObject
{
    private readonly Func<TOwner, TValue> _getter;
    private readonly Action<TOwner, TValue>? _setter;
    private readonly Func<TOwner, TValue>? _revertValue;
    private readonly Func<TOwner, TValue, bool>? _validator;

    /// <summary>Initializes a strongly typed tooling property descriptor.</summary>
    /// <param name="name">The nonblank property name.</param>
    /// <param name="getter">The required value reader.</param>
    /// <param name="setter">An optional value writer. Omit it to create a read-only descriptor.</param>
    /// <param name="revertValue">An optional factory for the current revert value.</param>
    /// <param name="validator">An optional predicate evaluated before each write.</param>
    /// <param name="stored">Whether packed scenes should capture this property from node owners.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> is blank, or <paramref name="revertValue"/> is supplied without a
    /// <paramref name="setter"/>, or <paramref name="stored"/> is true without a setter.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="getter"/> is <see langword="null"/>.</exception>
    public PropertyDescriptor(
        string name,
        Func<TOwner, TValue> getter,
        Action<TOwner, TValue>? setter = null,
        Func<TOwner, TValue>? revertValue = null,
        Func<TOwner, TValue, bool>? validator = null,
        bool stored = false)
        : base(name, typeof(TOwner), typeof(TValue), setter is null, stored)
    {
        ArgumentNullException.ThrowIfNull(getter);

        if (revertValue is not null && setter is null)
            throw new ArgumentException("A revert value requires a property setter.", nameof(revertValue));

        if (stored && setter is null)
            throw new ArgumentException("A stored property requires a property setter.", nameof(stored));

        _getter = getter;
        _setter = setter;
        _revertValue = revertValue;
        _validator = validator;
    }

    /// <summary>Reads the property's current value from a compatible live owner.</summary>
    /// <param name="owner">The owner passed to the configured getter.</param>
    /// <returns>The current property value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="owner"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">Disposal of <paramref name="owner"/> has started.</exception>
    /// <exception cref="Exception">The configured getter throws.</exception>
    public TValue GetValue(TOwner owner)
    {
        EnsureAlive(owner);
        return _getter(owner);
    }

    /// <summary>Validates and writes a property value to a compatible live owner.</summary>
    /// <param name="owner">The owner passed to the configured validator and setter.</param>
    /// <param name="value">The value to validate and write.</param>
    /// <exception cref="ArgumentNullException"><paramref name="owner"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The configured validator rejects <paramref name="value"/>.</exception>
    /// <exception cref="InvalidOperationException">The descriptor is read-only.</exception>
    /// <exception cref="ObjectDisposedException">Disposal of <paramref name="owner"/> has started.</exception>
    /// <exception cref="Exception">The configured validator or setter throws.</exception>
    public void SetValue(TOwner owner, TValue value)
    {
        EnsureAlive(owner);

        if (_setter is null)
            throw new InvalidOperationException($"Property '{Name}' is read-only.");

        if (_validator is not null && !_validator(owner, value))
            throw new ArgumentOutOfRangeException(nameof(value), value, $"Value is invalid for property '{Name}'.");

        _setter(owner, value);
    }

    /// <summary>Attempts to compute the property's current revert value.</summary>
    /// <param name="owner">The owner passed to the configured revert-value factory.</param>
    /// <param name="value">Receives the revert value, or <see langword="default"/> when no factory exists.</param>
    /// <returns><see langword="true"/> when a revert-value factory exists; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="owner"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">Disposal of <paramref name="owner"/> has started.</exception>
    /// <exception cref="Exception">The configured revert-value factory throws.</exception>
    public bool TryGetRevertValue(TOwner owner, out TValue value)
    {
        EnsureAlive(owner);

        if (_revertValue is null)
        {
            value = default!;
            return false;
        }

        value = _revertValue(owner);
        return true;
    }

    /// <inheritdoc />
    public override bool CanRevert(ElectronObject owner)
    {
        var typedOwner = GetOwner(owner);
        return _setter is not null && _revertValue is not null && !ValuesEqual(_getter(typedOwner), _revertValue(typedOwner));
    }

    /// <inheritdoc />
    public override void Revert(ElectronObject owner)
    {
        var typedOwner = GetOwner(owner);

        if (!TryGetRevertValue(typedOwner, out var value))
            throw new InvalidOperationException($"Property '{Name}' has no revert value.");

        SetValue(typedOwner, value);
    }

    internal override StoredPropertyValue CaptureStoredValue(ElectronObject owner)
    {
        var typedOwner = GetOwner(owner);

        if (typeof(Node).IsAssignableFrom(typeof(TValue)))
        {
            if (owner is not Node node) throw new NotSupportedException("Stored node references require a node owner.");
            var referenced = _getter(typedOwner) as Node;
            return new StoredNodeReferenceValue(typeof(TValue), referenced is null ? null : node.GetPathTo(referenced));
        }

        if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>() &&
            typeof(TValue) != typeof(string) &&
            typeof(TValue) != typeof(string[]) &&
            typeof(TValue) != typeof(byte[]) &&
            typeof(TValue) != typeof(Dictionary<string, int>) &&
            !typeof(Resource[]).IsAssignableFrom(typeof(TValue)) &&
            typeof(TValue) != typeof(float[]) &&
            typeof(TValue) != typeof(int[]) &&
            typeof(TValue) != typeof(Vector2[]) &&
            typeof(TValue) != typeof(Color[]) &&
            typeof(TValue) != typeof(int[][]) &&
            !typeof(Resource).IsAssignableFrom(typeof(TValue)) &&
            !ResourceFileTypes.HasCodec(typeof(TValue)))
        {
            throw new NotSupportedException(
                $"Stored property '{Name}' uses unsupported reference-shaped type {typeof(TValue).FullName}.");
        }

        var value = _getter(typedOwner);
        return new StoredPropertyValue<TValue>(StoredPropertyValue<TValue>.Snapshot(value));
    }

    internal override void RestoreStoredValue(
        ElectronObject owner,
        StoredPropertyValue value,
        Func<Resource, Resource> resolveResource)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(resolveResource);

        if (value is StoredNodeReferenceValue reference && reference.ValueType == typeof(TValue) && owner is Node node)
        {
            var resolved = reference.Path is null ? null : node.GetNodeOrNull(reference.Path);
            if (resolved is not null && resolved is not TValue)
                throw new InvalidOperationException($"Stored node reference '{Name}' resolves to an incompatible node type.");
            SetValue(GetOwner(owner), resolved is null ? default! : (TValue)(object)resolved);
            return;
        }

        if (value is not StoredPropertyValue<TValue> typedValue)
            throw new InvalidOperationException($"Stored property '{Name}' has an incompatible value type.");

        SetValue(GetOwner(owner), typedValue.Resolve(resolveResource));
    }

    private static void EnsureAlive(TOwner owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ObjectDisposedException.ThrowIf(owner.IsDisposed, owner);
    }

    internal override void WriteFileValue(ElectronObject owner, StoredPropertyValue value, ResourceArchiveWrite context, StreamPeerBuffer stream)
    {
        if (value is StoredNodeReferenceValue reference && reference.ValueType == typeof(TValue)) { ResourceFileTypes.Codec<string>().Write(stream, reference.Path!); return; }
        if (!value.TryGetValue<TValue>(out var typed)) throw new InvalidOperationException("File property value does not match its declared schema.");
        if (typeof(Resource).IsAssignableFrom(typeof(TValue))) stream.Put32(context.Reference(typed as Resource));
        else if (typeof(Resource[]).IsAssignableFrom(typeof(TValue))) { var resources = typed as Resource[]; stream.Put32(resources is null ? -1 : resources.Length); if (resources is not null) foreach (var resource in resources) stream.Put32(context.Reference(resource)); }
        else ResourceFileTypes.Codec<TValue>().Write(stream, typed);
    }
    internal override StoredPropertyValue ReadFileValue(ResourceArchiveRead context, StreamPeerBuffer stream)
    {
        if (typeof(Node).IsAssignableFrom(typeof(TValue))) return new StoredNodeReferenceValue(typeof(TValue), ResourceFileTypes.Codec<string>().Read(stream));
        if (typeof(Resource).IsAssignableFrom(typeof(TValue))) { var resource = context.Reference(stream.Get32()); if (resource is not null && resource is not TValue) throw new InvalidDataException("Resource reference does not match property type."); return new StoredPropertyValue<TValue>(resource is null ? default! : (TValue)(object)resource); }
        if (typeof(Resource[]).IsAssignableFrom(typeof(TValue))) return new StoredPropertyValue<TValue>((TValue)(object)ResourceFileTypes.ResourceArray(typeof(TValue)).Read(stream, context));
        return new StoredPropertyValue<TValue>(ResourceFileTypes.Codec<TValue>().Read(stream));
    }

    private TOwner GetOwner(ElectronObject owner)
    {
        EnsureCompatible(owner);
        ObjectDisposedException.ThrowIf(owner.IsDisposed, owner);
        return (TOwner)owner;
    }

    private static bool ValuesEqual(TValue left, TValue right)
    {
        if (left is Resource?[] resources && right is Resource?[] otherResources) return resources.AsSpan().SequenceEqual(otherResources);
        if (left is byte[] bytes && right is byte[] otherBytes) return bytes.AsSpan().SequenceEqual(otherBytes);
        if (left is Dictionary<string, int> map && right is Dictionary<string, int> otherMap) return map.Count == otherMap.Count && map.All(p => otherMap.TryGetValue(p.Key, out var v) && v == p.Value);
        if (left is string[] strings && right is string[] otherStrings) return strings.AsSpan().SequenceEqual(otherStrings);
        if (left is int[] integers && right is int[] otherIntegers) return integers.AsSpan().SequenceEqual(otherIntegers);
        if (left is float[] numbers && right is float[] otherNumbers) return numbers.AsSpan().SequenceEqual(otherNumbers);
        if (left is Vector2[] points && right is Vector2[] otherPoints) return points.AsSpan().SequenceEqual(otherPoints);
        if (left is Color[] colors && right is Color[] otherColors) return colors.AsSpan().SequenceEqual(otherColors);
        if (left is int[][] contours && right is int[][] otherContours)
            return contours.Length == otherContours.Length &&
                contours.Zip(otherContours).All(pair => pair.First.AsSpan().SequenceEqual(pair.Second));
        return EqualityComparer<TValue>.Default.Equals(left, right);
    }
}

internal abstract class StoredPropertyValue
{
    internal abstract Type ValueType { get; }

    internal abstract bool TryGetValue<TValue>(out TValue value);

    internal abstract StoredPropertyValue TransformResources(Func<Resource, Resource> transform);
}

internal sealed class StoredNodeReferenceValue(Type type, string? path) : StoredPropertyValue
{
    internal string? Path { get; } = path;
    internal override Type ValueType => type;
    internal override bool TryGetValue<TValue>(out TValue value)
    {
        if (typeof(TValue) == typeof(string)) { value = (TValue)(object)(Path ?? string.Empty); return true; }
        value = default!; return false;
    }
    internal override StoredPropertyValue TransformResources(Func<Resource, Resource> transform) => this;
}

internal sealed class StoredPropertyValue<TValue>(TValue value) : StoredPropertyValue
{
    internal override Type ValueType => typeof(TValue);

    internal TValue Value { get; } = value;

    internal TValue Resolve(Func<Resource, Resource> resolveResource) =>
        Transform(Value, resolveResource);

    internal override bool TryGetValue<TRequested>(out TRequested value)
    {
        if (Value is TRequested requested)
        {
            value = StoredPropertyValue<TRequested>.Snapshot(requested);
            return true;
        }

        if (Value is null && default(TRequested) is null)
        {
            value = default!;
            return true;
        }

        value = default!;
        return false;
    }

    internal override StoredPropertyValue TransformResources(Func<Resource, Resource> transform) =>
        new StoredPropertyValue<TValue>(Transform(Value, transform));

    private static TValue Transform(TValue value, Func<Resource, Resource> transform)
    {
        if (value is Resource resource) return (TValue)(object)transform(resource);
        if (value is Resource?[] resources) { var copy = (Resource?[])resources.Clone(); for (var i = 0; i < copy.Length; i++) if (copy[i] is { } item) copy[i] = transform(item); return (TValue)(object)copy; }
        return Snapshot(value);
    }
    internal static TValue Snapshot(TValue value) => value switch
    {
        Resource[] resources => (TValue)(object)resources.Clone(),
        byte[] bytes => (TValue)(object)bytes.Clone(),
        Dictionary<string, int> dictionary => (TValue)(object)new Dictionary<string, int>(dictionary, dictionary.Comparer),
        string[] strings => (TValue)(object)strings.Clone(),
        float[] numbers => (TValue)(object)numbers.Clone(),
        int[] integers => (TValue)(object)integers.Clone(),
        Vector2[] points => (TValue)(object)points.Clone(),
        Color[] colors => (TValue)(object)colors.Clone(),
        int[][] contours => (TValue)(object)contours.Select(indices => (int[])indices.Clone()).ToArray(),
        _ => SnapshotCustom(value),
    };
    private static TValue SnapshotCustom(TValue value)
    {
        if (!RuntimeHelpers.IsReferenceOrContainsReferences<TValue>() || value is null or string or Resource) return value;
        var codec = ResourceFileTypes.Codec<TValue>(); using var stream = new StreamPeerBuffer(); codec.Write(stream, value); stream.Seek(0); var copy = codec.Read(stream); if (stream.GetAvailableBytes() != 0) throw new InvalidDataException("Custom snapshot codec left trailing bytes."); return copy;
    }
}
