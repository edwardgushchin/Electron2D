using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

internal static partial class ShaderPrecision
{
    internal static (bool Declared, int Mask, bool Declared64) ReadFloatAtomicRequirements(byte[] code)
    {
        // ReadRequirements has already validated scalar types and instruction boundaries.
        var words = MemoryMarshal.Cast<byte, uint>(code.AsSpan()); var declared = false; var declared64 = false; var extension = false;
        var floats = new Dictionary<uint, uint>(); var pointers = new Dictionary<uint, (uint Storage, uint Element)>();
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
        {
            var count = (int)(words[at] >> 16); var op = words[at] & 65535;
            if (op == 17)
            {
                declared |= words[at + 1] == 6033;
                declared64 |= words[at + 1] == 6034;
                if (words[at + 1] is 5404 or 6095 or 5612 or 5613 or 5616) throw new NotSupportedException("Half, vector and minimum/maximum floating atomics require their native precision adapter.");
            }
            if (op == 10) extension |= MemoryMarshal.AsBytes(words.Slice(at + 1, count - 1)).SequenceEqual("SPV_EXT_shader_atomic_float_add\0"u8);
            if (op == 22) floats.Add(words[at + 1], words[at + 2]);
            if (op == 32)
            { if (count != 4) throw new ArgumentException("An atomic pointer requires an explicit storage class and element type."); pointers.Add(words[at + 1], (words[at + 2], words[at + 3])); }
        }
        var values = ReadPointerValues(words, pointers); var required = 0;
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
        {
            var count = (int)(words[at] >> 16); var op = words[at] & 65535;
            if (op is not (>= 227 and <= 242 or 6035 or 5614 or 5615)) continue;
            var minimum = op is 227 or 232 or 233 ? 6 : op == 228 ? 5 : op is 230 or 231 ? 9 : 7;
            if (count < minimum) throw new ArgumentException("An atomic instruction is missing required operands.");
            if (!values.TryGetValue(words[at + (op == 228 ? 1 : 3)], out var type) || !pointers.TryGetValue(type, out var pointer)) throw new NotSupportedException("Atomics require an explicit native pointer type.");
            if (!floats.TryGetValue(pointer.Element, out var width))
            { if (op is 6035 or 5614 or 5615) throw new ArgumentException("A floating atomic instruction requires a floating-point pointer."); continue; }
            if (op != 228 && words[at + 1] != pointer.Element) throw new ArgumentException("A floating atomic result must match its pointed-to scalar type.");
            if (width is not (32 or 64) || op is not (227 or 228 or 229 or 6035)) throw new NotSupportedException("This floating atomic operation requires its native precision adapter.");
            if (op == 6035 && (!(width == 32 ? declared : declared64) || !extension)) throw new ArgumentException("Floating atomic addition requires its matching precision capability and extension.");
            var bit = (width, pointer.Storage) switch { (32, 2 or 12 or 5349) => 0, (32, 4) => 2, (32, 11) => 4, (64, 2 or 12 or 5349) => 6, (64, 4) => 8, _ => throw new NotSupportedException("This floating atomic storage class requires its native resource adapter.") };
            required |= 1 << (bit + (op == 6035 ? 1 : 0));
        }
        if ((declared || declared64) && !extension) throw new ArgumentException("The floating atomic capability requires its extension declaration.");
        return (declared, required, declared64);
    }

    private static void ValidateFloatAtomics(nint device, byte[] code)
    {
        var required = ReadFloatAtomicRequirements(code);
        var enabled = SDL.GetGPUDeviceDriver(device) == "vulkan" ? SDL.GetNumberProperty(SDL.GetGPUDeviceProperties(device), GPUDeviceFactory.FloatAtomicProperty, 0) | SDL.GetNumberProperty(SDL.GetGPUDeviceProperties(device), GPUDeviceFactory.FloatAtomic64Property, 0) << 6 : 0;
        if ((required.Mask & ~enabled) != 0 || required.Declared && (enabled & 42) == 0 || required.Declared64 && (enabled & 640) == 0) throw new NotSupportedException("Floating atomics require the enabled native precision and buffer, shared or image base/add feature for their storage class.");
    }
}
