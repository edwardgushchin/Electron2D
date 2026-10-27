namespace Electron2D;

internal sealed unsafe partial class GPUCanvasBackend
{
    private RenderFormatSupport? _formatSupport;

    private void ValidateConfiguredSampling(nint texture, SamplerConfiguration configuration)
    {
        if (configuration.UnnormalizedUVW) SamplerConfiguration.RequireUnnormalizedView(Device, texture);
        if (Driver == "vulkan") (_formatSupport ??= GPUDeviceFactory.ReadFormatSupport(_device)).Require(texture, configuration.LinearFilter, configuration.EnableCompare);
    }
}
