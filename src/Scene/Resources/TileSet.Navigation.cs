namespace Electron2D;

public sealed partial class TileSet
{
    internal readonly List<uint> NavigationLayers = [];
    /// <summary>Returns the number of navigation layers shared by every source alternative.</summary>
    public int GetNavigationLayersCount() { ThrowIfDisposed(); return NavigationLayers.Count; }
    /// <summary>Inserts a navigation layer with membership bit one; a negative position appends.</summary>
    /// <param name="toPosition">Insertion position, or a negative value to append.</param>
    public void AddNavigationLayer(int toPosition = -1)
    {
        ThrowIfDisposed(); if (toPosition < 0) toPosition = NavigationLayers.Count;
        NavigationLayers.Insert(toPosition, 1); foreach (var source in _sources.Values) source?.InsertNavigationLayer(toPosition); ChangedSchema();
    }
    /// <summary>Moves a navigation layer before an insertion position, preserving each alternative's polygon.</summary>
    /// <param name="layerIndex">Zero-based navigation layer.</param>
    /// <param name="toPosition">Insertion position between zero and the layer count, inclusive.</param>
    public void MoveNavigationLayer(int layerIndex, int toPosition)
    { ThrowIfDisposed(); Move(NavigationLayers, layerIndex, toPosition); foreach (var source in _sources.Values) source?.MoveNavigationLayer(layerIndex, toPosition); ChangedSchema(); }
    /// <summary>Removes a navigation layer and its borrowed polygon references from all alternatives.</summary>
    /// <param name="layerIndex">Zero-based navigation layer.</param>
    public void RemoveNavigationLayer(int layerIndex)
    { ThrowIfDisposed(); NavigationLayers.RemoveAt(layerIndex); foreach (var source in _sources.Values) source?.RemoveNavigationLayer(layerIndex); ChangedSchema(); }
    /// <summary>Returns a navigation layer's 32-bit membership mask.</summary>
    /// <param name="layerIndex">Zero-based navigation layer.</param>
    public uint GetNavigationLayerLayers(int layerIndex) { ThrowIfDisposed(); return NavigationLayers[layerIndex]; }
    /// <summary>Sets a navigation layer's membership mask; zero excludes it from all path queries.</summary>
    /// <param name="layerIndex">Zero-based navigation layer.</param>
    /// <param name="layers">Navigation membership bits.</param>
    public void SetNavigationLayerLayers(int layerIndex, uint layers)
    { ThrowIfDisposed(); if (NavigationLayers[layerIndex] == layers) return; NavigationLayers[layerIndex] = layers; EmitChanged(); }
    /// <summary>Tests one numbered membership bit.</summary>
    /// <param name="layerIndex">Zero-based navigation layer.</param>
    /// <param name="layerNumber">Membership bit between one and 32.</param>
    public bool GetNavigationLayerLayerValue(int layerIndex, int layerNumber) => (GetNavigationLayerLayers(layerIndex) & NavigationBit(layerNumber)) != 0;
    /// <summary>Changes one numbered membership bit without changing other bits.</summary>
    /// <param name="layerIndex">Zero-based navigation layer.</param>
    /// <param name="layerNumber">Membership bit between one and 32.</param>
    /// <param name="value">Whether the bit is enabled.</param>
    public void SetNavigationLayerLayerValue(int layerIndex, int layerNumber, bool value)
    { var bits = GetNavigationLayerLayers(layerIndex); var bit = NavigationBit(layerNumber); SetNavigationLayerLayers(layerIndex, value ? bits | bit : bits & ~bit); }
    private static uint NavigationBit(int bit) => bit is >= 1 and <= 32 ? 1u << (bit - 1) : throw new ArgumentOutOfRangeException(nameof(bit));
    private IEnumerable<PropertyDescriptor> NavigationProperties()
    {
        yield return new PropertyDescriptor<TileSet, int>("NavigationLayerCount", n => n.GetNavigationLayersCount(), (n, v) => n.RestoreNavigationCount(v), _ => 0, stored: true);
        for (var i = 0; i < NavigationLayers.Count; i++)
        {
            var id = i;
            yield return new PropertyDescriptor<TileSet, uint>($"Navigation/{id}/Layers", n => n.GetNavigationLayerLayers(id), (n, v) => n.SetNavigationLayerLayers(id, v), _ => 1u, stored: true);
        }
    }
    private void RestoreNavigationCount(int count)
    { if (count < 0) throw new ArgumentOutOfRangeException(nameof(count)); while (NavigationLayers.Count > count) RemoveNavigationLayer(NavigationLayers.Count - 1); while (NavigationLayers.Count < count) AddNavigationLayer(); }
}
