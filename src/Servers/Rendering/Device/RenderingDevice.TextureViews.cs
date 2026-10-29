using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    /// <summary>Selects an ordinary image slice from texture storage.</summary>
    public enum TextureSliceType
    {
        /// <summary>Selects one image layer and the requested mip interval.</summary>
        Texture = 0
    }
    /// <summary>Creates a sampled/color/storage view sharing the original texture allocation.</summary>
    /// <param name="view">Copied format and sampled component mapping.</param>
    /// <param name="withTexture">A live source identity; an existing shared view resolves to its original owner.</param>
    /// <returns>A device-owned view identity, automatically freed with the original owner.</returns>
    /// <remarks>No image data is copied. The view includes the owner's complete image/layer/mip extent. FormatOverride must be declared shareable. GetFormat retains the owner's stored format; shader and attachment interfaces consume the selected view format.</remarks>
    /// <exception cref="NotSupportedException">The native view adapter or selected format role is unavailable.</exception>
    public RID TextureCreateShared(RDTextureView view, RID withTexture)
    {
        IdleList(); ArgumentNullException.ThrowIfNull(view); var (ownerID, owner) = TextureViewOwner(withTexture); var info = owner.Texture!;
        return CreateTextureView(view, ownerID, owner, 0, info.Layers, 0, info.Mips, info.Type);
    }
    /// <summary>Creates one shared image layer with a bounded mip interval.</summary>
    /// <param name="view">Copied format and sampled component mapping.</param>
    /// <param name="withTexture">A live source identity; an existing shared view resolves to its original owner.</param>
    /// <param name="layer">Zero-based layer of the original allocation; zero for an ordinary image.</param>
    /// <param name="mipmap">First mip of the original allocation.</param>
    /// <param name="mipmaps">Positive number of included mip levels.</param>
    /// <param name="sliceType">The ordinary image role.</param>
    /// <returns>A device-owned ordinary image view, automatically freed with the original owner.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The layer, mip interval or slice selector is invalid.</exception>
    /// <exception cref="NotSupportedException">The native view adapter or selected format role is unavailable.</exception>
    public RID TextureCreateSharedFromSlice(RDTextureView view, RID withTexture, uint layer, uint mipmap, uint mipmaps = 1, TextureSliceType sliceType = TextureSliceType.Texture)
    {
        IdleList(); ArgumentNullException.ThrowIfNull(view); if (sliceType != TextureSliceType.Texture) throw new ArgumentOutOfRangeException(nameof(sliceType));
        var (ownerID, owner) = TextureViewOwner(withTexture); var info = owner.Texture!;
        if (layer >= info.Layers) throw new ArgumentOutOfRangeException(nameof(layer));
        if (mipmaps == 0 || mipmap >= info.Mips || mipmaps > info.Mips - mipmap) throw new ArgumentOutOfRangeException(nameof(mipmaps));
        return CreateTextureView(view, ownerID, owner, layer, 1, mipmap, mipmaps, TextureType.Texture);
    }
    private (RID, Entry) TextureViewOwner(RID source)
    {
        var entry = Get(source, Kind.Texture);
        return entry.TextureOwner == default ? (source, entry) : (entry.TextureOwner, Get(entry.TextureOwner, Kind.Texture));
    }
    private void RequireTextureViews()
    {
        if (GPUTextureViews.Support(Device, 1) != 1) throw new NotSupportedException("This texture role requires the pinned native image/view adapter.");
    }
    private RID CreateTextureView(RDTextureView view, RID ownerID, Entry owner, uint layer, uint layers, uint mip, uint mips, TextureType type)
    {
        RequireTextureViews(); var original = owner.Texture!; var stored = original.StoredFormat == DataFormat.Max ? original.Format : original.StoredFormat;
        var format = view.FormatOverride == DataFormat.Max ? stored : view.FormatOverride;
        if (format != stored && !original.Shareable.Contains(format)) throw new ArgumentException("The view format is not declared shareable.", nameof(view));
        var create = NativeTextureInfo(original with { Format = stored });
        if (GPUTextureViews.FormatSupport(Device, 1, in create, NativeFormat(format)) != 1) throw new NotSupportedException("The shared native format is incompatible or lacks the requested usage/sample roles.");
        var info = CaptureTextureInfo(format, type, Math.Max(1, original.Width >> (int)mip), Math.Max(1, original.Height >> (int)mip), layers, mips, original.Usage, original.Shareable, original.Samples, original.Resolve, TextureSwizzles.Capture(view), stored);
        var ownedViews = owner.TextureViews ??= []; ownedViews.EnsureCapacity(ownedViews.Count + 1);
        var entry = new Entry { Kind = Kind.Texture, Texture = info, Size = info.Total, TextureOwner = ownerID, TextureSource = owner };
        try
        {
            entry.Handle = new(GPUTextureViews.View(Device, 1, owner.TextureAllocation?.DangerousGetHandle() ?? owner.NativeHandle, NativeFormat(format), layer, layers, mip, mips, type == TextureType.TextureArray ? 1u : 0u), h => GPUTextureViews.Destroy(Device, 1, h), _context.Handle);
            PrepareInputShadow(entry); var rid = Add(entry); ownedViews.Add(rid); return rid;
        }
        catch { entry.InputShadow?.Dispose(); entry.Handle?.Dispose(); throw; }
    }
    private static TextureInfo CaptureTextureInfo(DataFormat format, TextureType type, uint width, uint height, uint layers, uint mips, TextureUsageBits usage, DataFormat[] shareable, TextureSamples samples, bool resolve, uint swizzle, DataFormat stored)
    {
        if (samples != TextureSamples.Samples1 || DepthFormat(format)) return new(format, type, width, height, layers, mips, usage, [], [], [], 0, 0, shareable, samples, resolve, swizzle, stored);
        var native = NativeFormat(format); var offsets = new uint[mips]; var packed = new uint[mips]; var sizes = new uint[mips]; uint total = 0, transferSize = 0;
        var alignment = Math.Max(4u, SDL.GPUTextureFormatTexelBlockSize(native));
        for (var mip = 0; mip < mips; mip++)
        {
            packed[mip] = total; offsets[mip] = checked((transferSize + alignment - 1) / alignment * alignment); sizes[mip] = TextureBytes(native, Math.Max(1, width >> mip), Math.Max(1, height >> mip));
            if ((ulong)total + sizes[mip] > int.MaxValue || (ulong)offsets[mip] + sizes[mip] > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(format), "The mip chain exceeds managed transfer capacity.");
            total += sizes[mip]; transferSize = offsets[mip] + sizes[mip];
        }
        if (total == 0 || total > int.MaxValue || transferSize > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(format));
        return new(format, type, width, height, layers, mips, usage, offsets, packed, sizes, total, transferSize, shareable, samples, resolve, swizzle, stored);
    }
    // Storage-only multisample images retain a private color role for zero initialization.
    private static SDL.GPUTextureCreateInfo NativeTextureInfo(TextureInfo info) => new()
    {
        Type = info.Type == TextureType.Texture ? SDL.GPUTextureType.TextureType2D : SDL.GPUTextureType.TextureType2DArray,
        Format = NativeFormat(info.Format),
        Width = info.Width,
        Height = info.Height,
        LayerCountOrDepth = info.Layers,
        NumLevels = info.Mips,
        SampleCount = (SDL.GPUSampleCount)info.Samples,
        Usage = NativeUsage((info.Usage | (info.Samples != TextureSamples.Samples1 && (info.Usage & TextureUsageBits.Storage) != 0 ? TextureUsageBits.ColorAttachment : 0)) & ~(info.Type == TextureType.TextureArray ? TextureUsageBits.InputAttachment : 0))
    };
    private RenderHandle CreateTextureStorage(Entry entry, in SDL.GPUTextureCreateInfo create, DataFormat[] shareable, DataFormat viewedFormat)
    {
        var allocationFormat = create.Format;
        if (shareable.Length != 0 && !shareable.Contains(entry.Texture!.StoredFormat == DataFormat.Max ? entry.Texture.Format : entry.Texture.StoredFormat)) throw new ArgumentException("Shareable formats must include the allocation format.", nameof(shareable));
        var mutable = shareable.Any(f => NativeFormat(f) != allocationFormat);
        RenderHandle storage;
        if (mutable || create.SampleCount != SDL.GPUSampleCount.SampleCount1 && (create.Usage & (SDL.GPUTextureUsageFlags.Sampler | SDL.GPUTextureUsageFlags.ComputeStorageWrite)) != 0)
        {
            RequireTextureViews(); if (shareable.Length == 0) shareable = [entry.Texture!.StoredFormat == DataFormat.Max ? entry.Texture.Format : entry.Texture.StoredFormat];
            var formats = new SDL.GPUTextureFormat[shareable.Length];
            for (var i = 0; i < formats.Length; i++) { formats[i] = NativeFormat(shareable[i]); if (GPUTextureViews.FormatSupport(Device, 1, in create, formats[i]) != 1) throw new NotSupportedException("A shareable native format is incompatible or lacks the requested usage/sample roles."); }
            fixed (SDL.GPUTextureFormat* p = formats) storage = new(GPUTextureViews.Create(Device, 1, in create, (nint)p, (uint)formats.Length), h => GPUTextureViews.Destroy(Device, 1, h), _context.Handle);
        }
        else storage = new(SDL.CreateGPUTexture(Device, in create), h => SDL.ReleaseGPUTexture(Device, h), _context.Handle);
        try
        {
            if (NativeFormat(viewedFormat) == create.Format) return storage;
            if (!shareable.Contains(viewedFormat)) throw new ArgumentException("The initial view format is not declared shareable.", nameof(viewedFormat));
            var handle = new RenderHandle(GPUTextureViews.View(Device, 1, storage.DangerousGetHandle(), NativeFormat(viewedFormat), 0, create.LayerCountOrDepth, 0, create.NumLevels, create.Type == SDL.GPUTextureType.TextureType2DArray ? 1u : 0u), h => GPUTextureViews.Destroy(Device, 1, h), _context.Handle);
            entry.TextureAllocation = storage; return handle;
        }
        catch { storage.Dispose(); throw; }
    }
}
