using SDL3;

namespace Electron2D;

internal sealed unsafe class GPUTexture : IDisposable
{
    private readonly nint _device;
    private RenderHandle _texture;
    private readonly RenderHandle _transfer;
    private readonly SDL.GPUTextureUsageFlags _nativeUsage;
    private readonly bool _blockUpload;
    private readonly int _transferBytes;
    private object? _uploaded;
    private object? _recorded;
    private ulong _uploadedRevision, _recordedRevision;
    private bool _exportPrepared;
    internal bool IsDisposed => _texture.IsClosed;
    internal RenderHandle Allocation => _texture;
    internal RenderingDevice.TextureUsageBits DeviceUsage { get; private set; }
    internal LayeredTexturePixels? ArrayPixels;
    internal TexturePixels Pixels;
    internal SDL.GPUTextureFormat NativeTextureFormat { get; }
    internal bool HasSRGBView { get; private set; }
    internal nint Handle => _texture.DangerousGetHandle();

    internal GPUTexture(RenderHandle device, TexturePixels pixels, bool export = false) : this(device, pixels, 1, pixels.Upload.Data.Length, false, export) { }
    internal GPUTexture(RenderHandle device, LayeredTexturePixels pixels, bool export = false) : this(device, pixels.First, pixels.Layers.Length, pixels.UploadBytes, true, export) { ArrayPixels = pixels; }
    private GPUTexture(RenderHandle device, TexturePixels pixels, int layers, int bytes, bool array, bool export)
    {
        _device = device.DangerousGetHandle();
        Pixels = pixels;
        var type = array ? SDL.GPUTextureType.TextureType2DArray : SDL.GPUTextureType.TextureType2D;
        var blockFormat = BCTextureFormat(pixels.Source.Format);
        _blockUpload = blockFormat != SDL.GPUTextureFormat.Invalid && SDL.GPUTextureSupportsFormat(_device, blockFormat, type, SDL.GPUTextureUsageFlags.Sampler);
        var format = _blockUpload ? blockFormat : pixels.Upload.Format switch
        {
            Image.Format.Rgba8 => SDL.GPUTextureFormat.R8G8B8A8Unorm,
            Image.Format.Rgba16I => SDL.GPUTextureFormat.R16G16B16A16Uint,
            _ => SDL.GPUTextureFormat.R32G32B32A32Float
        };
        if (!SDL.GPUTextureSupportsFormat(_device, format, type, SDL.GPUTextureUsageFlags.Sampler))
            throw new NotSupportedException($"The active GPU backend cannot sample texture format {pixels.Source.Format}.");
        NativeTextureFormat = format;
        _transferBytes = _blockUpload ? array ? pixels.Source.Data.Length * layers : pixels.Source.Data.Length : bytes;
        var usage = SDL.GPUTextureUsageFlags.Sampler;
        DeviceUsage = RenderingDevice.TextureUsageBits.Sampling | RenderingDevice.TextureUsageBits.CanCopyFrom | RenderingDevice.TextureUsageBits.CanCopyTo | RenderingDevice.TextureUsageBits.CanUpdate;
        var storage = SDL.GPUTextureUsageFlags.GraphicsStorageRead | SDL.GPUTextureUsageFlags.ComputeStorageRead | SDL.GPUTextureUsageFlags.ComputeStorageWrite | SDL.GPUTextureUsageFlags.ComputeStorageSimultaneousReadWrite;
        if (SDL.GPUTextureSupportsFormat(_device, format, type, usage | storage)) { usage |= storage; DeviceUsage |= RenderingDevice.TextureUsageBits.Storage; }
        _nativeUsage = usage;
        var info = AllocationInfo(layers, array);
        _texture = (export ? TryCreateExportStorage(device, in info) : null) ??
            new RenderHandle(SDL.CreateGPUTexture(_device, in info), h => SDL.ReleaseGPUTexture(_device, h), device);
        _exportPrepared = export;
        try
        {
            var transfer = new SDL.GPUTransferBufferCreateInfo { Usage = SDL.GPUTransferBufferUsage.Upload, Size = (uint)_transferBytes };
            _transfer = new RenderHandle(SDL.CreateGPUTransferBuffer(_device, in transfer), h => SDL.ReleaseGPUTransferBuffer(_device, h), device);
        }
        catch { _texture.Dispose(); throw; }
    }

    private SDL.GPUTextureCreateInfo AllocationInfo(int layers, bool array) => new()
    {
        Type = array ? SDL.GPUTextureType.TextureType2DArray : SDL.GPUTextureType.TextureType2D,
        Format = NativeTextureFormat,
        Width = (uint)Pixels.Source.Width,
        Height = (uint)Pixels.Source.Height,
        LayerCountOrDepth = (uint)layers,
        NumLevels = (uint)Pixels.Levels,
        Usage = _nativeUsage
    };

