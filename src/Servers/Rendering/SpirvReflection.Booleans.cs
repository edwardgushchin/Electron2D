using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Electron2D;

internal static unsafe partial class SpirvReflection
{
    private static Dictionary<string, (int Width, int Length)> ReadBooleans(byte[] code)
    {
        var result = new Dictionary<string, (int, int)>(StringComparer.Ordinal);
        var words = MemoryMarshal.Cast<byte, uint>(code);
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
        {
            var count = (int)(words[at] >> 16);
            if ((words[at] & 0xffff) != 7 || count < 3) continue;
            var bytes = MemoryMarshal.AsBytes(words.Slice(at + 2, count - 2));
            if (!bytes.StartsWith("Electron2D:"u8)) continue;
            var end = bytes.IndexOf((byte)0);
            if (end < 0) throw new ArgumentException("Unterminated shader type metadata.", nameof(code));
            try
            {
                var utf8 = new UTF8Encoding(false, true);
                var parts = utf8.GetString(bytes[..end]).Split(':');
                if (parts.Length != 6 || parts[1] != "bool" || parts[2] != "1")
                    throw new NotSupportedException("Unsupported Electron2D shader type metadata.");
                if (!int.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out var width) || width is < 1 or > 4 ||
                    !int.TryParse(parts[4], NumberStyles.None, CultureInfo.InvariantCulture, out var length) || length is < 0 or > 1024)
                    throw new ArgumentException("Invalid boolean shape in shader type metadata.", nameof(code));
                var name = utf8.GetString(Convert.FromBase64String(parts[5]));
                if (string.IsNullOrWhiteSpace(name) || !result.TryAdd(name, (width, length)))
                    throw new ArgumentException("Shader type metadata requires unique nonblank uniform names.", nameof(code));
            }
            catch (Exception error) when (error is FormatException or DecoderFallbackException)
            { throw new ArgumentException("Malformed shader type metadata.", nameof(code), error); }
        }
        return result;
    }
}
