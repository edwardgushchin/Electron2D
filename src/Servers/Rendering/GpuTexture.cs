using SDL3;

namespace Electron2D;

internal sealed unsafe class GpuTexture : IDisposable
{
    private readonly nint _device;
    private readonly RenderHandle _texture;
    private readonly RenderHandle _transfer;
    private TexturePixels? _uploaded;
    internal TexturePixels Pixels;
    internal nint Handle => _texture.DangerousGetHandle();

    internal GpuTexture(RenderHandle device, TexturePixels pixels)
    {
        _device = device.DangerousGetHandle();
        Pixels = pixels;
        var format = pixels.Upload.Format == Image.Format.Rgba8 ? SDL.GPUTextureFormat.R8G8B8A8Unorm : SDL.GPUTextureFormat.R32G32B32A32Float;
        if (!SDL.GPUTextureSupportsFormat(_device, format, SDL.GPUTextureType.TextureType2D, SDL.GPUTextureUsageFlags.Sampler))
            throw new NotSupportedException($"The active GPU backend cannot sample texture format {pixels.Source.Format}.");
        var info = new SDL.GPUTextureCreateInfo
        {
            Type = SDL.GPUTextureType.TextureType2D,
            Format = format,
            Width = (uint)pixels.Source.Width,
            Height = (uint)pixels.Source.Height,
            LayerCountOrDepth = 1,
            NumLevels = (uint)pixels.Levels,
            Usage = SDL.GPUTextureUsageFlags.Sampler
        };
        _texture = new RenderHandle(SDL.CreateGPUTexture(_device, in info), h => SDL.ReleaseGPUTexture(_device, h), device);
        try
        {
            var transfer = new SDL.GPUTransferBufferCreateInfo { Usage = SDL.GPUTransferBufferUsage.Upload, Size = (uint)pixels.Upload.Data.Length };
            _transfer = new RenderHandle(SDL.CreateGPUTransferBuffer(_device, in transfer), h => SDL.ReleaseGPUTransferBuffer(_device, h), device);
        }
        catch { _texture.Dispose(); throw; }
    }

    internal void Upload(nint command)
    {
        if (ReferenceEquals(_uploaded, Pixels)) return;
        var data = Pixels.Upload.Data;
        var mapped = SDL.MapGPUTransferBuffer(_device, _transfer.DangerousGetHandle(), true);
        if (mapped == 0) throw CanvasBackend.Failure("map texture upload memory");
        try { data.AsSpan().CopyTo(new Span<byte>((void*)mapped, data.Length)); }
        finally { SDL.UnmapGPUTransferBuffer(_device, _transfer.DangerousGetHandle()); }
        var pass = SDL.BeginGPUCopyPass(command);
        if (pass == 0) throw CanvasBackend.Failure("begin texture upload");
        try
        {
            var offset = 0;
            var width = Pixels.Source.Width;
            var height = Pixels.Source.Height;
            for (var level = 0; level < Pixels.Levels; level++)
            {
                var source = new SDL.GPUTextureTransferInfo { TransferBuffer = _transfer.DangerousGetHandle(), Offset = (uint)offset };
                var target = new SDL.GPUTextureRegion { Texture = Handle, MipLevel = (uint)level, W = (uint)width, H = (uint)height, D = 1 };
                // Cycle only at the first level: all remaining levels must update the same native texture version.
                SDL.UploadToGPUTexture(pass, in source, in target, level == 0);
                offset = checked(offset + width * height * Pixels.BytesPerPixel);
                width = Math.Max(1, width / 2); height = Math.Max(1, height / 2);
            }
        }
        finally { SDL.EndGPUCopyPass(pass); }
    }

    internal void CommitUpload() => _uploaded = Pixels;
    public void Dispose() { _texture.Dispose(); _transfer.Dispose(); }
}
