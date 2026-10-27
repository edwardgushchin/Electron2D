namespace Electron2D;

public sealed partial class RenderingDevice
{
    private RenderFormatSupport? _formatSupport;
    private RenderFormatSupport FormatSupport => _formatSupport ??= GPUDeviceFactory.ReadFormatSupport(_context.Handle);

    /// <summary>Tests native ordinary sampled-image support for the requested nearest or linear filter.</summary>
    /// <param name="format">A defined data format, excluding the Max sentinel.</param>
    /// <param name="samplerFilter">Nearest or Linear; linear also covers ordinary mip interpolation.</param>
    /// <returns>True when the native format supports ordinary sampling with the filter; false for unmapped or unsupported formats.</returns>
    /// <remarks>Reads immutable optimal-tiling format features and the native device's enabled sampling roles.
    /// No image is allocated and no commands are submitted. A shared physical device caches one snapshot until retirement.
    /// The query does not promise every format/usage combination, sufficient memory or comparison-filter behavior.
    /// Integer filtering and depth-comparison roles retain their separate binding contracts.
    /// Owner-thread, lifetime and callback guards match other device queries. The current adapter is pinned SDL 3.4.18 Vulkan.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The format or filter is undefined, or format is Max.</exception>
    /// <exception cref="NotSupportedException">The native core or driver has no compatible query adapter.</exception>
    public bool SamplerIsFormatSupportedForFilter(DataFormat format, SamplerFilter samplerFilter)
    {
        Access();
        if (!Enum.IsDefined(format) || format == DataFormat.Max) throw new ArgumentOutOfRangeException(nameof(format));
        RDTextureConfiguration.Filter(samplerFilter);
        return FormatSupport.Supports(NativeFormat(format), samplerFilter);
    }

    private void ValidateFormatFilter(nint texture, bool linear, bool comparison = false)
    {
        if (_context.Driver == "vulkan") FormatSupport.Require(texture, linear, comparison);
    }
}
