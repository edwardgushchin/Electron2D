namespace Electron2D;

public sealed partial class RenderingServer
{
    /// <summary>Identifies a sampled native texture shape for canvas import.</summary>
    public enum TextureType
    {
        /// <summary>One two-dimensional color image.</summary>
        Texture = 0,
    }

    /// <summary>Wraps an existing native sampled texture for use by canvas commands.</summary>
    /// <param name="type">The ordinary image shape.</param>
    /// <param name="format">The portable pixel format. Direct3D RGBA8 and BGRA8 UNORM both use Rgba8.</param>
    /// <param name="nativeHandle">A live ID3D11Texture2D or ID3D12Resource interface pointer from the current Compatibility device.</param>
    /// <param name="width">The native image width in pixels.</param>
    /// <param name="height">The native image height in pixels.</param>
    /// <param name="depth">The native depth, which must be one for an ordinary image.</param>
    /// <param name="layers">The native layer count, which must be one for an ordinary image.</param>
    /// <param name="layeredType">The independent image-array role when importing a layered image.</param>
    /// <returns>A rendering-server-owned texture RID. Release it with <see cref="FreeRID"/>.</returns>
    /// <remarks>Call on the active renderer owner thread. The wrapper retains a reference to the same SDR allocation.
    /// Order Direct3D 11 writes on the device's immediate context before canvas submission.
    /// For Direct3D 12, complete all producer work before import, leave the resource in COPY_DEST,
    /// and give the renderer exclusive state and queue access. Retirement releases the wrapper and does not
    /// publish a native state for further external commands. The wrapper is local to the importing renderer.</remarks>
    /// <exception cref="ArgumentException">The shape, size, format declaration, interface or device is invalid.</exception>
    /// <exception cref="NotSupportedException">The backend or native allocation role cannot be imported.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable, closing, off-owner or submitting a canvas.</exception>
    public static RID TextureCreateFromNativeHandle(TextureType type, Image.Format format, ulong nativeHandle,
        int width, int height, int depth, int layers = 1, TextureLayered.LayeredType layeredType = TextureLayered.LayeredType.Array)
    {
        var server = RequireService();
        server.EnsureTextureChange();
        if (type != TextureType.Texture) throw new ArgumentOutOfRangeException(nameof(type));
        if (layeredType != TextureLayered.LayeredType.Array) throw new ArgumentOutOfRangeException(nameof(layeredType));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        if (depth != 1) throw new ArgumentOutOfRangeException(nameof(depth));
        if (layers != 1) throw new ArgumentOutOfRangeException(nameof(layers));
        if (!Enum.IsDefined(format)) throw new ArgumentOutOfRangeException(nameof(format));
        if (nativeHandle == 0 || nativeHandle > nuint.MaxValue) throw new ArgumentOutOfRangeException(nameof(nativeHandle));
        if (format != Image.Format.Rgba8 || server._backend is not CompatibilityCanvasBackend { Driver: "direct3d11" or "direct3d12" } backend)
            throw new NotSupportedException("This texture import requires RGBA8 on Direct3D 11 or Direct3D 12 Compatibility.");

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
            server._ownedTextureRIDs.EnsureCapacity(server._ownedTextureRIDs.Count + 1);
            var rid = RenderingTextureRegistry.Register(texture, server);
            texture.Bind(rid);
            server._ownedTextureRIDs.Add(rid);
            return rid;
        }
        catch
        {
            texture.Released = true;
            texture.Dispose();
            throw;
        }
    }
}
