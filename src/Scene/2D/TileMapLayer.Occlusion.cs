namespace Electron2D;

public partial class TileMapLayer
{
    private bool _occlusionEnabled = true;
    /// <summary>Gets or sets whether enabled atlas cells contribute shadow and distance-field contours.</summary>
    /// <value>True initially; placed scene occluders retain their ordinary child-node policy.</value>
    public bool OcclusionEnabled { get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _occlusionEnabled; } set { EnsureMutable(); if (_occlusionEnabled == value) return; _occlusionEnabled = value; BeginEventEmission(_publisherChanged.Handlers)?.Invoke(); } }

    internal void AppendOcclusion(CanvasLightingFrame frame, Transform transform, int canvasLayer)
    {
        if (!_enabled || !_occlusionEnabled || _tileSet is not { IsDisposed: false } set) return;
        foreach (var (coords, cell) in _cells)
        {
            if (EffectiveData(coords, cell) is not { IsDisposed: false } data) continue;
            var basis = transform * new Transform(0, MapToLocal(coords));
            for (var layer = 0; layer < Math.Min(set.OcclusionLayers.Count, data.Occlusion.Count); layer++)
            {
                var policy = set.OcclusionLayers[layer];
                for (var polygonIndex = 0; polygonIndex < data.Occlusion[layer].Count; polygonIndex++)
                {
                    var polygon = data.GetOccluderPolygon(layer, polygonIndex, (cell.Alternative & TileSetAtlasSource.TransformFlipH) != 0, (cell.Alternative & TileSetAtlasSource.TransformFlipV) != 0, (cell.Alternative & TileSetAtlasSource.TransformTranspose) != 0);
                    if (polygon is not null) frame.AddContour(polygon, basis, policy.LightMask, canvasLayer, policy.SDFCollision);
                }
            }
        }
    }
}
