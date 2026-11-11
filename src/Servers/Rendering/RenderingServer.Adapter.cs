namespace Electron2D;

public sealed partial class RenderingServer
{
    internal static GraphicsAdapterIdentity ActiveAdapter => Service?._backend.AdapterIdentity ?? GraphicsAdapterIdentity.Empty;

    /// <summary>Gets the adapter name reported by the active renderer.</summary>
    /// <returns>The name or an empty string when no adapter is reported.</returns>
    public static string GetVideoAdapterName() => ActiveAdapter.Name;

    /// <summary>Gets the hardware vendor reported by the active renderer.</summary>
    /// <returns>The vendor name or an empty string when unavailable.</returns>
    public static string GetVideoAdapterVendor() => ActiveAdapter.Vendor;

    /// <summary>Gets the graphics API version reported by the active renderer.</summary>
    /// <returns>The version or an empty string when unavailable.</returns>
    public static string GetVideoAdapterAPIVersion() => ActiveAdapter.APIVersion;

    /// <summary>Gets the physical graphics device type reported by the active renderer.</summary>
    /// <returns>The reported classification, or <see cref="RenderingDevice.DeviceType.Other"/> when unavailable.</returns>
    public static RenderingDevice.DeviceType GetVideoAdapterType() => ActiveAdapter.Type;
}
