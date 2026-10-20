using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private string? _textureDriver;
    private string TextureDriver => _textureDriver ??= SDL.GetGPUDeviceDriver(Device) ?? throw GPUPhysicsDevice.Failure("identify image transfer backend");

    /// <summary>Records a raw rectangular image copy on the GPU timeline.</summary>
    /// <param name="fromTexture">Live source with CanCopyFrom usage.</param>
    /// <param name="toTexture">Live destination with CanCopyTo usage.</param>
    /// <param name="fromPosition">Source texel coordinates; Z must be zero.</param>
    /// <param name="toPosition">Destination texel coordinates; Z must be zero.</param>
    /// <param name="size">Positive texel extent with Z equal to one.</param>
    /// <param name="sourceMipmap">Zero-based source mip level.</param>
    /// <param name="destinationMipmap">Zero-based destination mip level.</param>
    /// <param name="sourceLayer">Zero-based source image layer.</param>
    /// <param name="destinationLayer">Zero-based destination image layer.</param>
    /// <remarks>Coordinates are truncated to integral texels after bounds validation. Matching encoded
    /// texel/block footprints may be reinterpreted on Vulkan; other backends currently require the same
    /// format. Compressed offsets must begin on blocks and partial blocks must reach both image edges.
    /// Same-subresource rectangles must not overlap. A prepared native scratch image preserves disjoint
    /// self copies. This operation neither submits nor invokes readback callbacks.</remarks>
    /// <exception cref="ArgumentException">The texture, region, format footprint or overlap is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A coordinate, mip level or layer is out of range.</exception>
    /// <exception cref="InvalidOperationException">A required usage is absent, the device state is invalid or native recording fails.</exception>
    /// <exception cref="NotSupportedException">This backend cannot reinterpret the selected formats.</exception>
    public void TextureCopy(RID fromTexture, RID toTexture, Vector3 fromPosition, Vector3 toPosition, Vector3 size,
        uint sourceMipmap, uint destinationMipmap, uint sourceLayer, uint destinationLayer)
    {
        IdleList(); var source = Get(fromTexture, Kind.Texture); var destination = Get(toTexture, Kind.Texture);
        if (source.Texture!.Samples != TextureSamples.Samples1 || destination.Texture!.Samples != TextureSamples.Samples1) throw new NotSupportedException("Use multisample resolve rather than raw sample copies.");
        TextureRole(source, TextureUsageBits.CanCopyFrom); TextureRole(destination, TextureUsageBits.CanCopyTo);
        TextureSubresource(source, sourceMipmap, sourceLayer); TextureSubresource(destination, destinationMipmap, destinationLayer);
        if (!size.IsFinite() || size.X < 1 || size.Y < 1 || size.Z != 1) throw new ArgumentOutOfRangeException(nameof(size));
        var a = CopyLocation(source, sourceMipmap, sourceLayer, fromPosition, size);
        var b = CopyLocation(destination, destinationMipmap, destinationLayer, toPosition, size);
        var width = checked((uint)size.X); var height = checked((uint)size.Y);
        var sourceFormat = NativeFormat(source.Texture!.Format); var destinationFormat = NativeFormat(destination.Texture!.Format);
        if (BlockDimensions(sourceFormat) != BlockDimensions(destinationFormat) || SDL.GPUTextureFormatTexelBlockSize(sourceFormat) != SDL.GPUTextureFormatTexelBlockSize(destinationFormat)) throw new ArgumentException("Texture formats have different encoded footprints.");
        // shortcut: other drivers need a verified compatible-format adapter before reinterpretation is enabled.
        if (sourceFormat != destinationFormat && TextureDriver != "vulkan") throw new NotSupportedException("Format reinterpretation requires this backend's compatible-view adapter.");
        CopyBlocks(source, a, width, height); CopyBlocks(destination, b, width, height);
        var self = ReferenceEquals(source, destination) && sourceMipmap == destinationMipmap && sourceLayer == destinationLayer;
        if (self && a.X < (ulong)b.X + width && b.X < (ulong)a.X + width && a.Y < (ulong)b.Y + height && b.Y < (ulong)a.Y + height) throw new ArgumentException("Source and destination rectangles overlap.");
        var scratch = self ? PrepareTextureScratch(source, false) : null;
        var pass = SDL.BeginGPUCopyPass(Commands()); if (pass == 0) throw GPUPhysicsDevice.Failure("begin image region copy");
        try
        {
            if (scratch is not null)
            {
                var temporary = a; temporary.Texture = scratch.DangerousGetHandle(); temporary.Layer = 0;
                SDL.CopyGPUTextureToTexture(pass, a, temporary, width, height, 1, false); a = temporary;
            }
            SDL.CopyGPUTextureToTexture(pass, a, b, width, height, 1, false);
        }
        finally { SDL.EndGPUCopyPass(pass); }
    }

    private static void TextureRole(Entry entry, TextureUsageBits role)
    {
        if ((entry.Texture!.Usage & role) == 0) throw new InvalidOperationException("The texture lacks the requested transfer role.");
    }
    private static void TextureSubresource(Entry entry, uint mipmap, uint layer)
    {
        Layer(entry, layer); if (mipmap >= entry.Texture!.Mips) throw new ArgumentOutOfRangeException(nameof(mipmap));
    }
    private static SDL.GPUTextureLocation CopyLocation(Entry entry, uint mip, uint layer, Vector3 position, Vector3 size)
    {
        if (!position.IsFinite() || position.X < 0 || position.Y < 0 || position.Z != 0 ||
            (double)position.X + size.X > Math.Max(1, entry.Texture!.Width >> (int)mip) ||
            (double)position.Y + size.Y > Math.Max(1, entry.Texture.Height >> (int)mip)) throw new ArgumentOutOfRangeException(nameof(position));
        return new() { Texture = entry.NativeHandle, MipLevel = mip, Layer = layer, X = checked((uint)position.X), Y = checked((uint)position.Y) };
    }
    private static void CopyBlocks(Entry entry, SDL.GPUTextureLocation location, uint width, uint height)
    {
        var block = BlockDimensions(NativeFormat(entry.Texture!.Format));
        var mipWidth = Math.Max(1, entry.Texture.Width >> (int)location.MipLevel); var mipHeight = Math.Max(1, entry.Texture.Height >> (int)location.MipLevel);
        if (location.X % block.Width != 0 || location.Y % block.Height != 0 ||
            width % block.Width != 0 && location.X + width != mipWidth || height % block.Height != 0 && location.Y + height != mipHeight) throw new ArgumentException("Compressed copies require aligned blocks or complete image edges.");
    }
    private RenderHandle PrepareTextureScratch(Entry entry, bool clear)
    {
        var info = entry.Texture!; var format = NativeFormat(info.Format);
        if (entry.TextureScratch is null)
        {
            entry.ScratchColorTarget = SDL.GPUTextureSupportsFormat(Device, format, SDL.GPUTextureType.TextureType2D, SDL.GPUTextureUsageFlags.ColorTarget);
            if (clear && !entry.ScratchColorTarget) throw new NotSupportedException("This format requires a color-clear adapter; encoded blocks cannot be rendered as color targets.");
            entry.TextureScratch = new(SDL.CreateGPUTexture(Device, new() { Type = SDL.GPUTextureType.TextureType2D, Format = format, Width = info.Width, Height = info.Height, LayerCountOrDepth = 1, NumLevels = info.Mips, SampleCount = SDL.GPUSampleCount.SampleCount1, Usage = entry.ScratchColorTarget ? SDL.GPUTextureUsageFlags.ColorTarget : SDL.GPUTextureUsageFlags.Sampler }), h => SDL.ReleaseGPUTexture(Device, h), _context.Handle);
        }
        if (clear && !entry.ScratchColorTarget) throw new NotSupportedException("This format requires a color-clear adapter.");
        return entry.TextureScratch;
    }

    private static void ValidateSignedClear(DataFormat format, Color color)
    {
        var (bits, channels) = format switch
        {
            DataFormat.R8SINT => (8, 1),
            DataFormat.R8G8SINT => (8, 2),
            DataFormat.R8G8B8A8SINT => (8, 4),
            DataFormat.R16SINT => (16, 1),
            DataFormat.R16G16SINT => (16, 2),
            DataFormat.R16G16B16A16SINT => (16, 4),
            _ => (32, 0)
        };
        if (channels == 0) return;
        var values = stackalloc int[4] { BitConverter.SingleToInt32Bits(color.R), BitConverter.SingleToInt32Bits(color.G), BitConverter.SingleToInt32Bits(color.B), BitConverter.SingleToInt32Bits(color.A) };
        var limit = 1 << (bits - 1);
        for (var channel = 0; channel < channels; channel++) if (values[channel] < -limit || values[channel] >= limit) throw new ArgumentOutOfRangeException(nameof(color), "The signed integer clear lane exceeds its encoded component width.");
    }

    /// <summary>Records a color clear of complete selected image layers and mip levels.</summary>
    /// <param name="texture">Live destination with CanCopyTo usage.</param>
    /// <param name="color">Clear components; integer formats use their raw 32-bit lane bits on Vulkan.</param>
    /// <param name="baseMipmap">First zero-based mip level.</param>
    /// <param name="mipmapCount">Positive number of mip levels to clear.</param>
    /// <param name="baseLayer">First zero-based image layer.</param>
    /// <param name="layerCount">Positive number of layers to clear.</param>
    /// <remarks>Unselected subresources retain their contents. A retained private color target supports
    /// transfer-only destinations without changing their public usage. It is reused after preparation.
    /// Encoded block formats and unverified integer-clear backends fail explicitly. Narrow signed
    /// lanes must be representable; unsigned lanes narrow by discarding higher bits. The clear stays on
    /// the GPU timeline and neither submits nor delivers callbacks.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The selected range is empty/out of bounds, or a signed clear lane exceeds its component width.</exception>
    /// <exception cref="InvalidOperationException">The copy destination role or owner/list state is invalid, or native recording fails.</exception>
    /// <exception cref="NotSupportedException">The format/backend has no executable color-clear path.</exception>
    public void TextureClear(RID texture, Color color, uint baseMipmap, uint mipmapCount, uint baseLayer, uint layerCount)
    {
        IdleList(); var entry = Get(texture, Kind.Texture); TextureRole(entry, TextureUsageBits.CanCopyTo); var info = entry.Texture!;
        if (mipmapCount == 0 || baseMipmap >= info.Mips || mipmapCount > info.Mips - baseMipmap) throw new ArgumentOutOfRangeException(nameof(mipmapCount));
        if (layerCount == 0 || baseLayer >= info.Layers || layerCount > info.Layers - baseLayer) throw new ArgumentOutOfRangeException(nameof(layerCount));
        // shortcut: integer clear-lane conversion must be verified before enabling another native driver.
        if (PixelType(info.Format) != 0 && TextureDriver != "vulkan") throw new NotSupportedException("Integer clear lanes require verification of this backend's native conversion.");
        ValidateSignedClear(info.Format, color);
        if (info.Samples != TextureSamples.Samples1)
        {
            if (DepthFormat(info.Format)) throw new NotSupportedException("Clear depth/stencil through a draw pass.");
            if (PixelType(info.Format) == 0) FiniteColor(color);
            for (var layer = baseLayer; layer < baseLayer + layerCount; layer++) ClearMultisample(entry, color, layer); return;
        }
        var scratch = PrepareTextureScratch(entry, true); var command = Commands();
        for (var mip = baseMipmap; mip < baseMipmap + mipmapCount; mip++)
        {
            var target = new SDL.GPUColorTargetInfo { Texture = scratch.DangerousGetHandle(), MipLevel = mip, ClearColor = new() { R = color.R, G = color.G, B = color.B, A = color.A }, LoadOp = SDL.GPULoadOp.Clear, StoreOp = SDL.GPUStoreOp.Store };
            var render = SDL.BeginGPURenderPass(command, new ReadOnlySpan<SDL.GPUColorTargetInfo>(&target, 1), 1, 0); if (render == 0) throw GPUPhysicsDevice.Failure("begin image clear");
            SDL.EndGPURenderPass(render);
            var copy = SDL.BeginGPUCopyPass(command); if (copy == 0) throw GPUPhysicsDevice.Failure("begin cleared image copy");
            try
            {
                var source = new SDL.GPUTextureLocation { Texture = scratch.DangerousGetHandle(), MipLevel = mip };
                for (var layer = baseLayer; layer < baseLayer + layerCount; layer++) SDL.CopyGPUTextureToTexture(copy, source, new() { Texture = entry.NativeHandle, MipLevel = mip, Layer = layer }, Math.Max(1, info.Width >> (int)mip), Math.Max(1, info.Height >> (int)mip), 1, false);
            }
            finally { SDL.EndGPUCopyPass(copy); }
        }
    }
}
