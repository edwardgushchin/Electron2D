namespace Electron2D;

public partial class ElectronObject
{
    /// <summary>Reads a property or composed component through its compiled typed descriptor.</summary>
    /// <typeparam name="TOwner">The compatible root owner type.</typeparam><typeparam name="TValue">The exact property value type.</typeparam>
    /// <param name="propertyPath">A direct or composed descriptor; its name is metadata, not a runtime lookup key.</param>
    /// <returns>The current value; references remain borrowed.</returns>
    /// <exception cref="ArgumentNullException">The descriptor is null.</exception><exception cref="ArgumentException">This object does not match the declared owner.</exception>
    /// <exception cref="ObjectDisposedException">This object or a traversed object is disposed.</exception><exception cref="InvalidOperationException">An attached query runs off-owner or a link is null.</exception>
    /// <exception cref="Exception">A getter, component or collection operation fails.</exception>
    public TValue GetIndexed<TOwner, TValue>(PropertyDescriptor<TOwner, TValue> propertyPath) where TOwner : ElectronObject
    { ArgumentNullException.ThrowIfNull(propertyPath); return propertyPath.GetValue(IndexedOwner<TOwner>(false)); }
    /// <summary>Writes a property or composed component through its compiled typed setters and validators.</summary>
    /// <typeparam name="TOwner">The compatible root owner type.</typeparam><typeparam name="TValue">The exact property value type.</typeparam>
    /// <param name="propertyPath">A direct or composed descriptor.</param><param name="value">The typed replacement value.</param>
    /// <remarks>Composition may commit an inner object before an outer setter fails. Existing setter notifications and failure behavior remain observable.</remarks>
    /// <exception cref="ArgumentNullException">The descriptor is null.</exception><exception cref="ArgumentException">The owner or value is invalid.</exception>
    /// <exception cref="ObjectDisposedException">This object or a traversed object is disposed.</exception><exception cref="InvalidOperationException">The path is readonly/null, or mutation violates owner/capture/lifetime rules.</exception>
    /// <exception cref="Exception">A getter, setter, validator, component or collection operation fails.</exception>
    public void SetIndexed<TOwner, TValue>(PropertyDescriptor<TOwner, TValue> propertyPath, TValue value) where TOwner : ElectronObject
    { ArgumentNullException.ThrowIfNull(propertyPath); propertyPath.SetValue(IndexedOwner<TOwner>(true), value); }
    private TOwner IndexedOwner<TOwner>(bool mutation) where TOwner : ElectronObject
    {
        ThrowIfDisposed();
        if (this is not TOwner owner) throw new ArgumentException($"Indexed property requires {typeof(TOwner).Name}.");
        if (this is Node node) { if (mutation) node.EnsureIndexedMutation(); else node.Tree?.EnsureOwnerThread(); }
        else if (mutation) ValidateMutation();
        return owner;
    }
}
