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
            var ranges = (ControlCallbacks?.StructuredTextParser is { } query ? query(options, text) : OnStructuredTextParser(options, text)) ?? throw new InvalidOperationException("A structured text parser returned null.");
            foreach (var range in ranges)
            {
                if (range.Start < 0 || range.End < range.Start || range.End > count || range.Direction is < TextDirection.Auto or > TextDirection.Inherited)
                    throw new InvalidOperationException("A structured text parser returned an invalid scalar range or direction.");
                result.Add(range);
            }
            return;
        }
        TextServerManager.GetPrimaryInterface().ParseStructuredTextInto(parser, options, text, result);
    }

    internal static int CountScalars(ReadOnlySpan<char> text) => TextServer.CountScalars(text);
}
