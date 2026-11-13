using System.Globalization;
using System.Text;
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
            using var stream = new MemoryStream(data, writable: false);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = MaximumEncodedBytes,
            });
            if (reader.MoveToContent() != XmlNodeType.Element || reader.LocalName != "svg")
                throw new InvalidDataException("The document root must be an SVG element.");

            var viewBox = reader.GetAttribute("viewBox");
            var viewSize = ParseViewBox(viewBox);
            var (inlineWidth, inlineHeight) = InlineSVGDimensions(reader.GetAttribute("style"));
            var width = ParseLength(inlineWidth ?? reader.GetAttribute("width"), viewSize?.Width ?? 300);
            var height = ParseLength(inlineHeight ?? reader.GetAttribute("height"), viewSize?.Height ?? 150);
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
            if (inlineWidth is not null || inlineHeight is not null)
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
                    data = RewriteSVGViewport(reader, size, viewBox ?? FormattableString.Invariant($"0 0 {logicalWidth:R} {logicalHeight:R}"));
                    return size;
                }
            }
            while (reader.Read()) { }
            return size;
        }
        catch (XmlException error) { throw new InvalidDataException("Invalid SVG XML.", error); }
    }

    private static (string? Width, string? Height) InlineSVGDimensions(string? style)
    {
        if (style is null) return default;
        string? width = null, height = null;
        var widthImportant = false;
        var heightImportant = false;
        var declaration = new StringBuilder();
        var quote = '\0';
        var depth = 0;
        for (var i = 0; i <= style.Length; i++)
        {
            var end = i == style.Length;
            var ch = end ? ';' : style[i];
            if (end || (ch == ';' && quote == '\0' && depth == 0))
            {
                ReadDimension();
                declaration.Clear();
                continue;
            }
            if (ch == '\\' && i + 1 < style.Length)
            {
                declaration.Append(ch).Append(style[++i]);
                continue;
            }
            if (quote != '\0')
            {
                if (ch == quote) quote = '\0';
            }
            else if (ch is '\'' or '"') quote = ch;
            else if (ch == '/' && i + 1 < style.Length && style[i + 1] == '*')
            {
                var close = style.IndexOf("*/", i + 2, StringComparison.Ordinal);
                declaration.Append(' ');
                i = close < 0 ? style.Length - 1 : close + 1;
                continue;
            }
            else if (ch is '(' or '[' or '{') depth++;
            else if ((ch is ')' or ']' or '}') && depth > 0) depth--;
            declaration.Append(ch);
        }
        return (width, height);

        void ReadDimension()
        {
            var entry = declaration.ToString().AsSpan();
            var colon = entry.IndexOf(':');
            if (colon < 0) return;
            var property = entry[..colon].Trim();
            var isWidth = property.Equals("width", StringComparison.OrdinalIgnoreCase);
            if (!isWidth && !property.Equals("height", StringComparison.OrdinalIgnoreCase)) return;
            var value = entry[(colon + 1)..].Trim();
            var bang = value.LastIndexOf('!');
            var important = bang >= 0 && value[(bang + 1)..].Trim().Equals("important", StringComparison.OrdinalIgnoreCase);
            if (important) value = value[..bang].TrimEnd();
            if (value.IsEmpty) return;
            ref var selected = ref (isWidth ? ref width : ref height);
            ref var selectedImportant = ref (isWidth ? ref widthImportant : ref heightImportant);
            if (selectedImportant && !important) return;
            selected = value.Equals("auto", StringComparison.OrdinalIgnoreCase) ? "100%" : value.ToString();
            selectedImportant = important;
        }
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

    private static double? ParseLength(string? value, double percentBase)
    {
        if (value is null) return null;
        value = value.Trim();
        double factor = 1;
        if (value.EndsWith('%')) { factor = percentBase / 100; value = value[..^1]; }
        else
        {
            foreach (var (unit, multiplier) in new (string, double)[]
            {
                ("px", 1), ("pt", 96d / 72), ("pc", 16), ("mm", 96d / 25.4), ("cm", 96d / 2.54), ("in", 96)
            })
                if (value.EndsWith(unit, StringComparison.OrdinalIgnoreCase))
                {
                    factor = multiplier; value = value[..^unit.Length]; break;
                }
        }
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ||
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
