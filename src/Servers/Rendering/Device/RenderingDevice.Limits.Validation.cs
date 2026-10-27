using System.Runtime.InteropServices;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private void ValidateShaderLimits(byte[][] modules, Entry entry)
    {
        if (ValidationLimits is not { } limits) return;
        if ((ulong)entry.Layout.Select(b => b.Set).Distinct().Count() > limits[0]) throw new ArgumentException("The shader exceeds the device logical set capacity.");
        var total = new ulong[5];
        foreach (var stage in modules.Length == 1 ? new[] { ShaderStage.Compute } : new[] { ShaderStage.Vertex, ShaderStage.Fragment })
        {
            var counts = new ulong[5];
            foreach (var binding in entry.Layout.Concat(entry.Samplers.Select(s => s.Native)).Where(b => b.Stage == stage && b.Native >= 0))
            {
                var category = binding.Type switch
                {
                    UniformType.SamplerWithTexture => 0,
                    UniformType.StorageBuffer or UniformType.TextureBuffer or UniformType.ImageBuffer or UniformType.SamplerWithTextureBuffer => 2,
                    UniformType.Image or UniformType.InputAttachment => 3,
                    UniformType.UniformBuffer => 4,
                    _ => -1
                };
                if (category >= 0) counts[category] = Math.Max(counts[category], (ulong)binding.Native + 1);
            }
            foreach (var push in entry.Push.Where(p => p.Stage == stage)) counts[4] = Math.Max(counts[4], (ulong)push.Native + 1);
            counts[1] = counts[0];
            for (var i = 0; i < counts.Length; i++)
            {
                if (counts[i] > limits[15 + i]) throw new ArgumentException("A shader stage exceeds the native resource capacity.");
                total[i] += counts[i];
            }
        }
        for (var i = 0; i < total.Length; i++)
            if (total[i] > limits[2 + i]) throw new ArgumentException("The shader exceeds the native combined resource capacity.");
        if (entry.Stage != ShaderStage.Compute) return;
        var words = MemoryMarshal.Cast<byte, uint>(modules[0].AsSpan());
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
            if ((words[at] & 65535) == 16 && words[at] >> 16 == 6 && words[at + 2] == 17)
            {
                var x = words[at + 3]; var y = words[at + 4]; var z = words[at + 5];
                if (x > limits[32] || y > limits[33] || z > limits[34] || (ulong)x * y * z > limits[31])
                    throw new ArgumentException("The compute workgroup exceeds native device limits.");
            }
    }
}
