using SDL3;

namespace Electron2D;

internal sealed unsafe class NativeCanvasTexture : IDisposable
{
    private readonly RenderHandle _texture;
    private readonly bool _BGRA;
    private readonly bool _D3D12;
    internal CompatibilityCanvasBackend Backend { get; }
    internal nint Native { get; }
    internal nint Handle => _texture.DangerousGetHandle();
    internal Vector2i Size { get; }

    internal NativeCanvasTexture(CompatibilityCanvasBackend backend, RenderHandle renderer, nint native, Vector2i size)
    {
        var rendererHandle = renderer.DangerousGetHandle();
        var rendererProperties = SDL.GetRendererProperties(rendererHandle);
        if (SDL.GetNumberProperty(rendererProperties, SDL.Props.RendererOutputColorspaceNumber, 0) != (long)SDL.Colorspace.SRGB)
            throw new NotSupportedException("Native texture import requires an SDR renderer.");

        _D3D12 = backend.Driver == "direct3d12";
        var (deviceProperty, createProperty, nativeProperty) = _D3D12
            ? (SDL.Props.RendererD3D12DevicePointer, SDL.Props.TextureCreateD3D12TexturePointer, SDL.Props.TextureD3D12TexturePointer)
            : (SDL.Props.RendererD3D11DevicePointer, SDL.Props.TextureCreateD3D11TexturePointer, SDL.Props.TextureD3D11TexturePointer);
        var device = SDL.GetPointerProperty(rendererProperties, deviceProperty, 0);
        if (device == 0) throw new NotSupportedException("The renderer did not expose its Direct3D device.");
        _BGRA = _D3D12 ? D3D12CanvasResource.Validate(native, device, size) : D3D11CanvasTexture.Validate(native, device, size);
        Backend = backend;
        Native = native;
        Size = size;
        var properties = SDL.CreateProperties();
        if (properties == 0) throw CanvasBackend.Failure("allocate native texture properties");
        try
        {
            CanvasBackend.Check(SDL.SetNumberProperty(properties, SDL.Props.TextureCreateFormatNumber,
                (long)(_BGRA ? SDL.PixelFormat.ARGB8888 : SDL.PixelFormat.ABGR8888)) &&
                SDL.SetNumberProperty(properties, SDL.Props.TextureCreateWidthNumber, size.X) &&
                SDL.SetNumberProperty(properties, SDL.Props.TextureCreateHeightNumber, size.Y) &&
                SDL.SetPointerProperty(properties, createProperty, native), "configure native texture import");
            _texture = new(SDL.CreateTextureWithProperties(rendererHandle, properties), SDL.DestroyTexture, renderer);
            try
            {
                if (SDL.GetPointerProperty(SDL.GetTextureProperties(Handle), nativeProperty, 0) != native)
                    throw new NotSupportedException("The renderer cannot sample the imported allocation directly.");
            }
            catch { _texture.Dispose(); throw; }
        }
        finally { SDL.DestroyProperties(properties); }
    }

    internal Image Read() => _D3D12 ? Backend.ReadImportedTexture(Handle, Size) : D3D11CanvasTexture.Read(this);

    internal void Update(Image image)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (image.Width != Size.X || image.Height != Size.Y || image.PixelFormat != Image.Format.Rgba8 || image.HasMIPMaps)
            throw new ArgumentException("Native texture updates require matching RGBA8 dimensions without mipmaps.", nameof(image));
        var pixels = image.GetData();
        SwapChannels(pixels);
        fixed (byte* data = pixels)
            CanvasBackend.Check(SDL.UpdateTexture(Handle, 0, (nint)data, checked(Size.X * 4)), "update imported texture pixels");
    }

    internal void SwapChannels(Span<byte> pixels)
    {
        if (!_BGRA) return;
        for (var i = 0; i < pixels.Length; i += 4) (pixels[i], pixels[i + 2]) = (pixels[i + 2], pixels[i]);
    }

    public void Dispose() => _texture.Dispose();
}
