using System.Globalization;
using System.Text;

namespace Electron2D;

internal sealed partial class NativeSpeechText(string content, int prefix, int[]? positions, int positionCount, int sourceLength, string markPrefix)
{
    internal readonly string Content = content, MarkPrefix = markPrefix;
    internal readonly byte[] MarkPrefixBytes = Encoding.ASCII.GetBytes(markPrefix);
    private readonly int _prefix = prefix, _positionCount = positionCount, _sourceLength = sourceLength;
    private readonly int[]? _positions = positions;
    internal int SourcePosition(long position)
    {
        var offset = position - _prefix;
        if (_positions is null)
        {
            if (offset < 0 || offset > _sourceLength) throw new IOException("The speech provider reported a boundary outside its submitted source.");
            return (int)offset;
        }
        if (offset < 0 || offset >= _positionCount) throw new IOException("The speech provider reported a boundary outside its submitted source.");
        return _positions[(int)offset];
    }
    internal static NativeSpeechText Windows(string text, float pitch)
    {
        var prefix = "<pitch absmiddle=\"" + ((int)(pitch * 10 - 10)).ToString(CultureInfo.InvariantCulture) + "\">";
        var positions = new int[checked(text.Length + 1)]; var count = 0; var scalar = 0; var literalUntil = 0; var copied = 0;
        StringBuilder? normalized = null;
        for (var i = 0; i < text.Length;)
        {
            if (i >= literalUntil)
            {
                var tail = text.AsSpan(i); var end = tail.StartsWith("<!--") ? "-->" : tail.StartsWith("<![CDATA[") ? "]]>" : tail.StartsWith("<?") ? "?>" : null;
                if (end is not null) { var at = tail.IndexOf(end); literalUntil = at < 0 ? text.Length : i + at + end.Length; }
            }
            if (i >= literalUntil && text[i] == '&' && EntityTTS(text.AsSpan(i), out var length, out var units, out var decoded))
            {
                if (text[i + 1] == '#')
                {
                    // SAPI handles numeric references inconsistently; canonical literals/predefined entities preserve their XML value.
                    normalized ??= new(text.Length); normalized.Append(text.AsSpan(copied, i - copied));
                    normalized.Append(decoded switch { 34 => "&quot;", 38 => "&amp;", 39 => "&apos;", 60 => "&lt;", 62 => "&gt;", _ => new Rune((int)decoded).ToString() });
                    copied = i + length;
                }
                for (var j = 0; j < units; j++) positions[count++] = scalar;
                scalar += length; i += length;
            }
            else
            {
                var width = char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]) ? 2 : 1;
                for (var j = 0; j < width; j++) positions[count++] = scalar;
                scalar++; i += width;
            }
        }
        positions[count++] = scalar;
        if (normalized is not null) normalized.Append(text.AsSpan(copied));
        return new(prefix + (normalized?.ToString() ?? text) + "</pitch>", prefix.Length, positions, count, scalar, "");
    }
    internal static bool EntityTTS(ReadOnlySpan<char> text, out int length, out int units, out uint decoded)
    {
        length = 0; units = 1; decoded = 0;
        if (text.StartsWith("&amp;")) length = 5;
        else if (text.StartsWith("&lt;") || text.StartsWith("&gt;")) length = 4;
        else if (text.StartsWith("&quot;") || text.StartsWith("&apos;")) length = 6;
        else if (text.Length > 3 && text[1] == '#')
        {
            var index = 2; var hex = text[index] is 'x' or 'X'; if (hex) index++;
            var first = index; uint value = 0; var valid = true;
            for (; index < text.Length; index++)
            {
                var c = text[index]; var digit = c is >= '0' and <= '9' ? c - '0' : hex && c is >= 'a' and <= 'f' ? c - 'a' + 10 : hex && c is >= 'A' and <= 'F' ? c - 'A' + 10 : -1;
                if (digit < 0) break;
                if (value <= 0x10ffff) value = value * (hex ? 16u : 10u) + (uint)digit; else valid = false;
            }
            if (valid && index > first && index < text.Length && text[index] == ';' && value != 0 && Rune.IsValid((int)value)) { length = index + 1; units = value > 0xffff ? 2 : 1; decoded = value; }
        }
        return length != 0;
    }
}
