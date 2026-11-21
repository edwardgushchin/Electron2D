using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Electron2D;

public sealed partial class Image
{
    /// <summary>Rasterizes an SVG string and replaces the image atomically.</summary>
    /// <param name="svg">An uncompressed SVG document.</param>
    /// <param name="scale">Finite positive multiplier for the SVG's intrinsic dimensions.</param>
    /// <remarks>Uses the same size, format and failure contract as <see cref="LoadSVGFromBuffer"/>.</remarks>
    /// <exception cref="ArgumentNullException">The document is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Scale is not finite and positive.</exception>
    /// <exception cref="InvalidDataException">The document is malformed or exceeds image limits.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public void LoadSVGFromString(string svg, float scale = 1f)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(svg);
        if (!float.IsFinite(scale) || scale <= 0) throw new ArgumentOutOfRangeException(nameof(scale));
        if (svg.Length > MaximumEncodedBytes) throw new InvalidDataException("SVG input exceeds the 64 MiB image limit.");
        byte[] bytes;
        try
        {
            var encoding = new UTF8Encoding(false, true);
            if (encoding.GetByteCount(svg) > MaximumEncodedBytes) throw new InvalidDataException("SVG input exceeds the 64 MiB image limit.");
            bytes = encoding.GetBytes(svg);
        }
        catch (EncoderFallbackException error) { throw new InvalidDataException("SVG text must be valid UTF-8 content.", error); }
        LoadSVGFromBuffer(bytes, scale);
    }

    internal static Vector2i SVGSize(byte[] data, float scale) => PrepareSVG(ref data, scale, forRaster: false);

    private static Vector2i PrepareSVG(ref byte[] data, float scale, bool forRaster)
    {
        if (!float.IsFinite(scale) || scale <= 0) throw new ArgumentOutOfRangeException(nameof(scale));
        try
        {
            using var reader = CreateSVGReader(data);
            if (reader.MoveToContent() != XmlNodeType.Element || reader.LocalName != "svg")
                throw new InvalidDataException("The document root must be an SVG element.");

            var viewBox = reader.GetAttribute("viewBox");
            var viewSize = ParseViewBox(viewBox);
            var widthAttribute = reader.GetAttribute("width");
            var heightAttribute = reader.GetAttribute("height");
            var (styleWidth, styleHeight) = CascadedSVGDimensions(reader);
            var widthValue = styleWidth ?? widthAttribute;
            var heightValue = styleHeight ?? heightAttribute;
            var width = ParseLength(widthValue, viewSize?.Width ?? 300);
            var height = ParseLength(heightValue, viewSize?.Height ?? 150);
            if (width is null && height is null)
            {
                width = viewSize?.Width ?? 300;
                height = viewSize?.Height ?? 150;
            }
            else if (width is null) width = viewSize is { } view ? height * view.Width / view.Height : 300;
            else if (height is null) height = viewSize is { } view ? width * view.Height / view.Width : 150;

            var logicalWidth = width ?? throw new InvalidDataException("SVG width is missing.");
            var logicalHeight = height ?? throw new InvalidDataException("SVG height is missing.");
            var size = new Vector2i(RasterDimension(logicalWidth, scale), RasterDimension(logicalHeight, scale));
            if (styleWidth is not null || styleHeight is not null ||
                IsSVGCalculation(widthValue) || IsSVGCalculation(heightValue))
            {
                var userWidth = viewSize?.Width ?? logicalWidth;
                var userHeight = viewSize?.Height ?? logicalHeight;
                if (userWidth > float.MaxValue || userHeight > float.MaxValue ||
                    size.X / userWidth > float.MaxValue || size.Y / userHeight > float.MaxValue)
                    throw new InvalidDataException("SVG viewport coordinates exceed the rasterizer's numeric range.");
                if (forRaster)
                {
                    // SDL_image fits its own viewport with a uniform scale. Pass the final integer
                    // viewport and retain the document's logical coordinate system in the viewBox.
                    using var rasterReader = CreateSVGReader(data);
                    rasterReader.MoveToContent();
                    data = RewriteSVGViewport(rasterReader, size, viewBox ?? FormattableString.Invariant($"0 0 {logicalWidth:R} {logicalHeight:R}"));
                }
            }
            return size;
        }
        catch (XmlException error) { throw new InvalidDataException("Invalid SVG XML.", error); }
    }

    private static XmlReader CreateSVGReader(byte[] data) =>
        XmlReader.Create(new MemoryStream(data, writable: false), new XmlReaderSettings
        {
            CloseInput = true,
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = MaximumEncodedBytes,
        });

    private static readonly char[] CSSWhitespace = [' ', '\t', '\n', '\r', '\f'];

    private static readonly Regex SVGSelectorName = new(
        @"\G(?:--|-?[_a-zA-Z\u0080-\uFFFF])[-_a-zA-Z0-9\u0080-\uFFFF]*",
        RegexOptions.CultureInvariant);

    private readonly record struct SVGDeclaration(
        string? Value, (bool Important, bool Inline, int IDs, int Classes, int Types) Priority);

    private static (string? Width, string? Height) CascadedSVGDimensions(XmlReader reader)
    {
        var rootName = reader.LocalName;
        var rootNamespace = reader.NamespaceURI;
        var id = reader.GetAttribute("id");
        var classes = new HashSet<string>(
            (reader.GetAttribute("class") ?? "").Split(CSSWhitespace, StringSplitOptions.RemoveEmptyEntries),
            StringComparer.Ordinal);
        var inline = reader.GetAttribute("style");
        SVGDeclaration width = default, height = default;
        var properties = new Dictionary<string, SVGDeclaration>(StringComparer.Ordinal);

        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element || reader.Depth == 0 || reader.LocalName != "style" ||
                reader.NamespaceURI != rootNamespace || reader.IsEmptyElement)
                continue;

            var type = reader.GetAttribute("type")?.Trim();
            var media = reader.GetAttribute("media")?.Trim();
            if ((!string.IsNullOrEmpty(type) && !type.Equals("text/css", StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(media) && !media.Equals("all", StringComparison.OrdinalIgnoreCase)))
                continue;

            using var style = reader.ReadSubtree();
            var css = new StringBuilder();
            var textOnly = true;
            while (style.Read())
            {
                if (style.NodeType == XmlNodeType.Element && style.Depth > 0)
                    textOnly = false;
                if (style.NodeType is XmlNodeType.Text or XmlNodeType.CDATA or XmlNodeType.Whitespace or XmlNodeType.SignificantWhitespace)
                    css.Append(style.Value);
            }
            if (textOnly)
                ApplyStylesheet(css.ToString());
        }

        if (inline is not null)
            ApplyDeclarations(inline, inlineStyle: true, default);
        var variables = new SVGVariables(properties);
        return (ResolveDimension(width.Value), ResolveDimension(height.Value));

        string? ResolveDimension(string? value)
        {
            if (value is null)
                return null;
            var resolved = variables.Resolve(value).AsSpan().Trim(CSSWhitespace);
            return resolved.Equals("auto", StringComparison.OrdinalIgnoreCase) ? "100%" : resolved.ToString();
        }

        void ApplyStylesheet(string css)
        {
            var offset = 0;
            while (offset < css.Length)
            {
                var selectors = ReadCSSSegment(css, ref offset, "{;", out var delimiter);
                if (delimiter != '{')
                    continue;
                var declarations = ReadCSSSegment(css, ref offset, "}", out delimiter);
                var selectorOffset = 0;
                SkipCSSTrivia(selectors, ref selectorOffset);
                if (delimiter != '}' || selectors.AsSpan(selectorOffset).StartsWith("@", StringComparison.Ordinal))
                    continue;

                (int IDs, int Classes, int Types)? best = null;
                char separator;
                do
                {
                    var selector = ReadCSSSegment(selectors, ref selectorOffset, ",", out separator);
                    var start = 0;
                    SkipCSSTrivia(selector, ref start);
                    if (start == selector.Length)
                    {
                        best = null;
                        break;
                    }

                    var specificity = MatchSVGRoot(selector, start, rootName, id, classes);
                    if (specificity is { } match && (best is null || match.CompareTo(best.Value) > 0))
                        best = match;
                }
                while (separator == ',');

                if (best is { } winner)
                    ApplyDeclarations(declarations, inlineStyle: false, winner);
            }
        }

        void ApplyDeclarations(string css, bool inlineStyle, (int IDs, int Classes, int Types) specificity)
        {
            var offset = 0;
            while (offset < css.Length)
            {
                var entry = ReadCSSSegment(css, ref offset, ";", out _, preserveComments: false).AsSpan();
                var colon = entry.IndexOf(':');
                if (colon < 0)
                    continue;
                var property = entry[..colon].Trim(CSSWhitespace);
                var isWidth = property.Equals("width", StringComparison.OrdinalIgnoreCase);
                var isHeight = property.Equals("height", StringComparison.OrdinalIgnoreCase);
                string? customName = null;
                if (!isWidth && !isHeight && property.StartsWith("--", StringComparison.Ordinal))
                    customName = SVGCustomPropertyName(property.ToString());
                if (!isWidth && !isHeight && customName is null)
                    continue;

                var value = entry[(colon + 1)..].Trim(CSSWhitespace);
                var bang = value.LastIndexOf('!');
                var important = bang >= 0 && value[(bang + 1)..].Trim(CSSWhitespace).Equals("important", StringComparison.OrdinalIgnoreCase);
                if (important)
                    value = value[..bang].TrimEnd(CSSWhitespace);
                if (value.IsEmpty && customName is null)
                    continue;

                var priority = (important, inlineStyle, specificity.IDs, specificity.Classes, specificity.Types);
                var declaration = new SVGDeclaration(value.ToString(), priority);
                if (customName is not null)
                {
                    if (!properties.TryGetValue(customName, out var previous) || priority.CompareTo(previous.Priority) >= 0)
                        properties[customName] = declaration;
                    continue;
                }

                ref var selected = ref (isWidth ? ref width : ref height);
                if (selected.Value is null || priority.CompareTo(selected.Priority) >= 0)
                    selected = declaration;
            }
        }
    }

    private static (int IDs, int Classes, int Types)? MatchSVGRoot(string selector, int offset, string name, string? id, HashSet<string> classes)
    {
        var specificity = (IDs: 0, Classes: 0, Types: 0);
        var start = offset;
        if (selector[offset] == '*')
        {
            offset++;
            if (SkipCSSTrivia(selector, ref offset) && offset < selector.Length)
                return null;
        }

        while (offset < selector.Length)
        {
            var componentStart = offset;
            var prefix = selector[offset];
            if (prefix is '#' or '.' or ':')
            {
                offset++;
                // A hash and its name form one token. A dot or colon is a separate token.
                if (prefix != '#' && SkipCSSTrivia(selector, ref offset))
                    return null;
            }
            else
                prefix = '\0';

            var identifier = SVGSelectorName.Match(selector, offset);
            if (!identifier.Success)
                return null;

            var value = identifier.Value;
            switch (prefix)
            {
                case '\0' when componentStart == start && value == name:
                    specificity.Types++;
                    break;
                case '#' when value == id:
                    specificity.IDs++;
                    break;
                case '.' when classes.Contains(value):
                case ':' when value.Equals("root", StringComparison.OrdinalIgnoreCase):
                    specificity.Classes++;
                    break;
                default:
                    return null;
            }
            offset += identifier.Length;
            if (SkipCSSTrivia(selector, ref offset) && offset < selector.Length)
                return null;
        }
        return specificity;
    }

    private static bool SkipCSSTrivia(string css, ref int offset)
    {
        var whitespace = false;
        while (offset < css.Length)
        {
            if (CSSWhitespace.AsSpan().Contains(css[offset]))
            {
                whitespace = true;
                offset++;
            }
            else if (css.AsSpan(offset).StartsWith("/*", StringComparison.Ordinal))
                offset = EndCSSComment(css, offset);
            else
                break;
        }
        return whitespace;
    }

    private static int EndCSSComment(string css, int start)
    {
        var close = css.IndexOf("*/", start + 2, StringComparison.Ordinal);
        return close < 0 ? css.Length : close + 2;
    }

    private static string ReadCSSSegment(string css, ref int offset, string separators, out char delimiter, bool preserveComments = true)
    {
        var segment = new StringBuilder();
        var quote = '\0';
        var depth = 0;
        delimiter = '\0';
        while (offset < css.Length)
        {
            var ch = css[offset++];
            if (quote == '\0' && depth == 0 && separators.Contains(ch))
            {
                delimiter = ch;
                break;
            }
            if (ch == '\\' && offset < css.Length)
            {
                segment.Append(ch).Append(css[offset++]);
                continue;
            }
            if (quote != '\0')
            {
                if (ch == quote)
                    quote = '\0';
            }
            else if (ch is '\'' or '"')
                quote = ch;
            else if (ch == '/' && offset < css.Length && css[offset] == '*')
            {
                var start = offset - 1;
                offset = EndCSSComment(css, start);
                if (preserveComments || depth > 0)
                    segment.Append(css.AsSpan(start, offset - start));
                else
                    segment.Append(' ');
                continue;
            }
            else if (ch is '(' or '[' or '{')
                depth++;
            else if ((ch is ')' or ']' or '}') && depth > 0)
                depth--;
            segment.Append(ch);
        }
        return segment.ToString();
    }

    private static byte[] RewriteSVGViewport(XmlReader reader, Vector2i size, string viewBox)
    {
        using var output = new MemoryStream();
        using (var writer = XmlWriter.Create(output, new XmlWriterSettings { OmitXmlDeclaration = true, Encoding = new UTF8Encoding(false) }))
        {
            var empty = reader.IsEmptyElement;
            writer.WriteStartElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
            writer.WriteAttributeString("width", XmlConvert.ToString(size.X));
            writer.WriteAttributeString("height", XmlConvert.ToString(size.Y));
            writer.WriteAttributeString("viewBox", viewBox);
            if (reader.GetAttribute("preserveAspectRatio") is null)
                writer.WriteAttributeString("preserveAspectRatio", "xMidYMid meet");
            while (reader.MoveToNextAttribute())
                if (reader.NamespaceURI.Length != 0 || reader.LocalName is not ("width" or "height" or "viewBox"))
                    writer.WriteAttributeString(reader.Prefix, reader.LocalName, reader.NamespaceURI, reader.Value);
            reader.MoveToElement();
            reader.Read();
            if (!empty)
                while (reader.NodeType != XmlNodeType.EndElement)
                    writer.WriteNode(reader, false);
            writer.WriteEndElement();
            while (reader.Read()) { }
        }
        if (output.Length > MaximumEncodedBytes)
            throw new InvalidDataException("SVG viewport normalization exceeds the 64 MiB image limit.");
        return output.ToArray();
    }

    private static (double Width, double Height)? ParseViewBox(string? value)
    {
        if (value is null) return null;
        var parts = value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 4)
            parts = value.Split([',', ' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 4 || !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
            !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) ||
            !double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var width) ||
            !double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var height) ||
            !double.IsFinite(x) || !double.IsFinite(y) || !double.IsFinite(width) || !double.IsFinite(height) ||
            Math.Abs(x) > float.MaxValue || Math.Abs(y) > float.MaxValue ||
            width < float.Epsilon || height < float.Epsilon || width > float.MaxValue || height > float.MaxValue)
            throw new InvalidDataException("SVG viewBox must contain four finite numbers with positive dimensions.");
        return (width, height);
    }

    private static readonly (string Unit, double Factor)[] SVGPhysicalUnits =
    [
        ("px", 1), ("pt", 96d / 72), ("pc", 16), ("mm", 96d / 25.4), ("cm", 96d / 2.54), ("in", 96),
    ];

    private const NumberStyles SVGNumberStyle =
        NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;

    private static double? ParseLength(string? value, double percentBase)
    {
        if (value is null) return null;
        value = value.Trim(CSSWhitespace);
        if (IsSVGCalculation(value))
            return new SVGCalculation(value, percentBase).Read();

        double factor = 1;
        if (value.EndsWith('%')) { factor = percentBase / 100; value = value[..^1]; }
        else
        {
            foreach (var (unit, multiplier) in SVGPhysicalUnits)
                if (value.EndsWith(unit, StringComparison.OrdinalIgnoreCase))
                {
                    factor = multiplier; value = value[..^unit.Length]; break;
                }
        }
        if (!double.TryParse(value, SVGNumberStyle, CultureInfo.InvariantCulture, out var number) ||
            !double.IsFinite(number) || number <= 0 || !double.IsFinite(number * factor))
            throw new InvalidDataException("SVG dimensions must be positive finite pixel or physical lengths.");
        return number * factor;
    }

    private static int RasterDimension(double length, float scale)
    {
        var pixels = Math.Max(1, Math.Round(length * scale, MidpointRounding.AwayFromZero));
        if (!double.IsFinite(pixels) || pixels > 16384)
            throw new InvalidDataException("SVG raster dimensions exceed 16,384 pixels per axis.");
        return (int)pixels;
    }
}
