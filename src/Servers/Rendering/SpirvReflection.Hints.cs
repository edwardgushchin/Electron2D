using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Electron2D;

internal static unsafe partial class SpirvReflection
{
    private static void ReadHints(byte[] code, Dictionary<string, ShaderUniform> uniforms, Dictionary<string, ShaderUniform> instances, bool fragment)
    {
        var names = new HashSet<string>(StringComparer.Ordinal); var words = MemoryMarshal.Cast<byte, uint>(code);
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
        {
            var count = (int)(words[at] >> 16);
            if ((words[at] & 0xffff) != 7 || count < 3) continue;
            var bytes = MemoryMarshal.AsBytes(words.Slice(at + 2, count - 2));
            if (!bytes.StartsWith("Electron2D:hint:"u8)) continue;
            var end = bytes.IndexOf((byte)0); if (end < 0) throw new ArgumentException("Unterminated shader hint metadata.", nameof(code));
            try
            {
                var utf8 = new UTF8Encoding(false, true); var parts = utf8.GetString(bytes[..end]).Split(':');
                if (parts.Length != 7 || parts[2] != "1" || parts[6] is not ("0" or "1")) throw new NotSupportedException("Unsupported shader hint metadata.");
                var name = utf8.GetString(Convert.FromBase64String(parts[3]));
                if (string.IsNullOrWhiteSpace(name) || !names.Add(name)) throw new ArgumentException("Shader hints require unique nonblank names.", nameof(code));
                var owner = uniforms.ContainsKey(name) ? uniforms : instances;
                if (!owner.TryGetValue(name, out var uniform)) throw new ArgumentException("Shader hint names an absent, inactive or renderer-owned parameter.", nameof(code));
                if (!int.TryParse(parts[4], NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number is < 0 or > 2)
                    throw new ArgumentException("Shader hints support None, Range and Enum metadata.", nameof(code));
                var hint = (PropertyHint)number; var text = utf8.GetString(Convert.FromBase64String(parts[5])); var color = parts[6] == "1";
                if (!fragment && uniform.Buffer == 0) throw new ArgumentException("Renderer Frame fields cannot receive shader hint metadata.", nameof(code));
                if (uniform.ArrayLength != 0 || hint != PropertyHint.None && uniform.BooleanWidth != 0 || color && (uniform.Type != typeof(Vector4) || uniform.BooleanWidth != 0) ||
                    hint == PropertyHint.Range && uniform.Type != typeof(float) && uniform.Type != typeof(int) ||
                    hint == PropertyHint.Enum && (uniform.Type != typeof(int) || uniform.BooleanWidth != 0) ||
                    color && hint != PropertyHint.None)
                    throw new ArgumentException("Shader hint differs from the declared numeric role.", nameof(code));
                owner[name] = uniform with { HintMetadata = (hint, text, color) };
            }
            catch (Exception error) when (error is FormatException or DecoderFallbackException)
            { throw new ArgumentException("Malformed shader hint metadata.", nameof(code), error); }
        }
    }
}
