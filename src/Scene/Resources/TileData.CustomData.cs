namespace Electron2D;

public sealed partial class TileData
{
    internal List<TileCustomDataValue?> CustomValues = [];
    /// <summary>Stores a typed value on the named custom layer, independently of its configured default type.</summary>
    /// <typeparam name="T">The actual stored CLR value type.</typeparam><param name="layerName">Exact nonempty layer name.</param><param name="value">Stored value; arrays and resources are borrowed.</param>
    /// <remarks>Named access requires an attached tile set. Every write emits Changed.</remarks>
    /// <exception cref="InvalidOperationException">The data has no attached tile set.</exception>
    /// <exception cref="KeyNotFoundException">The named layer does not exist.</exception>
    public void SetCustomData<T>(string layerName, T value) => SetCustomDataByLayerID(CustomIndex(layerName), value);
    /// <summary>Reads the named layer using its actual stored type.</summary>
    /// <typeparam name="T">The stored value type; resource base types can read derived resources.</typeparam><param name="layerName">Exact layer name.</param><returns>The stored value, or default(T) for a nil entry.</returns>
    /// <exception cref="KeyNotFoundException">The layer name or stored type does not match.</exception>
    /// <exception cref="InvalidOperationException">The data has no attached tile set.</exception>
    public T GetCustomData<T>(string layerName) => GetCustomDataByLayerID<T>(CustomIndex(layerName));
    /// <summary>Tests whether the attached set defines this named custom layer, including nil entries.</summary>
    /// <param name="layerName">Exact layer name.</param><returns>Whether the named layer exists.</returns>
    /// <exception cref="InvalidOperationException">The data has no attached tile set.</exception>
    public bool HasCustomData(string layerName) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(layerName); return CustomSet().HasCustomDataLayerByName(layerName); }
    /// <summary>Stores a typed value at a valid custom-layer index.</summary>
    /// <typeparam name="T">The actual stored CLR value type.</typeparam><param name="layerID">Zero-based custom layer index.</param><param name="value">Stored value; borrowed references retain their own ownership.</param>
    /// <remarks>Changing the configured layer type later converts or resets values. Stable typed writes reuse their prepared holder.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The layer index is absent.</exception>
    public void SetCustomDataByLayerID<T>(int layerID, T value)
    {
        CustomLayer(layerID);
        if (CustomValues[layerID] is TileCustomDataValue<T> typed) typed.Value = value;
        else { TileCustomDataType.Register<T>(); CustomValues[layerID] = new TileCustomDataValue<T>(value); }
        CustomChanged();
    }
    /// <summary>Reads the indexed layer using its actual stored type.</summary>
    /// <typeparam name="T">The exact value type, or an assignable resource type.</typeparam><param name="layerID">Zero-based layer index.</param><returns>The stored value, or default(T) for nil.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The layer index is absent.</exception>
    /// <exception cref="KeyNotFoundException">The non-nil stored type does not match T.</exception>
    public T GetCustomDataByLayerID<T>(int layerID)
    {
        CustomLayer(layerID); var value = CustomValues[layerID];
        if (value is null) return default!;
        if (value is TileCustomDataValue<T> typed) return typed.Value;
        if (typeof(Resource).IsAssignableFrom(typeof(T)) && value.ResourceValue is T resource) return resource;
        throw new KeyNotFoundException("The custom tile value has a different stored type.");
    }
    private TileSet CustomSet() => TerrainSource?.OwnerSet ?? throw new InvalidOperationException("Named custom data requires an attached tile set.");
    private int CustomIndex(string name) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(name); var index = CustomSet().GetCustomDataLayerByName(name); if (index < 0) throw new KeyNotFoundException("No custom layer with this name."); return index; }
    private void CustomLayer(int index) { ThrowIfDisposed(); if ((uint)index >= (uint)CustomValues.Count) throw new ArgumentOutOfRangeException(nameof(index)); }
    internal void CustomChanged() { try { NotifyPropertyListChanged(); } finally { Changed?.Invoke(); } }
    internal List<TileCustomDataValue?> PrepareCustomSchema(TileSet? set)
    {
        if (set is null) return CustomValues;
        var result = new List<TileCustomDataValue?>();
        for (var i = 0; i < set.CustomLayers.Count; i++) result.Add(set.CustomLayers[i].Schema?.ConvertValue(i < CustomValues.Count ? CustomValues[i] : null));
        return result;
    }
    private void RestoreCustomTypes(string[] ids)
    {
        ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(ids); if (ids.Length > 65536) throw new ArgumentOutOfRangeException(nameof(ids)); var values = ids.Select(id => TileCustomDataType.Find(id ?? throw new ArgumentException("Null custom type ID."))?.CreateDefault()).ToList();
        CustomValues = values; CustomChanged();
    }
    private IEnumerable<PropertyDescriptor> CustomProperties<TOwner>(string prefix, Func<TOwner, TileData> data) where TOwner : ElectronObject
    {
        yield return new PropertyDescriptor<TOwner, string[]>(prefix + "CustomDataTypes", n => data(n).CustomValues.Select(v => v?.Schema.ID ?? string.Empty).ToArray(), (n, v) => data(n).RestoreCustomTypes(v), _ => [], stored: true);
        for (var i = 0; i < CustomValues.Count; i++) if (CustomValues[i] is { } value) yield return value.Property(prefix + "CustomData/" + i, data, i);
    }
}
