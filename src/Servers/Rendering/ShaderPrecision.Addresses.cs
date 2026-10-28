using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

internal static partial class ShaderPrecision
{
    internal static bool ReadAddressRequirement(byte[] code)
    {
        if (code.Length == 0) return false;
        var words = MemoryMarshal.Cast<byte, uint>(code.AsSpan()); var declared = false; var model = false; var pointers = false;
        for (var at = 5; at < words.Length;)
        {
            var count = (int)(words[at] >> 16); var op = words[at] & 65535;
            if (count == 0 || count > words.Length - at) throw new ArgumentException("Truncated shader instruction.", nameof(code));
            declared |= op == 17 && count == 2 && words[at + 1] == 5347;
            if (op == 14)
            {
                if (count != 3 || words[at + 2] != 1 || words[at + 1] is not (0 or 5348)) throw new NotSupportedException("Device shaders require logical or physical-storage GLSL450 addressing.");
                model |= words[at + 1] == 5348;
            }
            pointers |= op == 32 && count == 4 && words[at + 2] == 5349;
            at += count;
        }
        if (declared != model || pointers && !declared) throw new ArgumentException("Physical-storage pointers require their capability and addressing model.", nameof(code));
        return declared;
    }
    private static bool WritesPhysicalStorage(ReadOnlySpan<uint> words)
    {
        var pointers = new Dictionary<uint, (uint Storage, uint Element)>();
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
            if ((words[at] & 65535) == 32 && (words[at] >> 16) == 4) pointers.Add(words[at + 1], (words[at + 2], words[at + 3]));
        var values = ReadPointerValues(words, pointers);
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
        {
            var op = words[at] & 65535; var count = words[at] >> 16;
            var pointer = op is 62 or 63 or 64 or 228 ? 1 : op is >= 229 and <= 242 ? 3 : 0;
            if (pointer != 0 && count > pointer && values.TryGetValue(words[at + pointer], out var type) && pointers[type].Storage == 5349) return true;
        }
        return false;
    }
    private static void ValidateAddresses(nint device, byte[] code)
    {
        if (!ReadAddressRequirement(code)) return;
        var properties = SDL.GetGPUDeviceProperties(device);
        if (!SDL.GetBooleanProperty(properties, GPUDeviceFactory.AddressProperty, false)) throw new NotSupportedException("Physical-storage pointers require an enabled native buffer-address feature and allocation adapter.");
        var words = MemoryMarshal.Cast<byte, uint>(code.AsSpan());
        if (!WritesPhysicalStorage(words)) return;
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
            if ((words[at] & 65535) == 15 && (words[at] >> 16) >= 3 && words[at + 1] is 0 or 4)
            {
                var bit = words[at + 1] == 0 ? 1 : 2;
                if ((SDL.GetNumberProperty(properties, GPUDeviceFactory.GraphicsStorageProperty, 0) & bit) == 0) throw new NotSupportedException("Graphics physical-storage pointers require the stage's enabled native storage feature.");
            }
    }
}
