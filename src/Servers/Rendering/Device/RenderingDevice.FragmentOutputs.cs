using System.Runtime.InteropServices;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private static byte[] RemapFragmentOutputs(byte[] code, int[] slots, ShaderCompiler.DeviceIO[] outputs)
    {
        if (outputs.Length != slots.Length || outputs.Any(output => !slots.Contains((int)output.Location)))
            throw new ArgumentException("Fragment outputs must exactly match the active framebuffer color slots.");
        var words = MemoryMarshal.Cast<byte, uint>(code).ToArray();
        var variables = new HashSet<uint>(); var groups = new HashSet<uint>(); var locations = new Dictionary<uint, uint>();
        for (var i = 5; i < words.Length; i += (int)(words[i] >> 16))
        {
            var op = words[i] & 65535; var size = (int)(words[i] >> 16);
            if (op == 59 && size >= 4 && words[i + 3] == 3) variables.Add(words[i + 2]);
            if (op == 73 && size == 2) groups.Add(words[i + 1]);
        }
        for (var i = 5; i < words.Length; i += (int)(words[i] >> 16))
            if ((words[i] & 65535) == 71 && words[i] >> 16 == 4 && words[i + 2] == 30 && groups.Contains(words[i + 1])) locations.Add(words[i + 1], words[i + 3]);
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
                if (locations.ContainsKey(words[i + 1])) continue;
                words[i + 3] = Location(words[i + 1], words[i + 3]);
            }
            result.AddRange(words.AsSpan(i, size));
            // Expand grouped locations so shared input decorations keep their original values.
            if (op == 74 && locations.TryGetValue(words[i + 1], out var location))
                for (var target = i + 2; target < i + size; target++) result.AddRange([(4u << 16) | 71, words[target], 30, Location(words[target], location)]);
            if (op == 75 && locations.TryGetValue(words[i + 1], out location))
                for (var target = i + 2; target + 1 < i + size; target += 2) result.AddRange([(5u << 16) | 72, words[target], words[target + 1], 30, location]);
        }
        if (mapped.Count != outputs.Length) throw new NotSupportedException("Fragment output location encoding requires its native interface adapter.");
        return MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(result)).ToArray();
    }
}
