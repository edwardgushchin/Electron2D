using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Electron2D;

internal static unsafe partial class SpirvReflection
{
    private static void ReadDefaults(byte[] code, Dictionary<string, ShaderUniform> uniforms, Dictionary<string, ShaderUniform> instances)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        var words = MemoryMarshal.Cast<byte, uint>(code);
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
        {
            var count = (int)(words[at] >> 16);
            if ((words[at] & 0xffff) != 7 || count < 3) continue;
            var bytes = MemoryMarshal.AsBytes(words.Slice(at + 2, count - 2));
            if (!bytes.StartsWith("Electron2D:default:"u8)) continue;
            var end = bytes.IndexOf((byte)0);
            if (end < 0) throw new ArgumentException("Unterminated shader default metadata.", nameof(code));
            try
            {
                var utf8 = new UTF8Encoding(false, true); var parts = utf8.GetString(bytes[..end]).Split(':');
                if (parts.Length != 7 || parts[2] != "1") throw new NotSupportedException("Unsupported shader default metadata.");
                var name = utf8.GetString(Convert.FromBase64String(parts[5]));
                if (string.IsNullOrWhiteSpace(name) || !names.Add(name)) throw new ArgumentException("Shader defaults require unique nonblank names.", nameof(code));
                var owner = uniforms.ContainsKey(name) ? uniforms : instances;
                if (!owner.TryGetValue(name, out var uniform)) throw new ArgumentException("Shader default names an absent, inactive or renderer-owned parameter.", nameof(code));
                if (!int.TryParse(parts[4], NumberStyles.None, CultureInfo.InvariantCulture, out var length) || length != uniform.ArrayLength || parts[3] != uniform.DefaultTypeCode)
                    throw new ArgumentException("Shader default type or array shape differs from its declaration.", nameof(code));
                var data = Convert.FromBase64String(parts[6]);
                var expected = checked(uniform.DefaultWordWidth * uniform.Count);
                if (data.Length != expected * 4) throw new ArgumentException("Shader default has an invalid component count.", nameof(code));
                var values = new uint[expected];
                for (var i = 0; i < values.Length; i++)
                {
                    var value = values[i] = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(i * 4, 4));
                    if (uniform.BooleanWidth != 0 && value > 1 || parts[3][0] is 'f' or 'm' && !float.IsFinite(BitConverter.UInt32BitsToSingle(value)))
                        throw new ArgumentException("Shader defaults require finite floats and canonical boolean components.", nameof(code));
                }
                owner[name] = uniform with { DefaultWords = values };
            }
            catch (Exception error) when (error is FormatException or DecoderFallbackException)
            { throw new ArgumentException("Malformed shader default metadata.", nameof(code), error); }
        }
    }
}
