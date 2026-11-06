using System.Runtime.InteropServices;
using Electron2D;

internal static partial class BlendFactorsTests
{
    private static void Interfaces(RenderingDevice device)
    {
        var original = MemoryMarshal.Cast<byte, uint>(_fragment).ToArray();
        using var module = new RDShaderSPIRV { BytecodeVertex = _vertex };
        uint[] ChangeIndex(uint value)
        {
            var words = original.ToArray();
            for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
                if ((words[at] & 65535) == 71 && words[at] >> 16 == 4 && words[at + 2] == 32 && words[at + 3] == 1) words[at + 3] = value;
            return words;
        }
        module.BytecodeFragment = MemoryMarshal.AsBytes(ChangeIndex(2).AsSpan()).ToArray(); Reject<ArgumentException>(() => device.ShaderCreateFromSPIRV(module));
        module.BytecodeFragment = MemoryMarshal.AsBytes(ChangeIndex(0).AsSpan()).ToArray(); Reject<ArgumentException>(() => device.ShaderCreateFromSPIRV(module));
        var mismatched = original.ToArray();
        for (var at = 5; at < mismatched.Length; at += (int)(mismatched[at] >> 16))
            if ((mismatched[at] & 65535) == 71 && mismatched[at] >> 16 == 4 && mismatched[at + 2] == 30) { mismatched[at + 3] = 1; break; }
        module.BytecodeFragment = MemoryMarshal.AsBytes(mismatched.AsSpan()).ToArray(); Reject<ArgumentException>(() => device.ShaderCreateFromSPIRV(module));
        // Group both Location and Index as an independent precompiled-interface fixture.
        var grouped = new List<uint>(original.AsSpan(0, 5).ToArray()); var next = original[3];
        for (var at = 5; at < original.Length; at += (int)(original[at] >> 16))
        {
            var count = (int)(original[at] >> 16);
            if ((original[at] & 65535) == 71 && count == 4 && original[at + 2] is 30 or 32)
            {
                var group = next++; grouped.AddRange([0x20049, group, 0x40047, group, original[at + 2], original[at + 3], 0x3004A, group, original[at + 1]]);
            }
            else grouped.AddRange(original.AsSpan(at, count));
        }
        grouped[3] = next; module.BytecodeFragment = MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(grouped)).ToArray();
        var shader = device.ShaderCreateFromSPIRV(module); Check(shader.IsValid(), "Grouped primary/secondary locations preserve interface reflection and native shader publication."); device.FreeRID(shader);
    }
}
