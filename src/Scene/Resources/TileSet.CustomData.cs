namespace Electron2D;

public sealed partial class TileSet
{
    internal sealed class CustomLayerDefinition
    {
        internal string Name = string.Empty;
        internal TileCustomDataType? Schema;
        internal CustomLayerDefinition Copy() => new() { Name = Name, Schema = Schema };
    }
    internal List<CustomLayerDefinition> CustomLayers = [];
    private Dictionary<string, int> _customNames = new(StringComparer.Ordinal);
    private bool _editingCustom;
    /// <summary>Returns the number of ordered custom-data layers.</summary><returns>The layer count.</returns>
    public int GetCustomDataLayersCount() { ThrowIfDisposed(); return CustomLayers.Count; }
    /// <summary>Inserts an unnamed nil-type layer and its nil value into every alternative.</summary>
    /// <param name="toPosition">Insertion position; any negative value appends.</param>
    /// <exception cref="ArgumentOutOfRangeException">Position exceeds the layer count.</exception>
    public void AddCustomDataLayer(int toPosition = -1)
    {
        ThrowIfDisposed(); if (toPosition < 0) toPosition = CustomLayers.Count; var index = toPosition;
        EditCustom(layers => layers.Insert(index, new()), values => values.Insert(index, null));
    }
    /// <summary>Moves a layer before the insertion position, preserving each tile's corresponding value.</summary>
    /// <param name="layerIndex">Current zero-based layer index.</param><param name="toPosition">Insertion position, from zero through count.</param>
    /// <exception cref="ArgumentOutOfRangeException">An index is outside its range.</exception>
    public void MoveCustomDataLayer(int layerIndex, int toPosition)
    { ThrowIfDisposed(); CustomLayer(layerIndex); if ((uint)toPosition > (uint)CustomLayers.Count) throw new ArgumentOutOfRangeException(nameof(toPosition)); EditCustom(layers => Move(layers, layerIndex, toPosition), values => Move(values, layerIndex, toPosition)); }
    /// <summary>Removes a layer and its values from every alternative.</summary>
    /// <param name="layerIndex">Zero-based layer index.</param><exception cref="ArgumentOutOfRangeException">The layer does not exist.</exception>
    public void RemoveCustomDataLayer(int layerIndex)
    { CustomLayer(layerIndex); EditCustom(layers => layers.RemoveAt(layerIndex), values => values.RemoveAt(layerIndex)); }
    /// <summary>Finds a layer by its current exact nonempty name.</summary>
    /// <param name="layerName">Layer name.</param><returns>The index, or minus one if absent or empty.</returns>
    public int GetCustomDataLayerByName(string layerName) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(layerName); return _customNames.GetValueOrDefault(layerName, -1); }
    /// <summary>Tests whether a current nonempty layer name exists.</summary>
    /// <param name="layerName">Layer name.</param><returns>Whether the named layer exists.</returns>
    public bool HasCustomDataLayerByName(string layerName) => GetCustomDataLayerByName(layerName) >= 0;
    /// <summary>Returns a layer's name, empty for an unnamed layer.</summary>
    /// <param name="layerIndex">Layer index.</param><returns>The layer name.</returns>
    public string GetCustomDataLayerName(int layerIndex) => CustomLayer(layerIndex).Name;
    /// <summary>Changes a layer's exact name and rebuilds current name lookup.</summary>
    /// <param name="layerIndex">Layer index.</param><param name="layerName">Unique nonempty name, or empty for unnamed.</param>
    /// <exception cref="ArgumentException">Another layer has this nonempty name.</exception>
    public void SetCustomDataLayerName(int layerIndex, string layerName)
    { CustomLayer(layerIndex); ArgumentNullException.ThrowIfNull(layerName); EditCustom(layers => layers[layerIndex].Name = layerName, null); }
    /// <summary>Returns the configured default/conversion CLR type, or null for nil.</summary>
    /// <param name="layerIndex">Layer index.</param><returns>The configured type, independently of individual stored value types.</returns>
    public Type? GetCustomDataLayerType(int layerIndex) => CustomLayer(layerIndex).Schema?.Type;
    /// <summary>Changes the configured type using a previously prepared typed factory; null clears values to nil.</summary>
    /// <param name="layerIndex">Layer index.</param><param name="type">CLR type prepared by a typed setter or registered portable codec/resource, or null.</param>
    /// <remarks>All value conversions prepare before definition/value commitment and callbacks.
    /// Unconvertible values reset to the type's default. Conversion overflow rejects the complete edit.</remarks>
    /// <exception cref="NotSupportedException">No direct typed factory was prepared for this type.</exception>
    public void SetCustomDataLayerType(int layerIndex, Type? type)
    {
        CustomLayer(layerIndex); var schema = TileCustomDataType.Find(type);
        EditCustom(layers => layers[layerIndex].Schema = schema, values => values[layerIndex] = schema?.ConvertValue(values[layerIndex]));
    }
    /// <summary>Prepares a direct factory for T and changes a layer's default/conversion type.</summary>
    /// <typeparam name="T">Concrete CLR data type; prepare application portable codecs in each loading process.</typeparam><param name="layerIndex">Layer index.</param>
    public void SetCustomDataLayerType<T>(int layerIndex)
    { CustomLayer(layerIndex); TileCustomDataType.Register<T>(); SetCustomDataLayerType(layerIndex, typeof(T)); }
    private CustomLayerDefinition CustomLayer(int index)
    { ThrowIfDisposed(); if ((uint)index >= (uint)CustomLayers.Count) throw new ArgumentOutOfRangeException(nameof(index)); return CustomLayers[index]; }
    private static Dictionary<string, int> CustomNames(List<CustomLayerDefinition> layers)
    {
        var names = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < layers.Count; i++) if (layers[i].Name.Length != 0 && !names.TryAdd(layers[i].Name, i)) throw new ArgumentException("Custom layer names must be unique.");
        return names;
    }
    private void EditCustom(Action<List<CustomLayerDefinition>> edit, Action<List<TileCustomDataValue?>>? editValues)
    {
        if (_editingCustom) throw new InvalidOperationException("Custom layer definitions cannot re-enter an edit.");
        _editingCustom = true;
        try
        {
            var layers = CustomLayers.Select(l => l.Copy()).ToList(); edit(layers); var names = CustomNames(layers);
            var children = AtlasData().ToArray(); var prepared = new List<TileCustomDataValue?>[children.Length];
            for (var i = 0; i < children.Length; i++)
            {
                prepared[i] = new(children[i].CustomValues);
                while (prepared[i].Count < CustomLayers.Count) prepared[i].Add(CustomLayers[prepared[i].Count].Schema?.CreateDefault());
                if (prepared[i].Count > CustomLayers.Count) prepared[i].RemoveRange(CustomLayers.Count, prepared[i].Count - CustomLayers.Count);
                editValues?.Invoke(prepared[i]);
            }
            CustomLayers = layers; _customNames = names;
            for (var i = 0; i < children.Length; i++) children[i].CustomValues = prepared[i];
            Exception? failure = null;
            foreach (var child in children) if (!child.IsDisposed) try { child.CustomChanged(); } catch (Exception error) { failure ??= error; }
            try { NotifyPropertyListChanged(); } catch (Exception error) { failure ??= error; }
            try { EmitChanged(); } catch (Exception error) { failure ??= error; }
            if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
        finally { _editingCustom = false; }
    }
    private void RestoreCustomCount(int count)
    {
        ThrowIfDisposed(); if ((uint)count > 65536) throw new ArgumentOutOfRangeException(nameof(count));
        if (count == CustomLayers.Count) return;
        EditCustom(layers => ResizeCustom(layers, count, static () => new CustomLayerDefinition()), values => ResizeCustom(values, count, static () => null));
    }
    private static void ResizeCustom<T>(List<T> values, int count, Func<T> create)
    { if (values.Count > count) values.RemoveRange(count, values.Count - count); else while (values.Count < count) values.Add(create()); }
    private IEnumerable<PropertyDescriptor> CustomProperties()
    {
        yield return new PropertyDescriptor<TileSet, int>("CustomLayerCount", n => n.GetCustomDataLayersCount(), (n, v) => n.RestoreCustomCount(v), _ => 0, stored: true);
        for (var i = 0; i < CustomLayers.Count; i++)
        {
            var id = i; var prefix = "CustomLayers/" + id + "/";
            yield return new PropertyDescriptor<TileSet, string>(prefix + "Name", n => n.GetCustomDataLayerName(id), (n, v) => n.SetCustomDataLayerName(id, v), _ => string.Empty, stored: true);
            yield return new PropertyDescriptor<TileSet, string>(prefix + "Type", n => n.CustomLayers[id].Schema?.ID ?? string.Empty, (n, v) => n.SetCustomDataLayerType(id, TileCustomDataType.Find(v)?.Type), _ => string.Empty, stored: true);
        }
    }
}
