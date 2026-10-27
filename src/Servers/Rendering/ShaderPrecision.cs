using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

internal static class ShaderPrecision
{
    internal static bool RequiresFloat64(byte[] code)
    {
        ArgumentNullException.ThrowIfNull(code);
        if (code.Length < 20 || code.Length > 16 * 1024 * 1024 || code.Length % 4 != 0) throw new ArgumentException("A shader requires bounded aligned SPIR-V words.", nameof(code));
        var words = MemoryMarshal.Cast<byte, uint>(code.AsSpan());
        if (words[0] != 0x07230203 || words[3] is 0 or > 1_048_576 || words[4] != 0) throw new ArgumentException("Invalid shader header.", nameof(code));
        var capability = false; var usesFloat64 = false;
        for (var at = 5; at < words.Length;)
        {
            var count = (int)(words[at] >> 16); var op = words[at] & 65535;
            if (count == 0 || count > words.Length - at) throw new ArgumentException("Truncated shader instruction.", nameof(code));
            if (op == 17)
            {
                if (count != 2) throw new ArgumentException("Invalid shader capability.", nameof(code));
                capability |= words[at + 1] == 10;
                if (words[at + 1] is 8 or 9 or 11 or 12 or 22) throw new NotSupportedException("This scalar precision requires its native shader adapter.");
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
                if (words[at + 2] != 32) throw new NotSupportedException("Shader integer scalars require the 32-bit native profile.");
            }
            at += count;
        }
        if (usesFloat64 && !capability) throw new ArgumentException("A double shader type requires the Float64 capability.", nameof(code));
        return capability;
    }

    internal static void ValidateNative(nint device, byte[] code)
    {
        if (RequiresFloat64(code) && (SDL.GetGPUDeviceDriver(device) != "vulkan" ||
            !SDL.GetBooleanProperty(SDL.GetGPUDeviceProperties(device), GPUDeviceFactory.Float64Property, false)))
            throw new NotSupportedException("FP64 shaders require an enabled native Vulkan shaderFloat64 feature.");
    }
}
