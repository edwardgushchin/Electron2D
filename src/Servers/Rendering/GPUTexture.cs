using SDL3;

namespace Electron2D;

internal sealed unsafe class GPUTexture : IDisposable
{
    private readonly nint _device;
    private readonly RenderHandle _texture;
    private readonly RenderHandle _transfer;
    private object? _uploaded;
    internal LayeredTexturePixels? ArrayPixels;
    internal TexturePixels Pixels;
    internal nint Handle => _texture.DangerousGetHandle();

    internal GPUTexture(RenderHandle device, TexturePixels pixels) : this(device, pixels, 1, pixels.Upload.Data.Length, false) { }
    internal GPUTexture(RenderHandle device, LayeredTexturePixels pixels) : this(device, pixels.First, pixels.Layers.Length, pixels.UploadBytes, true) { ArrayPixels = pixels; }
    private GPUTexture(RenderHandle device, TexturePixels pixels, int layers, int bytes, bool array)
    {
        _device = device.DangerousGetHandle();
        Pixels = pixels;
        var type = array ? SDL.GPUTextureType.TextureType2DArray : SDL.GPUTextureType.TextureType2D;
        var format = pixels.Upload.Format == Image.Format.Rgba8 ? SDL.GPUTextureFormat.R8G8B8A8Unorm : SDL.GPUTextureFormat.R32G32B32A32Float;
        if (!SDL.GPUTextureSupportsFormat(_device, format, type, SDL.GPUTextureUsageFlags.Sampler))
            throw new NotSupportedException($"The active GPU backend cannot sample texture format {pixels.Source.Format}.");
        var info = new SDL.GPUTextureCreateInfo
        {
            Type = type,
            Format = format,
            Width = (uint)pixels.Source.Width,
            Height = (uint)pixels.Source.Height,
            LayerCountOrDepth = (uint)layers,
            NumLevels = (uint)pixels.Levels,
            Usage = SDL.GPUTextureUsageFlags.Sampler
        };
        _texture = new RenderHandle(SDL.CreateGPUTexture(_device, in info), h => SDL.ReleaseGPUTexture(_device, h), device);
        try
        {
            var transfer = new SDL.GPUTransferBufferCreateInfo { Usage = SDL.GPUTransferBufferUsage.Upload, Size = (uint)bytes };
            _transfer = new RenderHandle(SDL.CreateGPUTransferBuffer(_device, in transfer), h => SDL.ReleaseGPUTransferBuffer(_device, h), device);
        }
        catch { _texture.Dispose(); throw; }
    }

    private object Version => (object?)ArrayPixels ?? Pixels;
    internal void Upload(nint command)
    {
        if (ReferenceEquals(_uploaded, Version)) return;
        var count = ArrayPixels?.Layers.Length ?? 1;
        var bytes = ArrayPixels?.UploadBytes ?? Pixels.Upload.Data.Length;
        var mapped = SDL.MapGPUTransferBuffer(_device, _transfer.DangerousGetHandle(), true);
        if (mapped == 0) throw CanvasBackend.Failure("map texture upload memory");
        try
        {
            var offset = 0;
            for (var layer = 0; layer < count; layer++)
            {
                var data = (ArrayPixels?.Layers[layer] ?? Pixels).Upload.Data;
                data.AsSpan().CopyTo(new Span<byte>((void*)(mapped + offset), data.Length)); offset += data.Length;
            }
        }
        finally { SDL.UnmapGPUTransferBuffer(_device, _transfer.DangerousGetHandle()); }
        var pass = SDL.BeginGPUCopyPass(command);
        if (pass == 0) throw CanvasBackend.Failure("begin texture upload");
        try
        {
            var offset = 0;
            for (var layer = 0; layer < count; layer++)
            {
                var pixels = ArrayPixels?.Layers[layer] ?? Pixels;
                var width = pixels.Source.Width; var height = pixels.Source.Height;
                for (var level = 0; level < pixels.Levels; level++)
                {
                    var source = new SDL.GPUTextureTransferInfo { TransferBuffer = _transfer.DangerousGetHandle(), Offset = (uint)offset };
                    var target = new SDL.GPUTextureRegion { Texture = Handle, Layer = (uint)layer, MipLevel = (uint)level, W = (uint)width, H = (uint)height, D = 1 };
                    // Cycle once: every remaining layer and mip must update the same native version.
                    SDL.UploadToGPUTexture(pass, in source, in target, layer == 0 && level == 0);
                    offset = checked(offset + width * height * pixels.BytesPerPixel);
                    width = Math.Max(1, width / 2); height = Math.Max(1, height / 2);
                }
            }
            System.Diagnostics.Debug.Assert(offset == bytes);
        }
        finally { SDL.EndGPUCopyPass(pass); }
    }
    internal void CommitUpload() => _uploaded = Version;
    public void Dispose() { _texture.Dispose(); _transfer.Dispose(); }
}
