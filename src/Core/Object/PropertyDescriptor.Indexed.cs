namespace Electron2D;

public abstract partial class PropertyDescriptor
{
    /// <summary>Composes an object reference and its typed property into a live root-owner projection.</summary>
    /// <typeparam name="TOwner">The root owner type.</typeparam><typeparam name="TChild">The referenced object type.</typeparam><typeparam name="TValue">The leaf value type.</typeparam>
    /// <param name="parent">The current object-reference descriptor.</param><param name="property">The leaf descriptor.</param>
    /// <returns>A non-stored projection with no invented revert default; it is readonly when either descriptor is readonly.</returns>
    /// <remarks>Each access resolves the current object. Writes invoke the child setter followed by the parent setter, retaining validation and notification behavior; an outer failure does not roll back a committed inner write.</remarks>
    /// <exception cref="ArgumentNullException">A descriptor is null.</exception>
    public static PropertyDescriptor<TOwner, TValue> Through<TOwner, TChild, TValue>(PropertyDescriptor<TOwner, TChild?> parent, PropertyDescriptor<TChild, TValue> property)
        where TOwner : ElectronObject where TChild : ElectronObject
    {
        ArgumentNullException.ThrowIfNull(parent); ArgumentNullException.ThrowIfNull(property);
        return new(parent.Name + ":" + property.Name,
            owner => property.GetValue(Child(parent.GetValue(owner), false)),
            parent.IsReadOnly || property.IsReadOnly ? null : (owner, value) => { var child = Child(parent.GetValue(owner), true); property.SetValue(child, value); parent.SetValue(owner, child); })
        { Hint = property.Hint, HintString = property.HintString };
    }
    /// <summary>Composes a typed struct member into its owning property.</summary>
    /// <typeparam name="TOwner">The root owner type.</typeparam><typeparam name="TCompound">The compound value type.</typeparam><typeparam name="TValue">The member value type.</typeparam>
    /// <param name="parent">The compound property descriptor.</param><param name="name">The member's metadata label.</param><param name="getter">The compiled member reader.</param><param name="setter">The compiled value-copy updater, or null for a readonly member.</param>
    /// <returns>A non-stored projection; writes validate and commit the complete updated compound through the parent setter.</returns>
    /// <remarks>No reflection or label-based dispatch is performed. The updater must return the updated value without mutating reference-shaped state held inside it.</remarks>
    /// <exception cref="ArgumentNullException">Parent or getter is null.</exception><exception cref="ArgumentException">The label is blank.</exception>
    public static PropertyDescriptor<TOwner, TValue> Member<TOwner, TCompound, TValue>(PropertyDescriptor<TOwner, TCompound> parent, string name, Func<TCompound, TValue> getter, Func<TCompound, TValue, TCompound>? setter = null)
        where TOwner : ElectronObject where TCompound : struct
    {
        ArgumentNullException.ThrowIfNull(parent); ArgumentNullException.ThrowIfNull(getter); ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new(parent.Name + ":" + name, owner => getter(parent.GetValue(owner)),
            parent.IsReadOnly || setter is null ? null : (owner, value) => parent.SetValue(owner, setter(parent.GetValue(owner), value)));
    }
    /// <summary>Projects one typed array element through its owning property.</summary>
    /// <typeparam name="TOwner">The root owner type.</typeparam><typeparam name="TValue">The array element type.</typeparam>
    /// <param name="parent">The array descriptor.</param><param name="index">The fixed nonnegative element index.</param>
    /// <returns>A non-stored projection; writes copy the current array before invoking its parent setter.</returns>
    /// <remarks>Bounds are checked on every access. Validation failure before parent commitment leaves the original array unchanged. Array covariance retains ordinary runtime element-type checks.</remarks>
    /// <exception cref="ArgumentNullException">Parent is null.</exception><exception cref="ArgumentOutOfRangeException">Index is negative.</exception>
    public static PropertyDescriptor<TOwner, TValue> Element<TOwner, TValue>(PropertyDescriptor<TOwner, TValue[]> parent, int index) where TOwner : ElectronObject
    {
        ArgumentNullException.ThrowIfNull(parent); ArgumentOutOfRangeException.ThrowIfNegative(index);
        TValue[] Array(TOwner owner) { var array = parent.GetValue(owner) ?? throw new InvalidOperationException("Indexed array is null."); if ((uint)index >= (uint)array.Length) throw new ArgumentOutOfRangeException(nameof(index)); return array; }
        return new(parent.Name + $":{index}", owner => Array(owner)[index], parent.IsReadOnly ? null : (owner, value) => { var copy = (TValue[])Array(owner).Clone(); copy[index] = value; parent.SetValue(owner, copy); });
    }
    /// <summary>Projects one typed dictionary key through its owning property.</summary>
    /// <typeparam name="TOwner">The root owner type.</typeparam><typeparam name="TKey">The non-null key type.</typeparam><typeparam name="TValue">The stored value type.</typeparam>
    /// <param name="parent">The concrete dictionary descriptor.</param><param name="key">The key used for each access.</param>
    /// <returns>A non-stored projection. Reads require the key; writes may add it and commit a copied dictionary retaining its comparer.</returns>
    /// <remarks>Validation failure before parent commitment leaves the original dictionary unchanged. The key's text is a metadata label only.</remarks>
    /// <exception cref="ArgumentNullException">Parent or key is null.</exception>
    public static PropertyDescriptor<TOwner, TValue> Key<TOwner, TKey, TValue>(PropertyDescriptor<TOwner, Dictionary<TKey, TValue>> parent, TKey key)
        where TOwner : ElectronObject where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(parent); ArgumentNullException.ThrowIfNull(key);
        Dictionary<TKey, TValue> Dictionary(TOwner owner) => parent.GetValue(owner) ?? throw new InvalidOperationException("Indexed dictionary is null.");
        return new(parent.Name + ":" + key, owner => Dictionary(owner)[key], parent.IsReadOnly ? null : (owner, value) => { var current = Dictionary(owner); var copy = new Dictionary<TKey, TValue>(current, current.Comparer) { [key] = value }; parent.SetValue(owner, copy); });
    }
    private static TChild Child<TChild>(TChild? child, bool mutation) where TChild : ElectronObject
    {
        if (child is null) throw new InvalidOperationException("Indexed object reference is null.");
        ObjectDisposedException.ThrowIf(child.IsDisposed, child);
        if (child is Node node) { if (mutation) node.EnsureIndexedMutation(); else node.Tree?.EnsureOwnerThread(); }
        return child;
    }
}
