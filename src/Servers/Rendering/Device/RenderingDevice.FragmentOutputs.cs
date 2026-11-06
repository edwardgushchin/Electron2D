using System.Runtime.InteropServices;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private static byte[] RemapFragmentOutputs(byte[] code, int[] slots, ShaderCompiler.DeviceIO[] outputs)
    {
        if (outputs.Count(output => output.Index == 0) != slots.Length || outputs.Any(output => !slots.Contains((int)output.Location)))
            throw new ArgumentException("Fragment outputs must exactly match the active framebuffer color slots.");
        var words = MemoryMarshal.Cast<byte, uint>(ShaderCompiler.ExpandDeviceLocations(code)).ToArray();
        var variables = new HashSet<uint>();
        for (var i = 5; i < words.Length; i += (int)(words[i] >> 16))
        {
            var op = words[i] & 65535; var size = (int)(words[i] >> 16);
            if (op == 59 && size >= 4 && words[i + 3] == 3) variables.Add(words[i + 2]);
        }
        var result = new List<uint>(words.Length); result.AddRange(words.AsSpan(0, 5)); var mapped = new HashSet<uint>();
        uint Location(uint variable, uint logical)
        {
            if (!variables.Contains(variable)) return logical;
            var native = Array.IndexOf(slots, (int)logical);
            if (native < 0) throw new ArgumentException("A fragment output addresses an unused color slot.");
            mapped.Add(variable); return (uint)native;
        }
        for (var i = 5; i < words.Length; i += (int)(words[i] >> 16))
        {
            var op = words[i] & 65535; var size = (int)(words[i] >> 16);
            if (op == 71 && size == 4 && words[i + 2] == 30)
            {
                words[i + 3] = Location(words[i + 1], words[i + 3]);
            }
            result.AddRange(words.AsSpan(i, size));
        }
        if (mapped.Count != outputs.Length) throw new NotSupportedException("Fragment output location encoding requires its native interface adapter.");
        return MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(result)).ToArray();
    }
}
