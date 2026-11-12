using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private static readonly HashSet<ulong> Direct3DImports = [];

    /// <summary>Wraps a sampled native image allocated by an external producer on this device.</summary>
    /// <param name="type">An ordinary image or independent image array.</param>
    /// <param name="format">The native allocation's exact data format.</param>
    /// <param name="samples">The native sample count. Imports require one sample.</param>
    /// <param name="usageFlags">Sampling, declared transfer roles, color attachments, and supported storage images.</param>
    /// <param name="image">The live VkImage or ID3D12Resource pointer bits from the device returned by <see cref="GetDriverResource"/>.</param>
    /// <param name="width">The base mip width in texels.</param>
    /// <param name="height">The base mip height in texels.</param>
    /// <param name="depth">The native depth. Ordinary images and arrays require one.</param>
    /// <param name="layers">The native number of image layers.</param>
    /// <param name="MIPMaps">The native number of mip levels including the base level.</param>
    /// <returns>A device-owned RID for the wrapped allocation.</returns>
    /// <remarks>
    /// Vulkan imports require a same-device, optimal-tiling color image with sampled usage and an exact description.
    /// Create the image with the Vulkan usage flags corresponding to every declared role. Storage
    /// requires VK_IMAGE_USAGE_STORAGE_BIT and color attachments require VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT.
    /// The producer completes its work, transfers exclusive access to this device's queue family,
    /// and leaves every subresource in SHADER_READ_ONLY_OPTIMAL before import. The producer retains
    /// ownership of the image and its memory. Keep both alive and refrain from external commands
    /// until the RID is freed. FreeRID completes pending work, releases the
    /// wrapper and its views, and leaves the image in its sampled layout. External use on another
    /// queue family requires a new ownership transfer. Destroy producer-owned objects before the
    /// logical device closes.
    /// GPU Direct3D 12 imports require a same-device typed color resource on a non-shared default heap
    /// visible to the renderer queue. Color attachments require ALLOW_RENDER_TARGET. Ordinary color
    /// mip chains require the native shared-view adapter and a single-mip view for the framebuffer.
    /// Multi-layer color attachments require one mip. Ordinary compute
    /// storage images require ALLOW_UNORDERED_ACCESS. Array storage bindings and resources with
    /// simultaneous access are unsupported on Direct3D 12.
    /// The producer completes all work and leaves every subresource in ALL_SHADER_RESOURCE. The wrapper
    /// retains one COM reference and restores that state after rendering, compute and transfers. Keep external heaps alive and
    /// grant exclusive resource access until retirement. FreeRID drains the GPU before releasing the wrapper.
    /// Neither backend initializes the producer's pixels.
    /// </remarks>
    /// <exception cref="ArgumentException">The declared description is invalid or the image already belongs to this device's resources.</exception>
    /// <exception cref="NotSupportedException">The driver, installed native module, format or requested roles do not support import.</exception>
    /// <exception cref="InvalidOperationException">The caller is off-owner, a list is active, or a native wrapper could not be created.</exception>
    /// <exception cref="ObjectDisposedException">This device is disposed.</exception>
    public RID TextureCreateFromExtension(TextureType type, DataFormat format, TextureSamples samples,
        TextureUsageBits usageFlags, ulong image, uint width, uint height, uint depth, uint layers, uint MIPMaps = 1) =>
        ImportTexture(type, format, samples, usageFlags, image, width, height, depth, layers, MIPMaps, serverOwned: false);

    internal RID ImportTexture(TextureType type, DataFormat format, TextureSamples samples, TextureUsageBits usageFlags,
        ulong image, uint width, uint height, uint depth, uint layers, uint MIPMaps, bool serverOwned)
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
        const TextureUsageBits writable = TextureUsageBits.ColorAttachment | TextureUsageBits.Storage;
        if (samples != TextureSamples.Samples1 || DepthFormat(format) ||
            (usageFlags & TextureUsageBits.Sampling) == 0 || (usageFlags & ~(allowed | writable)) != 0)
            throw new NotSupportedException("Import requires a single-sample sampled color image with supported roles.");
        var driver = TextureDriver;
        if (driver is not ("vulkan" or "direct3d12"))
            throw new NotSupportedException("The GPU backend does not provide a native texture importer.");
        var writableImport = (usageFlags & writable) != 0;
        if (driver == "direct3d12")
        {
            if (type == TextureType.TextureArray && (usageFlags & TextureUsageBits.Storage) != 0)
                throw new NotSupportedException("Direct3D 12 storage arrays require a native array storage binding adapter.");
            if (MIPMaps != 1 && (usageFlags & TextureUsageBits.ColorAttachment) != 0 &&
                (layers != 1 || GPUTextureViews.Support(Device, GPUTextureViews.Direct3DViewABI) != 1))
                throw new NotSupportedException("Direct3D 12 mip-chain color attachments require a native ordinary shared texture view.");
        }
        if (driver == "direct3d12" && image > nuint.MaxValue) throw new ArgumentOutOfRangeException(nameof(image));
        if (ValidationLimits is { } limits && (width > limits[12] || height > limits[12] || layers > limits[10]))
            throw new ArgumentOutOfRangeException(nameof(width), "Texture dimensions or layers exceed device limits.");
        if (!TextureIsFormatSupportedForUsage(format, usageFlags))
            throw new NotSupportedException("The device does not support the imported format and usage.");

        var info = CaptureTextureInfo(format, type, width, height, layers, MIPMaps, usageFlags, [],
            samples, resolve: false, swizzle: 0, stored: format);
        var create = NativeTextureInfo(info);
        var device = Device;
        RenderHandle handle;
        try
        {
            if (driver == "vulkan")
            {
                RequireTextureViews();
                if (writableImport && GPUTextureViews.Support(device, 5) != 1)
                    throw new NotSupportedException("The native module does not support writable Vulkan texture import.");
            }
            foreach (var entry in _entries.Values)
                if (entry.Kind == Kind.Texture && entry.Alive &&
                    QueryDriverResource(device, 1, (uint)DriverResource.Texture, entry.NativeHandle, out var existing) == 1 &&
                    existing == image)
                    throw new ArgumentException("The image already belongs to a live resource of this device.", nameof(image));
            handle = driver == "direct3d12"
                ? ImportDirect3DTexture(device, in create, image, writableImport ? 2u : 1u)
                : new RenderHandle(GPUTextureViews.Import(device, 1, in create, image),
                    value => GPUTextureViews.Destroy(device, 1, value), _context.Handle);
        }
        catch (Exception error) when (error is EntryPointNotFoundException or DllNotFoundException)
        {
            throw new NotSupportedException("The native module does not provide this texture importer.", error);
        }
        try { return Add(new Entry { Kind = Kind.Texture, Texture = info, Size = info.Total, Handle = handle, ServerOwnsTexture = serverOwned }); }
        catch { handle.Dispose(); throw; }
    }

    private RenderHandle ImportDirect3DTexture(nint device, in SDL.GPUTextureCreateInfo create, ulong image, uint abi)
    {
        lock (Direct3DImports)
            if (!Direct3DImports.Add(image))
                throw new ArgumentException("The resource already has a live Direct3D 12 texture wrapper.", nameof(image));
        nint native;
        try
        {
            var status = ImportDirect3DTextureNative(device, abi, in create, image, out native);
            if (status == 0) throw new NotSupportedException("The native module cannot import this Direct3D 12 resource.");
            if (status == -1)
                throw new ArgumentException("The Direct3D 12 resource device, description, flags or heap does not match the import contract.", nameof(image));
            if (status != 1) throw GPUPhysicsDevice.Failure("import a Direct3D 12 texture");
        }
        catch
        {
            lock (Direct3DImports) Direct3DImports.Remove(image);
            throw;
        }

        return new RenderHandle(native, value =>
        {
            try
            {
                SDL.ReleaseGPUTexture(device, value);
                SDL.WaitForGPUIdle(device);
            }
            finally { lock (Direct3DImports) Direct3DImports.Remove(image); }
        }, _context.Handle);
    }

    [LibraryImport(NativeLibraries.EngineLibrary, EntryPoint = "e2d_render_d3d12_texture_import")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int ImportDirect3DTextureNative(nint device, uint abi,
        in SDL.GPUTextureCreateInfo create, ulong image, out nint texture);

    internal void RetireCanvasImport(RID rid)
    {
        IdleList();
        RetireRID(rid, _entries[rid]);
    }

    internal void DeliverCanvasImportReadbacks() => DeliverReadbacks();
}
