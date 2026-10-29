using System.Text;
using System.Runtime.InteropServices;

namespace Electron2D;

public abstract partial class TextServer
{
    /// <summary>Identifies capabilities supplied by a text implementation.</summary>
    [Flags]
    public enum Feature
    {
        /// <summary>Single-direction layout.</summary>
        SimpleLayout = 1,
        /// <summary>Bidirectional paragraph layout.</summary>
        BIDILayout = 2,
        /// <summary>Vertical glyph layout.</summary>
        VerticalLayout = 4,
        /// <summary>Contextual glyph shaping.</summary>
        Shaping = 8,
        /// <summary>Arabic elongation justification.</summary>
        KashidaJustification = 16,
        /// <summary>Unicode line and word iterators.</summary>
        BreakIterators = 32,
        /// <summary>Authored bitmap font sources.</summary>
        FontBitmap = 64,
        /// <summary>Scalable outline font sources.</summary>
        FontDynamic = 128,
        /// <summary>Multichannel signed-distance-field font caches.</summary>
        FontMSDF = 256,
        /// <summary>Operating-system font discovery.</summary>
        FontSystem = 512,
        /// <summary>OpenType variable font instances.</summary>
        FontVariable = 1024,
        /// <summary>Locale-sensitive Unicode case mapping.</summary>
        ContextSensitiveCaseConversion = 2048,
        /// <summary>Packaged Unicode support data.</summary>
        UseSupportData = 4096,
        /// <summary>Unicode identifier validation.</summary>
        UnicodeIdentifiers = 8192,
        /// <summary>Unicode confusable and spoof analysis.</summary>
        UnicodeSecurity = 16384
    }
    private const Feature NativeFeatures = (Feature)32767;
    /// <summary>Returns the implementation's supported capability mask.</summary>
    /// <returns>A combination of defined Feature values.</returns>
    public Feature GetFeatures()
    {
        var implementationCall = EnterImplementation();
        Exception? callbackFailure = null;
        try
        {
            var features = OnGetFeatures();
            if ((features & ~NativeFeatures) != 0) throw new InvalidOperationException("The text implementation returned undefined feature bits.");
            return features;
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }
    /// <summary>Supplies the implementation's capability mask.</summary><returns>Defined Feature bits.</returns>
    protected virtual Feature OnGetFeatures() => NativeFeatures;
    /// <summary>Tests one defined text capability.</summary><param name="feature">One capability.</param><returns>Whether it is supported.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The capability is undefined or combines flags.</exception>
    public bool HasFeature(Feature feature)
    {
        var implementationCall = EnterImplementation();
        Exception? callbackFailure = null;
        try
        {
            if (!Enum.IsDefined(feature)) throw new ArgumentOutOfRangeException(nameof(feature));
            return OnHasFeature(feature);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }
    /// <summary>Tests one text capability against the current implementation mask.</summary>
    /// <param name="feature">A validated capability.</param><returns>Whether it is supported.</returns>
    protected virtual bool OnHasFeature(Feature feature) => (GetFeatures() & feature) == feature;

    /// <summary>Parses independently directed logical contexts for structured text.</summary>
    /// <param name="parser">Structured text kind; Custom has no native override.</param>
    /// <param name="options">Read-only typed options. List accepts one nonempty delimiter.</param><param name="text">Source text.</param>
    /// <returns>Caller-owned logical scalar ranges in display order.</returns>
    /// <exception cref="ArgumentException">A string exceeds the text boundary limit.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The parser is undefined.</exception>
    /// <exception cref="InvalidOperationException">A callback returns invalid scalar ranges or directions.</exception>
    public TextBIDIRange[] ParseStructuredText(StructuredTextParser parser, IReadOnlyList<string> options, string text)
    { using var read = new ParserRead(this, parser, options, text); return read.Ranges.ToArray(); }
    internal void ParseStructuredTextInto(StructuredTextParser parser, IReadOnlyList<string> options, string text, List<TextBIDIRange> result)
    {
        using var consumer = TextServerManager.BeginConsumerRead(this);
        using var read = new ParserRead(this, parser, options, text);
        foreach (var range in read.Ranges) result.Add(range);
    }
    [ThreadStatic] private static int _parserDepth;
    [ThreadStatic] private static List<List<TextBIDIRange>>? _parserBuffers;
    private readonly ref struct ParserRead
    {
        private readonly ImplementationCall _call;
        internal readonly ReadOnlySpan<TextBIDIRange> Ranges;
        internal ParserRead(TextServer owner, StructuredTextParser parser, IReadOnlyList<string> options, string text)
        {
            _call = owner.EnterImplementation(); var entered = false;
            try
            {
                if (!Enum.IsDefined(parser)) throw new ArgumentOutOfRangeException(nameof(parser));
                ArgumentNullException.ThrowIfNull(options); NativeTextBreak.ValidateString(text);
                for (var i = 0; i < options.Count; i++) NativeTextBreak.ValidateString(options[i]);
                if (_parserDepth >= Font.MaximumReadAttempts) throw new InvalidOperationException("Structured parser reentry exceeds sixty-four levels.");
                _parserDepth++; entered = true;
                Ranges = owner.OnParseStructuredText(parser, options, text);
                var count = CountScalars(text);
                foreach (var range in Ranges)
                    if (range.Start < 0 || range.End < range.Start || range.End > count || range.Direction is < TextDirection.Auto or > TextDirection.Inherited)
                        throw new InvalidOperationException("A structured text callback returned an invalid scalar range or direction.");
            }
            catch { if (entered) _parserDepth--; _call.Dispose(); throw; }
        }
        public void Dispose() { _parserDepth--; _call.Dispose(); }
    }
    /// <summary>Supplies structured text contexts through the native built-in parser.</summary>
    /// <param name="parser">A validated parser kind.</param><param name="options">Read-only parser options.</param><param name="text">Source text.</param>
    /// <returns>Borrowed scalar contexts, valid until the next parse at this callback depth; Custom returns an empty span.</returns>
    protected virtual ReadOnlySpan<TextBIDIRange> OnParseStructuredText(StructuredTextParser parser, IReadOnlyList<string> options, string text)
    {
        if (parser == StructuredTextParser.Custom) return [];
        var buffers = _parserBuffers ??= [];
        while (buffers.Count < Math.Max(1, _parserDepth)) buffers.Add([]);
        var result = buffers[Math.Max(0, _parserDepth - 1)]; result.Clear();
        ParseStructuredTextCore(parser, options, text, result); return CollectionsMarshal.AsSpan(result);
    }
    private static void ParseStructuredTextCore(StructuredTextParser parser, IReadOnlyList<string> options, string text, List<TextBIDIRange> result)
    {
        var count = CountScalars(text);
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
    { var count = 0; foreach (var rune in text.EnumerateRunes()) count++; return count; }

}
