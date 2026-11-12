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
    internal RID TextureRDCreateCore(RID rid, TextureLayered.LayeredType layerType, bool ownDeviceImage = false)
    {
        EnsureTextureChange();
        if (layerType != TextureLayered.LayeredType.Array) throw new ArgumentOutOfRangeException(nameof(layerType));
        var device = GetRenderingDeviceCore() ?? throw new InvalidOperationException("A GPU renderer is required.");
        var info = device.CanvasInfo(rid) ?? throw new ArgumentException("The device image identity is not live.", nameof(rid));
        var ownedDeviceImage = ownDeviceImage ? rid : default;
        if (info.Type == RenderingDevice.TextureType.TextureArray)
            return OwnLayered(new ServerTextureArray(new TextureArrayRD { TextureRDRID = rid }, ownedDeviceImage));

        var texture = new ServerTexture(new TextureRD { TextureRDRID = rid }, ownedDeviceImage);
        return OwnTexture(texture);
    }
    /// <summary>Gets the borrowed device image identity represented by a canvas texture.</summary>
    /// <param name="texture">A live ordinary or layered canvas texture RID.</param>
    /// <param name="SRGB">Requests sRGB sampling over the selected image and subresources.</param>
    /// <returns>A retained global-device RID, or an empty identity for unavailable storage, an unrendered viewport,
    /// or the compatibility renderer.</returns>
    /// <remarks>Ordinary uploads, independent arrays and completed viewport images borrow renderer-owned native
    /// storage without copying. Compatible authored updates keep identity; allocation replacement, source disposal
    /// or viewport rebinding expires it. A viewport RID follows its completed ping-pong image at command recording.
    /// An already-sRGB image returns its source RID. Compatible device images, renderer uploads and
    /// completed SDR viewport images return a cached shared view that expires with the source RID.
    /// Supported Vulkan and Direct3D 12 adapters share the native allocation across linear and sRGB views.
    /// Renderer sRGB views support sampling and raw transfers. Storage-image access remains on the
    /// linear RID. Both viewport RIDs select the completed image at command recording.
    /// Creating a view requires an idle device.
    /// FreeRID on the rendering device releases a borrowed wrapper or a distinct view independently.
    /// For a direct native import, the root device identity is owned by the canvas texture and cannot be freed on the device.
    /// Device writes affect native storage; authored CPU
    /// snapshots remain unchanged and a later authored update replaces those writes. Prepare changed uploads before
    /// beginning a draw list. Access belongs to the scene owner thread.</remarks>
    /// <exception cref="ArgumentException">The canvas texture RID is not live.</exception>
    /// <exception cref="NotSupportedException">The source or native backend cannot supply the requested sRGB view.</exception>
    /// <exception cref="InvalidOperationException">Device list or renderer submission state rejects export.</exception>
    public static RID TextureGetRDTexture(RID texture, bool SRGB = false) => RequireService().TextureGetRDTextureCore(texture, SRGB);
    internal RID TextureGetRDTextureCore(RID texture, bool SRGB)
    {
        EnsureOwner();
        var resource = ResolveTextureExportSource(texture);
        return resource is null ? default : ExportRDTexture(resource, SRGB);
    }

    /// <summary>Gets the borrowed graphics handle of a live canvas texture.</summary>
    /// <param name="texture">A live ordinary or layered canvas texture RID.</param>
    /// <param name="SRGB">Requests the sRGB interpretation when one is available. The native allocation can serve both interpretations.</param>
    /// <returns>The native handle bits, or zero if the texture has no storage or the viewport has no completed image.</returns>
    /// <remarks>Call on the scene owner outside canvas submission. Export may prepare an upload or shared storage.
    /// The handle borrows the current allocation and does not pin it, synchronize GPU work or transfer ownership.
    /// Viewport handles follow the last completed image. Replacement, resizing, cycling, retirement or renderer
    /// shutdown can invalidate previously returned values. External consumers must coordinate native lifetime and synchronization.</remarks>
    /// <exception cref="ArgumentException">The texture RID is not live.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable, closing, off-owner or submitting a canvas.</exception>
    /// <exception cref="NotSupportedException">The selected backend or texture role has no native handle export.</exception>
    public static ulong TextureGetNativeHandle(RID texture, bool SRGB = false) =>
        RequireService().TextureGetNativeHandleCore(texture, SRGB);

    private ulong TextureGetNativeHandleCore(RID texture, bool SRGB)
    {
        EnsureOwner();
        if (_closing || _submittingTextures) throw new InvalidOperationException("Native texture export requires an active renderer outside canvas submission.");
        var resource = ResolveTextureExportSource(texture);
        if (resource is null) return 0;

        if (_backend is CompatibilityCanvasBackend compatibility) return compatibility.ExportNativeTexture(resource);
        if (_backend is not GPUCanvasBackend { Driver: "vulkan" or "direct3d12" })
            throw new NotSupportedException("This GPU backend has no native texture handle export.");

        var deviceTexture = ExportRDTexture(resource, SRGB: false);
        if (!deviceTexture.IsValid()) return 0;
        var device = GetRenderingDeviceCore()!;
        if (SRGB) deviceTexture = device.CanvasSRGBTexture(deviceTexture, allowBase: true);
        return deviceTexture.IsValid() ? device.GetDriverResource(RenderingDevice.DriverResource.Texture, deviceTexture, 0) : 0;
    }

    private static Resource? ResolveTextureExportSource(RID texture) =>
        RenderingTextureRegistry.ResolveSampledStorage(RenderingTextureRegistry.ResolveResource(texture));

    private RID ExportRDTexture(Resource resource, bool SRGB)
    {
        var result = resource switch { TextureRD ordinary => ordinary.TextureRDRID, TextureLayeredRD layered => layered.TextureRDRID, _ => default };
        if (!result.IsValid() && _backend is GPUCanvasBackend gpu) { EnsureDeviceAccess(); var device = GetRenderingDeviceCore()!; device.ValidateCanvasMutation(); result = gpu.ExportTexture(resource, device, SRGB); }
        if (!result.IsValid()) return default;
        var currentDevice = GetRenderingDeviceCore();
        if (currentDevice is null) return default;
        if (SRGB) return currentDevice.CanvasSRGBTexture(result);
        return currentDevice.CanvasInfo(result) is not null ? result : default;
    }
}
