using System.Numerics;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    /// <summary>Wraps a sampled native image allocated by an external producer on this device.</summary>
    /// <param name="type">An ordinary image or independent image array.</param>
    /// <param name="format">The native allocation's exact data format.</param>
    /// <param name="samples">The native sample count. Vulkan imports require one sample.</param>
    /// <param name="usageFlags">Sampling and any declared transfer roles supported by the native image.</param>
    /// <param name="image">The live VkImage handle bits from the device returned by <see cref="GetDriverResource"/>.</param>
    /// <param name="width">The base mip width in texels.</param>
    /// <param name="height">The base mip height in texels.</param>
    /// <param name="depth">The native depth. Ordinary images and arrays require one.</param>
    /// <param name="layers">The native number of image layers.</param>
    /// <param name="MIPMaps">The native number of mip levels including the base level.</param>
    /// <returns>A device-owned RID for the borrowed allocation and its owned views.</returns>
    /// <remarks>
    /// Vulkan imports require a same-device, optimal-tiling color image with sampled usage and an exact description.
    /// The producer completes its work, transfers exclusive access to this device's queue family,
    /// and leaves every subresource in SHADER_READ_ONLY_OPTIMAL before import. The producer retains
    /// ownership of the image and its memory. Keep both alive and refrain from external commands
    /// until the RID is freed. FreeRID completes pending work, releases the
    /// wrapper and its views, and leaves the image in its sampled layout. External use on another
    /// queue family requires a new ownership transfer. Destroy producer-owned objects before the
    /// logical device closes. This operation does not initialize pixels.
    /// </remarks>
    /// <exception cref="ArgumentException">The declared description is invalid or the image already belongs to this device's resources.</exception>
    /// <exception cref="NotSupportedException">The driver, installed native module, format or requested roles do not support import.</exception>
    /// <exception cref="InvalidOperationException">The caller is off-owner, a list is active, or a native wrapper could not be created.</exception>
    /// <exception cref="ObjectDisposedException">This device is disposed.</exception>
    public RID TextureCreateFromExtension(TextureType type, DataFormat format, TextureSamples samples,
        TextureUsageBits usageFlags, ulong image, uint width, uint height, uint depth, uint layers, uint MIPMaps = 1)
    {
        IdleList();
        if (image == 0) throw new ArgumentOutOfRangeException(nameof(image));
        if (type is not (TextureType.Texture or TextureType.TextureArray)) throw new ArgumentOutOfRangeException(nameof(type));
        if ((uint)format >= (uint)DataFormat.Max) throw new ArgumentOutOfRangeException(nameof(format));
        if ((uint)samples > (uint)TextureSamples.Samples64) throw new ArgumentOutOfRangeException(nameof(samples));
        ArgumentOutOfRangeException.ThrowIfZero(width);
        ArgumentOutOfRangeException.ThrowIfZero(height);
        ArgumentOutOfRangeException.ThrowIfZero(layers);
        if (depth != 1) throw new ArgumentOutOfRangeException(nameof(depth));
        if (type == TextureType.Texture && layers != 1) throw new ArgumentException("An ordinary image has one layer.", nameof(layers));
        if (MIPMaps == 0 || MIPMaps > 1 + BitOperations.Log2(Math.Max(width, height)))
            throw new ArgumentOutOfRangeException(nameof(MIPMaps));

        const TextureUsageBits allowed = TextureUsageBits.Sampling | TextureUsageBits.CanCopyFrom |
            TextureUsageBits.CanCopyTo | TextureUsageBits.CanUpdate | TextureUsageBits.CPURead;
        if (samples != TextureSamples.Samples1 || DepthFormat(format) ||
            (usageFlags & TextureUsageBits.Sampling) == 0 || (usageFlags & ~allowed) != 0)
            throw new NotSupportedException("Import requires a single-sample color image with sampled and optional transfer roles.");
        RequireTextureViews();
        if (ValidationLimits is { } limits && (width > limits[12] || height > limits[12] || layers > limits[10]))
            throw new ArgumentOutOfRangeException(nameof(width), "Texture dimensions or layers exceed device limits.");
        if (!TextureIsFormatSupportedForUsage(format, usageFlags))
            throw new NotSupportedException("The device does not support the imported format and usage.");

        var info = CaptureTextureInfo(format, type, width, height, layers, MIPMaps, usageFlags, [],
            samples, resolve: false, swizzle: 0, stored: format);
        var create = NativeTextureInfo(info);
        var device = Device;
        nint native;
        try
        {
            foreach (var entry in _entries.Values)
                if (entry.Kind == Kind.Texture && entry.Alive &&
                    QueryDriverResource(device, 1, (uint)DriverResource.Texture, entry.NativeHandle, out var existing) == 1 &&
                    existing == image)
                    throw new ArgumentException("The image already belongs to a live resource of this device.", nameof(image));
            native = GPUTextureViews.Import(device, 1, in create, image);
        }
        catch (Exception error) when (error is EntryPointNotFoundException or DllNotFoundException)
        {
            throw new NotSupportedException("The native module does not provide Vulkan texture import.", error);
        }
        var handle = new RenderHandle(native, value => GPUTextureViews.Destroy(device, 1, value), _context.Handle);
        try { return Add(new Entry { Kind = Kind.Texture, Texture = info, Size = info.Total, Handle = handle }); }
        catch { handle.Dispose(); throw; }
    }
}