    internal void PrepareForExport(RenderHandle device)
    {
        if (_exportPrepared) return;
        // Before the first device RID exists, the authored upload still owns all of this image's pixels.
        var info = AllocationInfo(ArrayPixels?.Layers.Length ?? 1, ArrayPixels is not null);
        var replacement = TryCreateExportStorage(device, in info);
        if (replacement is not null)
        {
            _texture.Dispose();
            _texture = replacement;
            _uploaded = _recorded = null;
        }
        _exportPrepared = true;
    }

    private RenderHandle? TryCreateExportStorage(RenderHandle device, in SDL.GPUTextureCreateInfo info)
    {
        var srgb = info.Format switch
        {
            SDL.GPUTextureFormat.R8G8B8A8Unorm => SDL.GPUTextureFormat.R8G8B8A8UnormSRGB,
            SDL.GPUTextureFormat.BC1RGBAUnorm => SDL.GPUTextureFormat.BC1RGBAUnormSRGB,
            SDL.GPUTextureFormat.BC2RGBAUnorm => SDL.GPUTextureFormat.BC2RGBAUnormSRGB,
            SDL.GPUTextureFormat.BC3RGBAUnorm => SDL.GPUTextureFormat.BC3RGBAUnormSRGB,
            SDL.GPUTextureFormat.BC7RGBAUnorm => SDL.GPUTextureFormat.BC7RGBAUnormSRGB,
            _ => SDL.GPUTextureFormat.Invalid
        };
        if (srgb == SDL.GPUTextureFormat.Invalid || GPUTextureViews.Support(_device, GPUTextureViews.SampledFormatABI) != 1 ||
            !SDL.GPUTextureSupportsFormat(_device, srgb, info.Type, SDL.GPUTextureUsageFlags.Sampler)) return null;
        SDL.GPUTextureFormat* formats = stackalloc SDL.GPUTextureFormat[2] { info.Format, srgb };
        var image = GPUTextureViews.Create(_device, GPUTextureViews.SampledFormatABI, in info, (nint)formats, 2);
        if (image == 0) return null;
        var handle = new RenderHandle(image, h => GPUTextureViews.Destroy(_device, 1, h), device);
        HasSRGBView = true;
        return handle;
    }

    private object Version => (object?)ArrayPixels ?? Pixels;
    private ulong Revision => ArrayPixels is null ? Pixels.Revision : 0;
    internal bool NeedsUpload => !ReferenceEquals(_uploaded, Version) || _uploadedRevision != Revision;
    internal bool NeedsRecording => (!ReferenceEquals(_recorded, Version) || _recordedRevision != Revision) && NeedsUpload;
    internal void Upload(nint command)
    {
        if (!NeedsUpload) return;
        var count = ArrayPixels?.Layers.Length ?? 1;
        var bytes = _transferBytes;
        var mapped = SDL.MapGPUTransferBuffer(_device, _transfer.DangerousGetHandle(), true);
        if (mapped == 0) throw CanvasBackend.Failure("map texture upload memory");
        try
        {
            var offset = 0;
            for (var layer = 0; layer < count; layer++)
            {
                var pixels = ArrayPixels?.Layers[layer] ?? Pixels; var data = (_blockUpload ? pixels.Source : pixels.Upload).Data;
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
                    offset = checked(offset + Image.TextureLevelByteSize(width, height, _blockUpload ? pixels.Source.Format : pixels.Upload.Format));
                    width = Math.Max(1, width / 2); height = Math.Max(1, height / 2);
                }
            }
            System.Diagnostics.Debug.Assert(offset == bytes);
        }
        finally { SDL.EndGPUCopyPass(pass); }
    }
    private static SDL.GPUTextureFormat BCTextureFormat(Image.Format format) => format switch
    {
        Image.Format.Dxt1 => SDL.GPUTextureFormat.BC1RGBAUnorm,
        Image.Format.Dxt3 => SDL.GPUTextureFormat.BC2RGBAUnorm,
        Image.Format.Dxt5 => SDL.GPUTextureFormat.BC3RGBAUnorm,
        Image.Format.RgtcR => SDL.GPUTextureFormat.BC4RUnorm,
        Image.Format.RgtcRg => SDL.GPUTextureFormat.BC5RGUnorm,
        Image.Format.BptcRgba => SDL.GPUTextureFormat.BC7RGBAUnorm,
        Image.Format.BptcRgbf => SDL.GPUTextureFormat.BC6HRGBFloat,
        Image.Format.BptcRgbfu => SDL.GPUTextureFormat.BC6HRGBUFloat,
        _ => SDL.GPUTextureFormat.Invalid
    };
    internal void RecordUpload() { _recorded = Version; _recordedRevision = Revision; }
    internal void CancelUpload() => _recorded = null;
    internal void CommitUpload() { _uploadedRevision = _recorded is null ? Revision : _recordedRevision; _uploaded = _recorded ?? Version; _recorded = null; }
    public void Dispose() { _texture.Dispose(); _transfer.Dispose(); }
}
