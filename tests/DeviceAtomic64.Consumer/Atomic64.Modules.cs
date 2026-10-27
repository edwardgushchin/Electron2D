using System.Runtime.InteropServices;

internal static partial class Atomic64
{
    private static byte[] CounterOpcodes(byte[] code)
    {
        var words = MemoryMarshal.Cast<byte, uint>(code.AsSpan()).ToArray();
        var constants = new Dictionary<uint, (uint Type, ulong Bits)>(); var output = new List<uint>(words.Take(5)); var id = words[3]++;
        var replacements = 0; var inserted = false;
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
        {
            var count = (int)(words[at] >> 16); var op = words[at] & 65535; var item = words.AsSpan(at, count).ToArray();
            if (op == 17 && item[1] == 11) continue;
            if (op == 43 && count == 5) constants.Add(item[2], (item[1], item[3] | (ulong)item[4] << 32));
            if (op == 54 && !inserted) { inserted = true; var type = constants.Values.First(c => c.Bits == unchecked((ulong)-5L)).Type; output.AddRange([5u << 16 | 43, type, id, 5, 0]); }
            if (op == 234 && count == 7 && constants.TryGetValue(item[6], out var value))
            {
                if (value.Bits is 1 or ulong.MaxValue) { item = item[..6]; item[0] = 6u << 16 | (value.Bits == 1 ? 232u : 233u); replacements++; }
                else if (value.Bits == unchecked((ulong)-5L)) { item[0] = 7u << 16 | 235; item[6] = id; replacements++; }
            }
            output.AddRange(item);
        }
        Check(replacements >= 4, "Raw fixture exercises increment, decrement and subtract without an explicit Int64 declaration."); output[3] = words[3];
        var result = MemoryMarshal.AsBytes(output.ToArray().AsSpan()).ToArray();
        var directory = Environment.GetEnvironmentVariable("ELECTRON2D_ATOMIC64_OUTPUT");
        if (!string.IsNullOrEmpty(directory)) File.WriteAllBytes(System.IO.Path.Combine(directory, "counter-" + replacements + "-" + code.Length + ".spv"), result);
        return result;
    }
}
