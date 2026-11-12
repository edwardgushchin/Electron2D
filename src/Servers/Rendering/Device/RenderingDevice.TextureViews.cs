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
    /// <summary>Creates an image view sharing the original texture allocation.</summary>
    /// <param name="view">Copied format and sampled component mapping.</param>
    /// <param name="withTexture">A live source identity; an existing shared view resolves to its original owner.</param>
    /// <returns>A device-owned view identity, automatically freed with the original owner.</returns>
    /// <remarks>No image data is copied. The view includes the owner's complete image/layer/mip extent. FormatOverride must be declared shareable. GetFormat retains the owner's stored format; shader and attachment interfaces consume the selected view format. A viewport view selects the completed buffer at command recording. Direct3D 12 supports exact-format, single-sample sampled color images and complete color arrays.</remarks>
    /// <exception cref="NotSupportedException">The native view adapter or selected format role is unavailable.</exception>
    public RID TextureCreateShared(RDTextureView view, RID withTexture)
    {
        IdleList(); ArgumentNullException.ThrowIfNull(view); var (ownerID, owner) = TextureViewOwner(withTexture); var info = owner.Texture!;
        return CreateTextureView(view.FormatOverride, TextureSwizzles.Capture(view), ownerID, owner, 0, info.Layers, 0, info.Mips, info.Type);
    }
    /// <summary>Creates one shared image layer with a bounded mip interval.</summary>
    /// <param name="view">Copied format and sampled component mapping.</param>
    /// <param name="withTexture">A live source identity; an existing shared view resolves to its original owner.</param>
    /// <param name="layer">Zero-based layer of the original allocation; zero for an ordinary image.</param>
    /// <param name="mipmap">First mip of the original allocation.</param>
    /// <param name="mipmaps">Positive number of included mip levels.</param>
    /// <param name="sliceType">The ordinary image role.</param>
    /// <returns>A device-owned ordinary image view, automatically freed with the original owner.</returns>
    /// <remarks>A viewport slice selects the completed buffer at command recording. On Direct3D 12 an ordinary mip view requires a single-layer color allocation.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The layer, mip interval or slice selector is invalid.</exception>
    /// <exception cref="NotSupportedException">The native view adapter or selected format role is unavailable.</exception>
    public RID TextureCreateSharedFromSlice(RDTextureView view, RID withTexture, uint layer, uint mipmap, uint mipmaps = 1, TextureSliceType sliceType = TextureSliceType.Texture)
    {
        IdleList(); ArgumentNullException.ThrowIfNull(view); if (sliceType != TextureSliceType.Texture) throw new ArgumentOutOfRangeException(nameof(sliceType));
        var (ownerID, owner) = TextureViewOwner(withTexture); var info = owner.Texture!;
        if (layer >= info.Layers) throw new ArgumentOutOfRangeException(nameof(layer));
        if (mipmaps == 0 || mipmap >= info.Mips || mipmaps > info.Mips - mipmap) throw new ArgumentOutOfRangeException(nameof(mipmaps));
        return CreateTextureView(view.FormatOverride, TextureSwizzles.Capture(view), ownerID, owner, layer, 1, mipmap, mipmaps, TextureType.Texture);
    }
    /// <summary>Reports whether a live texture is a view of another texture's allocation.</summary>
    /// <param name="texture">The identity to inspect on this device's owning thread.</param>
    /// <returns>True for a live shared view. Invalid, foreign and allocation-owner identities return false.</returns>
    public bool TextureIsShared(RID texture)
    {
        Access();
        return _entries.TryGetValue(texture, out var entry) && entry.TextureOwner.IsValid() && entry.Alive;
    }
    internal RID CanvasSRGBTexture(RID source, bool allowBase = false)
    {
        Access(true);
        if (!_entries.TryGetValue(source, out var entry) || entry.Texture is not { } info || !entry.Alive) return default;
        var format = SRGBViewFormat(info.Format);
        if (format == info.Format) return source;
        var sampledOnly = (entry.TextureSource ?? entry).Borrowed?.HasSRGBBacking == true;
        if (format == DataFormat.Max || (!sampledOnly && !info.Shareable.Contains(format)))
        {
            if (allowBase) return source;
            throw new NotSupportedException("This texture allocation does not declare a compatible sRGB view.");
        }
        if (entry.SRGBView.IsValid() && _entries.TryGetValue(entry.SRGBView, out var alias) && alias.Alive)
            return entry.SRGBView;

        IdleList();
        var (ownerID, owner) = TextureViewOwner(source);
        try
        {
            entry.SRGBView = CreateTextureView(format, info.Swizzle, ownerID, owner, info.BaseLayer, info.Layers, info.BaseMip, info.Mips, info.Type, sampledOnly);
            return entry.SRGBView;
        }
        catch (NotSupportedException) when (allowBase) { return source; }
    }
    private static DataFormat SRGBViewFormat(DataFormat format) => format switch
    {
        DataFormat.R8G8B8A8UNORM or DataFormat.R8G8B8A8SRGB => DataFormat.R8G8B8A8SRGB,
        DataFormat.B8G8R8A8UNORM or DataFormat.B8G8R8A8SRGB => DataFormat.B8G8R8A8SRGB,
        DataFormat.BC1RGBAUNORMBLOCK or DataFormat.BC1RGBASRGBBLOCK => DataFormat.BC1RGBASRGBBLOCK,
        DataFormat.BC2UNORMBLOCK or DataFormat.BC2SRGBBLOCK => DataFormat.BC2SRGBBLOCK,
        DataFormat.BC3UNORMBLOCK or DataFormat.BC3SRGBBLOCK => DataFormat.BC3SRGBBLOCK,
        DataFormat.BC7UNORMBLOCK or DataFormat.BC7SRGBBLOCK => DataFormat.BC7SRGBBLOCK,
        DataFormat.ASTC4X4UNORMBLOCK or DataFormat.ASTC4X4SRGBBLOCK => DataFormat.ASTC4X4SRGBBLOCK,
        DataFormat.ASTC5X4UNORMBLOCK or DataFormat.ASTC5X4SRGBBLOCK => DataFormat.ASTC5X4SRGBBLOCK,
        DataFormat.ASTC5X5UNORMBLOCK or DataFormat.ASTC5X5SRGBBLOCK => DataFormat.ASTC5X5SRGBBLOCK,
        DataFormat.ASTC6X5UNORMBLOCK or DataFormat.ASTC6X5SRGBBLOCK => DataFormat.ASTC6X5SRGBBLOCK,
        DataFormat.ASTC6X6UNORMBLOCK or DataFormat.ASTC6X6SRGBBLOCK => DataFormat.ASTC6X6SRGBBLOCK,
        DataFormat.ASTC8X5UNORMBLOCK or DataFormat.ASTC8X5SRGBBLOCK => DataFormat.ASTC8X5SRGBBLOCK,
        DataFormat.ASTC8X6UNORMBLOCK or DataFormat.ASTC8X6SRGBBLOCK => DataFormat.ASTC8X6SRGBBLOCK,
        DataFormat.ASTC8X8UNORMBLOCK or DataFormat.ASTC8X8SRGBBLOCK => DataFormat.ASTC8X8SRGBBLOCK,
        DataFormat.ASTC10X5UNORMBLOCK or DataFormat.ASTC10X5SRGBBLOCK => DataFormat.ASTC10X5SRGBBLOCK,
        DataFormat.ASTC10X6UNORMBLOCK or DataFormat.ASTC10X6SRGBBLOCK => DataFormat.ASTC10X6SRGBBLOCK,
        DataFormat.ASTC10X8UNORMBLOCK or DataFormat.ASTC10X8SRGBBLOCK => DataFormat.ASTC10X8SRGBBLOCK,
        DataFormat.ASTC10X10UNORMBLOCK or DataFormat.ASTC10X10SRGBBLOCK => DataFormat.ASTC10X10SRGBBLOCK,
        DataFormat.ASTC12X10UNORMBLOCK or DataFormat.ASTC12X10SRGBBLOCK => DataFormat.ASTC12X10SRGBBLOCK,
        DataFormat.ASTC12X12UNORMBLOCK or DataFormat.ASTC12X12SRGBBLOCK => DataFormat.ASTC12X12SRGBBLOCK,
        _ => DataFormat.Max
    };
    private (RID, Entry) TextureViewOwner(RID source)
    {
        var entry = Get(source, Kind.Texture);
        return entry.TextureOwner == default ? (source, entry) : (entry.TextureOwner, Get(entry.TextureOwner, Kind.Texture));
    }
    private void RequireTextureViews()
    {
        if (GPUTextureViews.Support(Device, 1) != 1) throw new NotSupportedException("This texture role requires the pinned native image/view adapter.");
    }
    private RID CreateTextureView(DataFormat formatOverride, uint swizzle, RID ownerID, Entry owner, uint layer, uint layers, uint mip, uint mips, TextureType type, bool sampledOnly = false)
    {
        var original = owner.Texture!;
        var direct3D = TextureDriver == "direct3d12";
        var nativeABI = direct3D ? GPUTextureViews.Direct3DViewABI : 1u;
        if (GPUTextureViews.Support(Device, nativeABI) != 1)
            throw new NotSupportedException("This texture role requires the pinned native image/view adapter.");
        if (direct3D && (sampledOnly || type == TextureType.Texture && original.Layers != 1 ||
            (original.Usage & (TextureUsageBits.InputAttachment | TextureUsageBits.DepthResolveAttachment)) != 0))
            throw new NotSupportedException("Direct3D 12 views require a supported exact-format sampled color texture and native view shape.");
        if (direct3D && type == TextureType.TextureArray && (original.Usage & TextureUsageBits.Storage) != 0)
            RequireStorageArrayViews();
        var stored = original.StoredFormat == DataFormat.Max ? original.Format : original.StoredFormat;
        var format = formatOverride == DataFormat.Max ? stored : formatOverride;
        if (format != stored && !sampledOnly && !original.Shareable.Contains(format)) throw new ArgumentException("The view format is not declared shareable.", "view");
        if (direct3D && format != stored) throw new NotSupportedException("Direct3D 12 views retain the exact source format.");
        var create = NativeTextureInfo(original with { Format = stored });
        if (stored == DataFormat.S8UINT)
        {
            if (format != DataFormat.S8UINT || !StencilSupported(original.Usage, original.Samples)) throw new NotSupportedException("A stencil-only view retains its native format and supported roles.");
        }
        else if (!sampledOnly && GPUTextureViews.FormatSupport(Device, nativeABI, in create, NativeFormat(format)) != 1) throw new NotSupportedException("The shared native format is incompatible or lacks the requested usage/sample roles.");
        var usage = sampledOnly ? original.Usage & (TextureUsageBits.Sampling | TextureUsageBits.CanUpdate | TextureUsageBits.CanCopyFrom | TextureUsageBits.CanCopyTo | TextureUsageBits.CPURead) : original.Usage;
        var info = CaptureTextureInfo(format, type, Math.Max(1, original.Width >> (int)mip), Math.Max(1, original.Height >> (int)mip),
            layers, mips, usage, original.Shareable, original.Samples, original.Resolve, swizzle, stored)
        with
        {
            BaseLayer = layer,
            BaseMip = mip
        };
        var ownedViews = owner.TextureViews ??= []; ownedViews.EnsureCapacity(ownedViews.Count + 1);
        var entry = new Entry { Kind = Kind.Texture, Texture = info, Size = info.Total, TextureOwner = ownerID, TextureSource = owner };
        var abi = sampledOnly ? GPUTextureViews.SampledFormatABI : nativeABI;
        var nativeFormat = NativeFormat(format);
        var array = type == TextureType.TextureArray ? 1u : 0u;
        RenderHandle CreateNativeView(nint image) =>
            new(GPUTextureViews.View(Device, abi, image, nativeFormat, layer, layers, mip, mips, array), h => GPUTextureViews.Destroy(Device, nativeABI, h), _context.Handle);
        try
        {
            if (owner.Borrowed is { Target: not null } viewport)
            {
                var (first, second) = viewport.ViewportImages;
                entry.Handle = CreateNativeView(first.DangerousGetHandle());
                entry.SecondTextureView = CreateNativeView(second.DangerousGetHandle());
            }
            else entry.Handle = CreateNativeView(owner.TextureAllocation?.DangerousGetHandle() ?? owner.NativeHandle);
            PrepareInputShadow(entry); var rid = Add(entry); ownedViews.Add(rid); return rid;
        }
        catch { entry.InputShadow?.Dispose(); entry.SecondTextureView?.Dispose(); entry.Handle?.Dispose(); throw; }
    }
    private static TextureInfo CaptureTextureInfo(DataFormat format, TextureType type, uint width, uint height, uint layers, uint mips, TextureUsageBits usage, DataFormat[] shareable, TextureSamples samples, bool resolve, uint swizzle, DataFormat stored)
    {
        if (samples == TextureSamples.Samples1 && DepthFormat(format)) return CaptureDepthInfo(format, type, width, height, layers, mips, usage, shareable, swizzle, stored) with { Resolve = resolve };
        if (samples != TextureSamples.Samples1) return new(format, type, width, height, layers, mips, usage, [], [], [], 0, 0, shareable, samples, resolve, swizzle, stored);
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
    // Storage-only multisample and input-only depth images retain private targets for zero initialization.
    private static SDL.GPUTextureCreateInfo NativeTextureInfo(TextureInfo info) => new()
    {
        Type = info.Type == TextureType.Texture ? SDL.GPUTextureType.TextureType2D : SDL.GPUTextureType.TextureType2DArray,
        Format = NativeFormat(info.Format),
        Width = info.Width,
        Height = info.Height,
        LayerCountOrDepth = info.Layers,
        NumLevels = info.Mips,
        SampleCount = (SDL.GPUSampleCount)info.Samples,
        Usage = NativeUsage((info.Usage | (info.Samples != TextureSamples.Samples1 && (info.Usage & TextureUsageBits.Storage) != 0 ? TextureUsageBits.ColorAttachment : 0) | (DepthFormat(info.Format) && (info.Usage & TextureUsageBits.InputAttachment) != 0 ? TextureUsageBits.DepthStencilAttachment : 0)) & ~(info.Type == TextureType.TextureArray ? TextureUsageBits.InputAttachment : 0))
    };
    private RenderHandle CreateTextureStorage(Entry entry, in SDL.GPUTextureCreateInfo create, DataFormat[] shareable, DataFormat viewedFormat)
    {
        if (entry.Texture!.Format == DataFormat.S8UINT)
        {
            if (viewedFormat != DataFormat.S8UINT || shareable.Any(format => format != DataFormat.S8UINT)) throw new ArgumentException("Stencil-only views retain their exact format.", nameof(shareable));
            return CreateStencilTexture(create);
        }
        var allocationFormat = create.Format;
        if (shareable.Length != 0 && !shareable.Contains(entry.Texture!.StoredFormat == DataFormat.Max ? entry.Texture.Format : entry.Texture.StoredFormat)) throw new ArgumentException("Shareable formats must include the allocation format.", nameof(shareable));
        var mutable = shareable.Any(f => NativeFormat(f) != allocationFormat);
        RenderHandle storage;
        if (mutable || DepthFormat(entry.Texture!.Format) && (entry.Texture.Usage & TextureUsageBits.InputAttachment) != 0 || create.SampleCount != SDL.GPUSampleCount.SampleCount1 && (create.Usage & (SDL.GPUTextureUsageFlags.Sampler | SDL.GPUTextureUsageFlags.ComputeStorageWrite)) != 0)
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
