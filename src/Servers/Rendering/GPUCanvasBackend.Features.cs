using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUCanvasBackend
{
    private readonly byte _osFeatureBits;
    private byte CaptureOSFeatures()
    {
        bool Supports(SDL.GPUTextureFormat format) => SDL.GPUTextureSupportsFormat(Device, format, SDL.GPUTextureType.TextureType2D, SDL.GPUTextureUsageFlags.Sampler);
        byte result = 0;
        if (Supports(SDL.GPUTextureFormat.BC1RGBAUnorm) && Supports(SDL.GPUTextureFormat.BC2RGBAUnorm) && Supports(SDL.GPUTextureFormat.BC3RGBAUnorm)) result |= 1;
        if (Supports(SDL.GPUTextureFormat.BC4RUnorm) && Supports(SDL.GPUTextureFormat.BC5RGUnorm)) result |= 2;
        if (Supports(SDL.GPUTextureFormat.BC6HRGBFloat) && Supports(SDL.GPUTextureFormat.BC6HRGBUFloat) && Supports(SDL.GPUTextureFormat.BC7RGBAUnorm)) result |= 4;
        return result;
    }
    internal override bool HasOSFeature(string feature)
    {
        var bit = feature.Equals("s3tc", StringComparison.OrdinalIgnoreCase) ? 1 : feature.Equals("rgtc", StringComparison.OrdinalIgnoreCase) ? 2 : feature.Equals("bptc", StringComparison.OrdinalIgnoreCase) ? 4 : 0;
        return (_osFeatureBits & bit) != 0;
    }
}
