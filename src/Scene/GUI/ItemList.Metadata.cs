namespace Electron2D;

public partial class ItemList
{
    private sealed class MetadataValue<T>(T value)
    {
        internal T Value = value;
    }

    /// <summary>Stores a runtime-only typed value for one item.</summary>
    /// <typeparam name="T">The exact value type used again when reading or finding metadata.</typeparam>
    /// <param name="index">An index; negative values count from the end.</param>
    /// <param name="value">The borrowed value; reference values are not copied or disposed.</param>
    public void SetItemMetadata<T>(int index, T value)
    {
        EnsureMutable(); GetItem(index, negative: true).Metadata = new MetadataValue<T>(value);
        InvalidateListLayout();
    }

    /// <summary>Reads an item's runtime-only metadata using its exact stored type.</summary>
    /// <typeparam name="T">The exact type used by SetItemMetadata.</typeparam>
    /// <param name="index">A valid zero-based item index.</param>
    /// <returns>The stored value, including null when that is the stored value.</returns>
    /// <exception cref="KeyNotFoundException">No metadata of the requested type is present.</exception>
    public T GetItemMetadata<T>(int index)
    {
        ThrowIfDisposed();
        return GetItem(index).Metadata is MetadataValue<T> typed ? typed.Value :
            throw new KeyNotFoundException("The item has no metadata of the requested type.");
    }

    /// <summary>Tries to read an item's runtime-only metadata using its exact stored type.</summary>
    /// <typeparam name="T">The exact type used by SetItemMetadata.</typeparam>
    /// <param name="index">A valid zero-based item index.</param>
    /// <param name="value">The stored value on success, otherwise the default of T.</param>
    /// <returns>True when the requested type was stored, even if its value is null.</returns>
    public bool TryGetItemMetadata<T>(int index, out T value)
    {
        ThrowIfDisposed();
        if (GetItem(index).Metadata is MetadataValue<T> typed) { value = typed.Value; return true; }
        value = default!; return false;
    }

    /// <summary>Finds the first item whose metadata has the requested type and equal value.</summary>
    /// <typeparam name="T">The exact stored metadata type.</typeparam>
    /// <param name="value">The value compared using EqualityComparer&lt;T&gt;.Default.</param>
    /// <returns>The first matching index, or minus one.</returns>
    public int FindMetadata<T>(T value)
    {
        ThrowIfDisposed();
        for (var index = 0; index < _items.Count; index++)
            if (_items[index].Metadata is MetadataValue<T> typed && EqualityComparer<T>.Default.Equals(typed.Value, value)) return index;
        return -1;
    }
}
