using System.Runtime.InteropServices;
using Electron2D;

internal static partial class GraphicsStorage
{
    private static void ExportTexel(RDShaderSPIRV module, string name)
    {
        var directory = Environment.GetEnvironmentVariable("ELECTRON2D_GRAPHICS_TEXEL_OUTPUT"); if (directory is null) return; Directory.CreateDirectory(directory);
        File.WriteAllBytes(System.IO.Path.Combine(directory, name + ".vert.spv"), module.BytecodeVertex); File.WriteAllBytes(System.IO.Path.Combine(directory, name + ".frag.spv"), module.BytecodeFragment);
    }
    private static byte[] SpecializeTexelArray(byte[] bytes)
    {
        var words = MemoryMarshal.Cast<byte, uint>(bytes).ToArray(); var constants = new Dictionary<uint, (uint Type, uint Value)>(); var arrays = new List<int>(); var unsigned = new HashSet<uint>(); var firstType = 0;
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
        {
            var count = (int)(words[at] >> 16); var op = words[at] & 65535; Check(count > 0 && count <= words.Length - at, "Bounded HLSL array fixture.");
            if (op is >= 19 and <= 39 && firstType == 0) firstType = at;
            if (op == 21 && count == 4 && words[at + 2] == 32 && words[at + 3] == 0) unsigned.Add(words[at + 1]);
            if (op == 43 && count == 4) constants.Add(words[at + 2], (words[at + 1], words[at + 3]));
            if (op == 28) arrays.Add(at);
        }
        Check(arrays.Count == 2 && firstType > 0, "The HLSL fixture has exactly two resource arrays."); var length = constants[words[arrays[0] + 3]]; Check(unsigned.Contains(length.Type) && length.Value == 2 && arrays.All(at => constants[words[at + 3]] == length), "Both HLSL resource arrays share a typed unsigned two-element extent.");
        var id = words[3]++; foreach (var at in arrays) words[at + 3] = id;
        var output = new List<uint>(words.Length + 8); output.AddRange(words.AsSpan(0, firstType).ToArray()); output.AddRange([(4u << 16) | 71, id, 1, 0]); output.AddRange(words.AsSpan(firstType, arrays[0] - firstType).ToArray()); output.AddRange([(4u << 16) | 50, length.Type, id, 2]); output.AddRange(words.AsSpan(arrays[0]).ToArray());
        return MemoryMarshal.AsBytes(output.ToArray().AsSpan()).ToArray();
    }
}
