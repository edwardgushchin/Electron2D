using System.Globalization;
using System.Text;

namespace Electron2D;

internal sealed partial class NativeSpeechText
{
    internal static NativeSpeechText SSIP(string text, string language)
    {
        var markPrefix = "e2d_" + Guid.NewGuid().ToString("N") + "_";
        var trimmed = text.AsSpan().Trim();
        var rooted = trimmed.StartsWith("<speak", StringComparison.Ordinal) && trimmed.Length > 6 && (char.IsWhiteSpace(trimmed[6]) || trimmed[6] == '>') && trimmed.EndsWith("</speak>", StringComparison.Ordinal);
        var result = new StringBuilder(text.Length); if (!rooted) result.Append("<speak>");
        var scalar = 0; var index = 0;
        var variant = language.IndexOf('_'); if (variant >= 0) language = language[..variant];
        using var provider = TextServerManager.BeginConsumerRead();
        while (index < text.Length)
        {
            if (text[index] == '<')
            {
                var end = MarkupEnd(text, index);
                var markup = text.AsSpan(index, end - index); result.Append(markup);
                scalar += ScalarCount(markup); index = end; continue;
            }
            var next = text.IndexOf('<', index); if (next < 0) next = text.Length;
            var block = text.AsSpan(index, next - index);
            var decoded = new StringBuilder(block.Length); var offsets = new List<int> { 0 }; var positions = new List<int> { scalar };
            for (var at = 0; at < block.Length;)
            {
                if (block[at] == '&' && EntityTTS(block[at..], out var length, out _, out var value))
                {
                    if (value == 0) value = block[at..].StartsWith("&amp;") ? 38u : block[at..].StartsWith("&lt;") ? 60u : block[at..].StartsWith("&gt;") ? 62u : block[at..].StartsWith("&quot;") ? 34u : 39u;
                    decoded.Append(new Rune((int)value).ToString()); at += length; scalar += length;
                }
                else
                {
                    var width = char.IsHighSurrogate(block[at]) && at + 1 < block.Length && char.IsLowSurrogate(block[at + 1]) ? 2 : 1;
                    decoded.Append(block.Slice(at, width)); at += width; scalar++;
                }
                offsets.Add(at); positions.Add(scalar);
            }
            var breaks = provider.Provider.StringGetWordBreaks(decoded.ToString(), language);
            if ((breaks.Length & 1) != 0) throw new IOException("The text implementation returned invalid speech word ranges.");
            var copied = 0; var previousEnd = 0;
            for (var word = 0; word < breaks.Length; word += 2)
            {
                var start = breaks[word]; var end = breaks[word + 1];
                if (start < previousEnd || end < start || end >= offsets.Count) throw new IOException("The text implementation returned a speech word outside its source.");
                previousEnd = end; if (start == end) continue;
                var offset = offsets[end]; AppendSSIPText(result, block, copied, offset); copied = offset;
                result.Append("<mark name=\"").Append(markPrefix).Append(positions[end].ToString(CultureInfo.InvariantCulture)).Append("\"/>");
            }
            AppendSSIPText(result, block, copied, block.Length); index = next;
        }
        if (!rooted) result.Append("</speak>");
        return new(result.ToString(), 0, null, 0, scalar, markPrefix);
    }
    private static void AppendSSIPText(StringBuilder result, ReadOnlySpan<char> text, int start, int end)
    {
        var copied = start;
        for (var at = start; at < end; at++)
        {
            if (text[at] is not ('.' or '?' or '!') || at + 1 < text.Length && !char.IsWhiteSpace(text[at + 1])) continue;
            result.Append(text[copied..(at + 1)]);
            // Explicit sentence separation keeps native pause checkpoints ahead of following word marks.
            result.Append("<break/>"); copied = at + 1;
        }
        result.Append(text[copied..end]);
    }
    private static int ScalarCount(ReadOnlySpan<char> text)
    { var count = 0; foreach (var _ in text.EnumerateRunes()) count++; return count; }
    private static int MarkupEnd(string text, int index)
    {
        var tail = text.AsSpan(index);
        var terminator = tail.StartsWith("<!--") ? "-->" : tail.StartsWith("<![CDATA[") ? "]]>" : tail.StartsWith("<?") ? "?>" : null;
        if (terminator is not null) { var at = tail.IndexOf(terminator); return at < 0 ? text.Length : index + at + terminator.Length; }
        var quote = '\0';
        for (var at = index + 1; at < text.Length; at++)
        {
            var value = text[at];
            if (quote != '\0') { if (value == quote) quote = '\0'; }
            else if (value is '\'' or '"') quote = value;
            else if (value == '>') return at + 1;
        }
        return text.Length;
    }
}
