using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

internal static class ShaderPrecision
{
    internal static (bool Float64, bool Int64) ReadRequirements(byte[] code)
    {
        ArgumentNullException.ThrowIfNull(code);
        if (code.Length < 20 || code.Length > 16 * 1024 * 1024 || code.Length % 4 != 0) throw new ArgumentException("A shader requires bounded aligned SPIR-V words.", nameof(code));
        var words = MemoryMarshal.Cast<byte, uint>(code.AsSpan());
        if (words[0] != 0x07230203 || words[3] is 0 or > 1_048_576 || words[4] != 0) throw new ArgumentException("Invalid shader header.", nameof(code));
        var float64 = false; var int64 = false; var usesFloat64 = false; var usesInt64 = false;
        for (var at = 5; at < words.Length;)
        {
            var count = (int)(words[at] >> 16); var op = words[at] & 65535;
            if (count == 0 || count > words.Length - at) throw new ArgumentException("Truncated shader instruction.", nameof(code));
            if (op == 17)
            {
                if (count != 2) throw new ArgumentException("Invalid shader capability.", nameof(code));
                float64 |= words[at + 1] == 10; int64 |= words[at + 1] == 11;
                if (words[at + 1] is 8 or 9 or 12 or 22) throw new NotSupportedException("This scalar precision requires its native shader adapter.");
            }
            if (op == 22)
            {
                if (count != 3) throw new ArgumentException("Invalid floating-point shader type.", nameof(code));
                if (words[at + 2] is not (32 or 64)) throw new NotSupportedException("This floating-point precision requires its native shader adapter.");
                usesFloat64 |= words[at + 2] == 64;
            }
            if (op == 21)
            {
                if (count != 4 || words[at + 3] > 1) throw new ArgumentException("Invalid integer shader type.", nameof(code));
                if (words[at + 2] is not (32 or 64)) throw new NotSupportedException("This integer precision requires its native shader adapter.");
                usesInt64 |= words[at + 2] == 64;
            }
            at += count;
        }
        if (usesFloat64 && !float64) throw new ArgumentException("A double shader type requires the Float64 capability.", nameof(code));
        if (usesInt64 && !int64) throw new ArgumentException("A 64-bit integer shader type requires the Int64 capability.", nameof(code));
        return (float64, int64);
    }

    internal static void ValidateNative(nint device, byte[] code)
    {
        var required = ReadRequirements(code);
        if (!required.Float64 && !required.Int64) return;
        var vulkan = SDL.GetGPUDeviceDriver(device) == "vulkan"; var properties = SDL.GetGPUDeviceProperties(device);
        if (required.Float64 && (!vulkan || !SDL.GetBooleanProperty(properties, GPUDeviceFactory.Float64Property, false)))
            throw new NotSupportedException("FP64 shaders require an enabled native Vulkan shaderFloat64 feature.");
        if (required.Int64 && (!vulkan || !SDL.GetBooleanProperty(properties, GPUDeviceFactory.Int64Property, false)))
            throw new NotSupportedException("64-bit integer shaders require an enabled native Vulkan shaderInt64 feature.");
    }
}
