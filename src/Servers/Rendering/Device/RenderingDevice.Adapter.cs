namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    /// <summary>Classifies the selected physical graphics device.</summary>
    public enum DeviceType
    {
        /// <summary>Another or unclassified device.</summary>
        Other = 0,
        /// <summary>An integrated graphics processor.</summary>
        IntegratedGPU = 1,
        /// <summary>A discrete graphics processor.</summary>
        DiscreteGPU = 2,
        /// <summary>A virtual graphics processor.</summary>
        VirtualGPU = 3,
        /// <summary>A CPU graphics device.</summary>
        CPU = 4,
        /// <summary>The upper bound of the device type values.</summary>
        Max = 5,
    }

    private GraphicsAdapterIdentity? _adapterIdentity;

    /// <summary>Gets the hardware vendor of this device when the backend reports it.</summary>
    /// <returns>The vendor name or an empty string when unavailable.</returns>
    public string GetDeviceVendorName()
    {
        Access();
        return (_adapterIdentity ??= GraphicsAdapterIdentity.ReadGPU(Device)).Vendor;
    }

    /// <summary>Gets the pipeline cache UUID of this device.</summary>
    /// <returns>The native pipeline cache identity or an empty string when unavailable. Vulkan reports a lowercase hexadecimal UUID.
    /// Direct3D 12 reports an adapter and driver version key.</returns>
    public string GetDevicePipelineCacheUUID()
    {
        Access();
        return (_adapterIdentity ??= GraphicsAdapterIdentity.ReadGPU(Device)).PipelineCacheUUID;
    }
}
