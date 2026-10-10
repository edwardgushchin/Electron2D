namespace Electron2D;

public sealed partial class NavigationRegion
{
    /// <summary>Occurs after successful geometry publication for a requested region bake.</summary>
    public event Action? BakeFinished;
    /// <summary>Reports whether this region's current resource is still preparing or awaiting publication.</summary><returns>False when no resource/bake is present.</returns>
    public bool IsBaking() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _polygon is not null && NavigationServer.IsBakingNavigationPolygon(_polygon); }
    /// <summary>Parses this region's source selection and bakes its navigation resource.</summary>
    /// <param name="onThread">True prepares on a worker; false publishes before returning.</param>
    /// <remarks>Scene parsing and completion run on the owner. A selected map supplies the compatible bake CellSize. Detached root traversal supports programmatic authoring.</remarks>
    /// <exception cref="InvalidOperationException">No navigation resource is assigned or a bake is already pending.</exception>
    public void BakeNavigationPolygon(bool onThread = true)
    {
        EnsureMutable(); var polygon = _polygon ?? throw new InvalidOperationException("A navigation polygon is required before baking.");
        if (NavigationServer.IsBakingNavigationPolygon(polygon)) throw new InvalidOperationException("This region resource already has an active bake.");
        var map = GetNavigationMap(); if (map.IsValid()) polygon.CellSize = NavigationServer.MapGetCellSize(map);
        using var source = new NavigationMeshSourceGeometryData(); NavigationServer.ParseSourceGeometryData(polygon, source, this);
        void Complete() { if (!IsDisposed) BakeFinished?.Invoke(); }
        if (onThread) NavigationServer.BakeFromSourceGeometryDataAsync(polygon, source, Complete, Tree); else NavigationServer.BakeFromSourceGeometryData(polygon, source, Complete);
    }
}
