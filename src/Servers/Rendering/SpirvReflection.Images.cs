using System.Runtime.InteropServices;
using System.Text;

namespace Electron2D;

internal static unsafe partial class SpirvReflection
{
    private static void ReadImages(byte[] code, ShaderTexture[] textures, bool fragment, bool canvas, Dictionary<string, ShaderUniform> uniforms)
    {
        var names = new HashSet<string>(StringComparer.Ordinal); var words = MemoryMarshal.Cast<byte, uint>(code);
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
        {
            var count = (int)(words[at] >> 16);
            if ((words[at] & 0xffff) != 7 || count < 3) continue;
            var bytes = MemoryMarshal.AsBytes(words.Slice(at + 2, count - 2));
            if (!bytes.StartsWith("Electron2D:image:"u8)) continue;
            var end = bytes.IndexOf((byte)0);
            if (end < 0 || bytes[(end + 1)..].ContainsAnyExcept((byte)0)) throw new ArgumentException("Invalid shader image metadata termination.", nameof(code));
            try
            {
                var utf8 = new UTF8Encoding(false, true); var parts = utf8.GetString(bytes[..end]).Split(':');
                if (parts.Length != 12 || parts[2] != "2" || !canvas) throw new NotSupportedException("Unsupported shader image metadata or domain.");
                string Name(int field)
                {
                    var encoded = Convert.FromBase64String(parts[field]);
                    if (parts[field] != Convert.ToBase64String(encoded)) throw new ArgumentException("Shader image metadata requires canonical names.", nameof(code));
                    var value = utf8.GetString(encoded);
                    if (value.Length != 0 && (!char.IsAsciiLetter(value[0]) || value.Any(c => !(char.IsAsciiLetterOrDigit(c) || c == '_')) || value.StartsWith("gl_", StringComparison.Ordinal) || value.Contains("__", StringComparison.Ordinal)))
                        throw new ArgumentException("Shader image metadata requires ASCII shader identifiers.", nameof(code));
                    return value;
                }
                var name = Name(3); var publicName = Name(4); var global = Name(5);
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(publicName) || !names.Add(name) || uniforms.ContainsKey(publicName) ||
                    publicName is "TIME" or "SCREEN_PIXEL_SIZE" or "SDF_COORDS" or "TEXTURE" or "NORMAL_TEXTURE" or "SPECULAR_SHININESS_TEXTURE" or "SCREEN_TEXTURE" or "SDF_TEXTURE")
                    throw new ArgumentException("Shader image metadata requires unique nonreserved names.", nameof(code));
                if (global.Length != 0)
                {
                    ShaderGlobalParameters.ValidateName(global);
                    if (name != publicName || !name.StartsWith("GLOBAL_", StringComparison.Ordinal)) throw new ArgumentException("Global image aliases require their native binding name.", nameof(code));
                }
                static int Number(string value, char max) => value.Length == 1 && value[0] >= '0' && value[0] <= max
                    ? value[0] - '0' : throw new ArgumentException("Invalid shader image policy value.");
                var array = Number(parts[6], '1') != 0; var color = Number(parts[7], '1') != 0;
                var fallback = Number(parts[8], '4'); var filter = Number(parts[9], '6'); var repeat = Number(parts[10], '2'); var screen = Number(parts[11], '1') != 0;
                var slot = Array.FindIndex(textures, t => t.NativeName == name);
                if (slot < 0) throw new ArgumentException("Shader image metadata names an absent or inactive image.", nameof(code));
                var texture = textures[slot];
                if (texture.IsEngineTexture || texture.PixelType != 0 || texture.ArrayLength != 0 || texture.IsArray != array || array && fallback > 2 ||
                    screen && (array || !fragment || color || fallback != 0))
                    throw new ArgumentException("Shader image policy differs from the image role, shape or stage.", nameof(code));
                textures[slot] = texture with { Name = publicName, ImagePolicy = new(color, fallback, filter, repeat, screen, global.Length == 0 ? null : global) };
            }
            catch (Exception error) when (error is FormatException or DecoderFallbackException)
            { throw new ArgumentException("Malformed shader image metadata.", nameof(code), error); }
        }
        if (textures.Select(t => t.Name).Distinct(StringComparer.Ordinal).Count() != textures.Length)
            throw new ArgumentException("Shader image aliases must remain unique across bindings.", nameof(code));
    }
}
