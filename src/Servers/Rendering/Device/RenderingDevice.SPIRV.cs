using System.Runtime.InteropServices;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private readonly record struct Binding(uint Set, int Slot, UniformType Type, int Native);
    private static (byte[] Code, Binding[] Layout) Remap(byte[] code)
    {
        if (code.Length < 20 || code.Length > 16 * 1024 * 1024 || code.Length % 4 != 0) throw new ArgumentException("A compute module must contain aligned SPIR-V words.", nameof(code));
        var words = MemoryMarshal.Cast<byte, uint>(code.AsSpan());
        if (words[0] != 0x07230203 || words[3] == 0 || words[3] > 1_048_576 || words[4] != 0) throw new ArgumentException("Invalid SPIR-V header.", nameof(code));
        var sets = new Dictionary<uint, int>(); var slots = new Dictionary<uint, int>();
        var pointers = new Dictionary<uint, uint>(); var bufferBlocks = new HashSet<uint>();
        var variables = new List<(uint ID, uint Type, uint Storage)>();
        var entry = false; var localSize = false;
        for (var i = 5; i < words.Length;)
        {
            var size = (int)(words[i] >> 16); var op = words[i] & 65535;
            if (size == 0 || size > words.Length - i) throw new ArgumentException("Truncated SPIR-V instruction.", nameof(code));
            switch (op)
            {
                case 15:
                    if (entry || size < 5 || words[i + 1] != 5 || words[i + 3] != 0x6e69616d || words[i + 4] != 0) throw new NotSupportedException("One compute entry point named main is required.");
                    entry = true; break;
                case 16 when size >= 3 && words[i + 2] == 17:
                    if (size != 6 || words[i + 3] == 0 || words[i + 4] == 0 || words[i + 5] == 0 || (ulong)words[i + 3] * words[i + 4] * words[i + 5] > 1024) throw new ArgumentException("Invalid compute workgroup dimensions.", nameof(code));
                    localSize = true; break;
                case 32 when size == 4: pointers.Add(words[i + 1], words[i + 3]); break;
                case 59 when size >= 4: variables.Add((words[i + 2], words[i + 1], words[i + 3])); break;
                case 71 when size >= 3:
                    if (words[i + 2] == 34 && size == 4) sets.Add(words[i + 1], i + 3);
                    else if (words[i + 2] == 33 && size == 4) slots.Add(words[i + 1], i + 3);
                    else if (words[i + 2] == 3) bufferBlocks.Add(words[i + 1]);
                    else if (words[i + 2] == 24) for (var j = 0; j < size; j++) words[i + j] = 1u << 16;
                    break;
                case 72 when size >= 4 && words[i + 3] == 24:
                    for (var j = 0; j < size; j++) words[i + j] = 1u << 16;
                    break;
            }
            i += size;
        }
        if (!entry || !localSize) throw new ArgumentException("Missing compute entry point or static workgroup dimensions.", nameof(code));
        var layout = new List<Binding>(); var storage = 0; var uniform = 0;
        foreach (var variable in variables)
        {
            if (variable.Storage is 9 or 0) throw new NotSupportedException("Push constants and texture/sampler descriptors are not integrated into local compute.");
            if (variable.Storage is not (2 or 12)) continue;
            if (!sets.TryGetValue(variable.ID, out var setOffset) || !slots.TryGetValue(variable.ID, out var slotOffset) || !pointers.TryGetValue(variable.Type, out var block)) throw new ArgumentException("A buffer is missing its descriptor layout.", nameof(code));
            var isStorage = variable.Storage == 12 || bufferBlocks.Contains(block);
            var native = isStorage ? storage++ : uniform++;
            if (storage > 8 || uniform > 4) throw new NotSupportedException("Local compute supports eight storage buffers and four uniform buffers per shader.");
            var binding = new Binding(words[setOffset], checked((int)words[slotOffset]), isStorage ? UniformType.StorageBuffer : UniformType.UniformBuffer, native);
            if (layout.Any(b => b.Set == binding.Set && b.Slot == binding.Slot)) throw new ArgumentException("Duplicate shader descriptor binding.", nameof(code));
            layout.Add(binding); words[setOffset] = isStorage ? 1u : 2u; words[slotOffset] = (uint)native;
        }
        return (code, layout.ToArray());
    }
}
