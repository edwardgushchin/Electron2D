namespace Electron2D;

public sealed partial class TileSet
{
    internal readonly record struct OcclusionLayer(uint LightMask, bool SDFCollision);
    internal readonly List<OcclusionLayer> OcclusionLayers = [];
    /// <summary>Returns the shared occlusion layer count.</summary>
    public int GetOcclusionLayersCount() { ThrowIfDisposed(); return OcclusionLayers.Count; }
    /// <summary>Inserts a layer with light membership one and distance-field participation disabled.</summary>
    /// <param name="toPosition">Insertion position, or a negative value to append.</param>
    public void AddOcclusionLayer(int toPosition = -1)
    {
        ThrowIfDisposed(); if (toPosition < 0) toPosition = OcclusionLayers.Count;
        OcclusionLayers.Insert(toPosition, new(1, false)); foreach (var source in _sources.Values) source?.InsertOcclusionLayer(toPosition); ChangedSchema();
    }
    /// <summary>Moves a layer before an insertion position, preserving its borrowed contours.</summary>
    /// <param name="layerIndex">Zero-based layer.</param><param name="toPosition">Insertion position from zero through the layer count.</param>
    public void MoveOcclusionLayer(int layerIndex, int toPosition)
    { ThrowIfDisposed(); Move(OcclusionLayers, layerIndex, toPosition); foreach (var source in _sources.Values) source?.MoveOcclusionLayer(layerIndex, toPosition); ChangedSchema(); }
    /// <summary>Removes a layer and releases its borrowed contour references.</summary>
    /// <param name="layerIndex">Zero-based layer.</param>
    public void RemoveOcclusionLayer(int layerIndex)
    { ThrowIfDisposed(); OcclusionLayers.RemoveAt(layerIndex); foreach (var source in _sources.Values) source?.RemoveOcclusionLayer(layerIndex); ChangedSchema(); }
    /// <summary>Returns shadow-casting membership for a layer.</summary><param name="layerIndex">Zero-based layer.</param>
    public uint GetOcclusionLayerLightMask(int layerIndex) { ThrowIfDisposed(); return OcclusionLayers[layerIndex].LightMask; }
    /// <summary>Sets shadow membership independently of distance-field participation.</summary><param name="layerIndex">Zero-based layer.</param><param name="lightMask">Shadow membership bits.</param>
    public void SetOcclusionLayerLightMask(int layerIndex, uint lightMask)
    { ThrowIfDisposed(); var layer = OcclusionLayers[layerIndex]; if (layer.LightMask == lightMask) return; OcclusionLayers[layerIndex] = layer with { LightMask = lightMask }; EmitChanged(); }
    /// <summary>Tests whether a layer contributes to the canvas distance field.</summary><param name="layerIndex">Zero-based layer.</param>
    public bool GetOcclusionLayerSDFCollision(int layerIndex) { ThrowIfDisposed(); return OcclusionLayers[layerIndex].SDFCollision; }
    /// <summary>Changes distance-field participation independently of shadow membership.</summary><param name="layerIndex">Zero-based layer.</param><param name="sdfCollision">Whether to include contours in the distance field.</param>
    public void SetOcclusionLayerSDFCollision(int layerIndex, bool sdfCollision)
    { ThrowIfDisposed(); var layer = OcclusionLayers[layerIndex]; if (layer.SDFCollision == sdfCollision) return; OcclusionLayers[layerIndex] = layer with { SDFCollision = sdfCollision }; EmitChanged(); }
    private IEnumerable<PropertyDescriptor> OcclusionProperties()
    {
        yield return new PropertyDescriptor<TileSet, int>("OcclusionLayerCount", n => n.GetOcclusionLayersCount(), (n, v) => n.RestoreOcclusionCount(v), _ => 0, stored: true);
        for (var i = 0; i < OcclusionLayers.Count; i++)
        {
            var id = i;
            yield return new PropertyDescriptor<TileSet, uint>($"Occlusion/{id}/LightMask", n => n.GetOcclusionLayerLightMask(id), (n, v) => n.SetOcclusionLayerLightMask(id, v), _ => 1u, stored: true);
            yield return new PropertyDescriptor<TileSet, bool>($"Occlusion/{id}/SDFCollision", n => n.GetOcclusionLayerSDFCollision(id), (n, v) => n.SetOcclusionLayerSDFCollision(id, v), _ => false, stored: true);
        }
    }
    private void RestoreOcclusionCount(int count)
    { if (count < 0) throw new ArgumentOutOfRangeException(nameof(count)); while (OcclusionLayers.Count > count) RemoveOcclusionLayer(OcclusionLayers.Count - 1); while (OcclusionLayers.Count < count) AddOcclusionLayer(); }
}
