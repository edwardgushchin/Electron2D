using System.Runtime.InteropServices;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private readonly record struct Binding(uint Set, int Slot, UniformType Type, int Native, bool Array = false, uint ImageFormat = 0, int PixelType = 0);
    private static (byte[] Code, Binding[] Layout) Remap(byte[] code)
    {
        if (code.Length < 20 || code.Length > 16 * 1024 * 1024 || code.Length % 4 != 0) throw new ArgumentException("A compute module must contain aligned SPIR-V words.", nameof(code));
        var words = MemoryMarshal.Cast<byte, uint>(code.AsSpan());
        if (words[0] != 0x07230203 || words[3] == 0 || words[3] > 1_048_576 || words[4] != 0) throw new ArgumentException("Invalid SPIR-V header.", nameof(code));
        var sets = new Dictionary<uint, int>(); var slots = new Dictionary<uint, int>();
        var pointers = new Dictionary<uint, uint>(); var bufferBlocks = new HashSet<uint>();
        var variables = new List<(uint ID, uint Type, uint Storage)>();
        var images = new Dictionary<uint, (uint Scalar, uint Array, uint Sampled, uint Format)>(); var scalars = new Dictionary<uint, int>(); var pairedSamplers = new HashSet<uint>(); var sampled = new Dictionary<uint, uint>(); var samplerTypes = new HashSet<uint>();
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
                    if (size != 6 || words[i + 3] is 0 or > 1024 || words[i + 4] is 0 or > 1024 || words[i + 5] is 0 or > 1024 || (ulong)words[i + 3] * words[i + 4] * words[i + 5] > 1024) throw new ArgumentException("Invalid compute workgroup dimensions.", nameof(code));
                    localSize = true; break;
                case 21 when size == 4:
                    if (words[i + 2] == 32 && words[i + 3] <= 1) scalars.Add(words[i + 1], words[i + 3] == 1 ? 1 : 2);
                    break;
                case 22 when size == 3:
                    if (words[i + 2] == 32) scalars.Add(words[i + 1], 0);
                    break;
                case 25 when size is 9 or 10:
                    if (words[i + 3] != 1 || words[i + 4] != 0 || words[i + 5] > 1 || words[i + 6] != 0 || words[i + 7] is not (1 or 2)) throw new NotSupportedException("Compute images must be non-multisampled two-dimensional textures.");
                    images.Add(words[i + 1], (words[i + 2], words[i + 5], words[i + 7], words[i + 8])); break;
                case 26 when size == 2: samplerTypes.Add(words[i + 1]); break;
                case 27 when size == 3: sampled.Add(words[i + 1], words[i + 2]); break;
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
        var logical = new Dictionary<uint, (uint Set, int Slot)>();
        foreach (var v in variables) if (sets.TryGetValue(v.ID, out var ds) && slots.TryGetValue(v.ID, out var bs)) logical[v.ID] = (words[ds], checked((int)words[bs]));
        var layout = new List<Binding>(); var storage = 0; var uniform = 0; var imageSlot = 0; var textureSlot = 0;
        foreach (var variable in variables)
        {
            if (variable.Storage == 9) throw new NotSupportedException("Push constants are not integrated into local compute.");
            if (variable.Storage == 0)
            {
                if (!sets.TryGetValue(variable.ID, out var imageSet) || !slots.TryGetValue(variable.ID, out var imageBinding) || !pointers.TryGetValue(variable.Type, out var imageType)) throw new ArgumentException("An image has no descriptor layout.", nameof(code));
                if (samplerTypes.Contains(imageType)) continue;
                var combined = sampled.TryGetValue(imageType, out var underlying); if (combined) imageType = underlying;
                if (!images.TryGetValue(imageType, out var image)) throw new NotSupportedException("Image descriptor arrays are not integrated.");
                if (!scalars.TryGetValue(image.Scalar, out var pixelType)) throw new NotSupportedException("Compute images require 32-bit float, signed or unsigned pixel scalars.");
                var writable = image.Sampled == 2;
                if (combined && writable) throw new ArgumentException("A storage image cannot contain a combined sampler.", nameof(code));
                if (writable && image.Array != 0) throw new NotSupportedException("Array image storage requires subresource-view integration.");
                var nativeImage = writable ? imageSlot++ : textureSlot++;
                if (imageSlot > 8 || textureSlot > 16) throw new NotSupportedException("Compute image bindings exceed backend limits.");
                var origin = logical[variable.ID];
                if (layout.Any(b => b.Set == origin.Set && b.Slot == origin.Slot)) throw new ArgumentException("Duplicate image binding.", nameof(code));
                layout.Add(new(words[imageSet], checked((int)words[imageBinding]), writable ? UniformType.Image : UniformType.SamplerWithTexture, nativeImage, image.Array != 0, image.Format, pixelType));
                if (!writable && !combined)
                {
                    var paired = variables.SingleOrDefault(v => pointers.TryGetValue(v.Type, out var ty) && samplerTypes.Contains(ty) && logical.TryGetValue(v.ID, out var loc) && loc == origin);
                    if (paired == default) throw new NotSupportedException("A separate image requires a sampler at the same logical set/binding.");
                    if (!pairedSamplers.Add(paired.ID)) throw new NotSupportedException("A separate sampler must pair with exactly one image.");
                    words[sets[paired.ID]] = 0; words[slots[paired.ID]] = (uint)nativeImage;
                }
                words[imageSet] = writable ? 1u : 0u; words[imageBinding] = (uint)nativeImage;
                continue;
            }
            if (variable.Storage is not (2 or 12)) continue;
            if (!sets.TryGetValue(variable.ID, out var setOffset) || !slots.TryGetValue(variable.ID, out var slotOffset) || !pointers.TryGetValue(variable.Type, out var block)) throw new ArgumentException("A buffer is missing its descriptor layout.", nameof(code));
            var isStorage = variable.Storage == 12 || bufferBlocks.Contains(block);
            var native = isStorage ? storage++ : uniform++;
            if (storage > 8 || uniform > 4) throw new NotSupportedException("Local compute supports eight storage buffers and four uniform buffers per shader.");
            var binding = new Binding(words[setOffset], checked((int)words[slotOffset]), isStorage ? UniformType.StorageBuffer : UniformType.UniformBuffer, native);
            if (layout.Any(b => b.Set == binding.Set && b.Slot == binding.Slot)) throw new ArgumentException("Duplicate shader descriptor binding.", nameof(code));
            layout.Add(binding); words[setOffset] = isStorage ? 1u : 2u; words[slotOffset] = (uint)native;
        }
        foreach (var variable in variables)
            if (variable.Storage == 0 && pointers.TryGetValue(variable.Type, out var type) && samplerTypes.Contains(type) && !pairedSamplers.Contains(variable.ID)) throw new NotSupportedException("An orphan sampler has no paired image.");
        // Writable image slots precede writable buffer slots in the native compute interface.
        foreach (var variable in variables.Where(v => v.Storage is 2 or 12))
            if (sets.TryGetValue(variable.ID, out var si) && slots.TryGetValue(variable.ID, out var bi) && words[si] == 1) words[bi] += (uint)imageSlot;
        return (code, layout.ToArray());
    }
}
