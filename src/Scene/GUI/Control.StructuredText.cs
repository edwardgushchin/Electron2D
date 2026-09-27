using System.Text;

namespace Electron2D;

public partial class Control
{
    /// <summary>Supplies independent bidirectional contexts when a text control selects a custom parser.</summary>
    /// <param name="options">Read-only typed parser options.</param><param name="text">The text being parsed.</param>
    /// <returns>Logical scalar ranges in display order. Empty output retains the ordinary paragraph context.</returns>
    /// <remarks>Ranges must stay within the text and use a defined TextDirection. The text consumer calls this
    /// hook on its scene owner thread. The default implementation returns no overrides.</remarks>
    protected virtual TextBIDIRange[] OnStructuredTextParser(IReadOnlyList<string> options, string text) => [];

    internal void ParseStructuredText(StructuredTextParser parser, IReadOnlyList<string> options, string text, List<TextBIDIRange> result)
    {
        ThrowIfDisposed(); Tree?.EnsureOwnerThread(); result.Clear();
        var count = CountScalars(text.AsSpan());
        if (parser == StructuredTextParser.Custom)
        {
            var ranges = OnStructuredTextParser(options, text) ?? throw new InvalidOperationException("A structured text parser returned null.");
            foreach (var range in ranges)
            {
                if (range.Start < 0 || range.End < range.Start || range.End > count || range.Direction is < TextDirection.Auto or > TextDirection.Inherited)
                    throw new InvalidOperationException("A structured text parser returned an invalid scalar range or direction.");
                result.Add(range);
            }
            return;
        }
        if (parser == StructuredTextParser.List)
        {
            if (options.Count != 1 || string.IsNullOrEmpty(options[0])) return;
            var delimiter = options[0]; var separatorLength = CountScalars(delimiter.AsSpan()); var start = 0; var scalarStart = 0;
            while (start <= text.Length)
            {
                var next = text.IndexOf(delimiter, start, StringComparison.Ordinal);
                if (next < 0) next = text.Length;
                var end = scalarStart + CountScalars(text.AsSpan(start, next - start));
                if (end > scalarStart) result.Add(new(scalarStart, end, TextDirection.Inherited));
                if (next == text.Length) break;
                result.Add(new(end, end + separatorLength, TextDirection.Inherited));
                start = next + delimiter.Length; scalarStart = end + separatorLength;
            }
            return;
        }
        if (parser is not (StructuredTextParser.URI or StructuredTextParser.File or StructuredTextParser.Email))
        {
            result.Add(new(0, count, TextDirection.Inherited)); return;
        }
        var previous = 0; var index = 0; var local = true;
        foreach (var rune in text.EnumerateRunes())
        {
            var character = rune.Value;
            var separator = parser switch
            {
                StructuredTextParser.URI => character is '\\' or '/' or '.' or ':' or '&' or '=' or '@' or '?' or '#',
                StructuredTextParser.File => character is '\\' or '/' or ':',
                _ => local ? character == '@' : character == '.'
            };
            if (separator)
            {
                if (previous != index || parser == StructuredTextParser.Email && local) result.Add(new(previous, index, TextDirection.Auto));
                result.Add(new(index, index + 1, TextDirection.LTR)); previous = index + 1;
                if (parser == StructuredTextParser.Email) local = false;
            }
            index++;
        }
        if (previous != count) result.Add(new(previous, count, TextDirection.Auto));
    }

    internal static int CountScalars(ReadOnlySpan<char> text)
    {
        var count = 0; foreach (var rune in text.EnumerateRunes()) count++; return count;
    }
}
