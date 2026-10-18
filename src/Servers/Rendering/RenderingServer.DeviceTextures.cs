namespace Electron2D;

public sealed partial class RenderingServer
{
    internal bool DeviceClosing => _closing;
    internal void EnsureDeviceAccess() { EnsureOwner(); if (_submittingTextures && !_preparingNativeDrawable) throw new InvalidOperationException("Device operations cannot reenter native canvas submission."); }
    internal RenderingDevice? GetRenderingDeviceCore()
    {
        EnsureOwner(); if (_closing) throw new InvalidOperationException("The renderer is closing.");
        return _backend is GPUCanvasBackend ? _renderingDevice ??= new RenderingDevice(this) : null;
    }
    /// <summary>Gets the retained GPU rendering device of this active rendering session.</summary>
    /// <returns>The renderer-owned device, or null for an unavailable or compatibility renderer.</returns>
    /// <remarks>Record compute work on the scene owner thread. The renderer submits it before canvas drawing;
    /// explicit Submit/Sync and public disposal belong to local devices. The retained device expires at shutdown.</remarks>
    public static RenderingDevice? GetRenderingDevice() => Service?.GetRenderingDeviceCore();
    /// <summary>Creates a server-owned canvas texture borrowing an existing device image.</summary>
    /// <param name="deviceTexture">A live sampled image of the current rendering device.</param>
    /// <param name="layerType">The independent array role, used when the allocation is layered; ordinary images are inferred.</param>
    /// <returns>A canvas RID freed through RenderingServer.FreeRID; it never owns the device image.</returns>
    /// <exception cref="ArgumentException">The image belongs to another device, is freed, or has incompatible sampling roles.</exception>
    /// <exception cref="InvalidOperationException">No active GPU renderer exists or the owner/list state rejects mutation.</exception>
    /// <exception cref="NotSupportedException">The image requires an integer canvas descriptor interface.</exception>
    public static RID TextureRDCreate(RID deviceTexture, TextureLayered.LayeredType layerType = TextureLayered.LayeredType.Array) => RequireService().TextureRDCreateCore(deviceTexture, layerType);
    internal RID TextureRDCreateCore(RID rid, TextureLayered.LayeredType layerType)
    {
        EnsureTextureChange();
        if (layerType != TextureLayered.LayeredType.Array) throw new ArgumentOutOfRangeException(nameof(layerType));
        var device = GetRenderingDeviceCore() ?? throw new InvalidOperationException("A GPU renderer is required.");
        var info = device.CanvasInfo(rid) ?? throw new ArgumentException("The device image identity is not live.", nameof(rid));
        if (info.Type == RenderingDevice.TextureType.TextureArray)
        {
            var array = new TextureArrayRD { TextureRDRID = rid }; return OwnLayered(array);
        }
        var texture = new TextureRD { TextureRDRID = rid, ServerOwner = this };
        var result = RenderingTextureRegistry.Register(texture, this); texture.BindRenderingRID(result); _ownedTextureRIDs.Add(result); return result;
    }
    /// <summary>Gets the borrowed device image identity represented by a canvas texture.</summary>
    /// <param name="texture">A live ordinary or layered canvas texture RID.</param>
    /// <param name="SRGB">Requests an sRGB view; a distinct view requires the native format-view adapter.</param>
    /// <returns>A retained global-device RID, or an empty identity for unavailable storage, an unrendered viewport,
    /// or the compatibility renderer.</returns>
    /// <remarks>Ordinary uploads, independent arrays and completed viewport images borrow renderer-owned native
    /// storage without copying. Compatible authored updates keep identity; allocation replacement, source disposal
    /// or viewport rebinding expires it. A viewport RID follows its completed ping-pong image at command recording.
    /// FreeRID on the rendering device releases only the wrapper. Device writes affect native storage; authored CPU
    /// snapshots remain unchanged and a later authored update replaces those writes. Prepare changed uploads before
    /// beginning a draw list. Access belongs to the scene owner thread.</remarks>
    /// <exception cref="ArgumentException">The canvas texture RID is not live.</exception>
    /// <exception cref="NotSupportedException">A distinct sRGB view requires native format-view ownership.</exception>
    /// <exception cref="InvalidOperationException">Device list or renderer submission state rejects export.</exception>
    public static RID TextureGetRDTexture(RID texture, bool SRGB = false) => RequireService().TextureGetRDTextureCore(texture, SRGB);
    internal RID TextureGetRDTextureCore(RID texture, bool SRGB)
    {
        EnsureOwner(); var resource = RenderingTextureRegistry.ResolveResource(texture);
        if (resource is ServerTexture { IsProxy: true } proxy) resource = RenderingTextureRegistry.ResolveProxySource(proxy.ProxyTarget) ?? resource;
        if (resource is LayeredTextureProxy arrayProxy) resource = RenderingTextureRegistry.ResolveLayeredProxySource(arrayProxy.ProxyTarget) ?? resource;
        var result = resource switch { TextureRD ordinary => ordinary.TextureRDRID, TextureLayeredRD layered => layered.TextureRDRID, _ => default };
        if (!result.IsValid() && _backend is GPUCanvasBackend gpu) { EnsureDeviceAccess(); var device = GetRenderingDeviceCore()!; device.ValidateCanvasMutation(); result = gpu.ExportTexture(resource, device, SRGB); }
        if (!result.IsValid()) return default;
        var info = GetRenderingDeviceCore()?.CanvasInfo(result); if (info is null) return default;
        if (SRGB && !info.Format.ToString().Contains("SRGB", StringComparison.Ordinal)) throw new NotSupportedException("Distinct sRGB device views require native format-view integration.");
        return result;
    }
}
