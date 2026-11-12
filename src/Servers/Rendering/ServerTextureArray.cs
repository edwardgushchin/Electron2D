namespace Electron2D;

internal sealed class ServerTextureArray(TextureLayered storage, RID ownedDeviceImage = default) : TextureLayered
{
    private TextureLayered _storage = storage;
    internal RID OwnedDeviceImage = ownedDeviceImage;
    internal TextureArrayRD? DeviceTexture => _storage as TextureArrayRD;

    internal void TakeStorage(ServerTextureArray source)
    {
        (_storage, source._storage) = (source._storage, _storage);
        (OwnedDeviceImage, source.OwnedDeviceImage) = (source.OwnedDeviceImage, OwnedDeviceImage);
        ServerPath = source.ServerPath;
    }

    internal void UpdateLayer(Image image, int layer)
    {
        ThrowIfDisposed();
        if (_storage is not TextureArray array)
            throw new InvalidOperationException("Update the underlying device image of this texture.");
        array.UpdateLayer(image, layer);
    }

    public override Image.Format GetFormat() { ThrowIfDisposed(); return _storage.GetFormat(); }
    public override int GetWidth() { ThrowIfDisposed(); return _storage.GetWidth(); }
    public override int GetHeight() { ThrowIfDisposed(); return _storage.GetHeight(); }
    public override int GetLayers() { ThrowIfDisposed(); return _storage.GetLayers(); }
    public override bool HasMIPMaps() { ThrowIfDisposed(); return _storage.HasMIPMaps(); }
    public override Image? GetLayerData(int layerIndex) { ThrowIfDisposed(); return _storage.GetLayerData(layerIndex); }
    internal override LayeredTexturePixels? CaptureLayers() { ThrowIfDisposed(); return _storage.CaptureLayers(); }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _storage.Dispose();
        base.Dispose(disposing);
    }
}
