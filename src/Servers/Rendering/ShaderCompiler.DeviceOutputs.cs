using System.Runtime.InteropServices;

namespace Electron2D;

internal static unsafe partial class ShaderCompiler
{
    internal static byte[] ExpandDeviceLocations(byte[] code)
    {
        var words = MemoryMarshal.Cast<byte, uint>(code).ToArray();
        var groups = new HashSet<uint>(); var decorations = new Dictionary<uint, List<(uint Kind, uint Value)>>();
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
            if ((words[at] & 65535) == 73) groups.Add(words[at + 1]);
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
            if ((words[at] & 65535) == 71 && words[at] >> 16 == 4 && words[at + 2] is 30 or 32 && groups.Contains(words[at + 1]))
            {
                if (!decorations.TryGetValue(words[at + 1], out var list)) decorations.Add(words[at + 1], list = []);
                list.Add((words[at + 2], words[at + 3]));
            }
        if (decorations.Count == 0) return code;
        var result = new List<uint>(words.Length); result.AddRange(words.AsSpan(0, 5));
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
        {
            var op = words[at] & 65535; var count = (int)(words[at] >> 16);
            if (op == 71 && count == 4 && words[at + 2] is 30 or 32 && decorations.ContainsKey(words[at + 1])) continue;
            result.AddRange(words.AsSpan(at, count));
            if (op == 74 && decorations.TryGetValue(words[at + 1], out var values))
                for (var i = at + 2; i < at + count; i++) foreach (var value in values) result.AddRange([0x40047, words[i], value.Kind, value.Value]);
            if (op == 75 && decorations.TryGetValue(words[at + 1], out values))
                for (var i = at + 2; i + 1 < at + count; i += 2) foreach (var value in values) result.AddRange([0x50048, words[i], words[i + 1], value.Kind, value.Value]);
        }
        return MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(result)).ToArray();
    }

    private static byte[] ReflectDeviceOutputs(byte[] code)
    {
        var words = MemoryMarshal.Cast<byte, uint>(ExpandDeviceLocations(code)).ToArray();
        var outputs = new HashSet<uint>(); var indices = new Dictionary<uint, uint>();
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
        {
            var op = words[at] & 65535; var count = words[at] >> 16;
            if (op == 59 && count >= 4 && words[at + 3] == 3) outputs.Add(words[at + 2]);
            if (op == 71 && count == 4 && words[at + 2] == 32) indices.Add(words[at + 1], words[at + 3]);
            if (op == 72 && count == 5 && words[at + 3] == 32 && words[at + 4] != 0) throw new NotSupportedException("Indexed fragment interface blocks require their native interface adapter.");
        }
        if (indices.Any(pair => !outputs.Contains(pair.Key) || pair.Value > 1)) throw new ArgumentException("A fragment output index must be zero or one.");
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
            if ((words[at] & 65535) == 71 && words[at] >> 16 == 4 && words[at + 2] == 30 && outputs.Contains(words[at + 1]))
            {
                if (words[at + 3] > 15) throw new ArgumentException("Invalid fragment output location.");
                // SDL reflection omits Index: reserve the second location bank only in this cold reflection copy.
                if (indices.GetValueOrDefault(words[at + 1]) == 1) words[at + 3] += 16;
            }
        return MemoryMarshal.AsBytes(words.AsSpan()).ToArray();
    }
}
