using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private void ValidateBinding(Binding binding, RID[] ids)
    {
        if (ids.Length != (binding.Type == UniformType.SamplerWithTexture ? 2 : 1)) throw new ArgumentException("Incorrect descriptor resource count.", nameof(ids));
        if (binding.Type is UniformType.StorageBuffer or UniformType.UniformBuffer) { Get(ids[0], binding.Type == UniformType.StorageBuffer ? Kind.Storage : Kind.Uniform); return; }
        var texture = Get(ids[binding.Type == UniformType.SamplerWithTexture ? 1 : 0], Kind.Texture).Texture!;
        if (PixelType(texture.Format) != binding.PixelType) throw new ArgumentException("The shader pixel scalar type does not match the texture format.", nameof(ids));
        if ((texture.Type == TextureType.TextureArray) != binding.Array) throw new ArgumentException("The texture array role does not match the shader.", nameof(ids));
        if (binding.Type == UniformType.SamplerWithTexture)
        {
            var sampler = Get(ids[0], Kind.Sampler); if ((texture.Usage & TextureUsageBits.Sampling) == 0) throw new ArgumentException("Texture lacks the sampling role.", nameof(ids));
            if (binding.PixelType != 0 && sampler.LinearSampling) throw new ArgumentException("Integer textures require nearest filtering without anisotropy.", nameof(ids));
        }
        else
        {
            if ((texture.Usage & TextureUsageBits.Storage) == 0) throw new ArgumentException("Texture lacks the storage role.", nameof(ids));
            if (binding.ImageFormat != 0 && StorageImageFormat(binding.ImageFormat) != texture.Format) throw new ArgumentException("Storage image format does not match the shader declaration.", nameof(ids));
        }
    }
    private static int PixelType(DataFormat format) => format switch
    {
        DataFormat.R8UINT => 2,
        DataFormat.R8G8UINT => 2,
        DataFormat.R8G8B8A8UINT => 2,
        DataFormat.R16UINT => 2,
        DataFormat.R16G16UINT => 2,
        DataFormat.R16G16B16A16UINT => 2,
        DataFormat.R32UINT => 2,
        DataFormat.R32G32UINT => 2,
        DataFormat.R32G32B32A32UINT => 2,
        DataFormat.R8SINT => 1,
        DataFormat.R8G8SINT => 1,
        DataFormat.R8G8B8A8SINT => 1,
        DataFormat.R16SINT => 1,
        DataFormat.R16G16SINT => 1,
        DataFormat.R16G16B16A16SINT => 1,
        DataFormat.R32SINT => 1,
        DataFormat.R32G32SINT => 1,
        DataFormat.R32G32B32A32SINT => 1,
        _ => 0
    };
    private static DataFormat StorageImageFormat(uint format) => format switch
    {
        1 => DataFormat.R32G32B32A32SFLOAT,
        2 => DataFormat.R16G16B16A16SFLOAT,
        3 => DataFormat.R32SFLOAT,
        4 => DataFormat.R8G8B8A8UNORM,
        5 => DataFormat.R8G8B8A8SNORM,
        6 => DataFormat.R32G32SFLOAT,
        7 => DataFormat.R16G16SFLOAT,
        8 => DataFormat.B10G11R11UFLOATPACK32,
        9 => DataFormat.R16SFLOAT,
        10 => DataFormat.R16G16B16A16UNORM,
        11 => DataFormat.A2B10G10R10UNORMPACK32,
        12 => DataFormat.R16G16UNORM,
        13 => DataFormat.R8G8UNORM,
        14 => DataFormat.R16UNORM,
        15 => DataFormat.R8UNORM,
        16 => DataFormat.R16G16B16A16SNORM,
        17 => DataFormat.R16G16SNORM,
        18 => DataFormat.R8G8SNORM,
        19 => DataFormat.R16SNORM,
        20 => DataFormat.R8SNORM,
        21 => DataFormat.R32G32B32A32SINT,
        22 => DataFormat.R16G16B16A16SINT,
        23 => DataFormat.R8G8B8A8SINT,
        24 => DataFormat.R32SINT,
        25 => DataFormat.R32G32SINT,
        26 => DataFormat.R16G16SINT,
        27 => DataFormat.R8G8SINT,
        28 => DataFormat.R16SINT,
        29 => DataFormat.R8SINT,
        30 => DataFormat.R32G32B32A32UINT,
        31 => DataFormat.R16G16B16A16UINT,
        32 => DataFormat.R8G8B8A8UINT,
        33 => DataFormat.R32UINT,
        34 => DataFormat.A2B10G10R10UINTPACK32,
        35 => DataFormat.R32G32UINT,
        36 => DataFormat.R16G16UINT,
        37 => DataFormat.R8G8UINT,
        38 => DataFormat.R16UINT,
        39 => DataFormat.R8UINT,
        40 => DataFormat.R64UINT,
        41 => DataFormat.R64SINT,
        _ => DataFormat.Max
    };
    internal sealed record TextureInfo(DataFormat Format, TextureType Type, uint Width, uint Height, uint Layers, uint Mips, TextureUsageBits Usage, uint[] Offsets, uint[] PackedOffsets, uint[] Sizes, uint Total, uint TransferSize, DataFormat[] Shareable);
    private static SDL.GPUTextureUsageFlags NativeUsage(TextureUsageBits usage)
    {
        var native = (SDL.GPUTextureUsageFlags)0;
        if ((usage & TextureUsageBits.Sampling) != 0) native |= SDL.GPUTextureUsageFlags.Sampler;
        if ((usage & TextureUsageBits.Storage) != 0) native |= SDL.GPUTextureUsageFlags.ComputeStorageRead | SDL.GPUTextureUsageFlags.ComputeStorageWrite | SDL.GPUTextureUsageFlags.ComputeStorageSimultaneousReadWrite;
        if (native == 0) native = SDL.GPUTextureUsageFlags.Sampler;
        return native;
    }
    /// <summary>Reports whether the selected format supports the requested two-dimensional texture usage.</summary>
    /// <param name="format">A concrete format to query.</param>
    /// <param name="usageFlags">Requested allocation and transfer roles.</param>
    /// <returns>False for formats or roles unavailable on this backend.</returns>
    public bool TextureIsFormatSupportedForUsage(DataFormat format, TextureUsageBits usageFlags)
    {
        Access(); RDTextureConfiguration.FormatValue(format); RDTextureConfiguration.Usage(usageFlags);
        if ((usageFlags & (TextureUsageBits.StorageAtomic | TextureUsageBits.InputAttachment | TextureUsageBits.DepthResolveAttachment | TextureUsageBits.DepthStencilAttachment | TextureUsageBits.ColorAttachment)) != 0) return false;
        var native = NativeFormat(format);
        return native != SDL.GPUTextureFormat.Invalid && SDL.GPUTextureSupportsFormat(Device, native, SDL.GPUTextureType.TextureType2D, NativeUsage(usageFlags));
    }
    /// <summary>Creates a device-owned two-dimensional image or image array with copied mip data.</summary>
    /// <param name="format">Allocation description copied before returning.</param>
    /// <param name="view">Initial view description; native format reinterpretation and swizzles require a view adapter.</param>
    /// <param name="data">One tightly packed mip chain per layer, or empty for zero initialization.</param>
    /// <returns>A texture RID belonging to this device.</returns>
    /// <exception cref="NotSupportedException">The requested format, view or allocation role is not supported.</exception>
    public RID TextureCreate(RDTextureFormat format, RDTextureView view, ReadOnlySpan<byte[]> data = default)
    {
        IdleList(); ArgumentNullException.ThrowIfNull(format); ArgumentNullException.ThrowIfNull(view);
        var shareable = format.ShareableFormats;
        var kind = format.TextureType; var layers = format.ArrayLayers; var width = format.Width; var height = format.Height; var mips = format.Mipmaps; var pixelFormat = format.Format; var usage = format.UsageBits;
        if (kind == TextureType.Texture && layers != 1) throw new ArgumentException("An ordinary image has one layer.", nameof(format));
        if (mips > 1 + System.Numerics.BitOperations.Log2(Math.Max(width, height))) throw new ArgumentException("The mip count exceeds this image.", nameof(format));

        if (view.FormatOverride != DataFormat.Max && view.FormatOverride != pixelFormat ||
            view.SwizzleR is not (TextureSwizzle.R or TextureSwizzle.Identity) || view.SwizzleG is not (TextureSwizzle.G or TextureSwizzle.Identity) ||
            view.SwizzleB is not (TextureSwizzle.B or TextureSwizzle.Identity) || view.SwizzleA is not (TextureSwizzle.A or TextureSwizzle.Identity) ||
            shareable.Any(f => f != pixelFormat)) throw new NotSupportedException("Alternate native formats and channel views require texture-view integration.");
        if (!TextureIsFormatSupportedForUsage(pixelFormat, usage)) throw new NotSupportedException("The device does not support this texture format/usage.");
        if (!data.IsEmpty && data.Length != layers) throw new ArgumentException("Provide one mip chain per layer.", nameof(data));
        var native = NativeFormat(pixelFormat); var offsets = new uint[mips]; var packed = new uint[mips]; var sizes = new uint[mips]; uint total = 0, transferSize = 0;
        var alignment = Math.Max(4u, SDL.GPUTextureFormatTexelBlockSize(native));
        for (var mip = 0; mip < mips; mip++)
        {
            packed[mip] = total; offsets[mip] = checked((transferSize + alignment - 1) / alignment * alignment); sizes[mip] = TextureBytes(native, Math.Max(1, width >> mip), Math.Max(1, height >> mip));
            if ((ulong)total + sizes[mip] > int.MaxValue || (ulong)offsets[mip] + sizes[mip] > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(format), "The mip chain exceeds managed transfer capacity.");
            total += sizes[mip]; transferSize = offsets[mip] + sizes[mip];
        }
        if (total == 0 || total > int.MaxValue || transferSize > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(format));
        foreach (var bytes in data) if (bytes is null || bytes.Length != total) throw new ArgumentException("Each mip chain must have the exact byte size.", nameof(data));
        var info = new TextureInfo(pixelFormat, kind, width, height, layers, mips, usage, offsets, packed, sizes, total, transferSize, shareable);
        var entry = new Entry { Kind = Kind.Texture, Texture = info, Size = total };
        try
        {
            entry.Handle = new(SDL.CreateGPUTexture(Device, new() { Type = kind == TextureType.Texture ? SDL.GPUTextureType.TextureType2D : SDL.GPUTextureType.TextureType2DArray, Format = native, Width = width, Height = height, LayerCountOrDepth = layers, NumLevels = mips, SampleCount = SDL.GPUSampleCount.SampleCount1, Usage = NativeUsage(usage) }), h => SDL.ReleaseGPUTexture(Device, h), _context.Handle);
            entry.Upload = Transfer(transferSize, SDL.GPUTransferBufferUsage.Upload);
            var zero = data.IsEmpty ? new byte[total] : null;
            for (uint layer = 0; layer < layers; layer++) UploadTexture(entry, layer, data.IsEmpty ? zero : data[(int)layer]);
            return Add(entry);
        }
        catch { entry.Handle?.Dispose(); entry.Upload?.Dispose(); throw; }
    }
    private static void Layer(Entry entry, uint layer) { if (layer >= entry.Texture!.Layers) throw new ArgumentOutOfRangeException(nameof(layer)); }
    private static SDL.GPUTextureRegion Region(Entry entry, uint layer, int mip) => new() { Texture = entry.Handle!.DangerousGetHandle(), Layer = layer, MipLevel = (uint)mip, W = Math.Max(1, entry.Texture!.Width >> mip), H = Math.Max(1, entry.Texture.Height >> mip), D = 1 };
    private void UploadTexture(Entry entry, uint layer, ReadOnlySpan<byte> data)
    {
        var command = Commands(); var pointer = SDL.MapGPUTransferBuffer(Device, entry.Upload!.DangerousGetHandle(), true);
        if (pointer == 0) throw GPUPhysicsDevice.Failure("map texture upload");
        try { for (var mip = 0; mip < entry.Texture!.Mips; mip++) data.Slice((int)entry.Texture.PackedOffsets[mip], (int)entry.Texture.Sizes[mip]).CopyTo(new Span<byte>((void*)(pointer + (nint)entry.Texture.Offsets[mip]), (int)entry.Texture.Sizes[mip])); }
        finally { SDL.UnmapGPUTransferBuffer(Device, entry.Upload.DangerousGetHandle()); }
        var pass = SDL.BeginGPUCopyPass(command); if (pass == 0) throw GPUPhysicsDevice.Failure("begin texture upload");
        try { for (var mip = 0; mip < entry.Texture!.Mips; mip++) SDL.UploadToGPUTexture(pass, new() { TransferBuffer = entry.Upload.DangerousGetHandle(), Offset = entry.Texture.Offsets[mip] }, Region(entry, layer, mip), false); }
        finally { SDL.EndGPUCopyPass(pass); }
    }
    /// <summary>Updates one complete layer including every mip level.</summary>
    /// <param name="texture">Live texture owned by this device.</param>
    /// <param name="layer">Zero-based layer.</param>
    /// <param name="data">Exact tightly packed layer mip-chain bytes.</param>
    public void TextureUpdate(RID texture, uint layer, ReadOnlySpan<byte> data)
    {
        IdleList(); var entry = Get(texture, Kind.Texture); Layer(entry, layer);
        if ((entry.Texture!.Usage & TextureUsageBits.CanUpdate) == 0) throw new InvalidOperationException("The texture has no update role.");
        if (data.Length != entry.Size) throw new ArgumentException("The layer byte size does not match.", nameof(data));
        UploadTexture(entry, layer, data);
    }
    /// <summary>Synchronizes and returns an independent tightly packed layer mip chain.</summary>
    /// <param name="texture">Live texture owned by this device.</param>
    /// <param name="layer">Zero-based layer.</param>
    /// <returns>A copied layer mip chain.</returns>
    public byte[] TextureGetData(RID texture, uint layer) { var entry = Get(texture, Kind.Texture); var bytes = new byte[entry.Size]; TextureGetData(texture, layer, bytes.AsSpan()); return bytes; }
    /// <summary>Synchronizes and reads one layer mip chain into caller-owned storage.</summary>
    /// <param name="texture">Live texture owned by this device.</param>
    /// <param name="layer">Zero-based layer.</param>
    /// <param name="destination">Storage with the exact layer byte capacity.</param>
    public void TextureGetData(RID texture, uint layer, Span<byte> destination)
    {
        IdleList(); var entry = ReadableTexture(texture, layer);
        if (destination.Length != entry.Size) throw new ArgumentException("The layer byte size does not match.", nameof(destination));
        entry.Download ??= Transfer(entry.Texture!.TransferSize, SDL.GPUTransferBufferUsage.Download);
        var pass = SDL.BeginGPUCopyPass(Commands()); if (pass == 0) throw GPUPhysicsDevice.Failure("begin texture read");
        try { for (var mip = 0; mip < entry.Texture!.Mips; mip++) SDL.DownloadFromGPUTexture(pass, Region(entry, layer, mip), new() { TransferBuffer = entry.Download.DangerousGetHandle(), Offset = entry.Texture.Offsets[mip] }); }
        finally { SDL.EndGPUCopyPass(pass); }
        SubmitRecorded(); CompleteSubmission(); var pointer = SDL.MapGPUTransferBuffer(Device, entry.Download.DangerousGetHandle(), false);
        if (pointer == 0) throw GPUPhysicsDevice.Failure("map texture read");
        try { for (var mip = 0; mip < entry.Texture!.Mips; mip++) new ReadOnlySpan<byte>((void*)(pointer + (nint)entry.Texture.Offsets[mip]), (int)entry.Texture.Sizes[mip]).CopyTo(destination.Slice((int)entry.Texture.PackedOffsets[mip], (int)entry.Texture.Sizes[mip])); }
        finally { SDL.UnmapGPUTransferBuffer(Device, entry.Download.DangerousGetHandle()); }
        DeliverReadbacks();
    }
    /// <summary>Queues a request-time layer snapshot and delivers an independent mip chain at device completion.</summary>
    /// <param name="texture">A live readable texture of this device.</param>
    /// <param name="layer">Zero-based layer.</param>
    /// <param name="callback">Receives tight mip-chain bytes on the creating thread.</param>
    /// <remarks>End the compute list and queue the read. Local devices use Submit/Sync; the retained renderer device completes before FramePostDraw. This shares buffer readback FIFO,
    /// callback-error, reentrancy and cancellation behavior. Later texture updates cannot replace the snapshot.</remarks>
    public void TextureGetDataAsync(RID texture, uint layer, Action<byte[]> callback)
    {
        IdleList(); ArgumentNullException.ThrowIfNull(callback); var entry = ReadableTexture(texture, layer);
        var data = new byte[entry.Size]; QueueReadback(entry, data, 0, callback, data, null, layer);
    }
    /// <summary>Queues a layer snapshot into reusable caller-owned storage.</summary>
    /// <param name="texture">A live readable texture of this device.</param>
    /// <param name="layer">Zero-based layer.</param>
    /// <param name="destination">Exact tight mip-chain capacity; keep valid and untouched until completion.</param>
    /// <param name="callback">Runs after the destination is filled on the owner thread.</param>
    /// <remarks>Preparation must cover the largest physical mip-chain size and peak concurrent request count.
    /// Device disposal cancels undelivered callbacks. Memory accessors cannot reenter this device.</remarks>
    public void TextureGetDataAsync(RID texture, uint layer, Memory<byte> destination, Action callback)
    {
        IdleList(); ArgumentNullException.ThrowIfNull(callback); var entry = ReadableTexture(texture, layer);
        if (destination.Length != entry.Size) throw new ArgumentException("The layer byte size does not match.", nameof(destination));
        QueueReadback(entry, destination, 0, null, null, callback, layer);
    }
    private Entry ReadableTexture(RID texture, uint layer)
    {
        var entry = Get(texture, Kind.Texture); Layer(entry, layer);
        if ((entry.Texture!.Usage & (TextureUsageBits.CPURead | TextureUsageBits.CanCopyFrom)) == 0) throw new InvalidOperationException("The texture has no readback role.");
        return entry;
    }
    /// <summary>Reports whether an identity is a live texture of this device.</summary>
    /// <param name="texture">The identity to inspect.</param>
    /// <returns>True only for a live owned texture.</returns>
    public bool TextureIsValid(RID texture) { Access(); return _entries.TryGetValue(texture, out var entry) && entry.Kind == Kind.Texture; }
    /// <summary>Copies the actual texture allocation description.</summary>
    /// <param name="texture">Live texture owned by this device.</param>
    /// <returns>A caller-owned description independent of the allocation.</returns>
    public RDTextureFormat TextureGetFormat(RID texture)
    {
        var info = Get(texture, Kind.Texture).Texture!;
        var result = new RDTextureFormat { Width = info.Width, Height = info.Height, ArrayLayers = info.Layers, Mipmaps = info.Mips, TextureType = info.Type, Format = info.Format, UsageBits = info.Usage };
        foreach (var format in info.Shareable) result.AddShareableFormat(format);
        return result;
    }
    /// <summary>Creates a device-owned sampler from copied filtering, addressing and LOD state.</summary>
    /// <param name="state">Caller-owned sampler configuration.</param>
    /// <returns>An owned sampler identity.</returns>
    /// <exception cref="NotSupportedException">Border, mirror-clamp or unnormalized sampling requires a shader adapter.</exception>
    public RID SamplerCreate(RDSamplerState state)
    {
        IdleList(); ArgumentNullException.ThrowIfNull(state);
        static SDL.GPUSamplerAddressMode Address(SamplerRepeatMode mode) => mode switch { SamplerRepeatMode.Repeat => SDL.GPUSamplerAddressMode.Repeat, SamplerRepeatMode.MirroredRepeat => SDL.GPUSamplerAddressMode.MirroredRepeat, SamplerRepeatMode.ClampToEdge => SDL.GPUSamplerAddressMode.ClampToEdge, _ => throw new NotSupportedException("The native sampler has no border/mirror-clamp mode.") };
        if (state.MinLOD < 0 || state.MaxLOD < state.MinLOD) throw new ArgumentException("Invalid sampler LOD interval.", nameof(state));
        if (state.LODBias != 0 && SDL.GetGPUDeviceDriver(Device) == "metal") throw new NotSupportedException("This backend requires shader-applied LOD bias.");
        var info = new SDL.GPUSamplerCreateInfo { MinFilter = (SDL.GPUFilter)state.MinFilter, MagFilter = (SDL.GPUFilter)state.MagFilter, MipmapMode = (SDL.GPUSamplerMipmapMode)state.MipFilter, AddressModeU = Address(state.RepeatU), AddressModeV = Address(state.RepeatV), AddressModeW = SDL.GPUSamplerAddressMode.ClampToEdge, MipLodBias = state.LODBias, MaxAnisotropy = state.AnisotropyMax, EnableAnisotropy = state.UseAnisotropy, MinLod = state.MinLOD, MaxLod = state.MaxLOD };
        return Add(new Entry { Kind = Kind.Sampler, LinearSampling = state.MinFilter == SamplerFilter.Linear || state.MagFilter == SamplerFilter.Linear || state.MipFilter == SamplerFilter.Linear || state.UseAnisotropy, Handle = new(SDL.CreateGPUSampler(Device, info), h => SDL.ReleaseGPUSampler(Device, h), _context.Handle) });
    }
}
