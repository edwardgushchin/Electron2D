namespace Electron2D;

internal static class RDTextureConfiguration
{
    internal static uint Positive(uint value) => value > 0 && value <= int.MaxValue ? value : throw new ArgumentOutOfRangeException(nameof(value));
    internal static float Finite(float value) => float.IsFinite(value) ? value : throw new ArgumentOutOfRangeException(nameof(value));
    internal static float Anisotropy(float value) => float.IsFinite(value) && value >= 1 && value <= 16 ? value : throw new ArgumentOutOfRangeException(nameof(value));
    private static T EnumValue<T>(T value, T max) where T : struct, Enum => Enum.IsDefined(value) && !value.Equals(max) ? value : throw new ArgumentOutOfRangeException(nameof(value));
    internal static RenderingDevice.DataFormat FormatValue(RenderingDevice.DataFormat value) => EnumValue(value, RenderingDevice.DataFormat.Max);
    internal static RenderingDevice.DataFormat ViewFormat(RenderingDevice.DataFormat value) => Enum.IsDefined(value) ? value : throw new ArgumentOutOfRangeException(nameof(value));
    internal static RenderingDevice.TextureType TextureKind(RenderingDevice.TextureType value) => EnumValue(value, RenderingDevice.TextureType.Max);
    internal static RenderingDevice.TextureSwizzle Swizzle(RenderingDevice.TextureSwizzle value) => EnumValue(value, RenderingDevice.TextureSwizzle.Max);
    internal static RenderingDevice.SamplerFilter Filter(RenderingDevice.SamplerFilter value) => Enum.IsDefined(value) ? value : throw new ArgumentOutOfRangeException(nameof(value));
    internal static RenderingDevice.SamplerRepeatMode Repeat(RenderingDevice.SamplerRepeatMode value) => EnumValue(value, RenderingDevice.SamplerRepeatMode.Max);
    internal static RenderingDevice.TextureUsageBits Usage(RenderingDevice.TextureUsageBits value) => (value & ~(RenderingDevice.TextureUsageBits)5119) == 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
}
