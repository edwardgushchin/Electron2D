namespace Electron2D;

public sealed partial class RenderingServer
{
    /// <summary>Identifies a sampled native texture shape for canvas import.</summary>
    public enum TextureType
    {
        /// <summary>One two-dimensional color image.</summary>
        Texture = 0,
        /// <summary>An array of independent two-dimensional color images.</summary>
        Layered = 1,
    }

    /// <summary>Wraps an existing native sampled texture for use by canvas commands.</summary>
    /// <param name="type">An ordinary image or an independent image array.</param>
    /// <param name="format">The pixel encoding. Vulkan uses its exact linear native equivalent. Direct3D RGBA8 and BGRA8 UNORM both use Rgba8.</param>
    /// <param name="nativeHandle">A live VkImage, ID3D11Texture2D or ID3D12Resource from the active renderer device.</param>
    /// <param name="width">The native image width in pixels.</param>
    /// <param name="height">The native image height in pixels.</param>
    /// <param name="depth">The native depth, which must be one.</param>
    /// <param name="layers">The native layer count, which must be one for an ordinary image.</param>
    /// <param name="layeredType">The independent image-array role when importing a layered image.</param>
    /// <returns>A rendering-server-owned texture RID. Release it with <see cref="FreeRID"/>.</returns>
    /// <remarks>Call on the active renderer owner thread. The wrapper retains a reference to the same SDR allocation.
    /// Order Direct3D 11 writes on the device's immediate context before canvas submission.
    /// For Direct3D 12, complete all producer work before import, leave the resource in COPY_DEST,
    /// and give the renderer exclusive state and queue access. Retirement releases the wrapper and does not
    /// publish a native state for further external commands. The wrapper is local to the importing renderer.
    /// GPU Vulkan requires a one-mip, single-sample, optimal-tiling image with sampled and both transfer
    /// usages. Complete producer work on the renderer queue family and leave all layers in SHADER_READ_ONLY_OPTIMAL.
    /// The producer retains the image and memory and grants exclusive use until retirement. Release the canvas RID
    /// before destroying producer storage. Retirement drains GPU work and leaves the image in its sampled layout.
    /// The canvas RID owns its device texture root. Destroy the producer allocation before the logical device closes.</remarks>
    /// <exception cref="ArgumentException">The shape, size, format declaration, interface or device is invalid.</exception>
    /// <exception cref="NotSupportedException">The backend or native allocation role cannot be imported.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable, closing, off-owner or submitting a canvas.</exception>
    public static RID TextureCreateFromNativeHandle(TextureType type, Image.Format format, ulong nativeHandle,
        int width, int height, int depth, int layers = 1, TextureLayered.LayeredType layeredType = TextureLayered.LayeredType.Array)
    {
        var server = RequireService();
        server.EnsureTextureChange();
        if (type is not (TextureType.Texture or TextureType.Layered)) throw new ArgumentOutOfRangeException(nameof(type));
        if (layeredType != TextureLayered.LayeredType.Array) throw new ArgumentOutOfRangeException(nameof(layeredType));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        if (depth != 1) throw new ArgumentOutOfRangeException(nameof(depth));
        if (layers < 1 || type == TextureType.Texture && layers != 1) throw new ArgumentOutOfRangeException(nameof(layers));
        if (!Enum.IsDefined(format)) throw new ArgumentOutOfRangeException(nameof(format));
        if (nativeHandle == 0) throw new ArgumentOutOfRangeException(nameof(nativeHandle));
        if (server._backend is GPUCanvasBackend { Driver: "vulkan" })
            return server.ImportVulkanCanvasTexture(type, format, nativeHandle, width, height, layers);

        if (format != Image.Format.Rgba8 || type != TextureType.Texture ||
            server._backend is not CompatibilityCanvasBackend { Driver: "direct3d11" or "direct3d12" } backend)
            throw new NotSupportedException("This texture import requires a supported GPU Vulkan or Direct3D Compatibility image.");

        if (nativeHandle > nuint.MaxValue) throw new ArgumentOutOfRangeException(nameof(nativeHandle));
        var native = unchecked((nint)nativeHandle);
        if (backend.Driver == "direct3d12")
        {
            foreach (var owned in server._ownedTextureRIDs)
                if (RenderingTextureRegistry.OwnedResource(owned, server) is ServerTexture { NativeTexture: { } source } &&
                    source.Native == native)
                    throw new ArgumentException("The resource already has a live renderer wrapper.", nameof(nativeHandle));
        }
        var texture = new ServerTexture(backend.ImportNativeTexture(native, new(width, height)));
        try
        {
            return server.OwnTexture(texture);
        }
        catch
        {
            texture.Released = true;
            texture.Dispose();
            throw;
        }
    }

    private RID ImportVulkanCanvasTexture(TextureType type, Image.Format format, ulong image, int width, int height, int layers)
    {
        var nativeFormat = RenderingDevice.CanvasImportFormat(format);
        if (nativeFormat == RenderingDevice.DataFormat.Max)
            throw new NotSupportedException("The image format has no exact native canvas import encoding.");

        var device = GetRenderingDeviceCore()!;
        var nativeType = type == TextureType.Texture ? RenderingDevice.TextureType.Texture : RenderingDevice.TextureType.TextureArray;
        const RenderingDevice.TextureUsageBits usage = RenderingDevice.TextureUsageBits.Sampling |
            RenderingDevice.TextureUsageBits.CanCopyFrom | RenderingDevice.TextureUsageBits.CanCopyTo |
            RenderingDevice.TextureUsageBits.CanUpdate | RenderingDevice.TextureUsageBits.CPURead;
        var rid = device.ImportTexture(nativeType, nativeFormat, RenderingDevice.TextureSamples.Samples1, usage,
            image, (uint)width, (uint)height, 1, (uint)layers, 1, serverOwned: true);
        try { return TextureRDCreateCore(rid, TextureLayered.LayeredType.Array, ownDeviceImage: true); }
        catch { device.RetireCanvasImport(rid); throw; }
    }
}
